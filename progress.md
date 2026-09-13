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
- Browser-goal descriptive evidence crosses `BrowserGoalEvidenceTrust`: detail/history are bounded and privacy-reduced, and projected parent-goal state never retains `PendingAction`; recovery authority remains the durable child job id plus exact approval scope.
- `BrowserGoalAgent` applies goal evidence projection in-process as well as at durable save/load boundaries, so immediate callers and subsequent planner turns do not get a more permissive evidence surface than restart/reload paths.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product/evaluator/protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role separation, protocol-level envelope trust, and deployment/preflight policy reuse.

### 2026-09-13 — Provider, desktop, and browser trust boundaries
- Added fail-closed Serverless lifecycle coverage, structured bounded provider failure provenance, `ProviderFailureCodeTrust`, and fixed local remediation only.
- Added `JobFailureDiagnostic`, `DesktopUiFailureProjector`, and `DesktopDisplayTextTrust`; provider/tool/runtime exception and display text cannot become UI/control authority.
- Added `BrowserProductOutcomeTrust` for constrained Core product/plugin outcomes.
- Added `BrowserGoalEvidenceTrust`: bounded/control-normalized goal detail and verification evidence, privacy-reduced verified HTTP(S) URLs, fail-closed non-web evidence, and unchanged exact approval scope/job/lifecycle authority.
- Applied goal projection at durable save/load and inside `BrowserGoalAgent`; subsequent planner turns and same-process/no-store returns now use the same constrained evidence surface.
- Hardened legacy plaintext→protected goal-state migration ordering and added adversarial evidence tests.

### 2026-09-13 — Browser pending-action data minimization
Completed in this run:
- Re-read this ledger completely, inspected recent commits and current browser-goal agent/store/contracts/tests, and audited every meaningful `PendingAction` use before mutation.
- Confirmed the full action object is not required for parent-goal crash recovery or approval resume. Missing reserved children are safely re-planned; existing children are reconciled by `PendingJobId`; lost approval is re-armed and explicit approval is resumed by `PendingJobId` + byte-for-byte `PendingExactScope`; the durable child job remains the source of truth for executable action data.
- Confirmed `BrowserAction` may contain sensitive typed values, upload/file paths or destinations, locator material, rationale, expected state, and typed postconditions. Keeping that object in parent-goal same-process state after child reservation therefore violates data minimization even though `JsonBrowserGoalSessionStore` already omitted it from disk.
- Updated `BrowserGoalEvidenceTrust.ProjectForPersistence(...)` to always set `PendingAction = null`. Because `BrowserGoalAgent.PersistAsync(...)` projects before save **and return**, this now clears sensitive action payloads in same-process/no-store goal state as well as durable save/load and legacy/corrupt record normalization.
- Deliberately retained the nullable `PendingAction` property on `BrowserGoalSession` for source/serialization compatibility; the trust projection strips its value rather than forcing a risky schema migration.
- Preserved `PendingJobId`, `PendingExactScope`, status, budgets, verified history, action counts, and approval comparison semantics. The minimization cannot grant, recreate, replay, navigate, submit, cancel, or otherwise execute an action.
- Added an adversarial projection regression with a password-like typed value, credential-bearing/query/fragment URL, private expected state/rationale, and secret-bearing postcondition; the projected parent retains the job id and exact scope but drops the entire action object.
- Strengthened the no-store waiting-approval regression to assert `PendingAction` is already null on the immediate returned session.
- Existing crash-consistency tests already exercise restart/rearm/reconcile paths using `PendingJobId` and `PendingExactScope` without a parent `PendingAction`, supporting the architectural conclusion that parent recovery does not require the duplicate action payload.

Engineering commits this run before this ledger update:
- `e5be22defb37f74ef2a76e90f97459d3deb2e919` — minimize projected browser pending-action data.
- `cc882e04bf220286bc0f67a7fd88a214e2a6508b` — assert immediate/no-store pending-action minimization.
- `1574d9f0581138971fdb427a8f1c3221a835f8c2` — add adversarial sensitive-action projection coverage.

Validation / evidence this run:
- Verified immediately before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- Starting repository head was `221a8a6183163b7fcd5482178ef0083cdc411c2e`.
- GitHub compare through `1574d9f0...` reports **3 commits ahead / 0 behind**, touching exactly `BrowserGoalEvidenceTrust.cs` and two focused browser-goal trust test files.
- Static review confirms the executable child job remains responsible for the actual action; parent minimization changes descriptive/recovery state only and does not alter exact approval authority or child execution semantics.
- Environment check reports no usable `dotnet` or `csc` executable.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.** The new tests are persisted but remain unexecuted here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider/model/site/tool strings remain non-authoritative; lifecycle and approval control flow depend on local enums, exact scopes, durable ids, and fixed policy.
- Browser-goal evidence sanitation applies consistently to durable records, restart/reload, immediate returns, and planner-history reuse.
- Parent browser-goal state no longer retains duplicate `BrowserAction` payloads after projection, reducing lifetime of typed text, upload/file material, destinations, rationale, and postcondition values.
- Exact approval scope remains byte-for-byte outside evidence minimization because the user-visible authority must equal the authority consumed by approval logic.
- Crash recovery still fails closed for ambiguous in-flight children and does not recreate side effects from parent descriptive state.
- Existing encrypted transport, signed binding, cancellation/recovery, exact-once ingestion, browser safety, download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this environment, so recent .NET/WPF/Worker changes require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- `BrowserHostRuntime.Describe(...)` still retains raw `LastError` in its trusted internal outcome for goal/recovery infrastructure; public browser product callers and browser-goal presentation paths are constrained, but future trusted internal consumers must not render it directly.
- The parent `BrowserGoalSession.PendingAction` duplication is now minimized, but the **durable child browser job/checkpoint must necessarily retain executable action data while work is pending or being verified**. Typed values/upload paths may therefore still exist in protected child state longer than needed after terminal outcomes unless terminal cleanup/scrubbing is explicit; this is the next browser privacy boundary to audit.
- Browser approval `ExactScope` may legitimately encode target-specific authority and is intentionally preserved verbatim. Any future redesign must keep the human-visible authority equal to the authority consumed by approval logic.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real embedding ranking and real Nebius Object Storage/Serverless execution still require live environment validation.
- Lower-level `NebiusResearchClientRuntime.Create(...)` retains a same-key compatibility fallback for legacy unit/contract callers; production composition/preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent migrations. If executable validation remains unavailable, audit **durable browser child-job/checkpoint sensitive-data lifetime and terminal cleanup**: identify exactly which action fields must survive pending/execution/verification, scrub typed values/upload paths and other sensitive payloads promptly after completed/cancelled/failed terminal states where safe, preserve exact-once/no-replay/recovery semantics, and add crash/restart/checkpoint-migration regressions proving sensitive action material is not retained longer than necessary.
