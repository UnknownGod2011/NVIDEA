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
- Provider/model/site/tool text is non-authoritative across explicit trust boundaries including `ProviderFailureCodeTrust`, `JobFailureDiagnostic`, `DesktopUiFailureProjector`, `DesktopDisplayTextTrust`, `BrowserProductOutcomeTrust`, `BrowserGoalEvidenceTrust`, and now `CapabilityIdentityTrust`.
- Parent browser-goal state no longer retains duplicate `PendingAction`; recovery authority remains durable child job id + exact approval scope.
- Browser child jobs scrub executable browser-action checkpoints after safely terminal failed/non-in-flight-cancelled outcomes; retryable and ambiguous-running jobs retain only material still required for retry/recovery verification.
- Executed-but-unverified browser actions use `AmbiguousJobExecutionException`; orchestrator state remains `Running`, automatic replay is blocked, and fresh verification is required.
- Browser exact approval scopes are data-minimal: capability id + stable action id + ordered permission names. Browser action values, upload paths, rationale, page URLs, untrusted source, and tool arguments are not part of the scope.
- Capability audit failure events do not persist raw backend/provider/site exception messages.
- Capability/action/tool identity tokens used by approval/audit are now bounded canonical ASCII tokens; malformed identity text is rejected before scope construction/backend execution/audit append on the capability-tool path.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product/evaluator/protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role separation, protocol-level envelope trust, and deployment/preflight policy reuse.

### 2026-09-13 — Trust boundaries and browser exact-once/privacy hardening
- Added structured provider-failure provenance, bounded remediation, generic failure-diagnostic quarantine, privacy-safe desktop exception/display projection, constrained browser product outcomes, bounded/privacy-reduced browser-goal evidence, and safe legacy goal-state migration.
- Applied browser-goal evidence projection both durably and in-process so planner history cannot receive a more permissive surface after same-process execution.
- Removed duplicate parent `PendingAction` retention while preserving `PendingJobId` + exact scope recovery semantics.
- Added terminal browser checkpoint scrubbing for safely failed/cancelled jobs.
- Added explicit ambiguous execution handling so driver-reported execution with failed verification is never auto-retried.
- Preserved ambiguous in-flight cancellation checkpoints for recovery verification instead of trading side-effect safety for premature data deletion.
- Confirmed exact browser approval scope contains only capability id, stable action id, and ordered permissions.
- Removed raw backend exception messages from capability failure audit summaries.

### 2026-09-13 — Capability identity/audit authority hardening
Completed in this run:
- Re-read this ledger and inspected recent commits plus the capability registry, permission policy, tool executor, browser capability execution path, and adversarial capability tests before mutation.
- Added `CapabilityIdentityTrust` as the canonical identity-token boundary for `CapabilityId`, `ActionId`, and `ToolName`.
- Identity tokens are **not sanitized or normalized**. They must already be canonical bounded ASCII tokens using letters, digits, `.`, `-`, `_`, or `:`. This avoids silently changing exact authorization identity through trimming/rewriting.
- Bounds are explicit: capability id <= 96 chars, action id <= 128 chars, tool name <= 96 chars.
- `CapabilityRegistry` now validates registered descriptor ids before constructing the registry and validates requested capability ids before lookup.
- `CapabilityPermissionPolicy` now fails closed on malformed capability/action ids before any approval scope can be constructed.
- `CapabilityToolExecutor` now validates capability id, action id, and tool name before policy evaluation, backend execution, or audit append. A malicious tool name therefore cannot become durable `metadata["tool"]`.
- Existing browser authority remains byte-for-byte compatible: `browser.agent|<32-char-guid>|BrowserWrite` and equivalent ordered permission scopes are unchanged.
- Added `CapabilityIdentityTrustTests` covering canonical identifiers, CR/LF/NUL/whitespace/delimiter injection, overlong values, invalid registry ids, invalid action ids, malformed tool names, no-backend/no-audit behavior, and preservation of existing browser approval/audit semantics.

Engineering commits this run before this ledger update:
- `9d89e8af63694ad1660d11b2b68de009cf092099` — add capability identity trust boundary.
- `d23b4fe30fc70df21078892334e408b6e237722f` — enforce identity trust in registry/policy.
- `83f8c86f2b5fe5734557d588ae868c6656cba15a` — validate capability/tool audit identities before execution.
- `e6d1d86a42d46725ce77e8702df7c608f19f2328` — add adversarial identity regressions.

Validation / evidence this run:
- Verified immediately before every successful GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- Starting repository head was `276661fb6e68824eef77698b4448154db83476db`.
- GitHub compare before this ledger commit reported **4 commits ahead / 0 behind**, touching exactly `CapabilityIdentityTrust.cs`, `CapabilityRegistry.cs`, `CapabilityToolExecutor.cs`, and `CapabilityIdentityTrustTests.cs`.
- Static review confirms the production browser capability id (`browser.agent`), tool (`browser.execute`), and GUID-N action ids all satisfy the new identity grammar.
- Static review confirms invalid identity text cannot enter an approval scope on `CapabilityPermissionPolicy`, and malformed tool-name text cannot reach backend execution or capability-tool audit emission.
- Environment check again found no usable `dotnet`, `csc`, or `msbuild` executable.
- **No compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.** The new regressions are persisted but unexecuted here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Exact approval identity is reject-only, not sanitizer-based, preventing alternate raw strings from being normalized into the same or a different authority.
- Capability/action/tool control characters, whitespace, separators such as `|`, and oversized secret-bearing values are rejected before capability-tool audit persistence.
- Browser approval scopes remain exact and privacy-minimized; browser values/source URLs/tool arguments are excluded.
- Durable capability failure audit records do not receive raw backend exception messages.
- Executable browser action data remains only where pending/retry/ambiguous-recovery semantics require it; safe terminal records use fixed tombstones.
- Executed-but-unverified side effects cannot enter automatic retry through the generic failure path.
- Ambiguous execution/cancellation retains the checkpoint intentionally because fresh verification requires intended action/postcondition data; privacy minimization does not override side-effect safety.
- Exact approval comparison semantics and ephemeral single-use approval grants remain unchanged.
- Existing encrypted transport, signed binding, remote exact-once ingestion, browser download quarantine, emergency stop, and permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this environment, so recent Core/WPF/Worker changes still require a real restore/build/test/run before compile confidence is justified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- `JsonLinesAuditTrail.ValidateEvent(...)` still directly accepts any nonblank `CapabilityId`/`ActionId`; the production capability-tool path now rejects malformed identities first, but other current/future direct audit producers should be audited and the durable audit sink should eventually enforce the same identity contract without breaking legitimate legacy records.
- Generic audit event type/metadata keys and values have broader trust semantics than capability identity fields; any user/provider-derived metadata must be explicitly bounded/projected before persistence.
- Ambiguous `Running`/in-flight-cancelled browser records intentionally retain executable action material until recovery resolves the side effect. Cleanup must scrub it immediately after trusted terminal reconciliation.
- `BrowserHostRuntime.Describe(...)` still retains raw `LastError` in its trusted internal outcome for goal/recovery infrastructure; public browser product callers and browser-goal presentation paths are constrained, but future trusted internal consumers must not render it directly.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real embedding ranking and real Nebius Object Storage/Serverless execution still require live environment validation.
- Lower-level `NebiusResearchClientRuntime.Create(...)` retains a same-key compatibility fallback for legacy unit/contract callers; production composition/preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent migrations. If executable validation remains unavailable, extend the same identity contract into the **durable audit sink(s)** (`JsonLinesAuditTrail` and segmented/bounded variants as appropriate), with migration-safe regressions proving malicious identity fields cannot be appended or loaded while legitimate existing NVIDEA event identities remain compatible.
