using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchWorkItemProtectorKeyTrustTests
{
    [Fact]
    public void Protect_ValidWorkerPublicKeyRoundTripsProtectedWorkItem()
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
    public void Protect_PrivateWorkerKeyFailsClosedAtProtocolBoundary()
    {
        using var worker = RSA.Create(2048);

        var error = Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Protect(
                CreateWorkItem(DateTimeOffset.UtcNow),
                worker.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Protect_WeakWorkerPublicKeyFailsClosedAtProtocolBoundary()
    {
        using var worker = RSA.Create(1024);

        var error = Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Protect(
                CreateWorkItem(DateTimeOffset.UtcNow),
                worker.ExportSubjectPublicKeyInfoPem()));

        Assert.Contains("2048", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("-----BEGIN PUBLIC KEY-----\nnot-valid-base64\n-----END PUBLIC KEY-----")]
    [InlineData("-----BEGIN PUBLIC KEY-----\nAAAA\0BBBB\n-----END PUBLIC KEY-----")]
    public void Protect_MalformedOrControlCharacterWorkerPublicKeyFailsClosedAtProtocolBoundary(string pem)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Protect(
                CreateWorkItem(DateTimeOffset.UtcNow),
                pem));
    }

    [Fact]
    public void Protect_OversizedWorkerPublicKeyFailsClosedAtProtocolBoundary()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ResearchWorkItemProtector.Protect(
                CreateWorkItem(DateTimeOffset.UtcNow),
                new string('A', 65537)));
    }

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
    public void WorkerPublicTrust_ValidPublicKeyCanonicalizes()
    {
        using var worker = RSA.Create(2048);

        var canonical = WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(
            worker.ExportRSAPublicKeyPem());

        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkerPublicTrust_PrivateKeyFailsClosed()
    {
        using var worker = RSA.Create(2048);

        var error = Assert.Throws<InvalidOperationException>(() =>
            WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(worker.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkerPublicTrust_WeakKeyFailsClosed()
    {
        using var worker = RSA.Create(1024);

        var error = Assert.Throws<InvalidOperationException>(() =>
            WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(worker.ExportSubjectPublicKeyInfoPem()));

        Assert.Contains("2048", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkerPublicTrust_MalformedOversizedAndControlCharacterPemFailClosed()
    {
        Assert.Throws<InvalidOperationException>(() =>
            WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(
                "-----BEGIN PUBLIC KEY-----\nnot-valid-base64\n-----END PUBLIC KEY-----"));

        Assert.Throws<InvalidOperationException>(() =>
            WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(new string('A', 65537)));

        Assert.Throws<InvalidOperationException>(() =>
            WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(
                "-----BEGIN PUBLIC KEY-----\nAAAA\0BBBB\n-----END PUBLIC KEY-----"));
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
