# Database Specification: Auth Separation

This document specifies the persistent storage required to support the three services described in the auth-separation example. It is technology-agnostic: it describes the data model, the access patterns, the indexes, the encryption posture, the retention rules, and the backup story for each store, without naming a specific database product. Any store that meets the capability requirements set out in section 8 is a valid implementation.

The architecture document establishes the principle that each service owns its own data and that no two services share a database. This document operationalises that principle.

---

## 1. The four stores at a glance

The three services own three primary stores. A fourth store, the AuthZ decision audit log, is logically distinct and called out separately because its access pattern differs sharply from the role and policy data alongside it.

| Store | Owned by | Holds | Read profile | Write profile | Sensitivity |
|-------|----------|-------|--------------|---------------|-------------|
| Credential store | AuthN | Login identifiers, password hashes, MFA secrets, refresh-token hashes, recovery codes | Bursty (login storms) | Low | High (PCI-equivalent) |
| Authorisation store | AuthZ | Roles, permissions, role assignments, policy bindings | Constant (every protected request) | Low (admin-driven) | Medium-High |
| Decision audit log | AuthZ | One entry per decision with actor, action, resource, allow/deny | Low (forensic only) | Very high (per-request) | High (immutability is the property) |
| Profile store | User Info | Profile attributes, preferences, consent grants | Moderate | Low | High (GDPR-personal data) |

No single store holds data from more than one row of this table. No row's data appears in more than one store outside of explicitly published events.

---

## 2. Credential store (AuthN)

The credential store is the highest-sensitivity store in the system. It holds material that, if exfiltrated, could be used to impersonate users.

### 2.1 Data model

The store contains four logical entities. They map to four tables (in a row-store implementation), four collections (in a document store), or four key prefixes (in a key-value store).

**Users**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `user_id` | UUID | Yes | Primary key. Stable for the lifetime of the user. Generated server-side at registration. |
| `login_identifier` | string (case-folded) | Yes | Unique. The value used to look up credentials at login. Indexed. |
| `password_hash` | binary blob | Yes | Output of a memory-hard KDF (Argon2id, scrypt, or bcrypt). Includes the algorithm parameters as a self-describing prefix. |
| `password_updated_at` | timestamp | Yes | Set on initial creation and on every subsequent change. |
| `failed_attempts` | integer | Yes | Counter for rate limiting and lockout. Reset on successful login. |
| `locked_until` | timestamp | No | If set and in the future, login is rejected. |
| `created_at` | timestamp | Yes | Set on registration. |
| `updated_at` | timestamp | Yes | Set on every write. |
| `deleted_at` | timestamp | No | Tombstone for asynchronous account deletion (see User Info GDPR doc). |

**MFA Factors**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `mfa_id` | UUID | Yes | Primary key. |
| `user_id` | UUID | Yes | Foreign key to Users. Indexed. |
| `method` | enum | Yes | One of `totp`, `webauthn`, `sms`. |
| `secret_ciphertext` | binary blob | Yes | Encrypted under the credential KMS key. |
| `secret_kek_id` | string | Yes | Identifier of the key encryption key used; supports rotation. |
| `enrolled_at` | timestamp | Yes | |
| `last_used_at` | timestamp | No | |
| `revoked_at` | timestamp | No | If set, factor is no longer accepted. |

**Refresh Tokens**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `token_hash` | binary | Yes | Primary key. SHA-256 of the issued plaintext token. |
| `user_id` | UUID | Yes | Indexed. |
| `device_id` | string | No | Stable client-supplied identifier. |
| `issued_at` | timestamp | Yes | |
| `expires_at` | timestamp | Yes | Indexed for cleanup. |
| `used_at` | timestamp | No | If set, the token has been consumed and must not be honoured again. |
| `revoked_at` | timestamp | No | If set, token is invalid. |

**Recovery Codes**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `code_hash` | binary | Yes | Primary key. KDF hash of the issued plaintext code. |
| `user_id` | UUID | Yes | Indexed. |
| `issued_at` | timestamp | Yes | |
| `consumed_at` | timestamp | No | If set, code has been used; single-use enforcement. |

### 2.2 Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| `users_pk` | `user_id` | Primary lookup. |
| `users_login_unique` | `login_identifier` | Login lookup; uniqueness constraint. Case-insensitive collation or lowered at write. |
| `mfa_user_idx` | `user_id`, `method`, `revoked_at IS NULL` | List active factors for a user. |
| `refresh_user_idx` | `user_id`, `revoked_at IS NULL`, `expires_at` | Revoke all sessions for a user. |
| `refresh_expiry_idx` | `expires_at` | Background sweep of expired tokens. |
| `recovery_user_idx` | `user_id`, `consumed_at IS NULL` | Verify a recovery code attempt. |

### 2.3 Access patterns

Reads:
- Login identifier lookup, then password hash verification. Highest QPS at peak.
- MFA factor lookup by user_id during MFA challenge.
- Refresh token hash lookup at refresh time.
- Recovery code hash lookup during recovery flow.

Writes:
- New user on registration.
- Update password_hash on password change or reset.
- Insert refresh token on every login and refresh.
- Mark refresh token used on refresh.
- Bulk revoke refresh tokens on password change or admin action.
- Tombstone update on deletion.

Bulk reads of personal data are never required; the store does not need analytical query support.

### 2.4 Encryption

- Whole-database encryption at rest is required.
- MFA secrets and refresh-token plaintext are never persisted; the store only holds hashes or ciphertext.
- The KEK (key-encryption key) protecting MFA secrets must be stored in an HSM or equivalent KMS, not in the application configuration.
- Backups inherit the same encryption.

### 2.5 Retention

- User rows retained while the account is active.
- Tombstoned users are scrubbed of all credential material within 60 seconds of deletion request; the user_id row may be retained as an audit shell only if required by the deletion runbook.
- Refresh tokens are deleted within 24 hours of expiry by a background sweep.
- Recovery codes are deleted within 24 hours of consumption.
- Audit metadata (the timestamps in the credential store) is retained for the operational retention period (typically 12 months online).

### 2.6 Backup and disaster recovery

- Backup encrypted with a key independent of the runtime encryption key.
- Backup integrity verified weekly via restore test.
- Point-in-time recovery within the last 35 days is the minimum target.
- Cross-region backup replication if availability requirements demand it.

---

## 3. Authorisation store (AuthZ)

The authorisation store is read on every protected request and written infrequently by administrative actions.

### 3.1 Data model

**Permissions**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `permission_key` | string | Yes | Primary key. Pattern `domain.action`, e.g. `invoice.read`. |
| `description` | string | Yes | Human-readable. |
| `created_at` | timestamp | Yes | |

**Roles**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `role_key` | string | Yes | Primary key. Pattern `^[a-z][a-z0-9_]{1,63}$`. |
| `name` | string | Yes | |
| `description` | string | No | |
| `created_at` | timestamp | Yes | |
| `updated_at` | timestamp | Yes | |

**Role Permissions** (many-to-many)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `role_key` | string | Yes | Composite key with permission_key. |
| `permission_key` | string | Yes | |
| `granted_at` | timestamp | Yes | |

**Role Assignments**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `assignment_id` | UUID | Yes | Primary key. |
| `user_id` | UUID | Yes | Indexed. |
| `role_key` | string | Yes | |
| `scope` | string | No | Optional resource scope, e.g. tenant ID. |
| `assigned_at` | timestamp | Yes | |
| `assigned_by` | UUID | Yes | user_id of the admin who created the assignment. |
| `expires_at` | timestamp | No | If in the past at decision time, treated as revoked. |
| `revoked_at` | timestamp | No | If set, treated as revoked from that moment. |
| `idempotency_key` | string | No | Honoured at write time to prevent duplicate retries. |

**Policies**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `policy_key` | string | Yes | Primary key. |
| `effect` | enum | Yes | One of `allow`, `deny`. |
| `subjects` | document/JSON | Yes | List of subject specifications (user_id or role_key). |
| `actions` | array of permission_key | Yes | |
| `resources` | array of string | No | Resource patterns. |
| `conditions` | document/JSON | No | Free-form condition expressions. |
| `created_at` | timestamp | Yes | |
| `updated_at` | timestamp | Yes | |

### 3.2 Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| `assignments_user_idx` | `user_id`, `revoked_at IS NULL` | Resolve effective roles for a user during a decision check. |
| `assignments_role_idx` | `role_key` | Bulk impact assessment when a role changes. |
| `assignments_idem_unique` | `idempotency_key` | Idempotency enforcement on assignRole. |
| `assignments_expiry_idx` | `expires_at`, `revoked_at IS NULL` | Background sweep of expired assignments (or honour at read time). |
| `role_perm_pk` | `role_key`, `permission_key` | Permission membership lookup. |
| `policies_action_idx` | `actions` (gin/array index) | Find policies relevant to a candidate action. |

### 3.3 Access patterns

Reads (by frequency, highest first):
- Decision check: load assignments for a user, load permissions for those roles, evaluate against requested action and resource. Sub-10ms p99 target.
- Decision check (batch): up to 50 in one round trip.
- Audit query: paginated by user_id and date range; low-frequency.

Writes (by frequency, lowest first):
- Role assignment via admin endpoint.
- Role definition change.
- Policy change.

The decision-path read is the hot path. The store must support fast, indexed lookups by user_id with sub-millisecond latency at the storage layer to leave headroom for the rest of the request.

### 3.4 Caching

A read-through cache in front of the authorisation store is permitted and recommended. Cache invalidation is event-driven from `RoleAssigned`, `RoleRevoked`, and `PolicyChanged` events on the bus. Cache TTL must be short enough that an event-loss scenario does not let stale data survive more than a few minutes.

### 3.5 Encryption

- Encryption at rest is required.
- Role and policy data are not personally identifiable but are commercially sensitive (knowing them helps an attacker target). Treat as Internal-Confidential.
- No KMS-backed column-level encryption is required for the authorisation store itself.

### 3.6 Retention

- Active role and policy data retained indefinitely.
- Revoked role assignments retained for 12 months for audit traceability, then archived.
- Deleted users have their assignments removed within 60 seconds of the `UserDeletionRequested` event.

### 3.7 Backup and disaster recovery

- Daily backup, 35-day point-in-time recovery, weekly restore test.
- Backup encrypted with an independent key.

---

## 4. Decision audit log (AuthZ)

The decision audit log is logically a fourth store, separate from the authorisation tables, because its access pattern is fundamentally different: very high write rate, low read rate, append-only.

### 4.1 Data model

**Audit Entries**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `audit_id` | UUID | Yes | Primary key. |
| `decided_at` | timestamp | Yes | High-resolution; indexed for time-range queries. |
| `user_id` | UUID | Yes | Subject of the decision. Indexed. |
| `action` | string | Yes | The permission_key checked. |
| `resource` | string | No | Specific resource identifier. |
| `allowed` | boolean | Yes | The decision. |
| `matched_rule` | string | No | role_key or policy_key that produced the decision. |
| `request_id` | string | Yes | Trace identifier from the calling resource API. |
| `decision_latency_ms` | float | No | For analytics. |
| `prev_hash` | binary | Yes | Hash of the previous entry, for tamper-evidence. |
| `entry_hash` | binary | Yes | Hash of this entry's content plus prev_hash. |

### 4.2 Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| `audit_pk` | `audit_id` | |
| `audit_user_time_idx` | `user_id`, `decided_at` | Audit query by user. |
| `audit_time_idx` | `decided_at` | Time-range scan. |

### 4.3 Access patterns

Writes are the dominant access pattern. Every protected request in the platform produces one entry; for a moderate-traffic platform this is thousands per second. The store must absorb write bursts without dropping entries and without blocking the decision path.

Reads are low-frequency: audit queries, examiner evidence collection, forensic investigations. Latency is not critical; query expressiveness is.

### 4.4 Storage choice

The audit log is a natural fit for a log-structured or append-only store rather than a row-update store. Practical implementations use:

- Append-only log files forwarded to immutable object storage.
- A time-series database with retention policies.
- A row-store table with disabled UPDATE and DELETE permissions plus periodic cold-tier archival.

The storage choice is constrained by two non-negotiables: append-only enforcement (writes that update past entries are rejected at the storage layer), and forwarding to immutable storage within seconds of the original write.

### 4.5 Tamper-evidence

Each entry contains a hash of the previous entry. A daemon validates the chain hourly. Any mismatch is a P1 incident.

This is not a substitute for write-once storage; it is a defence-in-depth measure. An attacker who breaks the chain cannot do so silently.

### 4.6 Retention

- 12 months online minimum.
- 36 months in cold tier minimum.
- Retention periods extend if a regulatory or contractual obligation requires it.

### 4.7 Backup and disaster recovery

The forwarding to immutable storage is itself the primary backup. Loss of the online store is recoverable from the immutable copy. The forwarding pipeline is monitored; a stalled pipeline is a P2 incident.

---

## 5. Profile store (User Info)

The profile store holds personal data. Every column that is identifiably about a person is encrypted at rest with KMS-managed keys.

### 5.1 Data model

**Profiles**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `user_id` | UUID | Yes | Primary key. Mirrors the AuthN user_id. |
| `display_name_ciphertext` | binary | No | Column-encrypted. |
| `given_name_ciphertext` | binary | No | Column-encrypted. |
| `family_name_ciphertext` | binary | No | Column-encrypted. |
| `pronouns_ciphertext` | binary | No | Column-encrypted. |
| `bio_ciphertext` | binary | No | Column-encrypted. |
| `avatar_url` | string | No | Treated as personal data; encrypt the URL if it embeds an identifier. |
| `created_at` | timestamp | Yes | Not encrypted. |
| `updated_at` | timestamp | Yes | Not encrypted. |
| `deleted_at` | timestamp | No | Tombstone for asynchronous deletion. |

**Preferences**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `user_id` | UUID | Yes | Primary key. |
| `locale` | string | No | BCP 47 tag, e.g. `en-GB`. |
| `timezone` | string | No | IANA zone, e.g. `Europe/London`. |
| `notifications_email` | boolean | No | |
| `notifications_push` | boolean | No | |
| `notifications_sms` | boolean | No | |
| `accessibility_high_contrast` | boolean | No | |
| `accessibility_reduced_motion` | boolean | No | |
| `accessibility_larger_text` | boolean | No | |
| `updated_at` | timestamp | Yes | |

Preferences are not encrypted at the column level but inherit the database-level encryption.

**Consent Grants**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `grant_id` | UUID | Yes | Primary key. |
| `user_id` | UUID | Yes | Indexed. |
| `consent_key` | string | Yes | Pattern `domain.purpose`, e.g. `marketing.email`. |
| `version` | string | Yes | Version of the consent text agreed to. |
| `granted_at` | timestamp | Yes | |
| `withdrawn_at` | timestamp | No | If set, grant is no longer active. |
| `evidence` | document/JSON | No | Free-form; may include IP, user agent, screen reference. |

**Audit Shell**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `user_id` | UUID | Yes | Primary key. |
| `deletion_requested_at` | timestamp | Yes | |
| `deletion_completed_at` | timestamp | No | Set when the asynchronous scrub finishes. |
| `request_id` | string | Yes | |

The audit shell holds no personal data. It exists to demonstrate compliance with an erasure request and to track in-flight deletions.

### 5.2 Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| `profile_pk` | `user_id` | Primary lookup. |
| `preferences_pk` | `user_id` | |
| `consent_user_idx` | `user_id`, `withdrawn_at IS NULL` | List active grants. |
| `consent_user_key_idx` | `user_id`, `consent_key` | Withdrawal lookup. |

### 5.3 Access patterns

Reads:
- Self-profile read by the user.
- Other-user profile read by an authorised caller (gated by AuthZ).
- Preferences read on session start.
- Consent grant list during privacy preference UI.

Writes:
- Profile patch on user update.
- Preferences replace.
- Consent grant on user opt-in.
- Consent withdrawal.
- PII scrub on account deletion.

### 5.4 Encryption

- Database-level encryption at rest is required.
- Column-level encryption with KMS-managed keys is required for every identifiable personal field.
- Encryption keys rotate annually; old ciphertext is re-encrypted in a background job.
- The encryption key is held in a KMS distinct from the AuthN MFA secret KMS, to keep blast radii independent.

### 5.5 Retention

- Profile data retained while the account is active.
- On erasure request, all PII is scrubbed within 60 seconds of the `UserDeletionRequested` event.
- Audit shell row retained for the statutory record-keeping period (typically 6 years; assess locally), then itself erased.
- Consent grant records retained for the full statutory period for accountability under GDPR Article 5(2).

### 5.6 Backup and disaster recovery

- Daily backup, 35-day point-in-time recovery, weekly restore test.
- Backup encrypted with an independent key.
- Erasure requests must be replayed against any restored backup; the deletion log is the canonical list of who has been deleted.

---

## 6. Cross-cutting concerns

### 6.1 Identifier strategy

`user_id` is a UUID generated at registration in AuthN and propagated to AuthZ and User Info via the `UserRegistered` event. UUIDs are version 4 (random) by default; version 7 (timestamp-prefixed) is acceptable if the storage benefits from time-ordered keys.

`user_id` is opaque to clients. It is published in events and used in API paths but carries no semantic meaning.

`role_key`, `permission_key`, and `consent_key` are human-readable identifiers chosen at design time. They are stable and case-sensitive.

### 6.2 Time and timezone

All timestamps are stored in UTC. Display localisation happens at the presentation layer using the user's `Preferences.timezone`.

All servers run NTP or chrony with a maximum drift of 50 milliseconds. Decision audit timestamps depend on this.

### 6.3 Soft delete vs hard delete

The system uses soft delete (`deleted_at` tombstone) at write time and hard delete via the asynchronous deletion pipeline within 60 seconds. Soft delete is operationally convenient (immediate indication that a row is no longer active); hard delete is the GDPR-required terminal state.

The credential store and profile store both follow this pattern. The authorisation store hard-deletes assignments for deleted users without an intermediate tombstone, because revoked assignments are already represented by `revoked_at` and a deleted user has no live assignments.

### 6.4 Migration policy

Schema changes follow a forwards-and-backwards-compatible migration pattern:

1. Deploy schema change that adds new structures without removing old ones.
2. Deploy application code that writes to both old and new structures.
3. Backfill data into the new structure.
4. Deploy application code that reads from the new structure only.
5. Deploy schema change that removes the old structures.

Each step is independently deployable and reversible. Breaking schema changes are not permitted in production deployments.

### 6.5 Multi-region considerations

If the deployment spans multiple regions:

- The credential store is the primary regional pin point. Cross-region replication of credentials raises GDPR transfer questions and PCI-equivalent residency questions.
- The profile store is similarly region-pinned.
- The authorisation store can replicate widely because the data is not personal.
- The decision audit log can replicate widely.

Cross-region replication topology is a separate decision documented in `auth-separation_deployment-topology_v1.md`.

### 6.6 No shared database

The conformance requirements in `auth-separation_architecture_v1.md` and the three compliance docs all depend on this: the four stores live in four databases, and no two services share connection strings, credentials, or instances. A shared database undermines every separation argument the rest of the spec set makes.

This is the property that an external assessor will probe first. Make it true and provable.

---

## 7. What the database does NOT hold

The boundaries between the stores are as important as their contents. The following anti-statements hold:

- The credential store does not hold names, emails (other than the login identifier itself), profile attributes, role assignments, or permission catalogues.
- The authorisation store does not hold passwords, MFA secrets, or any personally identifiable attribute beyond the user_id.
- The profile store does not hold credentials, MFA secrets, role assignments, or permissions.
- The decision audit log does not hold the values of resource fields the user accessed; it holds the action and the resource identifier only.
- No store holds raw access tokens or refresh tokens; only their hashes or ciphertext.
- No store holds the plaintext value of any field that has a hashed or encrypted equivalent.

If any of these statements becomes false, the spec is wrong and the deployment is non-conformant.

---

## 8. Required database capabilities (technology-agnostic)

A concrete database product is suitable for any of the four stores if it satisfies the capability requirements below for that store.

### 8.1 Capabilities required by all four stores

- Encryption at rest with a customer-managed key.
- Authenticated client connections with TLS 1.3.
- Role-based access control at the database level so that the application's connection cannot perform schema migrations, and the migration tool's connection cannot read sensitive columns.
- Backup with point-in-time recovery to at least 35 days.
- Restore tested at least weekly in production-equivalent environments.

### 8.2 Credential store

- Strong consistency on the user record (no eventual consistency on password updates).
- Unique constraint enforcement on `login_identifier`.
- Sub-millisecond p99 read latency on the user lookup.
- Write throughput sufficient for password change rates (low) and refresh token churn (moderate).
- Column-level encryption support for MFA secrets, or compatibility with envelope encryption at the application layer.

### 8.3 Authorisation store

- Strong consistency on role assignments (an admin who assigns a role expects the next decision check to honour it).
- Sub-millisecond p99 read latency on the assignments-by-user query.
- Compound index support across `(user_id, revoked_at IS NULL)` or equivalent.
- Idempotency-key uniqueness enforcement.
- Compatible with a read-through cache invalidated by event consumption.

### 8.4 Decision audit log

- Append-only mode (UPDATE and DELETE rejected at storage layer, or operationally disabled).
- High write throughput; the design budget is "every protected request in the platform produces one entry."
- Compatible with forwarding to immutable object storage in near-real time.
- Time-range queries with reasonable performance; full-table-scan acceptable for forensic queries.

### 8.5 Profile store

- Strong consistency on the user's own profile (a user reading their own profile after a write must see the write).
- Eventual consistency acceptable for cross-user reads of other profiles.
- Column-level encryption support, or compatibility with envelope encryption at the application layer.
- Free-text indexing not required (search lives in a separate read model if at all).

### 8.6 What is NOT required

- A graph database is not required for the role and policy data; a relational structure with indexed lookups is sufficient.
- Full-text search is not required by any store.
- Multi-master cross-region writes are not required by any store.
- Schema-less storage is not required by any store; the schemas in this document are stable enough that an enforced schema is a benefit, not a constraint.

These non-requirements rule out a few flashy technology choices that would otherwise be tempting but add complexity without buying anything the spec needs.

---

## 9. Evolution and breaking changes

Adding a column or table is non-breaking and follows the migration policy in section 6.4.

Removing a column or table is breaking and requires:

- A deprecation period of at least one major release.
- A coordinated update of the corresponding API contract.
- A new major version of the affected OpenAPI document.
- A migration plan that handles the data already in the deprecated column or table.

Renaming a column or table is two changes (add new, deprecate old) executed sequentially per the migration policy.

Changing the type of a column is the same as removing and adding.

Changing the encryption posture of a column (adding column-level encryption to a previously plaintext column) is a one-way migration and requires a re-encryption backfill before the application is allowed to write the new ciphertext form.

---

## 10. Conformance

A deployment claims conformance with this database specification when:

1. The four stores are physically separate databases owned by their respective services.
2. No service has read or write access to a database it does not own.
3. The schemas in sections 2 to 5 are implemented faithfully, with the listed indexes present.
4. Encryption at rest is enabled on every store, with KMS-managed keys.
5. Column-level encryption protects every identifiable personal field in the profile store and every secret field in the credential store.
6. Backup with at least 35-day point-in-time recovery is enabled and weekly restore tests are passing.
7. The decision audit log is append-only at the storage layer and forwarded to immutable storage.
8. Schema migrations follow the forwards-and-backwards-compatible policy.
9. The conformance checklists in `auth-separation_README_v1.md` and the three compliance docs are met.

Failure of any item is a remediation owner with a tracked closure date.
