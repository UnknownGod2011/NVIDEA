using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Canonical commitment to the exact protected remote-research envelope selected by the trusted
/// client before provider dispatch. The encoding is deliberately independent of JSON serializer
/// property order/settings: every UTF-8 field is domain-separated and length-prefixed before SHA-256.
/// </summary>
public static class ResearchWorkItemEnvelopeCommitment
{
    public const string CommitmentVersion = "nvidea.research.work-item-envelope.sha256.v1";
    public const int HexLength = 64;

    public static string ComputeSha256(ProtectedResearchWorkItemEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendField(hash, CommitmentVersion);
        AppendField(hash, envelope.ProtocolVersion);
        AppendField(hash, envelope.OpaqueWorkItemId);
        AppendField(hash, envelope.WrappedDataKey);
        AppendField(hash, envelope.Nonce);
        AppendField(hash, envelope.Ciphertext);
        AppendField(hash, envelope.AuthenticationTag);
        AppendField(hash, envelope.CreatedAt.ToUniversalTime().ToString("O"));
        AppendField(hash, envelope.ExpiresAt.ToUniversalTime().ToString("O"));
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    public static string ValidateCanonicalSha256(string value, string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length != HexLength
            || value.Any(static ch => !((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'))))
        {
            throw new InvalidOperationException(
                $"{parameterName ?? "Work-item envelope commitment"} must be a canonical lowercase SHA-256 hex digest.");
        }

        return value;
    }

    public static bool FixedTimeEquals(string expectedCanonicalSha256, string actualCanonicalSha256)
    {
        ValidateCanonicalSha256(expectedCanonicalSha256, nameof(expectedCanonicalSha256));
        ValidateCanonicalSha256(actualCanonicalSha256, nameof(actualCanonicalSha256));
        var expected = Convert.FromHexString(expectedCanonicalSha256);
        var actual = Convert.FromHexString(actualCanonicalSha256);
        try
        {
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expected);
            CryptographicOperations.ZeroMemory(actual);
        }
    }

    private static void AppendField(IncrementalHash hash, string value)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (value is null)
            throw new InvalidOperationException("Protected work-item envelope contains a null commitment field.");

        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }
}

/// <summary>
/// Persists the originating client's envelope commitment before any Nebius create call is allowed.
/// Retained for lower-level/legacy compositions. Production dispatch uses
/// <see cref="AtomicRemoteResearchDispatchReservation"/> so reservation provenance, audit intent and
/// the envelope commitment share one compare-and-swap boundary.
/// </summary>
public sealed class DurableResearchEnvelopeCommitment
{
    private readonly JsonAgentJobStore _store;

    public DurableResearchEnvelopeCommitment(JsonAgentJobStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<AgentJobRecord> AttachAsync(
        Guid jobId,
        ProtectedResearchWorkItemEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));
        ArgumentNullException.ThrowIfNull(envelope);

        var current = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no dispatch reservation provenance.");

        if (!string.Equals(current.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)
            || current.State != AgentJobState.Running
            || current.ExecutionLocation != JobExecutionLocation.Local
            || provenance.State != RemoteResearchProvenanceState.DispatchReserved
            || !string.IsNullOrWhiteSpace(provenance.RemoteJobId)
            || current.PendingAuditEvent is not null)
        {
            throw new InvalidOperationException(
                "Envelope commitment can only attach to an audit-settled local DispatchReserved research stage.");
        }

        if (!string.Equals(provenance.OpaqueWorkItemId, envelope.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(envelope.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
        {
            throw new CryptographicException("Protected work-item envelope does not match the durable dispatch reservation.");
        }

        var commitment = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope);
        if (current.RemoteWorkItemEnvelopeSha256 is not null)
        {
            if (!ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(current.RemoteWorkItemEnvelopeSha256, commitment))
                throw new CryptographicException("A different protected work-item envelope is already committed for this reservation.");
            return current;
        }

        var replacement = current with
        {
            RemoteWorkItemEnvelopeSha256 = commitment,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while the work-item envelope commitment was being attached.");

        return replacement;
    }
}

/// <summary>
/// Production trust-root boundary for a remote research dispatch. The exact checkpoint reservation,
/// opaque work-item identity/lifetime, validated audit intent, and canonical encrypted-envelope
/// commitment are committed in one durable job CAS. Nebius Create must not run unless this method
/// returns after settling the exact staged audit event.
/// </summary>
public sealed class AtomicRemoteResearchDispatchReservation
{
    private readonly JsonAgentJobStore _store;
    private readonly DurableJobAuditOutbox _auditOutbox;

    public AtomicRemoteResearchDispatchReservation(JsonAgentJobStore store, IAuditTrail auditTrail)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _auditOutbox = new DurableJobAuditOutbox(_store, auditTrail ?? throw new ArgumentNullException(nameof(auditTrail)));
    }

    public async Task<AgentJobRecord> ReserveAsync(
        RemoteResearchDispatchReservation reservation,
        ProtectedResearchWorkItemEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(envelope);
        if (reservation.LocalJobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(reservation));
        if (string.IsNullOrWhiteSpace(reservation.CheckpointStep)
            || string.IsNullOrWhiteSpace(reservation.OpaqueWorkItemId))
            throw new InvalidOperationException("Dispatch reservation metadata is incomplete.");
        if (!string.Equals(reservation.OpaqueWorkItemId, envelope.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(envelope.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
            throw new CryptographicException("Protected work-item envelope does not match dispatch reservation identity.");

        var expiresAt = reservation.WorkItemExpiresAt ?? envelope.ExpiresAt;
        if (expiresAt != envelope.ExpiresAt
            || expiresAt <= reservation.ReservedAt
            || expiresAt - reservation.ReservedAt > ResearchWorkItemProtector.MaxLifetime)
            throw new InvalidOperationException("Dispatch reservation does not match the protected work-item lifetime.");

        var current = await _store.GetAsync(reservation.LocalJobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{reservation.LocalJobId}' was not found.");
        ValidateTarget(current, reservation.CheckpointStep);

        var checkpoint = current.Checkpoint!;
        var provenance = new RemoteResearchProvenance(
            ProtocolVersion: ResearchWorkItemProtector.ProtocolVersion,
            OpaqueWorkItemId: reservation.OpaqueWorkItemId,
            RemoteJobId: null,
            InputCheckpointStep: checkpoint.Step,
            InputCheckpointSavedAt: checkpoint.SavedAt,
            DispatchedAt: reservation.ReservedAt,
            State: RemoteResearchProvenanceState.DispatchReserved,
            WorkItemExpiresAt: expiresAt);
        var replacement = current with
        {
            State = AgentJobState.Running,
            ExecutionLocation = JobExecutionLocation.Local,
            LastError = null,
            NextAttemptAt = null,
            RemoteResearch = provenance,
            RemoteWorkItemEnvelopeSha256 = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var audit = CreateReservationAudit(replacement);
        var durableReplacement = _auditOutbox.Stage(replacement, audit);

        if (!await _store.CompareExchangeAsync(current, durableReplacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while atomic remote dispatch authority was being reserved.");

        try
        {
            return await _auditOutbox.FlushAsync(durableReplacement, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not RemoteResearchDispatchReservationAuditPendingException)
        {
            throw new RemoteResearchDispatchReservationAuditPendingException(ex);
        }
    }

    private static void ValidateTarget(AgentJobRecord current, string checkpointStep)
    {
        if (!string.Equals(current.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Remote dispatch reservation only accepts research jobs.");
        if (current.PendingAuditEvent is not null)
            throw new InvalidOperationException("Remote dispatch reservation cannot replace an unsettled audit event.");
        if (current.State != AgentJobState.Pending || current.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Only a pending local research stage can be reserved for remote dispatch.");
        if (current.RemoteResearch is { State: not RemoteResearchProvenanceState.ResultApplied })
            throw new InvalidOperationException("Research job already carries unfinished remote execution provenance.");
        if (current.ApprovalScope is not null)
            throw new InvalidOperationException("Approval-bearing research cannot be dispatched remotely.");

        var checkpoint = current.Checkpoint
            ?? throw new InvalidOperationException("Research dispatch requires a durable input checkpoint.");
        if (!string.Equals(checkpoint.Step, checkpointStep, StringComparison.Ordinal))
            throw new InvalidOperationException("Dispatch reservation does not match the current research checkpoint.");
    }

    private static AuditEvent CreateReservationAudit(AgentJobRecord job)
    {
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            job.Definition.CapabilityId,
            job.JobId.ToString("N"),
            "research.remote_dispatch_reserved",
            job.Definition.Risk,
            true,
            false,
            string.Empty,
            "Encrypted research stage, exact envelope commitment, and provider-dispatch authority reserved atomically.",
            new Dictionary<string, string>
            {
                ["jobType"] = job.Definition.JobType,
                ["state"] = job.State.ToString(),
                ["executionLocation"] = job.ExecutionLocation.ToString(),
                ["attempt"] = job.Attempt.ToString()
            });
        AuditEventTrust.ValidateForPersistence(auditEvent, nameof(job));
        return auditEvent;
    }
}
