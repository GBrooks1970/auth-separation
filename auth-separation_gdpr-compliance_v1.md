# Compliance Scope: GDPR for the User Info Service

The User Info service owns personal data: display names, given and family names, avatar URLs, pronouns, biographical text, preferences, locale, timezone, and consent records. Under the EU General Data Protection Regulation (and the UK GDPR / Data Protection Act 2018, which mirror it), every byte of that data triggers controller obligations. This document is the GDPR scope for the User Info service: the lawful basis the data is held under, the rights the data subject can exercise, the controls the service implements, and the boundary that keeps GDPR scope contained within this one service.

The same posture transfers cleanly to other personal-data regimes (CCPA, LGPD, POPIA, PIPEDA). The vocabulary changes; the controls do not.

---

## 1. Why this scope sits in User Info alone

Personal data lives in one place, behind one set of access controls, with one operational owner. The architecture document already establishes that:

- AuthN holds credentials but no profile data. Credential breach does not expose names, preferences, or consent state.
- AuthZ holds roles and permissions but never stores personal attributes. A compromise of AuthZ does not return personal data to the attacker.
- User Info is the single service through which a data subject access request, a rectification request, a portability request, or an erasure request is fulfilled.

This containment is the practical justification for separation. A monolithic service mixing credentials, permissions, and personal data would force every access path through GDPR review. By isolating personal data in User Info, the GDPR audit boundary is one service, one schema, one set of logs.

Out of scope by construction: AuthN credential store, AuthZ role and policy store, the message bus (carries event metadata only; payloads never include name, email content, or other personal fields beyond the user_id), every business-domain service that consumes user_id without holding personal attributes itself.

---

## 2. Lawful basis and processing purposes

For each category of personal data the service processes, a lawful basis under GDPR Article 6 is identified at design time and recorded in the data inventory.

| Data category | Examples | Lawful basis | Purpose |
|---------------|----------|--------------|---------|
| Identity attributes | display_name, given_name, family_name, pronouns, avatar_url, bio | Contract (Art 6(1)(b)) | Operate the user-facing service the user has signed up for |
| Preferences | locale, timezone, notification opt-ins, accessibility | Contract / Legitimate interest | Personalise the service experience |
| Consent grants | marketing.email, analytics.product, sharing.partners | Consent (Art 6(1)(a)) | Activities the user has explicitly opted into |
| Audit metadata | created_at, updated_at, IP addresses captured for consent evidence | Legal obligation (Art 6(1)(c)) | Demonstrate accountability under Art 5(2) |

Processing for any new purpose requires a new entry in this table, a privacy review, and where the basis is consent, an updated consent prompt with a new version string. The `version` field on the `ConsentGrant` schema in `auth-separation_userinfo-api_v1.yaml` is the technical anchor for this.

---

## 3. Data subject rights

GDPR grants eight rights to data subjects. The User Info API is built so that each one is satisfied by a defined endpoint or operational procedure.

### Article 15: Right of access

The data subject can request a copy of the personal data the controller holds about them. The API provides this through `POST /users/me/export`, which returns 202 and triggers an asynchronous export delivered out of band (typically a signed URL emailed to the verified contact).

The export must include every personal attribute the service holds, the consent grant history, the preferences, and the audit metadata. It must not include credentials (those live in AuthN, not here) or role assignments (AuthZ).

Statutory response window: one calendar month, extendable to three months for complex requests. The implementation should target seven days as an internal SLO.

### Article 16: Right to rectification

The data subject can correct inaccurate personal data. The API provides this through `PATCH /users/me`. Partial updates are accepted; unspecified fields remain unchanged.

A rectification triggers a `ProfileUpdated` event listing the field names that changed. The event payload deliberately does not include the values, so the bus does not become a secondary store of personal data.

### Article 17: Right to erasure ("right to be forgotten")

The data subject can request deletion of all personal data. The API provides this through `DELETE /users/me`, which returns 202 and emits a `UserDeletionRequested` event on the `user.lifecycle` channel. Deletion propagates asynchronously across all three services per the architecture document section 4.5.

Within 60 seconds (the SLO embedded in the acceptance scenarios):

- AuthN revokes all sessions, deletes the credential row, deletes MFA secrets and refresh tokens.
- AuthZ removes every role assignment for the user_id.
- User Info scrubs every PII column. An audit-only shell row is retained holding only `user_id`, `deletion_requested_at`, `deletion_completed_at`, and `request_id`.

A `UserDeleted` event is published listing all three services that have completed their part. The retained audit row exists to demonstrate compliance with the request, not to enable re-identification, and is itself erased after the statutory record-keeping period (typically six years in the UK and EU for tax and accounting overlaps; assess locally).

Right of erasure can be refused under narrow grounds (legal obligation to retain, public interest, defence of legal claims). Where refusal is invoked, the response includes the lawful ground and the data subject's right to complain to the supervisory authority.

### Article 18: Right to restriction of processing

The data subject can require that personal data is held but not processed. This is operational rather than purely technical: a flag on the audit shell row marks the account as restricted, and downstream consumers consult this flag before processing. The current spec does not expose a public API for this; it is handled through a support workflow with an audit trail.

### Article 20: Right to data portability

Closely related to Article 15. The export delivered by `POST /users/me/export` must be in a structured, commonly used, machine-readable format (JSON is the default; CSV available on request). The schema published in the export documentation matches the User Info OpenAPI definition so that the export is self-describing.

### Article 21: Right to object

The data subject can object to processing carried out under legitimate interest, and absolutely to processing for direct marketing. The marketing object case is implemented through `DELETE /users/me/consents/marketing.email` (and equivalents for other marketing keys). Objection to legitimate-interest processing is handled by the same support workflow as restriction.

### Article 22: Rights related to automated decision-making

The current service does no automated decision-making with legal or similarly significant effects on the data subject. If a future capability adds such decision-making, an Article 22 review is required before it ships.

### Article 7(3): Right to withdraw consent

For any consent grant recorded under Article 6(1)(a), withdrawal must be as easy as the original grant. `DELETE /users/me/consents/{consentKey}` satisfies this. A `ConsentWithdrawn` event notifies downstream consumers, who must cease the consented processing before the next batch run.

---

## 4. Controller obligations

Beyond the data subject rights above, the service implements the following controller-side controls.

### Data minimisation (Article 5(1)(c))

Profile fields are optional. The service does not require the user to populate name, pronouns, or bio. Default values are blank, not synthesised. The schema in `auth-separation_userinfo-api_v1.yaml` deliberately marks only `user_id` as required on the `Profile` object.

### Purpose limitation (Article 5(1)(b))

Personal data is processed only for the purposes recorded in section 2. Any new internal use case (analytics on profile fields, recommendations based on bio text, training data for a model) requires a privacy impact assessment before it begins, and a consent or legitimate-interest assessment as appropriate.

### Storage limitation (Article 5(1)(e))

Personal data is retained only while the contract relationship is active. On account deletion, the scrubbing procedure described in Article 17 above removes it. The audit shell row has its own retention timer.

### Accuracy (Article 5(1)(d))

The rectification endpoint exists. Periodic prompts to the user to confirm preferences (annual or major-version updates of consent text) refresh accuracy without becoming nag patterns.

### Integrity and confidentiality (Article 5(1)(f))

All PII columns are encrypted at rest using a KMS-managed key. Decryption happens only inside the User Info process boundary. Backups inherit the same encryption. TLS 1.3 protects data in transit. Access to the User Info database from operations engineers is just-in-time and logged.

The `ProfileUpdated` event publishes only the names of changed fields, not the values, to keep personal data off the bus.

### Accountability (Article 5(2))

The service maintains a Record of Processing Activities (RoPA) reflecting the data inventory in section 2. The RoPA is reviewed quarterly and on every schema change.

### Data Protection Impact Assessment (Article 35)

A DPIA was conducted for the original deployment. A new DPIA is triggered by any of: introduction of new categories of personal data, new processing purposes, transfer to a new jurisdiction, or integration with a new third-party processor.

### Data Protection Officer (Article 37)

If the controlling organisation is required to appoint a DPO under Article 37(1), the User Info service is part of that DPO's portfolio. The DPO's contact details are published in the privacy notice that the service references.

### Records of consent (Article 7)

Every consent grant captures the version of the consent text the user agreed to, the timestamp, and an evidence object that may include the IP address, user agent, and a reference to the screen shown. The `ConsentGrant` schema in the User Info OpenAPI spec is the authoritative record.

---

## 5. International transfers (Articles 44 to 50)

If the service operates across jurisdictions, transfers of personal data outside the EU/EEA require a lawful transfer mechanism: an adequacy decision, Standard Contractual Clauses with supplementary measures, or Binding Corporate Rules.

The deployment posture should:

- Pin User Info storage and compute to a defined region or set of regions.
- Document any sub-processor (cloud provider, KMS provider, observability vendor) and the transfer basis for each.
- Maintain a Transfer Impact Assessment for any non-adequacy destination.

---

## 6. Breach notification (Articles 33 and 34)

A personal data breach must be notified to the supervisory authority within 72 hours of awareness, unless the breach is unlikely to result in a risk to the rights and freedoms of natural persons. High-risk breaches must additionally be communicated to affected data subjects without undue delay.

The User Info incident response procedure:

1. **Detect.** Anomaly detection on access logs (unexpected query volumes, off-hours administrative reads, unusual export request patterns) triggers an alert.
2. **Triage.** The on-call engineer assesses whether the event is a confirmed breach, a near-miss, or a false positive. Confirmed breaches engage the DPO within one hour.
3. **Contain.** Revoke compromised credentials via AuthN (`SessionRevoked` events with reason `suspected_compromise`). Block the suspect access path. Snapshot logs for forensic preservation.
4. **Assess.** Identify the categories of personal data affected, the number of data subjects, and the likely consequences.
5. **Notify.** If notifiable, the DPO files with the supervisory authority within 72 hours. The notification template is held in the runbook.
6. **Communicate.** If high risk to data subjects, individual notifications follow the corporate breach communications policy.
7. **Postmortem.** Blameless review within 14 days, corrective actions tracked to closure.

A credential-only breach handled by AuthN does not automatically constitute a personal data breach under GDPR, but the User Info team is informed by the standard incident workflow because the login identifier (typically an email address) is itself personal data.

---

## 7. Vendor and sub-processor management

Every third party that processes personal data on behalf of the controller is a sub-processor under Article 28. For User Info, the typical list is:

- The cloud provider hosting the database and compute.
- The KMS or HSM provider holding encryption keys.
- The object storage provider holding the asynchronous data export artefacts.
- The email or messaging provider used to deliver consent confirmations and export links.
- The observability vendor receiving logs and metrics (must be configured to drop personal data fields).

Each sub-processor has a current Data Processing Agreement, an annual review of their security posture, and a recorded transfer mechanism if outside the EEA.

---

## 8. Logging and observability constraints

Logs and metrics are themselves personal data when they include user identifiers. The service applies the following constraints:

- Logs include `user_id` (a UUID, pseudonymous to anyone without the User Info store) but never `display_name`, `given_name`, `family_name`, or `email`.
- Request bodies are not logged in production. If a request fails schema validation, the log records the field name and rule that failed, not the rejected value.
- Observability data is retained for 90 days online and 13 months in cold storage by default. Longer retention requires a documented purpose and a privacy review.
- Log access is restricted to engineers with a current need; access is logged.

---

## 9. Out-of-scope items that consumers of User Info must handle

GDPR compliance for personal data does not absolve callers of the service. The following are explicitly the responsibility of the consuming application:

- **Front-end clients** must not cache personal data beyond the session, must not log it client-side, and must not forward it to third-party analytics without consent.
- **Resource APIs** that retrieve a profile via `GET /users/{userId}` must respect the AuthZ decision and must not persist the returned fields beyond the request lifecycle unless they have their own lawful basis.
- **Search and recommendation systems** that index profile fields are themselves controllers or processors of that data and must execute their own DPIA.
- **Backups owned by other services** are subject to the same erasure obligations; an erasure request scopes to "all systems holding the personal data," not just User Info's primary store.

---

## 10. Conformance checklist

A deployment claims GDPR-aligned posture for User Info when it can demonstrate:

1. Every category of personal data has a recorded lawful basis.
2. Profile fields are optional except `user_id`; data minimisation is enforced at the schema level.
3. PII columns are encrypted at rest with KMS-managed keys.
4. The export, rectification, withdrawal, and erasure endpoints function end-to-end and meet their SLOs.
5. Account deletion completes across AuthN, AuthZ, and User Info within 60 seconds, with `UserDeleted` confirmation.
6. The audit shell row contains no personal attributes.
7. Events on the bus carry no personal attribute values.
8. Logs contain `user_id` only, never names or contact details.
9. A current RoPA exists and has been reviewed in the last quarter.
10. A current DPIA exists and has been reviewed within the last 12 months or on the most recent material change.
11. Sub-processor list is current and each entry has a valid DPA and transfer basis.
12. The breach response runbook has been rehearsed in the last 12 months.

If any item fails, the gap is logged, a remediation owner is assigned, and the gap is reported to the DPO.
