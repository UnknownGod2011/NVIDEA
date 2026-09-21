using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Single host-facing durable browser action boundary. Browser hosts should use this facade rather
/// than calling the orchestrator directly so stale judge evidence is cleared before admission and
/// terminal evidence is observed only after the orchestrator returns its authoritative record.
/// </summary>
internal sealed class BrowserDurableActionRuntime
{
    private readonly ResumableJobOrchestrator _jobs;
    private readonly BrowserVerificationActionLifecycle _verification;

    internal BrowserDurableActionRuntime(
        ResumableJobOrchestrator jobs,
        BrowserVerificationActionLifecycle verification)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _verification = verification ?? throw new ArgumentNullException(nameof(verification));
    }

    internal Task<AgentJobRecord> CreateAsync(
        Guid jobId,
        AgentJobDefinition definition,
        AgentJobCheckpoint initialCheckpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(initialCheckpoint);

        return _verification.AdmitAsync(
            token => _jobs.CreateAsync(jobId, definition, initialCheckpoint, token),
            cancellationToken);
    }

    internal async Task<AgentJobRecord> RunNextStepAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var publication = await _verification.AdvanceAsync(
            token => _jobs.RunNextStepAsync(jobId, token),
            cancellationToken).ConfigureAwait(false);

        // The authoritative durable job is always returned, even when observational judge-evidence
        // publication fails. This prevents a completed side effect from being mistaken as retryable.
        return publication.AuthoritativeJob;
    }
}
