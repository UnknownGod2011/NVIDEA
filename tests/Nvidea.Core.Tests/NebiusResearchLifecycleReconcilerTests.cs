using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLifecycleReconcilerTests
{
    [Fact]
    public async Task ReconcileReservedAsync_AttachesOnlyUniqueExactRecognizedAndVerifiedMatch()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, new NullResultTransport(), rsa.ExportPkcs8PrivateKeyPem(), audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(original.JobId, original.Checkpoint!.Step, opaqueId, now));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient
            {
                ListResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"items":[{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}},{"metadata":{"id":"job-other","name":"other"},"status":{"state":"RUNNING"}}]}"""),
                GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var attached = await reconciler.ReconcileReservedAsync(original.JobId);

            Assert.Equal(JobExecutionLocation.NebiusServerless, attached.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, attached.RemoteResearch!.State);
            Assert.Equal("job-123", attached.RemoteResearch.RemoteJobId);
            Assert.Equal(1, attached.Attempt);
            Assert.Equal("job-123", client.LastGetId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileReservedAsync_SearchesAllBoundedPagesBeforeAttachingUniqueMatch()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, new NullResultTransport(), rsa.ExportPkcs8PrivateKeyPem(), audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(original.JobId, original.Checkpoint!.Step, opaqueId, now));
            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);

            var client = new FakeServerlessClient
            {
                ListResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    "{\"items\":[{\"metadata\":{\"id\":\"job-other\",\"name\":\"other\"},\"status\":{\"state\":\"RUNNING\"}}],\"nextPageToken\":\"page-2\"}"),
                GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""")
            };
            client.PageResponses["page-2"] = new NebiusServerlessResponse(HttpStatusCode.OK,
                $$"""{"items":[{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}]}""");
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var attached = await reconciler.ReconcileReservedAsync(original.JobId);

            Assert.Equal("job-123", attached.RemoteResearch!.RemoteJobId);
            Assert.Equal(new string?[] { null, "page-2" }, client.ListTokens);
            Assert.Equal("job-123", client.LastGetId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileReservedAsync_RejectsUnknownOrAmbiguousMatchesAcrossPages()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, new NullResultTransport(), rsa.ExportPkcs8PrivateKeyPem(), audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(original.JobId, original.Checkpoint!.Step, opaqueId, now));
            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient();
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            client.ListResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                $$"""{"items":[{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"NEW_FUTURE_STATE"}}]}""");
            await Assert.ThrowsAsync<InvalidOperationException>(() => reconciler.ReconcileReservedAsync(original.JobId));

            client.ListResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                $$"""{"items":[{"metadata":{"id":"job-1","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}],"nextPageToken":"more"}""");
            client.PageResponses["more"] = new NebiusServerlessResponse(HttpStatusCode.OK,
                $$"""{"items":[{"metadata":{"id":"job-2","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}]}""");
            await Assert.ThrowsAsync<InvalidOperationException>(() => reconciler.ReconcileReservedAsync(original.JobId));

            var unchanged = await store.GetAsync(original.JobId);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, unchanged!.RemoteResearch!.State);
            Assert.Null(unchanged.RemoteResearch.RemoteJobId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileDispatchedAsync_PersistsRemoteFailureAsTerminalLocalFailure()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, new NullResultTransport(), rsa.ExportPkcs8PrivateKeyPem(), audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(original.JobId, original.Checkpoint!.Step, opaqueId, now, now.AddMinutes(20)));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(original.JobId, original.Checkpoint.Step, opaqueId, "job-123", now));
            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient
            {
                GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"FAILED"}}""")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var failed = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(1));

            Assert.Equal(AgentJobState.Failed, failed.State);
            Assert.Equal(JobExecutionLocation.Local, failed.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.RemoteFailed, failed.RemoteResearch!.State);
            Assert.NotNull(failed.RemoteResearch.TerminalAt);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_failed");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileDispatchedAsync_WaitsForDelayedCompletedResultUntilPersistedExpiry()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, new NullResultTransport(), rsa.ExportPkcs8PrivateKeyPem(), audit);
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(10);
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(original.JobId, original.Checkpoint!.Step, opaqueId, now, expiresAt));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(original.JobId, original.Checkpoint.Step, opaqueId, "job-123", now));
            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient
            {
                GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"COMPLETED"}}""")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var waiting = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(5));
            Assert.Equal(AgentJobState.Running, waiting.State);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, waiting.RemoteResearch!.State);
            Assert.Equal(expiresAt, waiting.RemoteResearch.WorkItemExpiresAt);

            var expired = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(11));
            Assert.Equal(AgentJobState.Failed, expired.State);
            Assert.Equal(JobExecutionLocation.Local, expired.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Expired, expired.RemoteResearch!.State);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_result_expired");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Cancellation_PersistsIntentBeforeProviderCall_ThenFinalizesOnlyOnConfirmedCancelled()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, new NullResultTransport(), rsa.ExportPkcs8PrivateKeyPem(), audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(original.JobId, original.Checkpoint!.Step, opaqueId, now));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(original.JobId, original.Checkpoint.Step, opaqueId, "job-123", now));
            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);

            var client = new FakeServerlessClient();
            client.OnCancel = async () =>
            {
                var durable = await store.GetAsync(original.JobId);
                Assert.Equal(RemoteResearchProvenanceState.CancelRequested, durable!.RemoteResearch!.State);
            };
            client.GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"CANCELLED"}}""");
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var requested = await reconciler.RequestCancellationAsync(original.JobId);
            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, requested.RemoteResearch!.State);
            Assert.Equal(AgentJobState.Running, requested.State);

            var cancelled = await reconciler.ReconcileCancellationAsync(original.JobId);
            Assert.Equal(AgentJobState.Cancelled, cancelled.State);
            Assert.Equal(JobExecutionLocation.Local, cancelled.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Cancelled, cancelled.RemoteResearch!.State);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_cancel_requested");
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_cancelled");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SnapshotParser_UsesCurrentNebiusStatesAndFailsClosedOnUnknownState()
    {
        Assert.Equal(NebiusRemoteJobState.Pending, NebiusServerlessJobSnapshotParser.ParseState("PROVISIONING"));
        Assert.Equal(NebiusRemoteJobState.Pending, NebiusServerlessJobSnapshotParser.ParseState("STARTING"));
        Assert.Equal(NebiusRemoteJobState.Running, NebiusServerlessJobSnapshotParser.ParseState("RUNNING"));
        Assert.Equal(NebiusRemoteJobState.Cancelling, NebiusServerlessJobSnapshotParser.ParseState("CANCELLING"));
        Assert.Equal(NebiusRemoteJobState.Completed, NebiusServerlessJobSnapshotParser.ParseState("COMPLETED"));
        Assert.Equal(NebiusRemoteJobState.Failed, NebiusServerlessJobSnapshotParser.ParseState("FAILED"));
        Assert.Equal(NebiusRemoteJobState.Failed, NebiusServerlessJobSnapshotParser.ParseState("ERROR"));
        Assert.Equal(NebiusRemoteJobState.Cancelled, NebiusServerlessJobSnapshotParser.ParseState("CANCELLED"));
        Assert.Equal(NebiusRemoteJobState.Unknown, NebiusServerlessJobSnapshotParser.ParseState("SOMETHING_NEW"));
    }

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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-nebius-lifecycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeServerlessClient : INebiusServerlessJobClient
    {
        public NebiusServerlessResponse ListResponse { get; set; } = new(HttpStatusCode.OK, "{\"items\":[]}");
        public Dictionary<string, NebiusServerlessResponse> PageResponses { get; } = new(StringComparer.Ordinal);
        public List<string?> ListTokens { get; } = new();
        public NebiusServerlessResponse GetResponse { get; set; } = new(HttpStatusCode.OK, "{}");
        public Func<Task>? OnCancel { get; set; }
        public string? LastGetId { get; private set; }

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default)
        {
            LastGetId = remoteJobId;
            return Task.FromResult(GetResponse);
        }

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            ListAsync(null, cancellationToken);

        public Task<NebiusServerlessResponse> ListAsync(string? pageToken, CancellationToken cancellationToken = default)
        {
            ListTokens.Add(pageToken);
            if (string.IsNullOrWhiteSpace(pageToken))
                return Task.FromResult(ListResponse);
            return PageResponses.TryGetValue(pageToken, out var response)
                ? Task.FromResult(response)
                : throw new InvalidOperationException($"Unexpected test page token '{pageToken}'.");
        }

        public async Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default)
        {
            if (OnCancel is not null)
                await OnCancel();
            return new NebiusServerlessResponse(HttpStatusCode.OK, "{}");
        }
    }

    private sealed class NullResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.FromResult<ProtectedResearchResultEnvelope?>(null);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
