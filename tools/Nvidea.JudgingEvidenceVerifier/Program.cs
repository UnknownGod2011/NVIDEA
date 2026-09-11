using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Jobs;

namespace Nvidea.JudgingEvidenceVerifier;

internal static class Program
{
    private const int MaximumArtifactLength = 256 * 1024;
    private const int MaximumJsonDepth = 32;

    private static readonly string[] RequiredPositiveChecks =
    [
        "context-memory-recall",
        "clipboard-withheld-by-default",
        "cited-research",
        "consequential-browser-approval",
        "browser-post-action-verification",
        "resumable-state-survives-restart",
        "ephemeral-exact-scope-approval",
        "private-background-work-stays-local",
        "credential-free"
    ];

    private static readonly string[] RequiredAdversarialChecks =
    [
        "prompt-injection-cannot-authorize",
        "denied-browser-approval-prevents-mutation",
        "failed-verification-stops-plan",
        "unknown-research-citation-is-flagged",
        "wrong-approval-scope-fails-closed",
        "ambiguous-running-job-does-not-replay"
    ];

    private static readonly JsonSerializerOptions StrictJson = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = MaximumJsonDepth,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            PrintUsage();
            return 0;
        }

        try
        {
            var options = ParseArguments(args);
            var positiveArtifact = ReadArtifact(options.PositivePath, "positive evaluator evidence");
            var adversarialArtifact = ReadArtifact(options.AdversarialPath, "adversarial evaluator evidence");
            var manifestArtifact = ReadArtifact(options.NebiusManifestPath, "Nebius deployment manifest");
            var passArtifact = ReadArtifact(options.NebiusPassPath, "Nebius live PASS evidence");

            var positive = Deserialize<PositiveEvidence>(positiveArtifact.Bytes, "positive evaluator evidence");
            var adversarial = Deserialize<AdversarialEvidence>(adversarialArtifact.Bytes, "adversarial evaluator evidence");
            ValidateEvaluator(positive.SchemaVersion, positive.GeneratedAt, positive.OverallPassed, positive.Checks, RequiredPositiveChecks, "positive evaluator");
            ValidateEvaluator(adversarial.SchemaVersion, adversarial.GeneratedAt, adversarial.OverallPassed, adversarial.Checks, RequiredAdversarialChecks, "adversarial evaluator");

            var nebius = NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                System.Text.Encoding.UTF8.GetString(manifestArtifact.Bytes),
                System.Text.Encoding.UTF8.GetString(passArtifact.Bytes));

            var summary = new JudgeEvidenceSummary(
                SchemaVersion: 1,
                OverallPassed: true,
                EvidenceBoundary: new EvidenceBoundary(
                    SyntheticEvidenceVerified: true,
                    LiveNebiusEvidenceVerified: true,
                    ClaimsExcluded:
                    [
                        "Synthetic evaluator PASS does not prove live Tavily, Playwright, WPF, speech, Ollama, Object Storage, or Serverless health.",
                        "Nebius live PASS verifies the supplied deployment fingerprint and recorded research evidence only; it is not third-party attestation."
                    ]),
                PositiveEvaluator: new EvaluatorSummary(
                    "synthetic",
                    positive.GeneratedAt,
                    positive.Checks.Count,
                    positive.Checks.Select(static check => check.Id).Order(StringComparer.Ordinal).ToArray(),
                    positiveArtifact.Sha256),
                AdversarialEvaluator: new EvaluatorSummary(
                    "synthetic",
                    adversarial.GeneratedAt,
                    adversarial.Checks.Count,
                    adversarial.Checks.Select(static check => check.Id).Order(StringComparer.Ordinal).ToArray(),
                    adversarialArtifact.Sha256),
                NebiusLiveEvidence: new NebiusSummary(
                    "live",
                    nebius.CompletedAtUtc,
                    nebius.DeploymentFingerprintSha256,
                    nebius.RemoteStageCount,
                    nebius.EvidenceItemCount,
                    nebius.ValidatedCitationCount,
                    manifestArtifact.Sha256,
                    passArtifact.Sha256));

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            Console.WriteLine(json);
            if (!string.IsNullOrWhiteSpace(options.OutputPath))
                await WriteAtomicAsync(options.OutputPath, json + Environment.NewLine).ConfigureAwait(false);
            return 0;
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or IOException or UnauthorizedAccessException or JsonException)
        {
            var failure = new FailureSummary(1, false, SanitizeFailure(exception.Message));
            Console.Error.WriteLine(JsonSerializer.Serialize(failure, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            return 1;
        }
    }

    private static void ValidateEvaluator(
        int schemaVersion,
        DateTimeOffset generatedAt,
        bool overallPassed,
        IReadOnlyList<EvalCheck>? checks,
        IReadOnlyCollection<string> requiredIds,
        string description)
    {
        if (schemaVersion != 1)
            throw new InvalidDataException($"The {description} schema version is unsupported.");
        if (generatedAt == default)
            throw new InvalidDataException($"The {description} generation timestamp is invalid.");
        if (checks is null)
            throw new InvalidDataException($"The {description} checks are missing.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var check in checks)
        {
            if (string.IsNullOrWhiteSpace(check.Id) || check.Id.Length > 128 || !ids.Add(check.Id))
                throw new InvalidDataException($"The {description} contains an invalid or duplicate check id.");
            if (check.Detail is null || check.Detail.Length > 4096)
                throw new InvalidDataException($"The {description} contains an invalid check detail.");
        }

        if (ids.Count != requiredIds.Count || requiredIds.Any(required => !ids.Contains(required)))
            throw new InvalidDataException($"The {description} does not contain the exact required check set.");
        if (!overallPassed || checks.Any(static check => !check.Passed))
            throw new InvalidDataException($"The {description} did not pass all required checks.");
    }

    private static Artifact ReadArtifact(string path, string description)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"A {description} path is required.");

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDataException($"The {description} path is invalid.");
        }

        try
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumArtifactLength)
                throw new InvalidDataException($"The {description} is missing or exceeds the verifier size limit.");
            var bytes = File.ReadAllBytes(fullPath);
            return new Artifact(bytes, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"The {description} could not be read.");
        }
    }

    private static T Deserialize<T>(byte[] bytes, string description)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaximumJsonDepth
            });
            RejectDuplicatePropertyNames(document.RootElement, description);
            return JsonSerializer.Deserialize<T>(bytes, StrictJson)
                   ?? throw new InvalidDataException($"The {description} is empty or invalid.");
        }
        catch (JsonException)
        {
            throw new InvalidDataException($"The {description} contains invalid or unsupported JSON.");
        }
    }

    private static void RejectDuplicatePropertyNames(JsonElement element, string description)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException($"The {description} contains duplicate JSON property names.");
                RejectDuplicatePropertyNames(property.Value, description);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                RejectDuplicatePropertyNames(item, description);
        }
    }

    private static async Task WriteAtomicAsync(string path, string content)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllTextAsync(temporary, content).ConfigureAwait(false);
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static Options ParseArguments(string[] args)
    {
        if (args.Length is not 4 and not 6)
            throw new ArgumentException("Expected four evidence paths and optional --output <path>. Use --help for usage.");
        if (args.Length == 6 && (!string.Equals(args[4], "--output", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(args[5])))
            throw new ArgumentException("Optional arguments must be --output <path>.");
        return new Options(args[0], args[1], args[2], args[3], args.Length == 6 ? args[5] : null);
    }

    private static string SanitizeFailure(string message)
    {
        var singleLine = (message ?? "Evidence verification failed.").ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= 512 ? singleLine : singleLine[..512];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: Nvidea.JudgingEvidenceVerifier <positive-eval.json> <adversarial-eval.json> <nebius-manifest.json> <nebius-pass.json> [--output <summary.json>]");
        Console.WriteLine("Verifies exact evaluator check sets, PASS state, bounded strict JSON, duplicate-property rejection, SHA-256 artifact hashes, and matching Nebius live deployment evidence.");
        Console.WriteLine("The emitted summary contains no artifact paths, evaluator details, credentials, provider errors, research payloads, or secrets.");
    }

    private sealed record Options(string PositivePath, string AdversarialPath, string NebiusManifestPath, string NebiusPassPath, string? OutputPath);
    private sealed record Artifact(byte[] Bytes, string Sha256);
    private sealed record EvalCheck(string Id, bool Passed, string? Detail);
    private sealed record PositiveEvidence(int SchemaVersion, DateTimeOffset GeneratedAt, bool OverallPassed, IReadOnlyList<EvalCheck>? Checks, JsonElement Metrics);
    private sealed record AdversarialEvidence(int SchemaVersion, DateTimeOffset GeneratedAt, bool OverallPassed, IReadOnlyList<EvalCheck>? Checks);
    private sealed record EvidenceBoundary(bool SyntheticEvidenceVerified, bool LiveNebiusEvidenceVerified, IReadOnlyList<string> ClaimsExcluded);
    private sealed record EvaluatorSummary(string EvidenceClass, DateTimeOffset GeneratedAt, int CheckCount, IReadOnlyList<string> CheckIds, string ArtifactSha256);
    private sealed record NebiusSummary(string EvidenceClass, DateTimeOffset CompletedAt, string DeploymentFingerprintSha256, int RemoteStageCount, int EvidenceItemCount, int ValidatedCitationCount, string ManifestSha256, string PassEvidenceSha256);
    private sealed record JudgeEvidenceSummary(int SchemaVersion, bool OverallPassed, EvidenceBoundary EvidenceBoundary, EvaluatorSummary PositiveEvaluator, EvaluatorSummary AdversarialEvaluator, NebiusSummary NebiusLiveEvidence);
    private sealed record FailureSummary(int SchemaVersion, bool OverallPassed, string Failure);
}
