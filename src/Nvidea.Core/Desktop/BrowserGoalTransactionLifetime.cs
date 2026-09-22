namespace Nvidea.Core.Desktop;

/// <summary>
/// Internal, least-authority lifetime boundary for an issued browser-goal agent.
/// Composition binds the root lifetime exactly once before publication; each public goal
/// transaction then executes under one lease. Internal transaction cores must never reacquire it.
/// </summary>
internal sealed class BrowserGoalTransactionLifetime
{
    private CompositionLifetimeGate? _lifetime;

    internal void Bind(CompositionLifetimeGate lifetime)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        if (Interlocked.CompareExchange(ref _lifetime, lifetime, null) is not null)
            throw new InvalidOperationException("Browser goal transaction lifetime is already bound.");
    }

    internal async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var lifetime = Volatile.Read(ref _lifetime)
            ?? throw new InvalidOperationException("Browser goal agent was published without composition lifetime authority.");

        await using var lease = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await operation(cancellationToken).ConfigureAwait(false);
    }
}
