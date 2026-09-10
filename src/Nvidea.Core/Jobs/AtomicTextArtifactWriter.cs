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

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"{description} path must be a valid file path in an existing directory.");
        }

        var parent = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            throw new InvalidOperationException($"{description} path must point into an existing directory.");

        if (string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)) || Directory.Exists(fullPath))
            throw new InvalidOperationException($"{description} path must include a file name and must not target a directory.");

        return fullPath;
    }

    /// <summary>
    /// Proves that the destination's parent directory accepts the same create/write/flush/delete
    /// operations required by <see cref="Write"/> without creating, truncating, or replacing the
    /// final artifact. A randomized sibling probe is always removed best-effort.
    /// </summary>
    public static string ValidateWritableDestination(string path, string description = "Artifact")
    {
        var fullPath = ValidateDestination(path, description);
        var parent = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);
        var probePath = Path.Combine(parent, $".{fileName}.{Guid.NewGuid():N}.probe.tmp");

        try
        {
            using (var stream = new FileStream(
                       probePath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 1,
                       FileOptions.WriteThrough))
            {
                stream.WriteByte(0x4e);
                stream.Flush(flushToDisk: true);
            }

            File.Delete(probePath);
            return fullPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new InvalidOperationException($"{description} destination is not writable.");
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                    File.Delete(probePath);
            }
            catch
            {
                // Best-effort cleanup only. The caller receives the original validation failure.
            }
        }
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
