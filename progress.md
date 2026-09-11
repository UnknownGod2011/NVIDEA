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
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, edit/delete/retention controls, deterministic fallback, retrieval-quality fixtures, loopback-only Ollama embeddings, vector-space provenance isolation, and local-only migration/re-index maintenance.
- Tavily Search + Extract research with canonical deduplication, quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify execution, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Browser pages flagged by the Playwright prompt-injection detector now require explicit user approval for otherwise non-consequential state-changing Navigate/Click/Type/Select/Download actions; Read/Back/Refresh remain available, Upload remains independently approval-gated, and credential typing remains blocked.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution through `ResearchCloudExecutionCoordinator`; WPF durable research uses lifecycle-aware product/UI projections.
- Desktop remote-research lifecycle and paid dispatch are separately opt-in. Lifecycle-only recovery remains available without Tavily; new paid dispatch requires local research availability plus exact one-shot approval.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Desktop readiness diagnostics distinguish ready/blocked/locked research capabilities without exposing secret values or constructing new provider authority.
- Windows voice invocation is local and review-first: `Ctrl+Shift+V` / Voice asks for microphone consent, transcribes through installed Windows speech recognition, and places text into the prompt without auto-running it or routing audio to cloud speech.
- Windows Memory maintenance previews and safely re-indexes stale/missing local embeddings with explicit Sensitive/Restricted opt-ins, stale-preview revalidation, progress/cancellation, and aggregate-only UI disclosure.
- `tools/Nvidea.PersonalAiDemoEval` provides deterministic, credential-free positive cross-cutting evidence over real Core contracts.
- `tools/Nvidea.PersonalAiAdversarialEval` now provides deterministic, credential-free negative-path evidence for prompt-injection authority, approval denial, verification failure, citation hallucination, exact approval scope, and ambiguous crash recovery.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, mounted transport, and worker hardening.

### 2026-09-10 — Native Object Storage + reproducible evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned worker requirements, live runtime factory/modes, reproducible redacted deployment fingerprints, MysteryBox validation, machine-readable evidence, atomic artifact persistence, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product lifecycle / authority hardening
Added `ResearchCloudExecutionCoordinator`, `ResearchProductRuntime`, lifecycle-aware WPF research, `BrowserProductRuntime`, restart-safe browser-goal recovery, assembly-internal privileged construction, lifecycle/dispatch readiness, one-shot cloud approval, Tavily-independent remote recovery, and local review-first Windows voice invocation.

### 2026-09-11 — Production local semantic memory
Added embedding provenance/model-space isolation, loopback-only Ollama `/api/embed`, bounded requests/batches, strict redirect refusal, explicit desktop opt-in, deterministic fallback, safe local embedding migration with high-sensitivity exclusion by default, WPF Memory maintenance, stale-consent protection, progress/cancellation, and deterministic semantic retrieval-quality fixtures.

### 2026-09-11 — Deterministic positive Personal AI evaluator
Added `tools/Nvidea.PersonalAiDemoEval` and `docs/personal-ai-demo-eval.md`. It exercises real Core contracts for context + durable memory recall, clipboard withholding, cited research, browser approval + post-action verification, restart-safe durable jobs, single-use exact-scope approvals, audit transitions, and local-vs-cloud privacy policy. Output is machine-readable JSON with stable check IDs and optional `--output` persistence. It uses synthetic external edges and is explicitly not live-provider evidence.

### 2026-09-11 — Adversarial evaluator + stronger prompt-injection execution boundary
Completed:
- Re-read this ledger completely and inspected current browser executor/safety policy, Playwright prompt-injection detection, research citation validation, durable approval semantics, and ambiguous `Running` recovery before implementation.
- Added `tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj` and `Program.cs` as a deterministic .NET 8 negative-path quality gate over real `Nvidea.Core` contracts.
- Added six stable adversarial checks:
  - `prompt-injection-cannot-authorize`: hostile page text claiming the user already approved a consequential action cannot mint execution authority; external approval is still required and the driver remains untouched when denied.
  - `denied-browser-approval-prevents-mutation`: denied Upload approval prevents any driver mutation/local-data trust-boundary crossing.
  - `failed-verification-stops-plan`: driver success without fresh verification stops `ExecutePlanAsync` before a second action.
  - `unknown-research-citation-is-flagged`: invented `[src:...]` output is excluded from `UsedCitations` and produces an explicit unknown-source warning.
  - `wrong-approval-scope-fails-closed`: a mismatched durable approval scope is rejected without changing the persisted wait or running the consequential step.
  - `ambiguous-running-job-does-not-replay`: a durable `Running` record is returned unchanged and its handler is not auto-replayed after possible crash residue.
- Added `docs/personal-ai-adversarial-eval.md` describing scope, command, stable checks, privacy boundary, and explicit non-claims about live providers/Windows integration.
- Adversarial review exposed a product-level gap: Playwright already marks prompt-injection-like pages, but otherwise-medium state-changing interactions could still execute without approval. Hardened `BrowserSafetyPolicy` so flagged-page Navigate/Click/Type/Select/Download operations are High-risk and require explicit user approval. Read/Back/Refresh remain usable; Upload and credential-sensitive rules retain their stricter independent behavior.
- Added focused `BrowserAgentTests` proving a generic Click on a prompt-injection-flagged page becomes High-risk/approval-gated while Read remains Low-risk and approval-free.

Engineering commits before this ledger update:
- `d659a309d1b68c739cc40d91fb40eb4413372645` — add adversarial personal AI evaluator project.
- `e2967fa40de689d4926aa66202f38a0e2eb25597` — implement adversarial personal AI safety evaluator.
- `105295efdac168301567128305b13ac795d6c4e2` — document adversarial personal AI evaluator.
- `2a94ff9802a0df1692d4b08ab8edacd51727bd24` — gate flagged prompt-injection browser mutations.
- `ed69858eaa59e690cb9eb1c7eccdf0589487250d` — test prompt-injection mutation approval gates.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `eef21ccaa27c1c6412d3fe49a116efee95b0c0fe` to engineering head `ed69858eaa59e690cb9eb1c7eccdf0589487250d` is **5 commits ahead / 0 behind** across exactly five focused files: the adversarial evaluator project/program/docs plus `BrowserSafetyPolicy.cs` and `BrowserAgentTests.cs`.
- `command -v dotnet` / `dotnet --info` again produced no usable .NET execution signal in the available runtime. Therefore Core/WPF/Worker compilation, XAML compilation, tests, and both evaluator binaries are **not claimed as executed or passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily calls, Playwright browser, Ollama runtime, or paid service was used by this run.

Security / privacy / failure review:
- Both evaluators use synthetic fixtures only and intentionally avoid real user memories, cookies, account sessions, API keys, provider resource IDs, cloud errors, or paid/network dependencies.
- The new prompt-injection gate applies at browser execution policy, downstream of model planning, so hostile webpage text cannot reduce its own approval requirement even if it influences the planner.
- Credential/OTP/payment/private-key typing remains blocked rather than merely approval-gated.
- Upload remains independently High-risk because it crosses a local-data boundary even on non-injection pages.
- Failed fresh post-action verification halts the remaining plan; driver-reported success alone is not treated as proof.
- Unknown research source IDs remain warnings and are not promoted into validated citation objects.
- Wrong exact approval scope leaves durable approval state unchanged; ambiguous Running jobs remain non-replayable without trusted reconciliation.
- Synthetic PASS evidence must not be presented as proof that live Nebius, Tavily, Playwright, WPF, speech, Ollama, Object Storage, or Serverless integration is healthy.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, and both evaluator tools are not compiled or executed here.
- The new adversarial evaluator and prompt-injection policy change are statically reviewed but require a real .NET 8 restore/build/test before their JSON/test results can be treated as executable evidence.
- The prompt-injection detector is heuristic; false negatives remain possible. The strengthened approval gate only applies when `ContainsUntrustedInstructions` is set, so planner/system-prompt defenses and downstream capability gates remain necessary defense-in-depth.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows .NET 8 build/run pass.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- A real Windows machine still needs microphone permission plus an installed speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, Nebius contract tools, `Nvidea.PersonalAiDemoEval`, and `Nvidea.PersonalAiAdversarialEval`; compile WPF/XAML; run focused memory/retrieval/migration, voice, readiness, lifecycle/research, browser authority/integration, API-surface, and security suites; run both evaluators with `--output`; then fix every compile/runtime defect before treating JSON as judging evidence. If executable validation remains unavailable, build a **single deterministic judging-evidence manifest/validator** that ingests positive/adversarial evaluator artifacts plus Nebius contract evidence, verifies schema/check completeness and artifact hashes, reports live-vs-synthetic evidence boundaries, and emits one redacted judge-facing PASS/FAIL summary without credentials or provider secrets.
