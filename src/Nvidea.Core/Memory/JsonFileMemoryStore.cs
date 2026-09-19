using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Security;

namespace Nvidea.Core.Memory;

public sealed class JsonFileMemoryStore : IMemoryStore, IDisposable
{
    private const string ProtectionPurpose = "personal-memory-v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly string _backupPath;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public JsonFileMemoryStore(string path, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A memory file path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _backupPath = $"{_path}.bak";
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return Array.Empty<MemoryRecord>();

            var (records, wasProtected) = await ReadSnapshotUnlockedAsync(_path, cancellationToken).ConfigureAwait(false);
            if (_protector is not null && !wasProtected)
                await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);

            return records;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(memories);
        ThrowIfDisposed();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await PersistUnlockedAsync(memories, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Explicitly restores the bounded last-known-good generation. Normal reads never fall back to the
    /// backup silently: malformed current state remains a fail-closed condition requiring user/operator intent.
    /// </summary>
    public async Task<IReadOnlyList<MemoryRecord>> RecoverLastKnownGoodAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_backupPath))
                throw new InvalidOperationException("No last-known-good memory snapshot is available.");

            var (records, _) = await ReadSnapshotUnlockedAsync(_backupPath, cancellationToken).ConfigureAwait(false);
            var persisted = await File.ReadAllBytesAsync(_backupPath, cancellationToken).ConfigureAwait(false);
            await ReplaceFileUnlockedAsync(_path, persisted, cancellationToken).ConfigureAwait(false);
            return records;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _gate.Dispose();
        _disposed = true;
    }

    private LocalStatePayload DecodePersisted(byte[] persisted)
    {
        if (_protector is not null)
            return LocalStateEnvelope.Decode(persisted, _protector, ProtectionPurpose);

        if (LocalStateEnvelope.HasProtectedHeader(persisted))
            throw new InvalidDataException("Memory store is protected but no local-state protector was configured.");

        return new LocalStatePayload(persisted, false);
    }

    private async Task<(List<MemoryRecord> Records, bool WasProtected)> ReadSnapshotUnlockedAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var persisted = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var payload = DecodePersisted(persisted);

        try
        {
            var records = JsonSerializer.Deserialize<List<MemoryRecord>>(payload.Plaintext, JsonOptions)
                ?? new List<MemoryRecord>();
            return (records, payload.WasProtected);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Memory store contains invalid data.", ex);
        }
    }

    private async Task PersistUnlockedAsync(
        IReadOnlyCollection<MemoryRecord> memories,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(memories, JsonOptions);
        var persisted = _protector is null
            ? plaintext
            : LocalStateEnvelope.Encode(plaintext, _protector, ProtectionPurpose);

        // Preserve at most one previous generation, but only when the current snapshot is demonstrably
        // readable with the configured protection context. A corrupt current file is never promoted to backup.
        if (File.Exists(_path))
        {
            try
            {
                _ = await ReadSnapshotUnlockedAsync(_path, cancellationToken).ConfigureAwait(false);
                var current = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
                await ReplaceFileUnlockedAsync(_backupPath, current, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                // Explicit writes may repair a corrupt primary, but the corrupt bytes must not displace a
                // previously known-good recovery generation.
            }
        }

        await ReplaceFileUnlockedAsync(_path, persisted, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReplaceFileUnlockedAsync(
        string destinationPath,
        byte[] persisted,
        CancellationToken cancellationToken)
    {
        var tempPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(persisted, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
