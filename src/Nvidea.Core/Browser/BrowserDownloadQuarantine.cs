using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Browser;

public enum BrowserDownloadState
{
    Receiving,
    Ready,
    Interrupted,
    Exported
}

public sealed record BrowserDownloadRecord(
    Guid DownloadId,
    Uri SourceUri,
    string SuggestedFileName,
    BrowserDownloadState State,
    long? LengthBytes,
    string? Sha256,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ExportedPath = null,
    string? Failure = null);

public sealed record BrowserDownloadExportReceipt(
    Guid DownloadId,
    string DestinationPath,
    long LengthBytes,
    string Sha256,
    DateTimeOffset ExportedAt);

/// <summary>
/// Durable download boundary for untrusted browser payloads. Browser bytes first land in an
/// NVIDEA-owned quarantine directory and are not considered user-visible files until an explicit
/// handoff copies a verified payload to a caller-selected destination.
/// </summary>
public sealed class BrowserDownloadQuarantine
{
    private const string ProtectionPurpose = "browser-download-metadata-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _root;
    private readonly string _payloadDirectory;
    private readonly string _metadataPath;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BrowserDownloadQuarantine(string stateDirectory, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        var stateRoot = Path.GetFullPath(stateDirectory);
        _root = Path.Combine(stateRoot, "browser-downloads");
        _payloadDirectory = Path.Combine(_root, "quarantine");
        _metadataPath = Path.Combine(_root, "downloads.json");
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task<BrowserDownloadRecord> CaptureAsync(
        Uri sourceUri,
        string? suggestedFileName,
        Func<string, CancellationToken, Task> saveToPathAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentNullException.ThrowIfNull(saveToPathAsync);
        ValidateSourceUri(sourceUri);

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var safeName = SanitizeFileName(suggestedFileName);
        var partialPath = GetPartialPath(id);
        var payloadPath = GetPayloadPath(id);
        var receiving = new BrowserDownloadRecord(
            id,
            sourceUri,
            safeName,
            BrowserDownloadState.Receiving,
            null,
            null,
            now,
            now);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_payloadDirectory);
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            records.Add(receiving);
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        try
        {
            await saveToPathAsync(partialPath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(partialPath))
                throw new InvalidDataException("Browser download did not produce a quarantine payload.");

            var length = new FileInfo(partialPath).Length;
            var sha256 = await ComputeSha256Async(partialPath, cancellationToken).ConfigureAwait(false);
            File.Move(partialPath, payloadPath, false);

            return await UpdateStateAsync(
                id,
                BrowserDownloadState.Ready,
                length,
                sha256,
                exportedPath: null,
                failure: null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            TryDelete(partialPath);
            await MarkInterruptedBestEffortAsync(id, ex, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<IReadOnlyList<BrowserDownloadRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var changed = false;

            // A process can stop after durable Receiving metadata is written but before completion.
            // On restart such entries are never promoted implicitly; they become interrupted.
            for (var i = 0; i < records.Count; i++)
            {
                if (records[i].State != BrowserDownloadState.Receiving)
                    continue;

                records[i] = records[i] with
                {
                    State = BrowserDownloadState.Interrupted,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Failure = "Download was interrupted before verified quarantine completion."
                };
                TryDelete(GetPartialPath(records[i].DownloadId));
                changed = true;
            }

            if (changed)
                await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);

            return records.OrderByDescending(x => x.CreatedAt).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<BrowserDownloadRecord?> GetAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        if (downloadId == Guid.Empty)
            throw new ArgumentException("Download id is required.", nameof(downloadId));

        var records = await ListAsync(cancellationToken).ConfigureAwait(false);
        return records.FirstOrDefault(x => x.DownloadId == downloadId);
    }

    public async Task<BrowserDownloadExportReceipt> ExportAsync(
        Guid downloadId,
        string destinationDirectory,
        bool userApproved,
        CancellationToken cancellationToken = default)
    {
        if (downloadId == Guid.Empty)
            throw new ArgumentException("Download id is required.", nameof(downloadId));
        if (!userApproved)
            throw new UnauthorizedAccessException("Explicit user approval is required before a quarantined download leaves NVIDEA state.");
        if (string.IsNullOrWhiteSpace(destinationDirectory))
            throw new ArgumentException("Destination directory is required.", nameof(destinationDirectory));

        var destinationRoot = Path.GetFullPath(destinationDirectory);
        if (!Directory.Exists(destinationRoot))
            throw new DirectoryNotFoundException("The approved destination directory must already exist.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = records.FindIndex(x => x.DownloadId == downloadId);
            if (index < 0)
                throw new KeyNotFoundException($"Download '{downloadId}' was not found.");

            var record = records[index];
            if (record.State is not (BrowserDownloadState.Ready or BrowserDownloadState.Exported))
                throw new InvalidOperationException($"Download is {record.State} and cannot be exported.");
            if (record.LengthBytes is null || string.IsNullOrWhiteSpace(record.Sha256))
                throw new InvalidDataException("Download metadata is incomplete and cannot authorize export.");

            var sourcePath = GetPayloadPath(downloadId);
            await VerifyPayloadAsync(sourcePath, record, cancellationToken).ConfigureAwait(false);

            var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, record.SuggestedFileName));
            EnsureDescendant(destinationRoot, destinationPath);
            if (File.Exists(destinationPath))
                throw new IOException("A file with the approved download name already exists at the destination.");

            var tempPath = destinationPath + ".nvidea-export-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await CopyAsync(sourcePath, tempPath, cancellationToken).ConfigureAwait(false);
                await VerifyPayloadAsync(tempPath, record, cancellationToken).ConfigureAwait(false);
                File.Move(tempPath, destinationPath, false);
            }
            finally
            {
                TryDelete(tempPath);
            }

            var updated = record with
            {
                State = BrowserDownloadState.Exported,
                ExportedPath = destinationPath,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            records[index] = updated;
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);

            return new BrowserDownloadExportReceipt(
                downloadId,
                destinationPath,
                record.LengthBytes.Value,
                record.Sha256,
                updated.UpdatedAt);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<BrowserDownloadRecord> UpdateStateAsync(
        Guid id,
        BrowserDownloadState state,
        long? length,
        string? sha256,
        string? exportedPath,
        string? failure,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = records.FindIndex(x => x.DownloadId == id);
            if (index < 0)
                throw new InvalidDataException("Download metadata disappeared while the payload was being quarantined.");

            var updated = records[index] with
            {
                State = state,
                LengthBytes = length,
                Sha256 = sha256,
                ExportedPath = exportedPath,
                Failure = failure,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            records[index] = updated;
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
            return updated;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task MarkInterruptedBestEffortAsync(Guid id, Exception error, CancellationToken cancellationToken)
    {
        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
                var index = records.FindIndex(x => x.DownloadId == id);
                if (index < 0)
                    return;

                records[index] = records[index] with
                {
                    State = BrowserDownloadState.Interrupted,
                    Failure = error is OperationCanceledException ? "Download was cancelled." : "Download failed before verified quarantine completion.",
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch
        {
            // Preserve the original download failure. A leftover Receiving record is converted to
            // Interrupted by List/Get after restart and can never be exported in that state.
        }
    }

    private async Task<IReadOnlyList<BrowserDownloadRecord>> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataPath))
            return Array.Empty<BrowserDownloadRecord>();

        var persisted = await File.ReadAllBytesAsync(_metadataPath, cancellationToken).ConfigureAwait(false);
        LocalStatePayload payload;
        if (_protector is not null)
            payload = LocalStateEnvelope.Decode(persisted, _protector, ProtectionPurpose);
        else if (LocalStateEnvelope.HasProtectedHeader(persisted))
            throw new InvalidDataException("Download metadata is protected but no local-state protector was configured.");
        else
            payload = new LocalStatePayload(persisted, false);

        List<BrowserDownloadRecord> records;
        try
        {
            records = JsonSerializer.Deserialize<List<BrowserDownloadRecord>>(payload.Plaintext, JsonOptions)
                ?? new List<BrowserDownloadRecord>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Download metadata contains invalid data.", ex);
        }

        ValidateRecords(records);
        if (_protector is not null && !payload.WasProtected)
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
        return records;
    }

    private async Task PersistUnlockedAsync(IReadOnlyList<BrowserDownloadRecord> records, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_payloadDirectory);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(records.OrderBy(x => x.CreatedAt), JsonOptions);
        var persisted = _protector is null
            ? plaintext
            : LocalStateEnvelope.Encode(plaintext, _protector, ProtectionPurpose);
        var temp = _metadataPath + ".tmp";
        await File.WriteAllBytesAsync(temp, persisted, cancellationToken).ConfigureAwait(false);
        File.Move(temp, _metadataPath, true);
    }

    private static void ValidateRecords(IReadOnlyList<BrowserDownloadRecord> records)
    {
        var ids = new HashSet<Guid>();
        foreach (var record in records)
        {
            if (record.DownloadId == Guid.Empty || !ids.Add(record.DownloadId))
                throw new InvalidDataException("Download metadata contains an empty or duplicate id.");
            ValidateSourceUri(record.SourceUri);
            if (!string.Equals(record.SuggestedFileName, SanitizeFileName(record.SuggestedFileName), StringComparison.Ordinal))
                throw new InvalidDataException("Download metadata contains an unsafe file name.");
            if (record.LengthBytes < 0)
                throw new InvalidDataException("Download metadata contains a negative length.");
            if (record.Sha256 is not null && (record.Sha256.Length != 64 || record.Sha256.Any(c => !Uri.IsHexDigit(c))))
                throw new InvalidDataException("Download metadata contains an invalid SHA-256 digest.");
        }
    }

    private async Task VerifyPayloadAsync(string path, BrowserDownloadRecord record, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Quarantined download payload is missing.", path);
        var length = new FileInfo(path).Length;
        if (length != record.LengthBytes)
            throw new InvalidDataException("Quarantined download length no longer matches durable metadata.");
        var hash = await ComputeSha256Async(path, cancellationToken).ConfigureAwait(false);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(hash), Convert.FromHexString(record.Sha256!)))
            throw new InvalidDataException("Quarantined download hash no longer matches durable metadata.");
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var sha = SHA256.Create();
        var digest = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(digest);
    }

    private static async Task CopyAsync(string source, string destination, CancellationToken cancellationToken)
    {
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private string GetPartialPath(Guid id) => Path.Combine(_payloadDirectory, id.ToString("N") + ".partial");
    private string GetPayloadPath(Guid id) => Path.Combine(_payloadDirectory, id.ToString("N") + ".payload");

    private static void ValidateSourceUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("Browser downloads must originate from absolute HTTP(S) URLs.", nameof(uri));
    }

    private static string SanitizeFileName(string? candidate)
    {
        var value = (candidate ?? string.Empty).Trim();
        value = value.Replace('\\', '/');
        var slash = value.LastIndexOf('/');
        if (slash >= 0)
            value = value[(slash + 1)..];

        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Where(c => !invalid.Contains(c) && !char.IsControl(c)).ToArray()).Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(safe) || safe is "." or "..")
            safe = "download";
        return safe.Length <= 160 ? safe : safe[..160];
    }

    private static void EnsureDescendant(string directory, string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Download export path escaped the approved destination directory.");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cleanup is best effort; durable metadata still prevents export of incomplete payloads.
        }
    }
}
