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
- Local capability audit records use per-event protection on Windows plus a versioned append-only hash chain.
- **New:** the audit trail now also maintains an independently purpose-bound protected tail seal with a write-ahead pending state. Final-record truncation is detectable after the seal exists, while crashes before/after the append are deterministically recoverable.
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
- Reworked `src/Nvidea.Core/Capabilities/AuditTrail.cs` from plaintext JSONL into a versioned per-record audit format containing sequence, predecessor hash, protected-payload marker, payload and SHA-256 chain hash.
- On Windows, `JsonLinesAuditTrail` defaults to CurrentUser DPAPI with independent purpose `audit-event-v1`; summaries and metadata are encrypted as part of the event payload.
- Existing plaintext JSONL is migrated only after complete successful parse/validation; malformed or duplicate legacy records abort without rewrite.
- Migration writes a protected chain once to avoid DPAPI nondeterminism breaking the recorded tail hash.
- Tests cover protected round-trip, payload tampering, interior deletion, migration, malformed legacy input and duplicate IDs.

### 2026-09-07 — Protected crash-safe audit tail seal
Completed:
- Added an independent protected sidecar at `audit.jsonl.seal`, encoded through the existing versioned local-state envelope with dedicated purpose `audit-tail-seal-v1`.
- The committed seal stores the exact audit sequence and tail SHA-256 chain hash. Once a seal exists, deleting the final record no longer leaves a valid-looking shortened chain: chain state must match the independently protected committed anchor.
- Append now uses a write-ahead two-state protocol: first persist a protected `pending` seal containing both the previously committed tail and the exact next sequence/hash, then append the already-built audit line, then atomically replace the sidecar with a `committed` seal for the new tail.
- Recovery is deterministic under the audit gate. If a pending seal exists and the file matches the pre-append tail, NVIDEA restores the old committed seal. If the file matches the exact post-append tail, NVIDEA finalizes that pending tail. Any other combination fails closed.
- Seal writes use temporary files plus replacement and the seal schema validates version, non-negative sequence, SHA-256 hash shape, state, and the invariant that a pending append is exactly one sequence after the committed tail.
- A missing seal is bootstrapped only after the existing audit file has already passed legacy/current-format parsing and hash-chain validation. This protects future truncation but cannot retroactively prove that an installation was not truncated before its first seal was created.
- Added regression fixtures for final-record deletion detection, pending-after-append recovery, pending-before-append recovery, seal creation during plaintext migration, invalid-legacy non-sealing, and duplicate-event preservation of both audit and seal bytes.

Validation / evidence:
- Implementation commit: `cc0c729df1c3df6b2fdc0964d4460fb89111e9b5`.
- Tail-seal regression-test commit: `babd1ad7d4fede118eff35ad04a3ed90f6e5f160`.
- The execution container was probed for `dotnet`, `msbuild` and `csc`; none is available. Therefore **no compile, unit-test, WPF, Chromium or Windows-DPAPI execution success is claimed**.
- Source review confirms the new tail seal is purpose-separated from protected audit-event payloads and does not persist approval grants, provider keys, browser credentials or raw user content outside the already-protected audit event.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- The tail anchor materially improves accidental/tamper evidence for final-record truncation and replacement while preserving O(1) append behavior.
- The write-ahead pending state avoids treating an expected crash window as corruption and does not authorize replay of any capability action; it only reconciles audit persistence state.
- The seal still does **not** defend against an attacker already executing as the same Windows user with enough authority to invoke DPAPI and rewrite both the chain and seal coherently. This remains an explicitly bounded local-threat assumption.
- Segment rotation/archival is not yet implemented, so audit growth is currently unbounded.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The live strict-schema probe still has not been compiled or run against Token Factory here because .NET and a Nebius API key are unavailable.
- DPAPI P/Invoke, protected stores, protected audit events and the new protected tail-seal path have not executed on a real Windows runner in this environment.
- Audit segment rotation/archival semantics are not implemented; current growth is unbounded and segment-boundary integrity has not been designed yet.
- Browser-profile ownership/authenticated persistent sessions, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration + Windows DPAPI round-trip signal**, and run `tools/Nvidea.NebiusContractProbe` against a configured Nebius Token Factory key; fix compiler/runtime/schema incompatibilities immediately. If executable validation remains unavailable, implement **bounded audit segment rotation/archival with protected cross-segment anchors and crash-safe rollover**, so audit growth is controlled without losing tamper evidence at segment boundaries.
