namespace Nvidea.Core.Desktop;

/// <summary>
/// Serializes composition-root resource acquisition with shutdown. A caller that obtains a
/// lease is guaranteed that disposal has not crossed the gate's linearization point; once
/// disposal begins, subsequent acquisitions fail closed.
/// </summary>
internal sealed class CompositionLifetimeGate : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _stateLock = new();
    private TaskCompletionSource? _disposalCompleted;
    private bool _disposing;
    private bool _disposed;

    public async ValueTask<Lease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (_disposing || _disposed)
                throw new ObjectDisposedException(nameof(CompositionLifetimeGate));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        bool reject;
        lock (_stateLock)
        {
            reject = _disposing || _disposed;
        }

        if (reject)
        {
            _gate.Release();
            throw new ObjectDisposedException(nameof(CompositionLifetimeGate));
        }

        return new Lease(this);
    }

    public async ValueTask<Lease?> BeginDisposeAsync()
    {
        Task? existingDisposal;
        lock (_stateLock)
        {
            if (_disposed)
                return null;

            if (_disposing)
            {
                existingDisposal = _disposalCompleted?.Task;
            }
            else
            {
                _disposing = true;
                _disposalCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                existingDisposal = null;
            }
        }

        if (existingDisposal is not null)
        {
            await existingDisposal.ConfigureAwait(false);
            return null;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
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
        TaskCompletionSource? disposalCompleted = null;
        if (completesDisposal)
        {
            lock (_stateLock)
            {
                _disposed = true;
                disposalCompleted = _disposalCompleted;
            }
        }

        _gate.Release();
        if (completesDisposal)
            disposalCompleted?.TrySetResult();
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
