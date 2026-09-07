# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest interaction ideas from keyboard.wtf while making NVIDEA independently stronger in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy and safety.

Target: **Personal AI**. Secondary target: **Best Use of Tavily**. Ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working NVIDEA functionality merely to simplify implementation.

## Target Architecture
- **Desktop shell:** Windows global hotkeys, voice/text invocation, orb/status, active-app/selected-text/clipboard context, local speech where useful, permission UX and emergency stop.
- **Agent core:** Nemotron through Nebius Token Factory, structured tools/output, bounded execution, verification, retries/cancellation and approvals.
- **Memory:** typed working/episodic/semantic/project/skill memory with privacy-aware writes, hybrid retrieval, provenance/confidence/sensitivity/retention and deletion controls.
- **Research:** Nemotron planning -> Tavily evidence -> untrusted-content boundary -> Nemotron synthesis -> validated citations.
- **Browser:** DOM/accessibility observation -> Nemotron one-step plan -> typed action + typed postconditions -> hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> deterministic verification -> repeat under strict budgets.
- **Skills / permissions:** capability registry, least privilege, monotonic risk, single-use approvals and append-only audit.
- **Jobs:** durable checkpoints, retries, cancellation, approval-paused states, ephemeral grants and local-vs-Nebius execution policy.
- **Cloud:** Nebius Serverless only for suitable long-running/background workloads; private OS actions stay local.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- .NET 8 core at `src/Nvidea.Core` plus WPF host at `src/Nvidea.Windows`.
- Nebius Token Factory inference client with Nemotron default, structured output/tools, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright .NET browser driver, hard safety policy, capability execution boundary and deterministic verifier under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and append-only audit under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns fresh untrusted observations into validated one-step decisions using typed postconditions.
- `BrowserGoalAgent` runs a bounded observe -> plan -> durable child -> verify loop, halts at approval boundaries, and independently rejects legacy/unverifiable autonomous action contracts before child reservation.
- Browser goal sessions persist separately from approval state with privacy-minimized verified history and action/planner/context/wall-clock budgets.
- Parent/child browser orchestration persists a reserved child ID before creation/execution and reconciles that exact child after restart.
- Durable `Running` child jobs are never blindly replayed. Fresh deterministic evidence may reconcile them; otherwise human resolution is required.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only selected-text capture, opt-in clipboard disclosure, confirmation UX, live status, emergency stop and interrupted-work evidence inspection.
- Typed browser postconditions support exact URL, title/text presence, element existence/value, checked state and enabled state; normal execution and crash reconciliation share the evaluator.
- Durable browser-action checkpoints carry verification contract v2. Safe legacy records are migrated; ambiguous legacy mutations are quarantined without execution.
- `BrowserActionJobHandler` accepts only current typed-verification checkpoints.
- Opt-in localhost Chromium integration harness covers approval boundaries and deterministic Nemotron planner -> durable child -> Playwright -> typed verifier behavior.
- Live Nebius strict-schema contract probe now exists under `tools/Nvidea.NebiusContractProbe`.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with structured tools/output, retries, timeout/cancellation, endpoint validation and conservative model routing.
- Added typed layered memory with provenance/confidence/importance/sensitivity/retention, secret detection, privacy-aware writes and hybrid retrieval.
- Added Tavily research planning/search/deduplication/provenance plus Nemotron synthesis with explicit untrusted-web boundaries and validated source IDs.
- Added provider-neutral browser contracts, hard-safety policy, bounded observe-act-observe-verify execution and receipts.
- Added capability registry, least privilege, exact single-use approvals, append-only audit, durable jobs and Nebius Serverless execution contracts.

### 2026-09-07 — Concrete browser, desktop and multi-step agent
- Added Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added ephemeral approval handoff, last-mile browser capability enforcement, durable browser child jobs and `BrowserHostRuntime`.
- Added trusted desktop composition root, WPF global hotkey/context capture/clipboard disclosure, cancellation and emergency stop.
- Added `NemotronBrowserPlanner`, bounded `BrowserGoalAgent`, privacy-minimized durable goal sessions and action/planner/context/wall-clock budgets.

### 2026-09-07 — Crash consistency and evidence-only recovery
- Added caller-supplied child IDs, idempotent creation and structural permission comparison.
- Split child creation from execution so the parent persists exact child identity before any browser side effect.
- Recovery handles missing/created/approval-paused/completed/lost-grant/ambiguous-running child states without blind replay.
- Added deterministic no-model ambiguous-side-effect reconciliation; uploads/downloads are never auto-reconciled from DOM evidence.
- Added Windows **Inspect evidence** UX with no retry-anyway affordance.

### 2026-09-07 — Typed browser verification
- Added typed postconditions capped at eight predicates and shared deterministic evaluation.
- Replaced autonomous planner `expected_state` with bounded `postconditions[]` and local shape validation.
- Added Chromium contract harness covering deterministic planner -> durable child -> approval -> Playwright -> fresh observation -> verifier -> durable history.
- Added conservative legacy-action migration; arbitrary free-text mutations require human review.
- Added an independent `BrowserGoalAgent` guard rejecting legacy or unverifiable autonomous actions before durable child reservation.
- Added verification contract v2 to durable browser checkpoints; safe unversioned records migrate under the job-store lock and ambiguous legacy mutations are quarantined with executable payload removed.

### 2026-09-07 — Live Nebius strict-schema probe
Completed:
- Added `tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj` as a minimal .NET 8 executable that references the production core rather than duplicating API/schema logic.
- Added `Program.cs` that constructs the real `NebiusTokenFactoryClient` and real `NemotronBrowserPlanner`, feeds a synthetic non-user browser observation, and asks the planner to return `Complete` through the production strict `json_schema` response contract.
- The probe never creates Playwright, `BrowserHostRuntime`, a browser job, a capability request, or an approval grant. It cannot execute browser actions.
- The probe exits non-zero when Token Factory rejects the request/schema or local structured-output parsing/validation fails. On provider HTTP failure it prints status only and intentionally suppresses the response body.
- Added `docs/nebius-contract-probe.md` with one-command usage, scope, safety properties, expected output and explicit non-guarantees.
- Fresh official Nebius material checked on 2026-09-07 still describes Token Factory as OpenAI-compatible with native structured JSON output/function calling and lists Nemotron 3 Super 120B as an agentic reasoning model.

Validation / evidence:
- Repository head before this run was `2b56dad27d350eb0c9c482f86ad9e4f42997af01`.
- New probe files were persisted through GitHub commits `b390d9ab9f1d7246ed45103215929894791bc74a`, `38b6acd099553d009f0bf46f6d2efe45a8f29a30`, and `7d17c04021daa0d843b21048bb870c772f589844` before this progress update.
- Source review confirms the probe reuses the application's environment/options, HTTPS endpoint validation, model routing, retry/timeout behavior and exact planner schema.
- This execution environment still does not expose `dotnet`, `msbuild`, or `csc`, so **no compile, live Token Factory, unit-test or Chromium success is claimed**.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- No credentials are committed. `NEBIUS_API_KEY` is consumed by the existing environment loader and never printed by the probe.
- The live probe transmits only a fixed synthetic goal/observation, not clipboard, page, memory, user or browser-session data.
- Provider HTTP response bodies are suppressed in probe output to avoid persisting echoed request/provider details in CI/demo logs.
- Browser execution, approval, capability, audit mutation and Playwright are outside the probe's object graph.
- Existing prompt-injection boundaries, exact single-use approvals, safety floor and capability enforcement are unchanged.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- The new strict-schema probe itself has not been compiled or run against live Token Factory in this environment because .NET and a Nebius API key are unavailable here.
- Matching Playwright Chromium binaries have not been installed/launched here, and WPF has not been compiled/launched on Windows here.
- `JsonAgentJobStore` now owns browser checkpoint schema migration; if job types grow substantially, migrate toward a generic registered job-schema migration pipeline.
- Recovery discovery still depends on goal-state reconciliation when a process dies before the parent classifies an ambiguous child.
- Goal sessions and child jobs are separate atomic files rather than one cross-file transaction; reserved-child-ID ordering remains the safety mechanism.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration signal** and run `tools/Nvidea.NebiusContractProbe` against a configured Nebius Token Factory key; fix any compiler, strict-schema or model-output incompatibility immediately. If that runtime/key remains unavailable, implement **encrypted local state at rest with a Windows DPAPI-backed key boundary and migration tests**, starting with the highest-sensitivity durable stores (memory and browser/job state) without making cloud execution depend on local secrets.
