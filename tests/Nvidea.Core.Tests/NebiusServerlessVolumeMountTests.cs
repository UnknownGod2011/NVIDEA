using System.Net;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusServerlessVolumeMountTests
{
    [Fact]
    public async Task CreateAsync_SerializesDocumentedVolumeMountShape()
    {
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, "{\"resourceId\":\"aijob-volume\"}");
        });
        var client = CreateClient(handler);
        var spec = ValidSpec() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount(
                    Source: "nvidea-research-transport",
                    ContainerPath: "/mnt/nvidea-research",
                    Mode: "READ_WRITE",
                    SourcePath: "remote-research")
            }
        };

        await client.CreateAsync(spec);

        using var json = JsonDocument.Parse(body!);
        var volumes = json.RootElement.GetProperty("spec").GetProperty("volumes");
        Assert.Equal(1, volumes.GetArrayLength());
        Assert.Equal("nvidea-research-transport", volumes[0].GetProperty("source").GetString());
        Assert.Equal("remote-research", volumes[0].GetProperty("sourcePath").GetString());
        Assert.Equal("/mnt/nvidea-research", volumes[0].GetProperty("containerPath").GetString());
        Assert.Equal("READ_WRITE", volumes[0].GetProperty("mode").GetString());
    }

    [Fact]
    public async Task CreateAsync_OmitsVolumesWhenNoneConfigured()
    {
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, "{\"resourceId\":\"aijob-no-volume\"}");
        });
        var client = CreateClient(handler);

        await client.CreateAsync(ValidSpec());

        using var json = JsonDocument.Parse(body!);
        Assert.False(json.RootElement.GetProperty("spec").TryGetProperty("volumes", out _));
    }

    [Theory]
    [InlineData("relative/path", "READ_WRITE")]
    [InlineData("/mnt/nvidea", "EXECUTABLE")]
    public async Task CreateAsync_RejectsUnsafeVolumeConfigurationBeforeNetworkCall(string containerPath, string mode)
    {
        var calls = 0;
        var handler = new StubHandler(request =>
        {
            calls++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler);
        var spec = ValidSpec() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount("bucket", containerPath, mode)
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateAsync(spec));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateContainerMountPointsBeforeNetworkCall()
    {
        var calls = 0;
        var handler = new StubHandler(request =>
        {
            calls++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler);
        var spec = ValidSpec() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount("bucket-a", "/mnt/nvidea"),
                new NebiusServerlessVolumeMount("bucket-b", "/mnt/nvidea")
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateAsync(spec));
        Assert.Equal(0, calls);
    }

    private static NebiusServerlessJobSpec ValidSpec() => new(
        Name: "nvidea-research",
        Image: "ghcr.io/example/nvidea-worker:1.0.0",
        ContainerCommand: "dotnet",
        Arguments: "Nvidea.Worker.dll research",
        Platform: "gpu-l40s-a",
        Preset: "1gpu-8vcpu-32gb",
        Timeout: "3600s",
        SubnetId: "vpcsubnet-test",
        Disk: new NebiusServerlessDiskSpec("NETWORK_SSD", 268435456000));

    private static NebiusServerlessJobClient CreateClient(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        new NebiusServerlessOptions("test-access-token", "project-123", MaxRetries: 0));

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _handler(request);
    }
}
