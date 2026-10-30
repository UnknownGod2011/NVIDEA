using System.Security.Cryptography;
using Nvidea.Core.Jobs;
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
}
