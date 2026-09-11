namespace Nvidea.Core.Memory;

public sealed record LocalMemoryEmbeddingConfiguration(
    bool Enabled,
    LocalOllamaMemoryEmbeddingOptions Options)
{
    public const string EnabledVariable = "NVIDEA_LOCAL_EMBEDDINGS";
    public const string EndpointVariable = "NVIDEA_LOCAL_EMBEDDING_ENDPOINT";
    public const string ModelVariable = "NVIDEA_LOCAL_EMBEDDING_MODEL";
    public const string DimensionsVariable = "NVIDEA_LOCAL_EMBEDDING_DIMENSIONS";

    public static LocalMemoryEmbeddingConfiguration FromEnvironment()
    {
        var enabled = ParseOptionalBoolean(Environment.GetEnvironmentVariable(EnabledVariable), EnabledVariable);
        if (!enabled)
            return new LocalMemoryEmbeddingConfiguration(false, new LocalOllamaMemoryEmbeddingOptions());

        var endpointRaw = Environment.GetEnvironmentVariable(EndpointVariable);
        var modelRaw = Environment.GetEnvironmentVariable(ModelVariable);
        var dimensionsRaw = Environment.GetEnvironmentVariable(DimensionsVariable);

        var endpoint = string.IsNullOrWhiteSpace(endpointRaw)
            ? new Uri("http://127.0.0.1:11434/api/embed")
            : Uri.TryCreate(endpointRaw.Trim(), UriKind.Absolute, out var parsedEndpoint)
                ? parsedEndpoint
                : throw new InvalidOperationException($"{EndpointVariable} must be an absolute loopback URI ending in /api/embed.");

        int? dimensions = null;
        if (!string.IsNullOrWhiteSpace(dimensionsRaw))
        {
            if (!int.TryParse(dimensionsRaw.Trim(), out var parsedDimensions) || parsedDimensions <= 0)
                throw new InvalidOperationException($"{DimensionsVariable} must be a positive integer when set.");
            dimensions = parsedDimensions;
        }

        var options = new LocalOllamaMemoryEmbeddingOptions
        {
            Endpoint = endpoint,
            Model = string.IsNullOrWhiteSpace(modelRaw) ? "embeddinggemma" : modelRaw.Trim(),
            ExpectedDimensions = dimensions,
            RequestTimeout = TimeSpan.FromSeconds(5),
        };

        // Constructor validation is intentionally reused here so invalid/remote endpoints fail at
        // startup rather than only when sensitive memory text is about to be embedded.
        using var validation = new LocalOllamaMemoryEmbeddingProvider(options);
        return new LocalMemoryEmbeddingConfiguration(true, options);
    }

    public LocalOllamaMemoryEmbeddingProvider? CreateProvider() =>
        Enabled ? new LocalOllamaMemoryEmbeddingProvider(Options) : null;

    private static bool ParseOptionalBoolean(string? raw, string variable)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        if (bool.TryParse(raw.Trim(), out var parsed))
            return parsed;
        throw new InvalidOperationException($"{variable} must be 'true' or 'false' when set.");
    }
}
