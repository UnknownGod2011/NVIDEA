using System.Security.Cryptography;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class WorkerResultVerificationFingerprintTests
{
    [Fact]
    public void Fingerprint_IsStableAcrossPublicAndPrivateRepresentations()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();

        var publicFingerprint = WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(publicPem);
        var privateFingerprint = WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(privatePem);

        Assert.Equal(publicFingerprint, privateFingerprint);
        Assert.Matches("^[0-9A-F]{64}$", publicFingerprint);
        Assert.DoesNotContain("BEGIN", publicFingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadRequiredFingerprint_ProjectsOnlyPublicIdentity()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();

        var fingerprint = WorkerResultVerificationPublicKeyTrust.LoadRequiredSha256Fingerprint(
            name => name == WorkerResultVerificationPublicKeyTrust.EnvironmentVariable ? privatePem : null);

        Assert.Equal(
            WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(rsa.ExportSubjectPublicKeyInfoPem()),
            fingerprint);
        Assert.DoesNotContain("PRIVATE", fingerprint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MYSTERY", fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fingerprint_ChangesWhenPinnedWorkerIdentityChanges()
    {
        using var first = RSA.Create(2048);
        using var second = RSA.Create(2048);

        Assert.NotEqual(
            WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(first.ExportSubjectPublicKeyInfoPem()),
            WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(second.ExportSubjectPublicKeyInfoPem()));
    }
}
