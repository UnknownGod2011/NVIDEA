using System.Text.Json;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Machine-readable, redacted evidence emitted only after a live research contract probe has
/// completed successfully. It deliberately excludes provider ids, payloads, URLs, secret
/// references, credentials, PEM material, and research text.
/// </summary>
public sealed record NebiusResearchPassEvidence(
    string SchemaVersion,
    string DeploymentFingerprintSha256,
    DateTimeOffset CompletedAtUtc,
    int RemoteStageCount,
    int EvidenceItemCount,
    int ValidatedCitationCount)
{
    public const string CurrentSchemaVersion = "nvidea.nebius.research-pass.v1";
}

public static class NebiusResearchPassEvidenceBuilder
{
    public static NebiusResearchPassEvidence Build(
        string deploymentFingerprintSha256,
        DateTimeOffset completedAtUtc,
        int remoteStageCount,
        int evidenceItemCount,
        int validatedCitationCount)
    {
        if (string.IsNullOrWhiteSpace(deploymentFingerprintSha256)
            || deploymentFingerprintSha256.Length != 64
            || deploymentFingerprintSha256.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Deployment fingerprint must be exactly 64 hexadecimal characters.", nameof(deploymentFingerprintSha256));
        }
        if (completedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("PASS evidence completion timestamp must be UTC.", nameof(completedAtUtc));
        if (remoteStageCount <= 0 || remoteStageCount > 16)
            throw new ArgumentOutOfRangeException(nameof(remoteStageCount), "Remote stage count must be between 1 and 16.");
        if (evidenceItemCount <= 0 || evidenceItemCount > 10000)
            throw new ArgumentOutOfRangeException(nameof(evidenceItemCount), "Evidence item count must be between 1 and 10000.");
        if (validatedCitationCount <= 0 || validatedCitationCount > evidenceItemCount)
            throw new ArgumentOutOfRangeException(nameof(validatedCitationCount), "Validated citation count must be positive and cannot exceed evidence count.");

        return new NebiusResearchPassEvidence(
            NebiusResearchPassEvidence.CurrentSchemaVersion,
            deploymentFingerprintSha256.ToLowerInvariant(),
            completedAtUtc,
            remoteStageCount,
            evidenceItemCount,
            validatedCitationCount);
    }

    public static string ToJson(NebiusResearchPassEvidence evidence, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return JsonSerializer.Serialize(evidence, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = indented
        });
    }

    /// <summary>
    /// Persists already-redacted evidence with temp-file + same-directory atomic replacement.
    /// The destination directory must exist; this method never creates directories implicitly.
    /// </summary>
    public static void PersistAtomically(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        var fullPath = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            throw new InvalidOperationException("Evidence path must point into an existing directory.");

        var fileName = Path.GetFileName(fullPath);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Evidence path must include a file name.");

        var tempPath = Path.Combine(parent, $".{fileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best-effort cleanup only. Never hide the original persistence failure.
            }
        }
    }
}