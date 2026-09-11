using Nvidea.Core.Nebius;
using Xunit;

namespace Nvidea.JudgingEvidenceVerifier.Tests;

public sealed class ModelCatalogEvidenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 17, 0, 0, TimeSpan.Zero);
    private const string ArtifactSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Fresh_live_catalog_for_configured_tiers_passes()
    {
        var summary = Program.ValidateModelCatalogEvidence(CreateEvidence(Now.AddMinutes(-5)), Now, ArtifactSha);

        Assert.Equal("provider-live-readiness", summary.EvidenceClass);
        Assert.Equal(900, summary.MaximumAgeSeconds);
        Assert.Equal("api.tokenfactory.us-central1.nebius.com", summary.EndpointHost);
        Assert.Equal(3, summary.RequiredModels.Count);
    }

    [Fact]
    public void Captured_catalog_cannot_prove_current_provider_readiness()
    {
        var evidence = CreateEvidence(Now.AddMinutes(-1)) with { Mode = "captured", EndpointHost = null };

        Assert.Throws<InvalidDataException>(() => Program.ValidateModelCatalogEvidence(evidence, Now, ArtifactSha));
    }

    [Fact]
    public void Stale_live_catalog_is_rejected()
    {
        var evidence = CreateEvidence(Now.AddMinutes(-16));

        Assert.Throws<InvalidDataException>(() => Program.ValidateModelCatalogEvidence(evidence, Now, ArtifactSha));
    }

    [Fact]
    public void Excessive_future_clock_skew_is_rejected()
    {
        var evidence = CreateEvidence(Now.AddMinutes(3));

        Assert.Throws<InvalidDataException>(() => Program.ValidateModelCatalogEvidence(evidence, Now, ArtifactSha));
    }

    [Fact]
    public void Untrusted_suffix_lookalike_host_is_rejected()
    {
        var evidence = CreateEvidence(Now.AddMinutes(-1)) with { EndpointHost = "evilnebius.com" };

        Assert.Throws<InvalidDataException>(() => Program.ValidateModelCatalogEvidence(evidence, Now, ArtifactSha));
    }

    [Fact]
    public void Evidence_for_different_model_binding_is_rejected()
    {
        var evidence = CreateEvidence(Now.AddMinutes(-1));
        var changed = evidence.RequiredModels!
            .Select(item => item.Tier == "deep" ? item with { Model = "nvidia/not-the-configured-model" } : item)
            .ToArray();

        Assert.Throws<InvalidDataException>(() => Program.ValidateModelCatalogEvidence(evidence with { RequiredModels = changed }, Now, ArtifactSha));
    }

    [Fact]
    public void Passing_evidence_with_failure_codes_is_rejected()
    {
        var evidence = CreateEvidence(Now.AddMinutes(-1)) with { FailureCodes = new[] { "unexpected" } };

        Assert.Throws<InvalidDataException>(() => Program.ValidateModelCatalogEvidence(evidence, Now, ArtifactSha));
    }

    private static Program.ModelCatalogEvidence CreateEvidence(DateTimeOffset observedAt) => new(
        SchemaVersion: "nvidea.nebius-model-catalog-check.v1",
        ObservedAtUtc: observedAt,
        Mode: "live",
        Passed: true,
        CatalogSha256: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        CatalogModelCount: 42,
        EndpointHost: "api.tokenfactory.us-central1.nebius.com",
        RequiredModels:
        [
            new Program.RequiredModelEvidence("fast", Current("NVIDEA_MODEL_FAST", NebiusOptions.VerifiedNemotronNanoModel), true),
            new Program.RequiredModelEvidence("standard", Current("NVIDEA_MODEL_STANDARD", NebiusOptions.VerifiedNemotronSuperModel), true),
            new Program.RequiredModelEvidence("deep", Current("NVIDEA_MODEL_DEEP", NebiusOptions.VerifiedNemotronUltraModel), true)
        ],
        FailureCodes: Array.Empty<string>());

    private static string Current(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
