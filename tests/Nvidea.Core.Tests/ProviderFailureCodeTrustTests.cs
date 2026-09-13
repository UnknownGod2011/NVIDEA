using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ProviderFailureCodeTrustTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("Quota", "Quota")]
    [InlineData("  FutureProviderCode  ", "FutureProviderCode")]
    public void TryCanonicalize_AcceptsAbsentAndBoundedUnknownEvidence(string? input, string? expected)
    {
        var accepted = ProviderFailureCodeTrust.TryCanonicalize(input, out var canonical);

        Assert.True(accepted);
        Assert.Equal(expected, canonical);
    }

    [Fact]
    public void TryCanonicalize_RejectsOversizedEvidence()
    {
        var accepted = ProviderFailureCodeTrust.TryCanonicalize(
            new string('x', ProviderFailureCodeTrust.MaxLength + 1),
            out var canonical);

        Assert.False(accepted);
        Assert.Null(canonical);
    }

    [Theory]
    [InlineData("Quota\nInjected")]
    [InlineData("Quota\rInjected")]
    [InlineData("Quota\0Injected")]
    public void TryCanonicalize_RejectsControlCharacters(string input)
    {
        var accepted = ProviderFailureCodeTrust.TryCanonicalize(input, out var canonical);

        Assert.False(accepted);
        Assert.Null(canonical);
    }

    [Fact]
    public void Migration_CanonicalizesAlreadyStructuredUnknownCodeWithoutGrantingRemediationAuthority()
    {
        var record = CreateRemoteFailure("  FutureProviderCode  ");

        var migrated = RemoteResearchFailureProvenanceMigration.Migrate(record);

        Assert.Equal("FutureProviderCode", migrated.RemoteResearch!.ProviderFailureCode);
        Assert.Null(NebiusFailureRemediationPolicy.Classify(migrated.RemoteResearch.ProviderFailureCode));
    }

    [Fact]
    public void Migration_RejectsMalformedAlreadyStructuredCodeInsteadOfFallingBackToLegacyErrorText()
    {
        var record = CreateRemoteFailure(new string('x', ProviderFailureCodeTrust.MaxLength + 1)) with
        {
            LastError = "Nebius remote research stage failed. Provider diagnostic (untrusted): code=Quota; message=retry automatically."
        };

        Assert.Throws<InvalidDataException>(() => RemoteResearchFailureProvenanceMigration.Migrate(record));
    }

    [Fact]
    public void Migration_RejectsStructuredFailureCodeOutsideRemoteFailedProvenance()
    {
        var failed = CreateRemoteFailure("Quota");
        var inconsistent = failed with
        {
            State = AgentJobState.Cancelled,
            RemoteResearch = failed.RemoteResearch! with
            {
                State = RemoteResearchProvenanceState.Cancelled
            }
        };

        var exception = Assert.Throws<InvalidDataException>(
            () => RemoteResearchFailureProvenanceMigration.Migrate(inconsistent));

        Assert.Contains("only valid for RemoteFailed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_PreservesLegacyAllowlistMigrationWhenStructuredCodeIsAbsent()
    {
        var record = CreateRemoteFailure(providerFailureCode: null) with
        {
            LastError = "Nebius remote research stage failed. Provider diagnostic (untrusted): code=Quota; message=untrusted."
        };

        var migrated = RemoteResearchFailureProvenanceMigration.Migrate(record);

        Assert.Equal("Quota", migrated.RemoteResearch!.ProviderFailureCode);
    }

    private static AgentJobRecord CreateRemoteFailure(string? providerFailureCode)
    {
        var now = DateTimeOffset.UtcNow;
        var checkpoint = new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, null, now.AddMinutes(-2));
        var provenance = new RemoteResearchProvenance(
            ResearchWorkItemProtector.ProtocolVersion,
            "opaque-work-item",
            "job-123",
            checkpoint.Step,
            checkpoint.SavedAt,
            now.AddMinutes(-1),
            RemoteResearchProvenanceState.RemoteFailed,
            TerminalAt: now,
            ProviderFailureCode: providerFailureCode);

        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true),
            AgentJobState.Failed,
            JobExecutionLocation.Local,
            Attempt: 1,
            checkpoint,
            ApprovalScope: null,
            LastError: "Nebius remote research stage failed.",
            CreatedAt: now.AddMinutes(-5),
            UpdatedAt: now,
            RemoteResearch: provenance);
    }
}
