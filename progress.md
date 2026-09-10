# NVIDEA Hackathon Progress

## Mission
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must remain independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Product / Architecture State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Concrete privileged persistence/runtime boundaries are increasingly Core-only: `JsonAgentJobStore`, `ResearchJobRuntime`, raw `BrowserHostRuntime` construction, privileged `BrowserGoalAgent(BrowserHostRuntime,...)` construction, raw persistent Playwright transport, path-backed `JsonBrowserGoalSessionStore`, and path-backed `BrowserDownloadStagingGuard` construction cannot be bootstrapped by ordinary external product/plugin code.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution flows through `ResearchCloudExecutionCoordinator`; WPF durable research uses the lifecycle-aware facade and `ResearchProductUiState`.
- Browser product/UI flows through `BrowserProductRuntime`; WPF does not receive the raw host.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol. Deployment preflight enforces mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned worker image, RSA identity consistency, bounded resources, and redacted deployment fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, live runtime factory, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 — Product lifecycle and authority hardening
Added `ResearchCloudExecutionCoordinator`, `ResearchProductRuntime`, replay-race protection under mutation leases, WPF lifecycle-aware research integration, explicit remote reconciliation/disclosure UX, `ResearchProductUiState`, `BrowserProductRuntime`, WPF browser/download migration, internal raw browser-host construction, and least-authority `IBrowserGoalHost` composition.

### 2026-09-10 to 2026-09-11 — Concrete persistence/runtime boundary hardening
Made concrete `ResearchJobRuntime` and `JsonAgentJobStore` construction assembly-internal, preserved public least-authority interfaces, narrowed privileged `BrowserGoalAgent` construction, hid the raw persistent Playwright transport, and added reflection regression tests preventing public reconstruction of privileged local lifecycle/persistence/browser authorities.

### 2026-09-11 — Browser goal-session persistence authority hardening
Completed:
- Re-read this ledger completely and inspected current repo head/history, the Core tree, `NvideaCompositionRoot`, `BrowserGoalSessionStore`, download quarantine/staging candidates, and existing API-surface regression-test conventions.
- Re-checked executable validation availability. This environment still has no usable `dotnet` executable, so no compile/test success is claimed.
- Identified a genuine remaining persistence bypass: `JsonBrowserGoalSessionStore` was publicly constructible with an arbitrary path and exposes low-level `SaveAsync(BrowserGoalSession)`. External in-process product/plugin code could therefore point a fresh concrete store at NVIDEA's durable `goal-sessions.json` and write descriptive lifecycle/recovery state without going through the trusted `BrowserGoalAgent`, product authority, or explicit recovery service.
- Changed `JsonBrowserGoalSessionStore(string, ILocalStateProtector?)` from public to assembly-internal. The public `IBrowserGoalSessionStore` contract remains available for least-authority composition/test doubles.
- Preserved all store behavior: DPAPI-at-rest default on Windows, pending-action/private typed-value stripping, read/list/save semantics, startup plaintext migration, and the trusted `NvideaCompositionRoot.CreateBrowserGoalStore()` path.
- Added XML documentation explaining why concrete path-backed construction is privileged.
- Added `tests/Nvidea.Core.Tests/BrowserGoalSessionStoreApiSurfaceTests.cs`. The regression suite asserts that the concrete path-backed store has no public constructor, retains the trusted internal `(string, ILocalStateProtector?)` construction seam, still implements the public interface, and cannot expose its raw save authority through public concrete construction.

Commits this run:
- `0ded85dd3fbf63ce7373b98bc68007c67a77be17` — narrow browser goal session store construction.
- `09c9bfa355d6515731c19cf1f6bc74a7cdbd6b2b` — lock browser goal persistence behind Core boundary.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every mutation target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static inspection confirms `NvideaCompositionRoot.CreateBrowserGoalStore()` constructs the internal concrete store from within the same Core assembly, so trusted browser-goal creation/list/recovery composition remains source-accessible.
- `src/Nvidea.Core/Properties/AssemblyInfo.cs` already grants `InternalsVisibleTo("Nvidea.Core.Tests")`, so existing store tests and the new API-surface test retain internal access.
- The container exposes no usable `dotnet` binary; Core compilation, WPF/XAML compilation, Worker compilation, and test execution are therefore **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- Ordinary external product/plugin code can no longer bootstrap NVIDEA's path-backed browser-goal session persistence and directly write status, pending-job identifiers, exact-scope descriptions, counters, or verified-step metadata around the higher-level browser authorities.
- This does not claim the goal-session records are authorization tokens; the store deliberately persists descriptive/non-authorizing state only. The hardening removes an integrity/recovery-confusion shortcut rather than elevating the data to a secret.
- The public `IBrowserGoalSessionStore` interface intentionally remains available so `BrowserGoalAgent` can be tested/composed against least-authority in-memory/custom stores without exporting the NVIDEA-owned path-backed implementation.
- Browser execution, exact approval grants, capability checks, persistent profile ownership, prompt-injection defenses, download quarantine, crash recovery, and emergency stop were not weakened or removed.

### 2026-09-11 — Browser download staging authority hardening
Completed:
- Re-read `progress.md` completely, inspected current commits/tree, `BrowserDownloadQuarantine`, `BrowserDownloadStagingGuard`, `BrowserHostRuntime`, and existing download API-surface tests before changing code.
- Identified that `BrowserDownloadStagingGuard` remained publicly constructible with an arbitrary state directory even though it owns Playwright staging paths and exposes `ReclaimStartupLeftovers()`, which deletes crash-leftover files inside NVIDEA-owned transient browser state.
- Changed the path-backed `BrowserDownloadStagingGuard(string, BrowserDownloadStagingOptions?)` constructor from public to assembly-internal. The public option/result records remain available and all runtime behavior is unchanged.
- Preserved trusted Core composition: `PersistentBrowserContextFactory` / `BrowserHostRuntime` remain in the same assembly and can construct the guard normally; existing test code retains access through the current `InternalsVisibleTo("Nvidea.Core.Tests")` seam.
- Added XML documentation explaining why path-backed construction is privileged.
- Added `tests/Nvidea.Core.Tests/BrowserDownloadStagingGuardApiSurfaceTests.cs` to assert there is no public constructor, the intended internal `(string, BrowserDownloadStagingOptions)` seam remains, and no public static factory reconstructs the authority.

Commits this run:
- `0ef56bbadc2194f47e6ec4874da5416354c0decd` — narrow browser download staging authority.
- `d27db51c159544e3b5f08e521a9b7c57c8eabf37` — lock staging cleanup behind Core boundary.

Validation / evidence:
- Repository identity was explicitly verified before each mutation and every write target was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- A static compare from `5f8b8da66f864513f1351abce6c6c4b9c7c55f34` to `d27db51c159544e3b5f08e521a9b7c57c8eabf37` shows exactly two changed files: the staging guard and its new API-surface regression test.
- The source change is limited to constructor visibility plus explanatory XML documentation; quota checks, cancellation, startup reclaim validation, reparse-point protection, strict-child path checks, and measurement logic are unchanged.
- The container still exposes no usable `dotnet` binary; Core/WPF/Worker compilation and test execution are therefore **not claimed**.
- No live Nebius credentials/resources or paid provider calls were used and no GitHub Actions workflow was triggered merely to obtain a green result.

Security / privacy / failure review:
- Ordinary external product/plugin code can no longer point a newly constructed staging guard at NVIDEA state and invoke its transient-file reclamation authority outside the trusted browser lifecycle.
- The guard still fails closed on reparse points, unexpected directories, oversized entry populations, staging/partial quotas, cancellation, and path escapes.
- Browser downloads, quarantine capture, explicit export/discard approvals, persistent Chromium profile ownership, prompt-injection defenses, durable jobs, and emergency-stop behavior were not removed or weakened.
- `BrowserDownloadQuarantine` remains publicly constructible and is now the next browser-download authority candidate; unlike the staging guard it also owns durable metadata/capture state, so its cross-assembly/API implications should be inspected carefully before narrowing.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/API-surface changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, inspect `BrowserDownloadQuarantine` and protected research transport constructors next: narrow only concrete NVIDEA-owned path/credential-bearing mutation surfaces that can bypass product capability/lifecycle authorities, while preserving public record/protocol abstractions required by Worker, contract tooling, tests and future Nebius composition.
