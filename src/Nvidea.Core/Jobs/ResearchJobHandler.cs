using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Research;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Durable, side-effect-free research workflow. Each expensive remote boundary is separated by a
/// persisted checkpoint so process restart does not repeat already completed Nemotron planning or
/// Tavily Search/Extract work.
/// </summary>
public sealed class ResearchJobHandler : IAgentJobHandler
{
    public const string Type = "research.deep";
    public const string RequestedStep = "research.requested.v1";
    public const string PlannedStep = "research.planned.v1";
    public const string EvidenceStep = "research.evidence.v1";
    public const string CompletedStep = "research.completed.v1";

    private const int MaxCheckpointUtf8Bytes = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly ResearchEngine _engine;

    public ResearchJobHandler(ResearchEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public string JobType => Type;

    public async Task<JobStepResult> ExecuteStepAsync(
        AgentJobRecord job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (!string.Equals(job.Definition.JobType, Type, StringComparison.Ordinal))
            throw new InvalidOperationException($"ResearchJobHandler cannot execute job type '{job.Definition.JobType}'.");

        var checkpoint = job.Checkpoint
            ?? throw new InvalidOperationException("Research jobs require an initial request checkpoint.");

        return checkpoint.Step switch
        {
            RequestedStep => await PlanAsync(checkpoint.Payload, cancellationToken).ConfigureAwait(false),
            PlannedStep => await GatherAsync(checkpoint.Payload, cancellationToken).ConfigureAwait(false),
            EvidenceStep => await SynthesizeAsync(checkpoint.Payload, cancellationToken).ConfigureAwait(false),
            CompletedStep => throw new InvalidOperationException("Completed research checkpoints must not be executed again."),
            _ => throw new InvalidOperationException($"Unsupported research checkpoint step '{checkpoint.Step}'.")
        };
    }

    public static AgentJobCheckpoint CreateInitialCheckpoint(string question)
    {
        ValidateQuestion(question);
        return new AgentJobCheckpoint(
            RequestedStep,
            SerializeBounded(new RequestedCheckpoint(question.Trim())),
            DateTimeOffset.UtcNow);
    }

    public static ResearchReport ReadCompletedReport(AgentJobRecord job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.State != AgentJobState.Completed || !string.Equals(job.Checkpoint?.Step, CompletedStep, StringComparison.Ordinal))
            throw new InvalidOperationException("The research job does not contain a completed report.");

        var completed = DeserializeBounded<CompletedCheckpoint>(job.Checkpoint.Payload);
        return completed.Report ?? throw new InvalidOperationException("Completed research checkpoint did not contain a report.");
    }

    private async Task<JobStepResult> PlanAsync(string? payload, CancellationToken cancellationToken)
    {
        var requested = DeserializeBounded<RequestedCheckpoint>(payload);
        ValidateQuestion(requested.Question);
        var plan = await _engine.PlanAsync(requested.Question, cancellationToken).ConfigureAwait(false);

        return new JobStepResult(
            Completed: false,
            CheckpointStep: PlannedStep,
            CheckpointPayload: SerializeBounded(new PlannedCheckpoint(requested.Question, plan)));
    }

    private async Task<JobStepResult> GatherAsync(string? payload, CancellationToken cancellationToken)
    {
        var planned = DeserializeBounded<PlannedCheckpoint>(payload);
        ValidateQuestion(planned.Question);
        var plan = planned.Plan ?? throw new InvalidOperationException("Research plan checkpoint is missing its plan.");
        var prepared = await _engine.GatherEvidenceAsync(planned.Question, plan, cancellationToken).ConfigureAwait(false);

        return new JobStepResult(
            Completed: false,
            CheckpointStep: EvidenceStep,
            CheckpointPayload: SerializeBounded(new EvidenceCheckpoint(planned.Question, prepared)));
    }

    private async Task<JobStepResult> SynthesizeAsync(string? payload, CancellationToken cancellationToken)
    {
        var evidenceCheckpoint = DeserializeBounded<EvidenceCheckpoint>(payload);
        ValidateQuestion(evidenceCheckpoint.Question);
        var prepared = evidenceCheckpoint.Prepared
            ?? throw new InvalidOperationException("Research evidence checkpoint is missing prepared evidence.");
        var report = await _engine.SynthesizeAsync(evidenceCheckpoint.Question, prepared, cancellationToken).ConfigureAwait(false);

        return new JobStepResult(
            Completed: true,
            CheckpointStep: CompletedStep,
            CheckpointPayload: SerializeBounded(new CompletedCheckpoint(report)));
    }

    private static string SerializeBounded<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaxCheckpointUtf8Bytes)
            throw new InvalidOperationException($"Research checkpoint exceeds the {MaxCheckpointUtf8Bytes} byte durability limit.");
        return json;
    }

    private static T DeserializeBounded<T>(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Research checkpoint payload is missing.");
        if (Encoding.UTF8.GetByteCount(payload) > MaxCheckpointUtf8Bytes)
            throw new InvalidOperationException($"Research checkpoint exceeds the {MaxCheckpointUtf8Bytes} byte durability limit.");

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Research checkpoint payload is invalid.");
    }

    private static void ValidateQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 4000)
            throw new InvalidOperationException("Research checkpoint contains an invalid question.");
    }

    private sealed record RequestedCheckpoint(string Question);
    private sealed record PlannedCheckpoint(string Question, ResearchPlan? Plan);
    private sealed record EvidenceCheckpoint(string Question, ResearchPreparedEvidence? Prepared);
    private sealed record CompletedCheckpoint(ResearchReport? Report);
}
