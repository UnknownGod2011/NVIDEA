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
- Local capability audit records use per-event protection on Windows plus a versioned append-only hash chain and crash-safe protected tail seals.
- **New:** production browser orchestration now uses `SegmentedAuditTrail`: active audit segments rotate after 1,000 events by default, immutable prior segments are pinned by protected cross-segment SHA-256 anchors, and rollover has a recoverable pending state rather than rewriting previous records.
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
- Added an independent protected sidecar at `audit.jsonl.seal`, encoded through the existing versioned local-state envelope with dedicated purpose `audit-tail-seal-v1`.
- The committed seal stores the exact audit sequence and tail SHA-256 chain hash. Once a seal exists, deleting the final record no longer leaves a valid-looking shortened chain.
- Append uses a write-ahead two-state protocol: protected `pending` seal -> append exact audit line -> protected `committed` seal.
- Recovery accepts only the exact pre-append or exact post-append chain state; anything else fails closed.
- A missing seal is bootstrapped only after the existing audit file passes parsing/hash-chain validation.
- Regression fixtures cover final-record deletion, pending-before/after-append recovery, legacy migration and duplicate preservation.

### 2026-09-07 — Bounded crash-safe audit segmentation
Completed:
- Added `src/Nvidea.Core/Capabilities/SegmentedAuditTrail.cs` as an `IAuditTrail` wrapper around the existing protected/hash-chained/tail-sealed `JsonLinesAuditTrail` rather than replacing the tested per-segment security primitive.
- The active segment is capped at 1,000 audit events by default. Segment 1 remains the existing `audit.jsonl`, preserving compatibility; later active segments use deterministic `audit.jsonl.segment-00000N.jsonl` names.
- Added a versioned `audit.jsonl.segments` manifest. On Windows it is protected through the existing CurrentUser DPAPI envelope with a distinct `audit-segment-manifest-v1` purpose.
- Every archived segment is pinned in that protected manifest by event count, SHA-256 of the exact segment bytes and SHA-256 of its protected tail-seal bytes. Missing/replaced/mutated archived data therefore fails closed before history is returned.
- Rollover is write-ahead and crash-safe: first persist a protected `pending` manifest that freezes the old active segment and names exactly the next index, then append the triggering event to the new independently sealed segment, then commit the manifest. If the new segment never appeared, recovery restores the previous committed manifest; if a non-empty valid new segment exists, recovery finalizes it without replaying any capability action.
- Global duplicate `AuditEvent.EventId` detection now spans archived plus active segments rather than resetting at a segment boundary.
- Existing single-file installations migrate conservatively: the base audit file is fully validated through `JsonLinesAuditTrail` before the first segment manifest is created; there is no destructive rename/rewrite during adoption.
- `BrowserHostRuntime` now constructs `SegmentedAuditTrail`, so both capability execution and resumable-job orchestration use segmented storage in the actual desktop/browser runtime.
- Added `SegmentedAuditTrailTests` covering bounded rotation/history order, archived-segment mutation detection through the protected cross-segment anchor, pending-before-new-segment recovery, pending-after-new-segment finalization without replay, and duplicate IDs across segments.

Validation / evidence:
- Rotation implementation commit: `dd377dff6aa598503313e2286bab83ce4d1a8a80`.
- Segmentation regression-test commit: `9c0a544b4b3580e617f0568410bfd2af6e55235e`.
- Production BrowserHost integration commit: `979bf29fec8f7d90e5544db94221fdee145494a6`; its GitHub commit diff was re-read and confirms the host change is exactly `JsonLinesAuditTrail` -> `SegmentedAuditTrail` at the audit composition point.
- The execution container was probed again for `dotnet`, `msbuild`, `csc` and `mcs`; none is available. Therefore **no compilation or test execution success is claimed**.
- No workflow run exists for the integration commit, and no GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- Segment archives retain the existing event encryption, intra-segment hash chain and protected tail seal; rotation adds an independently purpose-bound protected cross-segment manifest rather than weakening those controls.
- The pending rollover state only reconciles storage. It never reconstructs approval grants, executes a browser action, or retries an interrupted capability operation.
- Archive anchors hash exact data/seal bytes, so DPAPI nondeterminism cannot silently change an archived segment after it is frozen.
- As with the prior DPAPI design, this does not defend against an attacker already executing as the same Windows user with enough authority to invoke DPAPI and coherently rewrite all protected state.
- Segmentation bounds each active file by **event count**, not by byte size, and archived history is retained indefinitely. Total lifetime disk usage is therefore still unbounded until an explicit retention/compaction policy is designed; that limitation is intentional rather than silently deleting audit history.
- On non-Windows runtimes without an injected protector, the segment manifest is plaintext and therefore does not provide the Windows tamper-resistance property. Production Windows composition gets the DPAPI protector by default.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The live strict-schema probe still has not been compiled or run against Token Factory here because .NET and a Nebius API key are unavailable.
- DPAPI P/Invoke, protected stores, protected audit events/tail seals and protected segment manifests have not executed on a real Windows runner in this environment.
- Segmentation bounds active files by event count, but total archive retention and byte-size quotas are not yet implemented; deleting old audit history automatically would require an explicit user-visible retention policy and compacted integrity anchor rather than silent pruning.
- Browser-profile ownership/authenticated persistent sessions, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration + Windows DPAPI round-trip signal**, and run `tools/Nvidea.NebiusContractProbe` against a configured Nebius Token Factory key; fix compiler/runtime/schema incompatibilities immediately. If executable validation remains unavailable, implement **explicit browser-profile ownership and authenticated-session lifecycle with popup/new-tab tracking**, keeping credentials/session state local and preserving the current approval and host-boundary guarantees.
