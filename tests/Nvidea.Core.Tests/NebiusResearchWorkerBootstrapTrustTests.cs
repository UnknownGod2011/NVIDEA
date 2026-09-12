using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchWorkerBootstrapTrustTests
{
    [Fact]
    public void Load_ValidatesAndCanonicalizesPublicIdentityBeforeSecrets()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var reads = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea",
            ["NVIDEA_CLIENT_PUBLIC_KEY_PEM"] = publicPem,
            ["NEBIUS_API_KEY"] = "must-not-be-read",
            ["TAVILY_API_KEY"] = "must-not-be-read",
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = "must-not-be-read"
        };

        var trust = NebiusResearchWorkerBootstrapTrust.Load(name =>
        {
            reads.Add(name);
            return values.GetValueOrDefault(name);
        });

        Assert.Equal("/mnt/nvidea", trust.TransportRoot);
        Assert.Equal(publicPem, trust.ClientVerificationPublicKeyPem);
        Assert.Equal(
            new[] { "NVIDEA_TRANSPORT_ROOT", "NVIDEA_CLIENT_PUBLIC_KEY_PEM" },
            reads);
    }

    [Fact]
    public void Load_PrivateClientKeyFailsBeforeAnyProviderSecretRead()
    {
        using var rsa = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(rsa.ExportPkcs8PrivateKeyPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NEBIUS_API_KEY", reads);
        Assert.DoesNotContain("TAVILY_API_KEY", reads);
        Assert.DoesNotContain("NVIDEA_WORKER_PRIVATE_KEY_PEM", reads);
    }

    [Fact]
    public void Load_WeakClientKeyFailsBeforeAnyProviderSecretRead()
    {
        using var rsa = RSA.Create(1024);
        var reads = new List<string>();
        var values = CreateValues(rsa.ExportSubjectPublicKeyInfoPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("2048", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("NEBIUS_API_KEY", reads);
        Assert.DoesNotContain("TAVILY_API_KEY", reads);
        Assert.DoesNotContain("NVIDEA_WORKER_PRIVATE_KEY_PEM", reads);
    }

    [Fact]
    public void Load_InvalidTransportRootFailsBeforeClientKeyOrSecrets()
    {
        var reads = new List<string>();
        var values = CreateValues("not-used");
        values["NVIDEA_TRANSPORT_ROOT"] = "relative/path";

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Equal(new[] { "NVIDEA_TRANSPORT_ROOT" }, reads);
    }

    private static Dictionary<string, string> CreateValues(string clientKey) =>
        new(StringComparer.Ordinal)
        {
            ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea",
            ["NVIDEA_CLIENT_PUBLIC_KEY_PEM"] = clientKey,
            ["NEBIUS_API_KEY"] = "must-not-be-read",
            ["TAVILY_API_KEY"] = "must-not-be-read",
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = "must-not-be-read"
        };
}
