namespace Nvidea.Core.Browser;

/// <summary>
/// Bounds transient browser-managed download bytes before a payload reaches verified quarantine.
/// Playwright owns its staging files until the download completes, so this guard polls both the
/// NVIDEA-owned Playwright DownloadsPath and the quarantine .partial copy and cancels the browser
/// transfer if either crosses its configured limit.
/// </summary>
public sealed record BrowserDownloadStagingOptions(
    long MaxStagingBytes = 128L * 1024L * 1024L,
    long MaxPartialBytes = 128L * 1024L * 1024L,
    int PollIntervalMilliseconds = 50,
    int MaxStartupReclaimEntries = 2_048)
{
    internal void Validate()
    {
        if (MaxStagingBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxStagingBytes), "Browser staging quota must be positive.");
        if (MaxPartialBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxPartialBytes), "Partial-copy quota must be positive.");
        if (PollIntervalMilliseconds is < 10 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(PollIntervalMilliseconds), "Polling interval must be between 10 and 1000 milliseconds.");
        if (MaxStartupReclaimEntries is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(MaxStartupReclaimEntries), "Startup reclaim entry limit must be between 1 and 100000.");
    }
}

public sealed record BrowserDownloadStagingReclaimResult(int FilesDeleted, long BytesDeleted);

public sealed class BrowserDownloadStagingGuard
{
    private readonly BrowserDownloadStagingOptions _options;

    public BrowserDownloadStagingGuard(string stateDirectory, BrowserDownloadStagingOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        _options = options ?? new BrowserDownloadStagingOptions();
        _options.Validate();

        var stateRoot = Path.GetFullPath(stateDirectory);
        StagingDirectory = Path.Combine(stateRoot, "browser-downloads", "browser-staging");
    }

    public string StagingDirectory { get; }

    /// <summary>
    /// Reclaims crash-leftover Playwright staging files before Chromium is launched. This method
    /// must only be called while no browser context is active. It deliberately refuses recursive
    /// cleanup, reparse points, or unexpectedly large directory populations instead of broadening
    /// the deletion boundary. A failed cleanup therefore blocks browser startup rather than risking
    /// deletion outside NVIDEA-owned transient state.
    /// </summary>
    public BrowserDownloadStagingReclaimResult ReclaimStartupLeftovers()
    {
        Directory.CreateDirectory(StagingDirectory);
        RejectReparsePoint(StagingDirectory, "Browser staging root is a reparse point; startup cleanup refused.");

        var parent = Directory.GetParent(StagingDirectory)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent))
            RejectReparsePoint(parent, "Browser download state root is a reparse point; startup cleanup refused.");

        var entries = Directory.EnumerateFileSystemEntries(StagingDirectory, "*", SearchOption.TopDirectoryOnly)
            .Take(_options.MaxStartupReclaimEntries + 1)
            .ToArray();

        if (entries.Length > _options.MaxStartupReclaimEntries)
            throw new InvalidOperationException("Browser staging contains too many startup leftovers to reclaim safely.");

        long bytes = 0;
        foreach (var entry in entries)
        {
            var fullPath = Path.GetFullPath(entry);
            if (!IsStrictChildPath(StagingDirectory, fullPath))
                throw new InvalidOperationException("Browser staging cleanup encountered a path outside the owned staging directory.");

            var attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.Directory) != 0)
                throw new InvalidOperationException("Browser staging cleanup encountered an unexpected directory and refused recursive deletion.");
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Browser staging cleanup encountered a reparse point and refused deletion.");

            checked { bytes += new FileInfo(fullPath).Length; }
        }

        foreach (var entry in entries)
            File.Delete(entry);

        return new BrowserDownloadStagingReclaimResult(entries.Length, bytes);
    }

    /// <summary>
    /// Runs a Playwright save operation while bounding browser-managed staging bytes and the
    /// quarantine partial copy. The source cancel callback is invoked before a quota exception is
    /// surfaced so the browser does not keep receiving bytes after NVIDEA has rejected the transfer.
    /// </summary>
    public async Task RunBoundedAsync(
        string partialPath,
        Func<Task> operation,
        Func<Task> cancelSourceAsync,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partialPath))
            throw new ArgumentException("Partial path is required.", nameof(partialPath));
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(cancelSourceAsync);

        Directory.CreateDirectory(StagingDirectory);
        var operationTask = operation();

        try
        {
            while (!operationTask.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfQuotaExceeded(partialPath);

                var delay = Task.Delay(_options.PollIntervalMilliseconds, cancellationToken);
                var completed = await Task.WhenAny(operationTask, delay).ConfigureAwait(false);
                if (ReferenceEquals(completed, operationTask))
                    break;
            }

            await operationTask.ConfigureAwait(false);
            ThrowIfQuotaExceeded(partialPath);
        }
        catch (BrowserDownloadQuotaExceededException)
        {
            await CancelBestEffortAsync(cancelSourceAsync).ConfigureAwait(false);
            await ObserveBestEffortAsync(operationTask).ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CancelBestEffortAsync(cancelSourceAsync).ConfigureAwait(false);
            await ObserveBestEffortAsync(operationTask).ConfigureAwait(false);
            throw;
        }
    }

    internal (long StagingBytes, long PartialBytes) Measure(string partialPath)
    {
        long stagingBytes = 0;
        if (Directory.Exists(StagingDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(StagingDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    checked { stagingBytes += new FileInfo(path).Length; }
                }
                catch (FileNotFoundException)
                {
                    // Playwright can rename/remove its temporary file between enumeration and stat.
                }
            }
        }

        long partialBytes = 0;
        try
        {
            if (File.Exists(partialPath))
                partialBytes = new FileInfo(partialPath).Length;
        }
        catch (FileNotFoundException)
        {
        }

        return (stagingBytes, partialBytes);
    }

    private void ThrowIfQuotaExceeded(string partialPath)
    {
        var (stagingBytes, partialBytes) = Measure(partialPath);
        if (stagingBytes > _options.MaxStagingBytes)
            throw new BrowserDownloadQuotaExceededException("Browser download exceeded the transient Playwright staging quota and was cancelled.");
        if (partialBytes > _options.MaxPartialBytes)
            throw new BrowserDownloadQuotaExceededException("Browser download exceeded the in-progress quarantine partial quota and was cancelled.");
    }

    private static bool IsStrictChildPath(string root, string candidate)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return candidate.StartsWith(normalizedRoot, comparison) && candidate.Length > normalizedRoot.Length;
    }

    private static void RejectReparsePoint(string path, string message)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException(message);
    }

    private static async Task CancelBestEffortAsync(Func<Task> cancelSourceAsync)
    {
        try { await cancelSourceAsync().ConfigureAwait(false); }
        catch { }
    }

    private static async Task ObserveBestEffortAsync(Task operationTask)
    {
        try { await operationTask.ConfigureAwait(false); }
        catch { }
    }
}
