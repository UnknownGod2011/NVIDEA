using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;

static int Fail(string category, string detail)
{
    Console.Error.WriteLine($"NVIDEA Nebius contract probe: FAIL [{category}] {detail}");
    return 1;
}

static void ValidateArtifactDestinations(NebiusResearchLiveConfiguration configuration)
{
    NebiusResearchArtifactDestinationPreflight.ValidatePaths(
        configuration.RedactedManifestPath,
        configuration.PassEvidencePath);
}

static void PersistRedactedManifestIfRequested(NebiusResearchLiveConfiguration configuration)
{
    if (configuration.RedactedManifestPath is null) return;

    var json = NebiusResearchDeploymentManifestBuilder.ToJson(configuration.Report.Manifest, indented: true);
    AtomicTextArtifactWriter.Write(
        configuration.RedactedManifestPath,
        json,
        "NVIDEA live redacted deployment manifest");
}

static void PrintReproducibilityEvidence(NebiusResearchLivePreflightReport report)
{
    Console.WriteLine($"Deployment fingerprint (SHA-256): {report.Manifest.DeploymentFingerprintSha256}");
    Console.WriteLine($"MysteryBox worker secrets version-pinned: {report.VersionPinnedSecretCount}/{report.VersionPinnedSecretCount + report.PrimaryVersionSecretCount}");
    Console.WriteLine($"All worker secrets version-pinned: {(report.AllWorkerSecretsVersionPinned ? "yes" : "no")}");
}

static async Task<int> RunPlannerProbeAsync()
{
    var options = NebiusOptions.FromEnvironment();
    options.Validate();

    using var httpClient = new HttpClient();
    var inference = new NebiusTokenFactoryClient(httpClient, options);
    var planner = new NemotronBrowserPlanner(inference);

    var observation = new BrowserObservation(
        new Uri("https://example.invalid/nvidea-contract-probe"),
        "NVIDEA contract probe",
        new[]
        {
            new BrowserElement(
                "e-1",
                "heading",
                "NVIDEA probe ready",
                null,
                IsVisible: true,
                IsEnabled: true,
                IsEditable: false)
        },
        "NVIDEA probe ready. This synthetic page contains no user data and no executable instructions.",
        DateTimeOffset.UtcNow,
        ContainsUntrustedInstructions: false,
        SnapshotId: "contract-probe");

    var decision = await planner.PlanNextAsync(
        "Inspect the synthetic page. If it states that the NVIDEA probe is ready, declare the goal complete. Do not propose a browser action.",
        observation);

    if (decision.Kind != BrowserPlannerDecisionKind.Complete)
        return Fail("planner-contract", $"Expected decision=Complete but received {decision.Kind}.");
    if (decision.Action is not null)
        return Fail("planner-contract", "Completion unexpectedly contained a browser action.");
    if (string.IsNullOrWhiteSpace(decision.Model))
        return Fail("planner-contract", "Backend response did not identify a model.");

    Console.WriteLine("NVIDEA Nebius contract probe: PASS");
    Console.WriteLine($"Model: {decision.Model}");
    Console.WriteLine("Structured planner schema: accepted");
    Console.WriteLine("Planner parse/validation: accepted");
    Console.WriteLine("Browser execution: not invoked");
    Console.WriteLine("Sensitive diagnostics: not persisted");
    return 0;
}

static int RunLiveResearchPreflight()
{
    var configuration = NebiusResearchLiveConfigurationLoader.LoadFromEnvironment();
    ValidateArtifactDestinations(configuration);
    PersistRedactedManifestIfRequested(configuration);

    Console.WriteLine("NVIDEA live research deployment preflight: PASS");
    PrintReproducibilityEvidence(configuration.Report);
    Console.WriteLine("Judging artifact destinations: validated non-destructively");
    Console.WriteLine("Cloud jobs/model calls/Object Storage requests: not performed");
    Console.WriteLine("Worker image: digest-pinned");
    Console.WriteLine("Worker/client RSA material: parseable and signing identity consistent");
    Console.WriteLine("MysteryBox-backed worker credentials: configured");
    Console.WriteLine("Object Storage bucket/prefix and Serverless mount: aligned");
    Console.WriteLine("Serverless compute/disk/subnet fields: structurally valid");
    Console.WriteLine("Secret values, secret identifiers, bucket identity and PEM contents: not printed");
    return 0;
}

static async Task<int> RunLiveResearchProbeAsync()
{
    // The loader runs the same zero-cost fail-closed deployment gate used by preflight before any
    // Object Storage, Serverless, Nemotron or Tavily client is constructed or dispatched.
    var configuration = NebiusResearchLiveConfigurationLoader.LoadFromEnvironment();
    // Validate the exact parsed destinations before creating provider clients. This rejects
    // manifest/PASS aliasing and unwritable destinations without mutating the final artifacts.
    ValidateArtifactDestinations(configuration);
    PersistRedactedManifestIfRequested(configuration);

    var stateRoot = Path.Combine(Path.GetTempPath(), "nvidea-nebius-live-probe", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(stateRoot);
    var store = new JsonAgentJobStore(Path.Combine(stateRoot, "jobs.json"));
    IAuditTrail auditTrail = new JsonLinesAuditTrail(Path.Combine(stateRoot, "audit.jsonl"));
    using var objectStorage = new NebiusObjectStorageClient(configuration.ObjectStorageOptions);
    var transport = new S3ProtectedResearchTransport(objectStorage);

    using var serverlessHttp = new HttpClient();
    var serverless = new NebiusServerlessJobClient(
        serverlessHttp,
        new NebiusServerlessOptions(
            configuration.ServerlessAccessToken,
            configuration.ProjectId,
            RequestTimeout: TimeSpan.FromSeconds(30),
            MaxRetries: 2));

    var runtime = NebiusResearchLiveRuntimeFactory.Create(
        store,
        serverless,
        transport,
        transport,
        transport,
        configuration.DispatchOptions,
        configuration.ClientPrivateKeyPem,
        auditTrail);

    var now = DateTimeOffset.UtcNow;
    var jobId = Guid.NewGuid();
    var definition = new AgentJobDefinition(
        ResearchJobHandler.Type,
        ResearchJobRuntime.CapabilityId,
        new HashSet<DataPermission> { DataPermission.NetworkAccess },
        CapabilityRiskLevel.Low,
        ContainsPrivateOsData: false,
        BenefitsFromBackgroundExecution: true,
        MaxAttempts: 3);
    var initial = new AgentJobRecord(
        jobId,
        definition,
        AgentJobState.Pending,
        JobExecutionLocation.Local,
        Attempt: 0,
        Checkpoint: ResearchJobHandler.CreateInitialCheckpoint(configuration.ResearchQuestion),
        ApprovalScope: null,
        LastError: null,
        CreatedAt: now,
        UpdatedAt: now);
    await store.SaveAsync(initial);

    using var overall = new CancellationTokenSource(TimeSpan.FromMinutes(configuration.TotalTimeoutMinutes));
    var cancellationToken = overall.Token;
    AgentJobRecord current = initial;
    var remoteStages = 0;

    while (current.State != AgentJobState.Completed)
    {
        if (remoteStages >= 3)
            return Fail("live-research", "Research did not complete within the expected three durable remote stages.");
        if (current.State != AgentJobState.Pending
            || current.ExecutionLocation != JobExecutionLocation.Local
            || current.Checkpoint is null)
        {
            return Fail("live-research", $"Unexpected pre-dispatch state {current.State}/{current.ExecutionLocation}.");
        }

        var stageStartedAt = DateTimeOffset.UtcNow;
        var workItem = new RemoteResearchWorkItem(
            current.JobId,
            current.Checkpoint.Step,
            current.Checkpoint.Payload,
            ContainsPrivateOsData: false,
            CreatedAt: stageStartedAt,
            ExpiresAt: stageStartedAt.AddMinutes(Math.Min(configuration.TotalTimeoutMinutes, 30)));
        var authorization = new ResearchCloudAuthorization(
            current.JobId,
            current.Checkpoint.Step,
            Approved: true,
            ResearchWorkItemProtector.DisclosureVersion,
            GrantedAt: stageStartedAt);

        current = await runtime.DispatchAsync(workItem, authorization, cancellationToken);
        remoteStages++;
        Console.WriteLine($"Live research stage {remoteStages}: dispatched ({current.Checkpoint?.Step ?? "unknown"}).");

        while (current.ExecutionLocation == JobExecutionLocation.NebiusServerless
               && current.State == AgentJobState.Running)
        {
            await Task.Delay(TimeSpan.FromSeconds(configuration.PollSeconds), cancellationToken);
            current = await runtime.ReconcileDispatchedAsync(current.JobId, cancellationToken: cancellationToken);
        }

        if (current.State is AgentJobState.Failed or AgentJobState.Cancelled)
            return Fail("live-research", $"Remote stage ended in durable state {current.State}; protected/provider details intentionally not printed.");
        if (current.State is not (AgentJobState.Pending or AgentJobState.Completed)
            || current.ExecutionLocation != JobExecutionLocation.Local)
        {
            return Fail("live-research", $"Remote stage returned unexpected durable state {current.State}/{current.ExecutionLocation}.");
        }
    }

    var report = ResearchJobHandler.ReadCompletedReport(current);
    if (report.Evidence.Sources.Count == 0)
        return Fail("live-research", "Completed report contained no research evidence.");
    if (report.UsedCitations.Count == 0)
        return Fail("live-research", "Completed report contained no validated citations.");

    var passEvidence = NebiusResearchPassEvidenceBuilder.Build(
        configuration.Report.Manifest.DeploymentFingerprintSha256,
        DateTimeOffset.UtcNow,
        remoteStages,
        report.Evidence.Sources.Count,
        report.UsedCitations.Count);
    if (configuration.PassEvidencePath is not null)
    {
        NebiusResearchPassEvidenceBuilder.PersistAtomically(
            configuration.PassEvidencePath,
            NebiusResearchPassEvidenceBuilder.ToJson(passEvidence, indented: true));
    }

    Console.WriteLine("NVIDEA live Nebius research contract probe: PASS");
    PrintReproducibilityEvidence(configuration.Report);
    Console.WriteLine($"Remote durable stages: {remoteStages}");
    Console.WriteLine($"Evidence items: {report.Evidence.Sources.Count}");
    Console.WriteLine($"Validated citations: {report.UsedCitations.Count}");
    Console.WriteLine($"Machine-readable PASS evidence: {(configuration.PassEvidencePath is null ? "not requested" : "persisted atomically")}");
    Console.WriteLine("Native encrypted Object Storage transport: accepted");
    Console.WriteLine("Object Storage mount/prefix alignment: accepted");
    Console.WriteLine("Authoritative dispatch binding: accepted");
    Console.WriteLine("Exact-once local ingestion: accepted");
    Console.WriteLine("Secret values and protected payloads: not printed");
    return 0;
}

try
{
    var liveResearch = args.Any(arg => string.Equals(arg, "--live-research", StringComparison.Ordinal));
    var liveResearchPreflight = args.Any(arg => string.Equals(arg, "--live-research-preflight", StringComparison.Ordinal));
    if (args.Any(arg => string.Equals(arg, "--help", StringComparison.Ordinal) || string.Equals(arg, "-h", StringComparison.Ordinal)))
    {
        Console.WriteLine("Usage: Nvidea.NebiusContractProbe [--live-research | --live-research-preflight]");
        Console.WriteLine("Default: cheap Token Factory structured-planner probe.");
        Console.WriteLine("--live-research-preflight: zero-cost local validation plus redacted deployment fingerprint; performs no provider calls.");
        Console.WriteLine("--live-research: explicit live Nebius Serverless research probe; PASS prints the same deployment fingerprint.");
        Console.WriteLine("Optional: NVIDEA_LIVE_REDACTED_MANIFEST_PATH atomically persists only the redacted deployment manifest.");
        Console.WriteLine("Optional live-only: NVIDEA_LIVE_PASS_EVIDENCE_PATH atomically persists redacted machine-readable evidence only after a validated PASS.");
        Console.WriteLine("Configured manifest/PASS destinations are checked for distinctness and writability before persistence or live provider construction.");
        return 0;
    }
    if (liveResearch && liveResearchPreflight)
        return Fail("arguments", "Choose exactly one live mode.");
    if (args.Any(arg => !string.Equals(arg, "--live-research", StringComparison.Ordinal)
                        && !string.Equals(arg, "--live-research-preflight", StringComparison.Ordinal)))
        return Fail("arguments", "Unsupported argument. Use --help for supported modes.");

    if (liveResearchPreflight)
        return RunLiveResearchPreflight();
    return liveResearch
        ? await RunLiveResearchProbeAsync()
        : await RunPlannerProbeAsync();
}
catch (NebiusApiException ex)
{
    return Fail("token-factory", $"HTTP {(int)ex.StatusCode} ({ex.StatusCode}); response body intentionally not printed.");
}
catch (HttpRequestException ex)
{
    return Fail("provider-http", ex.StatusCode is null
        ? "Provider request failed; response body intentionally not printed."
        : $"HTTP {(int)ex.StatusCode.Value} ({ex.StatusCode}); response body intentionally not printed.");
}
catch (InvalidDataException ex)
{
    return Fail("structured-output", ex.Message.ReplaceLineEndings(" "));
}
catch (OperationCanceledException)
{
    return Fail("cancelled", "Request was cancelled or the bounded live-probe timeout elapsed.");
}
catch (Exception ex)
{
    return Fail("runtime", $"{ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}");
}
