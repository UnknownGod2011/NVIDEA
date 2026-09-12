# Nebius contract probes

`tools/Nvidea.NebiusContractProbe` has three deliberately different modes:

1. a cheap default Token Factory planner contract probe,
2. a zero-cost `--live-research-preflight` deployment validation mode,
3. an explicit `--live-research` Serverless research probe that creates real cloud workloads.

## Default planner probe

The default mode checks whether the configured Nebius Token Factory Nemotron endpoint accepts the production strict JSON schema used by `NemotronBrowserPlanner` and returns output that survives NVIDEA's parser/validation boundary. It uses a synthetic browser observation, invokes no browser action, creates no durable remote job, and never prints provider response bodies or API keys.

Run from the repository root:

```bash
NEBIUS_API_KEY="..." dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj
```

Optional existing model/base-URL environment variables remain supported: `NVIDEA_NEBIUS_BASE_URL`, `NVIDEA_MODEL_STANDARD`, `NVIDEA_MODEL_FAST`, and `NVIDEA_MODEL_DEEP`.

## Zero-cost live research deployment preflight

Run this before spending any Serverless/model/Object Storage resources:

```bash
dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj -- --live-research-preflight
```

This mode reads the same required deployment inputs used by the real live research probe and then performs local-only validation. It does **not** create a Nebius job, call Token Factory, call Tavily, or make an Object Storage request.

The preflight fails closed unless all of the following hold:

- required Serverless project/access-token, platform, preset, subnet, timeout, disk, and transport fields are present and bounded;
- `NVIDEA_LIVE_WORKER_IMAGE` is immutable and digest-pinned in `registry/path@sha256:<64-hex>` form;
- the worker envelope public-key PEM is public-only RSA and at least 2048 bits;
- the client dispatch-signing private-key PEM is private RSA, at least 2048 bits, and derives the exact public key injected into the worker as `NVIDEA_CLIENT_PUBLIC_KEY_PEM`;
- the **separate** client result-envelope private-key PEM is private RSA, at least 2048 bits, and derives the exact public key injected into the worker as `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`;
- the canonical client signing and result-envelope public identities are different RSA keys;
- worker `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM` remain MysteryBox-backed references instead of plaintext job environment variables;
- the Serverless transport volume is `READ_WRITE` and exactly matches `NVIDEA_TRANSPORT_ROOT`;
- the native Object Storage bucket and prefix exactly match the mounted Serverless volume source and `SourcePath`;
- the Object Storage endpoint is a clean absolute HTTPS origin, retry/timeout bounds are safe, and local static credentials are present without being printed.

The real `--live-research` mode executes this same zero-cost preflight before constructing provider clients or submitting a job, so the dry run is not a separate weaker checklist.

A preflight PASS proves local configuration consistency only. It does **not** prove that Nebius accepts the job specification, that the bucket mount succeeds, that MysteryBox permissions are correct, or that Nemotron/Tavily execute successfully. It also never writes `NVIDEA_LIVE_PASS_EVIDENCE_PATH`; PASS evidence is reserved for an actual validated live run.

## Generate the required RSA identities

Use three independent RSA keypairs. The production trust boundaries require at least RSA-2048; RSA-3072 is a sensible local default. **Never reuse the client dispatch-signing keypair as the client result-envelope keypair.**

```bash
# Worker work-item encryption/decryption
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out worker-private.pem
openssl pkey -in worker-private.pem -pubout -out worker-public.pem

# Client dispatch signing/verification
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out client-signing-private.pem
openssl pkey -in client-signing-private.pem -pubout -out client-signing-public.pem

# Client result encryption/decryption
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out client-result-private.pem
openssl pkey -in client-result-private.pem -pubout -out client-result-public.pem
```

Keep `client-signing-private.pem`, `client-result-private.pem`, and `worker-private.pem` outside the repository. For Serverless, place the worker private key in MysteryBox; the two client private keys remain only on the trusted machine running the live probe/client.

## Explicit live research probe

`--live-research` is intentionally opt-in because it creates real Nebius Serverless Jobs and can consume Nebius, Token Factory, Tavily, and Object Storage resources. It constructs the production path through `NebiusResearchLiveRuntimeFactory`, therefore both the zero-cost live preflight and the existing deployment preflight remain mandatory.

The probe creates a synthetic non-private research job and executes the complete three-stage durable research workflow remotely:

1. `research.requested.v1 -> research.planned.v1` using Nemotron planning.
2. `research.planned.v1 -> research.evidence.v1` using Tavily Search/Extract and evidence preparation.
3. `research.evidence.v1 -> research.completed.v1` using Nemotron synthesis.

Each stage uses the production two-phase dispatch path: encrypted work-item publication, durable `DispatchReserved`, Nebius create, authoritative remote-ID attachment, signed dispatch binding, worker execution, encrypted result return, exact-once local ingestion, and terminal binding cleanup. The run fails unless the final report contains both evidence and validated citations.

### Native Object Storage topology

The client uses `NebiusObjectStorageClient` + `S3ProtectedResearchTransport` to access the dedicated Nebius Object Storage bucket directly through the S3-compatible API. The worker uses `DirectoryProtectedResearchTransport` against the same bucket mounted `READ_WRITE` by Serverless.

The live probe fails before any Serverless submission unless the mapping is exact:

- `NVIDEA_LIVE_OBJECT_STORAGE_BUCKET` must exactly equal `NVIDEA_LIVE_TRANSPORT_SOURCE`.
- `NVIDEA_LIVE_OBJECT_STORAGE_PREFIX` must exactly equal `NVIDEA_LIVE_TRANSPORT_SOURCE_PATH` after conservative slash normalization.
- `NVIDEA_LIVE_WORKER_TRANSPORT_ROOT` must exactly equal the `READ_WRITE` volume `ContainerPath` through the production deployment preflight.

For the default prefix `nvidea-research`, the client writes objects such as `nvidea-research/work-items/<opaque-id>.json`. The Serverless volume mounts `SourcePath=nvidea-research` at `/mnt/nvidea-research`, so the worker sees that same object as `/mnt/nvidea-research/work-items/<opaque-id>.json`. The same mapping applies independently to `dispatch-bindings/` and `results/`.

Do not configure a broader bucket root on one side and a nested prefix on the other. NVIDEA deliberately rejects that ambiguity instead of waiting for a remote timeout.

### Required live configuration

Both live modes fail when any required setting is absent or malformed:

- `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN` — local credential used only by the client to call the Nebius Serverless Jobs API. The zero-cost preflight validates presence/shape but does not call the API.
- `NVIDEA_LIVE_SERVERLESS_PROJECT_ID` — Nebius project/parent id.
- `NVIDEA_LIVE_WORKER_IMAGE` — deployed NVIDEA worker image, required in immutable `registry/path@sha256:<digest>` form.
- `NVIDEA_LIVE_SUBNET_ID` — explicit Serverless subnet.
- `NVIDEA_LIVE_PLATFORM` — provider platform value.
- `NVIDEA_LIVE_PRESET` — provider compute preset.
- `NVIDEA_LIVE_TIMEOUT` — provider job timeout value.
- `NVIDEA_LIVE_DISK_TYPE` and `NVIDEA_LIVE_DISK_SIZE_BYTES` — explicit positive Serverless disk configuration.
- `NVIDEA_LIVE_TRANSPORT_SOURCE` — dedicated Nebius Object Storage bucket name mounted by Serverless. It must equal `NVIDEA_LIVE_OBJECT_STORAGE_BUCKET`.
- `NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT` — HTTPS S3-compatible Nebius Object Storage endpoint for the bucket's region.
- `NVIDEA_LIVE_OBJECT_STORAGE_REGION` — region used for S3 request signing.
- `NVIDEA_LIVE_OBJECT_STORAGE_BUCKET` — dedicated research bucket used by the native client. It must equal the Serverless volume source.
- `NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID` — local static Object Storage access key id for a least-privilege service account.
- `NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY` — matching local static Object Storage secret key. Never commit it or place it in command-line arguments.
- `NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE` — local file containing the worker RSA public key corresponding to the worker private key stored in MysteryBox.
- `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` — local file containing the client **dispatch-signing** RSA private key. Its derived public half becomes worker `NVIDEA_CLIENT_PUBLIC_KEY_PEM`.
- `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE` — local file containing the distinct client **result-decryption** RSA private key. Its derived public half becomes worker `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`.
- `NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID` — MysteryBox secret id injected as worker `NEBIUS_API_KEY`.
- `NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID` — MysteryBox secret id injected as worker `TAVILY_API_KEY`.
- `NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID` — MysteryBox secret id injected as worker `NVIDEA_WORKER_PRIVATE_KEY_PEM`.

The live loader derives and canonicalizes the two client public identities in memory. Production composition rejects missing/weak/malformed key material, public/private role confusion, and reuse of the same RSA identity across dispatch signing and result encryption.

Optional settings:

- `NVIDEA_LIVE_WORKER_TRANSPORT_ROOT` (default `/mnt/nvidea-research`).
- `NVIDEA_LIVE_OBJECT_STORAGE_PREFIX` (default `nvidea-research`).
- `NVIDEA_LIVE_TRANSPORT_SOURCE_PATH` (defaults to `NVIDEA_LIVE_OBJECT_STORAGE_PREFIX`; an explicit different value is rejected).
- `NVIDEA_LIVE_POLL_SECONDS` (1-30, default 5; used only by the real live probe).
- `NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES` (2-60, default 20; used only by the real live probe).
- `NVIDEA_LIVE_RESEARCH_QUESTION` (synthetic/default question is used when omitted; used only by the real live probe).
- `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` — optional file in an existing directory for the redacted deployment manifest.
- `NVIDEA_LIVE_PASS_EVIDENCE_PATH` — optional file in an existing directory for redacted machine-readable evidence. The path is validated before any paid/provider operation, but the file is created or replaced atomically only after every remote stage completes and the final report has both evidence and validated citations. Preflight, failure, timeout, and cancellation paths never write this PASS artifact.

Run the real contract:

```bash
dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj -- --live-research
```

Use `--help` to list modes. The cheap planner probe remains the default, so CI or accidental local invocations do not create Serverless jobs.

## Worker-visible key contract

Serverless worker configuration contains only public client identities:

- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` — dispatch-binding verification only.
- `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` — result-envelope encryption only.

These must be distinct canonical RSA public identities. Neither `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` nor `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE` is copied into worker configuration.

## Machine-readable live PASS evidence

When `NVIDEA_LIVE_PASS_EVIDENCE_PATH` is configured, a successful live run persists schema `nvidea.nebius.research-pass.v1` only after final research validation. The artifact contains only the deployment SHA-256 fingerprint, UTC completion timestamp, completed remote-stage count, evidence-item count, and validated-citation count.

The PASS file deliberately excludes Serverless job/resource ids, bucket names, Object Storage credentials, MysteryBox ids/version ids, PEM material, URLs, research text, evidence bodies, and provider responses. It is written through the same-directory temporary-file + flush + atomic replace primitive in `NebiusResearchPassEvidenceBuilder`, so the final path is not written incrementally.

The live probe prints whether evidence persistence was requested and completed, but never prints the configured output path. If PASS evidence persistence itself fails, the probe fails before printing `NVIDEA live Nebius research contract probe: PASS`.

## Credential and data handling

The Object Storage static key is a local client credential, not a Serverless worker credential. Give it only the bucket permissions required for protected work-item/binding/result create, read, and delete operations. Keep it in a short-lived local environment or OS secret store for the probe; never commit it. NVIDEA's storage errors are sanitized and do not print the bucket, object key, endpoint, access-key id, provider response body, protected payload, or secret value.

Worker `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM` remain MysteryBox-backed references and are validated by `NebiusResearchDeploymentPreflight`. Both client RSA private keys remain local. Object Storage contains encrypted work/result envelopes and signed binding metadata rather than research plaintext.

The dry-run validator never resolves MysteryBox values, never persists PEM material, and validates/canonicalizes the signing and result public identities before comparing them for forbidden key reuse.

## Safety and evidence

The live mode rejects private-OS-data dispatch, uses an explicit cloud authorization scoped to each synthetic stage, bounds polling and total runtime, requires MysteryBox references for worker credentials through the production preflight, validates native-S3/mounted-volume alignment before submission, requires a digest-pinned worker image, and does not print secret values, protected research payloads, research evidence bodies, binding contents, or provider response bodies.

Nebius documents Serverless job images in either `registry/path:tag` or `registry/path@digest` form. NVIDEA intentionally chooses the digest form for the competition/live contract so the deployed worker cannot drift between preflight and execution.

A live PASS is materially stronger than source-level tests or a dry-run PASS: it proves that native client Object Storage access, Serverless mounted-prefix resolution, authoritative Nebius ID handoff, deployed worker, Nemotron, Tavily, encrypted result protocol, lifecycle reconciliation, exact-once ingestion, and final citation-bearing report all cooperated in one bounded real run. Until such a PASS is observed, WPF should continue to avoid claiming production Serverless research readiness.
