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
- **Local security:** high-sensitivity durable Windows state protected with a CurrentUser DPAPI boundary, versioned envelopes, purpose binding, fail-closed decryption and plaintext migration.

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
- Live Nebius strict-schema contract probe exists under `tools/Nvidea.NebiusContractProbe`.
- **New:** memory, durable jobs and browser-goal sessions use a versioned local-state protection envelope; on Windows the default protector is CurrentUser DPAPI. Legacy plaintext is re-written into the protected format after successful parsing while holding the store lock.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with structured tools/output, retries, timeout/cancellation, endpoint validation and conservative model routing.
- Added typed layered memory with provenance/confidence/importance/sensitivity/retention, secret detection, privacy-aware writes and hybrid retrieval.
- Added Tavily research planning/search/deduplication/provenance plus Nemotron synthesis with explicit untrusted-web boundaries and validated source IDs.
- Added provider-neutral browser contracts, hard-safety policy, bounded observe-act-observe-verify execution and receipts.
- Added capability registry, least privilege, exact single-use approvals, append-only audit, durable jobs and Nebius Serverless execution contracts.

### 2026-09-07 — Browser, desktop and crash-safe agent
- Added Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added ephemeral approval handoff, last-mile browser capability enforcement, durable child jobs, trusted desktop composition root and WPF shell.
- Added `NemotronBrowserPlanner`, bounded `BrowserGoalAgent`, privacy-minimized durable goal sessions and strict execution budgets.
- Split child creation from advancement; persisted exact child IDs before side effects; added restart reconciliation without blind replay.
- Added deterministic evidence-only reconciliation for ambiguous `Running` children; uploads/downloads remain non-auto-reconcilable.
- Added Windows **Inspect evidence** recovery UX with no retry-anyway affordance.

### 2026-09-07 — Typed browser verification and durable migration
- Added bounded typed postconditions and one deterministic evaluator shared by normal execution and crash reconciliation.
- Replaced autonomous planner `expected_state` with typed `postconditions[]` and local shape validation.
- Added Chromium contract harness for planner -> durable child -> approval -> Playwright -> observation -> verifier -> durable history.
- Added conservative legacy-action migration and an independent `BrowserGoalAgent` guard rejecting legacy/unverifiable autonomous writes before child reservation.
- Added verification contract v2 to durable browser checkpoints; safe records migrate under the job-store lock and ambiguous legacy mutations are quarantined with executable payload removed.

### 2026-09-07 — Live Nebius strict-schema probe
- Added `tools/Nvidea.NebiusContractProbe`, reusing the production `NebiusTokenFactoryClient` and `NemotronBrowserPlanner` against a fixed synthetic observation.
- Probe cannot create Playwright, browser actions, capability requests or approval grants; provider error bodies are suppressed.
- Added `docs/nebius-contract-probe.md` with execution procedure and non-guarantees.

### 2026-09-07 — DPAPI-backed encrypted local state
Completed:
- Added `src/Nvidea.Core/Security/LocalStateProtection.cs` with `ILocalStateProtector`, a versioned `NVIDEA-STATE-V1` envelope and `WindowsDpapiLocalStateProtector`.
- Windows DPAPI is invoked directly through `CryptProtectData` / `CryptUnprotectData` with `CRYPTPROTECT_UI_FORBIDDEN`, default CurrentUser semantics and purpose-derived optional entropy. No reusable encryption key is committed or stored beside application state.
- Corrected native-memory ownership during review: DPAPI-produced data/output-description buffers are released with `LocalFree`; only application-allocated input/entropy buffers use `Marshal.FreeHGlobal`.
- `JsonFileMemoryStore`, `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` now protect persisted bytes on Windows by default. Existing callers do not need a second configuration path.
- Each store has an independent purpose (`personal-memory-v1`, `agent-jobs-v1`, `browser-goal-sessions-v1`) so protected payloads cannot be silently transplanted across store types.
- Legacy plaintext remains readable for migration. A store first parses the plaintext under its existing exclusive gate and only after successful parsing rewrites it atomically into the protected envelope. Invalid legacy data is not encrypted and legitimized.
- Stores fail closed when they encounter a protected envelope without an available protector or when protected data cannot be decrypted.
- Added `LocalStateProtectionTests` covering envelope round-trip/purpose binding, memory content not appearing in persisted text, protected memory round-trip, plaintext migration for memory/jobs/browser-goal stores, and non-Windows DPAPI fail-closed behavior.

Validation / evidence:
- Repository head before this run: `1f76d6ebc798c64d5843dd85c520bb930195699b`.
- Implementation commits before this progress update: `4fd59c5ffcaa5321c129823d3d986ca3a24743de`, `dbd4a000d8dbf165ec6a59a234259ce7bc06d871`, `b5506f0673cc2f5b3fb454a702ac7e092611931f`, `2ef3789211fefa572ad66d54d22668644dd842a6`, `7a729903fdb7fa4e0561cf9ed8c143e5728fa75b`, `ca23c98c9c5936da7610efc2d43311e95008fea3`, `9e0b8cebc667191ce5f27eea4ac9b9be7649c973`, `a93ac7490a251cd2a523aa6164155513c552e702`, `3f8064936e7b1e5a0144f1462aeba59d628f3bae`.
- Current Microsoft DPAPI documentation was checked on 2026-09-07. It confirms default same-user/same-machine semantics, matching optional entropy on unprotect, and `LocalFree` ownership for DPAPI output buffers.
- The execution container still exposes no `dotnet`, `msbuild` or `csc`; the toolchain probe returned no executable. Therefore **no compile or unit-test success is claimed**.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- State encryption is local-only and does not make Nebius/cloud jobs depend on a local secret/key file.
- DPAPI protection is deliberately scoped to the logged-in Windows user rather than `LOCAL_MACHINE` so another local account is not granted decrypt authority.
- Purpose binding is additional entropy/domain separation, not a secret and not advertised as one.
- Plaintext migration occurs only after successful deserialization and under each store's existing lock; writes retain temp-file replacement behavior.
- The envelope is versioned so future formats can be distinguished explicitly.
- No approval grants, provider API keys, browser credentials or tokens were added to durable state.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The live strict-schema probe still has not been compiled or run against Token Factory here because .NET and a Nebius API key are unavailable.
- DPAPI P/Invoke and protected-store tests have not executed on a real Windows runner in this environment; the implementation follows current Microsoft ownership/scope documentation but still needs executable verification.
- `audit.jsonl` remains plaintext at rest. It is append-only and privacy-minimized but should receive a separate encrypted/authenticated append format or protected segment design rather than being forced into the whole-file store abstraction.
- Browser-profile ownership/authenticated persistent sessions, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration + Windows DPAPI round-trip signal**, and run `tools/Nvidea.NebiusContractProbe` against a configured Nebius Token Factory key; fix compiler/runtime/schema incompatibilities immediately. If executable validation remains unavailable, implement **encrypted append-only audit storage** with tamper-evident chaining and migration/rotation semantics, then extend at-rest protection to any authenticated browser-profile metadata without persisting browser credentials outside the browser's own protected profile boundary.
