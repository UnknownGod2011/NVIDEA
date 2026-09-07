using Nvidea.Core.Browser;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class NemotronBrowserPlannerPostconditionTests
{
    [Fact]
    public async Task Planner_maps_typed_postconditions_and_drops_legacy_expected_state()
    {
        var inference = new StubInferenceClient("""
            {
              "decision": "act",
              "reason": "Submit and verify success.",
              "action": {
                "kind": "click",
                "locator_kind": "accessibility_ref",
                "locator_value": "e-submit",
                "locator_name": null,
                "locator_role": null,
                "value": null,
                "destination": null,
                "postconditions": [
                  {
                    "kind": "visible_text_contains",
                    "expected": "Saved successfully",
                    "locator_kind": null,
                    "locator_value": null,
                    "locator_name": null,
                    "locator_role": null,
                    "expected_boolean": null
                  },
                  {
                    "kind": "element_enabled_equals",
                    "expected": null,
                    "locator_kind": "role_and_name",
                    "locator_value": "Continue",
                    "locator_name": "Continue",
                    "locator_role": "button",
                    "expected_boolean": true
                  }
                ],
                "rationale": "User requested submission."
              }
            }
            """);

        var planner = new NemotronBrowserPlanner(inference);
        var decision = await planner.PlanNextAsync("Submit this form", Observation());

        Assert.Equal(BrowserPlannerDecisionKind.Act, decision.Kind);
        Assert.NotNull(decision.Action);
        Assert.Null(decision.Action!.ExpectedState);
        Assert.Equal(2, decision.Action.Postconditions!.Count);
        Assert.Equal(BrowserPostconditionKind.VisibleTextContains, decision.Action.Postconditions[0].Kind);
        Assert.Equal(BrowserPostconditionKind.ElementEnabledEquals, decision.Action.Postconditions[1].Kind);
        Assert.True(decision.Action.Postconditions[1].ExpectedBoolean);
    }

    [Fact]
    public async Task Planner_rejects_mutating_action_without_postcondition()
    {
        var planner = new NemotronBrowserPlanner(new StubInferenceClient(ActionJson("click", "[]")));

        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            planner.PlanNextAsync("Click submit", Observation()));

        Assert.Contains("typed postcondition", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Planner_rejects_css_postcondition_locator_that_runtime_cannot_verify()
    {
        var postconditions = """
            [{
              "kind": "element_exists",
              "expected": null,
              "locator_kind": "css",
              "locator_value": "#success",
              "locator_name": null,
              "locator_role": null,
              "expected_boolean": null
            }]
            """;
        var planner = new NemotronBrowserPlanner(new StubInferenceClient(ActionJson("click", postconditions)));

        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            planner.PlanNextAsync("Click submit", Observation()));

        Assert.Contains("not supported", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Planner_rejects_invented_accessibility_ref_in_postcondition()
    {
        var postconditions = """
            [{
              "kind": "element_value_equals",
              "expected": "done",
              "locator_kind": "accessibility_ref",
              "locator_value": "e-invented",
              "locator_name": null,
              "locator_role": null,
              "expected_boolean": null
            }]
            """;
        var planner = new NemotronBrowserPlanner(new StubInferenceClient(ActionJson("click", postconditions)));

        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            planner.PlanNextAsync("Click submit", Observation()));

        Assert.Contains("absent from the fresh observation", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Planner_rejects_malformed_boolean_postcondition()
    {
        var postconditions = """
            [{
              "kind": "element_checked_equals",
              "expected": null,
              "locator_kind": "role_and_name",
              "locator_value": "Remember me",
              "locator_name": "Remember me",
              "locator_role": "checkbox",
              "expected_boolean": null
            }]
            """;
        var planner = new NemotronBrowserPlanner(new StubInferenceClient(ActionJson("click", postconditions)));

        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            planner.PlanNextAsync("Click submit", Observation()));

        Assert.Contains("expected_boolean", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Planner_requires_exact_http_destination_for_url_postcondition()
    {
        var postconditions = """
            [{
              "kind": "url_equals",
              "expected": "javascript:alert(1)",
              "locator_kind": null,
              "locator_value": null,
              "locator_name": null,
              "locator_role": null,
              "expected_boolean": null
            }]
            """;
        var planner = new NemotronBrowserPlanner(new StubInferenceClient(NavigateJson(postconditions)));

        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            planner.PlanNextAsync("Open the result", Observation()));

        Assert.Contains("HTTP(S)", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static BrowserObservation Observation() => new(
        new Uri("https://example.test/form"),
        "Example form",
        new[]
        {
            new BrowserElement("e-submit", "button", "Submit", null, true, true, false),
            new BrowserElement("e-check", "checkbox", "Remember me", null, true, true, false)
        },
        "Example form Submit Remember me",
        DateTimeOffset.UtcNow);

    private static string ActionJson(string kind, string postconditions) => $$"""
        {
          "decision": "act",
          "reason": "next step",
          "action": {
            "kind": "{{kind}}",
            "locator_kind": "accessibility_ref",
            "locator_value": "e-submit",
            "locator_name": null,
            "locator_role": null,
            "value": null,
            "destination": null,
            "postconditions": {{postconditions}},
            "rationale": "test"
          }
        }
        """;

    private static string NavigateJson(string postconditions) => $$"""
        {
          "decision": "act",
          "reason": "navigate",
          "action": {
            "kind": "navigate",
            "locator_kind": null,
            "locator_value": null,
            "locator_name": null,
            "locator_role": null,
            "value": null,
            "destination": "https://example.test/result",
            "postconditions": {{postconditions}},
            "rationale": "test"
          }
        }
        """;

    private sealed class StubInferenceClient(string content) : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Contains("postconditions", request.ResponseJsonSchema ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("expected_state", request.ResponseJsonSchema ?? string.Empty, StringComparison.Ordinal);
            return Task.FromResult(new AgentCompletion(content, Array.Empty<ToolCall>(), "nemotron-test", "stop"));
        }
    }
}
