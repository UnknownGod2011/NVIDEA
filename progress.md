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
- **Local security:** high-sensitivity durable Windows state protected with a CurrentUser DPAPI boundary, versioned formats, purpose binding, conservative migration and fail-closed reads.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- .NET 8 core at `src/Nvidea.Core` plus WPF host at `src/Nvidea.Windows`.
- Nebius Token Factory inference client with Nemotron default, structured output/tools, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright .NET browser driver, hard safety policy, capability execution boundary and deterministic verifier under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and privacy-minimized audit under `Capabilities`.
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
- Memory, durable jobs and browser-goal sessions use versioned protected local-state envelopes; on Windows the default protector is CurrentUser DPAPI.
- **New:** local capability audit records now use per-event protection on Windows plus a versioned append-only hash chain. Existing plaintext JSONL is migrated only after complete successful parsing; malformed/duplicate legacy records abort migration.
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
- Added `src/Nvidea.Core/Security/LocalStateProtection.cs` with `ILocalStateProtector`, a versioned `NVIDEA-STATE-V1` envelope and `WindowsDpapiLocalStateProtector`.
- Windows DPAPI uses CurrentUser semantics plus purpose-derived optional entropy and `CRYPTPROTECT_UI_FORBIDDEN`; no reusable key is stored beside app state.
- Memory, agent jobs and browser-goal sessions protect persisted bytes on Windows by default and safely migrate legacy plaintext only after successful deserialization under their store lock.
- Corrected DPAPI native ownership so provider-returned buffers are released with `LocalFree`.
- Added protection/migration tests for memory/jobs/browser-goal stores and non-Windows fail-closed DPAPI behavior.

### 2026-09-07 — Protected append-only audit trail
Completed:
- Reworked `src/Nvidea.Core/Capabilities/AuditTrail.cs` from plaintext JSONL into a versioned per-record audit format containing sequence, predecessor hash, protected-payload marker, payload and SHA-256 chain hash.
- On Windows, `JsonLinesAuditTrail` now defaults to the same CurrentUser DPAPI boundary but with independent purpose `audit-event-v1`; summaries and metadata are encrypted as part of the event payload rather than persisted in cleartext.
- Preserved append-only event semantics and duplicate-event rejection. Every read validates format version, monotonic sequence, predecessor hash and record hash before returning events.
- Existing plaintext JSONL is migrated under the audit trail gate only after every legacy event parses and validates. Duplicate IDs or malformed legacy JSON abort migration without rewriting the original file.
- Migration writes to a temporary file and replaces the original only after the new protected chain has been built.
- During review, caught and fixed a crypto-specific migration bug: because DPAPI protection is nondeterministic, rebuilding protected lines a second time would produce a different tail hash. Migration now builds the protected chain exactly once, writes those exact records and reuses the exact written tail hash for subsequent append.
- Protected payload byte arrays and decrypted plaintext buffers are zeroed after use where practical.
- Added `tests/Nvidea.Core.Tests/AuditTrailProtectionTests.cs` covering protected round-trip without cleartext summary persistence, payload tamper detection, interior-record deletion detection, plaintext migration followed by a valid append to the exact migrated chain, invalid legacy JSON non-rewrite, and duplicate-event non-append behavior.

Validation / evidence:
- Head before this run: `acbab155a6e13017f48d7707ecf863336573a3da`.
- Audit implementation commits: `dbc168c5b5c783ce62e30b171f4060863302e13d` and bug-fix commit `54d5678db493444cac9cc7106847f31bf75d2fcf`.
- Audit test commits: `29bc3b62379ada6d8e76c85fb109cbf66d55ef9a` and `d60d25b4386a8f8298c689e3899aa5dfd32da410`.
- The execution container was probed again for `dotnet`, `msbuild` and `csc`; none is available. Therefore **no compile or unit-test success is claimed**.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- Audit payload protection is purpose-separated from memory/jobs/browser sessions and no approval grant/provider API key/browser credential was introduced into durable state.
- The hash chain is integrity evidence for accidental corruption, record mutation/reordering and interior deletion. It is **not** claimed to resist an attacker already executing as the same Windows user who can invoke DPAPI and rewrite the complete chain.
- A standalone hash chain cannot prove that the final tail was not truncated; an independently protected tail seal/anchor is still needed for strong tail-truncation detection.
- Current format deliberately remains per-record appendable rather than encrypting the entire file, avoiding O(n) whole-file rewrites on every audit append.
- Existing plaintext audit migration is conservative and only legitimizes data after complete successful parse/validation.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The live strict-schema probe still has not been compiled or run against Token Factory here because .NET and a Nebius API key are unavailable.
- DPAPI P/Invoke, protected stores and the new protected audit path have not executed on a real Windows runner in this environment.
- Audit tail truncation cannot yet be detected because the chain tail has no independently protected anchor/seal. Rotation/archival semantics are also not implemented yet.
- Browser-profile ownership/authenticated persistent sessions, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration + Windows DPAPI round-trip signal**, and run `tools/Nvidea.NebiusContractProbe` against a configured Nebius Token Factory key; fix compiler/runtime/schema incompatibilities immediately. If executable validation remains unavailable, harden the new audit format with an **independently DPAPI-protected tail seal plus crash-safe recovery and bounded rotation/archival semantics**, so final-record truncation and segment-boundary tampering are detectable without weakening append-only behavior.
