using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class WorkerProtectedResearchWorkItemLoaderTests
{
    private const string OpaqueId = "abcdefghijklmnopqrstuvwx12345678";

    [Fact]
    public async Task LoadAsync_DelayedVisibilityEventuallyReturnsEnvelope()
    {
        var envelope = CreateEnvelope();
        var transport = new ScriptedTransport((attempt, _) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(attempt < 3 ? null : envelope));
        var loader = CreateLoader(transport);

        var loaded = await loader.LoadAsync(OpaqueId);

        Assert.Same(envelope, loaded);
        Assert.Equal(3, transport.ReadCount);
    }

    [Fact]
    public async Task LoadAsync_TransientIoFailureEventuallyReturnsEnvelope()
    {
        var envelope = CreateEnvelope();
        var transport = new ScriptedTransport((attempt, _) =>
            attempt == 1
                ? Task.FromException<ProtectedResearchWorkItemEnvelope?>(new IOException("mount reconnecting"))
                : Task.FromResult<ProtectedResearchWorkItemEnvelope?>(envelope));
        var loader = CreateLoader(transport);

        var loaded = await loader.LoadAsync(OpaqueId);

        Assert.Same(envelope, loaded);
        Assert.Equal(2, transport.ReadCount);
    }

    [Fact]
    public async Task LoadAsync_CancellationRacingIoFailureStopsBeforeAnotherRead()
    {
        using var cancellation = new CancellationTokenSource();
        var transport = new ScriptedTransport((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromException<ProtectedResearchWorkItemEnvelope?>(new IOException("mount reconnecting"));
        });
        var loader = CreateLoader(transport);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            loader.LoadAsync(OpaqueId, cancellation.Token));

        Assert.Equal(1, transport.ReadCount);
    }

    [Fact]
    public async Task LoadAsync_MissingEnvelopeStopsAtBootstrapDeadline()
    {
        var transport = new ScriptedTransport((_, _) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(null));
        var loader = new WorkerProtectedResearchWorkItemLoader(
            transport,
            pollInterval: TimeSpan.FromMilliseconds(100),
            maxWait: TimeSpan.FromMilliseconds(240));

        await Assert.ThrowsAsync<TimeoutException>(() => loader.LoadAsync(OpaqueId));

        Assert.InRange(transport.ReadCount, 1, 4);
    }

    [Fact]
    public async Task LoadAsync_PermanentTransportValidationFailureIsNotRetried()
    {
        var transport = new ScriptedTransport((_, _) =>
            Task.FromException<ProtectedResearchWorkItemEnvelope?>(
                new InvalidOperationException("Protected research envelope has an invalid transport size.")));
        var loader = CreateLoader(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => loader.LoadAsync(OpaqueId));

        Assert.Equal(1, transport.ReadCount);
    }

    [Fact]
    public async Task LoadAsync_SubstitutedOpaqueIdFailsClosedWithoutRetry()
    {
        var envelope = CreateEnvelope() with { OpaqueWorkItemId = "zyxwvutsrqponmlkjihgfedc87654321" };
        var transport = new ScriptedTransport((_, _) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(envelope));
        var loader = CreateLoader(transport);

        await Assert.ThrowsAsync<CryptographicException>(() => loader.LoadAsync(OpaqueId));

        Assert.Equal(1, transport.ReadCount);
    }

    [Fact]
    public async Task LoadAsync_InvalidProtocolFailsClosedWithoutRetry()
    {
        var envelope = CreateEnvelope() with { ProtocolVersion = "nvidea.research.remote.v999" };
        var transport = new ScriptedTransport((_, _) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(envelope));
        var loader = CreateLoader(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => loader.LoadAsync(OpaqueId));

        Assert.Equal(1, transport.ReadCount);
    }

    [Fact]
    public async Task LoadAsync_ExpiredTransportMetadataCannotAuthorizeBootstrap()
    {
        var now = DateTimeOffset.UtcNow;
        var envelope = CreateEnvelope() with
        {
            CreatedAt = now.AddMinutes(-2),
            ExpiresAt = now.AddSeconds(-1)
        };
        var transport = new ScriptedTransport((_, _) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(envelope));
        var loader = CreateLoader(transport);

        await Assert.ThrowsAsync<TimeoutException>(() => loader.LoadAsync(OpaqueId));

        Assert.Equal(1, transport.ReadCount);
    }

    private static WorkerProtectedResearchWorkItemLoader CreateLoader(IProtectedResearchWorkItemTransport transport) =>
        new(
            transport,
            pollInterval: TimeSpan.FromMilliseconds(100),
            maxWait: TimeSpan.FromSeconds(2));

    private static ProtectedResearchWorkItemEnvelope CreateEnvelope()
    {
        var now = DateTimeOffset.UtcNow;
        return new ProtectedResearchWorkItemEnvelope(
            ResearchWorkItemProtector.ProtocolVersion,
            OpaqueId,
            Convert.ToBase64String(new byte[256]),
            Convert.ToBase64String(new byte[12]),
            Convert.ToBase64String(new byte[8]),
            Convert.ToBase64String(new byte[16]),
            now.AddSeconds(-1),
            now.AddMinutes(5));
    }

    private sealed class ScriptedTransport : IProtectedResearchWorkItemTransport
    {
        private readonly Func<int, CancellationToken, Task<ProtectedResearchWorkItemEnvelope?>> _read;
        private int _readCount;

        public ScriptedTransport(Func<int, CancellationToken, Task<ProtectedResearchWorkItemEnvelope?>> read)
        {
            _read = read;
        }

        public int ReadCount => Volatile.Read(ref _readCount);

        public Task PutAsync(
            ProtectedResearchWorkItemEnvelope envelope,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _readCount);
            return _read(attempt, cancellationToken);
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
