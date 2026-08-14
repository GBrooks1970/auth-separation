# Compliance Scope: SOC 2 for the AuthZ Service

The AuthZ service is the system of record for who can do what. Every protected request in the platform passes through it. That makes it the single most important component for demonstrating that access is granted on a defined basis, that decisions are reproducible, that role and policy changes are controlled, and that the audit trail can be produced on demand. Those are the questions a SOC 2 examiner asks, and the AuthZ service is where the answers live.

This document is the SOC 2 scope for AuthZ. It maps the relevant Trust Services Criteria to concrete controls in the service, references the API endpoints and event types that implement those controls, and defines the boundary that contains SOC 2 attention within this service.

The framing assumes a SOC 2 Type II report covering Security as the mandatory criterion, with Availability and Confidentiality as additional criteria where the platform commits to them. Processing Integrity and Privacy are addressed only insofar as they touch access decisions; broader Privacy concerns sit with the User Info GDPR scope.

---

## 1. Why this scope sits in AuthZ alone

The architecture document puts every authorisation decision through one service. That separation is what makes a SOC 2 audit of access controls tractable.

- AuthN proves who the caller is, but does not decide what they are allowed to do. Role and permission claims are deliberately kept out of access tokens.
- User Info holds personal attributes but never adjudicates access to them; it asks AuthZ.
- Resource APIs treat AuthZ as the authoritative oracle. None of them maintains its own role model.

This containment means the SOC 2 examiner reviews one service for access control evidence, one audit log for decision history, one change-management process for role and policy modifications, and one set of operational runbooks. A federation of resource APIs each maintaining their own role logic would make this evidence collection painful at best and impossible at worst.

Out of scope for the AuthZ SOC 2 boundary by construction: AuthN credential handling (covered by `auth-separation_pci-compliance_v1.md`), User Info personal data handling (covered by `auth-separation_gdpr-compliance_v1.md`), business-domain processing logic in resource APIs (each owner runs their own SOC 2 scope if applicable).

---

## 2. Trust Services Criteria mapped to AuthZ

The AICPA Trust Services Criteria (2017, updated 2022) are organised into common criteria (CC1 to CC9) shared across all categories, plus criteria specific to Availability, Processing Integrity, Confidentiality, and Privacy. The mapping below covers the criteria most relevant to access decision systems.

### CC1: Control Environment

**CC1.1 to CC1.5** require a defined organisation, ethical commitment, board oversight, competence, and accountability. For AuthZ, the team owning the service is named, has documented role descriptions, and reports through a defined management structure. Background checks for engineers with production access happen at hire and on role change.

### CC2: Communication and Information

**CC2.1 to CC2.3** require that information needed to operate controls is communicated. The AuthZ team maintains operational documentation: an architecture document (already in this folder), runbooks for the on-call rotation, a permission catalogue exposed at `GET /permissions`, a role catalogue at `GET /roles`, and decision audit accessible at `GET /audit/decisions`. Internal change announcements use a defined channel (release notes, internal Slack equivalent).

### CC3: Risk Assessment

**CC3.1 to CC3.4** require ongoing risk assessment. The AuthZ team conducts a documented risk assessment annually, plus on every significant architectural change. Risks specific to access decisions include: stale role assignments, policy misconfiguration, decision latency leading to fail-open behaviour in resource APIs, and audit log tampering.

### CC4: Monitoring Activities

**CC4.1 to CC4.2** require monitoring of controls. AuthZ exposes operational metrics: decision latency p50/p95/p99, decision rate by allow/deny, role assignment churn, policy change frequency. Alerts fire on anomalies.

### CC5: Control Activities

**CC5.1 to CC5.3** require defined control activities, documented technology, and policies. The control activities for AuthZ are listed in section 3 of this document. They are versioned alongside the code.

### CC6: Logical and Physical Access Controls

This is where AuthZ does most of its work. The relevant criteria:

**CC6.1: Logical access security**. AuthZ enforces logical access for the entire platform. The service implements this through:

- The `POST /decisions/check` endpoint that returns an allow or deny for every protected request.
- Tokens issued by AuthN that AuthZ verifies before considering the request authenticated.
- Role and policy data stored in the AuthZ database, modified only through authenticated admin endpoints.

**CC6.2: Authentication of users before access**. AuthZ never decides for an unauthenticated caller. Every endpoint requires `bearerAuth` per the OpenAPI spec. Tokens are validated against the AuthN JWKS endpoint.

**CC6.3: Role-based access control**. Roles are first-class entities in the AuthZ data model. Permissions attach to roles, roles attach to users (optionally scoped, optionally time-bound). The `RoleAssignment` schema captures `assigned_at`, `scope`, and `expires_at`.

**CC6.4: Restriction of physical access**. Inherited from the cloud provider's SOC 2 report. The AuthZ deployment runs in regions with attested physical controls.

**CC6.5: Logical access removal upon termination**. When a user is removed, every role assignment is removed (`RoleRevoked` events for each). When an employee leaves the organisation, the offboarding workflow includes revoking their role bindings; this is testable by querying `GET /users/{userId}/roles` and confirming the empty list.

**CC6.6: Authentication for software changes**. Role and policy changes go through the admin endpoints, which require an authenticated admin token plus an Idempotency-Key header. Every change is auditable.

**CC6.7: Restriction of information transmission**. Decisions are returned to the calling resource API and are not echoed elsewhere. The bus carries `RoleAssigned`, `RoleRevoked`, and `PolicyChanged` events for cache invalidation, but not the per-request decision stream.

**CC6.8: Detection and prevention of unauthorised changes**. Direct database access in production is forbidden. Schema migrations follow the change management workflow described under CC8. Any role or policy modification that bypasses the admin endpoints is detected via reconciliation between the `audit/decisions` event stream and the database state.

### CC7: System Operations

**CC7.1: Detection of vulnerabilities**. Static analysis on every PR, dependency scanning on every build, weekly authenticated vulnerability scans against the running service.

**CC7.2: Incident detection**. Anomaly detection on decision volumes, deny-rate spikes, and policy change frequency. Alerts route to the on-call rotation.

**CC7.3: Incident response**. Incident response runbook covers permission misconfiguration, suspected privilege escalation, and audit log integrity events. Annual tabletop exercise.

**CC7.4: Incident communication**. Customer-facing incident communications follow the platform-wide template; AuthZ-specific incidents are tagged for trend analysis.

**CC7.5: Continuous evaluation**. Quarterly review of incidents and near-misses; corrective actions tracked to closure.

### CC8: Change Management

**CC8.1 requires authorised, documented, tested change.** For AuthZ this means:

- Code changes go through pull request, code review, automated tests, and a CI pipeline.
- Role and policy changes go through the admin endpoints, with change events recorded on the bus.
- Schema migrations are version-controlled, applied through the deployment pipeline, and reversible.
- Emergency changes follow a documented break-glass procedure with retroactive review within 24 hours.

The `PolicyChanged` event records every create, replace, and delete on the policy store and is the auditable record of policy lifecycle.

### CC9: Risk Mitigation

**CC9.1: Identify, select, and develop risk mitigation activities**. Documented in the risk assessment cycle described under CC3.

**CC9.2: Assess and manage vendor risk**. Sub-processors with access to the AuthZ data plane (cloud provider, KMS, observability vendor) have current Data Processing Agreements and current SOC 2 reports of their own.

### Availability (A1)

**A1.1: Capacity**. AuthZ runs with autoscaling configured against the decision-rate metric. Capacity testing happens quarterly under load equivalent to two times current peak.

**A1.2: Backup and recovery**. Role, permission, and policy data is backed up daily with weekly restore tests. The decision audit log is forwarded to immutable storage in real time and is recoverable independently.

**A1.3: Recovery testing**. Disaster recovery tested annually, with documented RTO and RPO targets.

### Confidentiality (C1)

**C1.1: Identification of confidential information**. Role definitions and policy bindings are themselves sensitive (knowing them helps an attacker target). They are classified as Internal-Confidential and not exposed externally.

**C1.2: Disposal**. When a tenant or organisation is offboarded, their role and policy data is purged within the contracted retention window.

### Processing Integrity (PI1)

**PI1.1 to PI1.5** require complete, valid, accurate, timely, and authorised processing. For decision processing:

- **Complete**. Every decision request returns exactly one decision response. Batch requests return decisions in input order. The OpenAPI schema enforces this.
- **Valid**. Decision requests that fail schema validation return 400 with the violation; they do not produce a default decision.
- **Accurate**. Given the same inputs (user_id, action, resource, context, role state, policy state), AuthZ returns the same decision. Determinism is a tested property.
- **Timely**. Decisions return within the published latency budget (sub-10ms p99 at the edge per the architecture document).
- **Authorised**. The admin endpoints that mutate roles and policies require a token with the appropriate `role.write` and `policy.write` permissions, themselves managed by AuthZ (recursively, with a bootstrap policy for the initial admin).

### Privacy

Privacy criteria largely sit with the User Info service (see `auth-separation_gdpr-compliance_v1.md`). AuthZ touches privacy only insofar as the decision audit log records `user_id`, `action`, `resource`, and `decided_at`. The audit log is itself classified as Internal-Confidential and access is restricted on a need-to-know basis.

---

## 3. Concrete control activities

The following control activities are implemented in the AuthZ service and tested as part of the SOC 2 evidence collection.

### Access decision controls

| Control ID | Control |
|------------|---------|
| AZ-DEC-01 | Every protected request results in a decision check call. Resource APIs without an AuthZ call are flagged in code review. |
| AZ-DEC-02 | Decisions are deterministic given a fixed input set. Property-based tests assert this on every CI run. |
| AZ-DEC-03 | Default decision is deny. Missing role data, missing policy data, and unknown actions all return `allowed=false`. |
| AZ-DEC-04 | Decision latency is monitored and alerted. Sustained breach of the latency SLO escalates to on-call. |

### Role and permission management controls

| Control ID | Control |
|------------|---------|
| AZ-ROLE-01 | Role definitions follow the published `RoleDefinition` schema. Schema violations are rejected at write time. |
| AZ-ROLE-02 | Role assignments and revocations emit `RoleAssigned` and `RoleRevoked` events with the assigning identity captured. |
| AZ-ROLE-03 | Role assignments support optional `expires_at`. Expired assignments are honoured (treated as revoked) on the next decision check. |
| AZ-ROLE-04 | Idempotency-Key on assign-role prevents duplicate assignments from retried requests. |
| AZ-ROLE-05 | Role definitions are reviewed quarterly; orphaned roles (no assignments, no policy references) are flagged for retirement. |

### Policy management controls

| Control ID | Control |
|------------|---------|
| AZ-POL-01 | Policies are versioned through `PolicyChanged` events with change types `created`, `replaced`, `deleted`. |
| AZ-POL-02 | Policy creation requires an authenticated admin token with `policy.write` permission. |
| AZ-POL-03 | Policy testing in a staging environment is required before promotion to production. The promotion is itself a `PolicyChanged` event recorded in the audit trail. |
| AZ-POL-04 | Policy condition evaluation is sandboxed; conditions cannot escape the evaluation context. |

### Audit controls

| Control ID | Control |
|------------|---------|
| AZ-AUD-01 | Every decision is logged with a stable identifier, the user_id, the action, the resource, the allow/deny, the matched rule, and a timestamp from a synchronised time source. |
| AZ-AUD-02 | Audit log is forwarded to immutable storage within seconds of generation. |
| AZ-AUD-03 | Audit retention is at least 12 months online and 36 months in cold storage. |
| AZ-AUD-04 | Audit log access via `GET /audit/decisions` is itself authorised; non-admin callers cannot read other users' decisions. |
| AZ-AUD-05 | Tamper detection runs hourly: hash chains in the audit storage are validated against the running ledger. |

### Operational controls

| Control ID | Control |
|------------|---------|
| AZ-OPS-01 | Production access is just-in-time, MFA-protected, brokered through a logged approval workflow. |
| AZ-OPS-02 | Deployments use blue-green or canary patterns. Rollback is one-command and tested. |
| AZ-OPS-03 | Service is deployed in at least two availability zones. |
| AZ-OPS-04 | Incident postmortems are blameless and tracked to closure with a 14-day target. |
| AZ-OPS-05 | Quarterly access review: the named on-call rotation matches the actual list of accounts with production access. |

---

## 4. Evidence the examiner will request

A SOC 2 Type II examination covers a period (typically 6 to 12 months) and requires evidence that the controls operated continuously across that period. For AuthZ, the evidence pack typically includes:

| Evidence | Source |
|----------|--------|
| Sample decision audit entries demonstrating allow and deny outcomes | `GET /audit/decisions` filtered by date range |
| Sample `RoleAssigned` and `RoleRevoked` events | Bus consumer or audit log |
| Sample `PolicyChanged` events demonstrating change-managed policy updates | Bus consumer or audit log |
| Code review approvals for AuthZ codebase changes | Source control system |
| CI build records showing tests passed | CI system |
| Vulnerability scan reports and remediation tickets | Security tool dashboards |
| On-call rotation history and incident records | Incident management system |
| Quarterly access review documentation | Internal audit folder |
| Annual disaster recovery test report | Operations team |
| Annual risk assessment | Security team |

The audit endpoint and the event bus are the two primary sources. Designing them as first-class API surfaces rather than internal-only artefacts is what keeps the examination cost-effective.

---

## 5. Failure modes the controls address

| Failure mode | Detecting control | Mitigating control |
|--------------|-------------------|--------------------|
| Stale role assignment (employee changed roles, old role not revoked) | Quarterly access review (AZ-OPS-05) | `expires_at` on time-bound assignments (AZ-ROLE-03) |
| Permission misconfiguration in a new policy | Staging promotion requirement (AZ-POL-03) | Rapid rollback via `PolicyChanged` event with `change=deleted` (AZ-POL-01) |
| Audit log tampering | Tamper detection hash chain (AZ-AUD-05) | Immutable forwarding (AZ-AUD-02) |
| Decision endpoint outage causing fail-open in resource APIs | Decision latency alerting (AZ-DEC-04) | Resource APIs default to deny on AuthZ unavailability (architectural requirement) |
| Bypass of admin endpoints via direct database access | Production access controls (AZ-OPS-01) | Reconciliation between audit events and database state (CC6.8) |
| Privileged engineer abusing access | JIT access broker logging | Quarterly access review and privileged session recording |

---

## 6. Out-of-scope items that consumers of AuthZ must handle

SOC 2 attestation for AuthZ does not extend to the rest of the platform. The following remain the responsibility of the consuming service or business owner:

- **Resource APIs** must call AuthZ on every protected request. A resource API that caches decisions beyond the documented TTL is outside the AuthZ control boundary and is responsible for its own decision freshness.
- **Resource APIs** must default to deny on AuthZ unavailability. Failing open is a control failure on the resource API's side, not on AuthZ's.
- **Admin tooling** (whatever UI or CLI the platform provides for managing roles and policies) is itself in SOC 2 scope for change management; it must use the AuthZ admin endpoints rather than reaching into the database.
- **Identity proofing** (verifying that an admin requesting a role assignment is the person they claim to be) is AuthN's job, not AuthZ's.

---

## 7. Conformance checklist

A deployment claims SOC 2-aligned posture for AuthZ when it can demonstrate:

1. Every endpoint in `auth-separation_authz-api_v1.yaml` is reachable and behaves per the spec.
2. The decision audit log is complete for the attestation period, with no detected gaps and no detected tampering.
3. Role assignments, revocations, and policy changes during the attestation period are accounted for in the event stream and the database matches.
4. Production access during the attestation period was JIT-only; no standing access exceptions are unaccounted for.
5. Decision latency met its SLO across the attestation period.
6. The on-call rotation for the period matches the identities holding production access.
7. Quarterly access reviews were executed and documented.
8. Incident postmortems for the period are closed with corrective actions tracked.
9. The annual risk assessment and disaster recovery test were executed within the period.
10. Sub-processors with AuthZ-relevant access have current SOC 2 reports of their own.

If any item fails, the gap is logged, a remediation owner is assigned, and the gap is reported to the security committee and the external examiner.
