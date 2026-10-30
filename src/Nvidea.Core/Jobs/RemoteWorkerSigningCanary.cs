using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Public, bounded Object Storage artifacts for a worker signing canary. They deliberately carry
/// no PEM, provider token, SecretStash/MysteryBox reference, research payload, or raw job id.
/// The proof remains untrusted until it verifies against the outstanding challenge and pin.
/// </summary>
public sealed record WorkerSigningCanaryInvocation(
    WorkerSigningCanaryChallenge Challenge,
    string JobBindingSha256);

public interface IWorkerSigningCanaryArtifactTransport
{
    Task PublishInvocationAsync(
        WorkerSigningCanaryInvocation invocation,
        CancellationToken cancellationToken = default);

    Task<WorkerSigningCanaryProof?> GetProofAsync(
        string nonce,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// S3-compatible proof channel backed by the already configured Nebius Object Storage transport.
/// Keys are fixed protocol namespaces and nonce-derived only. The object client enforces
/// create-once writes and bounded reads.
/// </summary>
public sealed class S3WorkerSigningCanaryArtifactTransport : IWorkerSigningCanaryArtifactTransport
{
    private const int MaxArtifactBytes = 16 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IProtectedResearchObjectStoreClient _objects;

    public S3WorkerSigningCanaryArtifactTransport(IProtectedResearchObjectStoreClient objects) =>
        _objects = objects ?? throw new ArgumentNullException(nameof(objects));

    public Task PublishInvocationAsync(
        WorkerSigningCanaryInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ValidateInvocation(invocation);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(invocation, JsonOptions);
        if (bytes.Length is < 1 or > MaxArtifactBytes)
            throw new InvalidOperationException("Signing canary invocation exceeds the bounded artifact limit.");
        return _objects.PutIfAbsentAsync(InvocationKey(invocation.Challenge.Nonce), bytes, cancellationToken);
    }

    public async Task<WorkerSigningCanaryProof?> GetProofAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        ValidateNonce(nonce);
        var bytes = await _objects.GetAsync(ProofKey(nonce), MaxArtifactBytes, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<WorkerSigningCanaryProof>(bytes, JsonOptions)
                ?? throw new InvalidOperationException("Signing canary proof is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Signing canary proof is malformed.", ex);
        }
    }

    internal static string InvocationKey(string nonce) => $"signing-canary/invocations/{ValidateNonce(nonce)}.json";
    internal static string ProofKey(string nonce) => $"signing-canary/proofs/{ValidateNonce(nonce)}.json";

    internal static void ValidateInvocation(WorkerSigningCanaryInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        WorkerSigningCanary.ValidateSha256(invocation.JobBindingSha256, nameof(invocation.JobBindingSha256));
        // Decode applies protocol, nonce, deployment fingerprint, and bounded-lifetime validation.
        WorkerSigningCanary.DecodeChallenge(WorkerSigningCanary.EncodeChallenge(invocation.Challenge));
    }

    internal static string ValidateNonce(string nonce)
    {
        if (string.IsNullOrWhiteSpace(nonce)
            || nonce.Length != 43
            || nonce.Any(static character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
        {
            throw new InvalidOperationException("Signing canary nonce is invalid.");
        }

        return nonce;
    }
}

/// <summary>
/// Worker-side counterpart of <see cref="S3WorkerSigningCanaryArtifactTransport"/>. The mounted
/// storage namespace is public but untrusted, so a substituted invocation merely causes the
/// client-side challenge/job binding validation to fail closed.
/// </summary>
public static class WorkerSigningCanaryMountedArtifactTransport
{
    private const int MaxArtifactBytes = 16 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<WorkerSigningCanaryInvocation> WaitForInvocationAsync(
        string rootDirectory,
        WorkerSigningCanaryChallenge expectedChallenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedChallenge);
        var root = ValidateRoot(rootDirectory);
        var path = BuildPath(root, "invocations", expectedChallenge.Nonce);

        while (DateTimeOffset.UtcNow < expectedChallenge.ExpiresAt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                var invocation = await ReadAsync<WorkerSigningCanaryInvocation>(path, cancellationToken).ConfigureAwait(false);
                S3WorkerSigningCanaryArtifactTransport.ValidateInvocation(invocation);
                if (invocation.Challenge != expectedChallenge)
                    throw new CryptographicException("Signing canary invocation does not match the worker challenge.");
                return invocation;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Signing canary invocation was not visible before challenge expiry.");
    }

    public static async Task PublishProofAsync(
        string rootDirectory,
        WorkerSigningCanaryProof proof,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proof);
        var root = ValidateRoot(rootDirectory);
        S3WorkerSigningCanaryArtifactTransport.ValidateNonce(proof.Nonce);
        WorkerSigningCanary.ValidateSha256(proof.DeploymentFingerprintSha256, nameof(proof.DeploymentFingerprintSha256));
        WorkerSigningCanary.ValidateSha256(proof.JobBindingSha256, nameof(proof.JobBindingSha256));

        var target = BuildPath(root, "proofs", proof.Nonce);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(proof, JsonOptions);
        if (bytes.Length is < 1 or > MaxArtifactBytes)
            throw new InvalidOperationException("Signing canary proof exceeds the bounded artifact limit.");

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellationToken).ConfigureAwait(false);
            File.Move(temporary, target, overwrite: false);
        }
        catch (IOException) when (File.Exists(target))
        {
            throw new InvalidOperationException("A signing canary proof already exists for this challenge.");
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch
            {
                // Best-effort cleanup of an uncommitted artifact only.
            }
        }
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (info.Length is < 1 or > MaxArtifactBytes)
            throw new InvalidOperationException("Signing canary artifact has an invalid transport size.");

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Signing canary artifact is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Signing canary artifact is malformed.", ex);
        }
    }

    private static string ValidateRoot(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new InvalidOperationException("Signing canary transport root is missing.");
        var root = Path.GetFullPath(rootDirectory);
        if (!Path.IsPathFullyQualified(root))
            throw new InvalidOperationException("Signing canary transport root must be absolute.");
        return root;
    }

    private static string BuildPath(string root, string category, string nonce) =>
        Path.Combine(root, "signing-canary", category, S3WorkerSigningCanaryArtifactTransport.ValidateNonce(nonce) + ".json");
}

/// <summary>Safe, opaque identity of the exact worker deployment being challenged.</summary>
public sealed record WorkerSigningCanaryDeployment(
    string DeploymentFingerprintSha256,
    string WorkerImage,
    string VerificationKeySha256)
{
    public static WorkerSigningCanaryDeployment Create(
        NebiusResearchDispatchOptions options,
        string pinnedWorkerPublicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(options);
        NebiusResearchDeploymentPreflight.ValidateWorkerResultSigningSecretProvisioning(options);
        var secret = options.SecretEnvironmentVariables![NebiusResearchDeploymentPreflight.WorkerResultSigningPrivateKeyEnvironmentVariable];
        if (string.IsNullOrWhiteSpace(secret.VersionId))
            throw new InvalidOperationException("A version-pinned worker result-signing secret is required for the remote canary.");

        var canonicalPin = WorkerResultVerificationPublicKeyTrust.ValidateAndCanonicalize(pinnedWorkerPublicKeyPem);
        var pinFingerprint = WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(canonicalPin);
        var material = string.Join('\n',
            "nvidea.worker.signing-canary.deployment.v1",
            options.WorkerImage,
            secret.SecretId,
            secret.VersionId,
            pinFingerprint);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        return new WorkerSigningCanaryDeployment(fingerprint, options.WorkerImage, pinFingerprint);
    }
}

public enum WorkerSigningCanaryStatus
{
    NotConfigured,
    PublicKeyPinnedCanaryNotRun,
    Running,
    Failed,
    Verified,
    Stale
}

/// <summary>
/// A receipt contains the original public challenge and signed public proof, never a bool that a
/// caller can set. It is only projected as VERIFIED after the signature, job binding, freshness,
/// public pin, and deployment fingerprint all revalidate.
/// </summary>
public sealed record WorkerSigningCanaryReceipt(
    WorkerSigningCanaryChallenge Challenge,
    WorkerSigningCanaryProof Proof,
    DateTimeOffset VerifiedAt);

public sealed record WorkerSigningCanaryEvidenceSnapshot(
    WorkerSigningCanaryStatus Status,
    string VerificationKeySha256,
    string? DeploymentFingerprintSha256 = null,
    DateTimeOffset? VerifiedAt = null)
{
    public static WorkerSigningCanaryEvidenceSnapshot NotConfigured() =>
        new(WorkerSigningCanaryStatus.NotConfigured, string.Empty);
}

public static class WorkerSigningCanaryEvidenceEvaluator
{
    public static readonly TimeSpan MaximumEvidenceAge = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);

    public static WorkerSigningCanaryEvidenceSnapshot Evaluate(
        WorkerSigningCanaryReceipt? receipt,
        WorkerSigningCanaryDeployment deployment,
        string pinnedWorkerPublicKeyPem,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        var current = now ?? DateTimeOffset.UtcNow;
        var pin = WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(pinnedWorkerPublicKeyPem);
        if (!string.Equals(pin, deployment.VerificationKeySha256, StringComparison.Ordinal))
            throw new CryptographicException("Worker signing canary deployment does not match the active public pin.");

        if (receipt is null)
            return new WorkerSigningCanaryEvidenceSnapshot(WorkerSigningCanaryStatus.PublicKeyPinnedCanaryNotRun, pin);

        if (receipt.VerifiedAt > current.Add(AllowedClockSkew)
            || receipt.VerifiedAt < receipt.Challenge.IssuedAt
            || receipt.VerifiedAt > receipt.Challenge.ExpiresAt)
        {
            throw new CryptographicException("Worker signing canary receipt has an invalid verification timestamp.");
        }

        var verification = WorkerSigningCanary.Verify(
            receipt.Challenge,
            receipt.Proof,
            pinnedWorkerPublicKeyPem,
            receipt.Proof.JobBindingSha256,
            receipt.VerifiedAt);

        if (!string.Equals(verification.VerificationKeySha256, pin, StringComparison.Ordinal)
            || !string.Equals(verification.DeploymentFingerprintSha256, deployment.DeploymentFingerprintSha256, StringComparison.Ordinal))
        {
            return new WorkerSigningCanaryEvidenceSnapshot(WorkerSigningCanaryStatus.Stale, pin);
        }

        if (current - receipt.VerifiedAt > MaximumEvidenceAge)
            return new WorkerSigningCanaryEvidenceSnapshot(
                WorkerSigningCanaryStatus.Stale,
                pin,
                deployment.DeploymentFingerprintSha256,
                receipt.VerifiedAt);

        return new WorkerSigningCanaryEvidenceSnapshot(
            WorkerSigningCanaryStatus.Verified,
            pin,
            deployment.DeploymentFingerprintSha256,
            receipt.VerifiedAt);
    }
}

/// <summary>
/// DPAPI-protected local storage for a public proof receipt. Plain JSON is rejected, and reading
/// the receipt alone never grants trust; <see cref="WorkerSigningCanaryEvidenceEvaluator"/>
/// independently verifies the signature and current deployment before every projection.
/// </summary>
public sealed class WorkerSigningCanaryEvidenceStore
{
    private const string Purpose = "worker-signing-canary-receipt-v1";
    private readonly string _path;
    private readonly ILocalStateProtector _protector;

    public WorkerSigningCanaryEvidenceStore(string path, ILocalStateProtector protector)
    {
        _path = AtomicTextArtifactWriter.ValidateDestination(path, "Worker signing canary evidence");
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    public async Task<WorkerSigningCanaryReceipt?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_path))
            return null;

        var persisted = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
        if (!LocalStateEnvelope.HasProtectedHeader(persisted))
            throw new InvalidDataException("Worker signing canary evidence must be locally protected.");
        var payload = LocalStateEnvelope.Decode(persisted, _protector, Purpose);
        if (!payload.WasProtected)
            throw new InvalidDataException("Worker signing canary evidence must be locally protected.");

        try
        {
            return JsonSerializer.Deserialize<WorkerSigningCanaryReceipt>(payload.Plaintext, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("Worker signing canary evidence is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Worker signing canary evidence is malformed.", ex);
        }
    }

    public Task SaveAsync(WorkerSigningCanaryReceipt receipt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        cancellationToken.ThrowIfCancellationRequested();
        var plain = JsonSerializer.SerializeToUtf8Bytes(receipt, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var protectedBytes = LocalStateEnvelope.Encode(plain, _protector, Purpose);
        AtomicTextArtifactWriter.Write(_path, Encoding.UTF8.GetString(protectedBytes), "Worker signing canary evidence");
        CryptographicOperations.ZeroMemory(plain);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Explicit, operator-invoked Nebius Serverless signing canary. It creates a job with the exact
/// production image, volume, and version-pinned result-signing secret; waits for a terminal job;
/// then verifies the bounded Object Storage proof against the one-use outstanding challenge.
/// </summary>
public sealed class RemoteWorkerSigningCanary
{
    private readonly INebiusServerlessJobClient _serverless;
    private readonly IWorkerSigningCanaryArtifactTransport _artifacts;
    private readonly NebiusResearchDispatchOptions _options;
    private readonly string _pinnedWorkerPublicKeyPem;
    private readonly WorkerSigningCanaryDeployment _deployment;
    private readonly WorkerSigningCanaryEvidenceStore _evidenceStore;
    private readonly TimeSpan _pollInterval;
    private int _running;
    private int _lastRunFailed;

    public RemoteWorkerSigningCanary(
        INebiusServerlessJobClient serverless,
        IWorkerSigningCanaryArtifactTransport artifacts,
        NebiusResearchDispatchOptions options,
        string pinnedWorkerPublicKeyPem,
        WorkerSigningCanaryEvidenceStore evidenceStore,
        TimeSpan? pollInterval = null)
    {
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _pinnedWorkerPublicKeyPem = WorkerResultVerificationPublicKeyTrust.ValidateAndCanonicalize(pinnedWorkerPublicKeyPem);
        _deployment = WorkerSigningCanaryDeployment.Create(options, _pinnedWorkerPublicKeyPem);
        _evidenceStore = evidenceStore ?? throw new ArgumentNullException(nameof(evidenceStore));
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        if (_pollInterval < TimeSpan.FromMilliseconds(250) || _pollInterval > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
    }

    public WorkerSigningCanaryDeployment Deployment => _deployment;

    public async Task<WorkerSigningCanaryEvidenceSnapshot> GetEvidenceAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _running) != 0)
        {
            return new WorkerSigningCanaryEvidenceSnapshot(
                WorkerSigningCanaryStatus.Running,
                _deployment.VerificationKeySha256,
                _deployment.DeploymentFingerprintSha256);
        }

        try
        {
            var receipt = await _evidenceStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var evaluated = WorkerSigningCanaryEvidenceEvaluator.Evaluate(receipt, _deployment, _pinnedWorkerPublicKeyPem);
            return Volatile.Read(ref _lastRunFailed) != 0 && evaluated.Status != WorkerSigningCanaryStatus.Verified
                ? evaluated with { Status = WorkerSigningCanaryStatus.Failed }
                : evaluated;
        }
        catch
        {
            return new WorkerSigningCanaryEvidenceSnapshot(
                WorkerSigningCanaryStatus.Failed,
                _deployment.VerificationKeySha256,
                _deployment.DeploymentFingerprintSha256);
        }
    }

    public async Task<WorkerSigningCanaryEvidenceSnapshot> RunAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("A worker signing canary is already running.");

        string? remoteJobId = null;
        var terminal = false;
        try
        {
            var challenge = WorkerSigningCanary.Create(_deployment.DeploymentFingerprintSha256);
            var encodedChallenge = WorkerSigningCanary.EncodeChallenge(challenge);
            var jobName = $"nvidea-signing-canary-{challenge.Nonce[..12].ToLowerInvariant()}";
            var response = await _serverless.CreateAsync(BuildJobSpec(jobName, encodedChallenge, challenge.Nonce), cancellationToken).ConfigureAwait(false);
            remoteJobId = response.TryGetResourceId();
            if (string.IsNullOrWhiteSpace(remoteJobId))
                throw new InvalidOperationException("Nebius accepted the signing-canary request but did not expose a job resource id.");

            var jobBindingSha256 = HashRemoteJobId(remoteJobId);
            await _artifacts.PublishInvocationAsync(
                new WorkerSigningCanaryInvocation(challenge, jobBindingSha256),
                cancellationToken).ConfigureAwait(false);

            var proof = await WaitForCompletedProofAsync(remoteJobId, jobName, challenge, cancellationToken).ConfigureAwait(false);
            terminal = true;
            var verifier = new WorkerSigningCanaryVerifier(challenge, jobBindingSha256);
            var verification = verifier.VerifyAndConsume(proof, _pinnedWorkerPublicKeyPem);
            if (!string.Equals(verification.VerificationKeySha256, _deployment.VerificationKeySha256, StringComparison.Ordinal)
                || !string.Equals(verification.DeploymentFingerprintSha256, _deployment.DeploymentFingerprintSha256, StringComparison.Ordinal))
            {
                throw new CryptographicException("Signing canary proof does not match the active deployment identity.");
            }

            var receipt = new WorkerSigningCanaryReceipt(challenge, proof, verification.VerifiedAt);
            await _evidenceStore.SaveAsync(
                receipt,
                cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _lastRunFailed, 0);
            return WorkerSigningCanaryEvidenceEvaluator.Evaluate(receipt, _deployment, _pinnedWorkerPublicKeyPem);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!terminal)
                await TryCancelAsync(remoteJobId).ConfigureAwait(false);
            Volatile.Write(ref _lastRunFailed, 1);
            throw;
        }
        catch
        {
            if (!terminal)
                await TryCancelAsync(remoteJobId).ConfigureAwait(false);
            Volatile.Write(ref _lastRunFailed, 1);
            throw new InvalidOperationException("Remote worker signing canary did not verify. No deployed-worker trust was established.");
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private NebiusServerlessJobSpec BuildJobSpec(string name, string encodedChallenge, string nonce)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["NVIDEA_SIGNING_CANARY_PROTOCOL"] = WorkerSigningCanary.Protocol
        };
        foreach (var pair in _options.EnvironmentVariables ?? new Dictionary<string, string>())
            environment.Add(pair.Key, pair.Value);

        return new NebiusServerlessJobSpec(
            Name: name,
            Image: _options.WorkerImage,
            ContainerCommand: _options.ContainerCommand,
            Arguments: $"Nvidea.Worker.dll signing-canary --challenge={encodedChallenge} --input-id={nonce}",
            Platform: _options.Platform,
            Preset: _options.Preset,
            Timeout: _options.Timeout,
            SubnetId: _options.SubnetId,
            EnvironmentVariables: environment,
            Disk: _options.Disk,
            SecretEnvironmentVariables: _options.SecretEnvironmentVariables,
            Volumes: _options.Volumes);
    }

    private async Task<WorkerSigningCanaryProof> WaitForCompletedProofAsync(
        string remoteJobId,
        string expectedName,
        WorkerSigningCanaryChallenge challenge,
        CancellationToken cancellationToken)
    {
        var completed = false;
        while (DateTimeOffset.UtcNow < challenge.ExpiresAt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = NebiusServerlessJobSnapshotParser.ParseGet(
                await _serverless.GetAsync(remoteJobId, cancellationToken).ConfigureAwait(false));
            if (!string.Equals(snapshot.Id, remoteJobId, StringComparison.Ordinal)
                || !string.Equals(snapshot.Name, expectedName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Nebius signing-canary job verification returned substituted job identity.");
            }

            switch (snapshot.State)
            {
                case NebiusRemoteJobState.Completed:
                    completed = true;
                    var proof = await _artifacts.GetProofAsync(challenge.Nonce, cancellationToken).ConfigureAwait(false);
                    if (proof is not null)
                        return proof;
                    break;
                case NebiusRemoteJobState.Failed:
                case NebiusRemoteJobState.Cancelled:
                    throw new InvalidOperationException("Nebius signing-canary job reached a non-success terminal state.");
                case NebiusRemoteJobState.Unknown:
                    throw new InvalidOperationException("Nebius signing-canary job has an unknown lifecycle state.");
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(completed
            ? "Nebius signing-canary job completed but no proof artifact became visible before challenge expiry."
            : "Nebius signing-canary job did not complete before challenge expiry.");
    }

    private async Task TryCancelAsync(string? remoteJobId)
    {
        if (string.IsNullOrWhiteSpace(remoteJobId))
            return;
        try
        {
            await _serverless.CancelAsync(remoteJobId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The unverified result remains untrusted. Cancellation reconciliation is provider-side.
        }
    }

    private static string HashRemoteJobId(string remoteJobId)
    {
        if (string.IsNullOrWhiteSpace(remoteJobId) || remoteJobId.Length > 256 || remoteJobId.Any(char.IsControl))
            throw new InvalidOperationException("Nebius signing-canary job id is invalid.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("nvidea.worker.signing-canary.job.v1\n" + remoteJobId)));
    }
}
