using Nvidea.Core.Browser;
using Nvidea.Core.Nebius;

static int Fail(string category, string detail)
{
    Console.Error.WriteLine($"NVIDEA Nebius contract probe: FAIL [{category}] {detail}");
    return 1;
}

try
{
    var options = NebiusOptions.FromEnvironment();
    options.Validate();

    using var httpClient = new HttpClient();
    var inference = new NebiusTokenFactoryClient(httpClient, options);
    var planner = new NemotronBrowserPlanner(inference);

    var observation = new BrowserObservation(
        new Uri("https://example.invalid/nvidea-contract-probe"),
        "NVIDEA contract probe",
        new[]
        {
            new BrowserElement(
                "e-1",
                "heading",
                "NVIDEA probe ready",
                null,
                IsVisible: true,
                IsEnabled: true,
                IsEditable: false)
        },
        "NVIDEA probe ready. This synthetic page contains no user data and no executable instructions.",
        DateTimeOffset.UtcNow,
        ContainsUntrustedInstructions: false,
        SnapshotId: "contract-probe");

    var decision = await planner.PlanNextAsync(
        "Inspect the synthetic page. If it states that the NVIDEA probe is ready, declare the goal complete. Do not propose a browser action.",
        observation);

    if (decision.Kind != BrowserPlannerDecisionKind.Complete)
        return Fail("planner-contract", $"Expected decision=Complete but received {decision.Kind}.");
    if (decision.Action is not null)
        return Fail("planner-contract", "Completion unexpectedly contained a browser action.");
    if (string.IsNullOrWhiteSpace(decision.Model))
        return Fail("planner-contract", "Backend response did not identify a model.");

    Console.WriteLine("NVIDEA Nebius contract probe: PASS");
    Console.WriteLine($"Model: {decision.Model}");
    Console.WriteLine("Structured planner schema: accepted");
    Console.WriteLine("Planner parse/validation: accepted");
    Console.WriteLine("Browser execution: not invoked");
    Console.WriteLine("Sensitive diagnostics: not persisted");
    return 0;
}
catch (NebiusApiException ex)
{
    return Fail("token-factory", $"HTTP {(int)ex.StatusCode} ({ex.StatusCode}); response body intentionally not printed.");
}
catch (InvalidDataException ex)
{
    return Fail("structured-output", ex.Message.ReplaceLineEndings(" "));
}
catch (OperationCanceledException)
{
    return Fail("cancelled", "Request was cancelled or timed out.");
}
catch (Exception ex)
{
    return Fail("runtime", $"{ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}");
}
