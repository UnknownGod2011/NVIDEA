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
- Browser pages flagged by the Playwright prompt-injection detector require explicit approval for state-changing Navigate/Click/Type/Select/Download actions; Read/Back/Refresh remain usable, Upload remains independently approval-gated, and credential typing remains blocked.
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
- `tools/Nvidea.PersonalAiAdversarialEval` provides deterministic, credential-free negative-path evidence for prompt-injection authority, approval denial, verification failure, citation hallucination, exact approval scope, and ambiguous crash recovery.
- `tools/Nvidea.JudgingEvidenceVerifier` now combines positive/adversarial synthetic evaluator artifacts with matching Nebius live deployment evidence into one bounded, hashed, redacted judge-facing PASS/FAIL summary.

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

### 2026-09-11 — Deterministic Personal AI evaluators
Added `tools/Nvidea.PersonalAiDemoEval` for positive end-to-end evidence over context, durable memory, clipboard withholding, cited research, browser approval + verification, restart-safe jobs, exact-scope approvals, audit, and local-vs-cloud privacy policy. Added `tools/Nvidea.PersonalAiAdversarialEval` for prompt-injection authority, denied approval, failed verification, invented citations, wrong approval scope, and ambiguous `Running` crash residue. Both emit machine-readable JSON and intentionally use synthetic external edges.

### 2026-09-11 — Browser prompt-injection execution hardening
Adversarial review exposed that prompt-injection-like pages could still permit otherwise-medium state-changing actions. Hardened `BrowserSafetyPolicy` so flagged-page Navigate/Click/Type/Select/Download operations become High-risk and approval-gated. Read/Back/Refresh remain usable; Upload and credential-sensitive rules keep their stricter independent behavior. Added focused policy tests.

### 2026-09-11 — Unified judging evidence verifier
Completed:
- Re-read this ledger completely and inspected the current positive evaluator schema, adversarial evaluator schema, Nebius evidence verifier, and repository/tool layout before implementation.
- Added `tools/Nvidea.JudgingEvidenceVerifier/Nvidea.JudgingEvidenceVerifier.csproj` targeting .NET 8 with nullable checking and warnings-as-errors, referencing the existing Core project rather than duplicating Nebius verification logic.
- Added `Program.cs` implementing a credential-free verifier that requires four artifacts: positive evaluator JSON, adversarial evaluator JSON, redacted Nebius deployment manifest, and redacted Nebius live PASS evidence.
- The verifier enforces bounded input size (256 KiB each), strict JSON without comments/trailing commas, recursive duplicate-property rejection, evaluator schema version `1`, exact stable required check sets, unique check IDs, bounded check details, all checks passing, `overallPassed=true`, and the positive evaluator metrics object.
- Reused `NebiusResearchDeploymentEvidenceVerifier.VerifyJson(...)` as the canonical live-cloud evidence boundary. This recomputes the redacted deployment fingerprint, compares it to the live PASS fingerprint, and validates PASS completion/count invariants without credentials or network calls.
- Added SHA-256 hashes for all four exact artifact byte sequences to the resulting summary so the judge package can bind the summary to the reviewed evidence files.
- The success output clearly labels positive/adversarial evidence as `synthetic` and Nebius evidence as `live`, and explicitly records claims that remain outside the evidence boundary.
- The output deliberately excludes input paths, evaluator detail text, credentials, provider errors, prompts/results, browser session data, memory content, cookies, tokens, key material, and secret references.
- Success output does not add verifier wall-clock time, keeping the summary stable for identical evidence inputs apart from platform newline handling when saved.
- `--output` uses temp-file + same-directory replace semantics for atomic-ish artifact persistence; invalid/missing/mismatched artifacts fail closed with a bounded single-line error summary and nonzero exit code.
- Added `docs/judging-evidence-verifier.md` with usage, validation boundary, redaction guarantees, evidence-class semantics, deterministic-output behavior, and explicit non-claims.
- Static review found and fixed a completeness gap in the first pass: strict deserialization alone did not prove the positive metrics field existed, so the verifier now explicitly requires an object-valued metrics property.

Engineering commits before this ledger update:
- `3eaec159d42b74b6fcd2b1087558a63c73e47376` — add unified judging evidence verifier project.
- `2455fd4b562bf3ae5b143efcb7dc165b2de72775` — implement unified judging evidence verification.
- `9b09eedb4950a9af92adb695cfcc25a6d2b401f8` — harden judging evidence schema validation.
- `c190d7ca08c09cad771f6adb31f5092d0c03a0d7` — document unified judging evidence verification.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `f91674286876118b7df71cc51709ec0109b3851d` to engineering head `c190d7ca08c09cad771f6adb31f5092d0c03a0d7` is **4 commits ahead / 0 behind** across exactly three files: the new verifier project, verifier program, and documentation.
- `command -v dotnet` and `dotnet --info` again produced no usable execution signal in the available runtime. Therefore Core/WPF/Worker compilation, XAML compilation, tests, evaluator binaries, and the new judge verifier are **not claimed as compiled/executed/passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily calls, Playwright browser, Ollama runtime, or paid service was used by this run.

Security / privacy / failure review:
- The new judge verifier does not resolve secrets or perform network/provider calls; it consumes already-produced evidence only.
- Raw evaluator detail fields are validated but never copied into the success summary, reducing the chance fixture/user text leaks into a judge package.
- Input file paths are not emitted in success/failure JSON. Path/read errors are converted to generic artifact descriptions.
- Nebius verification stays delegated to the existing production verifier, preserving its bounded parsing, duplicate-property rejection, deployment-fingerprint recomputation, fixed-time fingerprint comparison, and PASS-count validation.
- SHA-256 hashes provide tamper-evident binding of the generated judge summary to the exact supplied artifact bytes; they are not signatures or third-party attestations.
- Synthetic PASS remains explicitly separated from live Nebius evidence and must not be represented as proof of live Tavily, browser, Windows UI/speech, Ollama, Object Storage, or all Serverless behavior.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, all evaluator tools, and the new judge evidence verifier are not compiled or executed here.
- The unified verifier is statically reviewed but still requires a real .NET 8 restore/build/run against actual generated evaluator and Nebius evidence artifacts before its JSON can be treated as executable judging evidence.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows .NET 8 build/run pass.
- The prompt-injection detector is heuristic; false negatives remain possible, so planner/system-prompt defenses and downstream capability gates remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- A real Windows machine still needs microphone permission plus an installed speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility hashes/fingerprints prove internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, Nebius contract tools, `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, and `Nvidea.JudgingEvidenceVerifier`; compile WPF/XAML; run focused memory/retrieval/migration, voice, readiness, lifecycle/research, browser authority/integration, API-surface, and security suites; generate both evaluator artifacts; run the unified verifier against real redacted Nebius preflight/PASS evidence; and fix every compile/runtime defect before treating the final summary as judging evidence. If executable validation remains unavailable, the next implementation target is a **judge-ready deterministic demo package specification + validation checklist** that maps each <=3-minute demo beat to concrete feature/evidence artifacts, prevents synthetic/live evidence overclaiming, checks README/setup/demo commands for consistency, and identifies any remaining missing hackathon submission assets without fabricating provider success.
