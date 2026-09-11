# Desktop Nebius remote-research lifecycle

The Windows desktop is **local-only by default**. Existing durable research continues to use local Nemotron + Tavily unless cloud lifecycle support is explicitly enabled.

## Why lifecycle and dispatch are separate

NVIDEA treats recovery of an already-remote durable job as a different authority from starting new paid cloud work. This prevents a user who only wants to reconcile or cancel a persisted Nebius Serverless stage from implicitly enabling future dispatch.

- `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true` enables provider-aware reconciliation and cancellation for persisted remote research.
- `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true` additionally permits the `ResearchProductRuntime` remote-dispatch path. It is rejected unless lifecycle support is also enabled.
- Omitting both variables keeps the desktop local-only.

`TAVILY_API_KEY` is required for **local research execution and all new Serverless dispatch**, but it is no longer required merely to recover an already-dispatched Nebius job. When lifecycle support is enabled and its Nebius deployment configuration passes preflight, the desktop can compose a lifecycle-only research surface with no Tavily credential. That surface can inspect durable research state, reconcile remote/ambiguous jobs, and request provider-aware cancellation. It cannot create, resume, re-arm, cancel local jobs, read completed local reports, or start any new cloud dispatch until the local research runtime is restored.

## Fail-closed provider startup

When remote lifecycle is enabled, desktop startup loads the same `NebiusResearchLiveConfiguration` used by the live contract probe. That configuration validates the Serverless deployment contract, Object Storage mapping, digest-pinned worker image, MysteryBox secret references, RSA signing identity, bounded compute/disk settings, and related live configuration before the desktop creates provider clients.

Immediately before provider construction, `NebiusResearchLiveProviderStartup` revalidates configured evidence-output destinations. Provider construction is therefore not reached when the live deployment configuration or final local artifact destinations fail validation.

The desktop then privately composes:

1. `NebiusObjectStorageClient` and `S3ProtectedResearchTransport` for encrypted work items, signed bindings, and protected results.
2. `NebiusServerlessJobClient` for provider lifecycle operations.
3. `NebiusResearchClientRuntime` for two-phase dispatch, reconciliation, exact-once result ingestion, and cancellation.
4. `ResearchCloudExecutionCoordinator` for the product-facing lease/provenance/authorization boundary.
5. `ResearchProductRuntime` as the only research authority exposed to WPF. Its local executor is optional specifically for recovery-only composition.

Raw provider clients, access tokens, Object Storage credentials, signing private keys, and mutable transport handles are not exposed through `NvideaCompositionRoot`.

## Desktop recovery behavior

With lifecycle enabled and new dispatch still disabled, the existing WPF research controls can safely:

- reconcile a `DispatchReserved` record without assuming whether Nebius accepted the create operation;
- reconcile an actively dispatched stage and apply a cryptographically verified result exactly once;
- request cancellation of an actively dispatched Nebius stage;
- reconcile an outstanding cancellation request;
- continue blocking all local replay while unfinished remote provenance exists.

These lifecycle operations remain available when `TAVILY_API_KEY` is missing. In that recovery-only state the UI keeps Start, Resume, local cancellation, completed-report reads, and new Serverless dispatch locked rather than trying to reconstruct local provider authority.

The emergency stop cancels the current local reconciliation request, but cancellation of the HTTP request does **not** claim that the provider-side job stopped. Durable remote state remains authoritative and must be reconciled again.

## Enabling the lifecycle path

First configure and pass the existing zero-cost live deployment preflight described in `docs/nebius-contract-probe.md`. Keep all secret values outside the repository.

Then set:

```text
NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true
```

If the purpose is only to recover existing remote work, `TAVILY_API_KEY` may be absent and `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH` should remain unset/false.

Do not set `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true` merely to recover existing remote work. Enable it only when `TAVILY_API_KEY` is present, local durable research is available, the product intentionally supports new Serverless dispatch, and the per-stage `ResearchCloudAuthorization` approval path is being used.

## Safety invariants

- Remote research containing private OS-local data remains ineligible for cloud dispatch.
- Approval-bearing stages cannot be dispatched remotely.
- Remote/ambiguous records cannot fall back to local execution.
- Missing Tavily credentials do not strand already-remote durable work, but they do fail closed every local research mutation and every new cloud dispatch.
- A `DispatchReserved` outcome must be reconciled before cancellation or retry.
- Consequential provider transitions run under the shared research state-directory mutation lease.
- Object Storage and Serverless clients are disposed with the desktop composition root, including partial-startup failure paths.
- No credential or PEM material is written into audit summaries or surfaced through product APIs.
