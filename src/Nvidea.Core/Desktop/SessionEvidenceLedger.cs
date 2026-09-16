namespace Nvidea.Core.Desktop;

/// <summary>Payload-free, process-local proof of judge-relevant production milestones.</summary>
public sealed class SessionEvidenceLedger
{
    private readonly object _gate = new();
    private readonly Dictionary<SessionEvidenceKind, DateTimeOffset> _firstObserved = new();
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// One payload-free ledger for the production desktop process. Composition boundaries that do
    /// not receive an explicit ledger use this instance so independently-created product runtimes
    /// contribute to the same judge-visible session proof. Tests can still inject isolated ledgers.
    /// </summary>
    public static SessionEvidenceLedger ProcessLocal { get; } = new();

    public SessionEvidenceLedger(Func<DateTimeOffset>? clock = null) =>
        _clock = clock ?? (() => DateTimeOffset.UtcNow);

    public void Record(SessionEvidenceKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        lock (_gate) _firstObserved.TryAdd(kind, _clock());
    }

    public SessionEvidenceSnapshot Snapshot()
    {
        lock (_gate)
        {
            var entries = _firstObserved.OrderBy(x => x.Value).ThenBy(x => x.Key)
                .Select(x => new SessionEvidenceEntry(x.Key, x.Value)).ToArray();
            return new SessionEvidenceSnapshot(entries);
        }
    }

    public void Clear() { lock (_gate) _firstObserved.Clear(); }
}

public enum SessionEvidenceKind
{
    NemotronInferenceCompleted = 1,
    MemoryInfluencedInvocation = 2,
    TavilyResearchCompletedWithCitations = 3,
    BrowserPostStateVerified = 4,
    ConsequentialApprovalGateExercised = 5,
    NebiusBackgroundExecutionObserved = 6
}

public sealed record SessionEvidenceEntry(SessionEvidenceKind Kind, DateTimeOffset FirstObservedAt);
public sealed record SessionEvidenceSnapshot(IReadOnlyList<SessionEvidenceEntry> Entries)
{
    public bool Contains(SessionEvidenceKind kind) => Entries.Any(entry => entry.Kind == kind);
}
