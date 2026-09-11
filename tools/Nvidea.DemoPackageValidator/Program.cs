using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nvidea.DemoPackageValidator;

internal static class Program
{
    private const int MaximumManifestBytes = 128 * 1024;
    private const int MaximumJsonDepth = 32;
    private const int HardMaximumDurationSeconds = 180;

    private static readonly string[] RequiredBeatIds =
    [
        "invoke-context",
        "durable-memory",
        "tavily-research",
        "browser-verify",
        "permission-gate",
        "nebius-background",
        "architecture-proof"
    ];

    private static readonly string[] RequiredRepositoryAssets =
    [
        "README.md",
        "LICENSE",
        "docs/personal-ai-demo-eval.md",
        "docs/personal-ai-adversarial-eval.md",
        "docs/judging-evidence-verifier.md",
        "tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj",
        "tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj",
        "tools/Nvidea.JudgingEvidenceVerifier/Nvidea.JudgingEvidenceVerifier.csproj"
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
            var repoRoot = Path.GetFullPath(options.RepositoryRoot);
            if (!Directory.Exists(repoRoot))
                throw new InvalidDataException("Repository root does not exist.");

            var manifestBytes = ReadBoundedManifest(options.ManifestPath);
            var manifest = DeserializeManifest(manifestBytes);
            var checks = Validate(repoRoot, manifest);
            var passed = checks.All(static check => check.Passed);

            var summary = new ValidationSummary(
                SchemaVersion: 1,
                OverallPassed: passed,
                ManifestSha256: Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant(),
                DeclaredMaximumDurationSeconds: manifest.MaxDurationSeconds,
                PlannedDurationSeconds: manifest.Beats.Sum(static beat => beat.DurationSeconds),
                BeatCount: manifest.Beats.Count,
                Checks: checks);

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            Console.WriteLine(json);
            if (!string.IsNullOrWhiteSpace(options.OutputPath))
                await WriteAtomicAsync(options.OutputPath, json + Environment.NewLine).ConfigureAwait(false);
            return passed ? 0 : 1;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            var failure = new FailureSummary(1, false, Sanitize(exception.Message));
            Console.Error.WriteLine(JsonSerializer.Serialize(failure, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            return 1;
        }
    }

    private static IReadOnlyList<ValidationCheck> Validate(string repoRoot, DemoManifest manifest)
    {
        var checks = new List<ValidationCheck>();

        checks.Add(Check("schema-version", manifest.SchemaVersion == 1, "Manifest schema version must be 1."));
        checks.Add(Check("duration-cap", manifest.MaxDurationSeconds is > 0 and <= HardMaximumDurationSeconds && manifest.Beats.Sum(static beat => beat.DurationSeconds) <= manifest.MaxDurationSeconds, "Declared and planned demo duration must remain within 180 seconds."));

        var beatIds = new HashSet<string>(StringComparer.Ordinal);
        var beatIdsValid = manifest.Beats.Count > 0 && manifest.Beats.All(beat =>
            !string.IsNullOrWhiteSpace(beat.Id) && beat.Id.Length <= 96 && beat.DurationSeconds > 0 && beat.DurationSeconds <= HardMaximumDurationSeconds && beatIds.Add(beat.Id));
        checks.Add(Check("beat-identities", beatIdsValid, "Beat IDs must be unique, bounded, and each beat must have a positive duration."));
        checks.Add(Check("required-demo-beats", beatIdsValid && RequiredBeatIds.All(beatIds.Contains), "Manifest must cover invocation/context, memory, Tavily, browser verification, permission gate, Nebius background work, and architecture proof."));

        var allPaths = manifest.Beats.SelectMany(static beat => beat.FeaturePaths)
            .Concat(manifest.Beats.SelectMany(static beat => beat.Evidence).Select(static evidence => evidence.Path))
            .Concat(manifest.Commands.Select(static command => command.ProjectPath))
            .ToArray();
        checks.Add(Check("safe-relative-paths", allPaths.All(IsSafeRelativePath), "Every referenced path must stay inside the repository."));
        checks.Add(Check("referenced-paths-exist", allPaths.Where(IsSafeRelativePath).All(path => PathExists(repoRoot, path)), "Every feature, evidence, and command project path must exist."));
        checks.Add(Check("required-repository-assets", RequiredRepositoryAssets.All(path => PathExists(repoRoot, path)), "Required README, license, evaluator docs, and evaluator projects must exist."));

        var evidenceClassesValid = manifest.Beats.SelectMany(static beat => beat.Evidence).All(static evidence => evidence.EvidenceClass is "synthetic" or "local-live" or "provider-live" or "documentation");
        checks.Add(Check("evidence-classes", evidenceClassesValid, "Evidence must be explicitly classified as synthetic, local-live, provider-live, or documentation."));

        var liveClaimsSafe = manifest.Beats.All(static beat => !beat.RequiresProviderLive || beat.Evidence.Any(static evidence => evidence.EvidenceClass == "provider-live"));
        checks.Add(Check("provider-live-claims", liveClaimsSafe, "Any beat claiming provider-live behavior must cite provider-live evidence rather than synthetic-only evidence."));

        var commandsValid = manifest.Commands.Count > 0 && manifest.Commands.All(command =>
            !string.IsNullOrWhiteSpace(command.Label) &&
            !string.IsNullOrWhiteSpace(command.Command) &&
            IsSafeRelativePath(command.ProjectPath) &&
            command.Command.Contains(command.ProjectPath.Replace('/', Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
            command.Command.Contains(command.ProjectPath, StringComparison.OrdinalIgnoreCase));
        checks.Add(Check("demo-commands", commandsValid, "Every demo command must reference its declared project path."));

        var secretFree = EnumerateManifestStrings(manifest).All(static value => !LooksLikeSecret(value));
        checks.Add(Check("manifest-secret-scan", secretFree, "Manifest must not contain private keys, bearer tokens, API-key assignments, or common secret prefixes."));

        return checks;
    }

    private static IEnumerable<string> EnumerateManifestStrings(DemoManifest manifest)
    {
        yield return manifest.Title;
        foreach (var beat in manifest.Beats)
        {
            yield return beat.Id;
            yield return beat.Label;
            yield return beat.JudgeClaim;
            foreach (var path in beat.FeaturePaths) yield return path;
            foreach (var evidence in beat.Evidence)
            {
                yield return evidence.EvidenceClass;
                yield return evidence.Path;
                yield return evidence.Claim;
            }
        }
        foreach (var command in manifest.Commands)
        {
            yield return command.Label;
            yield return command.Command;
            yield return command.ProjectPath;
        }
    }

    private static bool LooksLikeSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var lowered = value.ToLowerInvariant();
        return lowered.Contains("-----begin private key-----", StringComparison.Ordinal) ||
               lowered.Contains("authorization: bearer ", StringComparison.Ordinal) ||
               lowered.Contains("api_key=", StringComparison.Ordinal) ||
               lowered.Contains("apikey=", StringComparison.Ordinal) ||
               lowered.Contains("sk-", StringComparison.Ordinal);
    }

    private static bool IsSafeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return false;
        var normalized = path.Replace('\\', '/');
        return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).All(static segment => segment != "..");
    }

    private static bool PathExists(string repoRoot, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(repoRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !string.Equals(fullPath, repoRoot, StringComparison.OrdinalIgnoreCase))
            return false;
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    private static byte[] ReadBoundedManifest(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists || info.Length <= 0 || info.Length > MaximumManifestBytes)
            throw new InvalidDataException("Demo manifest is missing or exceeds the size limit.");
        return File.ReadAllBytes(fullPath);
    }

    private static DemoManifest DeserializeManifest(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = MaximumJsonDepth });
        RejectDuplicateProperties(document.RootElement);
        return JsonSerializer.Deserialize<DemoManifest>(bytes, StrictJson) ?? throw new InvalidDataException("Demo manifest is empty or invalid.");
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Demo manifest contains duplicate JSON property names.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
        }
    }

    private static ValidationCheck Check(string id, bool passed, string requirement) => new(id, passed, requirement);

    private static Options ParseArguments(string[] args)
    {
        if (args.Length is not 2 and not 4)
            throw new ArgumentException("Expected <repo-root> <manifest.json> and optional --output <path>. Use --help for usage.");
        if (args.Length == 4 && (args[2] != "--output" || string.IsNullOrWhiteSpace(args[3])))
            throw new ArgumentException("Optional arguments must be --output <path>.");
        return new Options(args[0], args[1], args.Length == 4 ? args[3] : null);
    }

    private static async Task WriteAtomicAsync(string path, string content)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllTextAsync(temporary, content).ConfigureAwait(false);
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string Sanitize(string message)
    {
        var value = (message ?? "Demo package validation failed.").ReplaceLineEndings(" ").Trim();
        return value.Length <= 512 ? value : value[..512];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: Nvidea.DemoPackageValidator <repo-root> <demo-manifest.json> [--output <summary.json>]");
        Console.WriteLine("Validates the <=3-minute judging package, referenced repository assets, demo commands, and synthetic/live evidence boundaries without network access.");
    }

    private sealed record Options(string RepositoryRoot, string ManifestPath, string? OutputPath);
    private sealed record DemoManifest(int SchemaVersion, string Title, int MaxDurationSeconds, IReadOnlyList<DemoBeat> Beats, IReadOnlyList<DemoCommand> Commands);
    private sealed record DemoBeat(string Id, string Label, int DurationSeconds, string JudgeClaim, bool RequiresProviderLive, IReadOnlyList<string> FeaturePaths, IReadOnlyList<EvidenceReference> Evidence);
    private sealed record EvidenceReference(string EvidenceClass, string Path, string Claim);
    private sealed record DemoCommand(string Label, string ProjectPath, string Command);
    private sealed record ValidationCheck(string Id, bool Passed, string Requirement);
    private sealed record ValidationSummary(int SchemaVersion, bool OverallPassed, string ManifestSha256, int DeclaredMaximumDurationSeconds, int PlannedDurationSeconds, int BeatCount, IReadOnlyList<ValidationCheck> Checks);
    private sealed record FailureSummary(int SchemaVersion, bool OverallPassed, string Failure);
}
