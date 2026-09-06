using System.Text.Json;

namespace Nvidea.Core.Jobs;

public sealed class JsonAgentJobStore : IAgentJobStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonAgentJobStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Job store path is required.", nameof(path));

        _path = Path.GetFullPath(path);
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
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(records, JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temp, _path, true);
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

        var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<AgentJobRecord>>(json, JsonOptions) ?? new List<AgentJobRecord>();
    }
}
