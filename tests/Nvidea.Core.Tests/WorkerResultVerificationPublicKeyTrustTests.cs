using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class WorkerResultVerificationPublicKeyTrustTests
{
    [Fact]
    public void LoadRequired_UsesDedicatedClientTrustSetting()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var reads = new List<string>();

        var canonical = WorkerResultVerificationPublicKeyTrust.LoadRequired(name =>
        {
            reads.Add(name);
            return name == WorkerResultVerificationPublicKeyTrust.EnvironmentVariable ? publicPem : null;
        });

        Assert.Equal(new[] { WorkerResultVerificationPublicKeyTrust.EnvironmentVariable }, reads);
        Assert.Contains("BEGIN PUBLIC KEY", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadRequired_FailsClosedWhenPinIsMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            WorkerResultVerificationPublicKeyTrust.LoadRequired(_ => null));

        Assert.Contains(WorkerResultVerificationPublicKeyTrust.EnvironmentVariable, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAndCanonicalize_StripsAccidentallySuppliedPrivateAuthority()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportRSAPrivateKeyPem();

        var canonical = WorkerResultVerificationPublicKeyTrust.ValidateAndCanonicalize(privatePem);

        Assert.Contains("BEGIN PUBLIC KEY", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE", canonical, StringComparison.Ordinal);

        using var publicOnly = RSA.Create();
        publicOnly.ImportFromPem(canonical);
        Assert.ThrowsAny<CryptographicException>(() => publicOnly.ExportParameters(true));
    }

    [Fact]
    public void ValidateAndCanonicalize_RejectsUndersizedIdentity()
    {
        using var rsa = RSA.Create(1024);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        Assert.Throws<CryptographicException>(() =>
            WorkerResultVerificationPublicKeyTrust.ValidateAndCanonicalize(publicPem));
    }
}
