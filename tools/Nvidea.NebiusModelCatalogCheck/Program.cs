using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Nebius;

namespace Nvidea.NebiusModelCatalogCheck;

public static class Program
{
    private const int MaxCatalogBytes = 2 * 1024 * 1024;

    public static async Task<int> Main(string[] args)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException)
        {
            WriteUsage();
            return 2;
        }

        ModelCatalogCheckResult result;
        try
        {
            var required = RequiredModelSet.FromEnvironment();
            if (options.InputPath is not null)
            {
                var bytes = await ReadBoundedFileAsync(options.InputPath, MaxCatalogBytes).ConfigureAwait(false);
                result = NebiusModelCatalogChecker.Evaluate(bytes, "captured", required, endpointHost: null);
            }
            else
            {
                result = await CheckLiveAsync(required).ConfigureAwait(false);
            }
        }
        catch (CatalogCheckException ex)
        {
            result = ModelCatalogCheckResult.Failed(
                options.InputPath is null ? "live" : "captured",
                ex.Code);
        }
        catch (OperationCanceledException)
        {
            result = ModelCatalogCheckResult.Failed(
                options.InputPath is null ? "live" : "captured",
                "catalog_request_timeout");
        }
        catch (HttpRequestException)
        {
            result = ModelCatalogCheckResult.Failed("live", "catalog_request_failed");
        }
        catch (IOException)
        {
            result = ModelCatalogCheckResult.Failed(
                options.InputPath is null ? "live" : "captured",
                "catalog_read_failed");
        }
        catch (UnauthorizedAccessException)
        {
            result = ModelCatalogCheckResult.Failed("captured", "catalog_read_failed");
        }

        var json = JsonSerializer.Serialize(result, JsonOptions.Pretty);
        Console.WriteLine(json);

        if (options.OutputPath is not null)
        {
            try
            {
                await PersistAtomicallyAsync(options.OutputPath, json).ConfigureAwait(false);
            }
            catch (IOException)
            {
                return 3;
            }
            catch (UnauthorizedAccessException)
            {
                return 3;
            }
        }

        return result.Passed ? 0 : 1;
    }

    private static async Task<ModelCatalogCheckResult> CheckLiveAsync(RequiredModelSet required)
    {
        var apiKey = Environment.GetEnvironmentVariable("NEBIUS_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new CatalogCheckException("missing_nebius_api_key");

        var baseUriText = Environment.GetEnvironmentVariable("NVIDEA_NEBIUS_BASE_URL");
        var baseUri = string.IsNullOrWhiteSpace(baseUriText)
            ? new Uri("https://api.tokenfactory.us-central1.nebius.com/v1/")
            : ParseTrustedBaseUri(baseUriText);
        ValidateTrustedEndpoint(baseUri);

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "models"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token).ConfigureAwait(false);

        if ((int)response.StatusCode is >= 300 and < 400)
            throw new CatalogCheckException("catalog_redirect_refused");
        if (!response.IsSuccessStatusCode)
            throw new CatalogCheckException($"catalog_http_{(int)response.StatusCode}");
        if (response.Content.Headers.ContentLength is > MaxCatalogBytes)
            throw new CatalogCheckException("catalog_too_large");

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
        var bytes = await ReadBoundedStreamAsync(stream, MaxCatalogBytes, cts.Token).ConfigureAwait(false);
        return NebiusModelCatalogChecker.Evaluate(bytes, "live", required, baseUri.Host);
    }

    private static Uri ParseTrustedBaseUri(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            throw new CatalogCheckException("invalid_nebius_base_url");
        return uri;
    }

    private static void ValidateTrustedEndpoint(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(uri.Host)
            || !uri.Host.EndsWith("nebius.com", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new CatalogCheckException("untrusted_nebius_endpoint");
        }
    }

    private static async Task<byte[]> ReadBoundedFileAsync(string path, int maxBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            throw new CatalogCheckException("catalog_file_missing");
        if (info.Length <= 0)
            throw new CatalogCheckException("catalog_empty");
        if (info.Length > maxBytes)
            throw new CatalogCheckException("catalog_too_large");

        return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadBoundedStreamAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream(capacity: Math.Min(maxBytes, 64 * 1024));
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;
            if (buffer.Length + read > maxBytes)
                throw new CatalogCheckException("catalog_too_large");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        if (buffer.Length == 0)
            throw new CatalogCheckException("catalog_empty");
        return buffer.ToArray();
    }

    private static async Task PersistAtomicallyAsync(string outputPath, string json)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllTextAsync(tempPath, json + Environment.NewLine, new UTF8Encoding(false)).ConfigureAwait(false);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine("Usage: Nvidea.NebiusModelCatalogCheck (--input <models.json> | --live) [--output <evidence.json>]");
    }
}

public sealed record RequiredModelSet(string Fast, string Standard, string Deep)
{
    public static RequiredModelSet FromEnvironment() => new(
        EnvironmentOrDefault("NVIDEA_MODEL_FAST", NebiusOptions.VerifiedNemotronNanoModel),
        EnvironmentOrDefault("NVIDEA_MODEL_STANDARD", NebiusOptions.VerifiedNemotronSuperModel),
        EnvironmentOrDefault("NVIDEA_MODEL_DEEP", NebiusOptions.VerifiedNemotronUltraModel));

    private static string EnvironmentOrDefault(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

public static class NebiusModelCatalogChecker
{
    private const int MaxModels = 2048;
    private const int MaxModelIdLength = 512;

    public static ModelCatalogCheckResult Evaluate(
        ReadOnlySpan<byte> utf8Json,
        string mode,
        RequiredModelSet required,
        string? endpointHost)
    {
        if (utf8Json.Length == 0)
            throw new CatalogCheckException("catalog_empty");
        if (utf8Json.Length > 2 * 1024 * 1024)
            throw new CatalogCheckException("catalog_too_large");
        if (!string.Equals(mode, "captured", StringComparison.Ordinal)
            && !string.Equals(mode, "live", StringComparison.Ordinal))
        {
            throw new CatalogCheckException("invalid_mode");
        }

        var hash = Convert.ToHexString(SHA256.HashData(utf8Json)).ToLowerInvariant();
        try
        {
            using var document = JsonDocument.Parse(utf8Json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            });

            RejectDuplicateProperties(document.RootElement);
            var ids = ParseModelIds(document.RootElement);
            var checks = new[]
            {
                new RequiredModelCheck("fast", required.Fast, ids.Contains(required.Fast)),
                new RequiredModelCheck("standard", required.Standard, ids.Contains(required.Standard)),
                new RequiredModelCheck("deep", required.Deep, ids.Contains(required.Deep))
            };

            var failureCodes = checks
                .Where(check => !check.Present)
                .Select(check => $"missing_{check.Tier}_model")
                .ToArray();

            return new ModelCatalogCheckResult(
                SchemaVersion: "nvidea.nebius-model-catalog-check.v1",
                ObservedAtUtc: DateTimeOffset.UtcNow,
                Mode: mode,
                Passed: failureCodes.Length == 0,
                CatalogSha256: hash,
                CatalogModelCount: ids.Count,
                EndpointHost: mode == "live" ? endpointHost : null,
                RequiredModels: checks,
                FailureCodes: failureCodes);
        }
        catch (JsonException)
        {
            throw new CatalogCheckException("catalog_json_invalid");
        }
    }

    private static HashSet<string> ParseModelIds(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Array)
        {
            throw new CatalogCheckException("catalog_shape_invalid");
        }

        if (data.GetArrayLength() is <= 0 or > MaxModels)
            throw new CatalogCheckException("catalog_model_count_invalid");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("id", out var idElement)
                || idElement.ValueKind != JsonValueKind.String)
            {
                throw new CatalogCheckException("catalog_model_entry_invalid");
            }

            var id = idElement.GetString();
            if (string.IsNullOrWhiteSpace(id) || id.Length > MaxModelIdLength || ContainsControlCharacter(id))
                throw new CatalogCheckException("catalog_model_id_invalid");
            if (!ids.Add(id))
                throw new CatalogCheckException("catalog_duplicate_model_id");
        }

        return ids;
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new CatalogCheckException("catalog_duplicate_json_property");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                RejectDuplicateProperties(item);
        }
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character))
                return true;
        }
        return false;
    }
}

public sealed record ModelCatalogCheckResult(
    string SchemaVersion,
    DateTimeOffset ObservedAtUtc,
    string Mode,
    bool Passed,
    string? CatalogSha256,
    int? CatalogModelCount,
    string? EndpointHost,
    IReadOnlyList<RequiredModelCheck> RequiredModels,
    IReadOnlyList<string> FailureCodes)
{
    public static ModelCatalogCheckResult Failed(string mode, string failureCode) => new(
        SchemaVersion: "nvidea.nebius-model-catalog-check.v1",
        ObservedAtUtc: DateTimeOffset.UtcNow,
        Mode: mode,
        Passed: false,
        CatalogSha256: null,
        CatalogModelCount: null,
        EndpointHost: null,
        RequiredModels: Array.Empty<RequiredModelCheck>(),
        FailureCodes: new[] { failureCode });
}

public sealed record RequiredModelCheck(string Tier, string Model, bool Present);

public sealed class CatalogCheckException : Exception
{
    public CatalogCheckException(string code) : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}

internal sealed record CliOptions(string? InputPath, string? OutputPath)
{
    public static CliOptions Parse(string[] args)
    {
        string? input = null;
        string? output = null;
        var live = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--input":
                    input = ReadValue(args, ref index, "--input");
                    break;
                case "--output":
                    output = ReadValue(args, ref index, "--output");
                    break;
                case "--live":
                    live = true;
                    break;
                default:
                    throw new ArgumentException("Unknown argument.");
            }
        }

        if (live == (input is not null))
            throw new ArgumentException("Choose exactly one source mode.");
        return new CliOptions(input, output);
    }

    private static string ReadValue(string[] args, ref int index, string name)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException($"{name} requires a value.");
        return args[index];
    }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Pretty = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
