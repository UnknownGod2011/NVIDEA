using System.Security.Cryptography;
using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;

static int Fail(string category, string detail)
{
    Console.Error.WriteLine($"NVIDEA Nebius contract probe: FAIL [{category}] {detail}");
    return 1;
}

static string RequiredEnvironment(string name)
{
    var value = Environment.GetEnvironmentVariable(name)?.Trim();
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Required live-probe configuration '{name}' is missing.");
    if (value.Length > 8192 || value.Any(char.IsControl))
        throw new InvalidOperationException($"Live-probe configuration '{name}' is invalid.");
    return value;
}

static string? OptionalEnvironment(string name)
{
    var value = Environment.GetEnvironmentVariable(name)?.Trim();
    if (string.IsNullOrWhiteSpace(value)) return null;
    if (value.Length > 8192 || value.Any(char.IsControl))
        throw new InvalidOperationException($"Live-probe configuration '{name}' is invalid.");
    return value;
}

static long RequiredPositiveInt64(string name)
{
    var value = RequiredEnvironment(name);
    if (!long.TryParse(value, out var parsed) || parsed <= 0)
        throw new InvalidOperationException($"Required live-probe configuration '{name}' must be a positive integer.");
    return parsed;
}

static int BoundedInt32(string name, int defaultValue, int minimum, int maximum)
{
    var value = OptionalEnvironment(name);
    if (value is null) return defaultValue;
    if (!int.TryParse(value, out var parsed) || parsed < minimum || parsed > maximum)
        throw new InvalidOperationException($"Live-probe configuration '{name}' must be between {minimum} and {maximum}.");
    return parsed;
}

static string ReadRequiredPemFile(string environmentName)
{
    var path = Path.GetFullPath(RequiredEnvironment(environmentName));
    var text = File.ReadAllText(path);
    if (string.IsNullOrWhiteSpace(text) || text.Length > 65536)
        throw new InvalidOperationException($"PEM material referenced by '{environmentName}' is missing or oversized.");
    return text;
}

static NebiusMysteryBoxSecretRef RequiredSecretRef(string environmentName) =>
    new(SecretId: RequiredEnvironment(environmentName));

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

static async Task<int> RunLiveResearchProbeAsync()
{
    // This mode is deliberately explicit and expensive. The host transport root must be a mounted
    // view of the same backing storage configured as the worker's Nebius READ_WRITE volume.
    var serverlessAccessToken = RequiredEnvironment("NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN");
    var projectId = RequiredEnvironment("NVIDEA_LIVE_SERVERLESS_PROJECT_ID");
    var workerImage = RequiredEnvironment("NVIDEA_LIVE_WORKER_IMAGE");
    var subnetId = RequiredEnvironment("NVIDEA_LIVE_SUBNET_ID");
    var platform = RequiredEnvironment("NVIDEA_LIVE_PLATFORM");
    var preset = RequiredEnvironment("NVIDEA_LIVE_PRESET");
    var timeout = RequiredEnvironment("NVIDEA_LIVE_TIMEOUT");
    var diskType = RequiredEnvironment("NVIDEA_LIVE_DISK_TYPE");
    var diskSizeBytes = RequiredPositiveInt64("NVIDEA_LIVE_DISK_SIZE_BYTES");
    var transportSource = RequiredEnvironment("NVIDEA_LIVE_TRANSPORT_SOURCE");
    var workerTransportRoot = OptionalEnvironment("NVIDEA_LIVE_WORKER_TRANSPORT_ROOT") ?? "/mnt/nvidea-research";
    var hostTransportRoot = Path.GetFullPath(RequiredEnvironment("NVIDEA_LIVE_CLIENT_TRANSPORT_ROOT"));
    var workerPublicKeyPem = ReadRequiredPemFile("NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE");
    var clientPrivateKeyPem = ReadRequiredPemFile("NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE");
    var pollSeconds = BoundedInt32("NVIDEA_LIVE_POLL_SECONDS", 5, 1, 30);
    var totalTimeoutMinutes = BoundedInt32("NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES", 20, 2, 60);
    var question = OptionalEnvironment("NVIDEA_LIVE_RESEARCH_QUESTION")
        ?? "What are the current official capabilities of NVIDIA Nemotron models served through Nebius for agentic research? Use authoritative sources and state uncertainty.";
    if (question.Length > 2000)
        throw new InvalidOperationException("NVIDEA_LIVE_RESEARCH_QUESTION exceeds the live-probe limit.");

    using var clientRsa = RSA.Create();
    clientRsa.ImportFromPem(clientPrivateKeyPem);
    var clientPublicKeyPem = clientRsa.ExportSubjectPublicKeyInfoPem();

    var secretEnvironment = new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
    {
        ["NEBIUS_API_KEY"] = RequiredSecretRef("NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID"),
        ["TAVILY_API_KEY"] = RequiredSecretRef("NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID"),
        ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = RequiredSecretRef("NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID")
    };
    var plainEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = workerTransportRoot,
        [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = clientPublicKeyPem
    };

    var dispatchOptions = new NebiusResearchDispatchOptions(
        WorkerImage: workerImage,
        WorkerPublicKeyPem: workerPublicKeyPem,
        ContainerCommand: "dotnet",
        Platform: platform,
        Preset: preset,
        Timeout: timeout,
        SubnetId: subnetId,
        Disk: new NebiusServerlessDiskSpec(diskType, diskSizeBytes),
        EnvironmentVariables: plainEnvironment,
        SecretEnvironmentVariables: secretEnvironment,
        Volumes: new[]
        {
            new NebiusServerlessVolumeMount(
                transportSource,
                workerTransportRoot,
                "READ_WRITE",
                OptionalEnvironment("NVIDEA_LIVE_TRANSPORT_SOURCE_PATH"))
        });

    var stateRoot = Path.Combine(Path.GetTempPath(), "nvidea-nebius-live-probe", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(stateRoot);
    var store = new JsonAgentJobStore(Path.Combine(stateRoot, "jobs.json"));
    IAuditTrail auditTrail = new JsonLinesAuditTrail(Path.Combine(stateRoot, "audit.jsonl"));
    var transport = new DirectoryProtectedResearchTransport(hostTransportRoot);

    using var serverlessHttp = new HttpClient();
    var serverless = new NebiusServerlessJobClient(
        serverlessHttp,
        new NebiusServerlessOptions(
            serverlessAccessToken,
            projectId,
            RequestTimeout: TimeSpan.FromSeconds(30),
            MaxRetries: 2));

    var runtime = NebiusResearchLiveRuntimeFactory.Create(
        store,
        serverless,
        transport,
        transport,
        transport,
        dispatchOptions,
        clientPrivateKeyPem,
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
        Checkpoint: ResearchJobHandler.CreateInitialCheckpoint(question),
        ApprovalScope: null,
        LastError: null,
        CreatedAt: now,
        UpdatedAt: now);
    await store.SaveAsync(initial);

    using var overall = new CancellationTokenSource(TimeSpan.FromMinutes(totalTimeoutMinutes));
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
            ExpiresAt: stageStartedAt.AddMinutes(Math.Min(totalTimeoutMinutes, 30)));
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
            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), cancellationToken);
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
    if (report.Evidence.Items.Count == 0)
        return Fail("live-research", "Completed report contained no research evidence.");
    if (report.UsedCitations.Count == 0)
        return Fail("live-research", "Completed report contained no validated citations.");

    Console.WriteLine("NVIDEA live Nebius research contract probe: PASS");
    Console.WriteLine($"Remote durable stages: {remoteStages}");
    Console.WriteLine($"Evidence items: {report.Evidence.Items.Count}");
    Console.WriteLine($"Validated citations: {report.UsedCitations.Count}");
    Console.WriteLine("Encrypted shared transport: accepted");
    Console.WriteLine("Authoritative dispatch binding: accepted");
    Console.WriteLine("Exact-once local ingestion: accepted");
    Console.WriteLine("Secret values and protected payloads: not printed");
    return 0;
}

try
{
    var liveResearch = args.Any(arg => string.Equals(arg, "--live-research", StringComparison.Ordinal));
    if (args.Any(arg => string.Equals(arg, "--help", StringComparison.Ordinal) || string.Equals(arg, "-h", StringComparison.Ordinal)))
    {
        Console.WriteLine("Usage: Nvidea.NebiusContractProbe [--live-research]");
        Console.WriteLine("Default: cheap Token Factory structured-planner probe.");
        Console.WriteLine("--live-research: explicit live Nebius Serverless research probe; see docs/nebius-contract-probe.md.");
        return 0;
    }
    if (args.Any(arg => !string.Equals(arg, "--live-research", StringComparison.Ordinal)))
        return Fail("arguments", "Unsupported argument. Use --help for supported modes.");

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
