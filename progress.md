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
- Provider/model/site/tool text is non-authoritative across explicit trust boundaries: `ProviderFailureCodeTrust`, `JobFailureDiagnostic`, `DesktopUiFailureProjector`, `DesktopDisplayTextTrust`, `BrowserProductOutcomeTrust`, and `BrowserGoalEvidenceTrust`.
- Parent browser-goal state no longer retains duplicate `PendingAction`; recovery authority remains the durable child job id plus exact approval scope.
- Browser child jobs now scrub executable browser-action checkpoint payloads after safely terminal **Failed** or non-in-flight **Cancelled** outcomes. Retryable jobs retain the checkpoint only while needed for retry/recovery.
- Browser execution now has an explicit `AmbiguousJobExecutionException` contract. If the driver reports that a side effect executed but post-action verification is inconclusive, the orchestrator leaves the job durable `Running`, blocks automatic retry, preserves its checkpoint for fresh verification, and emits only fixed local diagnostic/audit text.
- In-flight browser cancellation is also fail-closed: the durable `Running` checkpoint is preserved for fresh verification instead of being scrubbed/replayed. Explicit cancellation of an already-`Running` browser job becomes terminal but retains that checkpoint solely as ambiguous-side-effect evidence; pending/approval-waiting cancellation is scrubbed.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product/evaluator/protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role separation, protocol-level envelope trust, and deployment/preflight policy reuse.

### 2026-09-13 — Provider, desktop, and browser trust boundaries
Added structured provider-failure provenance, bounded remediation allowlists, generic failure-diagnostic quarantine, privacy-safe desktop exception/display projection, constrained browser product outcomes, bounded/privacy-reduced browser-goal evidence, safe legacy goal-state migration ordering, and same-process planner-history projection.

### 2026-09-13 — Parent browser pending-action minimization
- Audited every meaningful `BrowserGoalSession.PendingAction` use.
- Confirmed parent crash recovery/approval resume needs `PendingJobId` + exact approval scope, not a duplicate action object.
- `BrowserGoalEvidenceTrust.ProjectForPersistence(...)` now clears `PendingAction` in durable and same-process/no-store state while preserving child id, exact scope, lifecycle, budgets, history, and approval semantics.
- Added adversarial projection coverage for typed secrets, credential-bearing URLs, private paths, rationale, and postcondition values.

### 2026-09-13 — Child browser checkpoint lifetime + exact-once hardening
Completed in this run:
- Re-read this ledger and audited browser checkpoint migration, handler, orchestrator, job contracts, and focused job tests before mutation.
- Found that terminal failed/cancelled browser child records could retain the full executable pending checkpoint containing typed values, upload/file paths, destinations, locator material, expected state, rationale, and postconditions.
- Added `BrowserActionTerminalCheckpoint`: safely terminal browser jobs replace executable checkpoint data with fixed tombstone `browser.action.terminal.scrubbed` / `{"actionPayloadRemoved":true}` and clear obsolete approval scope. Non-browser jobs are unaffected.
- Applied scrubbing **before persistence and return** for exhausted browser failures and safely cancellable non-in-flight browser jobs. `RetryScheduled` deliberately keeps the checkpoint because the next attempt still needs it.
- Added regressions proving exhausted failure is scrubbed in-memory and durably, retry retains its exact checkpoint, and approval-paused cancellation scrubs checkpoint + scope.
- Found a more severe no-replay bug during the audit: `BrowserActionJobHandler` used an ordinary exception when the driver reported success but verification failed, allowing the generic orchestrator to retry a side effect that may already have happened.
- Added public handler contract `AmbiguousJobExecutionException`. `BrowserActionJobHandler` raises it only for driver-reported execution with inconclusive postcondition verification and no longer copies raw verification detail into that signal.
- `ResumableJobOrchestrator` catches ambiguous execution separately, keeps state `Running`, preserves the checkpoint, clears retry scheduling, writes only fixed local diagnostic/audit text, and returns the fail-closed state. A repeated `RunNextStepAsync` returns that `Running` job without invoking the handler again.
- Added regression proving ambiguous execution invokes the handler once, remains `Running`, preserves recovery checkpoint material, and is never auto-retried.
- Tightened cancellation discovered during review: cancellation arriving while a browser handler is in-flight remains `Running` with fixed ambiguity text and preserved checkpoint. Explicit cancellation of a pre-existing `Running` browser job does not scrub its checkpoint; safely terminal pending/approval-waiting cancellation still does.
- Memory, research, Nebius remote execution, non-browser retry behavior, exact approval equality, and verified browser completion were not intentionally changed.

Engineering commits this run before this ledger update:
- `60d4a67f8179f3a615a8c8d67f97c784634bfbcd` — terminal browser checkpoint scrubber.
- `6aa61e0b935aaaafe505cdf08e00f1523e966599` — initial orchestrator terminal scrubbing.
- `ea525d6910500c333f5052e021b096a8657102a3`, `d704b370f725a8e2c40e3bc0bbf9275efe746113` — focused terminal checkpoint regressions/refinement.
- `9c729943d18ecf282a7779668fedb6ccf7df64c1`, `5c0a5faea2d94543020b56f8faf83fa3af7ff2ff` — ambiguous-execution handler contract and public testable API.
- `9235f677c93d307ab098d3d01d8df8c088559033` — executed-but-unverified browser actions no longer use the ordinary retry path.
- `04920289c1924bfeb3a6ca6bccd8f48f2421e330` — orchestrator fail-closed ambiguous execution handling.
- `92203d469c5216a854426fc8b551fea6cbe656ec` — no-auto-retry ambiguity regression.
- `906ed8c513e714eaff3ac5d02c2000c804462b62` — preserve ambiguous in-flight cancellation evidence while retaining safe terminal scrubbing.

Validation / evidence this run:
- Verified immediately before every successful GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- Starting repository head was `e5b4bc531eb073a22d54ebb6052ef3bea80f1a1a`.
- GitHub compare before this ledger commit reports **10 commits ahead / 0 behind**, touching exactly five engineering/test files: `AmbiguousJobExecutionException.cs`, `BrowserActionJobHandler.cs`, `BrowserActionTerminalCheckpoint.cs`, `ResumableJobOrchestrator.cs`, and `BrowserActionTerminalCheckpointTests.cs`.
- Static review confirms normal retryable failures still use `RetryScheduled`; only explicit executed-but-unverified ambiguity bypasses retry and remains durable `Running` for existing ambiguous-side-effect recovery.
- Environment check found no usable `dotnet`, `csc`, or `msbuild` executable.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.** New regressions are persisted but unexecuted here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Executable browser action data is retained only while pending/retry/recovery semantics require it; safe terminal failed/cancelled records use a fixed tombstone instead.
- Executed-but-unverified side effects can no longer enter automatic retry through the generic failure path, materially strengthening exact-once/no-replay behavior for submit/send/upload-style actions.
- Ambiguous execution/cancellation retains the checkpoint intentionally because fresh verification requires the intended action/postcondition; privacy minimization does not override side-effect safety.
- `AmbiguousJobExecutionException` text is not copied into durable/audit output; fixed local strings carry the state transition so handler/provider/site text does not gain UI or control authority.
- Exact approval comparison semantics and ephemeral single-use approval grants remain unchanged.
- Existing encrypted transport, signed binding, remote exact-once ingestion, browser download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this environment, so recent Core/WPF/Worker changes still require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Ambiguous `Running`/in-flight-cancelled browser records intentionally retain executable action material until recovery resolves the side effect. Cleanup must scrub it immediately after a trusted terminal reconciliation.
- Browser approval `ExactScope` is intentionally preserved byte-for-byte while authority is live. Generic job audit also receives approval scope; confirm browser scopes do not embed raw typed secrets/upload material and, if necessary, introduce a privacy-safe auditable fingerprint without weakening exact human-visible approval equality.
- `BrowserHostRuntime.Describe(...)` still retains raw `LastError` in its trusted internal outcome for goal/recovery infrastructure; public browser product callers and browser-goal presentation paths are constrained, but future trusted internal consumers must not render it directly.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real embedding ranking and real Nebius Object Storage/Serverless execution still require live environment validation.
- Lower-level `NebiusResearchClientRuntime.Create(...)` retains a same-key compatibility fallback for legacy unit/contract callers; production composition/preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent migrations. If executable validation remains unavailable, audit **browser exact approval-scope construction and audit retention** end-to-end: prove sensitive typed values/upload material are not copied into durable audit authority strings, introduce a stable privacy-safe scope fingerprint for audit if needed while preserving byte-for-byte live approval equality, and add restart/adversarial regressions.
