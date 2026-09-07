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
- **Local security:** high-sensitivity durable Windows state protected with CurrentUser DPAPI, purpose binding, conservative migration and fail-closed reads.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- .NET 8 core at `src/Nvidea.Core` plus WPF host at `src/Nvidea.Windows`.
- Nebius Token Factory inference client with Nemotron default, structured output/tools, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright browser driver, hard safety policy, capability execution boundary and deterministic verifier under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and privacy-minimized audit under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns fresh untrusted observations into validated one-step decisions using typed postconditions.
- `BrowserGoalAgent` runs a bounded observe -> plan -> durable child -> verify loop, halts at approval boundaries, and independently rejects legacy/unverifiable autonomous action contracts before child reservation.
- Typed browser postconditions and verification-contract v2 are enforced for durable browser actions; safe legacy records migrate and ambiguous legacy mutations quarantine without execution.
- Memory, durable jobs and browser-goal sessions use versioned protected local-state envelopes; on Windows the default protector is CurrentUser DPAPI.
- Local capability audit uses protected per-event payloads, append-only hash chaining, crash-safe protected tail seals, segmented rotation and protected cross-segment manifests.
- Production browser orchestration uses `SegmentedAuditTrail`.
- Production browser runtime uses an NVIDEA-owned persistent Chromium profile and session-aware popup/new-tab tracking.
- Browser downloads are captured into an NVIDEA-owned durable quarantine before a `Download` action may return successfully; quarantine metadata is DPAPI-protected by default on Windows.
- `BrowserDownloadHandoffService` now provides an exact-scope, single-use approval and audit boundary for releasing a quarantined artifact to a user-selected destination. Production driver/UI migration to this service is still pending; the low-level quarantine boolean remains only as a trusted primitive for now.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform milestones
- Added Nebius/Nemotron inference abstraction, layered memory, Tavily research, browser contracts, capability registry, approval boundary, durable jobs and Nebius Serverless contracts.
- Added Playwright execution, Windows shell, durable parent/child browser orchestration, deterministic crash reconciliation, typed postconditions, verification-contract v2 migration and end-to-end Chromium contract harnesses.
- Added live Nebius strict-schema probe using the production `NebiusTokenFactoryClient` and `NemotronBrowserPlanner`.
- Added CurrentUser DPAPI-backed local-state protection for memory/jobs/browser-goal sessions.
- Reworked audit persistence into protected hash-chained records, tail seals and bounded segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent authenticated browser boundary
- Added NVIDEA-owned browser-profile ownership markers, persistent Playwright context startup, safe popup/new-tab tracking, privacy-minimized session snapshots, and opt-in Chromium persistence/boundary tests.
- Production `BrowserHostRuntime` uses the persistent Chromium composition.
- Fixed Windows integration tests to inspect logical DPAPI-decrypted job records instead of raw encrypted `jobs.json` text.

### 2026-09-08 — Durable permissioned browser downloads
- Added `BrowserDownloadQuarantine`: durable Receiving/Ready/Interrupted/Exported lifecycle; `.partial` -> SHA-256/length verification -> atomic `.payload`; restart interruption recovery; DPAPI-protected metadata; sanitized filenames; no-overwrite and destination-boundary checks; atomic verified export copy.
- `PlaywrightBrowserSessionDriver` correlates `Page.Download` events to the active page/action sequence and does not complete `BrowserActionKind.Download` until quarantine capture succeeds.
- Production persistent browser composition injects the download quarantine.
- Added quarantine regression tests for capture/hash persistence, tampering, cancellation, protected metadata and no-overwrite behavior.
- Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `588fdee148fca3c98c5ed90ac758aa899e689f69`, `58374c4126288b5bfffd62e343667f7d1ce746e9`, `2bd59d1aa21e2a246a36e2ea65e834d5db279ca0`.

### 2026-09-08 — Exact-scope download handoff approval boundary
Completed:
- Added `src/Nvidea.Core/Browser/BrowserDownloadHandoffService.cs`.
- `PrepareAsync` re-reads the durable quarantine record, requires a Ready/Exported state and an already-existing destination directory, then produces a high-risk consequential `FilesWrite` capability invocation.
- Approval scope binds the exact download id to a SHA-256 fingerprint of the canonical destination path via the stable action id. A grant prepared for one folder cannot authorize another folder.
- `ExportAsync` reconstructs the plan immediately before execution and fails closed if the download/destination/action/scope differs from what the confirmation surface prepared.
- The existing `ScopedApprovalAuthorizer.TryAuthorize` is used immediately before export, so grants are expiring and single-use. A failed/cancelled/ambiguous export does not restore the consumed grant; retry requires fresh human approval.
- Handoff audit events cover awaiting approval, scope change, started, succeeded, cancelled and failed. Audit metadata stores download id + destination fingerprint rather than a full potentially-sensitive destination path.
- The service deliberately delegates bytes/hash/path/no-overwrite/atomic-copy verification back to `BrowserDownloadQuarantine`; it does not duplicate the storage integrity layer.
- Added `tests/Nvidea.Core.Tests/BrowserDownloadHandoffServiceTests.cs` covering approval-required export, single-use enforcement, cross-destination scope mismatch, prepared-plan mutation rejection and failed-export grant consumption.

Validation / evidence:
- Scoped handoff implementation commit: `74b1c00ea306a825486a32d55be26dfca8bbf3fd`.
- Scoped handoff regression tests commit: `ec2eca992d867d27193e527fb9667b77f817de71`.
- Repository identity was explicitly re-verified as `UnknownGod2011/NVIDEA` before every mutation in this run.
- Source-level review used the current `ScopedApprovalAuthorizer`, `CapabilityPermissionPolicy`, `IAuditTrail`, quarantine and Playwright session code rather than inventing a parallel authorization design.
- Executable validation remains unavailable in this tool environment; no compile/test success is claimed and no GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy review:
- A boolean cannot serve as transferable proof of approval; the new service requires an ephemeral grant registered inside `ScopedApprovalAuthorizer` and scoped to the exact capability/action/permission tuple.
- Destination paths can themselves contain personal information; durable audit records therefore contain a SHA-256 destination fingerprint rather than the raw path.
- The untrusted source passed to capability policy is reduced to the source host rather than the full potentially-sensitive download URL.
- Scope is reconstructed immediately before export, mitigating caller mutation of a prepared confirmation object.
- Approval is intentionally consumed before the consequential filesystem copy. If the outcome is ambiguous, retrying cannot duplicate the side effect without fresh user confirmation.
- Remaining gap: `BrowserDownloadQuarantine.ExportAsync(..., userApproved: true)` is still a public low-level primitive and `PlaywrightBrowserSessionDriver.ExportDownloadAsync` still exposes the boolean path. The new service is the intended authoritative production boundary, but those production callers must be migrated before the boolean can safely be internalized/removed.

## Current Unverified / Risks
- **Highest risk remains executable validation:** source review is not a substitute for `dotnet build`, `dotnet test`, Windows WPF launch and a real Playwright Chromium launch.
- Persistent Chromium, browser download capture, new handoff service/tests and DPAPI-protected stores have not compiled/executed in this environment.
- Live Token Factory strict-schema probe remains unexecuted because .NET and a Nebius API key are unavailable here.
- Persistent Chromium profile contents and quarantined payload bytes are local but not application-encrypted by NVIDEA; OS/user-profile protections remain their confidentiality boundary.
- Production download export still needs migration from the legacy trusted boolean to `BrowserDownloadHandoffService`, followed by making the low-level boolean method non-public or removing it.
- A minimal Windows list/confirmation UI for Ready quarantined downloads is still absent.
- Unsolicited/background page downloads can be quarantined; cleanup/retention policy remains future work.
- Segmentation bounds active audit files by event count, but lifetime archive retention and byte-size quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
First obtain a real .NET 8 build/test/Chromium/Windows signal and immediately fix compile/runtime issues. If executable validation remains unavailable, wire `BrowserDownloadHandoffService` into the production persistent-browser/runtime boundary, replace `PlaywrightBrowserSessionDriver.ExportDownloadAsync(..., bool userApproved)` with prepare/approve/export APIs using `ApprovalGrant`, then make the quarantine's boolean export primitive internal. Surface a minimal trusted WPF Ready-download list + explicit destination confirmation that creates a short-lived exact-scope grant only after the human approves. Preserve the rule that the agent may quarantine autonomously but can never release a file outside NVIDEA state without fresh explicit approval.
