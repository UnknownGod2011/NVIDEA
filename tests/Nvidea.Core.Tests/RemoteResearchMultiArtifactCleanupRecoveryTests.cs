using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchMultiArtifactCleanupRecoveryTests
{
    [Fact]
    public async Task Partial_cleanup_failure_retries_already_deleted_artifact_and_clears_only_after_all_deletes_succeed()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-multi-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var transport = new PartialFailureTransport { FailNextWorkItemDelete = true };
            var job = CreateTerminalJob();
            await store.SaveAsync(job);

            var ingestor = new RemoteResearchResultIngestor(
                store,
                transport,
                rsa.ExportPkcs8PrivateKeyPem(),
                new InMemoryAuditTrail(),
                transport);

            var first = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ingestor.RecoverPendingCleanupAsync(job.JobId));
            Assert.Contains("cleanup remains pending", first.Message, StringComparison.OrdinalIgnoreCase);

            var stranded = await store.GetAsync(job.JobId);
            Assert.NotNull(stranded?.PendingProtectedPayloadCleanup);
            Assert.Equal(1, transport.ResultDeleteCalls);
            Assert.Equal(1, transport.WorkItemDeleteCalls);

            var recovered = await ingestor.RecoverPendingCleanupAsync(job.JobId);

            Assert.Null(recovered.PendingProtectedPayloadCleanup);
            Assert.Equal(2, transport.ResultDeleteCalls);
            Assert.Equal(2, transport.WorkItemDeleteCalls);
            Assert.Null((await store.GetAsync(job.JobId))!.PendingProtectedPayloadCleanup);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static AgentJobRecord CreateTerminalJob()
    {
        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
        var now = DateTimeOffset.Parse("2026-09-14T13:00:00Z");
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
            Checkpoint: new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{}", now.AddMinutes(-10)),
            ApprovalScope: null,
            LastError: "Nebius remote research stage failed.",
            CreatedAt: now.AddMinutes(-10),
            UpdatedAt: now,
            RemoteResearch: new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                opaqueId,
                "job-123",
                ResearchJobHandler.PlannedStep,
                now.AddMinutes(-10),
                now.AddMinutes(-9),
                RemoteResearchProvenanceState.RemoteFailed,
                TerminalAt: now),
            PendingProtectedPayloadCleanup: new PendingProtectedPayloadCleanup(
                Guid.NewGuid(),
                opaqueId,
                now));
    }

    private sealed class PartialFailureTransport : IProtectedResearchResultTransport, IProtectedResearchWorkItemTransport
    {
        public int ResultDeleteCalls { get; private set; }
        public int WorkItemDeleteCalls { get; private set; }
        public bool FailNextWorkItemDelete { get; set; }

        Task IProtectedResearchResultTransport.PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task<ProtectedResearchResultEnvelope?> IProtectedResearchResultTransport.GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);

        Task IProtectedResearchResultTransport.DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken)
        {
            ResultDeleteCalls++;
            return Task.CompletedTask;
        }

        Task IProtectedResearchWorkItemTransport.PutAsync(
            ProtectedResearchWorkItemEnvelope envelope,
            CancellationToken cancellationToken) => Task.CompletedTask;

        Task<ProtectedResearchWorkItemEnvelope?> IProtectedResearchWorkItemTransport.GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(null);

        Task IProtectedResearchWorkItemTransport.DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken)
        {
            WorkItemDeleteCalls++;
            if (FailNextWorkItemDelete)
            {
                FailNextWorkItemDelete = false;
                throw new IOException("Injected work-item cleanup failure.");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
