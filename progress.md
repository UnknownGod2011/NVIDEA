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
- Raw durable job persistence is now trusted Core infrastructure: `JsonAgentJobStore` remains the low-level `IAgentJobStore` implementation, but its concrete construction is assembly-internal so external product/plugin code cannot ordinarily point blind `SaveAsync` mutation at NVIDEA's job state around lifecycle/approval authorities.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport publishes encrypted work items/bindings/results directly through Nebius Object Storage while the worker consumes the same bucket prefix through a Serverless-mounted directory.
- Deployment preflight enforces exact Object Storage ↔ Serverless mount alignment, READ_WRITE transport, MysteryBox-backed worker credentials, digest-pinned worker image, RSA strength/identity consistency, bounded compute/storage settings, and a redacted reproducible deployment fingerprint.
- `Nvidea.NebiusContractProbe` supports cheap planner, zero-cost live preflight, and explicit paid live research; redacted manifest/PASS evidence persistence and offline verification are fail-closed and reproducible.
- `ResearchCloudExecutionCoordinator` is the narrow provider-aware bridge from durable research state to `NebiusResearchClientRuntime`, with exact checkpoint/disclosure approval, shared mutation lease, provider reconciliation/cancellation, private-data rejection, and no provider credential exposure.
- `ResearchJobRuntime` is intentionally local-only and validates current durable location/provenance under the mutation lease before any local Nemotron/Tavily handler invocation. Its concrete constructor is assembly-internal so external product/plugin code cannot bootstrap it around lifecycle-aware routing.
- `ResearchProductRuntime` is the lifecycle-aware product facade: truthful local/remote status, local work only through `ILocalResearchRuntime`, remote lifecycle only through `IResearchCloudExecutionCoordinator`, and independently gated paid Serverless dispatch.
- WPF durable-research reads/actions use `ResearchProductRuntime`; remote/ambiguous records are never offered as local recovery. `ResearchProductUiState` centralizes control enablement, labels, and cloud disclosure.
- Browser product/UI access goes through constrained `BrowserProductRuntime`; the composition root does not expose raw `BrowserHostRuntime`, and WPF navigation/download flows use only the facade.
- Raw `BrowserHostRuntime` construction is assembly-internal: there is no public instance constructor or public static factory that external product/plugin code can use to create the privileged host.
- `BrowserGoalAgent` publicly accepts only `IBrowserGoalHost`; its convenience constructor taking privileged `BrowserHostRuntime` is assembly-internal for trusted Core composition and tests.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox reference validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 — Remote research production safety
Added `ResearchCloudExecutionCoordinator`, narrow remote-runtime contracts, explicit per-stage cloud approval, remote lifecycle reconciliation/cancellation, privacy-safe Nebius status projection, and fail-closed recovery/cancellation for ambiguous `DispatchReserved` state. Closed duplicate-execution paths where uncertain/remote Serverless work could otherwise be mistaken for crashed local work.

### 2026-09-10 — Lifecycle-aware product + Windows integration
Added `ResearchProductRuntime`, lifecycle-aware status projection, replay-race closure under the mutation lease, WPF migration to the product facade, separate reconciliation UX, explicit cloud-disclosure copy, cancellation-token hardening, `ResearchProductUiState`, and removal of the public local `ResearchJobRuntime` composition-root escape hatch.

### 2026-09-10 — Browser product authority narrowing
Added `BrowserProductRuntime`, replaced the public composition-root raw-host getter with `GetBrowserProductAsync()`, migrated WPF browser/download flows, added reflection regression coverage, made `BrowserHostRuntime.CreateAsync(...)` assembly-internal, and narrowed `BrowserGoalAgent` so the privileged concrete-host constructor is assembly-internal while `IBrowserGoalHost` remains the public integration contract.

### 2026-09-10 — Local research construction hardening
Made `ResearchJobRuntime` construction assembly-internal and added API-surface regression coverage while preserving the public least-authority `ILocalResearchRuntime` contract and all working local research behavior.

### 2026-09-11 — Current run: close raw durable-job persistence bypass
Completed:
- Re-read this ledger completely before implementation and inspected current repository head/history, the Core tree, `NvideaCompositionRoot`, `JsonAgentJobStore`, `JobContracts`, `ResearchCloudExecutionCoordinator`, `Nvidea.Worker`, `InternalsVisibleTo`, and existing API-surface-test conventions.
- Confirmed there is still no usable `dotnet` executable in the execution environment, so the fallback authority audit was selected rather than fabricating build/test evidence.
- Identified a genuine bypass-capable primitive: `JsonAgentJobStore` had a public constructor while exposing blind low-level `SaveAsync(AgentJobRecord)`. External in-process product/plugin code could therefore point a new store at the same durable job file and write lifecycle/provenance/approval state without going through `BrowserProductRuntime`, `ResearchProductRuntime`, the browser host guard, or `ResearchCloudExecutionCoordinator`.
- Changed only `JsonAgentJobStore` construction from `public` to `internal`; its class, `IAgentJobStore` implementation, Get/List/Save behavior, CAS implementation, DPAPI protection, migration behavior, and trusted Core call sites are unchanged.
- Added XML documentation explicitly classifying the store as trusted persistence infrastructure and directing product/plugin callers to constrained runtimes/coordinator contracts instead of raw durable mutation.
- Added `tests/Nvidea.Core.Tests/JsonAgentJobStoreApiSurfaceTests.cs`. Reflection coverage asserts there is no public constructor, preserves the expected assembly-internal `(string, ILocalStateProtector?)` construction path for trusted Core/tests, verifies `IAgentJobStore` remains a public least-authority abstraction, and records that low-level `SaveAsync` stays reachable only after trusted construction.
- Inspected the separate `Nvidea.Worker` executable before narrowing construction; the worker does not instantiate `JsonAgentJobStore`. `ResearchCloudExecutionCoordinator` remains in the Core assembly and continues constructing the same store internally.

Commits this run:
- `04aeaf2ffaa64e97cda89bfef2353691bc0fb456` — narrow raw durable job store construction.
- `c6aff5276e5d9a0bfae036f63ad0b80b1adc338e` — lock raw job-store construction boundary with reflection coverage.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; each write target resolved to `full_name: UnknownGod2011/NVIDEA`. No other repository was mutated.
- Post-change source inspection confirms `JsonAgentJobStore` is still public and fully functional but its constructor is now `internal`.
- `src/Nvidea.Core/Properties/AssemblyInfo.cs` grants `InternalsVisibleTo("Nvidea.Core.Tests")`, preserving direct construction for the test assembly.
- `ResearchCloudExecutionCoordinator` is in `Nvidea.Core`, so its existing trusted `new JsonAgentJobStore(...)` path remains source-accessible.
- `Nvidea.Worker/Program.cs` uses the protected remote transport/worker protocol and does not depend on constructing the local JSON job store.
- The environment has no usable `dotnet` binary; Core compilation, WPF/XAML compilation, Worker compilation, and test execution are therefore **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- Ordinary external product/plugin code can no longer bootstrap direct blind writes against the protected browser/research job file through the concrete JSON persistence implementation.
- This preserves the intended architecture: product mutations flow through constrained lifecycle/capability authorities, while raw state persistence stays inside trusted Core composition/recovery code.
- DPAPI-at-rest protection, CAS semantics, browser checkpoint migration/quarantine, remote replay protection, exact cloud disclosure approval, mutation leases, private-data rejection, provider reconciliation/cancellation, and emergency-stop behavior are unchanged.
- The public `IAgentJobStore` contract intentionally remains available for least-authority composition/test doubles; this hardening blocks ordinary construction of NVIDEA's concrete path-backed mutator, not fully trusted reflection or arbitrary direct filesystem tampering inside the same user account.
- Narrowing the constructor could expose compile-time dependencies in another NVIDEA executable only if an uninspected external project constructs this exact concrete type; inspected Worker and desktop/Core composition do not. A real .NET build remains required to prove the entire solution boundary.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/API-surface changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, audit the remaining path-backed mutable persistence/provider constructors (especially browser goal/session and protected transport primitives) and narrow only concrete construction that can bypass product capability/lifecycle authorities; preserve public protocol abstractions required by the worker, tests, contract tooling, and future Nebius cloud composition.
