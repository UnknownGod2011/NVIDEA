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

    [Fact]
    public async Task Waiter_DelayedPublication_RetriesWithStableAuthoritativeBinding()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var now = DateTimeOffset.UtcNow;
        var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var workItemExpiry = now.AddMinutes(10);
        var signed = ResearchDispatchBindingProtector.Sign(
            opaqueId,
            "job-authoritative-delayed",
            now,
            now.AddMinutes(5),
            privatePem);
        var transport = new SequencedBindingTransport(read => read >= 3 ? signed : null);
        var waiter = new ResearchDispatchBindingWaiter(
            transport,
            publicPem,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(2));

        var resolved = await waiter.WaitAsync(opaqueId, workItemExpiry);

        Assert.Equal("job-authoritative-delayed", resolved.RemoteJobId);
        Assert.Equal(3, transport.ReadCount);
    }

    [Fact]
    public async Task Waiter_WorkItemExpiryBoundsConfiguredWaitBudget()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var transport = new SequencedBindingTransport(_ => null);
        var waiter = new ResearchDispatchBindingWaiter(
            transport,
            publicPem,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(5));

        var error = await Assert.ThrowsAsync<TimeoutException>(() =>
            waiter.WaitAsync(opaqueId, DateTimeOffset.UtcNow.AddMilliseconds(250)));

        Assert.Contains("work-item expiry", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(transport.ReadCount, 1, 3);
    }

    [Fact]
    public async Task Waiter_RejectsSignedBindingThatOutlivesProtectedWorkItem()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var now = DateTimeOffset.UtcNow;
        var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var workItemExpiry = now.AddMinutes(5);
        var signed = ResearchDispatchBindingProtector.Sign(
            opaqueId,
            "job-authoritative-too-long",
            now,
            now.AddMinutes(10),
            privatePem);
        var transport = new SequencedBindingTransport(_ => signed);
        var waiter = new ResearchDispatchBindingWaiter(
            transport,
            publicPem,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1));

        var error = await Assert.ThrowsAsync<CryptographicException>(() =>
            waiter.WaitAsync(opaqueId, workItemExpiry));

        Assert.Contains("outlives", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, transport.ReadCount);
    }

    [Fact]
    public async Task Waiter_CancellationDuringRetryStopsWithoutAnotherTransportRead()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        using var cts = new CancellationTokenSource();
        var transport = new SequencedBindingTransport(_ =>
        {
            cts.Cancel();
            return null;
        });
        var waiter = new ResearchDispatchBindingWaiter(
            transport,
            publicPem,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            waiter.WaitAsync(opaqueId, DateTimeOffset.UtcNow.AddMinutes(5), cts.Token));

        Assert.Equal(1, transport.ReadCount);
    }

    private sealed class SequencedBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        private readonly Func<int, ProtectedResearchDispatchBinding?> _onRead;

        public SequencedBindingTransport(Func<int, ProtectedResearchDispatchBinding?> onRead)
        {
            _onRead = onRead;
        }

        public int ReadCount { get; private set; }

        public Task PutAsync(
            ProtectedResearchDispatchBinding binding,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProtectedResearchDispatchBinding?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            return Task.FromResult(_onRead(ReadCount));
        }

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
