namespace Nvidea.Core.Desktop;

/// <summary>
/// Named checkpoints for the composition-root ownership boundary. Production startup can mark
/// these points without changing provider behavior; the deterministic harness below exercises
/// the same <see cref="StartupResourceLease"/> ownership semantics without live credentials.
/// </summary>
internal enum CompositionStartupCheckpoint
{
    AfterMemoryInitialization,
    AfterTavilyAcquisition,
    AfterCloudProviderTransfer,
    BeforeOwnershipRelease
}

internal static class CompositionStartupFailureBoundary
{
    internal static void RunForTest(
        CompositionStartupCheckpoint checkpoint,
        Exception authoritativeFailure,
        IList<string> cleanupOrder,
        string? cleanupFailureResource = null)
    {
        ArgumentNullException.ThrowIfNull(authoritativeFailure);
        ArgumentNullException.ThrowIfNull(cleanupOrder);

        using var lease = new StartupResourceLease();
        Own(lease, "nebius", cleanupOrder, cleanupFailureResource);
        Own(lease, "store", cleanupOrder, cleanupFailureResource);
        Own(lease, "embedding", cleanupOrder, cleanupFailureResource);
        Own(lease, "memory", cleanupOrder, cleanupFailureResource);

        if (checkpoint >= CompositionStartupCheckpoint.AfterTavilyAcquisition)
            Own(lease, "tavily", cleanupOrder, cleanupFailureResource);

        if (checkpoint >= CompositionStartupCheckpoint.AfterCloudProviderTransfer)
        {
            Own(lease, "object-storage", cleanupOrder, cleanupFailureResource);
            Own(lease, "serverless", cleanupOrder, cleanupFailureResource);
        }

        // Throw the exact supplied exception instance. The lease's best-effort Dispose boundary
        // must never replace it, even when one cleanup callback fails.
        throw authoritativeFailure;
    }

    private static void Own(
        StartupResourceLease lease,
        string name,
        IList<string> cleanupOrder,
        string? cleanupFailureResource)
    {
        lease.Own(new TrackedResource(name, cleanupOrder, cleanupFailureResource));
    }

    private sealed class TrackedResource : IDisposable
    {
        private readonly string _name;
        private readonly IList<string> _cleanupOrder;
        private readonly string? _cleanupFailureResource;
        private int _disposed;

        public TrackedResource(string name, IList<string> cleanupOrder, string? cleanupFailureResource)
        {
            _name = name;
            _cleanupOrder = cleanupOrder;
            _cleanupFailureResource = cleanupFailureResource;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                throw new InvalidOperationException($"Resource '{_name}' was disposed more than once.");

            _cleanupOrder.Add(_name);
            if (string.Equals(_name, _cleanupFailureResource, StringComparison.Ordinal))
                throw new InvalidOperationException("synthetic cleanup failure");
        }
    }
}
