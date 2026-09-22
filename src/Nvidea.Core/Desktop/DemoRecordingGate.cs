namespace Nvidea.Core.Desktop;

/// <summary>
/// Fail-closed, payload-free gate used before recording judge evidence. The required sequence is
/// supplied by the validated demo contract; only production session evidence can satisfy it.
/// </summary>
public static class DemoRecordingGate
{
    public static DemoRecordingGateResult Evaluate(
        SessionEvidenceSnapshot snapshot,
        IReadOnlyList<SessionEvidenceKind> requiredSequence)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(requiredSequence);

        if (requiredSequence.Count == 0)
            return new(false, Array.Empty<SessionEvidenceKind>(), "Demo contract contains no required production milestones.");

        if (requiredSequence.Any(kind => !Enum.IsDefined(kind)) || requiredSequence.Distinct().Count() != requiredSequence.Count)
            return new(false, Array.Empty<SessionEvidenceKind>(), "Demo contract milestone sequence is invalid or contains duplicates.");

        var observed = snapshot.Entries.OrderBy(entry => entry.FirstObservedAt).ThenBy(entry => entry.Kind).ToArray();
        var missing = requiredSequence.Where(kind => observed.All(entry => entry.Kind != kind)).ToArray();
        if (missing.Length > 0)
            return new(false, missing, $"Missing {missing.Length} required production milestone(s).");

        var cursor = 0;
        foreach (var entry in observed)
        {
            if (entry.Kind == requiredSequence[cursor] && ++cursor == requiredSequence.Count)
                return new(true, Array.Empty<SessionEvidenceKind>(), "All required production milestones were observed in contract order.");
        }

        return new(false, Array.Empty<SessionEvidenceKind>(), "Required production milestones were observed out of contract order. Start a fresh demo evidence session and rerun the beats.");
    }
}

public sealed record DemoRecordingGateResult(
    bool CanRecord,
    IReadOnlyList<SessionEvidenceKind> MissingMilestones,
    string Reason);
