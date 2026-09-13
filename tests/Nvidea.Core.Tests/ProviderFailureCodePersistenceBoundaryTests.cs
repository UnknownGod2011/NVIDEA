using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ProviderFailureCodePersistenceBoundaryTests
{
    [Fact]
    public async Task JobStore_SaveAndReload_CanonicalizesBoundedUnknownStructuredCode()
    {
        var directory = CreateTempDirectory();
        try
        {
            var store = new JsonAgentJobStore(Path.Combine(directory, "jobs.json"));
            var record = CreateRemoteFailure("  FutureProviderCode  ");

            await store.SaveAsync(record);
            var restored = await store.GetAsync(record.JobId);

            Assert.NotNull(restored);
            Assert.Equal("FutureProviderCode", restored!.RemoteResearch!.ProviderFailureCode);
            Assert.Null(NebiusFailureRemediationPolicy.Classify(restored.RemoteResearch.ProviderFailureCode));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task JobStore_Save_RejectsOversizedStructuredCodeBeforePersistence()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "jobs.json");
            var store = new JsonAgentJobStore(path);
            var record = CreateRemoteFailure(new string('x', ProviderFailureCodeTrust.MaxLength + 1));

            await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(record));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task JobStore_Save_RejectsControlCharacterStructuredCodeBeforePersistence()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "jobs.json");
            var store = new JsonAgentJobStore(path);
            var record = CreateRemoteFailure("Quota\nInjected");

            await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(record));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
