using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Worker-side bootstrap boundary for acquiring the encrypted research work item from a mounted
/// transport. Missing-object propagation and transport <see cref="IOException"/> failures are the
/// only retryable outcomes. Malformed, oversized, protocol-invalid, identity-substituted, or
/// lifetime-invalid envelopes fail closed immediately.
///
/// The envelope lifecycle metadata is still transport-visible at this point and therefore is not
/// treated as authenticated payload state. It is used only to reject clearly unusable envelopes and
/// to provide a stricter upper bound to the subsequent signed dispatch-binding wait. Cryptographic
/// authentication remains the responsibility of <see cref="ResearchWorkItemProtector.Unprotect"/>.
/// </summary>
public sealed class WorkerProtectedResearchWorkItemLoader
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(15);

    private readonly IProtectedResearchWorkItemTransport _transport;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _maxWait;

    public WorkerProtectedResearchWorkItemLoader(
        IProtectedResearchWorkItemTransport transport,
        TimeSpan? pollInterval = null,
        TimeSpan? maxWait = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        _maxWait = maxWait ?? TimeSpan.FromSeconds(30);

        if (_pollInterval < TimeSpan.FromMilliseconds(100) || _pollInterval > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pollInterval),
                "Work-item poll interval must be between 100 ms and 30 seconds.");
        }

        if (_maxWait < _pollInterval || _maxWait > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxWait),
                "Work-item bootstrap wait must cover at least one poll and be no more than 5 minutes.");
        }
    }

    public async Task<ProtectedResearchWorkItemEnvelope> LoadAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deadline = startedAt.Add(_maxWait);
        var retryDelay = _pollInterval;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTimeOffset.UtcNow >= deadline)
                throw CreateTimeout();

            ProtectedResearchWorkItemEnvelope? envelope;
            try
            {
                envelope = await _transport
                    .GetAsync(opaqueWorkItemId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (IOException)
            {
                // Mounted Object Storage may briefly report I/O failures while a mount reconnects
                // or propagates a newly written object. Cancellation wins over retry, and the same
                // absolute bootstrap deadline applies to every attempt.
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow >= deadline)
                    throw CreateTimeout();

                retryDelay = await DelayBeforeRetryAsync(
                    retryDelay,
                    deadline,
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            var observedAt = DateTimeOffset.UtcNow;
            if (observedAt >= deadline)
                throw CreateTimeout();

            if (envelope is not null)
            {
                ValidateTransportVisibleEnvelope(envelope, opaqueWorkItemId, observedAt);
                return envelope;
            }

            // A create-once work item may not yet be visible through the mounted transport even
            // though the client has durably reserved it. Treat absence as propagation only within
            // the bounded bootstrap window; it is never an unbounded availability assumption.
            retryDelay = await DelayBeforeRetryAsync(
                retryDelay,
                deadline,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateTransportVisibleEnvelope(
        ProtectedResearchWorkItemEnvelope envelope,
        string expectedOpaqueWorkItemId,
        DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!string.Equals(envelope.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Unsupported remote research work-item protocol version.");

        if (!string.Equals(envelope.OpaqueWorkItemId, expectedOpaqueWorkItemId, StringComparison.Ordinal))
        {
            throw new CryptographicException(
                "Protected remote research work item does not match the requested opaque work-item id.");
        }

        if (envelope.ExpiresAt <= envelope.CreatedAt
            || envelope.ExpiresAt - envelope.CreatedAt > ResearchWorkItemProtector.MaxLifetime)
        {
            throw new InvalidOperationException("Protected remote research work-item lifetime is invalid.");
        }

        if (envelope.ExpiresAt <= observedAt)
            throw new TimeoutException("Protected remote research work item expired before worker bootstrap completed.");
    }

    private static async Task<TimeSpan> DelayBeforeRetryAsync(
        TimeSpan retryDelay,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now >= deadline)
            return retryDelay;

        var remaining = deadline - now;
        var delay = remaining < retryDelay ? remaining : retryDelay;
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        return NextBackoff(retryDelay);
    }

    private static TimeSpan NextBackoff(TimeSpan current)
    {
        if (current >= MaxBackoff)
            return MaxBackoff;

        var doubledTicks = current.Ticks > MaxBackoff.Ticks / 2
            ? MaxBackoff.Ticks
            : current.Ticks * 2;
        return TimeSpan.FromTicks(Math.Min(doubledTicks, MaxBackoff.Ticks));
    }

    private static TimeoutException CreateTimeout() =>
        new("Protected remote research work item did not become available before the worker bootstrap deadline.");
}
