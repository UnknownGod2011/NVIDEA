using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

public sealed record RemoteResearchStageResult(
    Guid LocalJobId,
    string InputCheckpointStep,
    string OpaqueWorkItemId,
    string RemoteJobId,
    JobStepResult StepResult,
    DateTimeOffset CompletedAt,
    DateTimeOffset ExpiresAt);

public sealed record ProtectedResearchResultEnvelope(
    string ProtocolVersion,
    string OpaqueWorkItemId,
    string RemoteJobId,
    string WrappedDataKey,
    string Nonce,
    string Ciphertext,
    string AuthenticationTag,
    DateTimeOffset CompletedAt,
    DateTimeOffset ExpiresAt);

public interface IProtectedResearchResultTransport
{
    Task PutAsync(
        ProtectedResearchResultEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task<ProtectedResearchResultEnvelope?> GetAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Protects exactly one remote research stage result for the originating client.
/// Result contents, including the next checkpoint payload, are encrypted with a random
/// AES-256-GCM key; the data key is wrapped to the client's pinned RSA public key with
/// OAEP-SHA256. Opaque work-item id, remote-job id and bounded lifecycle data are authenticated
/// associated data so result provenance cannot be swapped without detection.
/// </summary>
public static class ResearchResultProtector
{
    public const string ProtocolVersion = "nvidea.research.remote-result.v1";
    public const int MaxPlaintextBytes = 2 * 1024 * 1024;
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromHours(24);

    private const int DataKeyBytes = 32;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;

    public static ProtectedResearchResultEnvelope Protect(
        RemoteResearchStageResult result,
        string clientPublicKeyPem)
    {
        ValidateResult(result);
        if (string.IsNullOrWhiteSpace(clientPublicKeyPem))
            throw new ArgumentException("Client public key is required.", nameof(clientPublicKeyPem));

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(
            result,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (plaintext.Length > MaxPlaintextBytes)
            throw new InvalidOperationException($"Remote research result exceeds the {MaxPlaintextBytes}-byte limit.");

        var dataKey = RandomNumberGenerator.GetBytes(DataKeyBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagBytes];
        var associatedData = BuildAssociatedData(
            result.OpaqueWorkItemId,
            result.RemoteJobId,
            result.CompletedAt,
            result.ExpiresAt);

        try
        {
            using (var aes = new AesGcm(dataKey, TagBytes))
                aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(clientPublicKeyPem);
            var wrappedKey = rsa.Encrypt(dataKey, RSAEncryptionPadding.OaepSHA256);

            return new ProtectedResearchResultEnvelope(
                ProtocolVersion,
                result.OpaqueWorkItemId,
                result.RemoteJobId,
                Convert.ToBase64String(wrappedKey),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(tag),
                result.CompletedAt,
                result.ExpiresAt);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static RemoteResearchStageResult Unprotect(
        ProtectedResearchResultEnvelope envelope,
        string clientPrivateKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (!string.Equals(envelope.ProtocolVersion, ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Unsupported remote research result protocol version.");
        if (string.IsNullOrWhiteSpace(clientPrivateKeyPem))
            throw new ArgumentException("Client private key is required.", nameof(clientPrivateKeyPem));

        ValidateOpaqueId(envelope.OpaqueWorkItemId);
        ValidateRemoteJobId(envelope.RemoteJobId);
        var current = now ?? DateTimeOffset.UtcNow;
        if (envelope.ExpiresAt <= current)
            throw new InvalidOperationException("Remote research result has expired.");
        if (envelope.ExpiresAt <= envelope.CompletedAt || envelope.ExpiresAt - envelope.CompletedAt > MaxLifetime)
            throw new InvalidOperationException("Remote research result lifetime is invalid.");

        var wrappedKey = DecodeBase64(envelope.WrappedDataKey, "wrapped data key");
        var nonce = DecodeBase64(envelope.Nonce, "nonce");
        var ciphertext = DecodeBase64(envelope.Ciphertext, "ciphertext");
        var tag = DecodeBase64(envelope.AuthenticationTag, "authentication tag");
        if (nonce.Length != NonceBytes || tag.Length != TagBytes || ciphertext.Length > MaxPlaintextBytes)
            throw new InvalidOperationException("Remote research result has invalid cryptographic dimensions.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(clientPrivateKeyPem);
        var dataKey = rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);
        if (dataKey.Length != DataKeyBytes)
        {
            CryptographicOperations.ZeroMemory(dataKey);
            throw new InvalidOperationException("Remote research result contains an invalid data key.");
        }

        var plaintext = new byte[ciphertext.Length];
        try
        {
            using (var aes = new AesGcm(dataKey, TagBytes))
            {
                aes.Decrypt(
                    nonce,
                    ciphertext,
                    tag,
                    plaintext,
                    BuildAssociatedData(
                        envelope.OpaqueWorkItemId,
                        envelope.RemoteJobId,
                        envelope.CompletedAt,
                        envelope.ExpiresAt));
            }

            var result = JsonSerializer.Deserialize<RemoteResearchStageResult>(
                plaintext,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Remote research result payload is empty.");

            ValidateResult(result);
            if (!string.Equals(result.OpaqueWorkItemId, envelope.OpaqueWorkItemId, StringComparison.Ordinal)
                || !string.Equals(result.RemoteJobId, envelope.RemoteJobId, StringComparison.Ordinal)
                || result.CompletedAt != envelope.CompletedAt
                || result.ExpiresAt != envelope.ExpiresAt)
            {
                throw new CryptographicException("Remote research result provenance does not match the protected payload.");
            }

            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static void ValidateResult(RemoteResearchStageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.LocalJobId == Guid.Empty)
            throw new ArgumentException("Local research job id is required.", nameof(result));
        if (string.IsNullOrWhiteSpace(result.InputCheckpointStep) || result.InputCheckpointStep.Length > 128)
            throw new ArgumentException("A bounded input checkpoint step is required.", nameof(result));
        ValidateOpaqueId(result.OpaqueWorkItemId);
        ValidateRemoteJobId(result.RemoteJobId);
        ArgumentNullException.ThrowIfNull(result.StepResult);
        if (result.ExpiresAt <= result.CompletedAt || result.ExpiresAt - result.CompletedAt > MaxLifetime)
            throw new ArgumentException("Remote research result must have a positive lifetime of no more than 24 hours.", nameof(result));
    }

    private static byte[] BuildAssociatedData(
        string opaqueWorkItemId,
        string remoteJobId,
        DateTimeOffset completedAt,
        DateTimeOffset expiresAt) =>
        Encoding.UTF8.GetBytes(string.Join(
            '\n',
            ProtocolVersion,
            opaqueWorkItemId,
            remoteJobId,
            completedAt.ToUniversalTime().ToString("O"),
            expiresAt.ToUniversalTime().ToString("O")));

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

    private static byte[] DecodeBase64(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Remote research result {fieldName} is missing.");
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Remote research result {fieldName} is malformed.", ex);
        }
    }
}

/// <summary>
/// Minimal worker-side execution primitive. It accepts only an opaque work-item id plus serverless
/// job provenance, retrieves/decrypts one checkpoint, executes exactly one ResearchJobHandler stage,
/// encrypts the resulting checkpoint to the originating client's public key, and publishes it.
/// The worker does not persist arbitrary local state and does not execute approval-bearing actions.
/// </summary>
public sealed class NebiusResearchWorker
{
    private readonly IProtectedResearchWorkItemTransport _workItems;
    private readonly IProtectedResearchResultTransport _results;
    private readonly ResearchJobHandler _handler;
    private readonly string _workerPrivateKeyPem;
    private readonly string _clientPublicKeyPem;

    public NebiusResearchWorker(
        IProtectedResearchWorkItemTransport workItems,
        IProtectedResearchResultTransport results,
        ResearchJobHandler handler,
        string workerPrivateKeyPem,
        string clientPublicKeyPem)
    {
        _workItems = workItems ?? throw new ArgumentNullException(nameof(workItems));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _workerPrivateKeyPem = string.IsNullOrWhiteSpace(workerPrivateKeyPem)
            ? throw new ArgumentException("Worker private key is required.", nameof(workerPrivateKeyPem))
            : workerPrivateKeyPem;
        _clientPublicKeyPem = string.IsNullOrWhiteSpace(clientPublicKeyPem)
            ? throw new ArgumentException("Client public key is required.", nameof(clientPublicKeyPem))
            : clientPublicKeyPem;
    }

    public async Task<ProtectedResearchResultEnvelope> ExecuteOneStageAsync(
        string opaqueWorkItemId,
        string remoteJobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueWorkItemId))
            throw new ArgumentException("Opaque work-item id is required.", nameof(opaqueWorkItemId));
        if (string.IsNullOrWhiteSpace(remoteJobId))
            throw new ArgumentException("Remote job id is required.", nameof(remoteJobId));

        var envelope = await _workItems.GetAsync(opaqueWorkItemId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remote research work item was not found.");
        if (!string.Equals(envelope.OpaqueWorkItemId, opaqueWorkItemId, StringComparison.Ordinal))
            throw new CryptographicException("Retrieved work item does not match the requested opaque id.");

        var workItem = ResearchWorkItemProtector.Unprotect(envelope, _workerPrivateKeyPem);
        if (workItem.ContainsPrivateOsData)
            throw new InvalidOperationException("Private OS-local research must not execute remotely.");

        var checkpoint = new AgentJobCheckpoint(
            workItem.CheckpointStep,
            workItem.CheckpointPayload,
            workItem.CreatedAt);
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true);
        var record = new AgentJobRecord(
            workItem.LocalJobId,
            definition,
            AgentJobState.Running,
            JobExecutionLocation.NebiusServerless,
            Attempt: 1,
            checkpoint,
            ApprovalScope: null,
            LastError: null,
            workItem.CreatedAt,
            DateTimeOffset.UtcNow);

        var stepResult = await _handler.ExecuteStepAsync(record, cancellationToken).ConfigureAwait(false);
        if (stepResult.RequiresApproval || !string.IsNullOrWhiteSpace(stepResult.ApprovalScope))
            throw new InvalidOperationException("Remote research worker cannot publish an approval-bearing stage result.");
        if (string.IsNullOrWhiteSpace(stepResult.CheckpointStep))
            throw new InvalidOperationException("Remote research stage did not produce a checkpoint step.");

        var completedAt = DateTimeOffset.UtcNow;
        var result = new RemoteResearchStageResult(
            workItem.LocalJobId,
            workItem.CheckpointStep,
            opaqueWorkItemId,
            remoteJobId,
            stepResult,
            completedAt,
            completedAt.Add(ResearchResultProtector.MaxLifetime));
        var protectedResult = ResearchResultProtector.Protect(result, _clientPublicKeyPem);
        await _results.PutAsync(protectedResult, cancellationToken).ConfigureAwait(false);
        return protectedResult;
    }
}
