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
- Windows-first desktop shell with global invocation, voice/text, context capture, permissions and emergency stop.
- Nemotron via Nebius Token Factory as the primary reasoning runtime with structured tools, routing, retries, cancellation and bounded execution.
- Layered privacy-aware memory, Tavily-backed research, safe browser automation, capability registry, single-use exact approvals, durable jobs and Nebius background execution.
- Private OS actions remain local; high-sensitivity Windows durable state uses CurrentUser DPAPI where implemented.

## Current State
- .NET 8 core at `src/Nvidea.Core`; WPF host at `src/Nvidea.Windows`.
- Nebius/Nemotron inference abstraction with structured output/tools, retries, timeout/cancellation and conservative routing.
- Layered personal memory, Tavily research engine, Playwright browser agent, deterministic verification, prompt-injection/safety boundaries, resumable browser-goal sessions and durable jobs.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer, protected segmented audit trail and emergency-stop plumbing.
- Production browser runtime uses an NVIDEA-owned persistent Chromium profile, popup/new-tab tracking and durable download quarantine.
- Browser downloads are captured as Receiving -> Ready/Interrupted with verified length/SHA-256 and DPAPI-protected metadata on Windows.
- `BrowserDownloadHandoffService` binds a specific download + exact canonical destination to a high-risk `FilesWrite` approval scope, revalidates immediately before export, consumes a short-lived single-use grant before side effects and audits the handoff without persisting the raw destination path.
- `BrowserDownloadDiscardService` similarly binds deletion to a specific retained payload identity, uses an exact fresh approval scope, and performs crash-recoverable `.payload -> .discarding -> Discarded` deletion without touching already-exported user files.
- Trusted WPF controls display sanitized filename, source host, verified size/SHA-256 and exact operation/destination before explicit human confirmation. WPF never receives or persists `ApprovalGrant`.
- Low-level quarantine export/discard primitives are assembly-internal; regression tests protect that API boundary.
- Retained download quarantine defaults to 512 MiB total / 128 MiB per verified file and never silently evicts Ready/Exported artifacts.
- Browser-managed in-progress download bytes are routed to an NVIDEA-owned Playwright `DownloadsPath` and guarded during transfer. Both Playwright staging bytes and the quarantine `.partial` copy default to 128 MiB ceilings; exceeding either invokes Playwright `Download.CancelAsync()` and fails closed before final quarantine promotion.
- Browser-managed temporary copies are explicitly deleted after quarantine capture/failure so they do not consume the next staging budget for the lifetime of the persistent context.
- Crash-leftover files in the NVIDEA-owned Playwright staging directory are reclaimed before Chromium starts. Reclamation is bounded, top-level only, rejects unexpected directories/reparse points, and blocks browser startup rather than broadening the delete boundary.
- Deterministic opt-in real-Chromium coverage exists for the oversized-download path. A throttled localhost fixture uses small assembly-internal test quotas to exercise real `DownloadsPath` growth, quota cancellation, durable `Interrupted` state and transient cleanup without weakening production defaults or moving 128 MiB during a test.
- **New:** protected segmented audit retention is now bounded by default to 32 archived segments and 64 MiB of archived segment+seal bytes. Retention pruning is represented by a protected cumulative tombstone (`PrunedThroughIndex`, pruned event count, and a SHA-256 digest chain over removed immutable anchors) before any evidence bytes are deleted. Exact pending-delete indices make pruning crash-recoverable and prevent retention from masquerading as unexplained history truncation.
- Root README + MIT license.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform milestones
Added Nebius/Nemotron inference, layered memory, Tavily research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification-contract migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit with tail seals and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent authenticated browser boundary
Added owned browser-profile markers, persistent Playwright startup, popup/new-tab tracking, privacy-minimized session snapshots, Chromium persistence/boundary tests and DPAPI-safe integration assertions.

### 2026-09-08 — Durable permissioned browser downloads
Added durable Receiving/Ready/Interrupted/Exported/Discarded lifecycle, SHA-256/length verification, exact-scope handoff, single-use grants, WPF confirmation, crash-recoverable discard and fail-closed retained-byte quotas. Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `bcca57e7c4577ac7bf118329fe3fd9ac4d2e20ab`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `494fb682f42d445d0ef50aca6f9d07c33ce8ca9c`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `26d8275339db8a5bd18a6fc5eab52d743b682a66`.

### 2026-09-08 — Bounded in-progress browser download staging
Completed:
- Added `BrowserDownloadStagingGuard` with independently configurable browser-staging and quarantine-partial limits plus a bounded polling interval.
- `PersistentBrowserContextFactory` configures Playwright `DownloadsPath` to `browser-downloads/browser-staging`, an NVIDEA-owned state path, deriving transient limits from the quarantine's 128 MiB single-download limit.
- `PlaywrightBrowserSessionDriver` runs `SaveAsAsync` through the staging guard, monitors both Playwright-managed staging bytes and the destination `.partial`, and invokes `IDownload.CancelAsync()` before surfacing `BrowserDownloadQuotaExceededException` if either exceeds policy.
- Capture has a bounded lifetime derived from the existing browser action timeout (clamped to 1–120 seconds) instead of allowing an event-forked download capture to wait indefinitely.
- The driver best-effort calls `IDownload.DeleteAsync()` after successful or failed quarantine capture so Playwright's transient duplicate does not consume the persistent context's staging budget after verification.
- Added `BrowserDownloadStagingGuardTests` for oversized `.partial`, oversized Playwright staging, normal under-limit completion, caller cancellation propagation and invalid configuration.
- Commits: `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `a1a8157454ee87dd60735c3bc515688d974f93ad`, `1f4a988c4ff367d02a9191dfa174e474515728b6`, `adbd92474ab4fe8240f685a6bc4c4b35b97a3fa6`.

### 2026-09-08 — Fail-closed startup staging reclamation
Completed:
- Added `BrowserDownloadStagingGuard.ReclaimStartupLeftovers()` and call it from `PersistentBrowserContextFactory` before `LaunchPersistentContextAsync`, when no legitimate browser transfer can be active.
- Reclamation is deliberately non-recursive and limited to 2,048 top-level entries by default. It first validates the whole candidate set before deleting anything, preventing a malformed sibling from causing partial cleanup.
- Unexpected directories, reparse-point entries, a reparse-point staging root/parent, path-boundary violations, or excessive entry populations fail closed and block browser startup rather than expanding deletion authority.
- Added a typed reclaim result with deleted file/byte counts for future diagnostics without retaining filenames/content.
- Added unit coverage for successful reclamation, refusal of unexpected directories while preserving sibling files, excessive-entry refusal before deletion, and invalid reclaim configuration.
- Commits: `5ed0e2753ad9d46610200303fc7b3418158d0d52`, `e0f5c693e380e230adf25ae71e776aebbdbee300`, `b5b4d896a5e59d3e67abeb9dc8da58ec57b6c42c`, `5d362f1067d2a31d146b7d256cb72b7f0a9699ef`.

### 2026-09-08 — Deterministic real-Chromium oversized-download fixture
Completed:
- Added an assembly-internal `PersistentBrowserContextFactory.LaunchAsync` configuration seam for integration/composition tests. The public production overload still owns conservative default quarantine/staging limits, preventing UI/model callers from widening them casually.
- Added `BrowserDownloadChromiumIntegrationTests.ThrottledOversizedDownload_IsCancelled_Interrupted_AndTransientStateIsRemoved` behind the existing `NVIDEA_RUN_BROWSER_INTEGRATION=1` opt-in gate.
- The fixture serves a 2 MiB attachment from localhost in 16 KiB throttled chunks while applying 128 KiB test-only staging/partial limits. This keeps the test small while forcing quota detection during a live transfer.
- Assertions cover `BrowserDownloadQuotaExceededException`, durable `Interrupted` metadata with no trusted length/hash, no promoted `.partial`/`.payload`, eventual empty Playwright staging, and server-side evidence that Chromium disconnected before the full attachment completed.
- Hardened asynchronous assertions with bounded eventual checks so source disconnect and Playwright file cleanup are not assumed to be synchronous.
- Updated `docs/browser-integration-harness.md` with prerequisites, exact test filters, expected failure interpretations and the security rationale for test-only quotas.
- Re-verified current official Playwright .NET documentation on 2026-09-08: `Page.Download` fires when transfer begins, `SaveAsAsync` is safe while transfer is in progress, `CancelAsync` cancels downloads, and download files are browser-context-owned temporary artifacts.
- Commits: `8d6fcd19cdf904c174d9a34a7047a1b13d6e5654`, `994234746644826d2f98a9ca3cd1974f85b1cce1`, `eb56c742605d1ce3b574fabe559125a57356c8ac`, `da392780c16bda6fa720328494c049882ccbecb7`, `3ea0254d5374ee13e0ef9b3e4afb223aaf1b26d9`.

### 2026-09-08 — Crash-safe bounded audit retention
Completed:
- Added `AuditRetentionPolicy`, enabled by default for `SegmentedAuditTrail` at 32 archived segments / 64 MiB archived bytes while keeping the active segment under the existing 1,000-event rotation boundary.
- Before a full active segment is rotated into immutable history, retention reserves one archive slot plus the segment's exact data+seal bytes. If that segment cannot fit the configured archived-byte budget on its own, the append fails closed before creating a new segment rather than silently deleting or partially recording audit evidence.
- Oldest archived segments are pruned only at complete segment boundaries. The protected manifest records `PrunedThroughIndex`, cumulative pruned-event count, and a cumulative SHA-256 digest over every removed immutable anchor.
- Retention uses a protected write-ahead delete set: the manifest commits the new pruned boundary and exact `PendingDeleteIndices` before deleting segment/seal files. A restart/read/append finishes only those exact pending deletions and then clears the list in a second protected manifest write.
- Manifest validation now supports a contiguous retained window after the pruned boundary and rejects malformed retention tombstones, invalid digest state, duplicate/out-of-order pending deletes, and inconsistent committed/pending rollover shapes.
- Added regression coverage for normal protected pruning, byte-quota fail-closed behavior before rotation, and simulated crash recovery from a manifest that committed pruning but had not yet deleted the old segment bytes.
- Commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `b74c884107056b175d9afbf773dc0bc5d7dc9125`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Re-read `progress.md`, recent commits, current repository tree, `AuditTrail.cs`, `SegmentedAuditTrail.cs`, `SegmentedAuditTrailTests.cs`, and the production `BrowserHostRuntime` audit composition before changing code.
- Re-read the changed retention implementation and tests after writes for manifest/rotation compatibility and crash-recovery ordering.
- Re-checked the execution environment for `dotnet`, `msbuild`, `csc`, and `mcs`; none is available, so compilation/test/WPF/Chromium/DPAPI execution is NOT claimed.
- No GitHub Actions workflow was rerun merely to obtain a green signal.

Security / privacy review:
- Retention cannot silently rewrite or truncate a retained segment: pruning operates only on previously anchored immutable segments.
- The protected retention tombstone preserves evidence that history was intentionally pruned, including how many events were removed and a digest chain over their immutable data/seal anchors, without retaining the events' sensitive plaintext metadata.
- Deletion authority is restricted to exact deterministic segment/seal paths already represented by protected pending-delete indices; no recursive cleanup or user-supplied path is introduced.
- Crash ordering is manifest-first, delete-second. If the process dies before the manifest commit, no segment is deleted. If it dies afterward, recovery can finish the exact committed deletions.
- A failed retention-capacity reservation blocks a new rotation before the new event is appended, preserving fail-closed audit semantics.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new audit-retention code and tests are statically reviewed but unexecuted here; constructor compatibility, System.Text.Json optional manifest-field migration, Windows file-delete behavior and DPAPI-protected retention recovery still require a real .NET 8 run.
- Audit byte retention currently bounds **archived segment + archived seal bytes**, not the current active segment or protected manifest itself. The active segment is event-count bounded, but an unusually large individual audit event could still make that segment large before rotation. A per-event logical payload ceiling / active-segment byte guard is a remaining hardening opportunity.
- Retention makes duplicate-event detection apply to the retained window only; intentionally pruned event IDs are no longer available for global duplicate detection. IDs are random GUIDs today, but this behavior should remain documented if external event IDs are introduced.
- The Chromium oversized-download fixture is implemented but unexecuted here; compile/runtime behavior, actual Windows `DownloadsPath` growth and source disconnect timing remain to be proven on a machine with .NET 8 + matching Playwright Chromium.
- The in-progress download guard is polling-based, not a filesystem hard quota. Overshoot can occur between polls and while browser cancellation propagates; it is bounded operationally rather than byte-perfect.
- Startup reclamation has unit-level design coverage only; Windows junction/reparse behavior and Chromium crash leftovers remain unexecuted here.
- WPF depends on .NET 8 `Microsoft.Win32.OpenFolderDialog`; compile on Windows before claiming compatibility.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- WPF download polling can initialize the browser runtime at window render time even when browser work was not requested, which is safe but suboptimal for startup latency/resources.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and immediately repair compile/runtime issues, including the new segmented-audit retention tests and throttled Chromium download fixture. If executable validation remains unavailable, add a conservative per-event audit payload ceiling plus active-segment byte guard and an explicit retention-status API/UI surface so long-running users can see that older protected audit history was deliberately pruned rather than infer it from a shortened event list.
