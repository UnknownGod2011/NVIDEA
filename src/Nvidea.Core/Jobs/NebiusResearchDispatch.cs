using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nvidea.Core.Jobs;

public sealed record ResearchCloudAuthorization(
    Guid LocalJobId,
    string CheckpointStep,
    bool Approved,
    string DisclosureVersion,
    DateTimeOffset GrantedAt);

public sealed record RemoteResearchWorkItem(
    Guid LocalJobId,
    string CheckpointStep,
    string? CheckpointPayload,
    bool ContainsPrivateOsData,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public sealed record ProtectedResearchWorkItemEnvelope(
    string ProtocolVersion,
    string OpaqueWorkItemId,
    string WrappedDataKey,
    string Nonce,
    string Ciphertext,
    string AuthenticationTag,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public sealed record NebiusResearchDispatchOptions(
    string WorkerImage,
    string WorkerPublicKeyPem,
    string ContainerCommand,
    string Platform,
    string Preset,
    string Timeout,
    string SubnetId,
    NebiusServerlessDiskSpec Disk,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    IReadOnlyDictionary<string, NebiusMysteryBoxSecretRef>? SecretEnvironmentVariables = null,
    IReadOnlyList<NebiusServerlessVolumeMount>? Volumes = null);

public sealed record NebiusResearchDispatchReceipt(
    Guid LocalJobId,
    string CheckpointStep,
    string OpaqueWorkItemId,
    string RemoteJobId,
    DateTimeOffset DispatchedAt);

public interface IProtectedResearchWorkItemTransport
{
    Task PutAsync(
        ProtectedResearchWorkItemEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task<ProtectedResearchWorkItemEnvelope?> GetAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Hybrid envelope protection for research checkpoints sent to a remote worker.
/// The payload is encrypted with a random AES-256-GCM key, and that data key is
/// wrapped to the worker's pinned RSA public key using OAEP-SHA256. Only a random
/// opaque work-item id and bounded lifecycle metadata remain outside the ciphertext.
/// </summary>
public static class ResearchWorkItemProtector
{
    public const string ProtocolVersion = "nvidea.research.remote.v1";
    public const string DisclosureVersion = "nvidea.research.cloud-disclosure.v1";
    public const int MaxPlaintextBytes = 2 * 1024 * 1024;
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromHours(24);

    private const int DataKeyBytes = 32;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int OpaqueIdBytes = 24;

    public static ProtectedResearchWorkItemEnvelope Protect(
        RemoteResearchWorkItem workItem,
        string workerPublicKeyPem)
    {
        ValidateWorkItem(workItem);
        if (string.IsNullOrWhiteSpace(workerPublicKeyPem))
            throw new ArgumentException("Worker public key is required.", nameof(workerPublicKeyPem));

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(
            workItem,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (plaintext.Length > MaxPlaintextBytes)
            throw new InvalidOperationException($"Remote research payload exceeds the {MaxPlaintextBytes}-byte limit.");

        var opaqueId = CreateOpaqueId();
        var dataKey = RandomNumberGenerator.GetBytes(DataKeyBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagBytes];
        var associatedData = BuildAssociatedData(opaqueId, workItem.CreatedAt, workItem.ExpiresAt);

        try
        {
            using (var aes = new AesGcm(dataKey, TagBytes))
                aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(workerPublicKeyPem);
            var wrappedKey = rsa.Encrypt(dataKey, RSAEncryptionPadding.OaepSHA256);

            return new ProtectedResearchWorkItemEnvelope(
                ProtocolVersion,
                opaqueId,
                Convert.ToBase64String(wrappedKey),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(tag),
                workItem.CreatedAt,
                workItem.ExpiresAt);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static RemoteResearchWorkItem Unprotect(
        ProtectedResearchWorkItemEnvelope envelope,
        string workerPrivateKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (!string.Equals(envelope.ProtocolVersion, ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Unsupported remote research protocol version.");
        if (string.IsNullOrWhiteSpace(workerPrivateKeyPem))
            throw new ArgumentException("Worker private key is required.", nameof(workerPrivateKeyPem));
        ValidateOpaqueId(envelope.OpaqueWorkItemId);

        var current = now ?? DateTimeOffset.UtcNow;
        if (envelope.ExpiresAt <= current)
            throw new InvalidOperationException("Remote research work item has expired.");
        if (envelope.ExpiresAt <= envelope.CreatedAt || envelope.ExpiresAt - envelope.CreatedAt > MaxLifetime)
            throw new InvalidOperationException("Remote research work-item lifetime is invalid.");

        var wrappedKey = DecodeBase64(envelope.WrappedDataKey, "wrapped data key");
        var nonce = DecodeBase64(envelope.Nonce, "nonce");
        var ciphertext = DecodeBase64(envelope.Ciphertext, "ciphertext");
        var tag = DecodeBase64(envelope.AuthenticationTag, "authentication tag");
        if (nonce.Length != NonceBytes || tag.Length != TagBytes || ciphertext.Length > MaxPlaintextBytes)
            throw new InvalidOperationException("Remote research envelope has invalid cryptographic dimensions.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(workerPrivateKeyPem);
        var dataKey = rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);
        if (dataKey.Length != DataKeyBytes)
        {
            CryptographicOperations.ZeroMemory(dataKey);
            throw new InvalidOperationException("Remote research envelope contains an invalid data key.");
        }

        var plaintext = new byte[ciphertext.Length];
        try
        {
            var associatedData = BuildAssociatedData(
                envelope.OpaqueWorkItemId,
                envelope.CreatedAt,
                envelope.ExpiresAt);
            using (var aes = new AesGcm(dataKey, TagBytes))
                aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

            var workItem = JsonSerializer.Deserialize<RemoteResearchWorkItem>(
                plaintext,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Remote research payload is empty.");

            ValidateWorkItem(workItem);
            if (workItem.CreatedAt != envelope.CreatedAt || workItem.ExpiresAt != envelope.ExpiresAt)
                throw new CryptographicException("Remote research lifecycle metadata does not match the protected payload.");
            return workItem;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static void ValidateAuthorization(
        ResearchCloudAuthorization authorization,
        RemoteResearchWorkItem workItem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(workItem);
        var current = now ?? DateTimeOffset.UtcNow;

        if (!authorization.Approved)
            throw new InvalidOperationException("Explicit cloud research approval is required.");
        if (!string.Equals(authorization.DisclosureVersion, DisclosureVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloud research disclosure version does not match the current protocol.");
        if (authorization.LocalJobId != workItem.LocalJobId
            || !string.Equals(authorization.CheckpointStep, workItem.CheckpointStep, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cloud research approval is not scoped to this exact job stage.");
        }
        if (authorization.GrantedAt > current + TimeSpan.FromMinutes(1)
            || authorization.GrantedAt < workItem.CreatedAt - TimeSpan.FromMinutes(5))
        {
            throw new InvalidOperationException("Cloud research approval timestamp is not valid for this work item.");
        }
        if (workItem.ContainsPrivateOsData)
            throw new InvalidOperationException("Research containing private OS-local data must remain on-device.");
    }

    private static void ValidateWorkItem(RemoteResearchWorkItem workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (workItem.LocalJobId == Guid.Empty)
            throw new ArgumentException("Local research job id is required.", nameof(workItem));
        if (string.IsNullOrWhiteSpace(workItem.CheckpointStep) || workItem.CheckpointStep.Length > 128)
            throw new ArgumentException("A bounded checkpoint step is required.", nameof(workItem));
        if (workItem.ExpiresAt <= workItem.CreatedAt || workItem.ExpiresAt - workItem.CreatedAt > MaxLifetime)
            throw new ArgumentException("Remote research work item must have a positive lifetime of no more than 24 hours.", nameof(workItem));
    }

    private static byte[] BuildAssociatedData(
        string opaqueWorkItemId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt) =>
        Encoding.UTF8.GetBytes(string.Join(
            '\n',
            ProtocolVersion,
            opaqueWorkItemId,
            createdAt.ToUniversalTime().ToString("O"),
            expiresAt.ToUniversalTime().ToString("O")));

    private static string CreateOpaqueId()
    {
        var bytes = RandomNumberGenerator.GetBytes(OpaqueIdBytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
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

    private static byte[] DecodeBase64(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Remote research envelope {fieldName} is missing.");
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Remote research envelope {fieldName} is malformed.", ex);
        }
    }
}

/// <summary>
/// Dispatches an already-staged research checkpoint to Nebius Serverless without placing the
/// checkpoint payload, question, or API secrets in job arguments/environment. A protected payload
/// is written to a dedicated transport first; the Serverless control plane receives only a random
/// opaque work-item id plus non-secret protocol metadata.
/// </summary>
public sealed class NebiusResearchDispatcher
{
    private readonly INebiusServerlessJobClient _serverless;
    private readonly IProtectedResearchWorkItemTransport _transport;
    private readonly NebiusResearchDispatchOptions _options;

    public NebiusResearchDispatcher(
        INebiusServerlessJobClient serverless,
        IProtectedResearchWorkItemTransport transport,
        NebiusResearchDispatchOptions options)
    {
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ValidateOptions(options);
    }

    public async Task<NebiusResearchDispatchReceipt> DispatchAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        CancellationToken cancellationToken = default)
    {
        ResearchWorkItemProtector.ValidateAuthorization(authorization, workItem);
        var envelope = ResearchWorkItemProtector.Protect(workItem, _options.WorkerPublicKeyPem);
        await _transport.PutAsync(envelope, cancellationToken).ConfigureAwait(false);

        try
        {
            var environment = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["NVIDEA_RESEARCH_PROTOCOL"] = ResearchWorkItemProtector.ProtocolVersion
            };
            if (_options.EnvironmentVariables is not null)
            {
                foreach (var pair in _options.EnvironmentVariables)
                    environment.Add(pair.Key, pair.Value);
            }

            var spec = new NebiusServerlessJobSpec(
                Name: $"nvidea-research-{envelope.OpaqueWorkItemId[..12].ToLowerInvariant()}",
                Image: _options.WorkerImage,
                ContainerCommand: _options.ContainerCommand,
                Arguments: $"Nvidea.Worker.dll research --work-item-id={envelope.OpaqueWorkItemId}",
                Platform: _options.Platform,
                Preset: _options.Preset,
                Timeout: _options.Timeout,
                SubnetId: _options.SubnetId,
                EnvironmentVariables: environment,
                Disk: _options.Disk,
                SecretEnvironmentVariables: _options.SecretEnvironmentVariables,
                Volumes: _options.Volumes);

            var response = await _serverless.CreateAsync(spec, cancellationToken).ConfigureAwait(false);
            var remoteJobId = response.TryGetResourceId();
            if (string.IsNullOrWhiteSpace(remoteJobId))
            {
                throw new InvalidOperationException(
                    "Nebius accepted the Serverless create request but did not expose a job resource id. " +
                    "The encrypted work item is intentionally retained until expiry because remote creation is ambiguous.");
            }

            return new NebiusResearchDispatchReceipt(
                workItem.LocalJobId,
                workItem.CheckpointStep,
                envelope.OpaqueWorkItemId,
                remoteJobId,
                DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("did not expose a job resource id", StringComparison.Ordinal))
        {
            throw;
        }
        catch
        {
            await TryDeleteAsync(envelope.OpaqueWorkItemId).ConfigureAwait(false);
            throw;
        }
    }

    private async Task TryDeleteAsync(string opaqueWorkItemId)
    {
        try
        {
            await _transport.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup only. The payload remains encrypted and has a bounded expiry.
        }
    }

    private static void ValidateOptions(NebiusResearchDispatchOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WorkerImage)
            || string.IsNullOrWhiteSpace(options.WorkerPublicKeyPem)
            || string.IsNullOrWhiteSpace(options.ContainerCommand)
            || string.IsNullOrWhiteSpace(options.Platform)
            || string.IsNullOrWhiteSpace(options.Preset)
            || string.IsNullOrWhiteSpace(options.Timeout)
            || string.IsNullOrWhiteSpace(options.SubnetId))
        {
            throw new ArgumentException("Worker image/key, command, platform, preset, timeout and subnet are required.", nameof(options));
        }
        if (options.Disk is null || string.IsNullOrWhiteSpace(options.Disk.Type) || options.Disk.SizeBytes <= 0)
            throw new ArgumentException("An explicit positive-size Serverless disk is required.", nameof(options));
        if (options.EnvironmentVariables?.ContainsKey("NVIDEA_RESEARCH_PROTOCOL") == true)
            throw new ArgumentException("NVIDEA_RESEARCH_PROTOCOL is reserved by the dispatcher.", nameof(options));
    }
}
