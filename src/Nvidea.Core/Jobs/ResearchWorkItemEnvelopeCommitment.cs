using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

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
/// This is a second, local CAS after the audited dispatch reservation: if the process dies before this
/// CAS, no remote create has occurred and recovery fails closed rather than signing mutable shared
/// transport state. Once present, the commitment is immutable and participates in job-store CAS.
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
