# Nebius research worker

NVIDEA's remote research worker is a real `.NET 8` executable at `src/Nvidea.Worker` with a container definition at `src/Nvidea.Worker/Dockerfile`. It executes exactly one durable `ResearchJobHandler` stage using NVIDIA Nemotron through Nebius Token Factory and Tavily, then publishes only an encrypted `ProtectedResearchResultEnvelope`.

## Storage boundary

`DirectoryProtectedResearchTransport` stores protected research objects under separate namespaces:

- `<root>/work-items/<opaque-id>.json` — encrypted work-item envelope.
- `<root>/dispatch-bindings/<opaque-id>.json` — client-signed Nebius resource-ID binding with no research payload.
- `<root>/results/<opaque-id>.json` — encrypted result envelope.

For Nebius Serverless, mount a dedicated Object Storage bucket read/write at a path such as `/nvidea-transport` and set `NVIDEA_TRANSPORT_ROOT=/nvidea-transport`. Keep this bucket dedicated to the research transport and configure a provider-side lifecycle rule as defense in depth in addition to NVIDEA's protocol TTL.

All three transport namespaces use create-once publication and temp-file + move semantics. Opaque IDs are constrained before becoming filenames. Work-item/result bodies remain encrypted; dispatch bindings contain control-plane identity only and are RSA-PSS/SHA-256 signed by the originating client.

## Required secrets/configuration

Inject worker secrets with Nebius MysteryBox rather than plaintext job environment configuration:

- `NEBIUS_API_KEY` — Token Factory key.
- `TAVILY_API_KEY` — Tavily key.
- `NVIDEA_WORKER_PRIVATE_KEY_PEM` — RSA private key used only to unwrap incoming protected work items.
- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` — pinned client public key used both to verify signed dispatch bindings and to protect returned results.

The corresponding client private key stays on the originating Windows client. It is used by `ResearchDispatchBindingPublisher` to sign the authoritative Nebius resource-ID handoff and must never be injected into the Serverless worker.

Non-secret worker configuration:

- `NVIDEA_TRANSPORT_ROOT` — absolute mounted transport directory.
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

The handoff protocol is now implemented in `ResearchDispatchBinding.cs`:

1. The Windows side obtains the authoritative remote resource ID from normal two-phase dispatch attachment or crash reconciliation.
2. `ResearchDispatchBindingPublisher` signs `opaque-id + remote resource id + deterministic job name + timestamps` with the originating client's RSA private key using RSA-PSS/SHA-256.
3. The signed binding is create-once published to `dispatch-bindings/<opaque-id>.json`.
4. The worker waits boundedly for that exact opaque ID, verifies the protocol version, opaque ID, deterministic name, expiry, and client signature using the already-pinned client public key, then passes only the verified resource ID into `NebiusResearchWorker`.
5. The returned encrypted result remains authenticated to that exact remote resource ID by `ResearchResultProtector`.

Publication is idempotent only when an existing binding is validly signed and targets the same authoritative resource ID. A conflicting binding fails closed rather than being overwritten.

### Remaining integration boundary

The binding protocol, mounted transport implementation, publisher, bounded worker waiter, and worker consumption path now exist. The next integration step is to invoke `ResearchDispatchBindingPublisher` automatically immediately after `AttachDispatchAsync`, and from lifecycle reconciliation after recovering a crash-window reservation. Reconciliation should also republish idempotently for an already attached dispatch when the binding is missing. Until that producer wiring and a live Serverless end-to-end probe succeed, WPF must not advertise Serverless research as production-ready.

## Container build

From the repository root:

```text
docker build -f src/Nvidea.Worker/Dockerfile -t <registry>/nvidea-research-worker:<tag> .
```

Use an immutable image digest for judging/demo deployment. Do not bake API keys or RSA private keys into the image.
