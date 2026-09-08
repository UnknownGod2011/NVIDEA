using System.Collections.Concurrent;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Browser;

public sealed record BrowserDownloadSnapshotItem(
    Guid DownloadId,
    string SourceHost,
    string SuggestedFileName,
    BrowserDownloadState State,
    long LengthBytes,
    string Sha256,
    DateTimeOffset CreatedAt);

public sealed record BrowserDownloadSnapshot(
    IReadOnlyList<BrowserDownloadSnapshotItem> RetainedDownloads,
    long RetainedBytes,
    long MaxRetainedBytes,
    long MaxSingleDownloadBytes,
    int PendingRecoveryCount,
    DateTimeOffset CapturedAt)
{
    public bool HasPendingRecovery => PendingRecoveryCount > 0;
}

internal static class BrowserDownloadStateSynchronization
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates =
        new(StringComparer.OrdinalIgnoreCase);

    public static SemaphoreSlim GetGate(string metadataPath)
    {
        if (string.IsNullOrWhiteSpace(metadataPath))
            throw new ArgumentException("Download metadata path is required.", nameof(metadataPath));

        var fullPath = Path.GetFullPath(metadataPath);
        return Gates.GetOrAdd(fullPath, static _ => new SemaphoreSlim(1, 1));
    }
}

/// <summary>
/// Strictly read-only view over durable browser-download metadata. It never performs crash
/// recovery, deletes transient files, verifies payload hashes, exports/discards payloads, creates
/// approval grants, or initializes Playwright. Stable retained entries are surfaced only when the
/// corresponding quarantine payload exists and its on-disk length matches protected metadata.
/// </summary>
internal sealed class BrowserDownloadSnapshotReader
{
    private const string ProtectionPurpose = "browser-download-metadata-v1";
    private const long MaxMetadataBytes = 4L * 1024L * 1024L;
    private const int MaxMetadataRecords = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _payloadDirectory;
    private readonly string _metadataPath;
    private readonly ILocalStateProtector? _protector;
    private readonly BrowserDownloadQuarantineOptions _options;
    private readonly SemaphoreSlim _gate;

    public BrowserDownloadSnapshotReader(
        string browserStateDirectory,
        BrowserDownloadQuarantineOptions? options = null,
        ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(browserStateDirectory))
            throw new ArgumentException("Browser state directory is required.", nameof(browserStateDirectory));

        _options = options ?? new BrowserDownloadQuarantineOptions();
        _options.Validate();

        var browserRoot = Path.GetFullPath(browserStateDirectory);
        var downloadRoot = Path.Combine(browserRoot, "browser-downloads");
        _payloadDirectory = Path.Combine(downloadRoot, "quarantine");
        _metadataPath = Path.Combine(downloadRoot, "downloads.json");
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
        _gate = BrowserDownloadStateSynchronization.GetGate(_metadataPath);
    }

    internal SemaphoreSlim SynchronizationGate => _gate;

    public async Task<BrowserDownloadSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (records.Count > MaxMetadataRecords)
                throw new InvalidDataException("Download metadata contains too many records for a passive snapshot.");

            var stable = new List<BrowserDownloadSnapshotItem>();
            var seenIds = new HashSet<Guid>();
            long retainedBytes = 0;
            var pendingRecoveryCount = 0;

            foreach (var record in records)
            {
                ValidateRecord(record);
                if (!seenIds.Add(record.DownloadId))
                    throw new InvalidDataException("Download metadata contains duplicate ids.");

                if (record.State == BrowserDownloadState.Receiving)
                {
                    pendingRecoveryCount++;
                    continue;
                }

                if (record.State is not (BrowserDownloadState.Ready or BrowserDownloadState.Exported))
                    continue;

                if (record.LengthBytes is not long length || length < 0 || string.IsNullOrWhiteSpace(record.Sha256))
                    throw new InvalidDataException("Retained download metadata is incomplete.");
                if (length > _options.MaxSingleDownloadBytes)
                    throw new InvalidDataException("Retained download metadata exceeds the configured per-file quota.");
                if (!IsSha256(record.Sha256))
                    throw new InvalidDataException("Retained download metadata contains an invalid SHA-256 identity.");

                var payloadPath = Path.Combine(_payloadDirectory, record.DownloadId.ToString("N") + ".payload");
                if (!File.Exists(payloadPath))
                    throw new InvalidDataException("Retained download payload is missing; open the trusted browser runtime to recover or inspect quarantine state.");

                var observedLength = new FileInfo(payloadPath).Length;
                if (observedLength != length)
                    throw new InvalidDataException("Retained download payload length does not match protected metadata.");

                if (length > _options.MaxRetainedBytes - retainedBytes)
                    throw new InvalidDataException("Retained download metadata exceeds the configured quarantine quota.");
                retainedBytes += length;

                stable.Add(new BrowserDownloadSnapshotItem(
                    record.DownloadId,
                    record.SourceUri.IdnHost,
                    record.SuggestedFileName,
                    record.State,
                    length,
                    record.Sha256.ToLowerInvariant(),
                    record.CreatedAt));
            }

            return new BrowserDownloadSnapshot(
                stable.OrderByDescending(static item => item.CreatedAt).ToArray(),
                retainedBytes,
                _options.MaxRetainedBytes,
                _options.MaxSingleDownloadBytes,
                pendingRecoveryCount,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<BrowserDownloadRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataPath))
            return Array.Empty<BrowserDownloadRecord>();

        var info = new FileInfo(_metadataPath);
        if (info.Length > MaxMetadataBytes)
            throw new InvalidDataException("Download metadata is too large for a passive snapshot.");

        var persisted = await File.ReadAllBytesAsync(_metadataPath, cancellationToken).ConfigureAwait(false);
        if (persisted.LongLength > MaxMetadataBytes)
            throw new InvalidDataException("Download metadata grew beyond the passive snapshot limit while being read.");

        LocalStatePayload payload;
        if (_protector is not null)
            payload = LocalStateEnvelope.Decode(persisted, _protector, ProtectionPurpose);
        else if (LocalStateEnvelope.HasProtectedHeader(persisted))
            throw new InvalidDataException("Download metadata is protected but no local-state protector was configured.");
        else
            payload = new LocalStatePayload(persisted, false);

        if (payload.Plaintext.LongLength > MaxMetadataBytes)
            throw new InvalidDataException("Decoded download metadata is too large for a passive snapshot.");

        try
        {
            return JsonSerializer.Deserialize<List<BrowserDownloadRecord>>(payload.Plaintext, JsonOptions)
                ?? new List<BrowserDownloadRecord>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Download metadata contains invalid data.", ex);
        }
    }

    private static void ValidateRecord(BrowserDownloadRecord record)
    {
        if (record.DownloadId == Guid.Empty)
            throw new InvalidDataException("Download metadata contains an empty id.");
        if (!record.SourceUri.IsAbsoluteUri ||
            (record.SourceUri.Scheme != Uri.UriSchemeHttp && record.SourceUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidDataException("Download metadata contains an invalid source URI.");
        if (string.IsNullOrWhiteSpace(record.SuggestedFileName) ||
            !string.Equals(record.SuggestedFileName, Path.GetFileName(record.SuggestedFileName), StringComparison.Ordinal) ||
            record.SuggestedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException("Download metadata contains an unsafe filename.");
        if (!Enum.IsDefined(record.State))
            throw new InvalidDataException("Download metadata contains an unknown state.");
    }

    private static bool IsSha256(string value)
    {
        if (value.Length != 64)
            return false;

        foreach (var ch in value)
        {
            if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')))
                return false;
        }

        return true;
    }
}
