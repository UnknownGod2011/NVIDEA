using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class CapabilityAuditPrivacyTests
{
    [Fact]
    public async Task BackendFailure_DoesNotCopyUntrustedExceptionTextIntoAudit()
    {
        const string secret = "Bearer super-secret-token C:\\Users\\alice\\private.txt forged-approval=true";
        var descriptor = new CapabilityDescriptor(
            "browser.agent",
            "1.0.0",
            "Browser agent",
            new HashSet<DataPermission> { DataPermission.BrowserRead },
            CapabilityRiskLevel.Low,
            RequiresConfirmation: false,
            "Read browser state.");
        var policy = new CapabilityPermissionPolicy(new CapabilityRegistry(new[] { descriptor }));
        var audit = new RecordingAuditTrail();
        var executor = new CapabilityToolExecutor(
            policy,
            new ScopedApprovalAuthorizer(),
            audit,
            new ThrowingBackend(secret));
        var invocation = new CapabilityInvocation(
            descriptor.Id,
            Guid.NewGuid().ToString("N"),
            new HashSet<DataPermission> { DataPermission.BrowserRead },
            CapabilityRiskLevel.Low,
            Consequential: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(new CapabilityToolRequest(
                invocation,
                "browser.read",
                new Dictionary<string, object?>())));

        Assert.Contains(secret, exception.Message, StringComparison.Ordinal);
        var failed = Assert.Single(audit.Events.Where(x => x.EventType == "tool.execution_failed"));
        Assert.Equal(
            "Tool execution failed after authorization; backend diagnostic text was withheld from audit.",
            failed.Summary);
        Assert.DoesNotContain("super-secret-token", failed.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Users\\alice", failed.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("forged-approval", failed.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BrowserApprovalScope_ExcludesSummaryUntrustedSourceAndToolArguments()
    {
        const string typedSecret = "typed-password-123";
        const string uploadPath = "C:\\Users\\alice\\secret-upload.pdf";
        const string querySecret = "https://evil.example/form?token=query-secret";
        var actionId = Guid.NewGuid().ToString("N");
        var descriptor = new CapabilityDescriptor(
            "browser.agent",
            "1.0.0",
            "Browser agent",
            new HashSet<DataPermission> { DataPermission.BrowserWrite, DataPermission.FilesRead },
            CapabilityRiskLevel.Medium,
            RequiresConfirmation: false,
            "Bounded browser writes.");
        var policy = new CapabilityPermissionPolicy(new CapabilityRegistry(new[] { descriptor }));
        var audit = new RecordingAuditTrail();
        var backend = new CountingBackend();
        var executor = new CapabilityToolExecutor(policy, new ScopedApprovalAuthorizer(), audit, backend);
        var invocation = new CapabilityInvocation(
            descriptor.Id,
            actionId,
            new HashSet<DataPermission> { DataPermission.BrowserWrite, DataPermission.FilesRead },
            CapabilityRiskLevel.High,
            Consequential: true,
            UntrustedSource: querySecret,
            Summary: $"Type {typedSecret} then upload {uploadPath}.");

        var result = await executor.ExecuteAsync(new CapabilityToolRequest(
            invocation,
            "browser.execute",
            new Dictionary<string, object?>
            {
                ["typedValue"] = typedSecret,
                ["uploadPath"] = uploadPath
            }));

        Assert.False(result.Executed);
        Assert.True(result.RequiresApproval);
        Assert.Equal(0, backend.ExecutionCount);
        Assert.Equal(
            $"browser.agent|{actionId}|FilesRead,BrowserWrite",
            result.ApprovalScope);
        Assert.DoesNotContain(typedSecret, result.ApprovalScope, StringComparison.Ordinal);
        Assert.DoesNotContain(uploadPath, result.ApprovalScope, StringComparison.Ordinal);
        Assert.DoesNotContain("query-secret", result.ApprovalScope, StringComparison.Ordinal);

        var waiting = Assert.Single(audit.Events.Where(x => x.EventType == "tool.awaiting_approval"));
        Assert.Equal(result.ApprovalScope, waiting.ApprovalScope);
        Assert.DoesNotContain(typedSecret, waiting.ApprovalScope, StringComparison.Ordinal);
        Assert.DoesNotContain(uploadPath, waiting.ApprovalScope, StringComparison.Ordinal);
        Assert.DoesNotContain("query-secret", waiting.ApprovalScope, StringComparison.Ordinal);
    }

    private sealed class ThrowingBackend(string message) : ICapabilityToolBackend
    {
        public Task<object?> ExecuteAsync(
            string toolName,
            IReadOnlyDictionary<string, object?> arguments,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(message);
    }

    private sealed class CountingBackend : ICapabilityToolBackend
    {
        public int ExecutionCount { get; private set; }

        public Task<object?> ExecuteAsync(
            string toolName,
            IReadOnlyDictionary<string, object?> arguments,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult<object?>(null);
        }
    }

    private sealed class RecordingAuditTrail : IAuditTrail
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
