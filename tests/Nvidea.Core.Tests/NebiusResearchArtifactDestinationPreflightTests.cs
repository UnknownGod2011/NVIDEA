using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchArtifactDestinationPreflightTests
{
    [Fact]
    public void Validate_WithNoConfiguredOutputs_IsSuccessfulAndSideEffectFree()
    {
        var report = NebiusResearchArtifactDestinationPreflight.Validate(_ => null);

        Assert.False(report.RedactedManifestConfigured);
        Assert.False(report.PassEvidenceConfigured);
        Assert.Equal(0, report.WritableDestinationCount);
    }

    [Fact]
    public void Validate_ProbesBothConfiguredOutputsWithoutCreatingFinalFiles()
    {
        var root = CreateRoot();
        try
        {
            var manifest = Path.Combine(root, "manifest.json");
            var pass = Path.Combine(root, "pass.json");
            var values = new Dictionary<string, string?>
            {
                [NebiusResearchArtifactDestinationPreflight.RedactedManifestEnvironmentVariable] = manifest,
                [NebiusResearchArtifactDestinationPreflight.PassEvidenceEnvironmentVariable] = pass
            };

            var report = NebiusResearchArtifactDestinationPreflight.Validate(name => values.GetValueOrDefault(name));

            Assert.True(report.RedactedManifestConfigured);
            Assert.True(report.PassEvidenceConfigured);
            Assert.Equal(2, report.WritableDestinationCount);
            Assert.False(File.Exists(manifest));
            Assert.False(File.Exists(pass));
            Assert.Empty(Directory.GetFiles(root, "*.probe.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Validate_RejectsSameManifestAndPassDestination()
    {
        var root = CreateRoot();
        try
        {
            var path = Path.Combine(root, "evidence.json");
            var values = new Dictionary<string, string?>
            {
                [NebiusResearchArtifactDestinationPreflight.RedactedManifestEnvironmentVariable] = path,
                [NebiusResearchArtifactDestinationPreflight.PassEvidenceEnvironmentVariable] = path
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                NebiusResearchArtifactDestinationPreflight.Validate(name => values.GetValueOrDefault(name)));

            Assert.Contains("must be different files", exception.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Validate_DoesNotModifyExistingArtifacts()
    {
        var root = CreateRoot();
        try
        {
            var manifest = Path.Combine(root, "manifest.json");
            var pass = Path.Combine(root, "pass.json");
            File.WriteAllText(manifest, "old-manifest");
            File.WriteAllText(pass, "old-pass");
            var values = new Dictionary<string, string?>
            {
                [NebiusResearchArtifactDestinationPreflight.RedactedManifestEnvironmentVariable] = manifest,
                [NebiusResearchArtifactDestinationPreflight.PassEvidenceEnvironmentVariable] = pass
            };

            NebiusResearchArtifactDestinationPreflight.Validate(name => values.GetValueOrDefault(name));

            Assert.Equal("old-manifest", File.ReadAllText(manifest));
            Assert.Equal("old-pass", File.ReadAllText(pass));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-artifact-preflight-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
