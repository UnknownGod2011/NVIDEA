using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchWorkItemProtectorKeyTrustTests
{
    [Fact]
    public void Unprotect_ValidWorkerPrivateKeyRoundTripsProtectedWorkItem()
    {
        using var worker = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = CreateWorkItem(now);
        var envelope = ResearchWorkItemProtector.Protect(
            workItem,
            worker.ExportSubjectPublicKeyInfoPem());

        var restored = ResearchWorkItemProtector.Unprotect(
            envelope,
            worker.ExportPkcs8PrivateKeyPem(),
            now);

        Assert.Equal(workItem, restored);
    }

    [Fact]
    public void Unprotect_PublicOnlyWorkerKeyFailsClosedAtProtocolBoundary()
    {
        using var worker = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchWorkItemProtector.Protect(
            CreateWorkItem(now),
            worker.ExportSubjectPublicKeyInfoPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Unprotect(
                envelope,
                worker.ExportSubjectPublicKeyInfoPem(),
                now));

        Assert.Contains("private", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unprotect_WeakWorkerPrivateKeyFailsClosedAtProtocolBoundary()
    {
        using var protectingWorker = RSA.Create(2048);
        using var weakWorker = RSA.Create(1024);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchWorkItemProtector.Protect(
            CreateWorkItem(now),
            protectingWorker.ExportSubjectPublicKeyInfoPem());

        var error = Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Unprotect(
                envelope,
                weakWorker.ExportPkcs8PrivateKeyPem(),
                now));

        Assert.Contains("2048", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unprotect_MalformedWorkerPrivateKeyFailsClosedAtProtocolBoundary()
    {
        using var worker = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchWorkItemProtector.Protect(
            CreateWorkItem(now),
            worker.ExportSubjectPublicKeyInfoPem());

        Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Unprotect(
                envelope,
                "-----BEGIN PRIVATE KEY-----\nnot-valid-base64\n-----END PRIVATE KEY-----",
                now));
    }

    [Fact]
    public void SharedTrust_CanonicalizationMatchesWorkerRuntimeEntryPoint()
    {
        using var worker = RSA.Create(2048);
        var pem = worker.ExportRSAPrivateKeyPem();

        var shared = WorkerEnvelopePrivateKeyTrust.ValidateAndCanonicalize(pem);
        var runtime = NebiusResearchWorkerRuntimeConfiguration.ValidateWorkerPrivateKey(pem);

        Assert.Equal(shared, runtime);
        Assert.StartsWith("-----BEGIN PRIVATE KEY-----", shared, StringComparison.Ordinal);
    }

    private static RemoteResearchWorkItem CreateWorkItem(DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            "research.search",
            "{\"question\":\"test\"}",
            ContainsPrivateOsData: false,
            CreatedAt: now,
            ExpiresAt: now.AddMinutes(30));
}
