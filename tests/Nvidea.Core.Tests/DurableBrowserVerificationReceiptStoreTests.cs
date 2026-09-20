using System.Text;
using Nvidea.Core.Browser;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableBrowserVerificationReceiptStoreTests
{
    [Fact]
    public async Task WriteRead_RoundTripsProtectedIntegrityCheckedReceipt()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-receipt-");
        try
        {
            var path = Path.Combine(directory.FullName, "browser-verification.state");
            var store = new DurableBrowserVerificationReceiptStore(path, new TestProtector());
            var receipt = CreateReceipt("PRIVATE_VALUE_MUST_NOT_PERSIST");

            await store.WriteAsync(receipt);
            var raw = await File.ReadAllTextAsync(path);
            var restored = await store.ReadAsync();

            Assert.StartsWith("NVIDEA-STATE-V1\n", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("PRIVATE_VALUE_MUST_NOT_PERSIST", raw, StringComparison.Ordinal);
            Assert.NotNull(restored);
            Assert.True(restored!.HasValidIntegrity());
            Assert.True(restored.ToPresentation().Verified);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Read_RejectsPlaintextDowngrade()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-receipt-");
        try
        {
            var path = Path.Combine(directory.FullName, "browser-verification.state");
            await File.WriteAllTextAsync(path, "{\"version\":1}");
            var store = new DurableBrowserVerificationReceiptStore(path, new TestProtector());

            await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Read_RejectsProtectedPayloadWhoseReceiptWasTampered()
    {
        var directory = Directory.CreateTempSubdirectory("nvidea-browser-receipt-");
        try
        {
            var path = Path.Combine(directory.FullName, "browser-verification.state");
            var protector = new TestProtector();
            var receipt = CreateReceipt("secret");
            var tampered = receipt with
            {
                Actions = receipt.Actions.Select(static x => x with { PostStateVerified = false }).ToArray()
            };
            var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(tampered, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            var envelope = LocalStateEnvelope.Encode(json, protector, "browser-verification-receipt/v1");
            await File.WriteAllBytesAsync(path, envelope);
            var store = new DurableBrowserVerificationReceiptStore(path, protector);

            await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static DurableBrowserVerificationReceipt CreateReceipt(string marker)
    {
        var now = DateTimeOffset.Parse("2026-09-20T15:00:00Z");
        var source = new BrowserActionReceipt(
            Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", marker), Value: marker),
            new BrowserActionDecision(BrowserRiskLevel.High, true, true, "private rationale"),
            now,
            now.AddSeconds(1),
            true,
            true,
            "private verification",
            new Uri("https://private.example/before"),
            new Uri("https://private.example/after"),
            ApprovalGranted: true);
        return DurableBrowserVerificationReceipt.Create(new[] { source });
    }

    private sealed class TestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => Transform(plaintext, purpose);
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => Transform(protectedData, purpose);

        private static byte[] Transform(ReadOnlySpan<byte> input, string purpose)
        {
            var key = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(purpose));
            var output = input.ToArray();
            for (var i = 0; i < output.Length; i++)
                output[i] ^= key[i % key.Length];
            return output;
        }
    }
}
