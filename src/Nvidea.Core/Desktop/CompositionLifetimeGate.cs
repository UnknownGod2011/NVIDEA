namespace Nvidea.Core.Desktop;

/// <summary>
/// Serializes composition-root resource acquisition with shutdown. A caller that obtains a
/// lease is guaranteed that disposal has not crossed the gate's linearization point; once
/// disposal begins, subsequent acquisitions fail closed.
/// </summary>
internal sealed class CompositionLifetimeGate : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposing;
    private bool _disposed;

    public async ValueTask<Lease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (_disposing || _disposed)
        {
            _gate.Release();
            throw new ObjectDisposedException(nameof(CompositionLifetimeGate));
        }

        return new Lease(this);
    }

    public async ValueTask<Lease?> BeginDisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        if (_disposing || _disposed)
        {
            _gate.Release();
            return null;
        }

        _disposing = true;
        return new Lease(this, completesDisposal: true);
    }

    public async ValueTask DisposeAsync()
    {
        var lease = await BeginDisposeAsync().ConfigureAwait(false);
        if (lease is not null)
            await lease.DisposeAsync().ConfigureAwait(false);
    }

    private void Release(bool completesDisposal)
    {
        if (completesDisposal)
            _disposed = true;
        _gate.Release();
    }

    internal sealed class Lease : IAsyncDisposable, IDisposable
    {
        private CompositionLifetimeGate? _owner;
        private readonly bool _completesDisposal;

        internal Lease(CompositionLifetimeGate owner, bool completesDisposal = false)
        {
            _owner = owner;
            _completesDisposal = completesDisposal;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.Release(_completesDisposal);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
