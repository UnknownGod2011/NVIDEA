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
- **New:** browser-managed in-progress download bytes are routed to an NVIDEA-owned Playwright `DownloadsPath` and guarded during transfer. Both Playwright staging bytes and the quarantine `.partial` copy default to 128 MiB ceilings; exceeding either invokes Playwright `Download.CancelAsync()` and fails closed before final quarantine promotion.
- Browser-managed temporary copies are explicitly deleted after quarantine capture/failure so they do not consume the next staging budget for the lifetime of the persistent context.
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
- `PersistentBrowserContextFactory` now configures Playwright `DownloadsPath` to `browser-downloads/browser-staging`, an NVIDEA-owned state path, and derives both transient limits from the quarantine's 128 MiB single-download limit.
- `PlaywrightBrowserSessionDriver` now runs `SaveAsAsync` through the staging guard, monitors both Playwright-managed staging bytes and the destination `.partial`, and invokes `IDownload.CancelAsync()` before surfacing `BrowserDownloadQuotaExceededException` if either exceeds policy.
- Capture now has a bounded lifetime derived from the existing browser action timeout (clamped to 1–120 seconds) instead of allowing an event-forked download capture to wait indefinitely.
- The driver best-effort calls `IDownload.DeleteAsync()` after successful or failed quarantine capture so Playwright's transient duplicate does not consume the persistent context's staging budget after verification.
- Added `BrowserDownloadStagingGuardTests` for oversized `.partial`, oversized Playwright staging, normal under-limit completion, caller cancellation propagation and invalid configuration.
- Commits: `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `a1a8157454ee87dd60735c3bc515688d974f93ad`, `1f4a988c4ff367d02a9191dfa174e474515728b6`, `adbd92474ab4fe8240f685a6bc4c4b35b97a3fa6`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Re-read `progress.md`, the full quarantine implementation, persistent-context composition, session driver and existing quota tests before changing code.
- Current Playwright .NET documentation confirms `Download.CancelAsync()` is the supported cancellation primitive and `BrowserTypeLaunchPersistentContextOptions.DownloadsPath` is supported for persistent contexts.
- `src/Nvidea.Core/Nvidea.Core.csproj` currently pins Microsoft.Playwright 1.62.0, well after `Download.CancelAsync()` was introduced.
- Recent commit chain was re-read after implementation and contains only the intended NVIDEA changes.
- Re-checked the execution environment for `dotnet`, `msbuild`, `csc`, and `mcs`; none is available, so compilation/test/WPF/Chromium/DPAPI execution is NOT claimed.
- No GitHub Actions workflow was rerun merely to obtain a green signal.

Security / privacy review:
- A hostile/accidental unknown-size transfer is no longer allowed to grow an NVIDEA `.partial` indefinitely before the final quota check.
- Playwright's own pre-SaveAs browser storage is also placed under an explicit NVIDEA-owned path and monitored, rather than leaving only the copied `.partial` bounded.
- Quota failure cancels the browser source before returning the failure and the quarantine's existing catch path deletes incomplete `.partial`/payload files and records Interrupted state.
- Retained verified artifacts are unaffected and never auto-evicted to make room for a hostile transfer.
- Transient browser bytes remain OS-user-profile protected rather than application-encrypted; they are untrusted and never exposed as approved user files.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new in-progress guard is polling-based, not a filesystem hard quota. Overshoot can occur between polls and while browser cancellation propagates; it is bounded operationally rather than byte-perfect.
- Need a real Playwright integration test proving `DownloadsPath` file growth is observable during a large transfer and `CancelAsync()` stops that transfer on Windows/Chromium as expected.
- Crash-leftover files in `browser-staging` should be explicitly reclaimed on next owned-browser startup; currently Playwright/context cleanup is relied upon for normal shutdown.
- WPF depends on .NET 8 `Microsoft.Win32.OpenFolderDialog`; compile on Windows before claiming compatibility.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Audit lifetime retention/byte quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- WPF download polling can initialize the browser runtime at window render time even when browser work was not requested, which is safe but suboptimal for startup latency/resources.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and immediately repair compile/runtime issues. If executable validation remains unavailable, add safe startup reclamation for stale NVIDEA-owned Playwright staging bytes and a deterministic integration fixture that serves a throttled large download, proving cancellation/cleanup behavior once Chromium execution becomes available.
