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
- Browser downloads are now captured into an NVIDEA-owned durable quarantine before a `Download` action may return successfully; quarantine metadata is DPAPI-protected by default on Windows and export is a separate explicit user-approved handoff.
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
- Added `BrowserProfileOwnership` with a dedicated `browser-profile` directory and ownership marker; unmarked/corrupt profiles fail closed.
- Added `PlaywrightBrowserSessionDriver` using `BrowserContext.Page` to track popups/new tabs and reject cross-boundary HTTP(S) pages.
- Added privacy-minimized session snapshots containing page URL/boundary state only.
- Added `PersistentBrowserContextFactory` using Playwright `LaunchPersistentContextAsync`; startup closes restored tabs while preserving browser-managed profile/auth state.
- Wired persistent Chromium into production `BrowserHostRuntime` and added opt-in integration coverage for cookie persistence, stale-tab non-adoption and popup boundaries.
- Fixed browser integration tests to inspect logical DPAPI-decrypted durable job records instead of raw `jobs.json` text.
- Representative commits: `f3335f59111de2928cc6e6c186370e25d8b29241`, `43cfb4ffb19d5c9a07d72887fa429353bbe656cf`, `d362130b0243c667f3d5de504a26ca41bf64cdb7`, `1ae2e2781846552b8d20c1250527a0be0a334e02`, `245e281ca329fe7b07d73526ea9234c3270df8a3`.

### 2026-09-08 — Durable permissioned browser download quarantine
Completed:
- Added `src/Nvidea.Core/Browser/BrowserDownloadQuarantine.cs`.
- Every browser artifact managed by this subsystem has a durable record with id, source page URI, sanitized suggested filename, lifecycle state, byte length, SHA-256 digest, timestamps, failure state and optional explicit export path.
- Download bytes first land at an NVIDEA-owned `.partial` quarantine path. Only after the producer completes, the file exists, its length is measured, and SHA-256 is computed is it atomically renamed to a stable `.payload` quarantine object and marked `Ready`.
- A durable `Receiving` record is written before browser bytes are accepted. On restart, any still-`Receiving` entry becomes `Interrupted`; partial files are cleanup-only and are never promoted automatically.
- Export is a separate operation requiring `userApproved: true`, an already-existing caller-selected destination directory, a safe sanitized leaf filename, no overwrite of an existing file, and re-verification of the quarantine payload's length + SHA-256.
- Export uses a destination-side temporary file, verifies the copied bytes again, then atomically renames it to the final destination. The original quarantine payload is retained for audit/recovery rather than silently moved away.
- Download metadata uses the existing `NVIDEA-STATE-V1` local-state envelope with purpose `browser-download-metadata-v1`; on Windows the default protector is CurrentUser DPAPI. The payload itself remains a local quarantined file and is not claimed to be application-encrypted.
- Added `tests/Nvidea.Core.Tests/BrowserDownloadQuarantineTests.cs` covering capture/hash persistence, approval-required export, tamper detection, successful handoff, path traversal sanitization, cancellation/interrupted state, protected metadata non-disclosure, and no-overwrite behavior.
- Hardened `PlaywrightBrowserSessionDriver` so browser actions are serialized and a `BrowserActionKind.Download` requires a configured quarantine boundary.
- `PlaywrightBrowserSessionDriver` subscribes to Playwright `Page.Download`, associates capture with the source page and a monotonic sequence, waits for the post-click download capture, calls Playwright `SaveAsAsync`, checks `FailureAsync`, and only allows the `Download` action to return after durable quarantine capture succeeds.
- A download from another page or an earlier sequence cannot prove the current download action. Unrelated captures may still be quarantined but are not accepted as action evidence.
- `PersistentBrowserContextFactory` now constructs `BrowserDownloadQuarantine(stateDirectory)` and injects it into the production persistent session driver, so real `BrowserHostRuntime` download actions use this boundary rather than merely clicking a download control.
- The session driver exposes `ListDownloadsAsync` and `ExportDownloadAsync` for an explicit handoff surface. The desktop UX/runtime still needs a polished user-facing download panel/confirmation flow; no autonomous export path was added.

Validation / evidence:
- Download quarantine commit: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`.
- Quarantine regression tests commit: `588fdee148fca3c98c5ed90ac758aa899e689f69`.
- Playwright download capture integration commit: `58374c4126288b5bfffd62e343667f7d1ce746e9`.
- Production persistent-session wiring commit: `2bd59d1aa21e2a246a36e2ea65e834d5db279ca0`.
- Current official Playwright .NET documentation was checked. It states that `Page.Download` is emitted when a download starts, downloaded context-owned files are otherwise temporary, and `SaveAsAsync` is the supported mechanism for persisting the completed download. The current BrowserType API also exposes `AcceptDownloads` on persistent contexts.
- Source-level review of the committed files was performed after mutation.
- Execution environment was checked again: `dotnet`, `msbuild`, `csc`, and `mcs` are unavailable. Direct GitHub cloning also still fails DNS resolution in the container. Therefore **no compile, unit-test, Chromium-run, DPAPI-run, or live Token Factory success is claimed**.
- No GitHub Actions workflow was created, modified or rerun merely to manufacture a green signal.

Security / privacy review:
- Browser download is no longer synonymous with publishing an arbitrary site-controlled filename into the user's Downloads folder.
- Suggested filenames are reduced to a bounded safe leaf filename; path separators/control characters are removed and export re-checks that the final path remains under the approved directory.
- Existing destination files are never overwritten by this handoff path.
- Quarantine payload tampering is detected before export by exact byte length and SHA-256 comparison, and the copied destination temp file is re-verified before final rename.
- Cancellation/restart cannot auto-export a partial payload. A failed or interrupted record is not exportable.
- Browser download metadata can contain sensitive source URLs/filenames, so it is DPAPI-protected by default on Windows. Quarantine payload bytes are not application-encrypted and rely on the local Windows user/profile boundary.
- The Playwright event-handler capture is deliberately non-authorizing: it may finish quarantining bytes after the calling action is cancelled, but this can only result in a local `Ready` quarantine record; it cannot export/publish the file.
- Current handoff approval is an explicit API boolean, not yet a single-use cryptographic approval grant integrated into `ScopedApprovalAuthorizer`. The desktop handoff UI must therefore remain a trusted caller boundary until that integration is added.

## Current Unverified / Risks
- **Highest risk remains executable validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The production persistent-context path, download event/capture path and browser integration tests have not compiled or executed in this environment.
- The live Token Factory strict-schema probe remains unexecuted because .NET and a Nebius API key are unavailable here.
- DPAPI P/Invoke, protected stores, audit payloads/tail seals/segment manifests and protected download metadata remain unexecuted on a real Windows runner in this environment.
- Persistent Chromium profile contents and quarantined download payload bytes are local but not application-encrypted by NVIDEA. OS/user-profile protections remain their confidentiality boundary.
- The download handoff API still uses a trusted `userApproved` boolean instead of a single-use scoped approval capability and is not yet surfaced as polished Windows UX.
- Unsolicited/background page downloads can be quarantined; their capture task is not authorization to export, but the session driver should eventually add explicit lifecycle diagnostics and cleanup/retention policy.
- Segmentation bounds active audit files by event count, but lifetime archive retention and byte-size quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
Obtain the first real .NET 8 build/test/Chromium/Windows signal and immediately fix compile/runtime issues in the persistent-context, protected-store and new download-quarantine paths. Run an opt-in localhost Chromium test that serves a real attachment, proves `BrowserActionKind.Download` cannot finish before quarantine capture, proves restart leaves only `Ready`/`Interrupted` durable states, and proves explicit export rejects tampering/overwrite. If executable validation remains unavailable, replace the download handoff `userApproved` boolean with a single-use exact-scope approval grant integrated with the existing capability/audit boundary and surface a minimal trusted Windows confirmation/list UI without enabling autonomous export.
