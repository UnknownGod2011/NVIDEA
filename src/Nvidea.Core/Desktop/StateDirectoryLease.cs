using System.Text;
using System.Text.Json;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Owns an OS-backed, cross-process lease for the NVIDEA durable state directory.
/// The lease file is intentionally persistent; process death releases the kernel lock,
/// so a stale file cannot permanently block future startup.
/// </summary>
public sealed class StateDirectoryLease : IDisposable
{
    public const string LeaseFileName = ".nvidea-state.lock";
    public const int CurrentFormatVersion = 1;
    private const int MaxOwnerMetadataBytes = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FileStream _stream;
    private bool _disposed;

    private StateDirectoryLease(string stateDirectory, string leasePath, FileStream stream, StateDirectoryLeaseOwner owner)
    {
        StateDirectory = stateDirectory;
        LeasePath = leasePath;
        Owner = owner;
        _stream = stream;
    }

    public string StateDirectory { get; }
    public string LeasePath { get; }
    public StateDirectoryLeaseOwner Owner { get; }

    public static StateDirectoryLease Acquire(string stateDirectory)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        var stateRoot = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(stateRoot);
        var leasePath = Path.GetFullPath(Path.Combine(stateRoot, LeaseFileName));
        EnsureChildPath(stateRoot, leasePath);
        RejectReparsePointIfPresent(leasePath);

        FileStream? stream = null;
        try
        {
            stream = new FileStream(
                leasePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.WriteThrough);

            try
            {
                stream.Lock(0, 1);
            }
            catch (IOException ex)
            {
                stream.Dispose();
                stream = null;
                throw CreateUnavailableException(leasePath, ex);
            }

            var owner = new StateDirectoryLeaseOwner(
                CurrentFormatVersion,
                Guid.NewGuid().ToString("N"),
                Environment.ProcessId,
                DateTimeOffset.UtcNow);
            WriteOwner(stream, owner);
            return new StateDirectoryLease(stateRoot, leasePath, stream, owner);
        }
        catch (StateDirectoryLeaseUnavailableException)
        {
            throw;
        }
        catch (IOException ex)
        {
            stream?.Dispose();
            throw CreateUnavailableException(leasePath, ex);
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _stream.Unlock(0, 1);
        }
        catch (IOException)
        {
            // Disposing the handle still releases any OS lock. Preserve shutdown reliability.
        }
        finally
        {
            _stream.Dispose();
        }
    }

    private static void WriteOwner(FileStream stream, StateDirectoryLeaseOwner owner)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(owner, JsonOptions);
        if (bytes.Length > MaxOwnerMetadataBytes)
            throw new InvalidOperationException("State lease owner metadata exceeds the supported bound.");

        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(flushToDisk: true);
    }

    private static StateDirectoryLeaseUnavailableException CreateUnavailableException(string leasePath, IOException cause)
    {
        var owner = TryReadOwner(leasePath);
        var detail = owner is null
            ? "Another NVIDEA process may already own this durable state directory."
            : $"Another NVIDEA process owns this durable state directory (process {owner.ProcessId}, acquired {owner.AcquiredAt:O}).";
        return new StateDirectoryLeaseUnavailableException(detail, owner, cause);
    }

    private static StateDirectoryLeaseOwner? TryReadOwner(string leasePath)
    {
        try
        {
            using var stream = new FileStream(
                leasePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.SequentialScan);
            if (stream.Length <= 0 || stream.Length > MaxOwnerMetadataBytes)
                return null;

            var bytes = new byte[checked((int)stream.Length)];
            var offset = 0;
            while (offset < bytes.Length)
            {
                var read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0)
                    return null;
                offset += read;
            }

            var owner = JsonSerializer.Deserialize<StateDirectoryLeaseOwner>(bytes, JsonOptions);
            if (owner is null
                || owner.FormatVersion != CurrentFormatVersion
                || owner.ProcessId <= 0
                || owner.AcquiredAt == default
                || string.IsNullOrWhiteSpace(owner.InstanceId)
                || !Guid.TryParseExact(owner.InstanceId, "N", out _))
            {
                return null;
            }

            return owner;
        }
        catch
        {
            return null;
        }
    }

    private static void RejectReparsePointIfPresent(string leasePath)
    {
        if (!File.Exists(leasePath))
            return;

        var attributes = File.GetAttributes(leasePath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("NVIDEA state lease file must not be a reparse point.");
    }

    private static void EnsureChildPath(string stateRoot, string candidate)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(stateRoot);
        var prefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("State lease must remain inside the NVIDEA state directory.");
    }
}

public sealed record StateDirectoryLeaseOwner(
    int FormatVersion,
    string InstanceId,
    int ProcessId,
    DateTimeOffset AcquiredAt);

public sealed class StateDirectoryLeaseUnavailableException : InvalidOperationException
{
    public StateDirectoryLeaseUnavailableException(
        string message,
        StateDirectoryLeaseOwner? owner,
        Exception innerException)
        : base(message, innerException)
    {
        Owner = owner;
    }

    public StateDirectoryLeaseOwner? Owner { get; }
}
