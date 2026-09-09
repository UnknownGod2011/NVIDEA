using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDispatchTests
{
    [Fact]
    public void Protector_RoundTripsCheckpointWithoutPlaintextLeakage()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var now = DateTimeOffset.UtcNow;
        var workItem = new RemoteResearchWorkItem(
            Guid.NewGuid(),
            "research.evidence.v1",
            "{\"question\":\"private research question\",\"evidence\":\"sensitive source content\"}",
            ContainsPrivateOsData: false,
            now,
            now.AddHours(1));

        var envelope = ResearchWorkItemProtector.Protect(workItem, publicKey);
        var serializedEnvelope = System.Text.Json.JsonSerializer.Serialize(envelope);

        Assert.DoesNotContain("private research question", serializedEnvelope, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive source content", serializedEnvelope, StringComparison.Ordinal);
        Assert.NotEqual(workItem.LocalJobId.ToString("D"), envelope.OpaqueWorkItemId);

        var restored = ResearchWorkItemProtector.Unprotect(envelope, privateKey, now.AddMinutes(1));
        Assert.Equal(workItem, restored);
    }

    [Fact]
    public void Protector_RejectsEnvelopeMetadataTampering()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = ValidWorkItem(now);
        var envelope = ResearchWorkItemProtector.Protect(workItem, rsa.ExportSubjectPublicKeyInfoPem());
        var replacementId = envelope.OpaqueWorkItemId[..^1] + (envelope.OpaqueWorkItemId[^1] == 'A' ? "B" : "A");

        Assert.Throws<CryptographicException>(() => ResearchWorkItemProtector.Unprotect(
            envelope with { OpaqueWorkItemId = replacementId },
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public async Task DispatchAsync_RequiresExactExplicitCloudApprovalBeforeUploadOrServerlessCall()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = ValidWorkItem(now);
        var transport = new RecordingTransport();
        var serverless = new RecordingServerlessClient();
        var dispatcher = CreateDispatcher(serverless, transport, rsa.ExportSubjectPublicKeyInfoPem());
        var authorization = new ResearchCloudAuthorization(
            Guid.NewGuid(),
            workItem.CheckpointStep,
            Approved: true,
            ResearchWorkItemProtector.DisclosureVersion,
            now);

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(workItem, authorization));

        Assert.Equal(0, transport.PutCalls);
        Assert.Equal(0, serverless.CreateCalls);
    }

    [Fact]
    public async Task DispatchAsync_RefusesPrivateOsDataEvenWithApproval()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = ValidWorkItem(now) with { ContainsPrivateOsData = true };
        var transport = new RecordingTransport();
        var serverless = new RecordingServerlessClient();
        var dispatcher = CreateDispatcher(serverless, transport, rsa.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(
            workItem,
            Approved(workItem, now)));

        Assert.Equal(0, transport.PutCalls);
        Assert.Equal(0, serverless.CreateCalls);
    }

    [Fact]
    public async Task DispatchAsync_PutsOnlyOpaqueIdInServerlessArgumentsAndPreservesEncryptedPayload()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = ValidWorkItem(now) with
        {
            CheckpointPayload = "{\"question\":\"compare private roadmap options\"}"
        };
        var transport = new RecordingTransport();
        var serverless = new RecordingServerlessClient();
        var dispatcher = CreateDispatcher(serverless, transport, rsa.ExportSubjectPublicKeyInfoPem());

        var receipt = await dispatcher.DispatchAsync(workItem, Approved(workItem, now));

        Assert.Equal(1, transport.PutCalls);
        Assert.Equal(1, serverless.CreateCalls);
        Assert.NotNull(transport.Stored);
        Assert.NotNull(serverless.LastSpec);
        Assert.Equal(receipt.OpaqueWorkItemId, transport.Stored!.OpaqueWorkItemId);
        Assert.Equal("aijob-remote-123", receipt.RemoteJobId);
        Assert.Contains(receipt.OpaqueWorkItemId, serverless.LastSpec!.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("compare private roadmap options", serverless.LastSpec.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("compare private roadmap options", string.Join(";", serverless.LastSpec.EnvironmentVariables!.Values), StringComparison.Ordinal);
        Assert.Equal(ResearchWorkItemProtector.ProtocolVersion, serverless.LastSpec.EnvironmentVariables["NVIDEA_RESEARCH_PROTOCOL"]);

        var restored = ResearchWorkItemProtector.Unprotect(
            transport.Stored,
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1));
        Assert.Equal(workItem, restored);
    }

    [Fact]
    public async Task DispatchAsync_DeletesEncryptedWorkItemWhenCreateFailsBeforeAcceptance()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = ValidWorkItem(now);
        var transport = new RecordingTransport();
        var serverless = new RecordingServerlessClient
        {
            CreateException = new HttpRequestException("temporary create failure")
        };
        var dispatcher = CreateDispatcher(serverless, transport, rsa.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<HttpRequestException>(() => dispatcher.DispatchAsync(workItem, Approved(workItem, now)));

        Assert.Equal(1, transport.PutCalls);
        Assert.Equal(1, transport.DeleteCalls);
        Assert.Null(transport.Stored);
    }

    [Fact]
    public async Task DispatchAsync_RetainsEncryptedWorkItemWhenCreateSucceededButResourceIdIsAmbiguous()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var workItem = ValidWorkItem(now);
        var transport = new RecordingTransport();
        var serverless = new RecordingServerlessClient
        {
            Response = new NebiusServerlessResponse(HttpStatusCode.OK, "{}")
        };
        var dispatcher = CreateDispatcher(serverless, transport, rsa.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(workItem, Approved(workItem, now)));

        Assert.Equal(1, transport.PutCalls);
        Assert.Equal(0, transport.DeleteCalls);
        Assert.NotNull(transport.Stored);
    }

    private static RemoteResearchWorkItem ValidWorkItem(DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            "research.planned.v1",
            "{\"queries\":[\"nemotron research\"]}",
            ContainsPrivateOsData: false,
            now,
            now.AddHours(2));

    private static ResearchCloudAuthorization Approved(RemoteResearchWorkItem workItem, DateTimeOffset now) =>
        new(
            workItem.LocalJobId,
            workItem.CheckpointStep,
            Approved: true,
            ResearchWorkItemProtector.DisclosureVersion,
            now);

    private static NebiusResearchDispatcher CreateDispatcher(
        INebiusServerlessJobClient client,
        IProtectedResearchWorkItemTransport transport,
        string publicKey) =>
        new(
            client,
            transport,
            new NebiusResearchDispatchOptions(
                WorkerImage: "ghcr.io/unknowngod2011/nvidea-worker:sha-test",
                WorkerPublicKeyPem: publicKey,
                ContainerCommand: "dotnet",
                Platform: "gpu-l40s-a",
                Preset: "1gpu-8vcpu-32gb",
                Timeout: "3600s",
                SubnetId: "vpcsubnet-test",
                Disk: new NebiusServerlessDiskSpec("NETWORK_SSD", 268435456000),
                SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>
                {
                    ["TAVILY_API_KEY"] = new(VersionId: "mbsecver-tavily"),
                    ["NEBIUS_TOKEN"] = new(VersionId: "mbsecver-nebius"),
                    ["NVIDEA_WORKER_PRIVATE_KEY"] = new(VersionId: "mbsecver-worker-key")
                }));

    private sealed class RecordingTransport : IProtectedResearchWorkItemTransport
    {
        public int PutCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public ProtectedResearchWorkItemEnvelope? Stored { get; private set; }

        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PutCalls++;
            Stored = envelope;
            return Task.CompletedTask;
        }

        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored is not null && string.Equals(Stored.OpaqueWorkItemId, opaqueWorkItemId, StringComparison.Ordinal)
                ? Stored
                : null);

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            if (Stored is not null && string.Equals(Stored.OpaqueWorkItemId, opaqueWorkItemId, StringComparison.Ordinal))
                Stored = null;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingServerlessClient : INebiusServerlessJobClient
    {
        public int CreateCalls { get; private set; }
        public NebiusServerlessJobSpec? LastSpec { get; private set; }
        public Exception? CreateException { get; init; }
        public NebiusServerlessResponse Response { get; init; } =
            new(HttpStatusCode.OK, "{\"resourceId\":\"aijob-remote-123\"}");

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastSpec = spec;
            if (CreateException is not null)
                return Task.FromException<NebiusServerlessResponse>(CreateException);
            return Task.FromResult(Response);
        }

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
