using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserVerificationPublicationBoundaryTests
{
    [Fact]
    public async Task ObserveCommittedAsync_PublicationFailure_PreservesCompletedJobAndFailsEvidenceClosed()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-browser-publication-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var protector = new ThrowingProtector();
            var store = new DurableBrowserVerificationReceiptStore(Path.Combine(root, "receipt.bin"), protector);
            var boundary = new BrowserVerificationPublicationBoundary(new DurableBrowserVerificationPublisher(store));
            var job = CompletedJob(Guid.NewGuid());

            var result = await boundary.ObserveCommittedAsync(job);

            Assert.Same(job, result.AuthoritativeJob);
            Assert.Equal(AgentJobState.Completed, result.AuthoritativeJob.State);
            Assert.False(result.Published);
            Assert.True(result.PublicationFailed);
            Assert.False(result.EvidenceVerified);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ObserveCommittedAsync_NonCompletedJob_DoesNotTouchEvidenceStore()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-browser-publication-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var protector = new ThrowingProtector();
            var store = new DurableBrowserVerificationReceiptStore(Path.Combine(root, "receipt.bin"), protector);
            var boundary = new BrowserVerificationPublicationBoundary(new DurableBrowserVerificationPublisher(store));
            var job = CompletedJob(Guid.NewGuid()) with { State = AgentJobState.Failed };

            var result = await boundary.ObserveCommittedAsync(job);

            Assert.Same(job, result.AuthoritativeJob);
            Assert.False(result.Published);
            Assert.False(result.PublicationFailed);
            Assert.False(result.EvidenceVerified);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static AgentJobRecord CompletedJob(Guid jobId)
    {
        var now = DateTimeOffset.UtcNow;
        var evidence = new DurableBrowserActionEvidence(
            jobId,
            BrowserActionKind.Click,
            BrowserRiskLevel.High,
            Allowed: true,
            RequiredApproval: true,
            ApprovalObserved: true,
            DriverReportedSuccess: true,
            PostStateVerified: true,
            StartedAt: now.AddSeconds(-1),
            CompletedAt: now);
        var payload = System.Text.Json.JsonSerializer.Serialize(new { durableEvidence = evidence });
        var definition = new AgentJobDefinition(
            BrowserActionJobHandler.Type,
            BrowserHostRuntime.BrowserCapabilityId,
            new HashSet<Nvidea.Core.Capabilities.DataPermission>(),
            Nvidea.Core.Capabilities.CapabilityRiskLevel.High,
            ContainsPrivateOsData: true,
            BenefitsFromBackgroundExecution: false,
            MaxAttempts: 1);
        return new AgentJobRecord(
            jobId,
            definition,
            AgentJobState.Completed,
            new AgentJobCheckpoint("browser.action.verified", payload, now),
            Attempt: 1,
            CreatedAt: now.AddSeconds(-2),
            UpdatedAt: now,
            NextAttemptAt: null,
            LastError: null,
            ApprovalScope: null);
    }

    private sealed class ThrowingProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) =>
            throw new IOException("simulated evidence-store failure");

        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) =>
            throw new IOException("simulated evidence-store failure");
    }
}
