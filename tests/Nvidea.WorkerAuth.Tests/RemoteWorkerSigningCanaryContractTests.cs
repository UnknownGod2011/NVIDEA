using System.Security.Cryptography;
using System.Net;
using System.Text.Json;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.WorkerAuth.Tests;

public sealed class RemoteWorkerSigningCanaryContractTests
{
    private static readonly string Deployment = new('A', 64);
    private static readonly string JobBinding = new('B', 64);

    [Fact]
    public void Fresh_challenge_binds_worker_pin_deployment_and_job_and_is_single_use()
    {
        using var worker = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var challenge = WorkerSigningCanary.Create(Deployment, now);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), JobBinding, now);
        var verifier = new WorkerSigningCanaryVerifier(challenge, JobBinding);

        var verified = verifier.VerifyAndConsume(proof, worker.ExportSubjectPublicKeyInfoPem(), now);

        Assert.Equal(Deployment, verified.DeploymentFingerprintSha256);
        Assert.Equal(JobBinding, verified.JobBindingSha256);
        Assert.Throws<CryptographicException>(() => verifier.VerifyAndConsume(proof, worker.ExportSubjectPublicKeyInfoPem(), now));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Verify(challenge, proof with { JobBindingSha256 = new string('C', 64) }, worker.ExportSubjectPublicKeyInfoPem(), JobBinding, now));
    }

    [Fact]
    public void Deployment_requires_version_pinned_secret_and_keeps_private_pem_out_of_plain_environment()
    {
        using var worker = RSA.Create(2048);
        var options = new NebiusResearchDispatchOptions(
            "registry.example/worker@sha256:" + new string('a', 64), worker.ExportSubjectPublicKeyInfoPem(), "dotnet", "cpu-d3", "1vcpu-4gb", "600s", "subnet", new NebiusServerlessDiskSpec("NETWORK_SSD", 10L * 1024 * 1024 * 1024),
            EnvironmentVariables: new Dictionary<string, string> { ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea" },
            SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>
            {
                [WorkerResultSigningPrivateKeyTrust.EnvironmentVariable] = new("mbsec-worker-result-signing", "mbsecver-worker-result-signing-v1")
            },
            Volumes: new[] { new NebiusServerlessVolumeMount("bucket", "/mnt/nvidea") });

        var deployment = WorkerSigningCanaryDeployment.Create(options, worker.ExportSubjectPublicKeyInfoPem());

        Assert.Equal(64, deployment.DeploymentFingerprintSha256.Length);
        Assert.DoesNotContain(WorkerResultSigningPrivateKeyTrust.EnvironmentVariable, options.EnvironmentVariables!.Keys);
        Assert.Throws<InvalidOperationException>(() => WorkerSigningCanaryDeployment.Create(options with
        {
            SecretEnvironmentVariables = new Dictionary<string, NebiusMysteryBoxSecretRef>()
        }, worker.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public async Task Remote_canary_uses_production_image_and_accepts_only_the_returned_job_bound_proof()
    {
        using var worker = RSA.Create(2048);
        var root = Path.Combine(Path.GetTempPath(), "nvidea-canary-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = CreateOptions(worker.ExportSubjectPublicKeyInfoPem());
            var jobs = new CompletedJobClient();
            var artifacts = new SigningArtifacts(worker.ExportPkcs8PrivateKeyPem());
            var evidence = new WorkerSigningCanaryEvidenceStore(Path.Combine(root, "receipt"), new PassThroughProtector());
            var canary = new RemoteWorkerSigningCanary(jobs, artifacts, options, worker.ExportSubjectPublicKeyInfoPem(), evidence, TimeSpan.FromMilliseconds(250));

            var snapshot = await canary.RunAsync();

            Assert.Equal(WorkerSigningCanaryStatus.Verified, snapshot.Status);
            Assert.Equal(options.WorkerImage, jobs.CreatedSpec!.Image);
            Assert.Equal("mbsecver-worker-result-signing-v1", jobs.CreatedSpec.SecretEnvironmentVariables![WorkerResultSigningPrivateKeyTrust.EnvironmentVariable].VersionId);
            Assert.Contains("signing-canary", jobs.CreatedSpec.Arguments, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusResearchDispatchOptions CreateOptions(string publicKeyPem) => new(
        "registry.example/worker@sha256:" + new string('a', 64), publicKeyPem, "dotnet", "cpu-d3", "1vcpu-4gb", "600s", "subnet", new NebiusServerlessDiskSpec("NETWORK_SSD", 10L * 1024 * 1024 * 1024),
        EnvironmentVariables: new Dictionary<string, string> { ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea" },
        SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>
        {
            [WorkerResultSigningPrivateKeyTrust.EnvironmentVariable] = new("mbsec-worker-result-signing", "mbsecver-worker-result-signing-v1")
        },
        Volumes: new[] { new NebiusServerlessVolumeMount("bucket", "/mnt/nvidea") });

    private sealed class SigningArtifacts(string workerPrivatePem) : IWorkerSigningCanaryArtifactTransport
    {
        private WorkerSigningCanaryProof? _proof;

        public Task PublishInvocationAsync(WorkerSigningCanaryInvocation invocation, CancellationToken cancellationToken = default)
        {
            _proof = WorkerSigningCanary.Sign(invocation.Challenge, workerPrivatePem, invocation.JobBindingSha256);
            return Task.CompletedTask;
        }

        public Task<WorkerSigningCanaryProof?> GetProofAsync(string nonce, CancellationToken cancellationToken = default) =>
            Task.FromResult(_proof);
    }

    private sealed class CompletedJobClient : INebiusServerlessJobClient
    {
        public NebiusServerlessJobSpec? CreatedSpec { get; private set; }
        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default)
        {
            CreatedSpec = spec;
            return Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{\"resourceId\":\"job-1\"}"));
        }

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new { metadata = new { id = "job-1", name = CreatedSpec!.Name }, status = new { state = "COMPLETED" } })));
        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) => Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"));
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
