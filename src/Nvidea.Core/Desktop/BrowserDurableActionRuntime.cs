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

    internal static BrowserDurableActionRuntime CreateWindows(
        ResumableJobOrchestrator jobs,
        string stateDirectory)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        return new BrowserDurableActionRuntime(
            jobs,
            BrowserVerificationRuntime.CreateWindows(stateDirectory));
    }

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
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
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
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        var publication = await _verification.AdvanceAsync(
            token => _jobs.RunNextStepAsync(jobId, token),
            cancellationToken).ConfigureAwait(false);

        // The authoritative durable job is always returned, even when observational judge-evidence
        // publication fails. This prevents a completed side effect from being mistaken as retryable.
        return publication.AuthoritativeJob;
    }

    /// <summary>
    /// Resumes an exact human-approved durable action and advances it through the same authoritative
    /// verification boundary as ordinary execution. Keeping resume + execute in this facade prevents
    /// the host from accidentally bypassing terminal evidence publication after approval.
    /// </summary>
    internal async Task<AgentJobRecord> ResumeAfterApprovalAndRunNextStepAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));

        var publication = await _verification.AdvanceAsync(
            async token =>
            {
                await _jobs.ResumeAfterApprovalAsync(jobId, exactScope, token).ConfigureAwait(false);
                return await _jobs.RunNextStepAsync(jobId, token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return publication.AuthoritativeJob;
    }

    /// <summary>
    /// Non-execution approval rearm remains inside the least-authority facade so production host code
    /// does not need the raw orchestrator merely to rotate an expired one-time approval scope.
    /// Rearming cannot publish green evidence because no browser action executes here.
    /// </summary>
    internal Task<AgentJobRecord> RearmApprovalAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));

        return _jobs.RearmApprovalAsync(jobId, exactScope, cancellationToken);
    }

    /// <summary>
    /// Cancellation is a non-execution transition and intentionally does not publish verification.
    /// Keeping it here lets the browser host shed raw orchestrator authority after composition lands.
    /// </summary>
    internal Task<AgentJobRecord> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        return _jobs.CancelAsync(jobId, cancellationToken);
    }

    /// <summary>
    /// Reads durable browser state without exposing the orchestrator. This is intentionally a
    /// non-execution operation and therefore cannot create or refresh judge verification evidence.
    /// </summary>
    internal Task<AgentJobRecord?> GetAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        return _jobs.GetAsync(jobId, cancellationToken);
    }

    /// <summary>
    /// Completes a crash-ambiguous running job only after the trusted host independently verifies
    /// post-state. Legacy ambiguous reconciliation intentionally does NOT publish judge verification:
    /// its checkpoint lacks the normal structural durable evidence produced by last-mile execution.
    /// Keeping this operation in the facade lets the host avoid retaining raw orchestrator authority
    /// without weakening the fail-closed evidence model.
    /// </summary>
    internal Task<AgentJobRecord> CompleteAmbiguousRunningWithoutVerificationAsync(
        Guid jobId,
        AgentJobCheckpoint verifiedCheckpoint,
        string detail,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        ArgumentNullException.ThrowIfNull(verifiedCheckpoint);
        if (string.IsNullOrWhiteSpace(detail))
            throw new ArgumentException("Reconciliation detail is required.", nameof(detail));

        return _jobs.CompleteAmbiguousRunningAsync(jobId, verifiedCheckpoint, detail, cancellationToken);
    }

    /// <summary>
    /// Payload-free read surface for trusted desktop/judge UI. No receipt, publisher, approval
    /// authority, browser payload, URL, locator, or typed value crosses this boundary.
    /// </summary>
    internal Task<DesktopBrowserVerificationPresentation> ReadVerificationPresentationAsync(
        CancellationToken cancellationToken = default) =>
        _verificationRuntime.ReadPresentationAsync(cancellationToken);
}
