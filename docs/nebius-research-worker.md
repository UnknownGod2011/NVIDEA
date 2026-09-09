# Nebius research worker

NVIDEA's remote research worker is a real `.NET 8` executable at `src/Nvidea.Worker` with a container definition at `src/Nvidea.Worker/Dockerfile`. It executes exactly one durable `ResearchJobHandler` stage using NVIDIA Nemotron through Nebius Token Factory and Tavily, then publishes only an encrypted `ProtectedResearchResultEnvelope`.

## Storage boundary

`DirectoryProtectedResearchTransport` stores protected research objects under separate namespaces:

- `<root>/work-items/<opaque-id>.json` — encrypted work-item envelope.
- `<root>/dispatch-bindings/<opaque-id>.json` — client-signed Nebius resource-ID binding with no research payload.
- `<root>/results/<opaque-id>.json` — encrypted result envelope.

For Nebius Serverless, mount a dedicated Object Storage bucket or supported filesystem read/write at a path such as `/mnt/nvidea-research` and set `NVIDEA_TRANSPORT_ROOT` to the exact same container path. Keep the backing storage dedicated to the research transport and configure a provider-side lifecycle rule as defense in depth in addition to NVIDEA's protocol TTL.

The client now models the current Nebius `spec.volumes[]` contract explicitly with `NebiusServerlessVolumeMount`: `source`, optional `sourcePath`, absolute `containerPath`, and provider mode `READ_WRITE` or `READ_ONLY`. `NebiusResearchDispatchOptions.Volumes` is passed through both the legacy dispatcher and the crash-safe `TwoPhaseNebiusResearchDispatcher`. Invalid relative paths, duplicate container paths, unsupported modes, oversized/control-character values, and malformed sources are rejected before network I/O.

For the production research worker the transport mount must be `READ_WRITE`, because the worker consumes the encrypted work item and signed binding and then publishes the encrypted result. A read-only mount is useful only for workloads that never publish transport state.

All three transport namespaces use create-once publication and temp-file + move semantics. Opaque IDs are constrained before becoming filenames. Work-item/result bodies remain encrypted; dispatch bindings contain control-plane identity only and are RSA-PSS/SHA-256 signed by the originating client.

## Required secrets/configuration

Inject worker secrets with Nebius MysteryBox rather than plaintext job environment configuration:

- `NEBIUS_API_KEY` — Token Factory key.
- `TAVILY_API_KEY` — Tavily key.
- `NVIDEA_WORKER_PRIVATE_KEY_PEM` — RSA private key used only to unwrap incoming protected work items.
- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` — pinned client public key used both to verify signed dispatch bindings and to protect returned results.

The corresponding client private key stays on the originating Windows client. It is used by `ResearchDispatchBindingPublisher` to sign the authoritative Nebius resource-ID handoff and must never be injected into the Serverless worker.

Non-secret worker configuration:

- `NVIDEA_TRANSPORT_ROOT` — absolute mounted transport directory; this must equal the configured `NebiusServerlessVolumeMount.ContainerPath`.
- optional `NVIDEA_BINDING_POLL_SECONDS` — bounded binding poll interval; defaults to 2 seconds and is constrained to 100 ms–30 seconds.
- optional `NVIDEA_BINDING_WAIT_SECONDS` — bounded total handoff wait; defaults to 5 minutes and is capped at 15 minutes.
- optional `NVIDEA_MODEL_STANDARD`, `NVIDEA_MODEL_FAST`, `NVIDEA_MODEL_DEEP`.

`NVIDEA_REMOTE_JOB_ID` is no longer required by the worker. The authoritative resource ID is resolved only through the signed dispatch-binding protocol after Serverless creation.

Worker command:

```text
Nvidea.Worker.dll research --work-item-id=<opaque-id>
```

The worker deliberately logs only `nvidea_worker_completed stage=research` or `nvidea_worker_failed error_type=<type>`; it does not print the research question, evidence, secret values, binding contents, or provider response bodies.

## Authoritative job-ID handoff

Nebius job creation returns the authoritative resource ID only after the job has been created. NVIDEA does not guess this value and does not substitute the deterministic job name.

The handoff protocol is implemented in `ResearchDispatchBinding.cs` and is wired into the durable client lifecycle:

1. The Windows side encrypts/uploads a work item and persists `DispatchReserved` before Nebius creation.
2. Nebius returns an authoritative remote resource ID.
3. `RemoteResearchResultIngestor.AttachDispatchAsync` CAS-attaches that ID to the exact durable reservation.
4. Only after attachment succeeds, `TwoPhaseNebiusResearchDispatcher` invokes `ResearchDispatchBindingPublisher`.
5. The publisher signs `opaque-id + remote resource id + deterministic job name + timestamps` with the originating client's RSA private key using RSA-PSS/SHA-256 and create-once publishes it to `dispatch-bindings/<opaque-id>.json`.
6. If a process dies after Nebius accepted creation but before binding publication, `NebiusResearchLifecycleReconciler.ReconcileReservedAsync` recovers and directly verifies the authoritative resource, attaches it durably, then publishes the binding.
7. `ReconcileDispatchedAsync` idempotently ensures the exact signed binding exists before reading provider lifecycle state. An existing valid binding for the same resource is accepted; a conflicting binding fails closed before provider-state mutation.
8. The worker waits boundedly for that exact opaque ID, verifies protocol version, opaque ID, deterministic name, expiry, and client signature using the pinned client public key, then passes only the verified resource ID into `NebiusResearchWorker`.
9. The returned encrypted result remains authenticated to that exact remote resource ID by `ResearchResultProtector`.
10. `NebiusResearchClientRuntime` performs best-effort signed-binding cleanup only after durable local state proves the stage terminal or its verified result applied; active dispatch/cancellation states retain the binding.

Publication is intentionally impossible before authoritative attachment. A binding conflict never triggers overwrite or fallback to the deterministic job name.

## Serverless job mount example

Conceptually, the job submitted by NVIDEA must contain the equivalent of:

```text
volume source=<dedicated-bucket-or-filesystem>
       containerPath=/mnt/nvidea-research
       mode=READ_WRITE
NVIDEA_TRANSPORT_ROOT=/mnt/nvidea-research
```

The exact JSON is emitted under `spec.volumes[]`; storage credentials, when required, belong in Nebius-managed secret configuration rather than plaintext NVIDEA job environment variables.

### Remaining integration boundary

The client and worker now share an explicit mount contract in code, so the earlier control-plane gap where `NebiusServerlessJobSpec` could not express any Object Storage/filesystem volume is closed. WPF still does not expose Serverless research because a live Nebius/Object Storage/MysteryBox/container contract probe has not yet succeeded in this environment.

The next proof must use a real mounted transport and immutable worker image to validate the complete path rather than assuming that provider-side storage/auth configuration is correct.

## Container build

From the repository root:

```text
docker build -f src/Nvidea.Worker/Dockerfile -t <registry>/nvidea-research-worker:<tag> .
```

Use an immutable image digest for judging/demo deployment. Do not bake API keys or RSA private keys into the image.
