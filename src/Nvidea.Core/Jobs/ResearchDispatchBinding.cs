using System.Security.Cryptography;
using System.Text;

namespace Nvidea.Core.Jobs;

public sealed record ProtectedResearchDispatchBinding(
    string ProtocolVersion,
    string OpaqueWorkItemId,
    string RemoteJobId,
    string RemoteJobName,
    DateTimeOffset PublishedAt,
    DateTimeOffset ExpiresAt,
    string Signature,
    string? WorkItemEnvelopeSha256 = null);

public interface IProtectedResearchDispatchBindingTransport
{
    Task PutAsync(
        ProtectedResearchDispatchBinding binding,
        CancellationToken cancellationToken = default);

    Task<ProtectedResearchDispatchBinding?> GetAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Authenticates the control-plane handoff that maps an opaque work-item id to the authoritative
/// Nebius resource id returned after Serverless job creation. V2 additionally signs the originating
/// client's canonical SHA-256 commitment to the exact encrypted work-item envelope. A worker that
/// requires V2 can therefore detect substitution by a writable shared transport before decryption or
/// execution. V1 remains verifiable only for bounded legacy recovery/cancellation compatibility.
/// </summary>
public static class ResearchDispatchBindingProtector
{
    public const string ProtocolVersion = "nvidea.research.dispatch-binding.v1";
    public const string EnvelopeBoundProtocolVersion = "nvidea.research.dispatch-binding.v2";
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromHours(24);

    public static ProtectedResearchDispatchBinding Sign(
        string opaqueWorkItemId,
        string remoteJobId,
        DateTimeOffset publishedAt,
        DateTimeOffset expiresAt,
        string clientPrivateKeyPem) =>
        SignCore(
            ProtocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            workItemEnvelopeSha256: null,
            publishedAt,
            expiresAt,
            clientPrivateKeyPem);

    public static ProtectedResearchDispatchBinding SignEnvelopeBound(
        string opaqueWorkItemId,
        string remoteJobId,
        string workItemEnvelopeSha256,
        DateTimeOffset publishedAt,
        DateTimeOffset expiresAt,
        string clientPrivateKeyPem)
    {
        ResearchWorkItemEnvelopeCommitment.ValidateCanonicalSha256(
            workItemEnvelopeSha256,
            nameof(workItemEnvelopeSha256));
        return SignCore(
            EnvelopeBoundProtocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            workItemEnvelopeSha256,
            publishedAt,
            expiresAt,
            clientPrivateKeyPem);
    }

    public static ProtectedResearchDispatchBinding Verify(
        ProtectedResearchDispatchBinding binding,
        string expectedOpaqueWorkItemId,
        string clientPublicKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ValidateOpaqueId(expectedOpaqueWorkItemId);
        if (!string.Equals(binding.ProtocolVersion, ProtocolVersion, StringComparison.Ordinal)
            && !string.Equals(binding.ProtocolVersion, EnvelopeBoundProtocolVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported remote research dispatch-binding protocol version.");
        }

        var isEnvelopeBound = string.Equals(
            binding.ProtocolVersion,
            EnvelopeBoundProtocolVersion,
            StringComparison.Ordinal);
        if (isEnvelopeBound)
        {
            ResearchWorkItemEnvelopeCommitment.ValidateCanonicalSha256(
                binding.WorkItemEnvelopeSha256 ?? string.Empty,
                nameof(binding.WorkItemEnvelopeSha256));
        }
        else if (!string.IsNullOrEmpty(binding.WorkItemEnvelopeSha256))
        {
            throw new CryptographicException("Legacy dispatch binding unexpectedly contains an envelope commitment.");
        }

        if (!string.Equals(binding.OpaqueWorkItemId, expectedOpaqueWorkItemId, StringComparison.Ordinal))
            throw new CryptographicException("Remote research dispatch binding does not match the requested opaque work-item id.");
        ValidateOpaqueId(binding.OpaqueWorkItemId);
        ValidateRemoteJobId(binding.RemoteJobId);
        ValidateLifetime(binding.PublishedAt, binding.ExpiresAt);
        if (!string.Equals(binding.RemoteJobName, GetDeterministicRemoteJobName(binding.OpaqueWorkItemId), StringComparison.Ordinal))
            throw new CryptographicException("Remote research dispatch binding contains an unexpected deterministic job name.");
        if (binding.ExpiresAt <= (now ?? DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Remote research dispatch binding has expired.");
        if (string.IsNullOrWhiteSpace(clientPublicKeyPem))
            throw new ArgumentException("Client public key is required.", nameof(clientPublicKeyPem));

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(binding.Signature);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Remote research dispatch binding signature is malformed.", ex);
        }

        var payload = BuildSignedPayload(
            binding.ProtocolVersion,
            binding.OpaqueWorkItemId,
            binding.RemoteJobId,
            binding.RemoteJobName,
            binding.WorkItemEnvelopeSha256,
            binding.PublishedAt,
            binding.ExpiresAt);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(clientPublicKeyPem);
        if (!rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            throw new CryptographicException("Remote research dispatch binding signature is invalid.");

        return binding;
    }

    public static ProtectedResearchDispatchBinding VerifyEnvelopeBound(
        ProtectedResearchDispatchBinding binding,
        ProtectedResearchWorkItemEnvelope envelope,
        string clientPublicKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (!string.Equals(binding.ProtocolVersion, EnvelopeBoundProtocolVersion, StringComparison.Ordinal))
            throw new CryptographicException("Remote research worker requires an envelope-bound dispatch binding.");

        var verified = Verify(binding, envelope.OpaqueWorkItemId, clientPublicKeyPem, now);
        var actualCommitment = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope);
        if (!ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(
                verified.WorkItemEnvelopeSha256!,
                actualCommitment))
        {
            throw new CryptographicException(
                "Protected remote research work-item envelope does not match the client-signed dispatch commitment.");
        }

        return verified;
    }

    public static string GetDeterministicRemoteJobName(string opaqueWorkItemId)
    {
        ValidateOpaqueId(opaqueWorkItemId);
        return $"nvidea-research-{opaqueWorkItemId[..12].ToLowerInvariant()}";
    }

    private static ProtectedResearchDispatchBinding SignCore(
        string protocolVersion,
        string opaqueWorkItemId,
        string remoteJobId,
        string? workItemEnvelopeSha256,
        DateTimeOffset publishedAt,
        DateTimeOffset expiresAt,
        string clientPrivateKeyPem)
    {
        ValidateOpaqueId(opaqueWorkItemId);
        ValidateRemoteJobId(remoteJobId);
        ValidateLifetime(publishedAt, expiresAt);
        if (string.IsNullOrWhiteSpace(clientPrivateKeyPem))
            throw new ArgumentException("Client private key is required.", nameof(clientPrivateKeyPem));

        var remoteJobName = GetDeterministicRemoteJobName(opaqueWorkItemId);
        var payload = BuildSignedPayload(
            protocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            remoteJobName,
            workItemEnvelopeSha256,
            publishedAt,
            expiresAt);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(clientPrivateKeyPem);
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return new ProtectedResearchDispatchBinding(
            protocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            remoteJobName,
            publishedAt,
            expiresAt,
            Convert.ToBase64String(signature),
            workItemEnvelopeSha256);
    }

    private static byte[] BuildSignedPayload(
        string protocolVersion,
        string opaqueWorkItemId,
        string remoteJobId,
        string remoteJobName,
        string? workItemEnvelopeSha256,
        DateTimeOffset publishedAt,
        DateTimeOffset expiresAt)
    {
        var fields = string.Equals(protocolVersion, EnvelopeBoundProtocolVersion, StringComparison.Ordinal)
            ? new[]
            {
                protocolVersion,
                opaqueWorkItemId,
                remoteJobId,
                remoteJobName,
                workItemEnvelopeSha256!,
                publishedAt.ToUniversalTime().ToString("O"),
                expiresAt.ToUniversalTime().ToString("O")
            }
            : new[]
            {
                protocolVersion,
                opaqueWorkItemId,
                remoteJobId,
                remoteJobName,
                publishedAt.ToUniversalTime().ToString("O"),
                expiresAt.ToUniversalTime().ToString("O")
            };
        return Encoding.UTF8.GetBytes(string.Join('\n', fields));
    }

    private static void ValidateLifetime(DateTimeOffset publishedAt, DateTimeOffset expiresAt)
    {
        if (expiresAt <= publishedAt || expiresAt - publishedAt > MaxLifetime)
            throw new InvalidOperationException("Remote research dispatch binding lifetime must be positive and no more than 24 hours.");
    }

    private static void ValidateOpaqueId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length < 24
            || value.Length > 64
            || value.Any(static ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_')))
        {
            throw new InvalidOperationException("Remote research work-item id is invalid.");
        }
    }

    private static void ValidateRemoteJobId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl))
            throw new InvalidOperationException("Remote research job id is invalid.");
    }
}

/// <summary>
/// Client-side publication boundary. Re-publication is idempotent only when the existing binding is
/// validly signed by this client and points to the same authoritative resource id. New production
/// dispatches use the envelope-bound overload; the V1 overload remains for bounded legacy recovery.
/// </summary>
public sealed class ResearchDispatchBindingPublisher
{
    private readonly IProtectedResearchDispatchBindingTransport _transport;
    private readonly string _clientPrivateKeyPem;
    private readonly string _clientPublicKeyPem;

    public ResearchDispatchBindingPublisher(
        IProtectedResearchDispatchBindingTransport transport,
        string clientPrivateKeyPem)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _clientPrivateKeyPem = string.IsNullOrWhiteSpace(clientPrivateKeyPem)
            ? throw new ArgumentException("Client private key is required.", nameof(clientPrivateKeyPem))
            : clientPrivateKeyPem;

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_clientPrivateKeyPem);
        _clientPublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
    }

    public Task<ProtectedResearchDispatchBinding> PublishAsync(
        string opaqueWorkItemId,
        string remoteJobId,
        DateTimeOffset expiresAt,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default) =>
        PublishCoreAsync(
            opaqueWorkItemId,
            remoteJobId,
            expiresAt,
            workItemEnvelopeSha256: null,
            requireEnvelopeBound: false,
            now,
            cancellationToken);

    public Task<ProtectedResearchDispatchBinding> PublishEnvelopeBoundAsync(
        string opaqueWorkItemId,
        string remoteJobId,
        string workItemEnvelopeSha256,
        DateTimeOffset expiresAt,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        ResearchWorkItemEnvelopeCommitment.ValidateCanonicalSha256(
            workItemEnvelopeSha256,
            nameof(workItemEnvelopeSha256));
        return PublishCoreAsync(
            opaqueWorkItemId,
            remoteJobId,
            expiresAt,
            workItemEnvelopeSha256,
            requireEnvelopeBound: true,
            now,
            cancellationToken);
    }

    private async Task<ProtectedResearchDispatchBinding> PublishCoreAsync(
        string opaqueWorkItemId,
        string remoteJobId,
        DateTimeOffset expiresAt,
        string? workItemEnvelopeSha256,
        bool requireEnvelopeBound,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        var current = now ?? DateTimeOffset.UtcNow;
        if (expiresAt <= current)
            throw new InvalidOperationException("Cannot publish an already-expired remote research dispatch binding.");

        var existing = await _transport.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return VerifyIdempotent(existing, opaqueWorkItemId, remoteJobId, workItemEnvelopeSha256, requireEnvelopeBound, current);

        var boundedExpiry = expiresAt - current > ResearchDispatchBindingProtector.MaxLifetime
            ? current.Add(ResearchDispatchBindingProtector.MaxLifetime)
            : expiresAt;
        var binding = requireEnvelopeBound
            ? ResearchDispatchBindingProtector.SignEnvelopeBound(
                opaqueWorkItemId,
                remoteJobId,
                workItemEnvelopeSha256!,
                current,
                boundedExpiry,
                _clientPrivateKeyPem)
            : ResearchDispatchBindingProtector.Sign(
                opaqueWorkItemId,
                remoteJobId,
                current,
                boundedExpiry,
                _clientPrivateKeyPem);

        try
        {
            await _transport.PutAsync(binding, cancellationToken).ConfigureAwait(false);
            return binding;
        }
        catch (InvalidOperationException)
        {
            var raced = await _transport.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
            if (raced is null)
                throw;
            return VerifyIdempotent(raced, opaqueWorkItemId, remoteJobId, workItemEnvelopeSha256, requireEnvelopeBound, current);
        }
    }

    private ProtectedResearchDispatchBinding VerifyIdempotent(
        ProtectedResearchDispatchBinding binding,
        string opaqueWorkItemId,
        string remoteJobId,
        string? expectedCommitment,
        bool requireEnvelopeBound,
        DateTimeOffset now)
    {
        var verified = ResearchDispatchBindingProtector.Verify(binding, opaqueWorkItemId, _clientPublicKeyPem, now);
        if (!string.Equals(verified.RemoteJobId, remoteJobId, StringComparison.Ordinal))
            throw new CryptographicException("Existing remote research dispatch binding targets a different Nebius resource id.");
        if (requireEnvelopeBound)
        {
            if (!string.Equals(verified.ProtocolVersion, ResearchDispatchBindingProtector.EnvelopeBoundProtocolVersion, StringComparison.Ordinal)
                || verified.WorkItemEnvelopeSha256 is null
                || !ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(verified.WorkItemEnvelopeSha256, expectedCommitment!))
            {
                throw new CryptographicException("Existing remote research dispatch binding targets a different protected work-item envelope.");
            }
        }
        return verified;
    }
}

/// <summary>
/// Worker-side delayed-publication boundary. Missing bindings and mounted-volume I/O faults are
/// retried with bounded exponential backoff. The envelope-bound overload additionally verifies the
/// exact staged encrypted envelope against the client-signed SHA-256 commitment before returning.
/// </summary>
public sealed class ResearchDispatchBindingWaiter
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly IProtectedResearchDispatchBindingTransport _transport;
    private readonly string _clientPublicKeyPem;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _maxWait;

    public ResearchDispatchBindingWaiter(
        IProtectedResearchDispatchBindingTransport transport,
        string clientPublicKeyPem,
        TimeSpan? pollInterval = null,
        TimeSpan? maxWait = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _clientPublicKeyPem = string.IsNullOrWhiteSpace(clientPublicKeyPem)
            ? throw new ArgumentException("Client public key is required.", nameof(clientPublicKeyPem))
            : clientPublicKeyPem;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        _maxWait = maxWait ?? TimeSpan.FromMinutes(5);
        if (_pollInterval < TimeSpan.FromMilliseconds(100) || _pollInterval > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(pollInterval), "Binding poll interval must be between 100 ms and 30 seconds.");
        if (_maxWait < _pollInterval || _maxWait > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(maxWait), "Binding wait must cover at least one poll and be no more than 15 minutes.");
    }

    public Task<ProtectedResearchDispatchBinding> WaitAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default) =>
        WaitCoreAsync(opaqueWorkItemId, workItemExpiresAt: null, envelope: null, cancellationToken);

    public Task<ProtectedResearchDispatchBinding> WaitAsync(
        string opaqueWorkItemId,
        DateTimeOffset workItemExpiresAt,
        CancellationToken cancellationToken = default) =>
        WaitCoreAsync(opaqueWorkItemId, workItemExpiresAt, envelope: null, cancellationToken);

    public Task<ProtectedResearchDispatchBinding> WaitAsync(
        string opaqueWorkItemId,
        ProtectedResearchWorkItemEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (!string.Equals(opaqueWorkItemId, envelope.OpaqueWorkItemId, StringComparison.Ordinal))
            throw new CryptographicException("Staged protected work item does not match the requested opaque id.");
        return WaitCoreAsync(opaqueWorkItemId, envelope.ExpiresAt, envelope, cancellationToken);
    }

    private async Task<ProtectedResearchDispatchBinding> WaitCoreAsync(
        string opaqueWorkItemId,
        DateTimeOffset? workItemExpiresAt,
        ProtectedResearchWorkItemEnvelope? envelope,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deadline = startedAt.Add(_maxWait);
        if (workItemExpiresAt is { } absoluteExpiry && absoluteExpiry < deadline)
            deadline = absoluteExpiry;

        if (deadline <= startedAt)
            throw new TimeoutException("Protected remote research work item expired before its authoritative dispatch binding became available.");

        var retryDelay = _pollInterval;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTimeOffset.UtcNow >= deadline)
                throw CreateTimeout(workItemExpiresAt, deadline);

            ProtectedResearchDispatchBinding? binding;
            try
            {
                binding = await _transport.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow >= deadline)
                    throw CreateTimeout(workItemExpiresAt, deadline);
                retryDelay = await DelayBeforeRetryAsync(retryDelay, deadline, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var observedAt = DateTimeOffset.UtcNow;
            if (observedAt >= deadline)
                throw CreateTimeout(workItemExpiresAt, deadline);

            if (binding is not null)
            {
                var verified = envelope is null
                    ? ResearchDispatchBindingProtector.Verify(binding, opaqueWorkItemId, _clientPublicKeyPem, observedAt)
                    : ResearchDispatchBindingProtector.VerifyEnvelopeBound(binding, envelope, _clientPublicKeyPem, observedAt);

                if (workItemExpiresAt is { } itemExpiry && verified.ExpiresAt > itemExpiry)
                    throw new CryptographicException("Authoritative Nebius dispatch binding outlives the protected research work item.");
                return verified;
            }

            retryDelay = await DelayBeforeRetryAsync(retryDelay, deadline, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<TimeSpan> DelayBeforeRetryAsync(
        TimeSpan retryDelay,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now >= deadline)
            return retryDelay;

        var remaining = deadline - now;
        var delay = remaining < retryDelay ? remaining : retryDelay;
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        return NextBackoff(retryDelay);
    }

    private static TimeSpan NextBackoff(TimeSpan current)
    {
        if (current >= MaxBackoff)
            return MaxBackoff;

        var doubledTicks = current.Ticks > MaxBackoff.Ticks / 2
            ? MaxBackoff.Ticks
            : current.Ticks * 2;
        return TimeSpan.FromTicks(Math.Min(doubledTicks, MaxBackoff.Ticks));
    }

    private static TimeoutException CreateTimeout(DateTimeOffset? workItemExpiresAt, DateTimeOffset deadline)
    {
        var reason = workItemExpiresAt is { } expiry && expiry <= deadline
            ? "protected work-item expiry"
            : "worker wait budget";
        return new TimeoutException($"Authoritative Nebius dispatch binding was not published before the {reason} deadline.");
    }
}
