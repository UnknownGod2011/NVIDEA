using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class CapabilityToolExecutorTests
{
    [Fact]
    public async Task ConsequentialTool_DoesNotExecuteWithoutFreshExactApproval()
    {
        var descriptor = new CapabilityDescriptor(
            "email.send",
            "1.0.0",
            "Email send",
            new HashSet<DataPermission> { DataPermission.EmailSend },
            CapabilityRiskLevel.High,
            true,
            "Send approved email.");
        var registry = new CapabilityRegistry(new[] { descriptor });
        var policy = new CapabilityPermissionPolicy(registry);
        var approvals = new ScopedApprovalAuthorizer();
        var audit = new InMemoryAuditTrail();
        var backend = new RecordingBackend();
        var executor = new CapabilityToolExecutor(policy, approvals, audit, backend);
        var invocation = new CapabilityInvocation(
            descriptor.Id,
            "send-draft-42",
            new HashSet<DataPermission> { DataPermission.EmailSend },
            CapabilityRiskLevel.High,
            true,
            Summary: "Send the reviewed draft.");
        var request = new CapabilityToolRequest(invocation, "email.send", new Dictionary<string, object?>());

        var waiting = await executor.ExecuteAsync(request);

        Assert.False(waiting.Executed);
        Assert.True(waiting.RequiresApproval);
        Assert.Equal(0, backend.ExecutionCount);

        var decision = policy.Evaluate(invocation);
        var grant = approvals.Grant(decision, TimeSpan.FromMinutes(2));
        var executed = await executor.ExecuteAsync(request with { Approval = grant });

        Assert.True(executed.Executed);
        Assert.Equal(1, backend.ExecutionCount);

        var replay = await executor.ExecuteAsync(request with { Approval = grant });
        Assert.False(replay.Executed);
        Assert.True(replay.RequiresApproval);
        Assert.Equal(1, backend.ExecutionCount);
    }

    [Fact]
    public async Task ResumedCheckpointCannotActAsAuthorization()
    {
        var descriptor = new CapabilityDescriptor(
            "browser.form",
            "1.0.0",
            "Browser form",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.High,
            true,
            "Submit a browser form.");
        var registry = new CapabilityRegistry(new[] { descriptor });
        var policy = new CapabilityPermissionPolicy(registry);
        var backend = new RecordingBackend();
        var executor = new CapabilityToolExecutor(policy, new ScopedApprovalAuthorizer(), new InMemoryAuditTrail(), backend);
        var invocation = new CapabilityInvocation(
            descriptor.Id,
            "job-abc:submit-step",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.High,
            true,
            Summary: "Checkpoint says user approved earlier, but no live grant is supplied.");

        var result = await executor.ExecuteAsync(new CapabilityToolRequest(
            invocation,
            "browser.submit",
            new Dictionary<string, object?> { ["checkpoint"] = "approved=true" }));

        Assert.False(result.Executed);
        Assert.True(result.RequiresApproval);
        Assert.Equal(0, backend.ExecutionCount);
    }

    [Fact]
    public async Task UndeclaredPermission_IsDeniedAndAudited()
    {
        var descriptor = new CapabilityDescriptor(
            "research.read",
            "1.0.0",
            "Research",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            false,
            "Read public research sources.");
        var registry = new CapabilityRegistry(new[] { descriptor });
        var policy = new CapabilityPermissionPolicy(registry);
        var audit = new InMemoryAuditTrail();
        var backend = new RecordingBackend();
        var executor = new CapabilityToolExecutor(policy, new ScopedApprovalAuthorizer(), audit, backend);
        var invocation = new CapabilityInvocation(
            descriptor.Id,
            "escalate",
            new HashSet<DataPermission> { DataPermission.NetworkAccess, DataPermission.FilesWrite },
            CapabilityRiskLevel.Low,
            false);

        var result = await executor.ExecuteAsync(new CapabilityToolRequest(invocation, "research.search", new Dictionary<string, object?>()));

        Assert.False(result.Allowed);
        Assert.Equal(0, backend.ExecutionCount);
        Assert.Contains(audit.Events, x => x.EventType == "tool.denied" && !x.Allowed);
    }

    private sealed class RecordingBackend : ICapabilityToolBackend
    {
        public int ExecutionCount { get; private set; }

        public Task<object?> ExecuteAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult<object?>(new { toolName });
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
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
}
