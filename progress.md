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
- Concrete privileged persistence/runtime boundaries are Core-only where appropriate: `JsonAgentJobStore`, `ResearchJobRuntime`, raw `BrowserHostRuntime` construction, privileged `BrowserGoalAgent(BrowserHostRuntime,...)` construction, raw persistent Playwright transport, path-backed `JsonBrowserGoalSessionStore`, path-backed `BrowserDownloadStagingGuard`, and path-backed `BrowserDownloadQuarantine` construction cannot be bootstrapped by ordinary external product/plugin code.
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
Made concrete `ResearchJobRuntime` and `JsonAgentJobStore` construction assembly-internal, preserved public least-authority interfaces, narrowed privileged `BrowserGoalAgent` construction, hid raw persistent Playwright transport, narrowed path-backed browser goal-session and staging construction, and added reflection regression tests preventing public reconstruction of privileged local lifecycle/persistence/browser authorities.

### 2026-09-11 — Browser download quarantine construction authority hardening
Completed:
- Re-read this ledger completely and inspected current repo head/history, the Core browser tree, `BrowserDownloadQuarantine`, `PersistentBrowserContextFactory`, download handoff/discard services, snapshot reader, and existing API-surface regression-test conventions.
- Re-checked executable validation availability. This environment still has no usable `dotnet` executable, so no compile/test success is claimed.
- Identified a genuine remaining browser-state authority bypass: `BrowserDownloadQuarantine` remained publicly constructible with an arbitrary state directory and exposes raw capture plus crash-recovery/list behavior over NVIDEA-owned durable download metadata/payload state. External in-process product/plugin code could therefore bootstrap that concrete state authority outside the trusted persistent-browser lifecycle even though low-level export/discard primitives were already internal.
- Changed both path-backed `BrowserDownloadQuarantine` constructors from public to assembly-internal. The public type and public record/options/receipt abstractions remain intact so higher-level public service signatures are not broken, while ordinary external code can no longer create a fresh concrete quarantine over NVIDEA state.
- Preserved all runtime behavior: bounded retained/per-file quotas, HTTP(S)-source validation, filename sanitization, capture cancellation, interrupted-download recovery, DPAPI metadata protection on Windows, hash/length verification, exact export/discard semantics, and shared metadata synchronization remain unchanged.
- Added XML documentation describing why concrete path-backed construction is privileged.
- Added `tests/Nvidea.Core.Tests/BrowserDownloadQuarantineConstructionApiSurfaceTests.cs`. It asserts there are no public constructors or public static factories producing a quarantine, both trusted internal constructor seams remain available, and the composed instance still exposes `CaptureAsync`, `ListAsync`, and `GetAsync` for trusted Core/browser use.

Commits this run:
- `b884245d9e9675c38d89fe8a26916ac25803fa29` — narrow browser download quarantine construction.
- `b1fd4a568678a123622e0b88e79890123f9149b3` — lock quarantine construction behind Core boundary.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every mutation target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static compare from `b3eb455913ddaf575c36e611c28044f6d76b4a39` to `b1fd4a568678a123622e0b88e79890123f9149b3` is two commits ahead and shows exactly two changed files: `BrowserDownloadQuarantine.cs` (4 additions, 2 deletions) and the new API-surface regression test.
- The source diff is limited to constructor visibility plus explanatory XML documentation; capture, recovery, quota, DPAPI, verification, export/discard and persistence algorithms are unchanged.
- `PersistentBrowserContextFactory` remains in the same Core assembly and continues constructing `BrowserDownloadQuarantine` through the internal trusted path, so the existing browser download pipeline remains source-accessible.
- Existing Core tests retain internal access through the established `InternalsVisibleTo("Nvidea.Core.Tests")` seam.
- The environment exposes no usable `dotnet` binary; Core/WPF/Worker compilation, XAML compilation and test execution are therefore **not claimed**.
- No live Nebius credentials/resources or paid provider calls were used and no GitHub Actions workflow was triggered merely to obtain a green result.

Security / privacy / failure review:
- Ordinary external product/plugin code can no longer instantiate NVIDEA's concrete path-backed quarantine and directly create/recover durable download state outside the trusted browser lifecycle.
- Consequential export/discard still require the existing higher-level exact-scope approval services; their internal quarantine mutation primitives were not widened.
- DPAPI-at-rest behavior, cryptographic payload verification, path-containment checks, bounded storage, prompt-injection defenses, persistent-profile ownership, durable browser jobs, and emergency-stop behavior were not removed or weakened.
- Keeping the class itself public avoids inconsistent-accessibility breakage in the existing public handoff/discard service constructors while removing the actionable bootstrap path: construction.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/API-surface changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, inspect protected research transport/provider constructors next and narrow only concrete NVIDEA-owned path/credential-bearing mutation surfaces that can bypass product lifecycle/capability authorities, while preserving the public protocol abstractions required by Worker, contract tooling, tests and future Nebius composition.
