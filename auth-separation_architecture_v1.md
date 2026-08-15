# Auth Separation: Architecture Overview

This document establishes the trust model, the data ownership boundaries, the token model, and the canonical flows that the three OpenAPI specifications and the AsyncAPI specification rely on. Read this before reading the API specs; the specs are coherent only against the model described here.

---

## 1. Trust boundaries

The system is composed of three independently deployable services. Each runs in its own process, owns its own data store, and is owned by a separate operational team in the canonical deployment.

```
                +----------------------+
                |        Client        |
                |  (web, mobile, BFF)  |
                +----------+-----------+
                           |
              (1) credentials / token requests
                           v
                +----------------------+
                |   AuthN Service      |     credential store
                |  proves identity     |<--->|  (passwords,
                |  issues tokens       |     |   MFA secrets,
                +-----+-----------+----+     |   refresh keys)
                      |           |
              (2) JWT |           | publishes UserCreated,
                      |           | CredentialsChanged events
                      v           v
                +----------------------+    +----------------------+
                |   AuthZ Service      |    |    Message Bus       |
                |  decides permissions |--->|  (events backbone)   |
                |  owns roles/policies |<---|                      |
                +-----+-----------+----+    +-----------+----------+
                      ^           |                     ^
                      |           |                     |
                      |     (3) decision                |
                      |           |                     | publishes
                      |           v                     | RoleAssigned,
                +----------------------+                | ProfileUpdated
                |   Resource API       |                | events
                | (any business API    |                |
                |  protected by token  |                |
                |  + AuthZ check)      |                |
                +----------------------+                |
                                                        |
                +----------------------+                |
                |   User Info Service  |----------------+
                |  owns profile data   |
                |  consent, prefs      |
                +----------------------+
                            |
                       profile store
                       (encrypted PII)
```

The trust boundaries between services are enforced by mutual TLS at the network layer and by signed JWTs at the application layer. No service trusts a header; every service verifies the token signature against the AuthN public key on every request.

---

## 2. Data ownership

Each service owns a disjoint slice of the user record. No two services hold the same field. Cross-service references are by stable, opaque user identifier only.

| Field | Owned by | Notes |
|------|---------|------|
| `user_id` | AuthN | Created at registration. Stable for the lifetime of the user. Opaque to clients. |
| `username` / `email` (login identifier) | AuthN | Used to look up credentials. Hashed in transit logs. |
| `password_hash`, `mfa_secret`, `recovery_codes` | AuthN | Never leaves the AuthN service. PCI-style isolation. |
| `roles`, `permissions`, `policy_bindings` | AuthZ | Keyed by `user_id`. AuthZ has no knowledge of credentials. |
| `display_name`, `given_name`, `family_name`, `avatar_url` | User Info | Personal data. Encrypted at rest. |
| `preferences`, `locale`, `timezone` | User Info | User-controlled. |
| `consent_grants` | User Info | GDPR / regulatory record of what the user has agreed to. |

If the same field appears in two stores, the spec is wrong. Resolve before implementing.

---

## 3. Token model

The system uses signed JWTs as bearer tokens. Two token types exist:

### Access token
- Signed by AuthN with an asymmetric algorithm (`RS256` or `ES256`).
- Lifetime: 15 minutes.
- Contains: `sub` (user_id), `iat`, `exp`, `iss`, `aud`, `token_type: "access"`, `auth_time`, optional `mfa: true`.
- Does **not** contain roles or permissions. Clients and resource APIs must call AuthZ to obtain authorisation decisions.
- Verified by every downstream service against AuthN's published JWKS endpoint.

### Refresh token
- Opaque, high-entropy string.
- Lifetime: 30 days, sliding.
- Stored hashed in the AuthN credential store, indexed by `user_id` and device.
- Single-use: every refresh rotates the refresh token.
- Revocable per device.

Decoupling roles from the access token is a deliberate choice. It means a permission change takes effect within seconds (next AuthZ call) rather than minutes (next token refresh), and it keeps the token small and free of sensitive permission detail.

---

## 4. Canonical flows

### 4.1 Login

```
Client          AuthN           AuthZ           User Info       Bus
  |               |               |                 |             |
  |--credentials->|               |                 |             |
  |               |--verify hash--|                 |             |
  |               |   (internal)  |                 |             |
  |               |---------------|---publish UserAuthenticated------>|
  |<--access+refresh tokens-------|                 |             |
  |               |               |                 |             |
```

### 4.2 Authorised request to a resource API

```
Client          Resource API    AuthZ           User Info
  |                   |             |                 |
  |--GET /thing+JWT-->|             |                 |
  |                   |--verify JWT signature (offline, JWKS)
  |                   |--check(user_id, "thing.read")>|
  |                   |<--allow / deny---------------|
  |                   |                               |
  |                   |--(if needed) GET /users/{id}->|
  |                   |<--profile fields-------------|
  |<--200 OK---------|                               |
```

### 4.3 Permission change

```
Admin          AuthZ           Bus           Caches
  |               |             |              |
  |--POST /roles->|             |              |
  |               |--publish RoleAssigned----->|
  |               |             |--invalidate->|
  |<--201--------|             |              |
```

Next AuthZ check picks up the new role from its store. No token reissue required.

### 4.4 Profile update

```
Client          User Info        Bus
  |                 |             |
  |--PATCH /me ----|             |
  |                 |--publish ProfileUpdated------>|
  |<--200----------|             |
```

The bus event allows downstream consumers (search index, analytics, audit log) to react. AuthN and AuthZ ignore profile events; profile data is not their concern.

### 4.5 Account deletion

```
Client          User Info       AuthZ          AuthN          Bus
  |                 |             |              |              |
  |--DELETE /me ---|             |              |              |
  |                 |--publish UserDeletionRequested------>     |
  |                 |             |              |              |
  |                 |             |--remove role bindings       |
  |                 |             |              |--revoke all tokens
  |                 |--scrub PII, retain audit metadata         |
  |<--202 Accepted-|             |              |              |
```

Deletion is asynchronous and event-driven. Each service is responsible for honouring the request within its own data store and emitting a confirmation event. A coordinator (not specified here) tracks completion.

---

## 5. Failure modes the architecture addresses

- **Credential leak.** Compromising the AuthN store does not reveal personal data (User Info) or permissions (AuthZ). The blast radius is bounded.
- **Permission misconfiguration.** A bad role binding is reversed in AuthZ alone. No token reissue, no credential rotation.
- **PII exposure.** Personal data lives in one place, encrypted, behind one set of access controls. GDPR access and erasure requests target one service.
- **Stale permissions.** Because access tokens carry no role data, revocation propagates within one cache TTL of an AuthZ check.

---

## 6. Failure modes the architecture does not address

- **Compromise of the JWT signing key.** Mitigation is operational: short key rotation intervals, hardware-backed key storage, monitoring of unusual issuance patterns.
- **Bus poisoning.** A malicious event publisher can corrupt downstream consumers. Mitigation: authenticated publishers, signed event payloads, schema validation on consume.
- **Side-channel correlation across services.** If logs across all three services include the same `user_id`, a log breach reassembles much of the user record. Mitigation: per-service pseudonymous IDs in logs, derived from `user_id` via a per-service salt.

These are noted so that an implementer does not assume the spec covers them.

---

## 7. Conformance requirements

A skeleton built from this spec set is conformant when:

- All three services start independently with no shared database.
- AuthN exposes a JWKS endpoint and signs tokens with an asymmetric algorithm.
- AuthZ decisions are made from its own data store and not from token claims.
- User Info encrypts all PII columns at rest and exposes the consent endpoints.
- All cross-service state changes are published as events on the bus and not as direct synchronous calls.
- Every endpoint in every OpenAPI file responds with a payload that validates against the declared schema.
- Every Gherkin scenario in `features/` passes end-to-end.
