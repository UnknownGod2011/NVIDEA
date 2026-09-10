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
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport publishes encrypted work items/bindings/results directly through Nebius Object Storage while the worker consumes the same bucket prefix through a Serverless-mounted directory.
- Deployment preflight enforces exact Object Storage ↔ Serverless mount alignment, READ_WRITE transport, MysteryBox-backed worker credentials, digest-pinned worker image, RSA strength/identity consistency, bounded compute/storage settings, and a redacted reproducible deployment fingerprint.
- `Nvidea.NebiusContractProbe` supports cheap planner, zero-cost live preflight, and explicit paid live research; redacted manifest/PASS evidence persistence and offline verification are fail-closed and reproducible.
- `ResearchCloudExecutionCoordinator` is the narrow provider-aware bridge from durable research state to `NebiusResearchClientRuntime`, with exact checkpoint/disclosure approval, shared mutation lease, provider reconciliation/cancellation, private-data rejection, and no provider credential exposure.
- `ResearchJobRuntime` is intentionally local-only and validates current durable location/provenance under the mutation lease before any local Nemotron/Tavily handler invocation.
- `ResearchProductRuntime` is the lifecycle-aware product facade: truthful local/remote status, local work only through `ILocalResearchRuntime`, remote lifecycle only through `IResearchCloudExecutionCoordinator`, and independently gated paid Serverless dispatch.
- WPF durable-research reads/actions use `ResearchProductRuntime`; remote/ambiguous records are never offered as local recovery. `ResearchProductUiState` centralizes control enablement, labels, and cloud disclosure.
- Browser product/UI access goes through constrained `BrowserProductRuntime`; the composition root does not expose raw `BrowserHostRuntime`, and WPF navigation/download flows use only the facade.
- Raw `BrowserHostRuntime` construction is now assembly-internal: the type remains compatible for trusted Core/test use, but there is no public instance constructor or public static factory that external product/plugin code can use to create the privileged host.

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
Added `BrowserProductRuntime`, replaced the public composition-root raw-host getter with `GetBrowserProductAsync()`, migrated WPF browser/download flows, and added reflection regression coverage that prevents raw `BrowserHostRuntime` leakage or accidental expansion of the product facade.

### 2026-09-10 — Current run: close raw browser-host construction authority
Completed:
- Re-read this ledger completely and inspected the current tree, recent commits, `BrowserHostRuntime`, `BrowserProductRuntime`, `BrowserGoalAgent`, `NvideaCompositionRoot`, and existing test-access configuration before changing code.
- Re-confirmed the remaining authority gap: despite the composition root no longer returning `BrowserHostRuntime`, external Core consumers could still call the public `BrowserHostRuntime.CreateAsync(...)` factory and obtain all privileged low-level durable-job, observation, re-arm and ambiguous-reconciliation operations.
- Changed the production `BrowserHostRuntime.CreateAsync(stateDirectory, options, cancellationToken)` factory from `public` to `internal`. The constructor was already private, so ordinary external callers now have no supported construction path for the privileged host.
- Kept the raw host type itself public for compatibility and minimized compile risk while no .NET execution signal is available. Trusted Core composition continues to create the host internally; tests retain access through `[assembly: InternalsVisibleTo("Nvidea.Core.Tests")]`.
- Updated the host documentation to state that construction is assembly-internal and that trusted Core composition owns Playwright creation.
- Added `tests/Nvidea.Core.Tests/BrowserHostConstructionApiSurfaceTests.cs`. Reflection asserts the host has no public instance constructor and no public static method returning `BrowserHostRuntime` (directly or through `Task<BrowserHostRuntime>`), so the privileged factory cannot silently reappear.
- Re-inspected `NvideaCompositionRoot.GetBrowserHostAsync()` after the change; it remains in the same Core assembly and still calls the internal factory, preserving existing product/goal/recovery functionality.

Commits this run:
- `eac797dadc8de89318c0dec730ce3320c5d01c58` — narrow raw browser host construction authority.
- `f713b5bcf98b126815162063da09b9dcd44e7104` — lock privileged host construction boundary with reflection regression coverage.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Current `BrowserHostRuntime` source was re-read before editing; its instance constructor was confirmed private and its public static production factory was the remaining normal construction path.
- Post-change static inspection confirms `NvideaCompositionRoot.GetBrowserHostAsync()` still creates the host internally, so `BrowserProductRuntime`, trusted goal-agent composition, and ambiguous-recovery composition retain the same underlying implementation.
- Post-change test source was re-read and explicitly guards both public constructors and public static factories returning the privileged host.
- The execution environment was checked again and has no usable `dotnet` binary, so Core compilation, WPF/XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- Product/plugin callers outside `Nvidea.Core` can no longer normally bootstrap a raw browser host and bypass the constrained `BrowserProductRuntime` surface.
- Existing exact approval, download quarantine, durable-job sequencing, state lease, audit, prompt-injection and emergency-stop behavior is unchanged; this run narrows construction authority rather than replacing execution logic.
- Test access remains intentionally assembly-scoped through `InternalsVisibleTo`, avoiding public production authority solely for testability.
- `BrowserGoalAgent` still has a public convenience constructor accepting `BrowserHostRuntime`. This no longer creates authority because external callers cannot construct a raw host through supported APIs, but the public signature still advertises trusted infrastructure and should be narrowed after compile compatibility is available or after a source-level caller audit.
- Reflection can bypass ordinary .NET accessibility in a fully trusted process; this boundary is an API/least-authority control, not a sandbox/security-process boundary.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.
- `BrowserGoalAgent` still publicly advertises a convenience constructor taking the now-nonconstructible privileged `BrowserHostRuntime`; this is API-surface debt rather than a current normal construction path.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, audit `BrowserGoalAgent` and other public Core signatures for privileged concrete runtime types and narrow the raw-host convenience constructor without reducing the safe `IBrowserGoalHost` abstraction or trusted composition-root goal/recovery paths.
