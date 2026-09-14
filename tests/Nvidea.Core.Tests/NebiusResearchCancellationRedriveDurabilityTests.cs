using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchCancellationRedriveDurabilityTests
{
    [Fact]
    public async Task ReconcileCancellationAsync_ReusesOneDurableActionAcrossAmbiguousDeliveryAndClearsAfterProviderProgress()
    {
        var root = CreateTempDirectory();
        try
        {
            var fixture = await CreateFixtureAsync(root);
            fixture.Client.GetResponse = RunningResponse(fixture.ExpectedName);

            await fixture.Reconciler.RequestCancellationAsync(fixture.JobId);
            Assert.Equal(1, fixture.Client.CancelCalls);

            fixture.Client.RemainingCancelFailures = 1;
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId));

            var afterAmbiguousDelivery = await fixture.Store.GetAsync(fixture.JobId);
            Assert.NotNull(afterAmbiguousDelivery);
            Assert.Null(afterAmbiguousDelivery!.PendingAuditEvent);
            var durableAction = Assert.IsType<PendingExternalAction>(afterAmbiguousDelivery.PendingExternalAction);
            Assert.Equal(DurableExternalActionKind.NebiusCancelRemoteResearch, durableAction.Kind);
            Assert.Equal("job-123", durableAction.TargetId);
            Assert.Single(fixture.Audit.Events.Where(e => e.EventType == "research.remote_cancel_redriven"));
            Assert.Equal(2, fixture.Client.CancelCalls);

            var redrivenAgain = await fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId);
            var reusedAction = Assert.IsType<PendingExternalAction>(redrivenAgain.PendingExternalAction);

            Assert.Equal(durableAction.ActionId, reusedAction.ActionId);
            Assert.Equal(durableAction.AuditEventId, reusedAction.AuditEventId);
            Assert.Single(fixture.Audit.Events.Where(e => e.EventType == "research.remote_cancel_redriven"));
            Assert.Equal(3, fixture.Client.CancelCalls);

            fixture.Client.GetResponse = CancellingResponse(fixture.ExpectedName);
            var progressed = await fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId);

            Assert.Null(progressed.PendingExternalAction);
            Assert.Null(progressed.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, progressed.RemoteResearch!.State);
            Assert.Equal(3, fixture.Client.CancelCalls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileCancellationAsync_AuditFailurePersistsActionAndBlocksProviderDeliveryUntilRecovery()
    {
        var root = CreateTempDirectory();
        try
        {
            var fixture = await CreateFixtureAsync(root);
            fixture.Client.GetResponse = RunningResponse(fixture.ExpectedName);
            await fixture.Reconciler.RequestCancellationAsync(fixture.JobId);

            fixture.Audit.FailEventType = "research.remote_cancel_redriven";
            var cancelCallsBeforeRedrive = fixture.Client.CancelCalls;

            await Assert.ThrowsAsync<IOException>(() =>
                fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId));

            var stranded = await fixture.Store.GetAsync(fixture.JobId);
            Assert.NotNull(stranded);
            var pendingAction = Assert.IsType<PendingExternalAction>(stranded!.PendingExternalAction);
            var pendingAudit = Assert.IsType<AuditEvent>(stranded.PendingAuditEvent);
            Assert.Equal(pendingAction.AuditEventId, pendingAudit.EventId);
            Assert.Equal("research.remote_cancel_redriven", pendingAudit.EventType);
            Assert.Equal(cancelCallsBeforeRedrive, fixture.Client.CancelCalls);
            Assert.DoesNotContain(fixture.Audit.Events, e => e.EventType == "research.remote_cancel_redriven");

            fixture.Audit.FailEventType = null;
            var recovered = await fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId);

            var recoveredAction = Assert.IsType<PendingExternalAction>(recovered.PendingExternalAction);
            Assert.Equal(pendingAction.ActionId, recoveredAction.ActionId);
            Assert.Equal(pendingAction.AuditEventId, recoveredAction.AuditEventId);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(cancelCallsBeforeRedrive + 1, fixture.Client.CancelCalls);
            Assert.Single(fixture.Audit.Events.Where(e => e.EventType == "research.remote_cancel_redriven"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileCancellationAsync_RejectsSubstitutedDurableActionBeforeProviderObservationOrDelivery()
    {
        var root = CreateTempDirectory();
        try
        {
            var fixture = await CreateFixtureAsync(root);
            fixture.Client.GetResponse = RunningResponse(fixture.ExpectedName);
            await fixture.Reconciler.RequestCancellationAsync(fixture.JobId);

            fixture.Client.RemainingCancelFailures = 1;
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId));

            var durable = await fixture.Store.GetAsync(fixture.JobId);
            Assert.NotNull(durable);
            var pending = Assert.IsType<PendingExternalAction>(durable!.PendingExternalAction);
            await fixture.Store.SaveAsync(durable with
            {
                PendingExternalAction = pending with { TargetId = "job-substituted" }
            });

            var getCallsBefore = fixture.Client.GetCalls;
            var cancelCallsBefore = fixture.Client.CancelCalls;

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                fixture.Reconciler.ReconcileCancellationAsync(fixture.JobId));

            Assert.Equal(getCallsBefore, fixture.Client.GetCalls);
            Assert.Equal(cancelCallsBefore, fixture.Client.CancelCalls);
            var unchanged = await fixture.Store.GetAsync(fixture.JobId);
            Assert.Equal("job-substituted", unchanged!.PendingExternalAction!.TargetId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<Fixture> CreateFixtureAsync(string root)
    {
        var rsa = RSA.Create(2048);
        var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
        var audit = new FaultInjectingAuditTrail();
        var ingestor = new RemoteResearchResultIngestor(
            store,
            new NullResultTransport(),
            rsa.ExportPkcs8PrivateKeyPem(),
            audit);
        var now = DateTimeOffset.UtcNow;
        var original = CreatePendingJob(now);
        await store.SaveAsync(original);

        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
        await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
            original.JobId,
            original.Checkpoint!.Step,
            opaqueId,
            now,
            now.AddMinutes(20)));
        await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
            original.JobId,
            original.Checkpoint.Step,
            opaqueId,
            "job-123",
            now));

        var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
        var client = new RecoverableServerlessClient();
        var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);
        return new Fixture(original.JobId, expectedName, rsa, store, audit, client, reconciler);
    }

    private static NebiusServerlessResponse RunningResponse(string expectedName) =>
        new(HttpStatusCode.OK,
            $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""");

    private static NebiusServerlessResponse CancellingResponse(string expectedName) =>
        new(HttpStatusCode.OK,
            $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"CANCELLING"}}""");

    private static AgentJobRecord CreatePendingJob(DateTimeOffset now) => new(
        Guid.NewGuid(),
        new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true,
            MaxAttempts: 3),
        AgentJobState.Pending,
        JobExecutionLocation.Local,
        Attempt: 0,
        new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
        ApprovalScope: null,
        LastError: null,
        CreatedAt: now,
        UpdatedAt: now);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nvidea-nebius-cancel-redrive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record Fixture(
        Guid JobId,
        string ExpectedName,
        RSA Rsa,
        JsonAgentJobStore Store,
        FaultInjectingAuditTrail Audit,
        RecoverableServerlessClient Client,
        NebiusResearchLifecycleReconciler Reconciler);

    private sealed class RecoverableServerlessClient : INebiusServerlessJobClient
    {
        public int GetCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public int RemainingCancelFailures { get; set; }
        public NebiusServerlessResponse GetResponse { get; set; } = new(HttpStatusCode.OK, "{}");

        public Task<NebiusServerlessResponse> CreateAsync(
            NebiusServerlessJobSpec spec,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> GetAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(GetResponse);
        }

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{\"items\":[]}"));

        public Task<NebiusServerlessResponse> ListAsync(
            string? pageToken,
            CancellationToken cancellationToken = default) =>
            ListAsync(cancellationToken);

        public Task<NebiusServerlessResponse> CancelAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            if (RemainingCancelFailures > 0)
            {
                RemainingCancelFailures--;
                throw new HttpRequestException("simulated ambiguous cancellation delivery");
            }

            return Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"));
        }
    }

    private sealed class FaultInjectingAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();
        public string? FailEventType { get; set; }

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (string.Equals(auditEvent.EventType, FailEventType, StringComparison.Ordinal))
                throw new IOException("simulated audit persistence failure");

            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
    }

    private sealed class NullResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
