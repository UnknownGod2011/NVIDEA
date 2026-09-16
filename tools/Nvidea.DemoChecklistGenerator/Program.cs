using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nvidea.DemoChecklistGenerator;

internal static class Program
{
    private const int MaxManifestBytes = 131072;
    private const int MaxDepth = 32;
    private const int MaxDemoSeconds = 180;

    private static readonly JsonSerializerOptions StrictJson = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = MaxDepth,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length is < 1 or > 3 || (args.Length == 3 && args[1] != "--output"))
                throw new ArgumentException("Usage: Nvidea.DemoChecklistGenerator <manifest.json> [--output <checklist.md>]");

            var manifestPath = Path.GetFullPath(args[0]);
            var outputPath = args.Length == 3 ? Path.GetFullPath(args[2]) : null;
            var manifestBytes = ReadManifest(manifestPath);
            var manifest = ParseManifest(manifestBytes);
            ValidateManifest(manifest);
            var markdown = Render(manifest);

            if (outputPath is null)
                Console.Write(markdown);
            else
                await AtomicWriteAsync(outputPath, markdown);

            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            Console.Error.WriteLine(ex.Message.ReplaceLineEndings(" ").Trim());
            return 1;
        }
    }

    private static byte[] ReadManifest(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length <= 0 || file.Length > MaxManifestBytes)
            throw new InvalidDataException("Manifest is missing, empty, or too large.");
        return File.ReadAllBytes(file.FullName);
    }

    private static Manifest ParseManifest(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = MaxDepth
        });
        RejectDuplicateProperties(document.RootElement);
        return JsonSerializer.Deserialize<Manifest>(bytes, StrictJson)
            ?? throw new InvalidDataException("Manifest is invalid.");
    }

    private static void ValidateManifest(Manifest manifest)
    {
        if (manifest.SchemaVersion != 2)
            throw new InvalidDataException("Checklist generation requires demo-package schema v2.");
        if (string.IsNullOrWhiteSpace(manifest.Title) || manifest.Beats is null || manifest.Beats.Count == 0 || manifest.Beats.Count > 64)
            throw new InvalidDataException("Manifest shape is invalid.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;
        foreach (var beat in manifest.Beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Id) || !ids.Add(beat.Id) || string.IsNullOrWhiteSpace(beat.Label) ||
                beat.DurationSeconds <= 0 || beat.DurationSeconds > MaxDemoSeconds ||
                beat.PreflightDependencies is null || beat.PreflightDependencies.Count == 0 || beat.PreflightDependencies.Any(string.IsNullOrWhiteSpace) ||
                beat.ExpectedSessionMilestones is null || beat.ExpectedSessionMilestones.Count > 16 || beat.ExpectedSessionMilestones.Any(string.IsNullOrWhiteSpace) ||
                string.IsNullOrWhiteSpace(beat.FallbackPolicy))
                throw new InvalidDataException($"Beat '{beat.Id}' has an invalid execution contract.");
            checked { total += beat.DurationSeconds; }
        }

        if (manifest.MaxDurationSeconds is <= 0 or > MaxDemoSeconds || total > manifest.MaxDurationSeconds)
            throw new InvalidDataException("Demo duration exceeds its declared safe limit.");
    }

    private static string Render(Manifest manifest)
    {
        var total = manifest.Beats.Sum(beat => beat.DurationSeconds);
        var buffer = new StringBuilder();
        buffer.AppendLine("# Generated Judge Demo Checklist");
        buffer.AppendLine();
        buffer.AppendLine("> Generated from `docs/demo-package.json`. Do not hand-edit this artifact; change the schema-v2 manifest and regenerate it.");
        buffer.AppendLine();
        buffer.AppendLine($"**Plan:** {Escape(manifest.Title)} — {total}s planned / {manifest.MaxDurationSeconds}s maximum / {manifest.MaxDurationSeconds - total}s contingency.");
        buffer.AppendLine();

        for (var index = 0; index < manifest.Beats.Count; index++)
        {
            var beat = manifest.Beats[index];
            buffer.AppendLine($"## {index + 1}. {Escape(beat.Label)} ({beat.DurationSeconds}s)");
            buffer.AppendLine();
            buffer.AppendLine("**Preflight**");
            foreach (var dependency in beat.PreflightDependencies)
                buffer.AppendLine($"- [ ] {Escape(dependency)}");
            buffer.AppendLine();
            buffer.AppendLine("**Expected session evidence**");
            if (beat.ExpectedSessionMilestones.Count == 0)
                buffer.AppendLine("- [ ] No runtime milestone expected; do not manufacture one.");
            else
                foreach (var milestone in beat.ExpectedSessionMilestones)
                    buffer.AppendLine($"- [ ] `{EscapeCode(milestone)}` is visible only after genuine production observation.");
            buffer.AppendLine();
            buffer.AppendLine($"**Fail-closed fallback:** {Escape(beat.FallbackPolicy)}");
            buffer.AppendLine();
        }

        buffer.AppendLine("## Take acceptance gate");
        buffer.AppendLine();
        buffer.AppendLine("- [ ] Every beat's preflight was satisfied before its action.");
        buffer.AppendLine("- [ ] Every declared runtime milestone was genuinely observed; no synthetic/documentation evidence was relabeled as live proof.");
        buffer.AppendLine("- [ ] Any failed beat followed its manifest fallback instead of being manually rescued or narrated as success.");
        buffer.AppendLine($"- [ ] Final cut is at most {manifest.MaxDurationSeconds} seconds.");
        return buffer.ToString();
    }

    private static string Escape(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Trim();
    private static string EscapeCode(string value) => value.Replace("`", "\\`").ReplaceLineEndings(" ").Trim();

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException("Duplicate JSON property.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                RejectDuplicateProperties(item);
        }
    }

    private static async Task AtomicWriteAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllTextAsync(temporary, content);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private sealed record Manifest(int SchemaVersion, string Title, int MaxDurationSeconds, IReadOnlyList<Beat> Beats);
    private sealed record Beat(string Id, string Label, int DurationSeconds, IReadOnlyList<string> ExpectedSessionMilestones, IReadOnlyList<string> PreflightDependencies, string FallbackPolicy);
}
