using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchDispatchBindingTests
{
    [Fact]
    public void Verify_RejectsRemoteJobIdSubstitution()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var now = DateTimeOffset.UtcNow;
        var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var signed = ResearchDispatchBindingProtector.Sign(
            opaqueId,
            "job-authoritative-1",
            now,
            now.AddMinutes(10),
            privatePem);

        var substituted = signed with { RemoteJobId = "job-substituted-2" };

        Assert.Throws<CryptographicException>(() =>
            ResearchDispatchBindingProtector.Verify(substituted, opaqueId, publicPem, now.AddSeconds(1)));
    }

    [Fact]
    public void Verify_RejectsExpiredBinding()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var now = DateTimeOffset.UtcNow;
        var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var signed = ResearchDispatchBindingProtector.Sign(
            opaqueId,
            "job-authoritative-1",
            now,
            now.AddSeconds(1),
            privatePem);

        Assert.Throws<InvalidOperationException>(() =>
            ResearchDispatchBindingProtector.Verify(signed, opaqueId, publicPem, now.AddSeconds(2)));
    }

    [Fact]
    public async Task Publisher_IsIdempotentForSameAuthoritativeResource_AndRejectsConflict()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var root = Path.Combine(Path.GetTempPath(), "nvidea-binding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var transport = new DirectoryProtectedResearchTransport(root);
            var publisher = new ResearchDispatchBindingPublisher(transport, privatePem);
            var now = DateTimeOffset.UtcNow;
            var opaqueId = "abcdefghijklmnopqrstuvwx12345678";

            var first = await publisher.PublishAsync(
                opaqueId,
                "job-authoritative-1",
                now.AddMinutes(10),
                now);
            var second = await publisher.PublishAsync(
                opaqueId,
                "job-authoritative-1",
                now.AddMinutes(10),
                now.AddSeconds(1));

            Assert.Equal(first.RemoteJobId, second.RemoteJobId);
            Assert.Equal(first.Signature, second.Signature);
            await Assert.ThrowsAsync<CryptographicException>(() => publisher.PublishAsync(
                opaqueId,
                "job-conflicting-2",
                now.AddMinutes(10),
                now.AddSeconds(1)));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task Waiter_ReturnsVerifiedBindingFromSharedTransport()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var root = Path.Combine(Path.GetTempPath(), "nvidea-binding-wait-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var transport = new DirectoryProtectedResearchTransport(root);
            var publisher = new ResearchDispatchBindingPublisher(transport, privatePem);
            var now = DateTimeOffset.UtcNow;
            var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
            await publisher.PublishAsync(opaqueId, "job-authoritative-1", now.AddMinutes(10), now);

            var waiter = new ResearchDispatchBindingWaiter(
                transport,
                publicPem,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1));
            var resolved = await waiter.WaitAsync(opaqueId);

            Assert.Equal("job-authoritative-1", resolved.RemoteJobId);
            Assert.Equal(
                ResearchDispatchBindingProtector.GetDeterministicRemoteJobName(opaqueId),
                resolved.RemoteJobName);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }
}
