# Live Nebius / Nemotron planner contract probe

This probe checks the highest-risk external contract in NVIDEA's autonomous browser stack: whether the currently configured Nebius Token Factory Nemotron endpoint accepts the production strict JSON schema used by `NemotronBrowserPlanner` and returns output that the planner can parse and validate.

## What it proves

The command uses the real `NebiusTokenFactoryClient` and the real `NemotronBrowserPlanner`. It submits a synthetic, non-user browser observation and asks the planner to recognize an already-satisfied goal. A passing run proves that:

- the configured Token Factory endpoint is reachable and authenticated;
- the selected Nemotron model accepts the planner's `response_format.type = json_schema` request;
- the strict planner schema is accepted by the backend;
- returned structured output survives NVIDEA's local parser and validation boundary;
- no browser execution is needed to perform the contract check.

It does **not** prove Playwright installation, browser execution, WPF behavior, approvals, or end-to-end goal execution.

## Safety properties

The probe never creates `BrowserHostRuntime`, Playwright, an approval grant, a capability execution request, or a durable browser job. The observation is synthetic and contains no user/browser data. The API key is read only through `NebiusOptions.FromEnvironment()` and is never printed. On Token Factory HTTP failures the response body is intentionally not printed, reducing the chance that echoed request material reaches logs.

## Run

From the repository root:

```bash
NEBIUS_API_KEY="..." dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj
```

On PowerShell:

```powershell
$env:NEBIUS_API_KEY = "..."
dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj
```

Optional existing NVIDEA environment variables remain supported:

- `NVIDEA_NEBIUS_BASE_URL`
- `NVIDEA_MODEL_STANDARD`
- `NVIDEA_MODEL_FAST`
- `NVIDEA_MODEL_DEEP`

The default standard model comes from `NebiusOptions.VerifiedNemotronSuperModel` so the probe follows the same model selection as the application.

## Expected result

A successful run exits with code `0` and prints only sanitized contract diagnostics such as the model identifier, schema acceptance, parser acceptance, and the fact that browser execution was not invoked.

A failure exits non-zero and classifies the failure as configuration/runtime, Token Factory HTTP rejection, or structured-output/parser rejection. Provider response bodies are not emitted by the probe.

## Why this exists

OpenAI-compatible inference endpoints can differ in the exact JSON Schema subset they accept. Source-level unit tests cannot establish that a live model-serving backend accepts the production planner schema. This probe gives maintainers and judges a deterministic, browser-side-effect-free way to verify that dependency before running an autonomous browser demo.
