using Nvidea.Core.Desktop;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserVerificationRuntimeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "nvidea-browser-verification-runtime-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SharedRuntime_BeginsFailClosed_AndUsesOneReceiptPath()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());

        var initial = await runtime.ReadPresentationAsync();
        Assert.False(initial.Verified);

        // A stale artifact is removed through the publication boundary before a new action is admitted.
        await File.WriteAllTextAsync(receiptPath, "stale-evidence-must-not-survive");
        await runtime.Publication.BeginActionAsync();

        Assert.False(File.Exists(receiptPath));
        var afterClear = await runtime.Publisher.ReadPresentationAsync();
        Assert.False(afterClear.Verified);
    }

    [Fact]
    public void ProductionReceiptName_IsDedicatedAndNonAuthorizing()
    {
        Assert.Equal("browser-verification-receipt.json.protected", BrowserVerificationRuntime.ReceiptFileName);
        Assert.DoesNotContain("approval", BrowserVerificationRuntime.ReceiptFileName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", BrowserVerificationRuntime.ReceiptFileName, StringComparison.OrdinalIgnoreCase);
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
