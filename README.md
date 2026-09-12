# NVIDEA

NVIDEA is an open-source, Windows-first **Personal AI operating layer** for the Nebius x NVIDIA Global AI Hackathon. It is inspired by interaction patterns that proved useful in keyboard.wtf, but it is a separate project with an NVIDIA/Nebius-first intelligence stack, durable personal memory, Tavily research, safe browser automation, long-running work, verification, and explicit permission boundaries.

## Hackathon target

- **Nebius x NVIDIA Global AI Hackathon**
- Primary track: **Personal AI**
- Bonus target: **Best Use of Tavily**
- Core reasoning runtime: **NVIDIA Nemotron on Nebius Token Factory / Nebius AI Cloud**

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
- local review-first voice invocation (`Ctrl+Shift+V` / Voice);
- Windows speech recognition without routing microphone audio to a cloud speech provider;
- emergency stop and cancellation;
- approval dialogs for consequential actions;
- durable research controls, readiness state, audit UI and browser-download handoff;
- Memory maintenance for local semantic embedding re-indexing.

Voice recognition places the transcript into the prompt for user review rather than auto-executing it.

### Personal memory

NVIDEA implements typed working, episodic, semantic, project/entity and skill memory with provenance, confidence, importance, sensitivity and retention metadata.

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

NVIDEA does not bypass CAPTCHA, login, site, browser or OS safeguards.

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

The remote protocol separates three RSA purposes: worker work-item decryption, client dispatch signing, and client result decryption. The two client identities are required to be distinct; Serverless receives only their public halves.

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

### Live Nebius research keys

The Serverless research contract uses three independent RSA keypairs. Production validation requires RSA-2048 or stronger and rejects reuse of the same client keypair for dispatch signing and result encryption/decryption.

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

## Build and validation

Prerequisite: **.NET 8 SDK**. Windows/WPF validation requires Windows.

```powershell
dotnet build .\src\Nvidea.Core\Nvidea.Core.csproj
dotnet build .\src\Nvidea.Windows\Nvidea.Windows.csproj
dotnet build .\src\Nvidea.Worker\Nvidea.Worker.csproj
dotnet test .\tests\Nvidea.Core.Tests\Nvidea.Core.Tests.csproj
dotnet test .\tests\Nvidea.DemoPackageValidator.Tests\Nvidea.DemoPackageValidator.Tests.csproj
```

Credential-free evaluators can then be run locally:

```powershell
dotnet run --project .\tools\Nvidea.PersonalAiDemoEval\Nvidea.PersonalAiDemoEval.csproj -- --output .\artifacts\personal-ai-positive.json
dotnet run --project .\tools\Nvidea.PersonalAiAdversarialEval\Nvidea.PersonalAiAdversarialEval.csproj -- --output .\artifacts\personal-ai-adversarial.json
dotnet run --project .\tools\Nvidea.DemoPackageValidator\Nvidea.DemoPackageValidator.csproj -- .\docs\demo-package.json
```

These commands are validation instructions, not claims that a particular environment has already produced a green result. See [`progress.md`](progress.md) for the latest executed/unexecuted evidence boundary.

## Privacy and safety contract

NVIDEA is designed around least privilege:

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

## Current verification gaps

Meaningful implementation remains, but the project is **not declared finished**. The most important outstanding verification is a real .NET 8 Windows restore/build/test pass covering Core, WPF/XAML, Worker, voice, local embeddings, browser integration and evaluator tooling. Live Nebius Object Storage/Serverless infrastructure and production provider behavior also require credential-backed validation before being represented as live evidence.

Other ongoing work includes memory compaction/summarization quality, broader reusable skills, browser UX polish, packaging/onboarding, local embedding quality calibration, and final judge-facing demo execution.

## Relationship to keyboard.wtf

keyboard.wtf is read-only reference material for useful interaction patterns such as Windows hotkeys, voice UI, local speech, workflows and permission concepts. NVIDEA is a separate repository and materially changes the backend architecture around NVIDIA/Nebius reasoning, memory, research, browser automation, durable work and safety.

Automation working on NVIDEA is not permitted to mutate the keyboard.wtf repository.

## Development ledger

See [`progress.md`](progress.md) for current architecture decisions, exact completed work, evidence, known blockers, risks and the next engineering priority.

## License

MIT. See [`LICENSE`](LICENSE).
