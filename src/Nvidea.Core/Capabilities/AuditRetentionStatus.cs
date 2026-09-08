using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Capabilities;

/// <summary>
/// Payload-free audit retention telemetry. This intentionally exposes only bounded storage/accounting
/// information and the protected pruning digest; it never exposes audit event contents, summaries,
/// targets, URLs, filenames, prompts, or metadata.
/// </summary>
public sealed record AuditRetentionStatus(
    int ActiveSegmentIndex,
    int ActiveEventCount,
    long ActiveSegmentBytes,
    int RetainedArchivedSegments,
    long RetainedArchivedBytes,
    int MaxArchivedSegments,
    long MaxArchivedBytes,
    int PrunedThroughSegmentIndex,
    long PrunedEventCount,
    string PrunedAnchorDigest,
    bool HasPrunedHistory)
{
    public long RetainedTotalBytes => checked(ActiveSegmentBytes + RetainedArchivedBytes);
}

internal sealed class AuditRetentionStatusReader
{
    private const string ManifestPurpose = "audit-segment-manifest-v1";
    private const int ManifestVersion = 1;
    private const string CommittedState = "committed";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _basePath;
    private readonly string _manifestPath;
    private readonly AuditRetentionPolicy _retention;
    private readonly ILocalStateProtector? _protector;

    public AuditRetentionStatusReader(
        string path,
        ILocalStateProtector? protector,
        AuditRetentionPolicy retention)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit path is required.", nameof(path));
        ArgumentNullException.ThrowIfNull(retention);
        retention.Validate();

        _basePath = Path.GetFullPath(path);
        _manifestPath = _basePath + ".segments";
        _retention = retention;
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task<AuditRetentionStatus> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_manifestPath))
        {
            return new AuditRetentionStatus(
                ActiveSegmentIndex: 1,
                ActiveEventCount: 0,
                ActiveSegmentBytes: MeasureFileIfPresent(_basePath) + MeasureFileIfPresent(_basePath + ".seal"),
                RetainedArchivedSegments: 0,
                RetainedArchivedBytes: 0,
                MaxArchivedSegments: _retention.MaxArchivedSegments,
                MaxArchivedBytes: _retention.MaxArchivedBytes,
                PrunedThroughSegmentIndex: 0,
                PrunedEventCount: 0,
                PrunedAnchorDigest: string.Empty,
                HasPrunedHistory: false);
        }

        var manifest = await ReadManifestAsync(cancellationToken).ConfigureAwait(false);
        ValidateStatusManifest(manifest);

        long archivedBytes = 0;
        long archivedEvents = 0;
        foreach (var anchor in manifest.Archived)
        {
            cancellationToken.ThrowIfCancellationRequested();
            archivedBytes = checked(archivedBytes + MeasureRequiredFile(SegmentPath(anchor.Index)));
            if (!string.IsNullOrEmpty(anchor.SealSha256))
                archivedBytes = checked(archivedBytes + MeasureRequiredFile(SegmentPath(anchor.Index) + ".seal"));
            archivedEvents = checked(archivedEvents + anchor.EventCount);
        }

        var activePath = SegmentPath(manifest.ActiveIndex);
        var activeBytes = checked(MeasureFileIfPresent(activePath) + MeasureFileIfPresent(activePath + ".seal"));

        // The segmented trail validates event counts and recovers pending retention work before this
        // reader is called by BoundedSegmentedAuditTrail. Derive the active count without exposing
        // event payloads by reading only the JSONL line count of the current segment file.
        var activeEventCount = await CountLinesAsync(activePath, cancellationToken).ConfigureAwait(false);

        return new AuditRetentionStatus(
            manifest.ActiveIndex,
            activeEventCount,
            activeBytes,
            manifest.Archived.Count,
            archivedBytes,
            _retention.MaxArchivedSegments,
            _retention.MaxArchivedBytes,
            manifest.PrunedThroughIndex,
            manifest.PrunedEventCount,
            manifest.PrunedAnchorDigest ?? string.Empty,
            manifest.PrunedThroughIndex > 0);
    }

    private async Task<StatusManifest> ReadManifestAsync(CancellationToken cancellationToken)
    {
        var persisted = await File.ReadAllBytesAsync(_manifestPath, cancellationToken).ConfigureAwait(false);
        byte[] plaintext;
        if (_protector is not null)
        {
            LocalStatePayload decoded;
            try
            {
                decoded = LocalStateEnvelope.Decode(persisted, _protector, ManifestPurpose);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(persisted);
            }

            if (!decoded.WasProtected)
            {
                CryptographicOperations.ZeroMemory(decoded.Plaintext);
                throw new InvalidDataException("Audit segment manifest exists without the required protected local-state envelope.");
            }
            plaintext = decoded.Plaintext;
        }
        else
        {
            plaintext = persisted;
        }

        try
        {
            return JsonSerializer.Deserialize<StatusManifest>(plaintext, JsonOptions)
                ?? throw new InvalidDataException("Audit segment manifest was empty after deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Audit segment manifest contains malformed JSON.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static void ValidateStatusManifest(StatusManifest manifest)
    {
        if (manifest.Version != ManifestVersion)
            throw new InvalidDataException("Unsupported audit segment-manifest version.");
        if (!string.Equals(manifest.State, CommittedState, StringComparison.Ordinal))
            throw new InvalidDataException("Audit retention status requires committed segment state.");
        if (manifest.ActiveIndex < 1 || manifest.PrunedThroughIndex < 0 || manifest.PrunedThroughIndex >= manifest.ActiveIndex)
            throw new InvalidDataException("Audit segment manifest contains an invalid retention boundary.");
        if (manifest.Archived.Count != manifest.ActiveIndex - manifest.PrunedThroughIndex - 1)
            throw new InvalidDataException("Audit segment manifest contains inconsistent retained segment state.");

        if (manifest.PrunedThroughIndex == 0)
        {
            if (manifest.PrunedEventCount != 0 || !string.IsNullOrEmpty(manifest.PrunedAnchorDigest))
                throw new InvalidDataException("Audit segment manifest has pruning evidence without a pruned boundary.");
        }
        else if (manifest.PrunedEventCount < 1 || !IsSha256Hex(manifest.PrunedAnchorDigest))
        {
            throw new InvalidDataException("Audit segment manifest has an invalid protected pruning digest.");
        }
    }

    private string SegmentPath(int index) =>
        index == 1 ? _basePath : _basePath + $".segment-{index:D6}.jsonl";

    private static long MeasureRequiredFile(string path) =>
        File.Exists(path)
            ? new FileInfo(path).Length
            : throw new InvalidDataException("A retained audit segment referenced by the protected manifest is missing.");

    private static long MeasureFileIfPresent(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

    private static async Task<int> CountLinesAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return 0;

        var count = 0;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is not null)
            count = checked(count + 1);
        return count;
    }

    private static bool IsSha256Hex(string? value)
    {
        if (value is null || value.Length != 64)
            return false;
        foreach (var c in value)
            if (!Uri.IsHexDigit(c))
                return false;
        return true;
    }

    private sealed record StatusManifest(
        int Version,
        string State,
        int ActiveIndex,
        IReadOnlyList<StatusAnchor> Archived,
        int? PendingActiveIndex,
        int PrunedThroughIndex = 0,
        long PrunedEventCount = 0,
        string? PrunedAnchorDigest = null,
        IReadOnlyList<int>? PendingDeleteIndices = null);

    private sealed record StatusAnchor(
        int Index,
        int EventCount,
        string DataSha256,
        string SealSha256);
}
