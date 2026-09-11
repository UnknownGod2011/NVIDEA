# Deterministic Personal AI Demo Evaluator

`Nvidea.PersonalAiDemoEval` is a credential-free executable quality gate for the cross-cutting Personal AI behavior that is difficult to demonstrate with isolated unit tests.

It deliberately exercises NVIDEA's real Core product contracts while replacing only external/provider edges with deterministic synthetic adapters. It is therefore useful for regression testing, local demo preparation, and machine-readable judging evidence without consuming Nebius, Tavily, browser, or embedding credits.

## What it proves

A passing run requires all of these invariants to hold together:

1. **Desktop context + durable memory** — `DesktopInvocationService` recalls a personal memory and receives selected-text context.
2. **Clipboard privacy by default** — clipboard content is withheld when the invocation did not opt in to clipboard context, and a sentinel private value must not cross the inference boundary.
3. **Source-grounded research** — the real `ResearchEngine` performs plan → evidence preparation → synthesis against fixture provider data and accepts only machine-verifiable `[src:SOURCE_ID]` citations.
4. **Consequential browser approval** — the real `BrowserSafetyPolicy` classifies a submit action as high risk and the real `BrowserAgentExecutor` requires approval before executing it.
5. **Post-action verification** — a browser side effect counts as successful only after a fresh observation satisfies the expected state.
6. **Restart-safe resumability** — a durable job checkpoint survives reconstruction of `ResumableJobOrchestrator` and resumes from the checkpoint rather than blindly replaying work.
7. **Exact, ephemeral approval** — the consequential resumed job consumes a single-use exact-scope approval and records approval/completion audit events.
8. **Private OS data remains local** — a job that benefits from background execution still resolves to `Local` when it contains private OS data.
9. **Credential-free execution** — all external edges in this evaluator are synthetic and perform zero network calls.

## Run

From the repository root with .NET 8 installed:

```bash
dotnet run --project tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj
```

To persist evidence:

```bash
dotnet run --project tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj -- \
  --output artifacts/personal-ai-demo-eval.json
```

The process exits `0` only when every check passes and exits `1` when any invariant fails or the evaluator encounters an exception.

## Evidence format

The JSON document contains:

- `schemaVersion`
- `generatedAt`
- `overallPassed`
- `checks[]` with stable check IDs, pass/fail state, and non-secret evidence summaries
- `metrics` such as memories used, citation count, browser approval requests, and audit-event count

The output intentionally contains synthetic fixture facts only. It must not include API keys, browser cookies, local file contents, real memory content, provider resource IDs, or raw cloud errors.

## What it does **not** prove

This evaluator is not a substitute for live integration evidence. A green result does **not** prove that:

- Nebius Token Factory credentials/model availability are valid;
- Tavily live search/extract is reachable;
- Playwright/Chromium is installed or authenticated sessions work;
- Nebius Object Storage or Serverless deployment is healthy;
- the Windows WPF shell, global hotkeys, microphone path, or installer compile/run correctly on the target machine.

Those surfaces must remain covered by their existing focused tests, preflight tools, live contract probes, and final Windows/Nebius demo validation.

## Why this belongs in the hackathon build

The final Personal AI demo spans several trust boundaries. A collection of isolated green tests can still miss a product-level regression—for example, memory retrieval works but private clipboard data leaks into inference, or browser execution works but consequential approval is skipped. This evaluator makes those judge-visible guarantees one deterministic contract with machine-readable evidence while keeping live-provider validation separate and honest.
