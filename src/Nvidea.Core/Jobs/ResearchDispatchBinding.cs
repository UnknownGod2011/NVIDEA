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
    string Signature);

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
/// Nebius resource id returned after Serverless job creation. The binding contains no research
/// payload. It is RSA-PSS/SHA-256 signed by the originating client and verified by the worker using
/// the already-pinned client public key, preventing a writable shared transport from substituting
/// another remote resource id without detection.
/// </summary>
public static class ResearchDispatchBindingProtector
{
    public const string ProtocolVersion = "nvidea.research.dispatch-binding.v1";
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromHours(24);

    public static ProtectedResearchDispatchBinding Sign(
        string opaqueWorkItemId,
        string remoteJobId,
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
        var payload = BuildSignedPayload(opaqueWorkItemId, remoteJobId, remoteJobName, publishedAt, expiresAt);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(clientPrivateKeyPem);
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return new ProtectedResearchDispatchBinding(
            ProtocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            remoteJobName,
            publishedAt,
            expiresAt,
            Convert.ToBase64String(signature));
    }

    public static ProtectedResearchDispatchBinding Verify(
        ProtectedResearchDispatchBinding binding,
        string expectedOpaqueWorkItemId,
        string clientPublicKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ValidateOpaqueId(expectedOpaqueWorkItemId);
        if (!string.Equals(binding.ProtocolVersion, ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Unsupported remote research dispatch-binding protocol version.");
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

        var payload = BuildSignedPayload(binding.OpaqueWorkItemId, binding.RemoteJobId, binding.RemoteJobName, binding.PublishedAt, binding.ExpiresAt);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(clientPublicKeyPem);
        if (!rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            throw new CryptographicException("Remote research dispatch binding signature is invalid.");

        return binding;
    }

    public static string GetDeterministicRemoteJobName(string opaqueWorkItemId)
    {
        ValidateOpaqueId(opaqueWorkItemId);
        return $"nvidea-research-{opaqueWorkItemId[..12].ToLowerInvariant()}";
    }

    private static byte[] BuildSignedPayload(
        string opaqueWorkItemId,
        string remoteJobId,
        string remoteJobName,
        DateTimeOffset publishedAt,
        DateTimeOffset expiresAt) =>
        Encoding.UTF8.GetBytes(string.Join(
            '\n',
            ProtocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            remoteJobName,
            publishedAt.ToUniversalTime().ToString("O"),
            expiresAt.ToUniversalTime().ToString("O")));

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
/// validly signed by this client and points to the same authoritative resource id. A conflicting
/// create-once object fails closed rather than being overwritten.
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

    public async Task<ProtectedResearchDispatchBinding> PublishAsync(
        string opaqueWorkItemId,
        string remoteJobId,
        DateTimeOffset expiresAt,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var current = now ?? DateTimeOffset.UtcNow;
        if (expiresAt <= current)
            throw new InvalidOperationException("Cannot publish an already-expired remote research dispatch binding.");

        var existing = await _transport.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return VerifyIdempotent(existing, opaqueWorkItemId, remoteJobId, current);

        var boundedExpiry = expiresAt - current > ResearchDispatchBindingProtector.MaxLifetime
            ? current.Add(ResearchDispatchBindingProtector.MaxLifetime)
            : expiresAt;
        var binding = ResearchDispatchBindingProtector.Sign(
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
            // Another local owner/recovery path may have won the create-once race. Accept that race
            // only if the published object authenticates and names the same authoritative resource.
            var raced = await _transport.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
            if (raced is null)
                throw;
            return VerifyIdempotent(raced, opaqueWorkItemId, remoteJobId, current);
        }
    }

    private ProtectedResearchDispatchBinding VerifyIdempotent(
        ProtectedResearchDispatchBinding binding,
        string opaqueWorkItemId,
        string remoteJobId,
        DateTimeOffset now)
    {
        var verified = ResearchDispatchBindingProtector.Verify(binding, opaqueWorkItemId, _clientPublicKeyPem, now);
        if (!string.Equals(verified.RemoteJobId, remoteJobId, StringComparison.Ordinal))
            throw new CryptographicException("Existing remote research dispatch binding targets a different Nebius resource id.");
        return verified;
    }
}

public sealed class ResearchDispatchBindingWaiter
{
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

    public async Task<ProtectedResearchDispatchBinding> WaitAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + _maxWait;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = await _transport.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false);
            if (binding is not null)
                return ResearchDispatchBindingProtector.Verify(binding, opaqueWorkItemId, _clientPublicKeyPem);

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                throw new TimeoutException("Authoritative Nebius dispatch binding was not published before the worker wait deadline.");

            await Task.Delay(remaining < _pollInterval ? remaining : _pollInterval, cancellationToken).ConfigureAwait(false);
        }
    }
}
