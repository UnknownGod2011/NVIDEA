using System.Text.Json;

namespace Nvidea.Core.Jobs;

/// <summary>
/// File-backed implementation for protected remote-research envelopes. It is intended for a
/// directory backed by a Nebius Serverless mounted Object Storage bucket or other explicitly shared
/// durable volume. Only already-encrypted protocol envelopes are stored here.
/// </summary>
public sealed class DirectoryProtectedResearchTransport :
    IProtectedResearchWorkItemTransport,
    IProtectedResearchResultTransport
{
    private const int MaxEnvelopeBytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _workItemsDirectory;
    private readonly string _resultsDirectory;

    public DirectoryProtectedResearchTransport(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("A shared transport root directory is required.", nameof(rootDirectory));

        var fullRoot = Path.GetFullPath(rootDirectory);
        if (!Path.IsPathFullyQualified(fullRoot))
            throw new ArgumentException("The shared transport root must resolve to an absolute path.", nameof(rootDirectory));

        _workItemsDirectory = Path.Combine(fullRoot, "work-items");
        _resultsDirectory = Path.Combine(fullRoot, "results");
        Directory.CreateDirectory(_workItemsDirectory);
        Directory.CreateDirectory(_resultsDirectory);
    }

    Task IProtectedResearchWorkItemTransport.PutAsync(
        ProtectedResearchWorkItemEnvelope envelope,
        CancellationToken cancellationToken) =>
        PutEnvelopeAsync(_workItemsDirectory, envelope.OpaqueWorkItemId, envelope, cancellationToken);

    Task<ProtectedResearchWorkItemEnvelope?> IProtectedResearchWorkItemTransport.GetAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken) =>
        GetEnvelopeAsync<ProtectedResearchWorkItemEnvelope>(_workItemsDirectory, opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchWorkItemTransport.DeleteAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken) =>
        DeleteEnvelopeAsync(_workItemsDirectory, opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchResultTransport.PutAsync(
        ProtectedResearchResultEnvelope envelope,
        CancellationToken cancellationToken) =>
        PutEnvelopeAsync(_resultsDirectory, envelope.OpaqueWorkItemId, envelope, cancellationToken);

    Task<ProtectedResearchResultEnvelope?> IProtectedResearchResultTransport.GetAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken) =>
        GetEnvelopeAsync<ProtectedResearchResultEnvelope>(_resultsDirectory, opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchResultTransport.DeleteAsync(
        string opaqueWorkItemId,
        CancellationToken cancellationToken) =>
        DeleteEnvelopeAsync(_resultsDirectory, opaqueWorkItemId, cancellationToken);

    private static async Task PutEnvelopeAsync<T>(
        string directory,
        string opaqueWorkItemId,
        T envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var target = GetEnvelopePath(directory, opaqueWorkItemId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        if (bytes.Length > MaxEnvelopeBytes)
            throw new InvalidOperationException($"Protected research envelope exceeds the {MaxEnvelopeBytes}-byte transport limit.");

        var temp = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);
            File.Move(temp, target, overwrite: false);
        }
        catch (IOException) when (File.Exists(target))
        {
            throw new InvalidOperationException("A protected research envelope already exists for this opaque work-item id.");
        }
        finally
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }

    private static async Task<T?> GetEnvelopeAsync<T>(
        string directory,
        string opaqueWorkItemId,
        CancellationToken cancellationToken)
    {
        var path = GetEnvelopePath(directory, opaqueWorkItemId);
        if (!File.Exists(path))
            return default;

        var info = new FileInfo(path);
        if (info.Length is < 1 or > MaxEnvelopeBytes)
            throw new InvalidOperationException("Protected research envelope has an invalid transport size.");

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return value ?? throw new InvalidOperationException("Protected research envelope is empty or malformed.");
    }

    private static Task DeleteEnvelopeAsync(
        string directory,
        string opaqueWorkItemId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetEnvelopePath(directory, opaqueWorkItemId);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private static string GetEnvelopePath(string directory, string opaqueWorkItemId)
    {
        ValidateOpaqueId(opaqueWorkItemId);
        return Path.Combine(directory, opaqueWorkItemId + ".json");
    }

    private static void ValidateOpaqueId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length < 24
            || value.Length > 64
            || value.Any(static ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_')))
        {
            throw new InvalidOperationException("Remote research work-item id is invalid.");
        }
    }
}
