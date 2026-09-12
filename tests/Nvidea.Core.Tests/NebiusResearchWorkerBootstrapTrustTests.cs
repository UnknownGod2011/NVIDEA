using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchWorkerBootstrapTrustTests
{
    [Fact]
    public void Load_ValidatesDistinctClientPublicIdentitiesBeforeSecrets()
    {
        using var signingRsa = RSA.Create(2048);
        using var resultRsa = RSA.Create(2048);
        var signingPublicPem = signingRsa.ExportSubjectPublicKeyInfoPem();
        var resultPublicPem = resultRsa.ExportSubjectPublicKeyInfoPem();
        var reads = new List<string>();
        var values = CreateValues(signingPublicPem, resultPublicPem);

        var trust = NebiusResearchWorkerBootstrapTrust.Load(name =>
        {
            reads.Add(name);
            return values.GetValueOrDefault(name);
        });

        Assert.Equal("/mnt/nvidea", trust.TransportRoot);
        Assert.Equal(signingPublicPem, trust.ClientVerificationPublicKeyPem);
        Assert.Equal(resultPublicPem, trust.ClientResultEncryptionPublicKeyPem);
        Assert.Equal(
            new[]
            {
                "NVIDEA_TRANSPORT_ROOT",
                "NVIDEA_CLIENT_PUBLIC_KEY_PEM",
                "NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM"
            },
            reads);
    }

    [Fact]
    public void Load_PrivateClientSigningKeyFailsBeforeResultIdentityOrSecrets()
    {
        using var rsa = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(rsa.ExportPkcs8PrivateKeyPem(), "must-not-be-read");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM", reads);
        AssertNoSecretsRead(reads);
    }

    [Fact]
    public void Load_WeakClientSigningKeyFailsBeforeResultIdentityOrSecrets()
    {
        using var rsa = RSA.Create(1024);
        var reads = new List<string>();
        var values = CreateValues(rsa.ExportSubjectPublicKeyInfoPem(), "must-not-be-read");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("2048", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM", reads);
        AssertNoSecretsRead(reads);
    }

    [Fact]
    public void Load_PrivateResultEncryptionKeyFailsBeforeAnyProviderSecretRead()
    {
        using var signingRsa = RSA.Create(2048);
        using var resultRsa = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            signingRsa.ExportSubjectPublicKeyInfoPem(),
            resultRsa.ExportPkcs8PrivateKeyPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
        AssertNoSecretsRead(reads);
    }

    [Fact]
    public void Load_ReusedSigningIdentityForResultEncryptionFailsBeforeAnyProviderSecretRead()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var reads = new List<string>();
        var values = CreateValues(publicPem, publicPem);

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("distinct", error.Message, StringComparison.OrdinalIgnoreCase);
        AssertNoSecretsRead(reads);
    }

    [Fact]
    public void Load_InvalidTransportRootFailsBeforeClientKeysOrSecrets()
    {
        var reads = new List<string>();
        var values = CreateValues("not-used", "not-used");
        values["NVIDEA_TRANSPORT_ROOT"] = "relative/path";

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerBootstrapTrust.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Equal(new[] { "NVIDEA_TRANSPORT_ROOT" }, reads);
    }

    private static void AssertNoSecretsRead(IReadOnlyCollection<string> reads)
    {
        Assert.DoesNotContain("NEBIUS_API_KEY", reads);
        Assert.DoesNotContain("TAVILY_API_KEY", reads);
        Assert.DoesNotContain("NVIDEA_WORKER_PRIVATE_KEY_PEM", reads);
    }

    private static Dictionary<string, string> CreateValues(string signingKey, string resultKey) =>
        new(StringComparer.Ordinal)
        {
            ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea",
            ["NVIDEA_CLIENT_PUBLIC_KEY_PEM"] = signingKey,
            ["NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM"] = resultKey,
            ["NEBIUS_API_KEY"] = "must-not-be-read",
            ["TAVILY_API_KEY"] = "must-not-be-read",
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = "must-not-be-read"
        };
}
