using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class StartupResourceLeaseTests
{
    [Fact]
    public void Dispose_ReleasesResourcesInReverseAcquisitionOrderExactlyOnce()
    {
        var events = new List<string>();
        var first = new RecordingDisposable("first", events);
        var second = new RecordingDisposable("second", events);

        var lease = new StartupResourceLease();
        Assert.Same(first, lease.Own(first));
        Assert.Same(second, lease.Own(second));

        lease.Dispose();
        lease.Dispose();

        Assert.Equal(new[] { "second", "first" }, events);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
    }

    [Fact]
    public void Dispose_ContainsCleanupFailureAndContinuesReleasingEarlierResources()
    {
        var events = new List<string>();
        var first = new RecordingDisposable("first", events);
        var failing = new RecordingDisposable("failing", events, throwOnDispose: true);
        var last = new RecordingDisposable("last", events);

        var lease = new StartupResourceLease();
        lease.Own(first);
        lease.Own(failing);
        lease.Own(last);

        var exception = Record.Exception(lease.Dispose);

        Assert.Null(exception);
        Assert.Equal(new[] { "last", "failing", "first" }, events);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, failing.DisposeCount);
        Assert.Equal(1, last.DisposeCount);
    }

    [Fact]
    public void ReleaseAll_TransfersOwnershipAndPreventsLeaseCleanup()
    {
        var events = new List<string>();
        var resource = new RecordingDisposable("resource", events);
        using var lease = new StartupResourceLease();
        lease.Own(resource);

        lease.ReleaseAll();
        lease.Dispose();

        Assert.Empty(events);
        Assert.Equal(0, resource.DisposeCount);
    }

    [Fact]
    public void Own_AfterReleaseOrDispose_FailsClosed()
    {
        using var released = new StartupResourceLease();
        released.ReleaseAll();
        Assert.Throws<InvalidOperationException>(() => released.Own(new RecordingDisposable("late", new List<string>())));

        var disposed = new StartupResourceLease();
        disposed.Dispose();
        Assert.Throws<ObjectDisposedException>(() => disposed.Own(new RecordingDisposable("late", new List<string>())));
    }

    private sealed class RecordingDisposable : IDisposable
    {
        private readonly string _name;
        private readonly IList<string> _events;
        private readonly bool _throwOnDispose;

        public RecordingDisposable(string name, IList<string> events, bool throwOnDispose = false)
        {
            _name = name;
            _events = events;
            _throwOnDispose = throwOnDispose;
        }

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            _events.Add(_name);
            if (_throwOnDispose)
                throw new InvalidOperationException("synthetic cleanup failure");
        }
    }
}
