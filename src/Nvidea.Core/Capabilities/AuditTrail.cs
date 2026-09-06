using System.Text.Json;

namespace Nvidea.Core.Capabilities;

public sealed record AuditEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string CapabilityId,
    string ActionId,
    string EventType,
    CapabilityRiskLevel Risk,
    bool Allowed,
    bool Approved,
    string ApprovalScope,
    string Summary,
    IReadOnlyDictionary<string, string>? Metadata = null);

public interface IAuditTrail
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default);
}

public sealed class JsonLinesAuditTrail : IAuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLinesAuditTrail(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit path is required.", nameof(path));

        _path = Path.GetFullPath(path);
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (auditEvent.EventId == Guid.Empty)
            throw new ArgumentException("Audit events require a non-empty event id.", nameof(auditEvent));
        if (string.IsNullOrWhiteSpace(auditEvent.CapabilityId) || string.IsNullOrWhiteSpace(auditEvent.ActionId))
            throw new ArgumentException("Audit events require capability and action identifiers.", nameof(auditEvent));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var existing = File.Exists(_path)
                ? await File.ReadAllLinesAsync(_path, cancellationToken).ConfigureAwait(false)
                : Array.Empty<string>();

            foreach (var line in existing.Where(static x => !string.IsNullOrWhiteSpace(x)))
            {
                var parsed = JsonSerializer.Deserialize<AuditEvent>(line, JsonOptions);
                if (parsed?.EventId == auditEvent.EventId)
                    throw new InvalidOperationException($"Audit event '{auditEvent.EventId}' already exists; audit records are append-only.");
            }

            var serialized = JsonSerializer.Serialize(auditEvent, JsonOptions);
            await File.AppendAllTextAsync(_path, serialized + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return Array.Empty<AuditEvent>();

        var lines = await File.ReadAllLinesAsync(_path, cancellationToken).ConfigureAwait(false);
        var result = new List<AuditEvent>(lines.Length);
        foreach (var line in lines.Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            var parsed = JsonSerializer.Deserialize<AuditEvent>(line, JsonOptions);
            if (parsed is not null)
                result.Add(parsed);
        }

        return result;
    }
}
