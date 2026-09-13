namespace Nvidea.Core.Desktop;

/// <summary>
/// Trust boundary for descriptive browser-goal evidence that may originate from model output,
/// websites, browser drivers, or tool/runtime diagnostics. The projection is deliberately
/// presentation/planner-context only: it must never change approval scope, job identity,
/// lifecycle state, or replay authority.
///
/// Pending browser actions are intentionally discarded at this boundary. Crash recovery and
/// approval resume are keyed by the durable child job id plus exact approval scope; retaining the
/// original action would unnecessarily persist typed values, upload paths, locators, rationale,
/// and postcondition material after the child job has become the source of truth.
/// </summary>
public static class BrowserGoalEvidenceTrust
{
    public const int MaxSessionDetailCharacters = 512;
    public const int MaxVerificationDetailCharacters = 320;

    public static BrowserGoalSession ProjectForPersistence(BrowserGoalSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var projectedSteps = (session.VerifiedSteps ?? Array.Empty<BrowserGoalVerifiedStep>())
            .Select(ProjectVerifiedStep)
            .ToArray();

        return session with
        {
            Detail = CanonicalizeOptional(
                session.Detail,
                MaxSessionDetailCharacters,
                "Browser goal status detail omitted."),
            // Recovery authority lives in PendingJobId/PendingExactScope and the durable child job.
            // Never retain action payloads (typed values, upload paths, etc.) in parent goal state.
            PendingAction = null,
            VerifiedSteps = projectedSteps
        };
    }

    public static BrowserGoalVerifiedStep ProjectVerifiedStep(BrowserGoalVerifiedStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return step with
        {
            UrlBefore = ProjectEvidenceUri(step.UrlBefore),
            UrlAfter = ProjectEvidenceUri(step.UrlAfter),
            VerificationDetail = CanonicalizeOptional(
                step.VerificationDetail,
                MaxVerificationDetailCharacters,
                "Browser verification detail omitted.")
        };
    }

    private static string? CanonicalizeOptional(string? value, int maxCharacters, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DesktopDisplayTextTrust.Canonicalize(value, maxCharacters, fallback);
    }

    private static Uri ProjectEvidenceUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
            return new Uri("about:blank");

        var builder = new UriBuilder(uri.Scheme, uri.IdnHost)
        {
            Port = uri.IsDefaultPort ? -1 : uri.Port,
            Path = uri.AbsolutePath,
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty
        };
        return builder.Uri;
    }
}
