# Nebius research worker

NVIDEA's remote research worker is a real `.NET 8` executable at `src/Nvidea.Worker` with a container definition at `src/Nvidea.Worker/Dockerfile`. It executes exactly one durable `ResearchJobHandler` stage using NVIDIA Nemotron through Nebius Token Factory and Tavily, then publishes only an encrypted `ProtectedResearchResultEnvelope`.

## Storage boundary

`DirectoryProtectedResearchTransport` stores only already-encrypted work-item/result envelopes under:

- `<root>/work-items/<opaque-id>.json`
- `<root>/results/<opaque-id>.json`

For Nebius Serverless, mount a dedicated Object Storage bucket read/write at a path such as `/nvidea-transport` and set `NVIDEA_TRANSPORT_ROOT=/nvidea-transport`. Nebius Serverless Jobs currently support Object Storage bucket mounts via `--volume`; credentials can be supplied with a MysteryBox-backed profile. Keep this bucket dedicated to the research transport and configure a provider-side lifecycle rule as defense in depth in addition to NVIDEA's protocol TTL.

## Required secrets/configuration

Inject secrets with Nebius MysteryBox rather than plaintext job environment configuration:

- `NEBIUS_API_KEY` — Token Factory key.
- `TAVILY_API_KEY` — Tavily key.
- `NVIDEA_WORKER_PRIVATE_KEY_PEM` — RSA private key used only to unwrap incoming protected work items.
- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` — client public key used to protect returned results.

Non-secret configuration:

- `NVIDEA_TRANSPORT_ROOT` — absolute mounted transport directory.
- `NVIDEA_REMOTE_JOB_ID` — exact Nebius Serverless job resource ID used as authenticated result provenance.
- optional `NVIDEA_MODEL_STANDARD`, `NVIDEA_MODEL_FAST`, `NVIDEA_MODEL_DEEP`.

Worker command:

```text
Nvidea.Worker.dll research --work-item-id=<opaque-id>
```

The worker deliberately logs only `nvidea_worker_completed stage=research` or `nvidea_worker_failed error_type=<type>`; it does not print the research question, evidence, secret values, or provider response bodies.

## Known production blocker: job-ID handoff

Nebius job creation returns the authoritative resource ID only after the job has been created. Current official Serverless Jobs documentation does not document an automatically injected environment variable that exposes the current job resource ID inside the container. NVIDEA therefore **must not guess this value or substitute the deterministic job name**.

Before `TwoPhaseNebiusResearchDispatcher` can launch this worker automatically, add a durable dispatch-binding object keyed by the opaque work-item ID. The Windows client should publish the exact resource ID only after `AttachDispatchAsync` succeeds (and reconciliation should publish it after recovering a crash). The worker must wait boundedly for that binding, verify it belongs to the same opaque work item, and only then execute/publish its encrypted result. This preserves the existing exact remote-ID authentication in `ResearchResultProtector`.

Until that bridge is implemented and live-tested, the worker image is deployable and locally/container testable, but the Windows UI must not advertise Serverless research as production-ready.

## Container build

From the repository root:

```text
docker build -f src/Nvidea.Worker/Dockerfile -t <registry>/nvidea-research-worker:<tag> .
```

Use an immutable image digest for judging/demo deployment. Do not bake API keys or RSA private keys into the image.
