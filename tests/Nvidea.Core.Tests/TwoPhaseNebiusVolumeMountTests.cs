using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class TwoPhaseNebiusVolumeMountTests
{
    [Fact]
    public async Task StartPreparedAsync_PassesConfiguredSharedTransportVolumeToNebius()
    {
        using var workerRsa = RSA.Create(2048);
        NebiusServerlessJobSpec? captured = null;
        var serverless = new CapturingServerlessClient(spec =>
        {
            captured = spec;
            return new NebiusServerlessResponse(HttpStatusCode.OK, "{\"resourceId\":\"aijob-mounted\"}");
        });
        var transport = new MemoryWorkItemTransport();
        var options = new NebiusResearchDispatchOptions(
            WorkerImage: "registry.example/nvidea-worker:sha256-test",
            WorkerPublicKeyPem: workerRsa.ExportSubjectPublicKeyInfoPem(),
            ContainerCommand: "dotnet",
            Platform: "cpu-d3",
            Preset: "1vcpu-4gb",
            Timeout: "3600s",
            SubnetId: "subnet-test",
            Disk: new NebiusServerlessDiskSpec("NETWORK_SSD", 10L * 1024 * 1024 * 1024),
            EnvironmentVariables: new Dictionary<string, string>
            {
                ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea-research"
            },
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount(
                    Source: "nvidea-research-transport",
                    ContainerPath: "/mnt/nvidea-research",
                    Mode: "READ_WRITE")
            });
        var dispatcher = new TwoPhaseNebiusResearchDispatcher(serverless, transport, options);
        var now = DateTimeOffset.UtcNow;
        var workItem = new RemoteResearchWorkItem(
            Guid.NewGuid(),
            "planned",
            "{}",
            ContainsPrivateOsData: false,
            CreatedAt: now,
            ExpiresAt: now.AddHours(1));
        var authorization = new ResearchCloudAuthorization(
            workItem.LocalJobId,
            workItem.CheckpointStep,
            Approved: true,
            ResearchWorkItemProtector.DisclosureVersion,
            GrantedAt: now);

        var prepared = await dispatcher.PrepareAsync(workItem, authorization);
        var receipt = await dispatcher.StartPreparedAsync(prepared);

        Assert.Equal("aijob-mounted", receipt.RemoteJobId);
        Assert.NotNull(captured);
        Assert.Single(captured!.Volumes!);
        Assert.Equal("nvidea-research-transport", captured.Volumes![0].Source);
        Assert.Equal("/mnt/nvidea-research", captured.Volumes[0].ContainerPath);
        Assert.Equal("READ_WRITE", captured.Volumes[0].Mode);
        Assert.Equal("/mnt/nvidea-research", captured.EnvironmentVariables!["NVIDEA_TRANSPORT_ROOT"]);
        Assert.Contains(prepared.OpaqueWorkItemId, captured.Arguments, StringComparison.Ordinal);
    }

    private sealed class CapturingServerlessClient : INebiusServerlessJobClient
    {
        private readonly Func<NebiusServerlessJobSpec, NebiusServerlessResponse> _create;

        public CapturingServerlessClient(Func<NebiusServerlessJobSpec, NebiusServerlessResponse> create) => _create = create;

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default) =>
            Task.FromResult(_create(spec));

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MemoryWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        private readonly Dictionary<string, ProtectedResearchWorkItemEnvelope> _items = new(StringComparer.Ordinal);

        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default)
        {
            _items[envelope.OpaqueWorkItemId] = envelope;
            return Task.CompletedTask;
        }

        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(opaqueWorkItemId, out var value);
            return Task.FromResult(value);
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            _items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }
}
