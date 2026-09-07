using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Security;

namespace Nvidea.Core.Capabilities;

/// <summary>
/// Bounded audit wrapper that rotates <see cref="JsonLinesAuditTrail"/> into immutable segments.
/// Every archived segment is pinned by a SHA-256 digest of both its data file and tail seal in a
/// locally protected manifest. Rollover uses a pending manifest so a crash before/after the first
/// append in the new segment can be recovered deterministically without rewriting old segments.
/// </summary>
public sealed class SegmentedAuditTrail : IAuditTrail
{
    private const int ManifestVersion = 1;
    private const string ManifestPurpose = "audit-segment-manifest-v1";
    private const string CommittedState = "committed";
    private const string PendingState = "pending";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _basePath;
    private readonly string _manifestPath;
    private readonly int _maxEventsPerSegment;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SegmentedAuditTrail(
        string path,
        int maxEventsPerSegment = 1_000,
        ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit path is required.", nameof(path));
        if (maxEventsPerSegment < 2)
            throw new ArgumentOutOfRangeException(nameof(maxEventsPerSegment), "Audit segments must allow at least two events.");

        _basePath = Path.GetFullPath(path);
        _manifestPath = _basePath + ".segments";
        _maxEventsPerSegment = maxEventsPerSegment;
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_basePath)!);
            var manifest = await LoadAndRecoverManifestAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = await LoadAllEventsAsync(manifest, cancellationToken).ConfigureAwait(false);
            if (snapshot.Events.Any(x => x.EventId == auditEvent.EventId))
                throw new InvalidOperationException($"Audit event '{auditEvent.EventId}' already exists; audit records are append-only.");

            if (snapshot.ActiveCount < _maxEventsPerSegment)
            {
                await CreateSegment(manifest.ActiveIndex).AppendAsync(auditEvent, cancellationToken).ConfigureAwait(false);
                return;
            }

            await RotateAndAppendAsync(manifest, auditEvent, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var manifest = await LoadAndRecoverManifestAsync(cancellationToken).ConfigureAwait(false);
            return (await LoadAllEventsAsync(manifest, cancellationToken).ConfigureAwait(false)).Events;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RotateAndAppendAsync(
        AuditSegmentManifest manifest,
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        var currentIndex = manifest.ActiveIndex;
        var currentPath = SegmentPath(currentIndex);
        var currentTrail = CreateSegment(currentIndex);
        var currentEvents = await currentTrail.ReadAllAsync(cancellationToken).ConfigureAwait(false);
        if (currentEvents.Count < _maxEventsPerSegment)
        {
            await currentTrail.AppendAsync(auditEvent, cancellationToken).ConfigureAwait(false);
            return;
        }

        var anchor = await BuildAnchorAsync(currentIndex, currentEvents.Count, cancellationToken).ConfigureAwait(false);
        var nextIndex = checked(currentIndex + 1);
        var pending = manifest with
        {
            State = PendingState,
            Archived = manifest.Archived.Concat(new[] { anchor }).ToArray(),
            PendingActiveIndex = nextIndex
        };
        ValidateManifest(pending);
        await WriteManifestAsync(pending, cancellationToken).ConfigureAwait(false);

        await CreateSegment(nextIndex).AppendAsync(auditEvent, cancellationToken).ConfigureAwait(false);

        var committed = pending with
        {
            State = CommittedState,
            ActiveIndex = nextIndex,
            PendingActiveIndex = null
        };
        ValidateManifest(committed);
        await WriteManifestAsync(committed, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuditSegmentManifest> LoadAndRecoverManifestAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_manifestPath))
        {
            // Validate the legacy/current base segment before legitimizing it with a manifest.
            await CreateSegment(1).ReadAllAsync(cancellationToken).ConfigureAwait(false);
            var initial = new AuditSegmentManifest(
                ManifestVersion,
                CommittedState,
                1,
                Array.Empty<AuditSegmentAnchor>(),
                null);
            await WriteManifestAsync(initial, cancellationToken).ConfigureAwait(false);
            return initial;
        }

        var manifest = await ReadManifestAsync(cancellationToken).ConfigureAwait(false);
        ValidateManifest(manifest);
        await VerifyArchivedSegmentsAsync(manifest.Archived, cancellationToken).ConfigureAwait(false);

        if (manifest.State == CommittedState)
            return manifest;

        var pendingIndex = manifest.PendingActiveIndex!.Value;
        var pendingPath = SegmentPath(pendingIndex);
        var pendingSealPath = pendingPath + ".seal";
        if (!File.Exists(pendingPath) && !File.Exists(pendingSealPath))
        {
            var restored = new AuditSegmentManifest(
                ManifestVersion,
                CommittedState,
                manifest.ActiveIndex,
                manifest.Archived.Take(manifest.Archived.Count - 1).ToArray(),
                null);
            ValidateManifest(restored);
            await WriteManifestAsync(restored, cancellationToken).ConfigureAwait(false);
            return restored;
        }

        if (!File.Exists(pendingPath))
            throw new InvalidDataException("Pending audit rollover has a seal without its data segment.");

        var pendingEvents = await CreateSegment(pendingIndex).ReadAllAsync(cancellationToken).ConfigureAwait(false);
        if (pendingEvents.Count == 0)
            throw new InvalidDataException("Pending audit rollover created an empty new segment; completion is ambiguous and requires manual inspection.");

        var finalized = manifest with
        {
            State = CommittedState,
            ActiveIndex = pendingIndex,
            PendingActiveIndex = null
        };
        ValidateManifest(finalized);
        await WriteManifestAsync(finalized, cancellationToken).ConfigureAwait(false);
        return finalized;
    }

    private async Task<AuditSnapshot> LoadAllEventsAsync(
        AuditSegmentManifest manifest,
        CancellationToken cancellationToken)
    {
        var all = new List<AuditEvent>();
        var ids = new HashSet<Guid>();

        foreach (var archived in manifest.Archived)
        {
            var events = await CreateSegment(archived.Index).ReadAllAsync(cancellationToken).ConfigureAwait(false);
            if (events.Count != archived.EventCount)
                throw new InvalidDataException($"Archived audit segment {archived.Index} event count no longer matches its protected anchor.");
            AddUnique(all, ids, events);
        }

        var active = await CreateSegment(manifest.ActiveIndex).ReadAllAsync(cancellationToken).ConfigureAwait(false);
        AddUnique(all, ids, active);
        return new AuditSnapshot(all, active.Count);
    }

    private static void AddUnique(List<AuditEvent> destination, HashSet<Guid> ids, IReadOnlyList<AuditEvent> source)
    {
        foreach (var auditEvent in source)
        {
            if (!ids.Add(auditEvent.EventId))
                throw new InvalidDataException($"Audit event '{auditEvent.EventId}' appears in more than one segment.");
            destination.Add(auditEvent);
        }
    }

    private async Task VerifyArchivedSegmentsAsync(
        IReadOnlyList<AuditSegmentAnchor> anchors,
        CancellationToken cancellationToken)
    {
        foreach (var anchor in anchors)
        {
            var path = SegmentPath(anchor.Index);
            if (!File.Exists(path))
                throw new InvalidDataException($"Archived audit segment {anchor.Index} is missing.");

            var dataHash = await ComputeFileHashAsync(path, cancellationToken).ConfigureAwait(false);
            if (!FixedTimeTextEquals(dataHash, anchor.DataSha256))
                throw new InvalidDataException($"Archived audit segment {anchor.Index} data hash does not match its protected anchor.");

            if (!string.IsNullOrEmpty(anchor.SealSha256))
            {
                var sealPath = path + ".seal";
                if (!File.Exists(sealPath))
                    throw new InvalidDataException($"Archived audit segment {anchor.Index} tail seal is missing.");
                var sealHash = await ComputeFileHashAsync(sealPath, cancellationToken).ConfigureAwait(false);
                if (!FixedTimeTextEquals(sealHash, anchor.SealSha256))
                    throw new InvalidDataException($"Archived audit segment {anchor.Index} tail-seal hash does not match its protected anchor.");
            }
        }
    }

    private async Task<AuditSegmentAnchor> BuildAnchorAsync(
        int index,
        int eventCount,
        CancellationToken cancellationToken)
    {
        var path = SegmentPath(index);
        if (!File.Exists(path))
            throw new InvalidDataException($"Audit segment {index} is missing during rotation.");

        var dataHash = await ComputeFileHashAsync(path, cancellationToken).ConfigureAwait(false);
        var sealPath = path + ".seal";
        string sealHash;
        if (_protector is not null)
        {
            if (!File.Exists(sealPath))
                throw new InvalidDataException($"Protected audit segment {index} is missing its tail seal during rotation.");
            sealHash = await ComputeFileHashAsync(sealPath, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            sealHash = File.Exists(sealPath)
                ? await ComputeFileHashAsync(sealPath, cancellationToken).ConfigureAwait(false)
                : string.Empty;
        }

        return new AuditSegmentAnchor(index, eventCount, dataHash, sealHash);
    }

    private JsonLinesAuditTrail CreateSegment(int index) =>
        new(SegmentPath(index), _protector);

    private string SegmentPath(int index) =>
        index == 1 ? _basePath : _basePath + $".segment-{index:D6}.jsonl";

    private async Task<AuditSegmentManifest> ReadManifestAsync(CancellationToken cancellationToken)
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
            return JsonSerializer.Deserialize<AuditSegmentManifest>(plaintext, JsonOptions)
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

    private async Task WriteManifestAsync(AuditSegmentManifest manifest, CancellationToken cancellationToken)
    {
        ValidateManifest(manifest);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        byte[] persisted;
        try
        {
            persisted = _protector is null
                ? plaintext.ToArray()
                : LocalStateEnvelope.Encode(plaintext, _protector, ManifestPurpose);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var tempPath = _manifestPath + ".write-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, persisted, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _manifestPath, overwrite: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(persisted);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static void ValidateManifest(AuditSegmentManifest manifest)
    {
        if (manifest.Version != ManifestVersion)
            throw new InvalidDataException("Unsupported audit segment-manifest version.");
        if (manifest.ActiveIndex < 1)
            throw new InvalidDataException("Audit segment manifest contains an invalid active index.");

        for (var i = 0; i < manifest.Archived.Count; i++)
        {
            var anchor = manifest.Archived[i];
            if (anchor.Index != i + 1 || anchor.EventCount < 1)
                throw new InvalidDataException("Audit segment manifest contains a non-contiguous or empty archived segment.");
            if (!IsSha256Hex(anchor.DataSha256))
                throw new InvalidDataException("Audit segment manifest contains an invalid archived data hash.");
            if (!string.IsNullOrEmpty(anchor.SealSha256) && !IsSha256Hex(anchor.SealSha256))
                throw new InvalidDataException("Audit segment manifest contains an invalid archived seal hash.");
        }

        if (manifest.State == CommittedState)
        {
            if (manifest.PendingActiveIndex is not null || manifest.Archived.Count != manifest.ActiveIndex - 1)
                throw new InvalidDataException("Committed audit segment manifest has inconsistent segment state.");
            return;
        }

        if (manifest.State != PendingState)
            throw new InvalidDataException("Audit segment manifest contains an unsupported state.");
        if (manifest.Archived.Count != manifest.ActiveIndex || manifest.Archived[^1].Index != manifest.ActiveIndex)
            throw new InvalidDataException("Pending audit segment manifest must pin the complete previous active segment.");
        if (manifest.PendingActiveIndex != manifest.ActiveIndex + 1)
            throw new InvalidDataException("Pending audit segment manifest must advance exactly one segment.");
    }

    private static async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        using var sha = SHA256.Create();
        var digest = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(digest);
    }

    private static bool IsSha256Hex(string? value)
    {
        if (value is null || value.Length != 64)
            return false;
        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }
        return true;
    }

    private static bool FixedTimeTextEquals(string expected, string actual)
    {
        if (expected.Length != actual.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(expected),
            System.Text.Encoding.ASCII.GetBytes(actual));
    }

    private sealed record AuditSegmentManifest(
        int Version,
        string State,
        int ActiveIndex,
        IReadOnlyList<AuditSegmentAnchor> Archived,
        int? PendingActiveIndex);

    private sealed record AuditSegmentAnchor(
        int Index,
        int EventCount,
        string DataSha256,
        string SealSha256);

    private sealed record AuditSnapshot(IReadOnlyList<AuditEvent> Events, int ActiveCount);
}