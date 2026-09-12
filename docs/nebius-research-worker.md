# Nebius research worker

NVIDEA's remote research worker is a real `.NET 8` executable at `src/Nvidea.Worker` with a container definition at `src/Nvidea.Worker/Dockerfile`. It executes exactly one durable `ResearchJobHandler` stage using NVIDIA Nemotron through Nebius Token Factory and Tavily, then publishes only an encrypted `ProtectedResearchResultEnvelope`.

## Storage boundary

`DirectoryProtectedResearchTransport` stores protected research objects under separate namespaces:

- `<root>/work-items/<opaque-id>.json` — encrypted work-item envelope.
- `<root>/dispatch-bindings/<opaque-id>.json` — client-signed Nebius resource-ID binding with no research payload.
- `<root>/results/<opaque-id>.json` — encrypted result envelope.

For Nebius Serverless, mount a dedicated Object Storage bucket or supported filesystem read/write at a path such as `/mnt/nvidea-research` and set `NVIDEA_TRANSPORT_ROOT` to the exact same container path. Keep the backing storage dedicated to the research transport and configure a provider-side lifecycle rule as defense in depth in addition to NVIDEA's protocol TTL.

The client models the current Nebius `spec.volumes[]` contract explicitly with `NebiusServerlessVolumeMount`: `source`, optional `sourcePath`, absolute `containerPath`, and provider mode `READ_WRITE` or `READ_ONLY`. `NebiusResearchDispatchOptions.Volumes` is passed through both the legacy dispatcher and the crash-safe `TwoPhaseNebiusResearchDispatcher`. Invalid relative paths, duplicate container paths, unsupported modes, oversized/control-character values, and malformed sources are rejected before network I/O.

For the production research worker the transport mount must be `READ_WRITE`, because the worker consumes the encrypted work item and signed binding and then publishes the encrypted result. A read-only mount is useful only for workloads that never publish transport state.

All three transport namespaces use create-once publication and temp-file + move semantics. Opaque IDs are constrained before becoming filenames. Work-item/result bodies remain encrypted; dispatch bindings contain control-plane identity only and are RSA-PSS/SHA-256 signed by the originating client.

## Cryptographic identities

Remote research deliberately uses **three separate RSA identities**. Do not reuse any of these keypairs for another role:

1. **Worker envelope keypair** — the client encrypts protected work items with the worker public key; the worker decrypts them with `NVIDEA_WORKER_PRIVATE_KEY_PEM`.
2. **Client dispatch-signing keypair** — the client signs authoritative dispatch bindings; the worker verifies them with `NVIDEA_CLIENT_PUBLIC_KEY_PEM`.
3. **Client result-envelope keypair** — the worker encrypts protected results to `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`; the Windows client decrypts them with its separate local result private key.

Production validation requires RSA keys of at least 2048 bits, strict public/private role separation, and distinct client signing/result identities. Reusing the same client RSA keypair for signing and result encryption is rejected.

A simple OpenSSL setup for local development is:

```bash
# 1) Worker work-item encryption/decryption identity
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out worker-private.pem
openssl pkey -in worker-private.pem -pubout -out worker-public.pem

# 2) Client dispatch signing/verification identity
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out client-signing-private.pem
openssl pkey -in client-signing-private.pem -pubout -out client-signing-public.pem

# 3) Client result encryption/decryption identity
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out client-result-private.pem
openssl pkey -in client-result-private.pem -pubout -out client-result-public.pem
```

Keep all private PEM files outside the repository. For real Serverless deployment, place the worker private key in Nebius MysteryBox and keep both client private keys only on the originating trusted client machine.

## Required secrets/configuration

Inject worker secrets with Nebius MysteryBox rather than plaintext job environment configuration:

- `NEBIUS_API_KEY` — Token Factory key.
- `TAVILY_API_KEY` — Tavily key.
- `NVIDEA_WORKER_PRIVATE_KEY_PEM` — worker RSA private key used only to unwrap incoming protected work items.

Supply these two **public-only** client identities to the worker as non-secret configuration:

- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` — client dispatch-signing public key used only to verify signed dispatch bindings.
- `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` — distinct client result-envelope public key used only to encrypt protected results returned to the client.

The corresponding client private keys stay on the originating Windows client:

- `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` points to the dispatch-signing private key file.
- `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE` points to the separate result-decryption private key file.

Neither client private key may be injected into the Serverless worker. The deployment preflight and worker bootstrap reject missing, malformed, weak, role-confused, or reused client public identities.

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

## Live deployment preflight

Production composition should use `NebiusResearchLiveRuntimeFactory.Create`, not the lower-level fixture-oriented `NebiusResearchClientRuntime.Create`. The live factory runs `NebiusResearchDeploymentPreflight` before constructing the remote runtime and fails before Serverless submission unless all of these invariants hold:

1. `NVIDEA_TRANSPORT_ROOT` is a bounded absolute Linux path.
2. Exactly one configured volume has a `ContainerPath` equal to that transport root.
3. That matching research transport volume is `READ_WRITE`.
4. `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM` are present as Nebius MysteryBox secret references, never plaintext environment values.
5. `NVIDEA_CLIENT_PUBLIC_KEY_PEM` is a validated public-only RSA identity for dispatch verification.
6. `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` is a validated public-only RSA identity for result encryption.
7. The canonical dispatch-verification and result-encryption client identities are different RSA keys.

The validator checks only secret references and topology; it never resolves or logs secret values. This is intentionally separate from generic Serverless request validation: unit/contract fixtures can continue testing lower-level serialization without pretending to be production-ready, while the live runtime has a single fail-fast gate.

## Authoritative job-ID handoff

Nebius job creation returns the authoritative resource ID only after the job has been created. NVIDEA does not guess this value and does not substitute the deterministic job name.

The handoff protocol is implemented in `ResearchDispatchBinding.cs` and is wired into the durable client lifecycle:

1. The Windows side encrypts/uploads a work item and persists `DispatchReserved` before Nebius creation.
2. Nebius returns an authoritative remote resource ID.
3. `RemoteResearchResultIngestor.AttachDispatchAsync` CAS-attaches that ID to the exact durable reservation.
4. Only after attachment succeeds, `TwoPhaseNebiusResearchDispatcher` invokes `ResearchDispatchBindingPublisher`.
5. The publisher signs `opaque-id + remote resource id + deterministic job name + timestamps` with the originating client's dispatch-signing RSA private key using RSA-PSS/SHA-256 and create-once publishes it to `dispatch-bindings/<opaque-id>.json`.
6. If a process dies after Nebius accepted creation but before binding publication, `NebiusResearchLifecycleReconciler.ReconcileReservedAsync` recovers and directly verifies the authoritative resource, attaches it durably, then publishes the binding.
7. `ReconcileDispatchedAsync` idempotently ensures the exact signed binding exists before reading provider lifecycle state. An existing valid binding for the same resource is accepted; a conflicting binding fails closed before provider-state mutation.
8. The worker waits boundedly for that exact opaque ID, verifies protocol version, opaque ID, deterministic name, expiry, and client signature using `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, then passes only the verified resource ID into `NebiusResearchWorker`.
9. The worker encrypts the returned result to the **separate** `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`; `ResearchResultProtector` authenticates that result to the exact remote resource ID.
10. The originating client decrypts with its local result private key and `NebiusResearchClientRuntime` performs best-effort signed-binding cleanup only after durable local state proves the stage terminal or its verified result applied; active dispatch/cancellation states retain the binding.

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

The client and worker share an explicit mount contract in code, and live runtime construction has a fail-fast topology/secret-reference gate. WPF still does not expose Serverless research because a live Nebius/Object Storage/MysteryBox/container contract probe has not yet succeeded in this environment.

Current Nebius lifecycle metadata also includes legitimate provider transition states beyond NVIDEA's current parser set (`IMAGE_PULLING` and `DELETING`). Those must be mapped conservatively before live reconciliation is considered ready; unknown future states should continue to fail closed.

The next proof must use a real mounted transport and immutable worker image to validate the complete path rather than assuming that provider-side storage/auth configuration is correct.

## Container build

From the repository root:

```text
docker build -f src/Nvidea.Worker/Dockerfile -t <registry>/nvidea-research-worker:<tag> .
```

Use an immutable image digest for judging/demo deployment. Do not bake API keys or RSA private keys into the image.
