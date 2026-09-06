using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class EphemeralJobApprovalTests
{
    [Fact]
    public async Task Resumed_job_receives_ephemeral_single_use_grant_only_on_next_step()
    {
        var store = new InMemoryStore();
        var audit = new InMemoryAudit();
        var authorizer = new ScopedApprovalAuthorizer();
        var ephemeral = new EphemeralJobApprovalStore();
        var handler = new ApprovalAwareHandler("browser.submit:abc");
        var orchestrator = new ResumableJobOrchestrator(
            store,
            new ConservativeJobExecutionPolicy(),
            audit,
            new[] { handler },
            authorizer,
            ephemeral);

        var job = await orchestrator.CreateAsync(Definition());
        var paused = await orchestrator.RunNextStepAsync(job.JobId);
        Assert.Equal(AgentJobState.WaitingForApproval, paused.State);

        await orchestrator.ResumeAfterApprovalAsync(job.JobId, "browser.submit:abc");
        var completed = await orchestrator.RunNextStepAsync(job.JobId);

        Assert.Equal(AgentJobState.Completed, completed.State);
        Assert.NotNull(handler.ReceivedGrant);
        Assert.Equal("browser.submit:abc", handler.ReceivedGrant!.ApprovalScope);
        Assert.Null(ephemeral.Take(job.JobId));

        var decision = new PermissionDecision(
            true,
            true,
            CapabilityRiskLevel.High,
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            "approval required",
            "browser.submit:abc");
        Assert.True(authorizer.TryAuthorize(handler.ReceivedGrant, decision));
        Assert.False(authorizer.TryAuthorize(handler.ReceivedGrant, decision));
    }

    [Fact]
    public async Task Persisted_job_state_never_contains_approval_grant_material()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nvidea-approval-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "jobs.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var audit = new InMemoryAudit();
            var authorizer = new ScopedApprovalAuthorizer();
            var ephemeral = new EphemeralJobApprovalStore();
            var handler = new ApprovalAwareHandler("browser.submit:abc");
            var orchestrator = new ResumableJobOrchestrator(
                store,
                new ConservativeJobExecutionPolicy(),
                audit,
                new[] { handler },
                authorizer,
                ephemeral);

            var job = await orchestrator.CreateAsync(Definition());
            await orchestrator.RunNextStepAsync(job.JobId);
            await orchestrator.ResumeAfterApprovalAsync(job.JobId, "browser.submit:abc");

            var json = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("GrantId", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("GrantedAt", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExpiresAt", json, StringComparison.OrdinalIgnoreCase);

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Null(persisted!.ApprovalScope);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Execution_context_refuses_scope_substitution_and_consumes_exact_grant_once()
    {
        var grant = new ApprovalGrant(Guid.NewGuid(), "email.send:42", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1));
        var context = new JobExecutionContextFactory().Create(Guid.NewGuid(), grant);

        Assert.Null(context.TakeApproval("email.send:other"));
        Assert.Same(grant, context.TakeApproval("email.send:42"));
        Assert.Null(context.TakeApproval("email.send:42"));
    }

    private static AgentJobDefinition Definition() =>
        new("approval-aware", "browser.agent", new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.High, true, false);

    private sealed class ApprovalAwareHandler : IAgentJobHandler
    {
        private readonly string _scope;
        private int _step;

        public ApprovalAwareHandler(string scope) => _scope = scope;
        public string JobType => "approval-aware";
        public ApprovalGrant? ReceivedGrant { get; private set; }

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Context-aware overload must be used.");

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, JobExecutionContext executionContext, CancellationToken cancellationToken = default)
        {
            _step++;
            if (_step == 1)
                return Task.FromResult(new JobStepResult(false, true, _scope, "ready"));

            ReceivedGrant = executionContext.TakeApproval(_scope);
            if (ReceivedGrant is null)
                throw new UnauthorizedAccessException("Expected an exact ephemeral approval grant.");

            return Task.FromResult(new JobStepResult(true, CheckpointStep: "submitted"));
        }
    }

    private sealed class InMemoryStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _records = new();
        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records.TryGetValue(jobId, out var value) ? value : null);
        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentJobRecord>>(_records.Values.ToArray());
        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            _records[record.JobId] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAudit : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
    }

    private sealed class JobExecutionContextFactory
    {
        public JobExecutionContext Create(Guid jobId, ApprovalGrant grant)
        {
            var store = new EphemeralJobApprovalStore();
            store.Put(jobId, grant);
            return (JobExecutionContext)Activator.CreateInstance(
                typeof(JobExecutionContext),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { jobId, store.Take(jobId) },
                culture: null)!;
        }
    }
}
