# Real Chromium browser integration harness

This repository includes opt-in integration tests for the trusted browser path:

- `tests/Nvidea.Core.Tests/BrowserHostRuntimeIntegrationTests.cs`
- `tests/Nvidea.Core.Tests/PersistentBrowserSessionIntegrationTests.cs`

The harness is intentionally **credential-free and localhost-only** except for the controlled blocked-popup URL used to prove host-boundary rejection. It launches the real `BrowserHostRuntime` in headless Chromium.

## 1. Exact approval boundary

The direct runtime test creates a consequential click with a typed postcondition and verifies that the mutation cannot happen before exact one-time approval, executes once after approval, and is verified from a fresh browser observation.

## 2. Nemotron planner -> durable goal -> Playwright -> typed verifier

A deterministic Nemotron fixture runs through the production planner contract, durable browser child job, approval pause, Playwright action and typed postcondition evaluator. It proves that autonomous browser planning uses `postconditions` rather than legacy free-text `expected_state`.

## 3. Persistent authenticated browser state

`PersistentBrowserSessionIntegrationTests.CookieState_SurvivesRuntimeRestart_WithoutRestoringOldTabs` proves the intended profile lifecycle:

1. NVIDEA launches a dedicated persistent Chromium context under the owned `browser-profile` directory.
2. A localhost response writes a harmless test cookie.
3. The runtime is disposed, closing the persistent context/browser through the Playwright-supported context boundary.
4. A second runtime uses the same NVIDEA-owned profile.
5. The localhost server observes the persisted cookie and renders `session-restored`.
6. The runtime exposes only one fresh start page after restart; stale restored tabs are not adopted as current agent context.

The test deliberately proves browser-managed session persistence without storing or printing the cookie through NVIDEA diagnostics.

## 4. Popup/new-tab host boundary

`PersistentBrowserSessionIntegrationTests.AllowedPopup_BecomesActive_AndCrossBoundaryPopup_IsNeverAdopted` proves that:

1. a same-host popup created by an approved browser click becomes the active page only after its URL is classified as permitted;
2. verification attaches to that permitted popup;
3. a cross-host popup is never adopted as active agent context;
4. the session snapshot contains only permitted pages after boundary reconciliation.

`PlaywrightBrowserSessionDriver` consumes `BrowserContext.Page` events instead of assuming undocumented ordering of `BrowserContext.Pages`. A newly-created `about:blank` page gets a short bounded classification window after a click so permitted popup navigation can commit before deterministic verification begins.

## Prerequisites

- .NET 8 SDK
- PowerShell (`pwsh`) for the generated Playwright install script
- network access for the one-time Chromium download

The repository pins `Microsoft.Playwright` in `src/Nvidea.Core/Nvidea.Core.csproj`. Playwright browser binaries are version-coupled to the package, so install Chromium from the generated script after building the tests.

## Windows / PowerShell

```powershell
dotnet build .\tests\Nvidea.Core.Tests\Nvidea.Core.Tests.csproj
pwsh .\tests\Nvidea.Core.Tests\bin\Debug\net8.0\playwright.ps1 install chromium

$env:NVIDEA_RUN_BROWSER_INTEGRATION = "1"
dotnet test .\tests\Nvidea.Core.Tests\Nvidea.Core.Tests.csproj --filter "FullyQualifiedName~BrowserHostRuntimeIntegrationTests|FullyQualifiedName~PersistentBrowserSessionIntegrationTests"
```

## Linux / macOS

```bash
dotnet build ./tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj
pwsh ./tests/Nvidea.Core.Tests/bin/Debug/net8.0/playwright.ps1 install chromium

NVIDEA_RUN_BROWSER_INTEGRATION=1 \
  dotnet test ./tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj \
  --filter "FullyQualifiedName~BrowserHostRuntimeIntegrationTests|FullyQualifiedName~PersistentBrowserSessionIntegrationTests"
```

If a Linux machine does not already have Chromium system dependencies, Playwright also supports installing browser dependencies through its generated CLI. Use this only on a disposable/dev environment where installing OS packages is appropriate.

## Why this is opt-in

Normal `dotnet test` runs must stay fast, credential-free, and free of large browser downloads. The real-browser tests therefore skip unless `NVIDEA_RUN_BROWSER_INTEGRATION=1` is set. This also avoids artifact-heavy CI and unnecessary GitHub Actions storage.

## Failure interpretation

- **Playwright executable/browser missing:** build the tests and install the matching Chromium binary.
- **Mutation changes before approval:** critical approval-boundary regression.
- **Planner schema contains `expected_state`:** autonomous planner contract regressed to legacy free-text verification.
- **Restart shows `session-missing`:** persistent browser profile/state wiring is broken.
- **Restart adopts stale tabs:** profile persistence is leaking historical browsing context into a fresh agent runtime.
- **Allowed popup never becomes active:** popup event/navigation classification or active-page adoption is broken.
- **A disallowed host appears as the active page:** critical browser-boundary regression.
- **Replay causes a second mutation:** critical single-use approval regression.

The harness never bypasses login, CAPTCHA, browser security, or site safeguards and does not require Nebius, NVIDIA, Tavily, OpenAI, Gemini, or Claude credentials.
