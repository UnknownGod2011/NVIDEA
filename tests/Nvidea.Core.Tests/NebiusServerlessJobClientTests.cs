using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusServerlessJobClientTests
{
    [Fact]
    public async Task CreateAsync_UsesCurrentRequiredPayloadShape()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            captured = CloneRequestMetadata(request);
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, "{\"resourceId\":\"aijob-123\"}");
        });
        var client = CreateClient(handler);
        var spec = ValidSpec(
            environmentVariables: new Dictionary<string, string> { ["TASK_KIND"] = "research" });

        var response = await client.CreateAsync(spec);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("aijob-123", response.TryGetResourceId());
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://api.nebius.cloud/ai/v1/jobs", captured.RequestUri!.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
        Assert.Equal("test-access-token", captured.Headers.Authorization.Parameter);

        using var json = JsonDocument.Parse(body!);
        Assert.Equal("project-123", json.RootElement.GetProperty("metadata").GetProperty("parentId").GetString());
        Assert.Equal("nvidea-research", json.RootElement.GetProperty("metadata").GetProperty("name").GetString());
        var payloadSpec = json.RootElement.GetProperty("spec");
        Assert.Equal(spec.Image, payloadSpec.GetProperty("image").GetString());
        Assert.Equal(spec.ContainerCommand, payloadSpec.GetProperty("containerCommand").GetString());
        Assert.Equal(spec.Arguments, payloadSpec.GetProperty("args").GetString());
        Assert.Equal(spec.Platform, payloadSpec.GetProperty("platform").GetString());
        Assert.Equal(spec.Preset, payloadSpec.GetProperty("preset").GetString());
        Assert.Equal(spec.Timeout, payloadSpec.GetProperty("timeout").GetString());
        Assert.Equal(spec.SubnetId, payloadSpec.GetProperty("subnetId").GetString());
        Assert.Equal("NETWORK_SSD", payloadSpec.GetProperty("disk").GetProperty("type").GetString());
        Assert.Equal(268435456000, payloadSpec.GetProperty("disk").GetProperty("sizeBytes").GetInt64());
    }

    [Fact]
    public async Task GetAsync_UsesDocumentedResourceEndpointAndReadsStateWithoutGuessing()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = CloneRequestMetadata(request);
            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                "{\"metadata\":{\"id\":\"aijob-123\"},\"status\":{\"state\":\"RUNNING\"}}"));
        });
        var client = CreateClient(handler);

        var response = await client.GetAsync("aijob-123");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://api.nebius.cloud/ai/v1/jobs/aijob-123", captured.RequestUri!.ToString());
        Assert.Equal("RUNNING", response.TryGetJobState());
        Assert.Null(response.TryGetResourceId());
    }

    [Fact]
    public async Task ListAsync_ScopesRequestToConfiguredProject()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = CloneRequestMetadata(request);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"items\":[]}"));
        });
        var client = CreateClient(handler);

        await client.ListAsync();

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://api.nebius.cloud/ai/v1/jobs?parentId=project-123", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task CancelAsync_UsesCancelControlPlaneAndExactId()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            captured = CloneRequestMetadata(request);
            body = await request.Content!.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, "{}");
        });
        var client = CreateClient(handler);

        await client.CancelAsync("aijob-abc");

        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://api.nebius.cloud/ai/v1/jobs/cancel", captured.RequestUri!.ToString());
        using var json = JsonDocument.Parse(body!);
        Assert.Equal("aijob-abc", json.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task CreateAsync_SerializesMysteryBoxReferencesWithoutPlaintextSecretValue()
    {
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, "{\"resourceId\":\"aijob-secret\"}");
        });
        var client = CreateClient(handler);
        var spec = ValidSpec(
            secretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>
            {
                ["TAVILY_API_KEY"] = new(VersionId: "mbsecver-tavily"),
                ["NEBIUS_TOKEN"] = new(SecretId: "mbsec-nebius")
            });

        await client.CreateAsync(spec);

        using var json = JsonDocument.Parse(body!);
        var environment = json.RootElement.GetProperty("spec").GetProperty("environmentVariables");
        Assert.Equal(2, environment.GetArrayLength());
        Assert.Equal("TAVILY_API_KEY", environment[0].GetProperty("name").GetString());
        Assert.False(environment[0].TryGetProperty("value", out _));
        Assert.Equal("mbsecver-tavily", environment[0].GetProperty("mysteryboxSecret").GetProperty("versionId").GetString());
        Assert.Equal("NEBIUS_TOKEN", environment[1].GetProperty("name").GetString());
        Assert.Equal("mbsec-nebius", environment[1].GetProperty("mysteryboxSecret").GetProperty("secretId").GetString());
        Assert.DoesNotContain("must-not-be-sent", body!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_RejectsPlaintextSecretLikeEnvironmentVariablesBeforeNetworkCall()
    {
        var calls = 0;
        var handler = new StubHandler(request =>
        {
            calls++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler);
        var spec = ValidSpec(
            environmentVariables: new Dictionary<string, string> { ["TAVILY_API_KEY"] = "must-not-be-sent" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateAsync(spec));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingSubnetOrDiskBeforeNetworkCall()
    {
        var calls = 0;
        var handler = new StubHandler(request =>
        {
            calls++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateAsync(ValidSpec() with { SubnetId = null }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateAsync(ValidSpec() with { Disk = null }));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicatePlaintextAndSecretEnvironmentVariable()
    {
        var calls = 0;
        var handler = new StubHandler(request =>
        {
            calls++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler);
        var spec = ValidSpec(
            environmentVariables: new Dictionary<string, string> { ["MODE"] = "safe" },
            secretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>
            {
                ["MODE"] = new(SecretId: "mbsec-mode")
            });

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateAsync(spec));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Client_RetriesServerErrorsButNotPermanentClientErrors()
    {
        var serverCalls = 0;
        var retryingHandler = new StubHandler(request =>
        {
            serverCalls++;
            return Task.FromResult(serverCalls == 1
                ? JsonResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"temporary\"}")
                : JsonResponse(HttpStatusCode.OK, "{\"items\":[]}"));
        });
        var retryingClient = CreateClient(retryingHandler, maxRetries: 1);

        await retryingClient.ListAsync();
        Assert.Equal(2, serverCalls);

        var badRequestCalls = 0;
        var permanentHandler = new StubHandler(request =>
        {
            badRequestCalls++;
            return Task.FromResult(JsonResponse(HttpStatusCode.BadRequest, "{\"error\":\"bad request\"}"));
        });
        var permanentClient = CreateClient(permanentHandler, maxRetries: 3);

        await Assert.ThrowsAsync<HttpRequestException>(() => permanentClient.ListAsync());
        Assert.Equal(1, badRequestCalls);
    }

    [Fact]
    public void Client_RejectsNonNebiusRemoteEndpoint()
    {
        var handler = new StubHandler(request => Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}")));
        using var http = new HttpClient(handler);

        Assert.Throws<ArgumentException>(() => new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions(
                "token",
                "project",
                new Uri("https://attacker.example/"))));
    }

    private static NebiusServerlessJobSpec ValidSpec(
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        IReadOnlyDictionary<string, NebiusMysteryBoxSecretRef>? secretEnvironmentVariables = null)
    {
        return new NebiusServerlessJobSpec(
            "nvidea-research",
            "ghcr.io/example/nvidea-worker:1.0.0",
            "dotnet",
            "Nvidea.Worker.dll research",
            "gpu-l40s-a",
            "1gpu-8vcpu-32gb",
            "3600s",
            "vpcsubnet-test",
            environmentVariables,
            new NebiusServerlessDiskSpec("NETWORK_SSD", 268435456000),
            secretEnvironmentVariables);
    }

    private static NebiusServerlessJobClient CreateClient(HttpMessageHandler handler, int maxRetries = 0)
    {
        var http = new HttpClient(handler);
        return new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions(
                "test-access-token",
                "project-123",
                MaxRetries: maxRetries));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static HttpRequestMessage CloneRequestMetadata(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Headers.Authorization is AuthenticationHeaderValue auth)
            clone.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, auth.Parameter);
        return clone;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _handler(request);
    }
}
