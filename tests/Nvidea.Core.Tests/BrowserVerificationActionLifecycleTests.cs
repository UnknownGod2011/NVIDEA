using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserVerificationActionLifecycleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "nvidea-browser-verification-lifecycle-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AdmitAsync_ClearsStaleEvidenceBeforeDurableCreate()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        await File.WriteAllTextAsync(receiptPath, "stale-green-evidence");

        var createObservedReceipt = true;
        var expected = CreateJob(AgentJobState.Pending);

        var actual = await lifecycle.AdmitAsync(_ =>
        {
            createObservedReceipt = File.Exists(receiptPath);
            return Task.FromResult(expected);
        });

        Assert.Same(expected, actual);
        Assert.False(createObservedReceipt);
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public async Task AdmitAsync_ClearFailure_PreventsDurableCreate()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        Directory.CreateDirectory(receiptPath); // File.Delete must fail before admission.
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        var createCalled = false;

        await Assert.ThrowsAnyAsync<Exception>(() => lifecycle.AdmitAsync(_ =>
        {
            createCalled = true;
            return Task.FromResult(CreateJob(AgentJobState.Pending));
        }));

        Assert.False(createCalled);
    }

    [Fact]
    public async Task AdvanceAsync_NonCompletedRecord_IsReturnedUnchangedWithoutPublication()
    {
        Directory.CreateDirectory(_root);
        var runtime = BrowserVerificationRuntime.Create(
            Path.Combine(_root, "receipt.protected"),
            new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        var authoritative = CreateJob(AgentJobState.WaitingForApproval);

        var result = await lifecycle.AdvanceAsync(_ => Task.FromResult(authoritative));

        Assert.Same(authoritative, result.AuthoritativeJob);
        Assert.False(result.Published);
        Assert.False(result.PublicationFailed);
        Assert.False(result.EvidenceVerified);
    }

    private static AgentJobRecord CreateJob(AgentJobState state)
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                "browser.action",
                "browser.agent",
                new HashSet<DataPermission> { DataPermission.BrowserRead },
                CapabilityRiskLevel.Medium,
                ContainsPrivateOsData: true,
                BenefitsFromBackgroundExecution: false),
            state,
            JobExecutionLocation.Local,
            Attempt: 0,
            Checkpoint: null,
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best-effort test cleanup only.
        }
    }

    private sealed class ReversibleTestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
