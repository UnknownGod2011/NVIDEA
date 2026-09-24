using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class WorkerResultSigningPrivateKeyTrustTests
{
    [Fact]
    public void LoadRequired_ReadsOnlyDedicatedSigningSecret()
    {
        using var rsa = RSA.Create(2048);
        var expected = rsa.ExportRSAPrivateKeyPem();
        var reads = new List<string>();

        var actual = WorkerResultSigningPrivateKeyTrust.LoadRequired(name =>
        {
            reads.Add(name);
            return name == WorkerResultSigningPrivateKeyTrust.EnvironmentVariable ? expected : "unexpected";
        });

        Assert.Equal(new[] { WorkerResultSigningPrivateKeyTrust.EnvironmentVariable }, reads);
        using var parsed = RSA.Create();
        parsed.ImportFromPem(actual);
        Assert.True(parsed.KeySize >= 2048);
        Assert.NotNull(parsed.ExportParameters(true).D);
    }

    [Fact]
    public void LoadRequired_RejectsMissingSecret()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            WorkerResultSigningPrivateKeyTrust.LoadRequired(_ => null));
        Assert.Contains(WorkerResultSigningPrivateKeyTrust.EnvironmentVariable, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAndCanonicalize_RejectsPublicOnlyIdentity()
    {
        using var rsa = RSA.Create(2048);
        Assert.Throws<CryptographicException>(() =>
            WorkerResultSigningPrivateKeyTrust.ValidateAndCanonicalize(rsa.ExportRSAPublicKeyPem()));
    }

    [Fact]
    public void ValidateAndCanonicalize_RejectsUndersizedRsaIdentity()
    {
        using var rsa = RSA.Create(1024);
        Assert.Throws<CryptographicException>(() =>
            WorkerResultSigningPrivateKeyTrust.ValidateAndCanonicalize(rsa.ExportRSAPrivateKeyPem()));
    }
}
