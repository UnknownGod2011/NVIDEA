namespace Nvidea.Core.Desktop;

/// <summary>
/// Owns disposable resources while a composition root is being assembled. Ownership is
/// transferred only after construction succeeds. If startup fails, resources are disposed in
/// reverse acquisition order and cleanup failures are contained so the original startup failure
/// remains authoritative.
/// </summary>
internal sealed class StartupResourceLease : IDisposable
{
    private readonly List<IDisposable> _resources = new();
    private bool _released;
    private bool _disposed;

    public T Own<T>(T resource) where T : class, IDisposable
    {
        ArgumentNullException.ThrowIfNull(resource);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_released)
            throw new InvalidOperationException("Startup resource ownership has already been transferred.");

        _resources.Add(resource);
        return resource;
    }

    /// <summary>
    /// Transfers ownership to the completed composition root. After this call Dispose is a no-op.
    /// </summary>
    public void ReleaseAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _released = true;
        _resources.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_released)
            return;

        for (var index = _resources.Count - 1; index >= 0; index--)
        {
            try
            {
                _resources[index].Dispose();
            }
            catch
            {
                // Startup cleanup is best-effort. Never replace the actual initialization
                // exception with a secondary disposal exception or project provider details.
            }
        }

        _resources.Clear();
    }
}
