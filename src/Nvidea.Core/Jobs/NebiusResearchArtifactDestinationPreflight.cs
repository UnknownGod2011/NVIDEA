namespace Nvidea.Core.Jobs;

public sealed record NebiusResearchArtifactDestinationPreflightReport(
    bool RedactedManifestConfigured,
    bool PassEvidenceConfigured,
    int WritableDestinationCount);

/// <summary>
/// Performs a zero-network, non-destructive check of the optional judging-artifact destinations.
/// The final manifest/PASS files are never created, truncated, or replaced by this check.
/// </summary>
public static class NebiusResearchArtifactDestinationPreflight
{
    public const string RedactedManifestEnvironmentVariable = "NVIDEA_LIVE_REDACTED_MANIFEST_PATH";
    public const string PassEvidenceEnvironmentVariable = "NVIDEA_LIVE_PASS_EVIDENCE_PATH";

    public static NebiusResearchArtifactDestinationPreflightReport ValidateFromEnvironment() =>
        Validate(Environment.GetEnvironmentVariable);

    public static NebiusResearchArtifactDestinationPreflightReport Validate(Func<string, string?> environmentReader)
    {
        ArgumentNullException.ThrowIfNull(environmentReader);

        var manifestPath = ReadOptionalPath(environmentReader, RedactedManifestEnvironmentVariable);
        var passPath = ReadOptionalPath(environmentReader, PassEvidenceEnvironmentVariable);

        if (manifestPath is not null && passPath is not null && PathsEqual(manifestPath, passPath))
        {
            throw new InvalidOperationException(
                "The redacted manifest and PASS evidence destinations must be different files.");
        }

        var writableCount = 0;
        if (manifestPath is not null)
        {
            AtomicTextArtifactWriter.ValidateWritableDestination(
                manifestPath,
                "Redacted deployment manifest");
            writableCount++;
        }

        if (passPath is not null)
        {
            AtomicTextArtifactWriter.ValidateWritableDestination(
                passPath,
                "PASS evidence");
            writableCount++;
        }

        return new NebiusResearchArtifactDestinationPreflightReport(
            manifestPath is not null,
            passPath is not null,
            writableCount);
    }

    private static string? ReadOptionalPath(Func<string, string?> environmentReader, string name)
    {
        var raw = environmentReader(name)?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.Length > NebiusResearchLiveConfigurationLoader.MaximumEnvironmentValueLength || raw.Any(char.IsControl))
            throw new InvalidOperationException($"Judging-artifact configuration '{name}' is invalid.");

        return AtomicTextArtifactWriter.ValidateDestination(raw, name);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
