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
        BrowserVerificationRuntime verificationRuntime,
        IBrowserVerificationLifecycleObserver? lifecycleObserver = null)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _verificationRuntime = verificationRuntime ?? throw new ArgumentNullException(nameof(verificationRuntime));
        _verification = new BrowserVerificationActionLifecycle(_verificationRuntime.Publication, lifecycleObserver);
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

    /// <summary>
    /// Test-only composition seam for deterministic race coverage. The observer is payload-free and
    /// remains internal; production factories intentionally provide no observer. Keeping this overload
    /// separate makes it difficult for product composition to accidentally install synchronization or
    /// inspection callbacks in the browser action authority path.
    /// </summary>
    internal static BrowserDurableActionRuntime CreateForTesting(
        ResumableJobOrchestrator jobs,
        BrowserVerificationRuntime verificationRuntime,
        IBrowserVerificationLifecycleObserver lifecycleObserver)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(verificationRuntime);
        ArgumentNullException.ThrowIfNull(lifecycleObserver);
        return new BrowserDurableActionRuntime(jobs, verificationRuntime, lifecycleObserver);
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
                return publication.AuthoritativeJob;
            },
            cancellationToken);
    }

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
                await _verificationRuntime.Publication.BeginActionAsync(token).ConfigureAwait(false);
                return await _jobs
                    .CompleteAmbiguousRunningAsync(jobId, verifiedCheckpoint, detail, token)
                    .ConfigureAwait(false);
            },
            cancellationToken);
    }

    internal Task<DesktopBrowserVerificationPresentation> ReadVerificationPresentationAsync(
        CancellationToken cancellationToken = default) =>
        SerializeTransitionAsync(
            token => _verificationRuntime.ReadPresentationAsync(token),
            cancellationToken);

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
