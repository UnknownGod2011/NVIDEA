# Nebius deployment evidence verification

NVIDEA can prove, without credentials or provider access, that a saved zero-cost preflight manifest and a later successful live-research PASS refer to the same redacted deployment fingerprint.

Generate the preflight manifest by setting `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` and running the contract probe in `--live-research-preflight` mode. Generate PASS evidence by setting `NVIDEA_LIVE_PASS_EVIDENCE_PATH` and completing a real `--live-research` run.

Then verify the pair locally:

```bash
dotnet run --project tools/Nvidea.NebiusEvidenceVerifier/Nvidea.NebiusEvidenceVerifier.csproj -- ./manifest.json ./pass.json
```

The verifier performs no Nebius, Tavily, Object Storage, MysteryBox, or other network calls. It reads only the two redacted JSON artifacts, enforces bounded artifact sizes, validates both schema versions, recomputes the deployment fingerprint from the manifest's redacted deployment contents, checks that the stored manifest fingerprint is self-consistent, validates the PASS timestamp/count invariants, and finally compares the manifest and PASS fingerprints using fixed-time byte comparison.

A successful verification therefore means the PASS artifact claims the same deployment description that was preflighted and that the saved manifest was not modified without its fingerprint changing. It does **not** provide cryptographic authorship or third-party attestation of the files; anyone able to replace both artifacts can create a new internally consistent pair. For judging evidence, keep the artifacts together with the corresponding immutable worker image digest and repository commit.

The verifier never needs or prints credentials, raw MysteryBox references, PEM contents, provider responses, research bodies, bucket names, or Serverless resource IDs. Errors identify only the artifact class or invariant that failed rather than dumping file contents.
