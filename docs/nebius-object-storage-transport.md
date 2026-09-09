# Native Nebius Object Storage transport

NVIDEA now includes a client-side S3-compatible transport for protected remote-research envelopes in `NebiusObjectStorageProtectedResearchTransport.cs`. This removes the architectural requirement for the Windows client to mount the same bucket as the Serverless worker.

## Why this exists

The worker can consume a Nebius-mounted Object Storage bucket as a filesystem through `DirectoryProtectedResearchTransport`, but requiring Windows to mount that same bucket externally is fragile and unsuitable for normal desktop onboarding. The native client path instead talks directly to Nebius Object Storage over its S3-compatible API while preserving the exact protected envelope protocol already used by the worker.

The transport still stores only three categories:

- `work-items/<opaque-id>.json` — encrypted work-item envelopes.
- `dispatch-bindings/<opaque-id>.json` — client-signed authoritative Nebius resource-ID bindings.
- `results/<opaque-id>.json` — encrypted result envelopes.

`S3ProtectedResearchTransport` is independent of the AWS SDK and depends only on `IProtectedResearchObjectStoreClient`. `NebiusObjectStorageClient` is the production S3-compatible implementation.

## Security and failure invariants

- Writes are create-once. `PutObject` uses `If-None-Match: *`; a pre-existing object fails closed and is never overwritten.
- A provider `409 ConditionalRequestConflict` receives only a small bounded retry sequence. Exhaustion fails closed rather than guessing whether a conflicting write succeeded.
- Each object read is bounded to 4 MiB before JSON parsing, including a streaming byte-count check in addition to the response content length.
- Work items, results, and bindings remain in separate prefixes so cleanup of one category cannot accidentally target another.
- Opaque work-item IDs are validated before becoming object keys.
- Optional top-level key prefixes are bounded and reject traversal/backslash/control-character forms.
- Every S3 call has a local cancellation-backed deadline. SDK retries are disabled so the NVIDEA retry budget is explicit.
- Errors returned by the production client contain only the operation and HTTP status. Bucket names, endpoints, object keys, access-key IDs, provider response bodies, and secret values are not included.
- Delete is idempotent.

The encrypted work-item/result protocols and signed dispatch-binding protocol remain the confidentiality/integrity boundary. Object Storage is durable transport, not trusted plaintext storage.

## Credentials

Nebius Object Storage uses S3-compatible static keys issued to a service account. Use a dedicated least-privilege service account scoped to the research bucket. The client requires:

- HTTPS Object Storage endpoint, e.g. `https://storage.eu-north1.nebius.cloud`.
- region, e.g. `eu-north1`.
- dedicated bucket name.
- static access-key ID.
- static secret access key.

Never commit these values. For the Windows app they should ultimately come from an OS-backed secret store/credential broker, not from checked-in configuration. For one-off contract probing, local process environment variables or a non-committed secret launcher are acceptable as long as values are never printed.

The Serverless worker continues to receive Token Factory/Tavily/RSA worker credentials through MysteryBox and does not need the Windows client's Object Storage secret.

## Package/runtime choice

The client uses AWS SDK for .NET v4 (`AWSSDK.S3`) because Nebius Object Storage exposes an S3-compatible interface. Version 4 is used rather than v3 because AWS SDK for .NET v3 reached end of support in 2026.

## Deployment topology

The intended topology is now:

```text
Windows client
  -> S3 API -> dedicated Nebius Object Storage bucket
                    | work-items/
                    | dispatch-bindings/
                    | results/
                    v
              Serverless bucket mount
                    v
             Nvidea.Worker
```

The worker may continue using `DirectoryProtectedResearchTransport` against its mounted bucket. The Windows side uses `S3ProtectedResearchTransport(new NebiusObjectStorageClient(...))` against the same bucket.

A prefix-to-mounted-path mapping must still be proven with the actual Nebius Serverless bucket mount used for the live probe. Do not claim the full end-to-end contract until the same object written through S3 is visibly available to the mounted worker path and vice versa.

## Required validation before UI exposure

1. Compile and run `S3ProtectedResearchTransportTests`.
2. Verify a dedicated test bucket accepts conditional `If-None-Match: *` writes through Nebius' S3-compatible endpoint.
3. Verify one S3-written protected object appears at the expected path in a Serverless mounted bucket.
4. Wire the native transport into `Nvidea.NebiusContractProbe --live-research`.
5. Run the complete encrypted dispatch -> signed binding -> worker -> encrypted result -> exact-once ingestion -> cleanup path.
6. Keep WPF Serverless controls hidden until that probe passes.
