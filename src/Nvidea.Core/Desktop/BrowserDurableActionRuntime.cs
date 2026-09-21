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
    private readonly SemaphoreSlim _transitionGate = new(1, 1);

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

        return SerializeTransitionAsync(
            token => _verification.AdmitAsync(
                innerToken => _jobs.CreateAsync(jobId, definition, initialCheckpoint, innerToken),
                token),
            cancellationToken);
    }

    internal Task<AgentJobRecord> RunNextStepAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        return SerializeTransitionAsync(
            async token =>
            {
                var publication = await _verification.AdvanceAsync(
                    innerToken => _jobs.RunNextStepAsync(jobId, innerToken),
                    token).ConfigureAwait(false);

                // The authoritative durable job is always returned, even when observational judge-evidence
                // publication fails. This prevents a completed side effect from being mistaken as retryable.
                return publication.AuthoritativeJob;
            },
            cancellationToken);
    }

    /// <summary>
    /// Resumes an exact human-approved durable action and advances it through the same authoritative
    /// verification boundary as ordinary execution. Keeping resume + execute in this facade prevents
    /// the host from accidentally bypassing terminal evidence publication after approval.
    /// </summary>
    internal Task<AgentJobRecord> ResumeAfterApprovalAndRunNextStepAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));

        return SerializeTransitionAsync(
            async token =>
            {
                var publication = await _verification.AdvanceAsync(
                    async innerToken =>
                    {
                        await _jobs.ResumeAfterApprovalAsync(jobId, exactScope, innerToken).ConfigureAwait(false);
                        return await _jobs.RunNextStepAsync(jobId, innerToken).ConfigureAwait(false);
                    },
                    token).ConfigureAwait(false);

                return publication.AuthoritativeJob;
            },
            cancellationToken);
    }

    /// <summary>
    /// Non-execution approval rearm remains inside the least-authority facade so production host code
    /// does not need the raw orchestrator merely to rotate an expired one-time approval scope.
    /// Rearming cannot publish green evidence because no browser action executes here. It also clears
    /// any existing protected receipt first so migrated/pre-verification state cannot leave stale green
    /// evidence visible while the durable action is explicitly waiting for fresh human authorization.
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

        return SerializeTransitionAsync(
            async token =>
            {
                await _verificationRuntime.Publication.BeginActionAsync(token).ConfigureAwait(false);
                return await _jobs.RearmApprovalAsync(jobId, exactScope, token).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>
    /// Cancellation is a non-execution transition and intentionally does not publish verification.
    /// Existing protected verification is invalidated before the durable cancellation transition so
    /// cancelled or migrated work cannot continue presenting a stale successful action to judge UI.
    /// If invalidation fails, cancellation is not committed and the caller receives the storage error.
    /// </summary>
    internal Task<AgentJobRecord> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        return SerializeTransitionAsync(
            async token =>
            {
                await _verificationRuntime.Publication.BeginActionAsync(token).ConfigureAwait(false);
                return await _jobs.CancelAsync(jobId, token).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>
    /// Completes a crash-ambiguous running job only after the trusted host independently verifies
    /// post-state. Legacy ambiguous reconciliation intentionally does NOT publish judge verification:
    /// its checkpoint lacks the normal structural durable evidence produced by last-mile execution.
    /// Before mutating durable state, this path clears any existing browser-verification receipt so a
    /// stale green receipt can never survive a crash-reconciled completion. If that clear fails, the
    /// reconciliation is not committed and the job remains fail-closed for explicit recovery.
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

        return SerializeTransitionAsync(
            async token =>
            {
                // Ambiguous recovery is deliberately not eligible for judge-green evidence. Clearing first is
                // important for migrated/pre-verification jobs where a protected receipt may predate this job's
                // admission lifecycle. Never complete durable reconciliation while stale green evidence remains.
                await _verificationRuntime.Publication.BeginActionAsync(token).ConfigureAwait(false);

                return await _jobs
                    .CompleteAmbiguousRunningAsync(jobId, verifiedCheckpoint, detail, token)
                    .ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>
    /// Payload-free read surface for trusted desktop/judge UI. No receipt, publisher, approval
    /// authority, browser payload, URL, locator, or typed value crosses this boundary.
    /// </summary>
    internal Task<DesktopBrowserVerificationPresentation> ReadVerificationPresentationAsync(
        CancellationToken cancellationToken = default) =>
        _verificationRuntime.ReadPresentationAsync(cancellationToken);

    /// <summary>
    /// The protected verification receipt represents the most recently admitted browser transition,
    /// so all transitions that can clear or publish it must be linearized. Without this gate, two jobs
    /// could interleave as A executes, B clears, A publishes, leaving A's stale green receipt visible
    /// even though B is the newer admitted action. Cancellation while waiting for the gate never enters
    /// the critical section; cancellation after entry is propagated to the underlying transition and
    /// the semaphore is always released.
    /// </summary>
    private async Task<T> SerializeTransitionAsync<T>(
        Func<CancellationToken, Task<T>> transition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transition);
        await _transitionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await transition(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _transitionGate.Release();
        }
    }
}
