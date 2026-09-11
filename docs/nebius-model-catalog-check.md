# Nebius model catalog drift check

`Nvidea.NebiusModelCatalogCheck` is a zero-inference readiness check for the Nemotron model IDs configured by NVIDEA. It exists to catch provider catalog drift before a demo or release without spending tokens on a chat completion.

Nebius Token Factory exposes an OpenAI-compatible API and its public model catalog changes over time. NVIDEA therefore treats model availability as a runtime dependency rather than assuming that a model ID verified during development will remain available forever.

## What it checks

The checker reads an OpenAI-compatible model-list payload (`{ "data": [{ "id": "..." }] }`) and verifies the exact configured IDs for all three routing tiers:

- Fast: `NVIDEA_MODEL_FAST`, defaulting to `NebiusOptions.VerifiedNemotronNanoModel`.
- Standard: `NVIDEA_MODEL_STANDARD`, defaulting to `NebiusOptions.VerifiedNemotronSuperModel`.
- Deep: `NVIDEA_MODEL_DEEP`, defaulting to `NebiusOptions.VerifiedNemotronUltraModel`.

Matching is exact and case-sensitive. If any required tier is absent, the result fails closed.

The parser also rejects malformed catalog shapes, duplicate JSON properties, duplicate model IDs, control characters in model IDs, empty catalogs, more than 2,048 entries, excessive nesting, comments/trailing commas, and inputs over 2 MiB.

## Captured/offline mode

Use a previously captured `/models` JSON response when credentials or network access are unavailable:

```powershell
dotnet run --project tools/Nvidea.NebiusModelCatalogCheck -- \
  --input .\artifacts\nebius-models.json \
  --output .\artifacts\nebius-model-catalog-check.json
```

Offline evidence is explicitly marked `mode: "captured"`. It proves only that the exact captured bytes contained all configured tier IDs at the time they were obtained. The evidence includes the SHA-256 of those bytes so a later reviewer can bind the result to the exact snapshot.

## Live zero-inference mode

Set `NEBIUS_API_KEY` and optionally `NVIDEA_NEBIUS_BASE_URL`, then run:

```powershell
dotnet run --project tools/Nvidea.NebiusModelCatalogCheck -- \
  --live \
  --output .\artifacts\nebius-model-catalog-check.json
```

Live mode performs an authenticated **GET** of `models`; it does not call `chat/completions` and is not intended to consume inference tokens. The request is bounded to 20 seconds and 2 MiB, refuses redirects, and only accepts HTTPS endpoints whose host ends in `nebius.com` and contains no URI user-info.

A live PASS is useful immediately before recording the demo. It still does **not** prove that a subsequent inference request will succeed, that quota is sufficient, or that every model supports every requested feature. Keep the contract probe and an actual pre-demo inference smoke test as separate evidence.

## Evidence and redaction

The JSON output contains only:

- schema version and observation time;
- `captured` or `live` mode;
- PASS/FAIL;
- SHA-256 of the exact catalog bytes when parsing reached the catalog;
- unique model count;
- live endpoint **host only** (never scheme path/query/user-info);
- the three configured tier IDs and whether each was present;
- stable failure codes.

The tool never emits the Nebius API key, Authorization header, raw HTTP error body, source file path, or the full provider model catalog.

## Exit codes

- `0`: all required tier IDs are present.
- `1`: catalog/read/provider validation failed or at least one required model is absent.
- `2`: invalid CLI usage.
- `3`: evidence output could not be persisted.

## Regression tests

`tests/Nvidea.NebiusModelCatalogCheck.Tests` covers:

- all-tier PASS;
- missing Deep tier drift;
- exact case-sensitive IDs;
- duplicate model IDs;
- duplicate JSON properties;
- malformed catalog shape;
- captured evidence endpoint redaction; and
- live evidence limiting endpoint disclosure to the host supplied by the trusted live path.

Run with:

```powershell
dotnet test tests/Nvidea.NebiusModelCatalogCheck.Tests/Nvidea.NebiusModelCatalogCheck.Tests.csproj
```

## Judging / demo use

Recommended order before the final recording:

1. Run this checker in `--live` mode and require PASS.
2. Run the existing Nebius contract/evidence probe.
3. Run the positive and adversarial Personal AI evaluators.
4. Build the unified judging evidence package.
5. Run the deterministic demo-package validator.

This keeps the claim boundary precise: catalog presence is evidence of model availability in the returned Token Factory catalog, not a fabricated claim of successful paid inference.
