using System.Text.Json;
using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;

namespace Nvidea.PersonalAiDemoEval;

internal static class Program
{
    private const string ApprovalScope = "browser.submit:demo-research-report";

    public static async Task<int> Main(string[] args)
    {
        var outputPath = ParseOutputPath(args);
        var checks = new List<EvalCheck>();
        var metrics = new Dictionary<string, object>(StringComparer.Ordinal);

        try
        {
            var fixedNow = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
            var time = new FixedTimeProvider(fixedNow);
            var memoryStore = new InMemoryMemoryStore();
            var embeddings = new DeterministicEmbeddingProvider(time);
            using var memory = new PersonalMemoryService(memoryStore, embeddingProvider: embeddings, timeProvider: time);
            await memory.InitializeAsync().ConfigureAwait(false);
            await memory.RememberAsync(new MemoryWriteRequest
            {
                Layer = MemoryLayer.Semantic,
                Key = "travel.style",
                Content = "For personal trips, prefer Kyoto neighborhoods that are quiet, walkable, and close to independent cafes rather than nightlife.",
                Tags = ["travel", "preference"],
                Importance = 0.9,
                Confidence = 1.0,
                Sensitivity = MemorySensitivity.Personal,
                Retention = MemoryRetention.ThirtyDays,
                Provenance = new MemoryProvenance("explicit-user", ObservedAt: fixedNow),
                ExplicitUserApproval = true
            }).ConfigureAwait(false);

            var inference = new DeterministicInferenceClient();
            var invocation = new DesktopInvocationService(inference, memory);
            var invocationResult = await invocation.InvokeAsync(new DesktopInvocationRequest(
                "Where should I stay for a calm, pedestrian-friendly trip with good coffee?",
                new DesktopContext(
                    ActiveApplication: "Travel Notes",
                    SelectedText: "I want somewhere restful after long conference days.",
                    ClipboardText: "PRIVATE_CLIPBOARD_FIXTURE_SHOULD_NOT_APPEAR",
                    WindowTitle: "Japan planning"),
                DesktopInvocationMode.Chat,
                AllowClipboardContext: false)).ConfigureAwait(false);

            AddCheck(checks,
                "context-memory-recall",
                invocationResult.MemoriesUsed.Any(m => m.Memory.Key == "travel.style")
                && inference.SawSelectedContext
                && inference.SawMemoryContext,
                "Desktop invocation recalled a durable personal preference and supplied selected-text context to the trusted inference boundary.");
            AddCheck(checks,
                "clipboard-withheld-by-default",
                inference.SawClipboardWithheldMarker && !inference.SawClipboardFixture,
                "Clipboard data remained withheld because the invocation did not opt in to clipboard context.");
            metrics["memoriesUsed"] = invocationResult.MemoriesUsed.Count;

            var researchInference = new DeterministicInferenceClient();
            var researchProvider = new FixtureResearchProvider(fixedNow);
            var research = new ResearchEngine(researchInference, researchProvider);
            var report = await research.ResearchAsync("What makes this demo architecture credible for a personal AI judge?").ConfigureAwait(false);
            AddCheck(checks,
                "cited-research",
                report.UsedCitations.Count == 2
                && report.AnswerMarkdown.Contains("[src:nebius-doc]", StringComparison.Ordinal)
                && report.AnswerMarkdown.Contains("[src:tavily-doc]", StringComparison.Ordinal)
                && !report.Warnings.Any(w => w.Contains("unknown source", StringComparison.OrdinalIgnoreCase)),
                "Research planning, fixture search, deterministic evidence preparation, and citation validation produced two machine-verifiable citations.");
            metrics["researchSources"] = report.Evidence.Sources.Count;
            metrics["usedCitations"] = report.UsedCitations.Count;

            var browserDriver = new FixtureBrowserDriver(fixedNow);
            var approvalGate = new RecordingApprovalGate(approve: true);
            var browserExecutor = new BrowserAgentExecutor(
                browserDriver,
                new BrowserSafetyPolicy(),
                approvalGate,
                new ConservativeBrowserVerifier());
            var browserReceipt = await browserExecutor.ExecuteOneAsync(new BrowserAction(
                BrowserActionKind.Click,
                BrowserLocator.Accessibility("submit-1"),
                ExpectedState: "Submission complete",
                Rationale: "Submit research report for review")).ConfigureAwait(false);
            AddCheck(checks,
                "consequential-browser-approval",
                browserReceipt.Decision.Risk == BrowserRiskLevel.High
                && browserReceipt.Decision.RequiresApproval
                && approvalGate.RequestCount == 1,
                "The browser safety policy classified submit as consequential and required explicit approval before execution.");
            AddCheck(checks,
                "browser-post-action-verification",
                browserReceipt.DriverReportedSuccess && browserReceipt.Verified && browserDriver.Submitted,
                "The browser action executed only after approval and was accepted only after fresh post-action state verification.");
            metrics["browserApprovalRequests"] = approvalGate.RequestCount;

            var jobStore = new InMemoryJobStore();
            var audit = new InMemoryAuditTrail();
            var handler = new DemoResumableHandler();
            var orchestratorBeforeRestart = new ResumableJobOrchestrator(
                jobStore,
                new ConservativeJobExecutionPolicy(),
                audit,
                [handler]);
            var definition = new AgentJobDefinition(
                DemoResumableHandler.Type,
                "demo.personal-ai",
                new HashSet<DataPermission>
                {
                    DataPermission.MemoryRead,
                    DataPermission.BrowserRead,
                    DataPermission.BrowserWrite
                },
                CapabilityRiskLevel.High,
                ContainsPrivateOsData: true,
                BenefitsFromBackgroundExecution: true,
                MaxAttempts: 2);

            var job = await orchestratorBeforeRestart.CreateAsync(definition).ConfigureAwait(false);
            job = await orchestratorBeforeRestart.RunNextStepAsync(job.JobId).ConfigureAwait(false);
            var checkpointSurvived = job.State == AgentJobState.Pending
                && string.Equals(job.Checkpoint?.Step, "research-prepared", StringComparison.Ordinal)
                && job.ExecutionLocation == JobExecutionLocation.Local;

            var orchestratorAfterRestart = new ResumableJobOrchestrator(
                jobStore,
                new ConservativeJobExecutionPolicy(),
                audit,
                [handler]);
            job = await orchestratorAfterRestart.RunNextStepAsync(job.JobId).ConfigureAwait(false);
            var pausedForApproval = job.State == AgentJobState.WaitingForApproval
                && string.Equals(job.ApprovalScope, ApprovalScope, StringComparison.Ordinal);
            job = await orchestratorAfterRestart.ResumeAfterApprovalAsync(job.JobId, ApprovalScope).ConfigureAwait(false);
            job = await orchestratorAfterRestart.RunNextStepAsync(job.JobId).ConfigureAwait(false);
            var auditEvents = await audit.ReadAllAsync().ConfigureAwait(false);

            AddCheck(checks,
                "resumable-state-survives-restart",
                checkpointSurvived && pausedForApproval,
                "A durable checkpoint survived orchestrator reconstruction and resumed into an explicit approval wait instead of replaying blindly.");
            AddCheck(checks,
                "ephemeral-exact-scope-approval",
                job.State == AgentJobState.Completed
                && handler.ConsumedExactApproval
                && auditEvents.Any(e => e.EventType == "job.approved")
                && auditEvents.Any(e => e.EventType == "job.completed"),
                "The resumed consequential step consumed a single-use exact-scope approval and completed with auditable state transitions.");
            AddCheck(checks,
                "private-background-work-stays-local",
                job.ExecutionLocation == JobExecutionLocation.Local,
                "The conservative execution policy kept a background-beneficial job local because it was marked as containing private OS data.");
            metrics["auditEvents"] = auditEvents.Count;

            AddCheck(checks,
                "credential-free",
                researchProvider.NetworkCalls == 0 && inference.NetworkCalls == 0 && researchInference.NetworkCalls == 0,
                "The evaluator used only synthetic adapters and did not require live Nebius, Tavily, browser, cloud, or embedding credentials.");
        }
        catch (Exception ex)
        {
            checks.Add(new EvalCheck("evaluator-completed", false, $"Evaluator threw {ex.GetType().Name}: {ex.Message}"));
        }

        var evidence = new EvalEvidence(
            SchemaVersion: 1,
            GeneratedAt: DateTimeOffset.UtcNow,
            OverallPassed: checks.Count > 0 && checks.All(c => c.Passed),
            Checks: checks,
            Metrics: metrics);
        var json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });

        Console.WriteLine(json);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var fullPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(fullPath, json + Environment.NewLine).ConfigureAwait(false);
        }

        return evidence.OverallPassed ? 0 : 1;
    }

    private static string? ParseOutputPath(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--output", StringComparison.Ordinal))
                continue;
            if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                throw new ArgumentException("--output requires a file path.");
            return args[i + 1];
        }
        return null;
    }

    private static void AddCheck(List<EvalCheck> checks, string id, bool passed, string detail) =>
        checks.Add(new EvalCheck(id, passed, detail));

    private sealed record EvalCheck(string Id, bool Passed, string Detail);

    private sealed record EvalEvidence(
        int SchemaVersion,
        DateTimeOffset GeneratedAt,
        bool OverallPassed,
        IReadOnlyList<EvalCheck> Checks,
        IReadOnlyDictionary<string, object> Metrics);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryMemoryStore : IMemoryStore
    {
        private IReadOnlyList<MemoryRecord> _records = Array.Empty<MemoryRecord>();

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_records);
        }

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records = memories.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class DeterministicEmbeddingProvider(TimeProvider timeProvider) : IProvenancedMemoryEmbeddingProvider
    {
        public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            (await EmbedWithMetadataAsync(text, cancellationToken).ConfigureAwait(false)).Vector;

        public Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalized = text.ToLowerInvariant();
            var vector = normalized.Contains("quiet", StringComparison.Ordinal)
                || normalized.Contains("walkable", StringComparison.Ordinal)
                || normalized.Contains("pedestrian", StringComparison.Ordinal)
                || normalized.Contains("calm", StringComparison.Ordinal)
                    ? new float[] { 1f, 0f, 0f }
                    : new float[] { 0f, 1f, 0f };
            return Task.FromResult(new MemoryEmbeddingVector(
                vector,
                new MemoryEmbeddingProvenance("fixture-local", "demo-embedding-v1", 3, true, timeProvider.GetUtcNow())));
        }
    }

    private sealed class DeterministicInferenceClient : IAgentInferenceClient
    {
        public bool SawSelectedContext { get; private set; }
        public bool SawMemoryContext { get; private set; }
        public bool SawClipboardWithheldMarker { get; private set; }
        public bool SawClipboardFixture { get; private set; }
        public int NetworkCalls => 0;

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(request.ResponseJsonSchema))
            {
                return Task.FromResult(new AgentCompletion(
                    "{\"queries\":[{\"query\":\"Nebius personal AI architecture\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null},{\"query\":\"Tavily grounded research provenance\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}",
                    Array.Empty<ToolCall>(),
                    "fixture-nemotron",
                    "stop"));
            }

            var system = request.Messages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
            var user = request.Messages.FirstOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
            if (system.Contains("research analyst", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new AgentCompletion(
                    "NVIDEA keeps core reasoning and long-running execution on the Nebius/NVIDIA path [src:nebius-doc] while research evidence remains source-grounded and attributable [src:tavily-doc].",
                    Array.Empty<ToolCall>(),
                    "fixture-nemotron",
                    "stop"));
            }

            SawSelectedContext = user.Contains("restful after long conference days", StringComparison.OrdinalIgnoreCase);
            SawMemoryContext = user.Contains("travel.style", StringComparison.OrdinalIgnoreCase)
                && user.Contains("quiet, walkable", StringComparison.OrdinalIgnoreCase);
            SawClipboardWithheldMarker = user.Contains("intentionally withheld", StringComparison.OrdinalIgnoreCase);
            SawClipboardFixture = user.Contains("PRIVATE_CLIPBOARD_FIXTURE_SHOULD_NOT_APPEAR", StringComparison.Ordinal);
            return Task.FromResult(new AgentCompletion(
                "Based on your preference for a quiet, walkable area with independent cafes, use a calmer Kyoto neighborhood as the planning default.",
                Array.Empty<ToolCall>(),
                "fixture-nemotron",
                "stop"));
        }
    }

    private sealed class FixtureResearchProvider(DateTimeOffset retrievedAt) : IResearchProvider
    {
        public int NetworkCalls => 0;

        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var firstQuery = queries.First().Query;
            var secondQuery = queries.Count > 1 ? queries[1].Query : firstQuery;
            var sources = new[]
            {
                new ResearchSource(
                    "nebius-doc",
                    "Nebius fixture architecture note",
                    new Uri("https://docs.nebius.example/personal-ai"),
                    "https://docs.nebius.example/personal-ai",
                    "Fixture evidence: long-running AI work can be separated from private on-device operating-system actions.",
                    0.95,
                    firstQuery,
                    retrievedAt,
                    retrievedAt.AddDays(-1)),
                new ResearchSource(
                    "tavily-doc",
                    "Tavily fixture provenance note",
                    new Uri("https://docs.tavily.example/research"),
                    "https://docs.tavily.example/research",
                    "Fixture evidence: research answers should retain source provenance and machine-verifiable citations.",
                    0.93,
                    secondQuery,
                    retrievedAt,
                    retrievedAt.AddDays(-1))
            };
            var citations = sources.Select(source => new ResearchCitation(
                source.Id,
                source.Title,
                source.Url,
                source.CanonicalUrl,
                source.Query,
                source.RetrievedAt,
                source.PublishedAt)).ToArray();
            return Task.FromResult(new ResearchBatch(sources, citations, 0, Array.Empty<string>()));
        }
    }

    private sealed class FixtureBrowserDriver(DateTimeOffset observedAt) : IBrowserDriver
    {
        public bool Submitted { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new BrowserObservation(
                new Uri("https://demo.nvidea.example/review"),
                Submitted ? "Review complete" : "Review report",
                [new BrowserElement("submit-1", "button", "Submit research report", null, true, true, false)],
                Submitted ? "Submission complete" : "Draft is ready for final review.",
                observedAt,
                ContainsUntrustedInstructions: false,
                SnapshotId: Submitted ? "after-submit" : "before-submit"));
        }

        public Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (action.Kind != BrowserActionKind.Click || action.Locator?.Value != "submit-1")
                throw new InvalidOperationException("Fixture browser received an unexpected action.");
            Submitted = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingApprovalGate(bool approve) : IBrowserApprovalGate
    {
        public int RequestCount { get; private set; }

        public Task<bool> RequestApprovalAsync(
            BrowserAction action,
            BrowserActionDecision decision,
            BrowserObservation observation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            return Task.FromResult(approve);
        }
    }

    private sealed class InMemoryJobStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _jobs = new();

        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _jobs.TryGetValue(jobId, out var value);
            return Task.FromResult(value);
        }

        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AgentJobRecord>>(_jobs.Values.ToArray());
        }

        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _jobs[record.JobId] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        private readonly List<AuditEvent> _events = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
        }
    }

    private sealed class DemoResumableHandler : IAgentJobHandler
    {
        public const string Type = "demo-personal-ai";
        public string JobType => Type;
        public bool ConsumedExactApproval { get; private set; }

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default) =>
            ExecuteStepAsync(job, new JobExecutionContextProxy(), cancellationToken);

        public Task<JobStepResult> ExecuteStepAsync(
            AgentJobRecord job,
            JobExecutionContext executionContext,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return job.Checkpoint?.Step switch
            {
                null => Task.FromResult(new JobStepResult(
                    Completed: false,
                    CheckpointStep: "research-prepared",
                    CheckpointPayload: "fixture:cited-research-ready")),
                "research-prepared" => Task.FromResult(new JobStepResult(
                    Completed: false,
                    RequiresApproval: true,
                    ApprovalScope: ApprovalScope,
                    CheckpointStep: "awaiting-submit",
                    CheckpointPayload: "fixture:browser-submit-ready")),
                "awaiting-submit" => ExecuteApprovedStep(executionContext),
                _ => throw new InvalidOperationException($"Unexpected demo checkpoint '{job.Checkpoint?.Step}'.")
            };
        }

        private Task<JobStepResult> ExecuteApprovedStep(JobExecutionContext executionContext)
        {
            var grant = executionContext.TakeApproval(ApprovalScope)
                ?? throw new UnauthorizedAccessException("Exact-scope approval was not available to the resumed consequential step.");
            ConsumedExactApproval = string.Equals(grant.ApprovalScope, ApprovalScope, StringComparison.Ordinal);
            return Task.FromResult(new JobStepResult(
                Completed: true,
                CheckpointStep: "completed",
                CheckpointPayload: "fixture:verified"));
        }

        private sealed class JobExecutionContextProxy : JobExecutionContext
        {
            public JobExecutionContextProxy() : base(Guid.Empty, null)
            {
            }
        }
    }
}
