using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nvidea.DemoChecklistGenerator;

internal static class Program
{
    private const int MaxManifestBytes = 131072;
    private const int MaxDepth = 32;
    private const int MaxDemoSeconds = 180;
    private static readonly JsonSerializerOptions StrictJson = new(JsonSerializerDefaults.Web) { MaxDepth = MaxDepth, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length is < 1 or > 3 || (args.Length == 3 && args[1] != "--output")) throw new ArgumentException("Usage: Nvidea.DemoChecklistGenerator <manifest.json> [--output <checklist.md>]");
            var bytes = ReadManifest(Path.GetFullPath(args[0]));
            var manifest = ParseManifest(bytes);
            ValidateManifest(manifest);
            var markdown = Render(manifest);
            if (args.Length == 3) await AtomicWriteAsync(Path.GetFullPath(args[2]), markdown); else Console.Write(markdown);
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException or JsonException or NotSupportedException or OverflowException)
        {
            Console.Error.WriteLine(ex.Message.ReplaceLineEndings(" ").Trim());
            return 1;
        }
    }

    private static byte[] ReadManifest(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length <= 0 || file.Length > MaxManifestBytes) throw new InvalidDataException("Manifest is missing, empty, or too large.");
        return File.ReadAllBytes(file.FullName);
    }

    private static Manifest ParseManifest(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = MaxDepth });
        RejectDuplicateProperties(document.RootElement);
        return JsonSerializer.Deserialize<Manifest>(bytes, StrictJson) ?? throw new InvalidDataException("Manifest is invalid.");
    }

    private static void ValidateManifest(Manifest manifest)
    {
        if (manifest.SchemaVersion != 2) throw new InvalidDataException("Checklist generation requires demo-package schema v2.");
        if (string.IsNullOrWhiteSpace(manifest.Title) || manifest.Beats is null || manifest.Commands is null || manifest.Beats.Count == 0 || manifest.Beats.Count > 64) throw new InvalidDataException("Manifest shape is invalid.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;
        foreach (var beat in manifest.Beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Id) || !ids.Add(beat.Id) || string.IsNullOrWhiteSpace(beat.Label) || string.IsNullOrWhiteSpace(beat.JudgeClaim) || beat.DurationSeconds <= 0 || beat.DurationSeconds > MaxDemoSeconds || beat.PreflightDependencies is null || beat.PreflightDependencies.Count == 0 || beat.PreflightDependencies.Any(string.IsNullOrWhiteSpace) || beat.ExpectedSessionMilestones is null || beat.ExpectedSessionMilestones.Count > 16 || beat.ExpectedSessionMilestones.Any(string.IsNullOrWhiteSpace) || string.IsNullOrWhiteSpace(beat.FallbackPolicy) || beat.FeaturePaths is null || beat.Evidence is null) throw new InvalidDataException($"Beat '{beat.Id}' has an invalid execution contract.");
            checked { total += beat.DurationSeconds; }
        }
        if (manifest.MaxDurationSeconds is <= 0 or > MaxDemoSeconds || total > manifest.MaxDurationSeconds) throw new InvalidDataException("Demo duration exceeds its declared safe limit.");
    }

    private static string Render(Manifest manifest)
    {
        var total = manifest.Beats.Sum(beat => beat.DurationSeconds);
        var b = new StringBuilder();
        b.AppendLine("# Generated Judge Demo Checklist").AppendLine();
        b.AppendLine("> Generated from `docs/demo-package.json`. Do not hand-edit this artifact; change the schema-v2 manifest and regenerate it.").AppendLine();
        b.AppendLine($"**Plan:** {Escape(manifest.Title)} — {total}s planned / {manifest.MaxDurationSeconds}s maximum / {manifest.MaxDurationSeconds - total}s contingency.").AppendLine();
        for (var i = 0; i < manifest.Beats.Count; i++)
        {
            var beat = manifest.Beats[i];
            b.AppendLine($"## {i + 1}. {Escape(beat.Label)} ({beat.DurationSeconds}s)").AppendLine();
            b.AppendLine($"**Judge claim:** {Escape(beat.JudgeClaim)}").AppendLine();
            b.AppendLine("**Preflight**");
            foreach (var dependency in beat.PreflightDependencies) b.AppendLine($"- [ ] {Escape(dependency)}");
            b.AppendLine().AppendLine("**Expected session evidence**");
            if (beat.ExpectedSessionMilestones.Count == 0) b.AppendLine("- [ ] No runtime milestone expected; do not manufacture one.");
            else foreach (var milestone in beat.ExpectedSessionMilestones) b.AppendLine($"- [ ] `{EscapeCode(milestone)}` is visible only after genuine production observation.");
            b.AppendLine().AppendLine($"**Fail-closed fallback:** {Escape(beat.FallbackPolicy)}").AppendLine();
        }
        b.AppendLine("## Take acceptance gate").AppendLine();
        b.AppendLine("- [ ] Every beat's preflight was satisfied before its action.");
        b.AppendLine("- [ ] Every declared runtime milestone was genuinely observed; no synthetic/documentation evidence was relabeled as live proof.");
        b.AppendLine("- [ ] Any failed beat followed its manifest fallback instead of being manually rescued or narrated as success.");
        b.AppendLine($"- [ ] Final cut is at most {manifest.MaxDurationSeconds} seconds.");
        return b.ToString();
    }

    private static string Escape(string value) => value.ReplaceLineEndings(" ").Replace("|", "\\|").Trim();
    private static string EscapeCode(string value) => value.Replace("`", "\\`").ReplaceLineEndings(" ").Trim();

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject()) { if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON property."); RejectDuplicateProperties(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    private static async Task AtomicWriteAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try { await File.WriteAllTextAsync(temporary, content); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed record Manifest(int SchemaVersion, string Title, int MaxDurationSeconds, IReadOnlyList<Beat> Beats, IReadOnlyList<Command> Commands);
    private sealed record Beat(string Id, string Label, int DurationSeconds, string JudgeClaim, bool RequiresProviderLive, IReadOnlyList<string> ExpectedSessionMilestones, IReadOnlyList<string> PreflightDependencies, string FallbackPolicy, IReadOnlyList<string> FeaturePaths, IReadOnlyList<Evidence> Evidence);
    private sealed record Evidence(string EvidenceClass, string Path, string Claim);
    private sealed record Command(string Label, string ProjectPath, string Command);
}
