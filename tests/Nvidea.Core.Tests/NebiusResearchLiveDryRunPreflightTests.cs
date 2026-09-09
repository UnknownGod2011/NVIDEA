using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLiveDryRunPreflightTests
{
    [Fact]
    public void Validate_AcceptsDigestPinnedAlignedConfigurationAndMatchingSigningIdentity()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());
        var storage = CreateStorage();

        NebiusResearchLiveDryRunPreflight.Validate(
            dispatch,
            storage,
            "serverless-access-token",
            "project-test",
            client.ExportPkcs8PrivateKeyPem());
    }

    [Fact]
    public void Validate_RejectsMutableWorkerImageTag()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem()) with
        {
            WorkerImage = "cr.eu-north1.nebius.cloud/nvidea/worker:latest"
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveDryRunPreflight.Validate(
                dispatch,
                CreateStorage(),
                "serverless-access-token",
                "project-test",
                client.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("digest-pinned", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsClientSigningKeyThatDoesNotMatchWorkerVerificationKey()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        using var otherClient = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveDryRunPreflight.Validate(
                dispatch,
                CreateStorage(),
                "serverless-access-token",
                "project-test",
                otherClient.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("does not match", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsMalformedWorkerEnvelopePublicKey()
    {
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch("not-a-public-key", client.ExportSubjectPublicKeyInfoPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveDryRunPreflight.Validate(
                dispatch,
                CreateStorage(),
                "serverless-access-token",
                "project-test",
                client.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("worker envelope public key", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsObjectStoragePrefixMismatchBeforeAnyProviderCall()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());
        var storage = CreateStorage() with { Prefix = "other-prefix" };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveDryRunPreflight.Validate(
                dispatch,
                storage,
                "serverless-access-token",
                "project-test",
                client.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("prefix", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static NebiusResearchDispatchOptions CreateDispatch(string workerPublicKeyPem, string clientPublicKeyPem) =>
        new(
            WorkerImage: $"cr.eu-north1.nebius.cloud/nvidea/worker@sha256:{new string('a', 64)}",
            WorkerPublicKeyPem: workerPublicKeyPem,
            ContainerCommand: "dotnet",
            Platform: "cpu-d3",
            Preset: "1vcpu-4gb",
            Timeout: "3600s",
            SubnetId: "subnet-test",
            Disk: new NebiusServerlessDiskSpec("NETWORK_SSD", 10L * 1024 * 1024 * 1024),
            EnvironmentVariables: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = "/mnt/nvidea-research",
                [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = clientPublicKeyPem
            },
            SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
            {
                ["NEBIUS_API_KEY"] = new(SecretId: "mbsec-nebius"),
                ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily"),
                ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = new(VersionId: "mbsecver-worker")
            },
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount(
                    "nvidea-live-bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE",
                    "nvidea-research")
            });

    private static NebiusObjectStorageClientOptions CreateStorage() =>
        new(
            Endpoint: "https://storage.eu-north1.nebius.cloud",
            Region: "eu-north1",
            Bucket: "nvidea-live-bucket",
            AccessKeyId: "access-key",
            SecretAccessKey: "secret-key",
            Prefix: "nvidea-research",
            OperationTimeout: TimeSpan.FromSeconds(30),
            MaxRetries: 2);
}
