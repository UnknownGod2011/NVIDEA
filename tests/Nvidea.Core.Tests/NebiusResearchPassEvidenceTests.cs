using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchPassEvidenceTests
{
    [Fact]
    public void Build_ProducesBoundedRedactedMachineReadableEvidence()
    {
        var evidence = NebiusResearchPassEvidenceBuilder.Build(
            new string('a', 64),
            new DateTimeOffset(2026, 9, 10, 0, 15, 0, TimeSpan.Zero),
            remoteStageCount: 3,
            evidenceItemCount: 7,
            validatedCitationCount: 4);

        var json = NebiusResearchPassEvidenceBuilder.ToJson(evidence, indented: false);

        Assert.Equal(NebiusResearchPassEvidence.CurrentSchemaVersion, evidence.SchemaVersion);
        Assert.Equal(new string('a', 64), evidence.DeploymentFingerprintSha256);
        Assert.Equal(3, evidence.RemoteStageCount);
        Assert.Equal(7, evidence.EvidenceItemCount);
        Assert.Equal(4, evidence.ValidatedCitationCount);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bucket", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payload", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void Build_RejectsInvalidDeploymentFingerprint(string fingerprint)
    {
        Assert.Throws<ArgumentException>(() => NebiusResearchPassEvidenceBuilder.Build(
            fingerprint,
            DateTimeOffset.UtcNow,
            1,
            1,
            1));
    }

    [Fact]
    public void Build_RejectsNonUtcTimestampAndInvalidCounts()
    {
        var fingerprint = new string('b', 64);
        Assert.Throws<ArgumentException>(() => NebiusResearchPassEvidenceBuilder.Build(
            fingerprint,
            new DateTimeOffset(2026, 9, 10, 5, 30, 0, TimeSpan.FromHours(5.5)),
            1,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NebiusResearchPassEvidenceBuilder.Build(
            fingerprint,
            DateTimeOffset.UtcNow,
            0,
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NebiusResearchPassEvidenceBuilder.Build(
            fingerprint,
            DateTimeOffset.UtcNow,
            1,
            1,
            2));
    }

    [Fact]
    public void PersistAtomically_ReplacesExistingEvidenceWithoutLeavingTemporaryFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-pass-evidence-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "pass.json");
            File.WriteAllText(path, "old");

            NebiusResearchPassEvidenceBuilder.PersistAtomically(path, "new-evidence");

            Assert.Equal("new-evidence", File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(root, ".pass.json.*.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PersistAtomically_RequiresExistingParentDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-pass-evidence-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "missing", "pass.json");

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchPassEvidenceBuilder.PersistAtomically(path, "{}"));
    }
}
