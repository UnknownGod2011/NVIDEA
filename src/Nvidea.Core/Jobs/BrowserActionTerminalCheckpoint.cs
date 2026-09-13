namespace Nvidea.Core.Jobs;

/// <summary>
/// Removes executable browser-action material once a child job becomes terminal.
/// Retryable and in-flight records intentionally keep their original checkpoint;
/// completed browser actions already replace it with a verified receipt.
/// </summary>
internal static class BrowserActionTerminalCheckpoint
{
    internal const string JobType = "browser.action";
    internal const string ScrubbedStep = "browser.action.terminal.scrubbed";
    internal const string ScrubbedPayload = "{\"actionPayloadRemoved\":true}";

    internal static AgentJobRecord ScrubIfTerminal(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!string.Equals(record.Definition.JobType, JobType, StringComparison.OrdinalIgnoreCase)
            || record.State is not (AgentJobState.Failed or AgentJobState.Cancelled))
        {
            return record;
        }

        var checkpoint = new AgentJobCheckpoint(
            ScrubbedStep,
            ScrubbedPayload,
            DateTimeOffset.UtcNow);

        return record with
        {
            Checkpoint = checkpoint,
            // Exact approval scope is needed only while execution can still resume.
            // A terminal browser child cannot consume it, so retaining it only
            // preserves potentially sensitive typed values/targets unnecessarily.
            ApprovalScope = null
        };
    }
}
