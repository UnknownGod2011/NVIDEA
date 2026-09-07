using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nvidea.Core.Desktop;

public interface IBrowserGoalSessionStore
{
    Task<BrowserGoalSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrowserGoalSession>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(BrowserGoalSession session, CancellationToken cancellationToken = default);
}

/// <summary>
/// Durable browser-goal state. Only descriptive/non-authorizing state is persisted:
/// no approval grants, grant ids, bearer tokens, credentials, or typed browser values.
/// PendingAction is deliberately stripped because its Value may contain private user data.
/// </summary>
public sealed class JsonBrowserGoalSessionStore : IBrowserGoalSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonBrowserGoalSessionStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Browser goal session store path is required.", nameof(path));
        _path = Path.GetFullPath(path);
    }

    public async Task<BrowserGoalSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Browser goal session id is required.", nameof(sessionId));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
            return records.FirstOrDefault(x => x.SessionId == sessionId)?.ToSession();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<BrowserGoalSession>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
            return records.Select(static record => record.ToSession()).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(BrowserGoalSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SessionId == Guid.Empty)
            throw new ArgumentException("Browser goal session id is required.", nameof(session));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var persisted = PersistedBrowserGoalSession.FromSession(session);
            var index = records.FindIndex(x => x.SessionId == session.SessionId);
            if (index >= 0)
                records[index] = persisted;
            else
                records.Add(persisted);

            records.Sort(static (a, b) => a.StartedAt.CompareTo(b.StartedAt));
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(records, JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<PersistedBrowserGoalSession>> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return Array.Empty<PersistedBrowserGoalSession>();

        var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<List<PersistedBrowserGoalSession>>(json, JsonOptions)
                ?? new List<PersistedBrowserGoalSession>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Browser goal session store contains invalid JSON.", ex);
        }
    }

    private sealed record PersistedBrowserGoalSession(
        Guid SessionId,
        string Goal,
        int ActionCount,
        int MaxActions,
        int PlannerTurnCount,
        int MaxPlannerTurns,
        long PlannerContextCharacters,
        long MaxPlannerContextCharacters,
        int MaxWallClockSeconds,
        BrowserGoalStatus Status,
        string? Detail,
        Guid? PendingJobId,
        string? PendingExactScope,
        DateTimeOffset StartedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<BrowserGoalVerifiedStep> VerifiedSteps)
    {
        public static PersistedBrowserGoalSession FromSession(BrowserGoalSession session) => new(
            session.SessionId,
            session.Goal,
            session.ActionCount,
            session.MaxActions,
            session.PlannerTurnCount,
            session.MaxPlannerTurns,
            session.PlannerContextCharacters,
            session.MaxPlannerContextCharacters,
            session.MaxWallClockSeconds,
            session.Status,
            session.Detail,
            session.PendingJobId,
            session.PendingExactScope,
            session.StartedAt,
            session.UpdatedAt,
            session.VerifiedSteps ?? Array.Empty<BrowserGoalVerifiedStep>());

        public BrowserGoalSession ToSession() => new(
            SessionId,
            Goal,
            ActionCount,
            MaxActions,
            Status,
            Detail,
            PendingJobId,
            PendingExactScope,
            null,
            StartedAt,
            UpdatedAt,
            PlannerTurnCount,
            MaxPlannerTurns,
            PlannerContextCharacters,
            MaxPlannerContextCharacters,
            MaxWallClockSeconds,
            VerifiedSteps ?? Array.Empty<BrowserGoalVerifiedStep>());
    }
}
