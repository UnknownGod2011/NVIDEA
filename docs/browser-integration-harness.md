# Real Chromium browser approval harness

This repository includes an opt-in integration test for the trusted browser path:

`tests/Nvidea.Core.Tests/BrowserHostRuntimeIntegrationTests.cs`

The harness is intentionally **credential-free and localhost-only**. It starts a tiny loopback HTTP server, launches the real `BrowserHostRuntime` in headless Chromium, creates a consequential browser click, and verifies the security contract end to end:

1. the job pauses in `WaitingForApproval`;
2. the local page has not been mutated while approval is pending;
3. the persisted job file contains no approval grant/token material;
4. the exact approval scope resumes the action once;
5. Playwright performs the real form submission;
6. a fresh observation verifies `approved mutation complete`;
7. the job reaches `Completed`;
8. replaying the same approval fails and cannot cause a second mutation.

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

Normal `dotnet test` runs must stay fast, credential-free, and free of large browser downloads. The integration test therefore reports itself skipped unless `NVIDEA_RUN_BROWSER_INTEGRATION=1` is set. This also keeps GitHub Actions/storage usage lean: no workflow is required merely to retain browser binaries or screenshots.

## Failure interpretation

- **Playwright executable/browser missing:** build the tests and install the matching Chromium binary from the generated script.
- **State is not `WaitingForApproval`:** browser or capability policy no longer recognizes the test action as consequential; treat this as a security regression.
- **Mutation count changes before approval:** critical approval-boundary regression.
- **Completion is not observed after approval:** inspect Playwright action execution and post-action observation/verification.
- **Replay changes mutation count above one:** critical single-use approval regression.
- **`jobs.json` contains grant/token material:** critical persistence regression; ephemeral authorization must never be serialized.

The harness never bypasses login, CAPTCHA, browser security, or site safeguards and does not require any Nebius, NVIDIA, Tavily, OpenAI, Gemini, or Claude credential.
