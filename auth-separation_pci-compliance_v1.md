# Compliance Scope: PCI DSS for the AuthN Service

The AuthN service stores credentials, MFA secrets, refresh tokens, and recovery codes. None of these are cardholder data, so PCI DSS does not strictly apply. The architecture document refers to "PCI-style isolation" because PCI DSS is the most mature, widely audited standard for storing high-sensitivity secrets behind a hardened boundary, and its control set is the closest off-the-shelf model for credential handling. This document treats PCI DSS v4.0 as the reference baseline and adapts each requirement to credentials.

If the deployment also processes cardholder data, the controls below become literal compliance obligations. If it does not, they remain the recommended floor.

---

## 1. Why this scope sits in AuthN alone

The separation of concerns described in `auth-separation_architecture_v1.md` puts every credential under one service, behind one set of access controls, with one operational owner. This containment is what makes a PCI-style audit feasible. A monolithic service that mixed credentials, profile data, and authorisation logic would drag the entire codebase, every database, and every operations team into scope. By isolating credentials in AuthN:

- The audit boundary is one service, not the whole platform.
- The blast radius of a credential compromise stops at AuthN. AuthZ and User Info are separate trust zones.
- The personnel with access to the credential store is a small, named subset, not the whole engineering organisation.
- Controls that are expensive (HSM-backed key storage, dedicated network segments, change-control rigour) are paid for once, in one place.

Out of scope by construction: AuthZ (no credentials, no card data), User Info (personal data only), the message bus (carries event metadata, never credential material), every business-domain service downstream.

---

## 2. PCI DSS requirements mapped to the AuthN service

The 12 PCI DSS requirements are summarised below with the concrete control each one implies for AuthN. Where the requirement is intrinsically about cardholder data, the adapted credential-equivalent is given in parentheses.

### Requirement 1: Install and maintain network security controls

The AuthN service runs in a dedicated network segment with deny-by-default ingress and egress. The only ingress is the public API gateway. The only egress is the credential database, the JWKS publication endpoint, and the event bus publisher. No direct connections from AuthN to AuthZ, User Info, or any business service.

Controls expected:
- Stateful firewall in front of the segment, with rule changes logged and reviewed quarterly.
- All inbound TLS terminates at the gateway with mutual TLS to AuthN.
- Outbound traffic restricted to a named allow-list of internal hosts and the bus broker.

### Requirement 2: Apply secure configurations to all system components

No default vendor passwords, no shared service accounts, no debug endpoints exposed in production. Configuration baselines are version-controlled and applied through infrastructure as code; drift is detected and alerted.

### Requirement 3: Protect stored account data (adapted: protect stored credentials)

Credentials never live in the clear. Specific controls:

- Passwords are stored as the output of a memory-hard key derivation function (Argon2id, scrypt, or bcrypt at a cost factor reviewed annually). Plain text is never persisted in any database, log, cache, queue, or backup.
- MFA secrets are encrypted at rest using a key held in an HSM or equivalent KMS. The plaintext secret is held in process memory only during the verification window.
- Refresh tokens are stored as SHA-256 hashes of the issued value, indexed by user_id and device. The plaintext token is never persisted server-side.
- Recovery codes are hashed with the same KDF used for passwords and marked as single-use.

Database backups inherit the same encryption and the same access controls. Restoring a backup requires the same key material and the same authorisation.

### Requirement 4: Protect cardholder data with strong cryptography during transmission (adapted: credentials in transit)

All API traffic uses TLS 1.3 with current cipher suites. Internal service-to-service traffic uses mutual TLS. The JWKS endpoint serves only over TLS. Logs and event payloads never contain plaintext credentials, even transiently.

### Requirement 5: Protect all systems and networks from malicious software

The AuthN container image is built from a minimal base, scanned for vulnerabilities on every build, and rebuilt at least weekly. Runtime protection (eBPF-based monitoring or equivalent) detects unexpected process launches inside the container.

### Requirement 6: Develop and maintain secure systems and software

The AuthN codebase has its own security review process. Specific controls:

- Static analysis (semgrep or equivalent) runs on every pull request.
- Dependency scanning runs on every build and blocks merges with known critical CVEs.
- Code changes to the credential store, hashing logic, or token issuance require approval from a named security reviewer in addition to the normal code owner.
- All cryptographic primitives come from a vetted library; no hand-rolled crypto under any circumstances.

### Requirement 7: Restrict access to system components by business need to know

Production access to AuthN, the credential database, and the KMS keys is restricted to a named on-call rota. Engineers do not have standing production access; access is brokered through a just-in-time access tool with logged approvals.

The application-level access policy is the same: AuthZ never sees credentials, User Info never sees credentials, no business service sees credentials.

### Requirement 8: Identify users and authenticate access

Operators authenticating to the AuthN admin plane use MFA. Service-to-service calls use signed tokens or mTLS client certificates with short rotation periods. Shared admin accounts are forbidden. Every administrative action is attributable to an individual.

### Requirement 9: Restrict physical access to cardholder data

The AuthN database, KMS, and HSM live in a cloud region or data centre with PCI-attested physical controls. The deployment never runs on developer laptops with real credential data; only synthetic data is used outside production.

### Requirement 10: Log and monitor all access to system components and cardholder data

Every authentication attempt, credential read, credential write, and key operation is logged with a stable event ID, the actor identifier, the affected user_id, the source IP, and a timestamp from a synchronised time source. Logs are forwarded to an immutable store within seconds of generation.

The minimum log retention is 12 months online and 24 months in cold storage, matching PCI DSS retention. Logs are reviewed daily by automation; anomalies (spike in failures, unusual geographic patterns, off-hours admin actions) trigger alerts to the on-call team.

The `AuthenticationFailed` event in `auth-separation_events_v1.yaml` already publishes a hashed login identifier so that downstream analytics never see the raw value.

### Requirement 11: Test security of systems and networks regularly

The AuthN service is part of every quarterly penetration test and every annual external audit. Internal vulnerability scanning runs weekly. Authenticated scans probe the admin plane.

### Requirement 12: Support information security with organisational policies and programs

The team owning AuthN has a documented incident response plan with defined RTO/RPO targets, a named on-call rotation, and an annual tabletop exercise focused on credential compromise scenarios. The plan is the source of truth for the conformance item in the architecture document referencing operational runbooks.

---

## 3. Key rotation and lifecycle

Cryptographic keys used by AuthN have a defined rotation schedule and an emergency rotation procedure.

| Key | Purpose | Rotation cadence | Emergency rotation |
|------|---------|-----------------|------------------|
| JWT signing key (asymmetric) | Sign access tokens | 90 days | Immediate; previous key remains in JWKS for one access-token lifetime |
| Credential database encryption key | Envelope-encrypt the credential rows | 1 year | Re-encrypt under new key; old key destroyed after verification |
| MFA secret encryption key | Encrypt TOTP and WebAuthn secrets | 1 year | As above |
| Service-to-service mTLS certificates | Authenticate AuthN to the bus and JWKS distribution | 90 days | Immediate; old cert revoked |
| Backup encryption key | Protect database backups | 1 year | New backups under new key; old backups remain restorable until retention expires |

The rotation procedures are scripted, dry-run quarterly, and tested in production-equivalent environments before each scheduled rotation.

---

## 4. Breach response

A credential compromise is the highest-severity incident the platform handles. The response sequence is:

1. **Contain.** Revoke all sessions for affected users via `SessionRevoked` events with reason `suspected_compromise`. Force password reset on next login.
2. **Investigate.** Pull the AuthN audit log for the relevant time window. Identify scope: which user_ids, which credentials, which key material.
3. **Rotate.** If the JWT signing key is implicated, rotate immediately and accept the brief window of token churn. If the database encryption key is implicated, rotate and re-encrypt.
4. **Notify.** If personally identifiable information is in scope (it usually is, because the login identifier is typically an email address), notify the User Info compliance team to evaluate GDPR Article 33 obligations. See `auth-separation_gdpr-compliance_v1.md`.
5. **Report.** External communication follows the corporate breach disclosure policy. PCI DSS requires acquirer notification for cardholder data; the equivalent for credentials is regulatory notification under whatever regime applies (FCA, ICO, state attorneys general, depending on jurisdiction).
6. **Postmortem.** Blameless write-up within 14 days; corrective actions tracked to closure.

---

## 5. Out-of-scope items that consumers of AuthN must handle

PCI compliance for credentials does not absolve the rest of the system. The following are explicitly the responsibility of other services:

- **AuthZ** must verify access tokens against the JWKS endpoint and treat any token that fails verification as unauthenticated, regardless of source.
- **User Info** must enforce the consent and lifecycle requirements documented in `auth-separation_gdpr-compliance_v1.md`. Loss of credentials does not reduce its obligations toward personal data.
- **Resource APIs** must not log access tokens, must not echo them back in responses, and must not pass them onward to third parties without explicit user consent.
- **The bus** must reject events from any publisher whose mTLS certificate has been revoked.

---

## 6. Conformance checklist

A deployment claims PCI-equivalent posture for AuthN when it can demonstrate:

1. The credential store is encrypted at rest with KMS-managed keys.
2. Passwords use a memory-hard KDF with parameters reviewed in the last 12 months.
3. MFA secrets and refresh tokens are hashed or encrypted; no plaintext is persisted.
4. TLS 1.3 is enforced on all ingress and egress.
5. JWKS is published only over TLS and rotates on the documented schedule.
6. Production access is just-in-time, MFA-protected, and fully logged.
7. Audit logs are immutable, forwarded within seconds, and retained for at least 12 months online.
8. The codebase passes static analysis, dependency scanning, and security review on every change.
9. The on-call team has executed a credential-compromise tabletop exercise within the last 12 months.
10. An external assessor has reviewed the controls within the last 12 months.

If any item fails, the gap is logged, a remediation owner is assigned, and the gap is reported to the security committee.
