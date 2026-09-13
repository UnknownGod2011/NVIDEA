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
- .NET 8 core in `src/Nvidea.Core`, WPF Windows host in `src/Nvidea.Windows`, deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with retries, cancellation/timeouts, structured tool calling, response-schema support, and Nano/Super/Ultra routing.
- Layered personal memory with privacy-aware writes, provenance, semantic/recency/importance retrieval, local Ollama embeddings, migration/re-indexing, and maintenance UX.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, evidence quality/freshness/diversity ranking, citations/provenance, resumable checkpoints, and explicit untrusted-evidence handling.
- Safe Playwright browser agent with persistent Chromium state, popup tracking, plan-act-observe-verify, prompt-injection detection, consequential-action approvals, quarantined downloads, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default; job state uses durable CAS, leases, and hash-chained/segmented audit.
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, Nebius Object Storage, and Serverless-mounted worker transport.
- Worker envelope, dispatch-signing, and result-envelope RSA purposes are separated end-to-end with RSA >=2048, OAEP-SHA256 capability proofs, bounded PEM, canonicalization, and temporary-buffer zeroization.
- Nebius Serverless lifecycle interpretation is explicitly allowlisted. Bounded provider diagnostics are untrusted evidence only and never determine lifecycle transitions.
- Durable remote provenance contains structured `ProviderFailureCode`; `ProviderFailureCodeTrust` is the single evidence-shape boundary and only fixed local remediation codes can generate guidance.
- Generic provider/handler exception text crosses `JobFailureDiagnostic`; WPF exceptions cross `DesktopUiFailureProjector`; non-exception runtime display strings cross `DesktopDisplayTextTrust`.
- Browser product outcomes cross `BrowserProductOutcomeTrust`; failed/retry messages are local state-derived, approval presentation is bounded/privacy-reduced, and exact approval scope remains unchanged.
- Durable browser-goal descriptive evidence now crosses `BrowserGoalEvidenceTrust`: session/recovery detail and verified-step evidence are bounded/control-normalized; verified HTTP(S) URLs drop user-info/query/fragment; unexpected schemes fail closed to `about:blank`; approval scope/job/state/replay authority are not modified.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product/evaluator/protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role separation, protocol-level envelope trust, and deployment/preflight policy reuse.

### 2026-09-13 — Provider diagnostic and desktop trust boundaries
- Added exhaustive fail-closed Serverless lifecycle coverage and bounded `state_details` evidence.
- Added `ProviderFailureCodeTrust`, structured remote failure provenance, fixed `NotEnoughResources`/`Quota` remediation only, durable migration, CAS equivalence, direct reconciler capture, and parser→reconciler→store coverage.
- Added `JobFailureDiagnostic`; arbitrary exception text is bounded/marked untrusted and excluded from append-only audit summaries.
- Added `DesktopUiFailureProjector` and `DesktopDisplayTextTrust`; direct exception/runtime text leakage was removed from product-facing WPF surfaces and URL presentation strips user-info/query/fragment.
- Added `BrowserProductOutcomeTrust` in Core so plugin/product browser callers receive constrained outcomes even outside WPF.

### 2026-09-13 — Browser goal durable evidence trust boundary
Completed in this run:
- Re-read this ledger and inspected `BrowserGoalAgent`, `BrowserGoalSessionStore`, browser product/runtime contracts, display-text trust, and relevant test patterns before mutation.
- Confirmed that `BrowserGoalSession.Detail`, `BrowserGoalVerifiedStep.VerificationDetail`, and verified step URLs can originate from model/site/tool/runtime evidence and are persisted/reused as planner context.
- Added `BrowserGoalEvidenceTrust` with separate 512-character session-detail and 320-character verification-detail bounds; controls/whitespace are normalized through the existing presentation primitive.
- Privacy-reduced verified HTTP(S) evidence URLs to scheme + host + port + path, removing user-info/query/fragment credentials/tokens. Unexpected/non-web schemes fail closed to `about:blank` because verified history is descriptive evidence, not navigation authority.
- Preserved goal identity, job id, action kind, timestamps, lifecycle state, `PendingJobId`, and `PendingExactScope`; the projection cannot approve, retry, replay, navigate, or mutate an action.
- Applied the projection before `JsonBrowserGoalSessionStore` persistence and again on load so legacy/corrupt records cannot bypass the current trust boundary.
- Hardened plaintext→protected legacy migration ordering: records are normalized before re-persistence instead of merely encrypting old tainted descriptive fields unchanged.
- Added adversarial tests covering oversized/control-character model text, secret-bearing URLs, non-HTTP schemes, missing optional evidence, and exact approval-scope/job/state preservation.

Engineering commits this run before this ledger update:
- `5d8c560dda40d1ddf7ec2c927adcfd9bd18e60f0` — add durable browser-goal evidence trust boundary.
- `9b9a73a686047c1ede3d91e3f74148705a0c7cb3` — apply browser-goal evidence projection at save/load boundaries.
- `006f538d47898422472de4d60444af0e68deb962` — add adversarial browser-goal evidence trust tests.
- `4c348767e365ff5db734e53905850c94b18a143c` — sanitize legacy goal records before local-state migration.

Validation / evidence this run:
- Verified before every GitHub mutation that the repository target was exactly `UnknownGod2011/NVIDEA`.
- Starting repository head was `b7900bedf64d197aff3cbfa9ac67342dacb573c4`.
- GitHub compare through `006f538d...` showed **3 commits ahead / 0 behind** before the final legacy-migration hardening and ledger commit.
- Static re-read confirms the trust projection is descriptive-only and leaves exact approval scope/lifecycle identifiers untouched.
- Local execution environment still has no `dotnet` or `csc` executable.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider/model/site/tool strings remain non-authoritative; lifecycle and approval control flow depend on local enums, exact scopes, durable ids, and fixed policy.
- Browser goal evidence sanitation cannot grant approval or replay authority and does not alter exact scope.
- Verified-history URLs no longer durably retain query/fragment/user-info secrets; unexpected schemes are discarded from descriptive history.
- Legacy goal records are normalized before local-state protection migration, reducing retention of historical tainted descriptive data.
- Existing encrypted transport, signed binding, cancellation/recovery, exact-once ingestion, browser safety, download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this environment, so recent .NET/WPF/Worker changes require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- `BrowserGoalAgent` can still return same-process in-memory `Detail`/`VerificationDetail` originating from planner/host output before a caller reloads through the durable store. Durable/restart planner context is now constrained, but an explicit goal product projection or in-agent sanitation should close the immediate-return surface too.
- `BrowserHostRuntime.Describe(...)` still retains raw `LastError` in its trusted internal outcome for goal/recovery infrastructure; public browser product callers are protected, but trusted internal consumers must not render it directly.
- Browser approval `ExactScope` may legitimately encode target-specific authority and is intentionally preserved verbatim. Any future redesign must keep the human-visible authority equal to the authority consumed by approval logic.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real embedding ranking and real Nebius Object Storage/Serverless execution still require live environment validation.
- Lower-level `NebiusResearchClientRuntime.Create(...)` retains a same-key compatibility fallback for legacy unit/contract callers; production composition/preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent migrations. If executable validation remains unavailable, close the remaining **same-process browser-goal presentation gap** by applying a dedicated product/in-agent projection to planner reasons, child outcome messages, verified-step details, and recovery evidence before `BrowserGoalAgent` returns them, while continuing to preserve exact approval scope and all control-flow authority; add adversarial contract tests proving untrusted strings cannot become approval/replay authority.
