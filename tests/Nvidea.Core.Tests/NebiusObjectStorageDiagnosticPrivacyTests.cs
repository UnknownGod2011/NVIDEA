using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusObjectStorageDiagnosticPrivacyTests
{
    [Fact]
    public void CallerCancellation_IsRebuiltWithoutRawDiagnostic_AndPreservesToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sanitized = NebiusObjectStorageClient.CreateSanitizedCallerCancellation(cts.Token);

        Assert.Equal(cts.Token, sanitized.CancellationToken);
        Assert.Null(sanitized.InnerException);
        Assert.Equal("Nebius Object Storage request was canceled by the caller.", sanitized.Message);
        Assert.DoesNotContain("Bearer", sanitized.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", sanitized.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClientFailureDiagnostic_IsFixedAndCarriesNoProviderDetail()
    {
        const string secret = "Bearer super-secret-object-storage-token https://storage.example.invalid/a?token=secret";
        var raw = new HttpRequestException(secret);

        Assert.True(NebiusObjectStorageClient.IsQuarantinableClientFailure(raw));

        var sanitized = NebiusObjectStorageClient.CreateSanitizedClientFailure("get");

        Assert.Null(sanitized.InnerException);
        Assert.Equal(
            "Nebius Object Storage get request failed before a trusted provider response was available.",
            sanitized.Message);
        Assert.DoesNotContain("super-secret-object-storage-token", sanitized.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", sanitized.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", sanitized.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NetworkAndStreamFailures_AreQuarantinable_ButProgrammingFailuresAreNot()
    {
        Assert.True(NebiusObjectStorageClient.IsQuarantinableClientFailure(
            new HttpRequestException("proxy diagnostic")));
        Assert.True(NebiusObjectStorageClient.IsQuarantinableClientFailure(
            new IOException("stream diagnostic")));
        Assert.False(NebiusObjectStorageClient.IsQuarantinableClientFailure(
            new InvalidOperationException("local invariant")));
        Assert.False(NebiusObjectStorageClient.IsQuarantinableClientFailure(
            new ArgumentException("programming error")));
    }
}
