namespace Nvidea.Core.Jobs;

/// <summary>
/// Persists already-redacted text artifacts using a same-directory temporary file followed by
/// atomic replacement. The destination directory must already exist; no directory is created
/// implicitly. This keeps judging/evidence artifacts from being left partially written after a
/// crash or process termination during serialization.
/// </summary>
public static class AtomicTextArtifactWriter
{
    public static string ValidateDestination(string path, string description = "Artifact")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            throw new InvalidOperationException($"{description} path must point into an existing directory.");

        if (string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
            throw new InvalidOperationException($"{description} path must include a file name.");

        return fullPath;
    }

    public static void Write(string path, string contents, string description = "Artifact")
    {
        ArgumentNullException.ThrowIfNull(contents);
        var fullPath = ValidateDestination(path, description);
        var parent = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);
        var tempPath = Path.Combine(parent, $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
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
                // Best-effort cleanup only. Never mask the original persistence failure.
            }
        }
    }
}
