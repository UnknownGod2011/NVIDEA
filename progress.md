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
- Tavily research uses Search plus query-focused advanced Extract. Search results are canonicalized/deduplicated, then a bounded top subset is re-read through `/extract`; extracted evidence replaces snippets only for matching URLs and total Tavily credits remain accounted.
- Tavily Extract defaults to at most 8 sources and 3 focused chunks/source, uses markdown, disables images/favicon, and shares bounded retry/timeout/cancellation behavior. Extract failures retain Search evidence with explicit warnings; user cancellation still propagates.
- Research now has a deterministic evidence-quality layer before Nemotron synthesis. `ResearchEvidenceRanker` combines provider relevance (60%), conservative host-authority heuristics (22%) and freshness evidence (18%), then applies a bounded same-host diversity penalty during ordering.
- Freshness is not fabricated. Existing `PublishedAt` timestamps are scored when available; otherwise an explicit query `StartDate` is treated only as a bounded search-window freshness signal. News evidence with neither timestamp nor bounded window receives an explicit unverified-freshness warning. News evidence with a known publication timestamp older than 30 days receives a stale warning.
- Current Tavily Search docs were checked before this scoring work: Tavily documents `start_date`/`end_date` as filters based on publish date or last-updated date, while the documented result object exposes relevance score but no publication timestamp. NVIDEA therefore does not invent a `PublishedAt` value from Search results.
- Authority scoring is intentionally syntactic and heuristic, not a trust oracle: `.gov`/`.mil`/`.int`, country-code government/military suffixes, `.edu`/country-code education/academic suffixes, and documentation-like subdomains receive modest boosts. A deceptive subdomain such as `gov.example.com` remains default-web authority.
- Evidence ranking preserves exact source IDs, canonical URLs and citation provenance. Locally computed quality metadata is passed to Nemotron separately and explicitly labeled as heuristic rather than proof of truth.
- Research synthesis remains Nemotron-first and prompt-injection hardened: source text is untrusted data, extracted text is preferred over snippets, quality metadata cannot redefine policy, conflicts/insufficient evidence must be surfaced, and `[src:SOURCE_ID]` markers are machine-validated.
- Durable research jobs now checkpoint versioned request -> plan -> prepared evidence -> completed report stages. The prepared-evidence checkpoint contains ranked source/citation provenance, deterministic quality metadata, warnings and cumulative Tavily credit usage, allowing synthesis to resume without re-running Search/Extract or re-spending Tavily credits.
- Research checkpoints are bounded to 2 MiB UTF-8 and reject malformed/oversized payloads before remote work.
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
- Root README + MIT license; README documents Tavily Search + Extract + deterministic evidence-quality ranking as judging-visible core architecture.

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
Added `IResearchExtractionProvider`, production advanced Tavily Extract enrichment, shared bounded Search/Extract transport, credit accounting, fail-soft partial/full extraction handling, synthesis integration, extraction contract tests and README/judging documentation.

Representative commits: `9b45da151dedd946625962f485713a4543543cd6`, `cb521700f2edd588be5385b33c72f2275d7aea3e`, `b584a113452a091a9159b5ae3dd6581c2aa1a978`, `1c9ab04569f0c09bff7de07e9807a4b04223d933`, `fc2bc6c0f8fc23a8c3034553f902ce00fc23530e`, `c95a4f5405c0745e5e0cf1acf683d5efbdad6f02`.

### 2026-09-09 — Deterministic evidence authority/freshness/diversity ranking
Completed this run:
- Re-read `progress.md`, recent commits, the research implementation, existing research tests and README before changing code.
- Re-verified current official Tavily Search documentation. The docs state that Search results are ranked by relevance; `news` is intended for real-time updates; `start_date`/`end_date` filter by publish date or last-updated date; and the documented result object does not expose a publication timestamp. This is why the implementation preserves `PublishedAt = null` for current Tavily Search results rather than inferring one.
- Added `ResearchEvidenceQuality`, `ResearchEvidenceRanking` and `ResearchEvidenceRanker` in `src/Nvidea.Core/Research/ResearchEvidenceQuality.cs`.
- Added deterministic composite ranking: 60% provider relevance, 22% conservative syntactic authority, 18% freshness evidence. Ranking then applies a bounded repeated-host penalty of 0.09 per earlier result from the same host, capped at 0.18, so one domain is less likely to monopolize the top evidence set.
- Added provenance-safe freshness bases: known `PublishedAt`, bounded requested Search window, or Unknown. News sources without a timestamp or bounded date window emit an explicit uncertainty warning; known news evidence older than 30 days emits a stale warning.
- Added an independent-corroboration warning when at least three sources come from only one host.
- Added conservative authority tiers for government/international, academic/institutional, documentation-like, and default web hosts. During review, caught an unsafe first version that would have boosted any hostname containing a `gov`/`edu` label; corrected it so `gov.example.com` does not receive government authority. Country-code institutional recognition now requires the institutional label immediately before a two-letter country-code suffix.
- Kept quality metadata separate from `ResearchSource`/`ResearchCitation` contracts instead of silently rewriting provenance fields. The ranker reorders existing sources/citations and returns a source-ID keyed quality map.
- Wired `ResearchEngine` to rank evidence after Tavily Extract but before untrusted-evidence construction/Nemotron synthesis.
- Added a deterministic quality metadata block keyed to exact source IDs. Nemotron is explicitly told these are heuristics rather than proof of truth and that unknown/stale warnings must be respected.
- Added `ResearchEvidenceRankerTests` covering authority preference, deceptive-subdomain rejection, bounded search-window freshness without inventing publish time, stale/unknown news warnings, host diversity ordering and citation-order preservation.
- Updated `ResearchEngineTests` to prove quality metadata and the untrusted evidence boundary both reach the synthesis request.
- Updated README so this scoring layer is judging-visible and no longer listed as future work.
- Implementation/test/docs commits before this progress update: `206515fafd9a0ac27314711a5f4e99bb301ce5ed` (initial quality-layer draft, immediately superseded by the provenance-compatible correction), `9451f69ada117196668d9051f314b8df4e72c0a6`, `9c7fcd4547f28367297a006ccdde2e753b249be5`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `bdccdba2fe5ec605b918dbeb1e6f30741b751c94`, `eaf6ba6ffecb8006cc64ec193a07ac53795cbbc0`, `36d41fcf65835cb28a2d373d68c460371ead1c43`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation.
- Official Tavily Search docs were checked on 2026-09-09 before implementing freshness logic; the code does not claim a response timestamp Tavily does not document.
- The `ResearchEngine` integration diff was re-read after mutation to confirm ranking occurs after Extract, quality metadata is distinct from source text, and exact citation validation remains unchanged.
- Tests were added at both the deterministic ranker layer and ResearchEngine synthesis-input layer.
- `command -v dotnet`, `msbuild`, `csc`, and `mcs` returned no executable in this runtime, so compilation/test execution is not claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- No other repository was mutated.

Security / privacy / cost review:
- Evidence-quality scoring is deterministic local computation: it adds no external API call, token cost or new cloud data disclosure.
- Source content remains untrusted web data. Quality metadata contains only source IDs and numeric/enumerated local scores, not copied web instructions.
- Authority is explicitly heuristic and deliberately weighted below provider relevance. The implementation avoids claiming that a hostname suffix proves factual correctness.
- Freshness warnings fail toward uncertainty rather than silently declaring undated current-event evidence fresh.
- Tavily date windows retain their documented semantics but do not become invented publication timestamps in NVIDEA provenance.
- No secrets, browser credentials, local file contents or personal-memory data are added by this layer.

### 2026-09-09 — Durable resumable Nemotron + Tavily research jobs
Completed this run:
- Re-read `progress.md`, recent commits, `ResearchEngine`, job contracts/orchestrator, Tavily contracts and existing tests before modifying code.
- Split `ResearchEngine` at durable remote-work boundaries. `PlanAsync` remains Nemotron planning; `GatherEvidenceAsync` performs Tavily Search, optional Tavily Extract and deterministic rank/quality preparation; `SynthesizeAsync` consumes only already-prepared evidence and performs no Tavily call or re-ranking.
- Added `ResearchPreparedEvidence` so the ranked `ResearchBatch` and source-ID keyed deterministic quality metadata can be checkpointed together without recomputing diversity ordering after restart.
- Added `ResearchJobHandler` with versioned `research.requested.v1`, `research.planned.v1`, `research.evidence.v1`, and `research.completed.v1` checkpoints.
- The evidence checkpoint persists the actual source/citation provenance, extracted evidence, warnings, Tavily `ProviderCreditsUsed`, and deterministic authority/freshness/diversity quality metadata before final Nemotron synthesis.
- Added a 2 MiB UTF-8 ceiling to every research checkpoint serialization/deserialization boundary. Oversized or missing payloads fail before remote work instead of becoming unbounded durable state.
- Added `ReadCompletedReport` for a trusted caller to recover the final report from a completed durable job without invoking providers again.
- Added `ResearchJobHandlerTests` covering stage transitions, persisted credit/quality metadata, evidence-resume behavior with a provider that throws if called, and oversized-checkpoint rejection before any network/provider call.
- During review, caught a subtle initial design issue where `GatherEvidenceAsync` returned an already-ranked batch but `SynthesizeAsync` reranked it, which could alter diversity penalties/order after resume. Replaced that with explicit `ResearchPreparedEvidence`; synthesis now preserves the exact checkpointed ranking and quality decisions.
- Implementation/test commits before this progress update: `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `5affb9d37266b3d137fe2adb4c84dc367bdfcadb`, `a536f9ab9af8f09db74d5752a3147ce1e8dfe557`, `d22608d4bfd1b5ee38cf53dd400894ffb736f1c0`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` immediately before every mutation.
- Existing research and durable-job contracts were re-read before implementation; no other repository was mutated.
- Static call-flow review confirms the `research.evidence.v1` resume path calls only `ResearchEngine.SynthesizeAsync`; it has no provider/Tavily invocation path.
- Regression tests use a `ThrowingProvider` on resume so any accidental Search call fails the test, and assert Tavily credit plus quality metadata are present in the durable evidence checkpoint.
- This environment still does not provide a usable .NET SDK/compiler signal, so compilation and test execution are not claimed. GitHub Actions was not triggered merely to manufacture a green result.

Security / privacy / cost review:
- Research checkpoints can contain web evidence and the original research question; they intentionally contain no API keys, approval grants, browser credentials, or OS-private context.
- The 2 MiB ceiling bounds local durable-state growth and JSON parsing exposure. A future production store should additionally encrypt these research checkpoints at rest when they may include sensitive user questions.
- Resume after the evidence checkpoint avoids duplicate Tavily Search/Extract charges and preserves exact source/citation/quality provenance.
- Research remains side-effect-free with no consequential-action approval bypass introduced by this handler.
- Untrusted web evidence is still passed to Nemotron through the existing explicit untrusted-evidence boundary; checkpointing does not promote source text into trusted instructions.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The Tavily Extract tests, evidence-ranker tests, ResearchEngine staged API changes, and new `ResearchJobHandlerTests` are implemented but unexecuted here. Verify .NET compilation and run the test suite before relying on the demo path.
- Live Tavily Search/Extract compatibility still needs a controlled test with a real `TAVILY_API_KEY`; in particular, current production Search still has no provider-populated `PublishedAt` because the documented result shape lacks that field.
- Durable research checkpoints currently rely on whichever `IAgentJobStore` is composed by the host; the existing JSON store does not yet add DPAPI/application-level protection specifically for research payloads.
- The durable research handler is not yet wired into the Windows composition root/UI, so the final demo cannot yet start, display, interrupt and resume these research stages visibly.
- Authority scoring is intentionally a bounded syntactic heuristic, not registrable-domain/Public-Suffix-List validation or a curated source reputation database. It should never be surfaced as a binary trust verdict.
- Search-window freshness depends on research providers honoring the `ResearchQuery` date contract. Production Tavily does send those bounds and current Tavily docs define their publish/update-date semantics; future provider adapters must preserve that contract.
- The browser lease tests, real child-process fixture, recovery UX, oversized-download fixture and Windows staging/reparse behavior also remain unexecuted here.
- Passive browser snapshots intentionally verify retained payload length, not SHA-256, on every four-second poll; trusted export/discard performs full hash verification before consequential mutation.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- A verified production embedding adapter remains absent.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, wire `ResearchJobHandler` into the production composition root and Windows UX with a privacy-safe stage/status surface, explicit cancel/resume controls, and protected local checkpoint storage so the <=3 minute demo visibly proves interruption-safe Nemotron + Tavily research.