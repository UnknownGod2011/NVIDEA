# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI edition inspired by keyboard.wtf for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest Windows interaction ideas while materially upgrading the intelligence/runtime into an NVIDIA/Nebius-first agent system with durable memory, research, complex browser automation, skills, permissioning, verification, and long-running execution.

Target track: **Personal AI**. Secondary prize target: **Best Use of Tavily**. Overall ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## HARD REPOSITORY BOUNDARY
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material. Never commit, edit, delete, open PRs/issues, change settings, rerun workflows, or otherwise mutate it.
- Never write to ANY other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not delete existing NVIDEA functionality merely to simplify implementation; migrate/refactor carefully and keep working paths unless a tested replacement exists.

## Required Work Loop
Every run must:
1. Read this file first.
2. Inspect current NVIDEA state and recent changes.
3. Choose the highest-value unfinished engineering task.
4. Verify current official platform/model/API assumptions when they can be stale.
5. Implement real code/config/tests/docs in NVIDEA; do not spend a run only planning when safe implementation is possible.
6. Validate as far as available tooling permits; never claim unverified behavior works.
7. Review correctness, privacy, permissions, prompt injection/tool trust, secrets, recovery/cancellation, UX, latency/cost and hackathon fit.
8. Update this file with completed work, evidence, unverified items, risks, blockers and the single best next task.
9. Continue improving on future runs while meaningful product/engineering work remains.

## Product gaps inherited from keyboard.wtf
- **Memory:** replace bounded key/value intent memory with layered working, episodic, semantic/profile, project/entity and skill/workflow memory, retrieval, provenance, sensitivity/retention and user controls.
- **Research:** Tavily-backed multi-query research with freshness/quality, source provenance, citations, explicit uncertainty and resumable tasks.
- **Browser automation:** DOM/accessibility-first plan-act-observe-verify agency with robust locators, recovery, permissions, prompt-injection defenses and audit evidence.

## Target Architecture
- **Desktop shell:** Windows hotkeys, voice/text, orb/status, active-app/selected-text/clipboard context, permission UX, local speech where useful and emergency stop.
- **Agent core:** task planner/state machine, structured tool calls, bounded iterative execution, verification, cancellation/retries and approvals.
- **NVIDIA/Nebius:** NVIDIA open models through Nebius Token Factory / AI Cloud as genuine core runtime; no hidden Gemini/OpenAI/Claude dependency.
- **Memory:** layered typed memory with privacy-aware writes, semantic/lexical retrieval, recency/importance, provenance/confidence/sensitivity/retention and deletion controls.
- **Research:** Nemotron planning -> Tavily evidence -> untrusted-content boundary -> Nemotron synthesis -> validated source IDs.
- **Skills / permissions:** capability registry, declared data/tool permissions, risk class, confirmation policy and evaluation fixtures.
- **Nebius execution:** cloud/serverless only where long-running background work benefits; private OS actions stay local.

## Hackathon product bar
Final <=3 minute demo should prove: invocation anywhere on Windows, context awareness, durable memory affecting later behavior, Tavily research with sources, complex browser work with verification, approval before consequential actions, meaningful background Nebius execution, and a concise architecture view proving Nemotron/Nebius/Tavily are core.

## Engineering priorities
1. Clean solution/package/build/test baseline.
2. Nebius Token Factory + NVIDIA Nemotron core backend.
3. Layered memory + retrieval + tests.
4. Tavily research + citations/provenance + tests.
5. Safe browser agent/runtime with observation/action/verification loop.
6. Skills + permission/risk engine + audit trail.
7. Resumable jobs + Nebius serverless where justified.
8. Windows desktop UX integration using useful keyboard.wtf patterns read-only.
9. Reliability/evals, cancellation/retries/offline/error states.
10. Security/privacy threat model and hardening.
11. Packaging/onboarding/demo environment.
12. Hackathon README, architecture diagrams, setup, attribution, significant-changes documentation, demo scenario and final rubric audit.

## Current State
- Standalone .NET 8 core at `src/Nvidea.Core`.
- NVIDIA/Nebius-first Token Factory inference client with verified default `nvidia/nemotron-3-super-120b-a12b`.
- Structured tools, structured output, cancellation/timeouts, bounded retries and endpoint validation.
- Layered privacy-aware personal memory under `src/Nvidea.Core/Memory`.
- Tavily research provider + Nemotron research engine under `src/Nvidea.Core/Research`.
- Contract tests under `tests/Nvidea.Core.Tests` for inference, memory and research.
- Root README + MIT LICENSE.
- keyboard.wtf has only been inspected read-only; no repository other than NVIDEA has been mutated.

## Progress Log
### 2026-09-06 — Initialization
- Established immutable repository-safety boundary.
- Captured keyboard.wtf strengths/gaps, target architecture and judging bar.

### 2026-09-06 — Nebius/Nemotron core foundation
- Verified current Nebius Token Factory OpenAI-compatible API, structured output/tool support and documented Nemotron 3 Super model ID.
- Added `NebiusTokenFactoryClient`, `IAgentInferenceClient`, conservative fast/standard/deep routing, bearer auth, structured tools/tool-call parsing, cancellation/timeouts, bounded retries and endpoint validation.
- Added contract tests for request shape, routing, auth, tool calls, retries, failures and endpoint safety.

### 2026-09-06 — Layered personal-memory foundation
- Added Working/Episodic/Semantic/Project/Skill memory layers with provenance, confidence, importance, sensitivity, retention, expiry and optional embeddings.
- Added replaceable embedding/store/write-policy boundaries.
- Added secret-detection/write policy that rejects credential-like material and requires approval for sensitive or indefinite memory.
- Added session-only memory, atomic JSON persistence, expiry cleanup, update-in-place, deletion and hybrid semantic/lexical/recency/importance retrieval.
- Added tests for privacy policy, retention, expiry, sensitive retrieval, semantic ranking, lexical fallback, duplicate prevention and persistence.
- Fixed a regex syntax risk and a retrieval relevance flaw during source review.

### 2026-09-06 — Tavily research + provenance foundation
Completed:
- Verified the current official Tavily Search contract directly from `docs.tavily.com`: `POST https://api.tavily.com/search`, bearer authentication, `general`/`news` topics, `advanced` search depth, date bounds, domain filters, result scores and optional usage-credit reporting.
- Added `src/Nvidea.Core/Research/TavilyResearch.cs`:
  - `IResearchProvider` abstraction and typed `ResearchQuery`, `ResearchSource`, `ResearchCitation`, `ResearchBatch` models;
  - strict `https://api.tavily.com/` endpoint validation and environment-based API key loading;
  - bounded batch size, query/result limits, request timeouts, cancellation and retries for timeout/429/5xx failures;
  - official bearer-auth request shape with `include_answer=false`, `include_raw_content=false`, `include_usage=true` so NVIDEA owns synthesis/provenance rather than trusting opaque provider answers;
  - canonical URL normalization that removes fragments, default ports and common tracking parameters and sorts remaining query parameters;
  - deduplication across multiple research queries by canonical URL while preserving the stronger provider-scored result;
  - source IDs, retrieval timestamps, provider scores, originating query and citation objects;
  - explicit `BuildUntrustedEvidenceBlock` security boundary that labels web text as data and forbids treating embedded commands/tool requests/credential requests/policy changes as instructions.
- Added `src/Nvidea.Core/Research/ResearchEngine.cs`:
  - structured Nemotron query planning through existing `IAgentInferenceClient` + JSON schema;
  - bounded 1-6 query plan validation, topic/date validation and conservative max-result bounds;
  - Tavily execution through provider abstraction;
  - synthesis through the existing Nemotron/Nebius inference abstraction rather than a second AI dependency;
  - prompt-injection boundary carried into synthesis;
  - required `[src:SOURCE_ID]` citation markers;
  - machine validation of cited source IDs against collected evidence, explicit warnings for unknown or missing markers;
  - no synthesis call when no evidence exists.
- Added `TavilyResearchClientTests.cs` covering bearer auth/request shape, date/topic fields, canonical deduplication, rate-limit retry, untrusted-content marking and unsafe endpoint rejection.
- Added `ResearchEngineTests.cs` covering plan->search->synthesis flow, verified citation selection, hallucinated source-ID warning, no-evidence short circuit and invalid date-range rejection.
- Updated README with live configuration, research security/provenance contract and implemented status.

Validation / evidence:
- Official Tavily Search documentation was checked on 2026-09-06 before implementation; no stale SDK assumptions were used.
- Performed manual source review after implementation and fixed two issues before closing the run: the structured-output schema initially contained an extra wrapper even though `NebiusTokenFactoryClient` already supplies the JSON-schema wrapper, and strict `DateOnly` parsing was changed to the explicit invariant-culture overload.
- Rechecked the execution container for `dotnet`, `csc` and `msbuild`; none are installed. Therefore the new code/tests remain source-reviewed but NOT compiled/executed in this automation environment. Do not describe the tests as green until a .NET-capable runner verifies them.

Unverified / risks:
- Real Tavily API integration still requires a live `TAVILY_API_KEY`; contract tests use mocked HTTP by design.
- Real Nemotron structured query planning/synthesis requires a live Nebius key and should be exercised before demo freeze.
- Tavily Search snippets currently provide the evidence body; Tavily Extract is not yet integrated for deeper page extraction.
- `PublishedAt` exists in provenance models but current Search response handling does not populate it. Freshness scoring should use verified provider metadata/extraction rather than guessing dates from text.
- Source quality currently retains Tavily relevance score but does not yet combine domain authority, source diversity, recency and corroboration.
- Citation validation confirms IDs exist but does not yet entailment-check every sentence against the cited source.
- Full compilation remains the highest immediate verification risk because the automation runtime lacks a .NET toolchain.
- Memory persistent storage is still a JSON baseline rather than Windows DPAPI/encrypted-at-rest storage.

Next highest-value task:
- Build the safe browser-agent foundation under `Nvidea.Core`: browser observation/action contracts, locator hierarchy, risk classification, immutable action/audit records, plan-act-observe-verify state machine, cancellation/retry/recovery semantics, high-impact approval gates, prompt-injection/trusted-instruction boundary, and mocked browser-driver tests. Prefer a provider-neutral DOM/accessibility contract first so the Windows shell can later use Playwright/extension/CDP without coupling core policy to one transport. Do not implement CAPTCHA/login bypasses or unsafe generic execution.
