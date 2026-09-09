using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Nvidea.Core.Jobs;

public sealed record NebiusObjectStorageClientOptions(
    string Endpoint,
    string Region,
    string Bucket,
    string AccessKeyId,
    string SecretAccessKey,
    string Prefix = "nvidea-research",
    TimeSpan? OperationTimeout = null,
    int MaxRetries = 3);

/// <summary>
/// Minimal object-store boundary used by the protected research transport. Implementations must
/// provide create-once writes, bounded reads and idempotent deletes without exposing credentials in errors.
/// </summary>
public interface IProtectedResearchObjectStoreClient
{
    Task PutIfAbsentAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task<byte[]?> GetAsync(string key, int maxBytes, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// S3-compatible Nebius Object Storage client using static service-account keys. The client intentionally
/// exposes only the narrow object operations required by protected remote research.
/// </summary>
public sealed class NebiusObjectStorageClient : IProtectedResearchObjectStoreClient, IDisposable
{
    private const int MaxKeyLength = 512;
    private static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(30);

    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _prefix;
    private readonly TimeSpan _operationTimeout;
    private readonly int _maxRetries;

    public NebiusObjectStorageClient(NebiusObjectStorageClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var configuration = new AmazonS3Config
        {
            ServiceURL = options.Endpoint.TrimEnd('/'),
            AuthenticationRegion = options.Region,
            ForcePathStyle = false,
            MaxErrorRetry = 0
        };

        _client = new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            configuration);
        _bucket = options.Bucket;
        _prefix = NormalizePrefix(options.Prefix);
        _operationTimeout = options.OperationTimeout ?? DefaultOperationTimeout;
        _maxRetries = options.MaxRetries;
    }

    public async Task PutIfAbsentAsync(
        string key,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        var objectKey = BuildObjectKey(key);
        if (content.Length is < 1 or > S3ProtectedResearchTransport.MaxEnvelopeBytes)
            throw new InvalidOperationException("Protected research object has an invalid transport size.");

        for (var attempt = 0; ; attempt++)
        {
            using var timeout = CreateTimeoutToken(cancellationToken);
            using var stream = new MemoryStream(content.ToArray(), writable: false);
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = objectKey,
                InputStream = stream,
                ContentType = "application/json",
                IfNoneMatch = "*"
            };

            try
            {
                await _client.PutObjectAsync(request, timeout.Token).ConfigureAwait(false);
                return;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new InvalidOperationException("A protected research object already exists for this opaque work-item id.");
            }
            catch (AmazonS3Exception ex) when ((int)ex.StatusCode == 409 && attempt < _maxRetries)
            {
                await DelayRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Nebius Object Storage request timed out.");
            }
            catch (AmazonS3Exception ex)
            {
                throw CreateSanitizedStorageException("put", ex.StatusCode);
            }
        }
    }

    public async Task<byte[]?> GetAsync(
        string key,
        int maxBytes,
        CancellationToken cancellationToken = default)
    {
        if (maxBytes is < 1 or > S3ProtectedResearchTransport.MaxEnvelopeBytes)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));

        var objectKey = BuildObjectKey(key);
        using var timeout = CreateTimeoutToken(cancellationToken);
        try
        {
            using var response = await _client.GetObjectAsync(
                new GetObjectRequest { BucketName = _bucket, Key = objectKey },
                timeout.Token).ConfigureAwait(false);

            if (response.ContentLength is <= 0 || response.ContentLength > maxBytes)
                throw new InvalidOperationException("Protected research object has an invalid transport size.");

            using var buffer = new MemoryStream(capacity: checked((int)response.ContentLength));
            var chunk = new byte[64 * 1024];
            var total = 0;
            while (true)
            {
                var read = await response.ResponseStream.ReadAsync(chunk.AsMemory(), timeout.Token).ConfigureAwait(false);
                if (read == 0)
                    break;
                total += read;
                if (total > maxBytes)
                    throw new InvalidOperationException("Protected research object exceeds the configured transport limit.");
                await buffer.WriteAsync(chunk.AsMemory(0, read), timeout.Token).ConfigureAwait(false);
            }

            if (total == 0)
                throw new InvalidOperationException("Protected research object is empty.");
            return buffer.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Nebius Object Storage request timed out.");
        }
        catch (AmazonS3Exception ex)
        {
            throw CreateSanitizedStorageException("get", ex.StatusCode);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var objectKey = BuildObjectKey(key);
        using var timeout = CreateTimeoutToken(cancellationToken);
        try
        {
            await _client.DeleteObjectAsync(
                new DeleteObjectRequest { BucketName = _bucket, Key = objectKey },
                timeout.Token).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Idempotent delete.
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Nebius Object Storage request timed out.");
        }
        catch (AmazonS3Exception ex)
        {
            throw CreateSanitizedStorageException("delete", ex.StatusCode);
        }
    }

    public void Dispose() => _client.Dispose();

    private CancellationTokenSource CreateTimeoutToken(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(_operationTimeout);
        return source;
    }

    private string BuildObjectKey(string key)
    {
        ValidateRelativeObjectKey(key);
        var combined = string.IsNullOrEmpty(_prefix) ? key : $"{_prefix}/{key}";
        if (combined.Length > MaxKeyLength)
            throw new InvalidOperationException("Protected research object key exceeds the configured limit.");
        return combined;
    }

    private static void ValidateOptions(NebiusObjectStorageClientOptions options)
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new ArgumentException("Nebius Object Storage endpoint must be an HTTPS origin URL.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Region) || options.Region.Length > 64)
            throw new ArgumentException("A bounded Object Storage region is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.Bucket) || options.Bucket.Length > 63)
            throw new ArgumentException("A bounded Object Storage bucket name is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.AccessKeyId) || string.IsNullOrWhiteSpace(options.SecretAccessKey))
            throw new ArgumentException("Object Storage static access credentials are required.", nameof(options));
        if (options.Prefix.Length > 128 || options.Prefix.Any(static ch => char.IsControl(ch)))
            throw new ArgumentException("Object Storage prefix is invalid.", nameof(options));

        var timeout = options.OperationTimeout ?? DefaultOperationTimeout;
        if (timeout < TimeSpan.FromSeconds(1) || timeout > TimeSpan.FromMinutes(2))
            throw new ArgumentOutOfRangeException(nameof(options), "Object Storage operation timeout must be between 1 second and 2 minutes.");
        if (options.MaxRetries is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(options), "Object Storage retries must be between 0 and 5.");
    }

    private static string NormalizePrefix(string value)
    {
        var prefix = value.Trim().Trim('/');
        if (prefix.Contains("..", StringComparison.Ordinal)
            || prefix.Contains('\\')
            || prefix.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(static segment => segment is "." or ".."))
        {
            throw new ArgumentException("Object Storage prefix must remain within its configured namespace.", nameof(value));
        }
        return prefix;
    }

    private static void ValidateRelativeObjectKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.StartsWith('/', StringComparison.Ordinal)
            || value.Contains('\\')
            || value.Contains("..", StringComparison.Ordinal)
            || value.Any(static ch => char.IsControl(ch)))
        {
            throw new InvalidOperationException("Protected research object key is invalid.");
        }
    }

    private static async Task DelayRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var milliseconds = Math.Min(1000, 100 * (1 << Math.Min(attempt, 3)));
        await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken).ConfigureAwait(false);
    }

    private static InvalidOperationException CreateSanitizedStorageException(string operation, HttpStatusCode statusCode) =>
        new($"Nebius Object Storage {operation} request failed with HTTP {(int)statusCode}.");
}

/// <summary>
/// Object-store-backed implementation for protected research envelopes. It keeps work items, results and
/// dispatch bindings in independent prefixes and preserves create-once semantics across distributed clients.
/// </summary>
public sealed class S3ProtectedResearchTransport :
    IProtectedResearchWorkItemTransport,
    IProtectedResearchResultTransport,
    IProtectedResearchDispatchBindingTransport
{
    public const int MaxEnvelopeBytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProtectedResearchObjectStoreClient _objects;

    public S3ProtectedResearchTransport(IProtectedResearchObjectStoreClient objects)
    {
        _objects = objects ?? throw new ArgumentNullException(nameof(objects));
    }

    Task IProtectedResearchWorkItemTransport.PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken) =>
        PutEnvelopeAsync("work-items", envelope.OpaqueWorkItemId, envelope, cancellationToken);

    Task<ProtectedResearchWorkItemEnvelope?> IProtectedResearchWorkItemTransport.GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
        GetEnvelopeAsync<ProtectedResearchWorkItemEnvelope>("work-items", opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchWorkItemTransport.DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
        DeleteEnvelopeAsync("work-items", opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchResultTransport.PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken) =>
        PutEnvelopeAsync("results", envelope.OpaqueWorkItemId, envelope, cancellationToken);

    Task<ProtectedResearchResultEnvelope?> IProtectedResearchResultTransport.GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
        GetEnvelopeAsync<ProtectedResearchResultEnvelope>("results", opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchResultTransport.DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
        DeleteEnvelopeAsync("results", opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchDispatchBindingTransport.PutAsync(ProtectedResearchDispatchBinding binding, CancellationToken cancellationToken) =>
        PutEnvelopeAsync("dispatch-bindings", binding.OpaqueWorkItemId, binding, cancellationToken);

    Task<ProtectedResearchDispatchBinding?> IProtectedResearchDispatchBindingTransport.GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
        GetEnvelopeAsync<ProtectedResearchDispatchBinding>("dispatch-bindings", opaqueWorkItemId, cancellationToken);

    Task IProtectedResearchDispatchBindingTransport.DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
        DeleteEnvelopeAsync("dispatch-bindings", opaqueWorkItemId, cancellationToken);

    private async Task PutEnvelopeAsync<T>(string category, string opaqueWorkItemId, T envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ValidateOpaqueId(opaqueWorkItemId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        if (bytes.Length > MaxEnvelopeBytes)
            throw new InvalidOperationException($"Protected research envelope exceeds the {MaxEnvelopeBytes}-byte transport limit.");
        await _objects.PutIfAbsentAsync(BuildKey(category, opaqueWorkItemId), bytes, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> GetEnvelopeAsync<T>(string category, string opaqueWorkItemId, CancellationToken cancellationToken)
    {
        ValidateOpaqueId(opaqueWorkItemId);
        var bytes = await _objects.GetAsync(BuildKey(category, opaqueWorkItemId), MaxEnvelopeBytes, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
            return default;
        if (bytes.Length is < 1 or > MaxEnvelopeBytes)
            throw new InvalidOperationException("Protected research envelope has an invalid transport size.");

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions)
                ?? throw new InvalidOperationException("Protected research envelope is empty or malformed.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Protected research envelope is malformed.");
        }
    }

    private Task DeleteEnvelopeAsync(string category, string opaqueWorkItemId, CancellationToken cancellationToken)
    {
        ValidateOpaqueId(opaqueWorkItemId);
        return _objects.DeleteAsync(BuildKey(category, opaqueWorkItemId), cancellationToken);
    }

    private static string BuildKey(string category, string opaqueWorkItemId) => $"{category}/{opaqueWorkItemId}.json";

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
