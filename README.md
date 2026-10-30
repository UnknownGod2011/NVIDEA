# keyboard-wtf

keyboard-wtf is an open-source, Windows-first **Personal AI operating layer** for the Nebius x NVIDIA Global AI Hackathon. It combines an NVIDIA/Nebius-first intelligence stack with durable personal memory, Tavily research, safe browser automation, long-running work, verification, and explicit permission boundaries.

## Hackathon target

- **Nebius x NVIDIA Global AI Hackathon**
- Primary track: **Personal AI**
- Bonus target: **Best Use of Tavily**
- Core reasoning runtime: **NVIDIA Nemotron on Nebius Token Factory / Nebius AI Cloud**

Every cloud reasoning request is sent through the NVIDIA Nemotron / Nebius Token Factory adapter. Local speech recognition and speech output stay on Windows by default; optional Ollama is used only for local memory embeddings, never for the reasoning runtime.

## Quick start for judges

The normal judge path is a local source build; no website download is required.

1. Install **Windows 10/11** and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone this repository and open PowerShell in its root.
3. Create a Nebius Token Factory API key and a Tavily API key, then set them for the current PowerShell session:

   ```powershell
   $env:NEBIUS_API_KEY = "your-nebius-token-factory-key"
   $env:TAVILY_API_KEY = "your-tavily-key"
   ```

4. Restore, build, and run the Windows app:

   ```powershell
   dotnet restore .\src\Nvidea.Windows\Nvidea.Windows.csproj
   dotnet run --project .\src\Nvidea.Windows\Nvidea.Windows.csproj -c Release
   ```

The app starts in local-only mode when `NEBIUS_API_KEY` is absent, so Local Dictation remains available. Smart Dictation, Jarvis reasoning, and Tavily research fail at their own cloud boundary with configuration-only guidance. A complete, copy/paste-friendly guide—including optional local memory and the opt-in Serverless path—is in [`docs/judge-setup.md`](docs/judge-setup.md).

For voice, enable a Windows speech-recognition language and grant microphone permission. Open **Settings** to configure global shortcuts for the app, Local Dictation, Smart Dictation, Jarvis, Emergency Stop, and Settings. Defaults are `Ctrl+Shift+Space` (open app), `Ctrl+Alt+D` (Local Dictation), `Ctrl+Alt+K` (Smart Dictation), `Ctrl+Alt+Q` (Jarvis), `Ctrl+Alt+X` (Emergency Stop), and `Ctrl+Alt+,` (Settings). On first run, compatible shortcuts are imported from `%LOCALAPPDATA%\keyboard.wtf\settings.json` if it exists and no keyboard-wtf shortcut file has been saved. Only shortcut values are copied; the old file is not changed, and legacy provider credentials are not copied. Local Dictation works without a Nebius or Tavily key; Smart Dictation and Jarvis require `NEBIUS_API_KEY`. Speech is transcribed locally; only transcript text is sent to Nemotron for Smart Dictation and Jarvis.

## What is implemented

### NVIDIA / Nebius reasoning

`src/Nvidea.Core/Nebius` contains a direct `HttpClient` Token Factory adapter with structured tool calling, optional JSON-schema output, retry handling, cancellation, timeouts, endpoint validation, and secret-safe provider errors.

Current default workload routing uses Token Factory model identifiers verified in the official Nebius cookbook:

| Workload | Default model |
| --- | --- |
| Fast / lightweight | `nvidia/nvidia-nemotron-3-nano-30b-a3b` |
| Standard | `nvidia/nemotron-3-super-120b-a12b` |
| Deep / difficult | `nvidia/Nemotron-3-Ultra-550b-a55b` |

All three remain overrideable through environment variables so a provider catalog change does not require an architectural rewrite. If an optional fast/deep tier is deliberately disabled in code, routing fails safely back to the standard model rather than inventing an ID.

### Windows interaction shell

`src/Nvidea.Windows` provides the trusted desktop surface with:

- global text invocation and active-app context capture;
- selected-text and opt-in clipboard context paths;
- unified Local Dictation, Smart Dictation and Jarvis voice modes with configurable per-mode global shortcuts;
- continuous local Windows speech recognition with live partial transcript feedback and pause-based turn detection, without routing microphone audio to a cloud speech provider;
- local SAPI spoken responses, interruption, emergency stop, and a bounded multi-turn Jarvis session;
- emergency stop and cancellation;
- approval dialogs for consequential actions;
- durable research controls, readiness state, audit UI and browser-download handoff;
- Memory maintenance for local semantic embedding re-indexing.

Voice modes are deliberately distinct:

- **Local Dictation** transcribes locally and inserts the exact final transcript only after restoring the foreground window captured before invocation; a changed target fails closed. When started from the main window without an external target, it leaves the transcript in the prompt for review.
- **Smart Dictation** sends transcript text—not raw microphone audio—to NVIDIA Nemotron on Nebius, requests grammar/formatting cleanup while preserving meaning, then requires approval before safe insertion. When started from the main window without an external target, the result stays in the prompt for review.
- **Jarvis** keeps a bounded multi-turn conversation, retrieves relevant personal memory, uses the same NVIDIA Nemotron client, speaks through local Windows SAPI, and accepts only schema-validated typed tools. The initial supported local tools are approved application launch, approved HTTPS website launch, and approved insertion into the captured application. It never accepts model-generated shell commands.

The baseline speech path depends on the installed Windows recognizer/SAPI voice. Jarvis is turn-taking rather than full-duplex: local speech recognition produces a transcript, Nemotron reasons over text through Nebius, and Windows SAPI speaks the response. NVIDIA Nemotron VoiceChat full-duplex inference is not bundled or claimed as a feature; its hosted endpoint is currently early-access gated, and NVIDIA's documented local runtime requires a separate model and suitable GPU.

### Personal memory

keyboard-wtf implements typed working, episodic, semantic, project/entity and skill memory with provenance, confidence, importance, sensitivity and retention metadata.

The retrieval path combines lexical relevance, semantic similarity when compatible embeddings exist, recency and importance. Vector comparisons are isolated by provider/model/dimension provenance so vectors from incompatible embedding spaces are never silently mixed.

A production local embedding adapter is available through Ollama's `/api/embed` endpoint. It is **opt-in**, loopback-only, bounded, redirect-refusing and failure-tolerant. If Ollama is absent or embedding fails, memory remains usable through deterministic lexical/recency/importance retrieval.

Memory re-indexing is local-only and resumable. Sensitive and Restricted memories are excluded by default and require separate explicit opt-ins. The Windows maintenance flow previews aggregate scope, revalidates stale consent before execution, supports cancellation, and persists completed batches safely.

See:

- [`docs/local-memory-embeddings.md`](docs/local-memory-embeddings.md)
- [`docs/memory-embedding-migration.md`](docs/memory-embedding-migration.md)

### Tavily research with provenance

The research pipeline is deliberately staged instead of being one opaque model call:

1. Nemotron creates a bounded structured query plan.
2. Tavily Search performs discovery with topic/date/domain controls.
3. Top evidence is enriched through Tavily Extract.
4. URLs are canonicalized and deduplicated.
5. Evidence is ranked with explicit relevance, modest authority heuristics, freshness signals and source diversity.
6. Web content is wrapped as **untrusted evidence** before model synthesis.
7. Nemotron synthesizes with required `[src:SOURCE_ID]` markers.
8. Only source IDs present in collected evidence become validated citations.

Durable checkpoints preserve plan, ranked evidence, warnings, Tavily usage and completed reports. A normal resume does not repeat Tavily search/extract once evidence is safely checkpointed.

### Safe complex browser automation

The browser subsystem uses persistent Playwright/Chromium sessions with a plan → act → observe → verify loop. It includes:

- DOM/accessibility-oriented observation and robust action execution;
- post-action state verification rather than trusting driver success;
- prompt-injection heuristics and untrusted-page boundaries;
- approval gates for consequential actions;
- additional High-risk approval gating for state-changing actions on prompt-injection-flagged pages;
- credential/OTP/payment/private-key typing blocks;
- durable download quarantine and explicit handoff;
- cancellation, emergency stop, crash recovery and ambiguous-action fail-closed behavior;
- single-owner profile/state leases to prevent concurrent mutation.

keyboard-wtf does not bypass CAPTCHA, login, site, browser or OS safeguards.

### Skills, permissions and audit

The capability layer includes a registry, risk classification, scoped approvals, single-use job approval context, and durable audit infrastructure. Consequential operations such as send/submit/publish/delete/purchase/account/security/financial changes require explicit authority rather than being inferred from model text or web content.

Protected local state uses Windows CurrentUser DPAPI by default where applicable, and audit storage is hash-chained/segmented and bounded.

### Long-running Nebius execution

`src/Nvidea.Worker` and the Core job/runtime components implement an explicit remote research path for Nebius infrastructure:

- encrypted opaque work items;
- two-phase dispatch;
- signed resource-ID binding;
- S3-compatible Nebius Object Storage transport;
- Serverless-mounted worker transport;
- crash/lifecycle reconciliation and durable cancellation;
- exact-once result ingestion;
- non-root worker image;
- deployment preflight for mount alignment, MysteryBox-backed credentials, resource bounds, separated RSA identities and digest-pinned images.

The remote protocol separates four RSA purposes: worker work-item decryption, client dispatch signing, client result decryption, and the worker remote-result signing identity. The two client identities are required to be distinct; Serverless receives only their public halves and a version-pinned worker signing secret.

The repository also includes `Nvidea.NebiusContractProbe` and evidence verification tooling for zero-cost preflight, explicit paid live research, redacted deployment fingerprints and offline validation.

**Important validation boundary:** the Serverless contract path is implemented, but this repository does **not** claim that a credential-backed live Nebius Serverless PASS has been demonstrated until a matching live PASS artifact is actually generated and verified.

## Deterministic judging evidence

The repository includes credential-free evidence tools that exercise real Core contracts without pretending synthetic evidence is a live-provider result:

- `tools/Nvidea.PersonalAiDemoEval` — positive cross-cutting Personal AI scenarios.
- `tools/Nvidea.PersonalAiAdversarialEval` — negative-path security and fail-closed scenarios.
- `tools/Nvidea.JudgingEvidenceVerifier` — combines synthetic evaluator artifacts with matching Nebius live deployment evidence and emits a bounded, hashed, redacted PASS/FAIL summary.
- `tools/Nvidea.DemoPackageValidator` — validates the final ≤3-minute judging package, required demo beats, evidence classes, asset paths, commands, provider-live claim boundaries and secret markers.
- `tests/Nvidea.DemoPackageValidator.Tests` — adversarial regression cases against the actual validator CLI.

The canonical recording plan is [`docs/demo-package.json`](docs/demo-package.json), currently budgeted below the 180-second limit. See [`docs/demo-package.md`](docs/demo-package.md).

## Architecture

```text
Windows interaction shell
  hotkeys / voice / text / selected context
                    |
                    v
              Personal AI core
        plan -> act -> observe -> verify
          /          |             \
         v           v              v
      Memory       Skills       Risk/approval
         \           |              /
          \          v             /
          Nemotron model routing
       Nano / Super / Ultra tiers
                    |
          Nebius Token Factory
                    |
      +-------------+-------------+
      |                           |
      v                           v
 Tavily research             Tool execution
 provenance/citations         browser / OS
      |                           |
      +-------------+-------------+
                    v
          verified task outcome

Cloud-safe long-running research -> explicit Nebius remote contract
Private OS/browser operations   -> local Windows runtime
```

## Configuration

Live Token Factory + Tavily usage requires secrets supplied outside the repository:

```powershell
$env:NEBIUS_API_KEY = "your-token-factory-key"
$env:TAVILY_API_KEY = "your-tavily-key"
```

Current defaults can be overridden explicitly:

```powershell
$env:NVIDEA_NEBIUS_BASE_URL = "https://api.tokenfactory.us-central1.nebius.com/v1/"
$env:NVIDEA_MODEL_FAST = "nvidia/nvidia-nemotron-3-nano-30b-a3b"
$env:NVIDEA_MODEL_STANDARD = "nvidia/nemotron-3-super-120b-a12b"
$env:NVIDEA_MODEL_DEEP = "nvidia/Nemotron-3-Ultra-550b-a55b"
```

Local semantic memory embeddings are deliberately opt-in. See [`docs/local-memory-embeddings.md`](docs/local-memory-embeddings.md) for the Ollama endpoint/model settings and privacy behavior.

To persist the two normal runtime keys for future PowerShell sessions, use `setx` and open a new terminal afterward. Do not commit keys into `.env`, source, screenshots, or a GitHub issue.

```powershell
setx NEBIUS_API_KEY "your-nebius-token-factory-key"
setx TAVILY_API_KEY "your-tavily-key"
```

### Live Nebius research keys

The Serverless research contract uses four independent RSA keypairs. Production validation requires RSA-2048 or stronger and rejects reuse of identities across roles.

```bash
# Worker work-item encryption/decryption
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out worker-private.pem
openssl pkey -in worker-private.pem -pubout -out worker-public.pem

# Client dispatch signing/verification
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out client-signing-private.pem
openssl pkey -in client-signing-private.pem -pubout -out client-signing-public.pem

# Client result encryption/decryption — MUST be a different keypair
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out client-result-private.pem
openssl pkey -in client-result-private.pem -pubout -out client-result-public.pem

# Worker remote-result signing — distinct from every identity above
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out worker-result-signing-private.pem
openssl pkey -in worker-result-signing-private.pem -pubout -out worker-result-signing-public.pem
```

For live client composition:

```powershell
$env:NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE = "C:\secure\worker-public.pem"
$env:NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE = "C:\secure\client-signing-private.pem"
$env:NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE = "C:\secure\client-result-private.pem"
```

The worker receives only the two distinct client public identities:

- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` — dispatch-binding verification only.
- `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` — protected-result encryption only.

The client dispatch-signing private key and client result-decryption private key remain local. The worker private key should be supplied through Nebius MysteryBox. Never commit any private key or provider credential.

The explicit Serverless contract uses additional `NVIDEA_LIVE_*` configuration for Object Storage, deployment shape, MysteryBox references and immutable worker images. See [`docs/nebius-contract-probe.md`](docs/nebius-contract-probe.md) and [`docs/nebius-research-worker.md`](docs/nebius-research-worker.md).

For remote worker-origin verification, configure only the matching public pin on Windows as `NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM`. The private result-signing PEM belongs only in the immutable Nebius SecretStash/MysteryBox reference `NVIDEA_LIVE_SECRET_WORKER_RESULT_SIGNING_PRIVATE_KEY_ID` plus `NVIDEA_LIVE_SECRET_WORKER_RESULT_SIGNING_PRIVATE_KEY_VERSION_ID`.

## Build and validation

Prerequisite: **.NET 8 SDK**. Windows/WPF validation requires Windows.

```powershell
dotnet build .\src\Nvidea.Core\Nvidea.Core.csproj
dotnet build .\src\Nvidea.Windows\Nvidea.Windows.csproj
dotnet build .\src\Nvidea.Worker\Nvidea.Worker.csproj
dotnet test .\tests\Nvidea.WorkerAuth.Tests\Nvidea.WorkerAuth.Tests.csproj
```

The focused worker-authentication suite is the validated security regression path for this branch. The broader historical `Nvidea.Core.Tests` and `Nvidea.DemoPackageValidator.Tests` projects are retained but presently have unrelated old-source/API-drift compile failures; their failure is documented in [`progress.md`](progress.md) and is not required to launch the app or evaluate the signed-worker path.

Credential-free evaluators can then be run locally:

```powershell
dotnet run --project .\tools\Nvidea.PersonalAiDemoEval\Nvidea.PersonalAiDemoEval.csproj -- --output .\artifacts\personal-ai-positive.json
dotnet run --project .\tools\Nvidea.PersonalAiAdversarialEval\Nvidea.PersonalAiAdversarialEval.csproj -- --output .\artifacts\personal-ai-adversarial.json
dotnet run --project .\tools\Nvidea.DemoPackageValidator\Nvidea.DemoPackageValidator.csproj -- .\docs\demo-package.json
```

These commands are validation instructions, not claims that a particular environment has already produced a green result. See [`progress.md`](progress.md) for the latest executed/unexecuted evidence boundary.

## Privacy and safety contract

keyboard-wtf is designed around least privilege:

- observe separately from act;
- require explicit approval for consequential operations;
- treat web pages, retrieved text and tool output as untrusted data, not authority;
- never treat prompt-injection content as authorization;
- send cloud inference only the data required for the current task;
- keep credentials/authentication secrets out of personal memory;
- expose cancellation and emergency stop;
- maintain an audit trail;
- keep private OS-local actions on-device;
- do not bypass CAPTCHA/login/site/OS safeguards.

## Packaging and GitHub Releases

Maintainers can create a portable Windows build without changing source or needing a website deployment:

```powershell
.\scripts\publish-windows.ps1 -OutputDirectory .\artifacts\keyboard-wtf-win-x64
```

Upload the resulting directory as a GitHub Release asset after performing the documented Windows validation. This repository does not claim a binary is a signed public release merely because it was built locally.

## Current verification gaps

The Windows Core, Worker, WPF/XAML, focused signed-worker contract suite, and recording-receipt verifier have Release build evidence recorded in [`progress.md`](progress.md). The broad legacy Core and DemoPackageValidator test projects still contain pre-existing unrelated source/API-drift failures, which are documented rather than hidden. Live Nebius Object Storage/Serverless infrastructure and production provider behavior require credential-backed validation before being represented as live evidence.

Other ongoing work includes memory compaction/summarization quality, broader reusable skills, browser UX polish, packaging/onboarding, local embedding quality calibration, and final judge-facing demo execution.

## Development ledger

See [`progress.md`](progress.md) for current architecture decisions, exact completed work, evidence, known blockers, risks and the next engineering priority.

## License

MIT. See [`LICENSE`](LICENSE).
