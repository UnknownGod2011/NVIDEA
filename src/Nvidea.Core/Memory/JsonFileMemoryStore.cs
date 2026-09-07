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
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public JsonFileMemoryStore(string path, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A memory file path is required.", nameof(path));

        _path = Path.GetFullPath(path);
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

            var persisted = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
            var payload = DecodePersisted(persisted);

            List<MemoryRecord>? records;
            try
            {
                records = JsonSerializer.Deserialize<List<MemoryRecord>>(payload.Plaintext, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Memory store contains invalid data.", ex);
            }

            var result = records ?? new List<MemoryRecord>();
            if (_protector is not null && !payload.WasProtected)
                await PersistUnlockedAsync(result, cancellationToken).ConfigureAwait(false);

            return result;
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

        var tempPath = $"{_path}.{Guid.NewGuid():N}.tmp";
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

            File.Move(tempPath, _path, overwrite: true);
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
