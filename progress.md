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
- Production browser runtime now uses an NVIDEA-owned persistent Chromium profile and session-aware popup/new-tab tracking.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform milestones
- Added Nebius/Nemotron inference abstraction, layered memory, Tavily research, browser contracts, capability registry, approval boundary, durable jobs and Nebius Serverless contracts.
- Added Playwright execution, Windows shell, durable parent/child browser orchestration, deterministic crash reconciliation, typed postconditions, verification-contract v2 migration and end-to-end Chromium contract harnesses.
- Added live Nebius strict-schema probe using the production `NebiusTokenFactoryClient` and `NemotronBrowserPlanner`.
- Added CurrentUser DPAPI-backed local-state protection for memory/jobs/browser-goal sessions.
- Reworked audit persistence into protected hash-chained records, tail seals and bounded segmented rotation.

### 2026-09-07 — Browser profile ownership + popup/new-tab tracking
- Added `BrowserProfileOwnership` with a dedicated `browser-profile` directory beneath NVIDEA state and an atomic `.nvidea-profile.json` ownership marker.
- Existing unmarked/corrupt profile directories fail closed instead of being adopted.
- Added `PlaywrightBrowserSessionDriver`, which uses `BrowserContext.Page` events to track new pages and closes cross-boundary HTTP(S) popups rather than exposing them to the agent.
- Added privacy-minimized session snapshots containing page URL/boundary status only, with no cookie/local-storage/auth-header disclosure.
- Added profile-boundary tests.
- Commits: `f3335f59111de2928cc6e6c186370e25d8b29241`, `43cfb4ffb19d5c9a07d72887fa429353bbe656cf`, `384d136338f26eb9d17f61d4eb4f81c7c9a092a5`, `970b40bce228adfe97cfeacf34422b01e7887de8`.

### 2026-09-08 — Persistent Chromium session active in production runtime
Completed:
- Added `src/Nvidea.Core/Browser/PersistentBrowserContextFactory.cs`.
- The factory validates/creates the dedicated NVIDEA-owned profile and launches Chromium with Playwright `LaunchPersistentContextAsync`.
- On every startup, any restored tabs are closed before agent use. Browser-managed authenticated/profile state such as cookies/local storage may persist, but stale prior pages are never implicitly trusted as current agent context.
- The runtime creates a fresh explicitly permitted `StartUri` page after persistent-context launch.
- `BrowserHostRuntime` now uses the persistent-context factory and `PlaywrightBrowserSessionDriver` at the real production composition point. The former ephemeral `IBrowser -> NewContextAsync -> single PlaywrightBrowserDriver` path is removed.
- Persistent-context shutdown now closes the context as the browser-process ownership boundary; the obsolete separate `IBrowser` close path is removed.
- `BrowserHostRuntime.GetSessionSnapshotAsync` exposes the existing privacy-minimized session diagnostic.
- Hardened click/popup timing: already-created `about:blank` candidate pages receive a short bounded classification window so an allowed popup can finish navigation before the next typed verifier observation. This does not discover arbitrary future pages or interact with the popup before classification.
- Added `tests/Nvidea.Core.Tests/PersistentBrowserSessionIntegrationTests.cs` with opt-in real-Chromium coverage for harmless cookie persistence across a runtime restart, stale-tab non-adoption, same-host popup adoption and cross-host popup rejection.
- Updated `docs/browser-integration-harness.md` with the persistent-session and popup-boundary scenarios.

Validation / evidence:
- Persistent context factory commit: `d362130b0243c667f3d5de504a26ca41bf64cdb7`.
- Production BrowserHostRuntime wiring commit: `1ae2e2781846552b8d20c1250527a0be0a334e02`.
- Persistent-session integration tests commit: `4668403236f247eeb3832505b7b02780c5efe85e`.
- Popup classification timing hardening commit: `0995c08844300d9451b7ae55653a265f116c87ad`.
- Persistence-boundary clarification commit: `d42f2c7e2c5a0c2973f91b71d2b7271611295e36`.
- Integration-harness docs commit: `de674ceb9dfaad9691e08f9c7e7817d2b1c3f1dd`.
- Current official Playwright .NET docs were checked during this work. They confirm that `LaunchPersistentContextAsync` stores browser session data such as cookies/local storage in the supplied user-data directory, that closing the persistent context closes its browser, that a separate automation profile should be used instead of the user's default Chrome profile, and that `BrowserContext.Page` is the supported new-page event.
- No compilation, unit-test or Chromium-execution success was claimed because the available execution environment lacked a .NET toolchain.

Security / privacy review:
- The desktop runtime no longer needs or accepts the user's normal Chrome/Edge profile. Authentication state is scoped to the NVIDEA-owned profile directory.
- Startup deliberately discards restored page/tab context while retaining browser-managed profile state, reducing the chance that a stale authenticated page silently becomes agent-visible context after restart.
- Popup adoption remains host-boundary constrained; a disallowed HTTP(S) page is closed rather than becoming active agent context.
- Session diagnostics expose URLs/boundary status only and do not enumerate cookie values or local storage.
- Persistent Chromium profile data is local browser-managed data, **not** DPAPI-wrapped application state. Do not claim that browser cookies/profile files receive the same application-level encryption as NVIDEA memory/jobs/audit files.

### 2026-09-08 — DPAPI-safe real-browser integration assertions
Completed:
- Re-read `BrowserHostRuntimeIntegrationTests` and confirmed two integration scenarios still inspected raw `jobs.json` text even though the production job store is DPAPI-protected by default on Windows.
- Replaced those raw filesystem assertions with logical `JsonAgentJobStore.GetAsync(...)` assertions. This means Windows integration tests now validate the decrypted durable record through the same storage abstraction the application uses rather than making assumptions about ciphertext representation.
- The consequential-click test now verifies the durable child is actually `WaitingForApproval`, carries the exact approval scope, retains typed postconditions in its checkpoint, contains no grant/token material in the checkpoint, becomes `Completed`, and clears its persisted approval scope after the single approved execution.
- The Nemotron planner integration test now verifies the actual durable child record preserves typed postconditions, omits grant/bearer material, and clears approval state after verified completion.
- No production security boundary was weakened; the tests continue to verify that approval capabilities remain ephemeral while becoming compatible with protected-at-rest state.

Validation / evidence:
- Test-hardening commit: `245e281ca329fe7b07d73526ea9234c3270df8a3`.
- Source-level review confirms `JsonAgentJobStore` decrypts DPAPI-protected state on Windows before returning logical `AgentJobRecord` values and applies browser-checkpoint migration under its storage lock.
- The execution environment was checked again for `dotnet`, `msbuild`, and `csc`; none is available, so **no compile/test success is claimed**.
- No GitHub Actions workflow was created or rerun merely to manufacture a green result.

Security / privacy review:
- Tests no longer encourage treating encrypted durable files as inspectable plaintext.
- Assertions target only the durable record fields/checkpoint that are intentionally persisted; ephemeral approval grants remain outside `AgentJobRecord`.
- Clearing `ApprovalScope` after completion is now explicitly covered at the integration level, strengthening replay-resistance evidence.

## Current Unverified / Risks
- **Highest risk remains executable validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The production persistent-context path and browser integration tests have not compiled or executed in this environment.
- The live Token Factory strict-schema probe remains unexecuted because .NET and a Nebius API key are unavailable here.
- DPAPI P/Invoke, protected stores, audit payloads/tail seals/segment manifests remain unexecuted on a real Windows runner in this environment.
- Persistent Chromium profile contents are local but not application-encrypted by NVIDEA. OS/user-profile protections remain the boundary for Chromium-managed cookies and storage.
- Segmentation bounds active audit files by event count, but lifetime archive retention and byte-size quotas remain absent.
- Durable download lifecycle remains incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
Obtain the first real .NET 8 build/test/Chromium/Windows signal and immediately fix compile/runtime issues in the persistent-context and protected-store paths. Run the persistent restart + allowed/disallowed popup integration tests and the live Nebius strict-schema probe. If executable validation remains unavailable, implement the durable, permissioned download lifecycle next: quarantine browser downloads inside NVIDEA-owned state, verify completion/metadata before exposure, require an explicit user handoff for moving files outside quarantine, and ensure cancellation/restart cannot silently publish partial or unverified files.
