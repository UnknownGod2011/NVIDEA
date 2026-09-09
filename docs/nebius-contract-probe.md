# Nebius contract probes

`tools/Nvidea.NebiusContractProbe` has two deliberately different modes: a cheap default Token Factory planner contract probe, and an explicit `--live-research` Serverless research probe.

## Default planner probe

The default mode checks whether the configured Nebius Token Factory Nemotron endpoint accepts the production strict JSON schema used by `NemotronBrowserPlanner` and returns output that survives NVIDEA's parser/validation boundary. It uses a synthetic browser observation, invokes no browser action, creates no durable remote job, and never prints provider response bodies or API keys.

Run from the repository root:

```bash
NEBIUS_API_KEY="..." dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj
```

Optional existing model/base-URL environment variables remain supported: `NVIDEA_NEBIUS_BASE_URL`, `NVIDEA_MODEL_STANDARD`, `NVIDEA_MODEL_FAST`, and `NVIDEA_MODEL_DEEP`.

## Explicit live research probe

`--live-research` is intentionally opt-in because it creates real Nebius Serverless Jobs and can consume Nebius, Token Factory, Tavily, and Object Storage resources. It constructs the production path through `NebiusResearchLiveRuntimeFactory`, therefore the existing deployment preflight remains mandatory.

The probe creates a synthetic non-private research job and executes the complete three-stage durable research workflow remotely:

1. `research.requested.v1 -> research.planned.v1` using Nemotron planning.
2. `research.planned.v1 -> research.evidence.v1` using Tavily Search/Extract and evidence preparation.
3. `research.evidence.v1 -> research.completed.v1` using Nemotron synthesis.

Each stage uses the production two-phase dispatch path: encrypted work-item publication, durable `DispatchReserved`, Nebius create, authoritative remote-ID attachment, signed dispatch binding, worker execution, encrypted result return, exact-once local ingestion, and terminal binding cleanup. The run fails unless the final report contains both evidence and validated citations.

### Native Object Storage topology

The client no longer needs a host-mounted copy of the worker's storage. It uses `NebiusObjectStorageClient` + `S3ProtectedResearchTransport` to access the dedicated Nebius Object Storage bucket directly through the S3-compatible API. The worker still uses `DirectoryProtectedResearchTransport` against the same bucket mounted `READ_WRITE` by Serverless.

The live probe fails before any Serverless submission unless the mapping is exact:

- `NVIDEA_LIVE_OBJECT_STORAGE_BUCKET` must exactly equal `NVIDEA_LIVE_TRANSPORT_SOURCE`.
- `NVIDEA_LIVE_OBJECT_STORAGE_PREFIX` must exactly equal `NVIDEA_LIVE_TRANSPORT_SOURCE_PATH` after conservative slash normalization.
- `NVIDEA_LIVE_WORKER_TRANSPORT_ROOT` must exactly equal the `READ_WRITE` volume `ContainerPath` through the production deployment preflight.

For the default prefix `nvidea-research`, the client writes objects such as `nvidea-research/work-items/<opaque-id>.json`. The Serverless volume mounts `SourcePath=nvidea-research` at `/mnt/nvidea-research`, so the worker sees that same object as `/mnt/nvidea-research/work-items/<opaque-id>.json`. The same mapping applies independently to `dispatch-bindings/` and `results/`.

Do not configure a broader bucket root on one side and a nested prefix on the other. NVIDEA deliberately rejects that ambiguity instead of waiting for a remote timeout.

### Required live configuration

The probe fails before submission when any required setting is absent or malformed:

- `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN` — local credential used only by the client to call the Nebius Serverless Jobs API.
- `NVIDEA_LIVE_SERVERLESS_PROJECT_ID` — Nebius project/parent id.
- `NVIDEA_LIVE_WORKER_IMAGE` — deployed NVIDEA worker image; use an immutable digest for judging/demo runs.
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
- `NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE` — local file containing the worker public key corresponding to the worker private key stored in MysteryBox.
- `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` — local client signing/decryption private key. The probe derives the public verification key in memory; it never injects this private key into Serverless.
- `NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID` — MysteryBox secret id injected as worker `NEBIUS_API_KEY`.
- `NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID` — MysteryBox secret id injected as worker `TAVILY_API_KEY`.
- `NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID` — MysteryBox secret id injected as worker `NVIDEA_WORKER_PRIVATE_KEY_PEM`.

Optional settings:

- `NVIDEA_LIVE_WORKER_TRANSPORT_ROOT` (default `/mnt/nvidea-research`).
- `NVIDEA_LIVE_OBJECT_STORAGE_PREFIX` (default `nvidea-research`).
- `NVIDEA_LIVE_TRANSPORT_SOURCE_PATH` (defaults to `NVIDEA_LIVE_OBJECT_STORAGE_PREFIX`; an explicit different value is rejected).
- `NVIDEA_LIVE_POLL_SECONDS` (1-30, default 5).
- `NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES` (2-60, default 20).
- `NVIDEA_LIVE_RESEARCH_QUESTION` (synthetic/default question is used when omitted).

Run:

```bash
dotnet run --project tools/Nvidea.NebiusContractProbe/Nvidea.NebiusContractProbe.csproj -- --live-research
```

Use `--help` to list modes. The cheap planner probe remains the default, so CI or accidental local invocations do not create Serverless jobs.

## Credential and data handling

The Object Storage static key is a local client credential, not a Serverless worker credential. Give it only the bucket permissions required for protected work-item/binding/result create, read, and delete operations. Keep it in a short-lived local environment or OS secret store for the probe; never commit it. NVIDEA's storage errors are sanitized and do not print the bucket, object key, endpoint, access-key id, provider response body, protected payload, or secret value.

Worker `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM` remain MysteryBox-backed references and are validated by `NebiusResearchDeploymentPreflight`. The client RSA private key remains local. Object Storage contains encrypted work/result envelopes and signed binding metadata rather than research plaintext.

## Safety and evidence

The live mode rejects private-OS-data dispatch, uses an explicit cloud authorization scoped to each synthetic stage, bounds polling and total runtime, requires MysteryBox references for worker credentials through the production preflight, validates native-S3/mounted-volume alignment before submission, and does not print secret values, protected research payloads, research evidence bodies, binding contents, or provider response bodies.

A live PASS is materially stronger than source-level tests: it proves that native client Object Storage access, Serverless mounted-prefix resolution, authoritative Nebius ID handoff, deployed worker, Nemotron, Tavily, encrypted result protocol, lifecycle reconciliation, exact-once ingestion, and final citation-bearing report all cooperated in one bounded real run. Until such a PASS is observed, WPF should continue to avoid claiming production Serverless research readiness.
