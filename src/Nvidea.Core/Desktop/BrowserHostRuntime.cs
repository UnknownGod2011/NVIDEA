using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;
using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

public sealed record BrowserHostOptions(
    Uri StartUri,
    IReadOnlySet<string>? AllowedHosts = null,
    bool Headless = false)
{
    public static BrowserHostOptions Default { get; } = new(new Uri("https://example.com"));
}

public sealed record BrowserApprovalPrompt(
    Guid JobId,
    string ExactScope,
    BrowserActionKind ActionKind,
    string Summary,
    string Target,
    DateTimeOffset RequestedAt);

public sealed record BrowserJobOutcome(
    Guid JobId,
    AgentJobState State,
    string Message,
    BrowserApprovalPrompt? Approval = null,
    BrowserGoalVerifiedStep? VerifiedStep = null)
{
    public bool IsTerminal => State is AgentJobState.Completed or AgentJobState.Failed or AgentJobState.Cancelled;
}

/// <summary>
/// Owns one local Playwright browser session and composes the existing safety,
/// capability, approval, audit and resumable-job layers into a desktop-safe API.
/// Browser writes never execute directly from UI code.
/// </summary>
public sealed class BrowserHostRuntime : IAsyncDisposable, IBrowserAmbiguousRecoveryHost
{
    public const string BrowserCapabilityId = "browser.agent";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IPlaywright _playwright;
    private readonly IBrowserContext _context;
    private readonly IBrowserDriver _driver;
    private readonly PlaywrightBrowserSessionDriver _sessionDriver;
    private readonly BrowserDownloadHandoffService _downloadHandoff;
    private readonly BrowserDownloadDiscardService _downloadDiscard;
    private readonly ScopedApprovalAuthorizer _approvals;
    private readonly IAgentJobStore _jobStore;
    private readonly ResumableJobOrchestrator _jobs;
    private bool _disposed;

    private BrowserHostRuntime(
        IPlaywright playwright,
        IBrowserContext context,
        PlaywrightBrowserSessionDriver driver,
        BrowserDownloadHandoffService downloadHandoff,
        BrowserDownloadDiscardService downloadDiscard,
        ScopedApprovalAuthorizer approvals,
        IAgentJobStore jobStore,
        ResumableJobOrchestrator jobs)
    {
        _playwright = playwright;
        _context = context;
        _driver = driver;
        _sessionDriver = driver;
        _downloadHandoff = downloadHandoff;
        _downloadDiscard = downloadDiscard;
        _approvals = approvals;
        _jobStore = jobStore;
        _jobs = jobs;
    }

    public static async Task<BrowserHostRuntime> CreateAsync(
        string stateDirectory,
        BrowserHostOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        var effective = options ?? BrowserHostOptions.Default;
        ValidateOptions(effective);
        var fullStateDirectory = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(fullStateDirectory);

        IPlaywright? playwright = null;
        IBrowserContext? context = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            playwright = await Playwright.CreateAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

            var driverOptions = new PlaywrightBrowserDriverOptions(
                effective.AllowedHosts,
                MaxObservationCharacters: 12_000,
                MaxObservedElements: 250,
                ActionTimeoutMilliseconds: 15_000);
            var session = await PersistentBrowserContextFactory.LaunchAsync(
                playwright,
                fullStateDirectory,
                effective.StartUri,
                driverOptions,
                effective.Headless,
                cancellationToken).ConfigureAwait(false);
            context = session.Context;
            var driver = session.Driver;

            var registry = new CapabilityRegistry(new[]
            {
                new CapabilityDescriptor(
                    BrowserCapabilityId,
                    "1.0.0",
                    "Browser agent",
                    new HashSet<DataPermission>
                    {
                        DataPermission.BrowserRead,
                        DataPermission.BrowserWrite,
                        DataPermission.FilesRead,
                        DataPermission.FilesWrite
                    },
                    CapabilityRiskLevel.Medium,
                    RequiresConfirmation: false,
                    "Bounded local browser automation with exact approvals for consequential writes."),
                new CapabilityDescriptor(
                    BrowserDownloadHandoffService.CapabilityId,
                    "1.0.0",
                    "Browser download handoff",
                    new HashSet<DataPermission> { DataPermission.FilesWrite },
                    CapabilityRiskLevel.High,
                    RequiresConfirmation: true,
                    "Release a verified quarantined browser download to a human-selected destination only after exact approval."),
                new CapabilityDescriptor(
                    BrowserDownloadDiscardService.CapabilityId,
                    "1.0.0",
                    "Browser download discard",
                    new HashSet<DataPermission> { DataPermission.FilesWrite },
                    CapabilityRiskLevel.High,
                    RequiresConfirmation: true,
                    "Delete one verified retained browser-download payload only after exact human approval.")
            });
            var capabilityPolicy = new CapabilityPermissionPolicy(registry);
            var approvals = new ScopedApprovalAuthorizer();
            var ephemeralApprovals = new EphemeralJobApprovalStore();
            var audit = new BoundedSegmentedAuditTrail(Path.Combine(fullStateDirectory, "audit.jsonl"));
            var downloadHandoff = new BrowserDownloadHandoffService(
                session.Downloads,
                capabilityPolicy,
                approvals,
                audit);
            var downloadDiscard = new BrowserDownloadDiscardService(
                session.Downloads,
                capabilityPolicy,
                approvals,
                audit);
            var backend = new BrowserCapabilityBackend(driver);
            var toolExecutor = new CapabilityToolExecutor(capabilityPolicy, approvals, audit, backend);
            var execution = new BrowserCapabilityExecutionService(
                BrowserCapabilityId,
                driver,
                new BrowserSafetyPolicy(),
                capabilityPolicy,
                toolExecutor,
                new ConservativeBrowserVerifier());
            var handler = new BrowserActionJobHandler(execution);
            var store = new JsonAgentJobStore(Path.Combine(fullStateDirectory, "jobs.json"));
            var orchestrator = new ResumableJobOrchestrator(
                store,
                new ConservativeJobExecutionPolicy(),
                audit,
                new[] { handler },
                approvals,
                ephemeralApprovals);

            return new BrowserHostRuntime(
                playwright,
                context,
                driver,
                downloadHandoff,
                downloadDiscard,
                approvals,
                store,
                orchestrator);
        }
        catch
        {
            if (context is not null)
                await SafeCloseAsync(context).ConfigureAwait(false);
            playwright?.Dispose();
            throw;
        }
    }

    public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _driver.ObserveAsync(cancellationToken);
    }

    public Task<BrowserSessionSnapshot> GetSessionSnapshotAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _sessionDriver.GetSessionSnapshotAsync(cancellationToken);
    }

    public Task<IReadOnlyList<BrowserDownloadRecord>> ListDownloadsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _sessionDriver.ListDownloadsAsync(cancellationToken);
    }

    public Task<BrowserDownloadHandoffPlan> PrepareDownloadHandoffAsync(
        Guid downloadId,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _downloadHandoff.PrepareAsync(downloadId, destinationDirectory, cancellationToken);
    }

    public async Task<BrowserDownloadExportReceipt> ApproveAndExportDownloadAsync(
        BrowserDownloadHandoffPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(approvedPlan);
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));
        if (!string.Equals(exactScope, approvedPlan.Decision.ApprovalScope, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Download approval scope does not match the prepared handoff.");

        var grant = _approvals.Grant(approvedPlan.Decision, TimeSpan.FromMinutes(2));
        return await _downloadHandoff.ExportAsync(approvedPlan, grant, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Prepares a non-authorizing, exact approval scope for deleting one retained quarantine payload.
    /// The returned plan is safe to display to trusted UI; no ApprovalGrant is exposed.
    /// </summary>
    public Task<BrowserDownloadDiscardPlan> PrepareDownloadDiscardAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _downloadDiscard.PrepareAsync(downloadId, cancellationToken);
    }

    /// <summary>
    /// Trusted human-confirmation boundary for quota reclamation. The exact scope shown by the UI
    /// must be echoed back before a short-lived single-use grant is created and immediately consumed.
    /// </summary>
    public async Task<BrowserDownloadDiscardReceipt> ApproveAndDiscardDownloadAsync(
        BrowserDownloadDiscardPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(approvedPlan);
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));
        if (!string.Equals(exactScope, approvedPlan.Decision.ApprovalScope, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Download discard approval scope does not match the prepared plan.");

        var grant = _approvals.Grant(approvedPlan.Decision, TimeSpan.FromMinutes(2));
        return await _downloadDiscard.DiscardAsync(approvedPlan, grant, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserJobOutcome> CreateActionAsync(
        Guid jobId,
        BrowserAction action,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        ArgumentNullException.ThrowIfNull(action);

        var permissions = BrowserCapabilityExecutionService.PermissionsFor(action.Kind);
        var observation = await _driver.ObserveAsync(cancellationToken).ConfigureAwait(false);
        var definition = new AgentJobDefinition(
            BrowserActionJobHandler.Type,
            BrowserCapabilityId,
            permissions,
            ToCapabilityRisk(new BrowserSafetyPolicy().Evaluate(action, observation).Risk),
            ContainsPrivateOsData: true,
            BenefitsFromBackgroundExecution: false,
            MaxAttempts: 2);

        var created = await _jobs.CreateAsync(
            jobId,
            definition,
            BrowserActionJobHandler.CreateCheckpoint(action),
            cancellationToken).ConfigureAwait(false);
        return Describe(created, action);
    }

    public async Task<BrowserJobOutcome> AdvanceActionAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        var advanced = await _jobs.RunNextStepAsync(jobId, cancellationToken).ConfigureAwait(false);
        return Describe(advanced, TryReadAction(advanced));
    }

    public async Task<BrowserJobOutcome> StartActionAsync(
        BrowserAction action,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(action);

        var jobId = Guid.NewGuid();
        await CreateActionAsync(jobId, action, cancellationToken).ConfigureAwait(false);
        return await AdvanceActionAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserJobOutcome> ApproveAndResumeAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));

        await _jobs.ResumeAfterApprovalAsync(jobId, exactScope, cancellationToken).ConfigureAwait(false);
        var advanced = await _jobs.RunNextStepAsync(jobId, cancellationToken).ConfigureAwait(false);
        return Describe(advanced, TryReadAction(advanced));
    }

    public async Task<BrowserJobOutcome> RearmApprovalAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("Exact approval scope is required.", nameof(exactScope));

        var rearmed = await _jobs.RearmApprovalAsync(jobId, exactScope, cancellationToken).ConfigureAwait(false);
        return Describe(rearmed, TryReadAction(rearmed));
    }

    public async Task<BrowserAmbiguousRecoveryResult> TryReconcileAmbiguousAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));

        var job = await _jobStore.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Browser child job '{jobId}' was not found.");
        var action = TryReadAction(job)
            ?? throw new InvalidOperationException("Ambiguous browser job is missing its descriptive action checkpoint.");

        if (job.State != AgentJobState.Running)
        {
            return new BrowserAmbiguousRecoveryResult(
                BrowserAmbiguousRecoveryStatus.NotAmbiguous,
                jobId,
                $"Child job is {job.State}; ambiguous-running reconciliation is not applicable.",
                Describe(job, action));
        }

        var evidence = await _driver.ObserveAsync(cancellationToken).ConfigureAwait(false);
        var proof = BrowserAmbiguousStateReconciler.TryVerify(action, evidence);
        if (!proof.Verified)
        {
            return new BrowserAmbiguousRecoveryResult(
                BrowserAmbiguousRecoveryStatus.NeedsHumanResolution,
                jobId,
                proof.Detail,
                new BrowserJobOutcome(jobId, AgentJobState.Running,
                    "Browser action remains side-effect ambiguous and was not replayed automatically."),
                evidence);
        }

        var checkpoint = new AgentJobCheckpoint(
            "browser.action.verified",
            JsonSerializer.Serialize(new VerifiedBrowserActionCheckpoint(
                action.Kind.ToString(),
                jobId,
                evidence.Url,
                evidence.Url,
                proof.Detail,
                DateTimeOffset.UtcNow), JsonOptions),
            DateTimeOffset.UtcNow);
        var reconciled = await _jobs
            .CompleteAmbiguousRunningAsync(jobId, checkpoint, proof.Detail, cancellationToken)
            .ConfigureAwait(false);
        var outcome = Describe(reconciled, action);
        if (outcome.VerifiedStep is null)
            throw new InvalidOperationException("Reconciled browser job did not produce a verified-step checkpoint.");

        return new BrowserAmbiguousRecoveryResult(
            BrowserAmbiguousRecoveryStatus.Reconciled,
            jobId,
            proof.Detail,
            outcome,
            evidence);
    }

    public async Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var cancelled = await _jobs.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
        return Describe(cancelled, TryReadAction(cancelled));
    }

    public async Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var job = await _jobStore.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        return job is null ? null : Describe(job, TryReadAction(job));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        await SafeCloseAsync(_context).ConfigureAwait(false);
        _playwright.Dispose();
    }

    private static BrowserJobOutcome Describe(AgentJobRecord job, BrowserAction? action)
    {
        BrowserApprovalPrompt? prompt = null;
        if (job.State == AgentJobState.WaitingForApproval && !string.IsNullOrWhiteSpace(job.ApprovalScope))
        {
            var target = action?.Destination?.AbsoluteUri
                ?? action?.Locator?.Name
                ?? action?.Locator?.Value
                ?? "current browser context";
            var summary = action?.Rationale;
            if (string.IsNullOrWhiteSpace(summary))
                summary = $"{action?.Kind.ToString() ?? "Browser"} action on {target}";

            prompt = new BrowserApprovalPrompt(
                job.JobId,
                job.ApprovalScope,
                action?.Kind ?? BrowserActionKind.Click,
                summary,
                target,
                job.UpdatedAt);
        }

        var message = job.State switch
        {
            AgentJobState.WaitingForApproval => "Browser action is paused and has not executed. Explicit one-time approval is required.",
            AgentJobState.Completed => "Browser action executed or was crash-reconciled and its intended post-action state was verified.",
            AgentJobState.Cancelled => "Browser action was cancelled.",
            AgentJobState.Failed => $"Browser action failed: {job.LastError}",
            AgentJobState.RetryScheduled => $"Browser action failed safely and is eligible for retry: {job.LastError}",
            AgentJobState.Pending => "Browser action is durably created and has not executed yet.",
            AgentJobState.Running => "Browser action has an ambiguous in-flight checkpoint and will not be replayed automatically.",
            _ => "Browser action is pending."
        };

        return new BrowserJobOutcome(job.JobId, job.State, message, prompt, TryReadVerifiedStep(job));
    }

    private static BrowserGoalVerifiedStep? TryReadVerifiedStep(AgentJobRecord job)
    {
        if (job.State != AgentJobState.Completed
            || job.Checkpoint is null
            || !string.Equals(job.Checkpoint.Step, "browser.action.verified", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(job.Checkpoint.Payload))
        {
            return null;
        }

        try
        {
            var checkpoint = JsonSerializer.Deserialize<VerifiedBrowserActionCheckpoint>(job.Checkpoint.Payload, JsonOptions);
            if (checkpoint is null
                || !Enum.TryParse<BrowserActionKind>(checkpoint.Kind, ignoreCase: true, out var kind)
                || checkpoint.UrlBefore is null
                || checkpoint.UrlAfter is null)
            {
                return null;
            }

            return new BrowserGoalVerifiedStep(
                job.JobId,
                kind,
                checkpoint.UrlBefore,
                checkpoint.UrlAfter,
                checkpoint.VerificationDetail,
                checkpoint.CompletedAt);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static BrowserAction? TryReadAction(AgentJobRecord job)
    {
        if (job.Checkpoint is null || string.IsNullOrWhiteSpace(job.Checkpoint.Payload))
            return null;
        if (!job.Checkpoint.Step.StartsWith("browser.action", StringComparison.Ordinal))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BrowserAction>(job.Checkpoint.Payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CapabilityRiskLevel ToCapabilityRisk(BrowserRiskLevel risk) => risk switch
    {
        BrowserRiskLevel.Low => CapabilityRiskLevel.Low,
        BrowserRiskLevel.Medium => CapabilityRiskLevel.Medium,
        BrowserRiskLevel.High => CapabilityRiskLevel.High,
        _ => CapabilityRiskLevel.Blocked
    };

    private static void ValidateOptions(BrowserHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.StartUri.IsAbsoluteUri || options.StartUri.Scheme is not ("http" or "https"))
            throw new ArgumentException("Browser start URI must be absolute HTTP(S).", nameof(options));
        if (options.AllowedHosts is { Count: > 0 }
            && !options.AllowedHosts.Contains(options.StartUri.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Browser start host must be included in the explicit host allowlist.", nameof(options));
    }

    private static async Task SafeCloseAsync(IBrowserContext context)
    {
        try { await context.CloseAsync().ConfigureAwait(false); }
        catch { }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record VerifiedBrowserActionCheckpoint(
        string Kind,
        Guid ActionId,
        Uri UrlBefore,
        Uri UrlAfter,
        string? VerificationDetail,
        DateTimeOffset CompletedAt);
}
