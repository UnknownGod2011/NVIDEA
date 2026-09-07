using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Jobs;

public sealed class JsonAgentJobStore : IAgentJobStore
{
    private const string ProtectionPurpose = "agent-jobs-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonAgentJobStore(string path, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Job store path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var all = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(x => x.JobId == jobId);
    }

    public async Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.JobId == Guid.Empty)
            throw new ArgumentException("Jobs require a non-empty id.", nameof(record));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = records.FindIndex(x => x.JobId == record.JobId);
            if (index >= 0)
                records[index] = record;
            else
                records.Add(record);

            records.Sort(static (a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<AgentJobRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<AgentJobRecord>> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return Array.Empty<AgentJobRecord>();

        var persisted = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
        LocalStatePayload payload;
        if (_protector is not null)
            payload = LocalStateEnvelope.Decode(persisted, _protector, ProtectionPurpose);
        else if (LocalStateEnvelope.HasProtectedHeader(persisted))
            throw new InvalidDataException("Job store is protected but no local-state protector was configured.");
        else
            payload = new LocalStatePayload(persisted, false);

        List<AgentJobRecord> records;
        try
        {
            records = JsonSerializer.Deserialize<List<AgentJobRecord>>(payload.Plaintext, JsonOptions) ?? new List<AgentJobRecord>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Job store contains invalid data.", ex);
        }

        var changed = _protector is not null && !payload.WasProtected;
        for (var i = 0; i < records.Count; i++)
        {
            var migrated = BrowserActionCheckpointMigrationService.MigrateRecord(records[i]);
            if (migrated.Status is BrowserActionCheckpointRecordMigrationStatus.Migrated
                or BrowserActionCheckpointRecordMigrationStatus.Quarantined)
            {
                records[i] = migrated.Record;
                changed = true;
            }
        }

        if (changed)
        {
            records.Sort(static (a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
        }

        return records;
    }

    private async Task PersistUnlockedAsync(
        IReadOnlyList<AgentJobRecord> records,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(records, JsonOptions);
        var persisted = _protector is null
            ? plaintext
            : LocalStateEnvelope.Encode(plaintext, _protector, ProtectionPurpose);

        var temp = _path + ".tmp";
        await File.WriteAllBytesAsync(temp, persisted, cancellationToken).ConfigureAwait(false);
        File.Move(temp, _path, true);
    }
}
