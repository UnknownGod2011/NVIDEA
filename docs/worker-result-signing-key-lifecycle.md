# Worker result-signing identity lifecycle

NVIDEA authenticates remote research results with a dedicated RSA worker identity. This identity is **not** the worker work-item decryption key and is **not** either client keypair. Its only authority is signing the already-encrypted `ProtectedResearchResultEnvelope` before publication.

This runbook is intentionally provider-control-plane conservative: NVIDEA validates MysteryBox references without resolving or logging secret values. Provider UI/CLI commands can change; use the current Nebius MysteryBox documentation when creating a real secret/version, then apply the invariants below.

## Identity contract

- Worker secret: `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM`.
- Windows verification pin: `NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM`.
- Algorithm: RSA-PSS with SHA-256, RSA >= 2048 bits; use 3072 bits for new production identities.
- The private PEM exists only in the operator's secure key-generation environment and Nebius MysteryBox. Never commit it, place it in a container image, paste it into plaintext Serverless environment configuration, or copy it to the Windows client.
- The Windows pin is public-only SubjectPublicKeyInfo. Supplying a private PEM to the client trust loader does not grant signing authority: it canonicalizes retained material to the public identity.
- Do not reuse the work-item decryption key, client dispatch-signing key, or client result-decryption key for this role.

## Generate a new identity

Generate outside the repository on a trusted machine:

```bash
umask 077
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out worker-result-signing-private.pem
openssl pkey -in worker-result-signing-private.pem -pubout -out worker-result-signing-public.pem
openssl pkey -in worker-result-signing-private.pem -check -noout
openssl pkey -pubin -in worker-result-signing-public.pem -text -noout
```

Treat the private file as temporary high-value material. Record a public-key fingerprint for change review without recording the private key:

```bash
openssl pkey -pubin -in worker-result-signing-public.pem -outform DER \
  | openssl dgst -sha256
```

A fingerprint is operational metadata, not a substitute for the PEM pin used by NVIDEA.

## Provision Nebius

1. Create or select a dedicated MysteryBox secret for the worker result-signing identity. Do not combine unrelated API keys or RSA identities merely for convenience.
2. Create a new immutable secret version whose value is the complete private PEM.
3. Configure the Serverless worker secret reference so that it materializes as `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM`.
4. Pin the deployment to the intended MysteryBox secret **version**, not an implicit moving/latest value.
5. Keep `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM` absent from plaintext environment configuration. NVIDEA's live deployment preflight rejects plaintext provisioning and requires a valid MysteryBox reference before runtime construction.
6. Keep the matching `worker-result-signing-public.pem` available for trusted Windows-client configuration and release operations. It is not secret, but its authenticity matters.

Never log the private PEM or copy it into tickets, CI output, screenshots, demo artifacts, `progress.md`, or repository documentation.

## Distribute the Windows public pin

Set `NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM` from the matching public-only PEM through the trusted desktop configuration path. Verify the SHA-256 public fingerprint out-of-band against the release/deployment change record before enabling remote research.

The desktop cloud-research composition fails closed if the pin is missing or malformed. The authenticated result transport verifies the worker signature while the result remains encrypted; only authenticated envelopes may proceed to client-private-key unwrap, AEAD decryption, JSON parsing, and durable result application.

## Rotation without an authentication outage

The current NVIDEA verifier intentionally pins **one** worker public identity. Therefore do not rotate the worker signing secret in place while old clients still pin the old public key. Use a coordinated cutover:

1. Generate keypair B and provision it as a **new MysteryBox version** without changing the active worker deployment.
2. Prepare client configuration/release B with public pin B. Do not activate that client against workers still signing with A.
3. Quiesce new remote-research dispatches. Let already-dispatched A jobs reach a terminal state and ingest their A-signed results. If they cannot finish, cancel/reconcile them under the existing lifecycle rather than accepting unauthenticated results.
4. Confirm there are no A-signed in-flight results that still need ingestion.
5. Deploy the worker pinned to MysteryBox version B.
6. Activate client configuration/release B and run a canary remote-research job end to end.
7. Only after the canary succeeds should normal dispatch resume.
8. Retain version A for a short rollback window according to operator policy; then disable/revoke it after rollback is no longer permitted.

This sequencing trades a short dispatch maintenance window for a strict single-key trust model. Do **not** weaken verification or temporarily accept unsigned results to avoid the window.

## Rollback

Rollback is symmetric and must remain version-pinned:

1. Quiesce new dispatches.
2. Drain/cancel B-signed in-flight work.
3. Redeploy the worker explicitly pinned to the previous approved MysteryBox version A.
4. Restore the Windows public pin A through the trusted configuration path.
5. Run a canary before resuming dispatch.

Never point production at an unversioned/latest secret as a rollback shortcut.

## Compromise / emergency revocation

If signing key A may be compromised, **do not** perform a normal rollback and do not continue trusting A-signed results merely because signatures verify.

1. Stop new remote-research dispatch immediately and use NVIDEA's cancellation/emergency-stop controls for active work where applicable.
2. Mark A revoked in the operator incident record and remove/disable its deployment authority in MysteryBox according to current Nebius controls.
3. Treat results produced after the earliest plausible compromise time as untrusted; rerun required research after the new identity is active.
4. Generate a fresh keypair B in a trusted environment, create a new MysteryBox version, distribute public pin B, and perform the coordinated cutover above.
5. Do not restore A during incident rollback. Roll back application code independently while retaining the uncompromised signing identity.

NVIDEA does not currently ship a multi-key grace ring or signed key-transition certificate. Adding either would materially change the trust model and requires adversarial qualification before use.

## Release checklist

- Private signing key is RSA >= 2048 bits (3072 recommended) and role-unique.
- Worker receives the private PEM only through a version-pinned MysteryBox secret reference.
- No plaintext `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM` exists in Serverless environment configuration, images, source, logs, CI, or artifacts.
- Windows has only the matching public pin and its fingerprint was independently checked.
- Worker and client cut over together after old in-flight jobs drain.
- A successful canary proves signed-result ingestion before normal dispatch resumes.
- Rollback version and public pin are known before deployment; compromised keys are never rollback candidates.

## Evidence and limitations

Provider-free tests cover secret-role isolation, weak/malformed key rejection, missing signatures, wrong pinned workers, replay/substitution, and verify-before-decrypt behavior at the production ingestor boundary. Live Nebius MysteryBox creation, secret-version injection, Windows deployment, and end-to-end provider validation still require a real account and are not claimed by this runbook.
