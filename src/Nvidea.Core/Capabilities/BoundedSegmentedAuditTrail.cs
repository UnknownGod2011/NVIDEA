using System.Collections.Concurrent;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Capabilities;

/// <summary>
/// Logical payload limits for the mutable active audit segment. Archived bytes are governed by
/// <see cref="AuditRetentionPolicy"/>; these limits prevent a single hostile or accidental audit
/// event from inflating the current segment before count-based rotation can occur.
/// </summary>
public sealed record AuditPayloadPolicy(
    int MaxEventPayloadBytes = 64 * 1024,
    long MaxActiveSegmentPayloadBytes = 4L * 1024L * 1024L)
{
    public static AuditPayloadPolicy Default { get; } = new();

    internal void Validate()
    {
        if (MaxEventPayloadBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxEventPayloadBytes), "Audit event payload limit must be positive.");
        if (MaxActiveSegmentPayloadBytes < MaxEventPayloadBytes)
            throw new ArgumentOutOfRangeException(nameof(MaxActiveSegmentPayloadBytes), "Active-segment payload limit must be at least the single-event limit.");
    }
}

/// <summary>
/// Production-safe facade over <see cref="SegmentedAuditTrail"/> that adds deterministic logical
/// payload ceilings without weakening its protected hash chain, rollover, retention, or crash
/// recovery semantics. Limits are evaluated against UTF-8 JSON for <see cref="AuditEvent"/> before
/// any protected audit side effect occurs; encryption/base64/file-format overhead therefore cannot
/// be attacker-controlled without first passing the smaller logical payload ceiling.
/// </summary>
public sealed class BoundedSegmentedAuditTrail : IAuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SharedGates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly SegmentedAuditTrail _inner;
    private readonly AuditRetentionStatusReader _statusReader;
    private readonly int _maxEventsPerSegment;
    private readonly AuditPayloadPolicy _payloadPolicy;
    private readonly SemaphoreSlim _gate;

    public BoundedSegmentedAuditTrail(
        string path,
        int maxEventsPerSegment = 1_000,
        ILocalStateProtector? protector = null,
        AuditRetentionPolicy? retention = null,
        AuditPayloadPolicy? payloadPolicy = null)
    {
        if (maxEventsPerSegment < 2)
            throw new ArgumentOutOfRangeException(nameof(maxEventsPerSegment), "Audit segments must allow at least two events.");

        var effectiveRetention = retention ?? AuditRetentionPolicy.Default;
        effectiveRetention.Validate();
        _payloadPolicy = payloadPolicy ?? AuditPayloadPolicy.Default;
        _payloadPolicy.Validate();
        _maxEventsPerSegment = maxEventsPerSegment;
        _inner = new SegmentedAuditTrail(path, maxEventsPerSegment, protector, effectiveRetention);
        _statusReader = new AuditRetentionStatusReader(path, protector, effectiveRetention);
        _gate = GetSynchronizationGate(path);
    }

    internal static SemaphoreSlim GetSynchronizationGate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit path is required.", nameof(path));

        // Local-state status readers and the browser runtime may intentionally compose separate
        // objects over the same protected audit path. Serialize them on one process-wide gate so a
        // read-only status request cannot race an append/rotation/prune performed by the browser.
        var synchronizationPath = Path.GetFullPath(path);
        return SharedGates.GetOrAdd(synchronizationPath, static _ => new SemaphoreSlim(1, 1));
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var eventBytes = MeasurePayloadBytes(auditEvent);
        if (eventBytes > _payloadPolicy.MaxEventPayloadBytes)
        {
            throw new InvalidOperationException(
                $"Audit event logical payload is {eventBytes} bytes, exceeding the {_payloadPolicy.MaxEventPayloadBytes}-byte limit.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var retained = await _inner.ReadAllAsync(cancellationToken).ConfigureAwait(false);

            // Retention only prunes complete archived segments, each containing exactly
            // _maxEventsPerSegment records. Therefore retained-count modulo the segment size still
            // identifies the active segment after pruning. A zero remainder with retained events
            // means the active segment is full and the next append will rotate into an empty one.
            var remainder = retained.Count % _maxEventsPerSegment;
            var activeCount = retained.Count == 0
                ? 0
                : remainder == 0 ? _maxEventsPerSegment : remainder;

            long activePayloadBytes = 0;
            if (activeCount < _maxEventsPerSegment)
            {
                for (var i = retained.Count - activeCount; i < retained.Count; i++)
                    activePayloadBytes = checked(activePayloadBytes + MeasurePayloadBytes(retained[i]));
            }

            var nextActiveBytes = checked(activePayloadBytes + eventBytes);
            if (nextActiveBytes > _payloadPolicy.MaxActiveSegmentPayloadBytes)
            {
                throw new InvalidOperationException(
                    $"Audit active-segment logical payload would reach {nextActiveBytes} bytes, exceeding the {_payloadPolicy.MaxActiveSegmentPayloadBytes}-byte limit.");
            }

            await _inner.AppendAsync(auditEvent, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _inner.ReadAllAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Returns privacy-safe storage and retention telemetry after forcing the inner trail through
    /// its normal crash-recovery and retention-validation path. Audit event payloads are never
    /// returned by this API.
    /// </summary>
    public async Task<AuditRetentionStatus> GetRetentionStatusAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // ReadAllAsync validates protected anchors and completes pending retention cleanup.
            // Discard its payload result; the status reader only observes validated accounting state.
            _ = await _inner.ReadAllAsync(cancellationToken).ConfigureAwait(false);
            return await _statusReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static int MeasurePayloadBytes(AuditEvent auditEvent) =>
        JsonSerializer.SerializeToUtf8Bytes(auditEvent, JsonOptions).Length;
}
