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

## Current Architecture / Product State
- .NET 8 core at `src/Nvidea.Core`; WPF host at `src/Nvidea.Windows`.
- Nebius/Nemotron inference abstraction with structured output/tools, retries, timeout/cancellation and conservative routing.
- Layered personal memory, Tavily research engine, Playwright browser agent, deterministic verification, prompt-injection/safety boundaries, resumable browser-goal sessions and durable jobs.
- Tavily research now uses both Search and query-focused Extract in the production research pipeline. Search results are canonicalized/deduplicated, then a bounded top-ranked subset is re-read through advanced `/extract`; extracted evidence replaces snippets only for matching URLs and total Tavily credits remain accounted.
- Tavily Extract is bounded to 8 sources per batch and 3 query-focused chunks per source by default; both are validated against Tavily's documented limits. Extract uses markdown, disables images/favicon, tracks usage, and inherits the existing bounded retry/timeout/cancellation path.
- Extract failures are fail-soft for research quality: a whole Extract outage or per-source extraction failure retains the original Search evidence and emits warnings rather than discarding the research run. User cancellation still propagates.
- Research synthesis remains Nemotron-first and prompt-injection hardened: source text is explicitly untrusted, source IDs/canonical URLs are preserved, synthesis is told to prefer extracted source text, and referenced `[src:SOURCE_ID]` markers are machine-validated.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer, protected segmented audit trail and emergency-stop plumbing.
- Production browser runtime uses an NVIDEA-owned persistent Chromium profile with popup/new-tab tracking and durable download quarantine.
- Browser downloads use Receiving -> Ready/Interrupted with verified length/SHA-256 and DPAPI-protected metadata on Windows.
- Exact-scope download handoff/discard require short-lived single-use approvals; WPF never receives `ApprovalGrant`.
- Retained download quarantine defaults to 512 MiB total / 128 MiB per file; in-progress browser staging and quarantine `.partial` copies are bounded and oversized transfers are cancelled.
- Crash-leftover browser staging cleanup is bounded, top-level only and fail-closed around unexpected directories/reparse points.
- Protected segmented audit retention defaults to 32 archived segments / 64 MiB archived segment+seal bytes with crash-safe protected prune tombstones and exact pending-delete recovery.
- `BoundedSegmentedAuditTrail` enforces 64 KiB/event and 4 MiB/current-active-segment logical payload ceilings before append side effects, and production browser composition uses it.
- Privacy-safe audit retention status exposes retained counts/bytes, quotas and protected pruning evidence without audit payloads.
- `LocalStateRuntime` exposes browser-free read-only audit/download telemetry without browser actions, approval grants, audit append, export/discard, repair or delete methods.
- WPF `Audit status` and passive browser-download polling use `NvideaCompositionRoot.LocalState`, so simply rendering local state does not initialize Playwright/Chromium.
- Browser-download metadata mutation and passive snapshot reads share one same-path in-process synchronization gate.
- WPF surfaces an explicit **Download recovery needed** state for passive `Receiving` records. Recovery occurs only after deliberate user action and remains emergency-stop cancellable.
- `StateDirectoryLease` combines process-local ownership with OS-backed `FileStream.Lock(0, 1)` on `.nvidea-state.lock`; stale lock files are not ownership.
- Browser-state ownership is acquired by `BrowserHostRuntime.CreateAsync` before `Playwright.CreateAsync`; that exact lease is transferred once into `PersistentBrowserContextFactory.LaunchOwnedAsync` and retained through context lifetime. Direct factory callers remain intrinsically protected.
- Cross-process lease coverage includes independent child-process ownership, contention, abrupt process-tree death and stale lock-file reacquisition. No-browser coverage proves contention prevents Playwright factory invocation and transport-creation failure releases the lease.
- Root README + MIT license; README now documents Tavily Search + Extract as a judging-visible core dependency rather than future work.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily Search research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent browser and safe downloads
Added owned persistent Chromium profile/session state, popup/new-tab tracking, durable download quarantine, verified payload identity, exact-scope export/discard, single-use grants, WPF confirmation, crash-recoverable discard, retained-byte quotas, bounded in-progress staging, pre-launch stale-staging reclamation and deterministic opt-in Chromium cancellation fixture.

Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `5ed0e2753ad9d46610200303fc7b3418158d0d52`, `994234746644826d2f98a9ca3cd1974f85b1cce1`.

### 2026-09-08 — Bounded audit + browser-free telemetry
Added crash-safe archived audit retention, protected pruning tombstones/digests, per-event/active-segment ceilings, production bounded-audit composition, privacy-safe retention telemetry, browser-free read-only audit status/download snapshots, bounded metadata reads, trusted snapshot-to-action revalidation, and same-path audit/download synchronization.

Representative commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `58ad476cba7059220f9fd0129a086a2a661deb7c`, `6d547d218e03da473e552d69cfb02765a91407f2`, `d55c87a4333bfa1a48aa2c1fe87398371d4b180f`, `e09cdfe6c90db5e330abbc9b020a92ae642a72d7`, `44228a3ca42d3d06d9c0184a47a0d07efcdb56d9`, `537c3df7fdf10d46ccb7dde7b3bbed0615f4f32a`, `148962033b88a4b54b58346e7c9ee0b89ebfafbf`, `5a4fe4ab5660679bf24bad889cdfcd83473fe693`.

### 2026-09-08 — Trusted download recovery + single-owner browser state
Added deliberate **Recover safely** UX, trusted quarantine reconciliation only after user action, emergency-stop cancellation, `StateDirectoryLease`, same-state exclusivity/reacquisition tests, a real child-process contention/crash fixture, and intrinsic lease ownership at the persistent browser boundary.

Representative commits: `ac374dee484eb51b8c92e96b48b5b6d58c0200a9`, `1d8a164add9606af5ae5640eaad36ffbdd2499db`, `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `97cb35651728f91cb7be6d9fda47e80e259bd92e`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `9a2afc79fbcc09ad89f47f5e30be187b5b47f51e`.

### 2026-09-08 — Pre-Playwright durable-state ownership
Moved state ownership ahead of Playwright transport startup, added explicit single-lease transfer into the persistent context factory, hardened validation/startup/cancellation cleanup, and added no-browser tests proving contention blocks Playwright factory invocation and transport failure releases ownership.

Representative commits: `b840a2220353d035eca1d1c13de6cb15b8875c85`, `79b9338dceb55470f775d5467fd8b5ad2461ea29`, `0a4658deb4d78e0d747663fe9f80b9e0a47dca4a`, `1e7438cb9bdd80b3b5d5bdda10971033af650947`, `bd96428dae7222d702429a42ecaf1e337a88bcfa`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily Extract evidence pipeline
Completed this run:
- Re-read `progress.md`, recent commits, full repo tree, `ResearchEngine`, `TavilyResearchClient`, existing Tavily tests, ResearchEngine tests and README before implementation.
- Verified current official Tavily API behavior before coding: `/extract` supports a URL/list of URLs, query-focused reranking, `chunks_per_source` 1-5, `extract_depth` basic/advanced, markdown/text format, `include_usage`, and batches up to 20 URLs. Current docs also confirm Bearer authentication and the `https://api.tavily.com` base URL.
- Added `IResearchExtractionProvider` so extraction is a clean research capability rather than being hard-wired into the generic search interface.
- Implemented `TavilyResearchClient.EnrichAsync`: selects a bounded top-ranked source subset, calls advanced Tavily Extract with the user's research intent for chunk reranking, canonicalizes response URLs, replaces matching search snippets with extracted content, preserves provenance/source IDs, and adds Extract credits to the batch usage total.
- Refactored Tavily HTTP calls through one bounded retry/timeout/authentication path so Search and Extract share the same transport and error policy.
- Added fail-soft behavior: provider/network/timeout failures return the original Search evidence with a warning; per-source failed extractions keep that source's snippet and emit a host-only warning; explicit user cancellation still propagates.
- Wired `ResearchEngine.ResearchAsync` to automatically enrich evidence whenever the provider implements `IResearchExtractionProvider`, before untrusted-evidence wrapping and Nemotron synthesis.
- Updated synthesis policy to prefer extracted source text over snippets while retaining exact `[src:SOURCE_ID]` citation validation and source-text-as-untrusted-data boundaries.
- Added `TavilyExtractEnrichmentTests` covering the official request shape, advanced/query-focused extraction, credit accounting, partial source failure, and full Extract outage fallback.
- Added `ResearchExtractionPipelineTests` proving enriched content, not the original snippet, is what reaches synthesis and that extraction is skipped when search returns no sources.
- Updated README so Tavily Extract is represented as implemented core architecture and judging-visible pipeline behavior rather than future work.
- Implementation/test/docs commits before this progress update: `9b45da151dedd946625962f485713a4543543cd6`, `cb521700f2edd588be5385b33c72f2275d7aea3e`, `b584a113452a091a9159b5ae3dd6581c2aa1a978`, `1c9ab04569f0c09bff7de07e9807a4b04223d933`, `fc2bc6c0f8fc23a8c3034553f902ce00fc23530e`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation.
- Current official Tavily Extract documentation was checked on 2026-09-09 before implementation; no stale API shape was guessed.
- The Extract implementation commit diff was re-read after mutation and confirmed to preserve the existing Search contract while adding a separate enrichment interface and shared transport path.
- `command -v dotnet`, `msbuild`, `csc`, and `mcs` still returned no executable in this environment, so compilation/test execution is not claimed.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- No other repository was mutated.

Security / privacy / cost review:
- Extracted web content remains untrusted evidence and cannot become instructions merely because it is deeper source text.
- No API keys, credentials or browser/session state are added to persisted research objects.
- Failure warnings use only the failed source host rather than Tavily's arbitrary remote error string, reducing accidental untrusted-data propagation into UI/logs.
- Query-focused extraction sends the user's research question plus selected public result URLs to Tavily; this is appropriate for the web-research subsystem but should remain disclosed as cloud research data flow.
- Extraction is bounded by source count and chunk count, images/favicon are disabled, and Tavily-reported Extract credits are accumulated into `ProviderCreditsUsed` for cost observability.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new Tavily Extract and research-pipeline tests are implemented but unexecuted here. Verify .NET serialization of the current request DTOs and live Tavily response compatibility with a real `TAVILY_API_KEY` before the demo.
- The browser lease tests, real child-process fixture, recovery UX, oversized-download fixture and Windows staging/reparse behavior also remain unexecuted here.
- Research source `PublishedAt` is still not populated from provider evidence, so freshness scoring/stale-info warnings are not yet strong enough for a competition-grade current-events demo.
- Source ranking still primarily uses Tavily provider score; richer deterministic authority/freshness/diversity scoring is absent.
- Research jobs are not yet packaged as a durable resumable research workflow with visible checkpoint/source evidence for the final demo.
- Passive browser snapshots intentionally verify retained payload length, not SHA-256, on every four-second poll; trusted export/discard performs full hash verification before consequential mutation.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- A verified production embedding adapter remains absent.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, strengthen the Tavily research path further with deterministic source authority/freshness/diversity scoring and stale-information warnings while preserving provenance, then add a durable resumable research job/checkpoint surface suitable for the <=3 minute demo.
