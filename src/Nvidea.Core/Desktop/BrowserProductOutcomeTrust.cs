using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Product-facing trust boundary for browser outcomes. The privileged browser host may retain
/// detailed diagnostics for durable recovery and local debugging, but product/UI/plugin callers
/// receive only fixed NVIDEA-authored status text plus bounded presentation-only approval fields.
/// This projection never changes approval scope or browser authority.
/// </summary>
internal static class BrowserProductOutcomeTrust
{
    public static BrowserJobOutcome Project(BrowserJobOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        var approval = outcome.Approval is null
            ? null
            : ProjectApproval(outcome.Approval);

        var message = outcome.State switch
        {
            AgentJobState.WaitingForApproval =>
                "Browser action is paused and has not executed. Explicit one-time approval is required.",
            AgentJobState.Completed =>
                "Browser action executed or was crash-reconciled and its intended post-action state was verified.",
            AgentJobState.Cancelled =>
                "Browser action was cancelled.",
            AgentJobState.Failed =>
                "Browser action failed safely. Review local diagnostics or retry from a trusted product flow.",
            AgentJobState.RetryScheduled =>
                "Browser action failed safely and is eligible for a bounded retry.",
            AgentJobState.Pending =>
                "Browser action is durably created and has not executed yet.",
            AgentJobState.Running =>
                "Browser action has an ambiguous in-flight checkpoint and will not be replayed automatically.",
            _ =>
                "Browser action is pending."
        };

        return outcome with
        {
            Message = message,
            Approval = approval
        };
    }

    private static BrowserApprovalPrompt ProjectApproval(BrowserApprovalPrompt prompt)
    {
        var summary = DesktopDisplayTextTrust.Canonicalize(
            prompt.Summary,
            DesktopDisplayTextTrust.MaxApprovalSummaryCharacters,
            "Browser action requires explicit one-time approval.");

        var target = ProjectTarget(prompt.Target);

        return prompt with
        {
            Summary = summary,
            Target = target
        };
    }

    private static string ProjectTarget(string? target)
    {
        const string fallback = "current browser context";
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
            return DesktopDisplayTextTrust.ProjectNavigationTarget(uri, fallback);

        return DesktopDisplayTextTrust.Canonicalize(
            target,
            DesktopDisplayTextTrust.MaxApprovalTargetCharacters,
            fallback);
    }
}
