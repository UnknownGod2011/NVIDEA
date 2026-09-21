using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Single host-facing durable browser action boundary. Browser hosts should use this facade rather
/// than calling the orchestrator directly so stale judge evidence is cleared before admission and
/// terminal evidence is observed only after the orchestrator returns its authoritative record.
/// The same instance also owns the payload-free verification read path, preventing host execution
/// and judge presentation from accidentally drifting onto different protected receipt stores.
/// </summary>
internal sealed class BrowserDurableActionRuntime
{
    private readonly ResumableJobOrchestrator _jobs;
    private readonly BrowserVerificationActionLifecycle _verification;
    private readonly BrowserVerificationRuntime _verificationRuntime;

    private BrowserDurableActionRuntime(
        ResumableJobOrchestrator jobs,
        BrowserVerificationRuntime verificationRuntime)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _verificationRuntime = verificationRuntime ?? throw new ArgumentNullException(nameof(verificationRuntime));
        _verification = new BrowserVerificationActionLifecycle(_verificationRuntime.Publication);
    }

    /// <summary>
    /// Production composition root for durable browser execution + protected judge evidence.
    /// Windows CurrentUser DPAPI protection and the receipt path are created exactly once here,
    /// beneath the browser's already single-owner state directory.
    /// </summary>
    internal static BrowserDurableActionRuntime CreateWindows(
        ResumableJobOrchestrator jobs,
        string stateDirectory)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        return new BrowserDurableActionRuntime(
            jobs,
            BrowserVerificationRuntime.CreateWindows(stateDirectory));
    }

    /// <summary>
    /// Deterministic composition seam for tests. Callers still cannot obtain the publisher/store
    /// through this facade; the supplied runtime is retained only so reads share execution state.
    /// </summary>
    internal static BrowserDurableActionRuntime Create(
        ResumableJobOrchestrator jobs,
        BrowserVerificationRuntime verificationRuntime)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(verificationRuntime);
        return new BrowserDurableActionRuntime(jobs, verificationRuntime);
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

    /// <summary>
    /// Payload-free read surface for trusted desktop/judge UI. No receipt, publisher, approval
    /// authority, browser payload, URL, locator, or typed value crosses this boundary.
    /// </summary>
    internal Task<DesktopBrowserVerificationPresentation> ReadVerificationPresentationAsync(
        CancellationToken cancellationToken = default) =>
        _verificationRuntime.ReadPresentationAsync(cancellationToken);
}
