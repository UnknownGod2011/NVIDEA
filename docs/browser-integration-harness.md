# Real Chromium browser approval + planner harness

This repository includes opt-in integration tests for the trusted browser path:

`tests/Nvidea.Core.Tests/BrowserHostRuntimeIntegrationTests.cs`

The harness is intentionally **credential-free and localhost-only**. It starts a tiny loopback HTTP server and launches the real `BrowserHostRuntime` in headless Chromium.

It now proves two security-critical paths.

## 1. Exact approval boundary

The direct runtime test creates a consequential click with a **typed** postcondition and verifies:

1. the job pauses in `WaitingForApproval`;
2. the local page has not been mutated while approval is pending;
3. the durable child contains the typed postcondition;
4. the persisted job file contains no approval grant/token material;
5. the exact approval scope resumes the action once;
6. Playwright performs the real form submission;
7. a fresh observation verifies `approved mutation complete` through `BrowserPostconditionEvaluator`;
8. the job reaches `Completed` with typed-verification evidence;
9. replaying the same approval fails and cannot cause a second mutation.

## 2. Nemotron planner -> durable goal -> Playwright -> typed verifier

A second deterministic test supplies fixture responses through the real `NemotronBrowserPlanner` without requiring any cloud credential. It verifies:

1. the planner schema contains `postconditions` and no autonomous `expected_state` field;
2. the planned click uses a role/name locator and a typed visible-text postcondition;
3. `BrowserGoalAgent` persists the parent/child linkage before execution;
4. the consequential step pauses before the local server is mutated;
5. the durable child job retains the typed postcondition while persisting no grant/bearer material;
6. exact approval executes the real Chromium form submission once;
7. the fresh post-action observation satisfies the same typed verifier used by production execution;
8. verified-step history is persisted in the durable browser-goal store;
9. a second deterministic Nemotron response marks the goal complete.

The planner fixture is deliberately deterministic: this harness is intended to validate the product contract, not model quality or availability. Live Nebius/Nemotron schema compatibility remains a separate provider-contract check.

## Prerequisites

- .NET 8 SDK
- PowerShell (`pwsh`) for the generated Playwright install script
- network access for the one-time Chromium download

The repository currently pins `Microsoft.Playwright` in `src/Nvidea.Core/Nvidea.Core.csproj`. Playwright browser binaries are version-coupled to the Playwright package, so install Chromium from the generated script after building the test project.

## Windows / PowerShell

```powershell
dotnet build .\tests\Nvidea.Core.Tests\Nvidea.Core.Tests.csproj
pwsh .\tests\Nvidea.Core.Tests\bin\Debug\net8.0\playwright.ps1 install chromium

$env:NVIDEA_RUN_BROWSER_INTEGRATION = "1"
dotnet test .\tests\Nvidea.Core.Tests\Nvidea.Core.Tests.csproj --filter "FullyQualifiedName~BrowserHostRuntimeIntegrationTests"
```

## Linux / macOS

```bash
dotnet build ./tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj
pwsh ./tests/Nvidea.Core.Tests/bin/Debug/net8.0/playwright.ps1 install chromium

NVIDEA_RUN_BROWSER_INTEGRATION=1 \
  dotnet test ./tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj \
  --filter "FullyQualifiedName~BrowserHostRuntimeIntegrationTests"
```

If a Linux machine does not already have Chromium's system dependencies, Playwright also supports installing browser dependencies through its generated CLI. Use this only on a disposable/dev environment where installing OS packages is appropriate.

## Why this is opt-in

Normal `dotnet test` runs must stay fast, credential-free, and free of large browser downloads. The integration tests therefore report themselves skipped unless `NVIDEA_RUN_BROWSER_INTEGRATION=1` is set. This also keeps GitHub Actions/storage usage lean: no workflow is required merely to retain browser binaries or screenshots.

## Failure interpretation

- **Playwright executable/browser missing:** build the tests and install the matching Chromium binary from the generated script.
- **State is not `WaitingForApproval`:** browser or capability policy no longer recognizes the controlled submit action as consequential; treat this as a security regression.
- **Mutation count changes before approval:** critical approval-boundary regression.
- **Durable child does not contain the typed postcondition:** planner/job serialization contract regression.
- **Completion is not observed after approval:** inspect Playwright execution, fresh observation and `BrowserPostconditionEvaluator`.
- **Verified-step detail does not report typed verification:** legacy verification may have re-entered the autonomous path.
- **Planner schema contains `expected_state`:** autonomous planner contract regressed to free-text verification.
- **Replay changes mutation count above one:** critical single-use approval regression.
- **`jobs.json` contains grant/token/bearer material:** critical persistence regression; ephemeral authorization must never be serialized.

The harness never bypasses login, CAPTCHA, browser security, or site safeguards and does not require any Nebius, NVIDIA, Tavily, OpenAI, Gemini, or Claude credential.
