using System.Text;
using System.Text.Json;
using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableBrowserVerificationPublisherTests
{
    [Fact]
    public async Task CompletedVerifiedApprovedAction_PublishesPayloadFreePresentation()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-publisher-");
        try
        {
            var store = new DurableBrowserVerificationReceiptStore(Path.Combine(directory.FullName, "receipt.state"), new TestProtector());
            var publisher = new DurableBrowserVerificationPublisher(store);
            var jobId = Guid.NewGuid();
            var job = CreateJob(jobId, AgentJobState.Completed, CreateEvidence(jobId, requiredApproval: true, approvalObserved: true));

            Assert.True(await publisher.TryPublishCompletedAsync(job));
            var presentation = await publisher.ReadPresentationAsync();

            Assert.True(presentation.Verified);
            Assert.Equal(1, presentation.ActionCount);
            Assert.Equal(1, presentation.ApprovedConsequentialActionCount);
        }
        finally { directory.Delete(recursive: true); }
    }

    [Theory]
    [InlineData(AgentJobState.Pending)]
    [InlineData(AgentJobState.Running)]
    [InlineData(AgentJobState.WaitingForApproval)]
    [InlineData(AgentJobState.Failed)]
    [InlineData(AgentJobState.Cancelled)]
    public async Task NonCompletedState_NeverPublishes(AgentJobState state)
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-publisher-");
        try
        {
            var publisher = new DurableBrowserVerificationPublisher(
                new DurableBrowserVerificationReceiptStore(Path.Combine(directory.FullName, "receipt.state"), new TestProtector()));
            var jobId = Guid.NewGuid();

            Assert.False(await publisher.TryPublishCompletedAsync(CreateJob(jobId, state, CreateEvidence(jobId, false, false))));
            Assert.False((await publisher.ReadPresentationAsync()).Verified);
        }
        finally { directory.Delete(recursive: true); }
    }

    [Fact]
    public async Task CrossJobEvidence_IsRejectedBeforePersistence()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-publisher-");
        try
        {
            var publisher = new DurableBrowserVerificationPublisher(
                new DurableBrowserVerificationReceiptStore(Path.Combine(directory.FullName, "receipt.state"), new TestProtector()));
            var job = CreateJob(Guid.NewGuid(), AgentJobState.Completed, CreateEvidence(Guid.NewGuid(), false, false));

            await Assert.ThrowsAsync<InvalidDataException>(() => publisher.TryPublishCompletedAsync(job));
            Assert.False((await publisher.ReadPresentationAsync()).Verified);
        }
        finally { directory.Delete(recursive: true); }
    }

    [Fact]
    public async Task ConsequentialCompletionWithoutObservedApproval_IsRejected()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-publisher-");
        try
        {
            var publisher = new DurableBrowserVerificationPublisher(
                new DurableBrowserVerificationReceiptStore(Path.Combine(directory.FullName, "receipt.state"), new TestProtector()));
            var jobId = Guid.NewGuid();

            await Assert.ThrowsAsync<InvalidDataException>(() => publisher.TryPublishCompletedAsync(
                CreateJob(jobId, AgentJobState.Completed, CreateEvidence(jobId, true, false))));
            Assert.False((await publisher.ReadPresentationAsync()).Verified);
        }
        finally { directory.Delete(recursive: true); }
    }

    [Fact]
    public async Task BeginAction_ClearsPreviouslyVerifiedEvidence()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-publisher-");
        try
        {
            var publisher = new DurableBrowserVerificationPublisher(
                new DurableBrowserVerificationReceiptStore(Path.Combine(directory.FullName, "receipt.state"), new TestProtector()));
            var jobId = Guid.NewGuid();
            Assert.True(await publisher.TryPublishCompletedAsync(
                CreateJob(jobId, AgentJobState.Completed, CreateEvidence(jobId, false, false))));
            Assert.True((await publisher.ReadPresentationAsync()).Verified);

            await publisher.BeginActionAsync();

            Assert.False((await publisher.ReadPresentationAsync()).Verified);
        }
        finally { directory.Delete(recursive: true); }
    }

    private static AgentJobRecord CreateJob(Guid jobId, AgentJobState state, DurableBrowserActionEvidence evidence)
    {
        var now = DateTimeOffset.Parse("2026-09-20T18:00:00Z");
        var payload = JsonSerializer.Serialize(new { durableEvidence = evidence }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return new AgentJobRecord(
            jobId,
            new AgentJobDefinition("browser.action", "browser.agent", new HashSet<DataPermission> { DataPermission.BrowserWrite }, CapabilityRiskLevel.High, true, false),
            state,
            JobExecutionLocation.Local,
            1,
            new AgentJobCheckpoint("browser.action.verified", payload, now.AddSeconds(2)),
            null,
            null,
            now,
            now.AddSeconds(2));
    }

    private static DurableBrowserActionEvidence CreateEvidence(Guid actionId, bool requiredApproval, bool approvalObserved)
    {
        var now = DateTimeOffset.Parse("2026-09-20T18:00:00Z");
        return new DurableBrowserActionEvidence(
            actionId,
            BrowserActionKind.Click,
            requiredApproval ? BrowserRiskLevel.High : BrowserRiskLevel.Low,
            Allowed: true,
            RequiredApproval: requiredApproval,
            ApprovalObserved: approvalObserved,
            DriverReportedSuccess: true,
            PostStateVerified: true,
            StartedAt: now,
            CompletedAt: now.AddSeconds(1));
    }

    private sealed class TestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => Transform(plaintext, purpose);
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => Transform(protectedData, purpose);

        private static byte[] Transform(ReadOnlySpan<byte> input, string purpose)
        {
            var key = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(purpose));
            var output = input.ToArray();
            for (var i = 0; i < output.Length; i++) output[i] ^= key[i % key.Length];
            return output;
        }
    }
}
