using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Nvidea.DemoPackageValidator.Tests;

public sealed class DemoPackageValidatorTests
{
    private static readonly string[] RequiredAssets =
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

    [Fact]
    public async Task Valid_manifest_passes()
    {
        using var fixture = new ValidatorFixture();
        Assert.Equal(0, await fixture.RunAsync(fixture.CreateManifest()).ConfigureAwait(false));
    }

    [Fact]
    public async Task Null_required_arrays_fail_closed()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        manifest["beats"] = null;
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Path_traversal_is_rejected()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        var beats = (List<Dictionary<string, object?>>)manifest["beats"]!;
        beats[0]["featurePaths"] = new[] { "../outside.txt" };
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Windows_drive_relative_path_is_rejected()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        var beats = (List<Dictionary<string, object?>>)manifest["beats"]!;
        beats[0]["featurePaths"] = new[] { "C:relative.txt" };
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Duration_overflow_is_rejected()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        var beats = (List<Dictionary<string, object?>>)manifest["beats"]!;
        beats[0]["durationSeconds"] = 181;
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Duplicate_beat_ids_are_rejected()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        var beats = (List<Dictionary<string, object?>>)manifest["beats"]!;
        beats[1]["id"] = beats[0]["id"];
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Missing_required_asset_is_rejected()
    {
        using var fixture = new ValidatorFixture();
        File.Delete(Path.Combine(fixture.Root, "LICENSE"));
        Assert.Equal(1, await fixture.RunAsync(fixture.CreateManifest()).ConfigureAwait(false));
    }

    [Fact]
    public async Task Matching_project_substring_cannot_bypass_blank_command_label()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        var commands = (List<Dictionary<string, object?>>)manifest["commands"]!;
        commands[0]["label"] = " ";
        commands[0]["command"] = "dotnet run --project tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj";
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Secret_markers_are_rejected()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        manifest["title"] = "demo sk-super-secret";
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    [Fact]
    public async Task Provider_live_claim_requires_provider_live_evidence()
    {
        using var fixture = new ValidatorFixture();
        var manifest = fixture.CreateManifest();
        var beats = (List<Dictionary<string, object?>>)manifest["beats"]!;
        beats[5]["requiresProviderLive"] = true;
        Assert.Equal(1, await fixture.RunAsync(manifest).ConfigureAwait(false));
    }

    private sealed class ValidatorFixture : IDisposable
    {
        public ValidatorFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "nvidea-demo-validator-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            foreach (var asset in RequiredAssets)
                WriteFixtureFile(asset);
            Directory.CreateDirectory(Path.Combine(Root, "src/Nvidea.Core/Desktop"));
            Directory.CreateDirectory(Path.Combine(Root, "src/Nvidea.Core/Memory"));
            Directory.CreateDirectory(Path.Combine(Root, "src/Nvidea.Core/Research"));
            Directory.CreateDirectory(Path.Combine(Root, "src/Nvidea.Core/Browser"));
            Directory.CreateDirectory(Path.Combine(Root, "src/Nvidea.Core/Capabilities"));
            WriteFixtureFile("docs/evidence.json");
        }

        public string Root { get; }

        public Dictionary<string, object?> CreateManifest()
        {
            var beats = new List<Dictionary<string, object?>>();
            var ids = new[] { "invoke-context", "durable-memory", "tavily-research", "browser-verify", "permission-gate", "nebius-background", "architecture-proof" };
            var paths = new[] { "src/Nvidea.Core/Desktop", "src/Nvidea.Core/Memory", "src/Nvidea.Core/Research", "src/Nvidea.Core/Browser", "src/Nvidea.Core/Capabilities", "src/Nvidea.Core/Research", "README.md" };
            foreach (var pair in ids.Zip(paths))
            {
                beats.Add(new Dictionary<string, object?>
                {
                    ["id"] = pair.First,
                    ["label"] = pair.First,
                    ["durationSeconds"] = 20,
                    ["judgeClaim"] = "Deterministic demo claim",
                    ["requiresProviderLive"] = false,
                    ["featurePaths"] = new[] { pair.Second },
                    ["evidence"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["evidenceClass"] = "synthetic",
                            ["path"] = "docs/evidence.json",
                            ["claim"] = "Synthetic validation only"
                        }
                    }
                });
            }

            return new Dictionary<string, object?>
            {
                ["schemaVersion"] = 1,
                ["title"] = "NVIDEA deterministic demo",
                ["maxDurationSeconds"] = 180,
                ["beats"] = beats,
                ["commands"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["label"] = "Positive evaluator",
                        ["projectPath"] = "tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj",
                        ["command"] = "dotnet run --project tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj"
                    }
                }
            };
        }

        public async Task<int> RunAsync(Dictionary<string, object?> manifest)
        {
            var manifestPath = Path.Combine(Root, "demo.json");
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest)).ConfigureAwait(false);

            var assembly = Assembly.Load("Nvidea.DemoPackageValidator");
            var program = assembly.GetType("Nvidea.DemoPackageValidator.Program", throwOnError: true)!;
            var main = program.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("Validator Main method was not found.");
            var result = main.Invoke(null, new object[] { new[] { Root, manifestPath } });
            return await ((Task<int>)result!).ConfigureAwait(false);
        }

        private void WriteFixtureFile(string relativePath)
        {
            var fullPath = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, "fixture");
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
