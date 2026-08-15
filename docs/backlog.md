<!--
  AUDIENCE: Engineers, AI agents, and project leads maintaining work-in-progress tracking.
  PURPOSE:  Single source of truth for outstanding work, risks, and delivery planning
            for the auth-separation project.
  LOCATION: docs/backlog.md
  TEMPLATE: ../../templates/backlog.template.md (portfolio root)
-->

# auth-separation — Backlog

**Version:** 6 — AS-06 resolved. **Zero outstanding `AS-nn` items**; the implementation programme (`AUTH-001` onward) is all that remains.
**Last Updated:** 2026-08-15
**Based on:** `auth-separation_implementation-kanban_v1.html` (51 tickets, payload `generatedAt` 2026-04-26 22:50:00Z, board version 1.0) and the README production-readiness checklist

This backlog tracks two distinct bodies of work: the **portfolio-integration items** (`AS-nn`) that make this
repository a first-class portfolio project, and the **implementation programme** (`AUTH-nnn`) that builds the
system the spec set describes. Ordering principle: `AS-nn` items are ordered by priority score and are
actionable now; `AUTH-nnn` items are ordered by phase and gated by an explicit dependency chain, so their
sequence is fixed by the graph rather than by score.

**Priority Scoring System:**
- **Score = Security Impact (0–10) + Breakage Probability (0–10) + Maintenance Burden (0–10)**
- **HIGH (20–30):** Critical — immediate action required
- **MEDIUM (10–19):** Important — schedule within current sprint cycle
- **LOW (0–9):** Desirable — schedule when capacity allows

## Status

🟡 **Specification complete and validated, nothing implemented.** The 13-artefact SDD spec set is in place
and diff-verified against its canonical source. No service code and no deployment — but the specs are now
machine-checked by `npm run verify` in CI. The repository's first commit (`7d4dbcd`) is the spec set alone —
deliberate SDD evidence that the specification preceded the code.

Published at **https://github.com/GBrooks1970/auth-separation** (public, MIT), registered in the portfolio
(`presentation_role: methodology`, gate `npm run verify`), with `WORKLIST_auth-separation.md` tracked at the
portfolio root.

**Portfolio integration is complete.** `AS-01`..`AS-06` are all closed, so the only work left is the
implementation programme itself, starting at `AUTH-001`.

**Definition of done for the whole project** is the 10-item production-readiness checklist in
`auth-separation_README_v1.md` §"Production-readiness checklist". Do not restate it here; that list is
authoritative and `AUTH-084` is the ticket that asserts it.

## Deviations from the backlog template

Recorded explicitly so a successor does not read these as omissions:

1. **`AUTH-nnn` items carry the spec set's own `P0`/`P1`/`P2` priority, not a three-axis score.** They are
   planned build work, not remediation risks; inventing Security/Breakage/Maintenance numbers for 51
   unbuilt tickets would fabricate precision the source does not have. The HIGH/MEDIUM/LOW column is a
   derived view for portfolio tooling, mapped `P0 → HIGH`, `P1 → MEDIUM`, `P2 → LOW`. Only the `AS-nn`
   items carry real scored breakdowns.
2. **`AUTH-nnn` items carry no effort estimates.** The Kanban does not record any. Estimate at the point
   a ticket is picked up, and record the actual in the implementation log.
3. **Full ticket detail lives in the Kanban, not here.** Each `AUTH-nnn` ticket has a description,
   3–5 acceptance criteria, a spec rationale, and a `blocks` list in
   `auth-separation_implementation-kanban_v1.html`. This backlog carries identity, ordering, dependency
   and status; the Kanban carries the body. **Sync rule:** this file is the source of truth for *status*;
   the Kanban is the source of truth for *content*. When a ticket's status changes, update this file — and
   only regenerate the Kanban's `payload-tickets` status field when doing a deliberate board refresh.

---

## Outstanding Risks

### HIGH Priority (Score: 20–30)

None. No implemented surface exists yet, so no live security, breakage, or maintenance exposure — the
risk profile is entirely forward-looking and expressed as the `AUTH-nnn` programme below.

---

### MEDIUM Priority (Score: 10–19)

None outstanding.

---

### LOW Priority (Score: 0–9)

None outstanding.

---

### Resolved Risks

_Resolved items are kept, never deleted._

#### Risk #AS-06: Only one of the seven feature files declares the service-running Background (Score: 5) ✅ Resolved 2026-08-15

**Decision:** option (a) — environment readiness is the runner's job, asserted once in a `BeforeAll`-style
hook, not repeated as a `Background` in seven files.

**Resolution:** Removed the `Background` from
`features/auth-separation_acceptance-registration-and-first-login_v1.feature`, the only file that carried
it. All seven files are now consistent and no feature file declares one. The four steps are **not lost**:
`features/README.md` states them as the suite-wide precondition that must hold before any scenario runs,
and `AUTH-070`'s acceptance criteria in the Kanban now require the hook that asserts them. The requirement
is unchanged; only its home moved.

Scenario count stays at **21 across 7 files** (a `Background` is not a scenario), `npm run verify` green,
and the Kanban re-rendered offline at 51 cards after the ticket edit.
**See:** PR #4.

#### Risk #AS-01: Repository is local-only and unlicensed (Score: 8) ✅ Resolved 2026-08-15

**Resolution:** Added `LICENSE` (MIT, Gary Brooks 2026, matching the portfolio convention) with a licence
pointer in the README, then published the repository publicly as
**https://github.com/GBrooks1970/auth-separation** and pushed `main`. The spec set was checked for
disclosure risk before publishing: no secrets, keys, or real hostnames — every server URL is
`*.example.internal`.
**See:** commit `563dd6a`.

#### Risk #AS-02: Kanban board depends on three CDN scripts (Score: 12) ✅ Resolved 2026-08-15

**Resolution:** Vendored React 18.2.0, ReactDOM 18.2.0 and `@babel/standalone` 7.23.9 into `vendor/` at
exactly the versions the board already referenced, obtained via `npm pack` from the npm registry rather
than a CDN mirror, and repointed the three `<script src>` tags at the local copies. Provenance and the
"do not repoint at a CDN" rule are recorded in `vendor/README.md`. The README file index now states the
offline guarantee explicitly instead of merely claiming self-containment.

**Verified** in headless Chromium with every non-`file://` request aborted at the network layer:

| | External requests | Page errors | Ticket cards rendered |
|---|---|---|---|
| Before | 3 (cdnjs) | 3 | **0** |
| After | **0** | **0** | **51** |

Babel is 2.8 MB of the 2.9 MB vendored — larger than the "~1 MB" this item originally estimated. The cost
was accepted rather than pre-compiling the JSX: pre-compiling would add a build step to a file whose value
is that it opens from disk and works, and would replace readable inline JSX with compiled output. That
option remains available if repository weight ever matters; the reasoning is recorded in `vendor/README.md`.

#### Risk #AS-05: The acceptance feature bundles seven Features and cannot be run (Score: 11) ✅ Resolved 2026-08-15

**Resolution:** Split into seven files under `features/`, one Feature each, keeping the spec set's
filename versioning convention (`auth-separation_acceptance-<slug>_v1.feature`). A pure restructuring:
**all 21 scenarios preserved verbatim**, confirmed by rebuilding the original body from the seven parts
and comparing byte for byte. `features/README.md` carries the original header comment unchanged plus an
index. The old bundled file is removed; git history retains it.

`scripts/validate-gherkin.mjs` was rewritten to scan `features/`, enforce exactly one `Feature:` per file,
and assert the total scenario count — both guards negative-tested (re-bundling two features fails;
deleting one scenario fails on the count). The `ACCEPT_BUNDLED_FEATURES` allowance is gone, since the
condition it tolerated no longer exists.

Stale references repointed at `features/`: `auth-separation_architecture_v1.md` line 201, the README file
index and prose, and five ticket bodies in the Kanban payload (`AUTH-030`, `AUTH-042`, `AUTH-070` ×2,
`AUTH-072`). The board was re-rendered afterwards to confirm all 51 cards still load.

Left open as `AS-06`: the `Background` sits in one file only, which the split deliberately did not change.
**See:** PR #3.

#### Risk #AS-04: Project is absent from the portfolio registry (Score: 7) ✅ Resolved 2026-08-15

**Resolution:** Added the `registry.yml` row and regenerated the README registry table with
`tools/render-registry.py` (12 projects); `python tools/check-library.py` passes. Added the root-tracked
`WORKLIST_auth-separation.md`, derived from this backlog — the two remaining `AS-nn` items plus `AUTH-001`,
with the other 50 `AUTH-nnn` tickets deliberately omitted because every one is gated behind `AUTH-001`.

Recorded as `presentation_role: methodology` rather than `showcase`: a contracts-only SDD exemplar with
nothing implemented sits in the same family as the prompt library, not alongside the test-automation
suites. Flip it if the project should reach the landing page. `orchestration_target: true`, gate
`npm run verify` — Docker-free and fast, so it is safe for the fan-outs. No `deviations:` block: the
backlog is at the default path.
**See:** portfolio-prompts PR #62 and test-automation-portfolio PR #76.

#### Risk #AS-03: No gate command, so the project cannot be orchestrated (Score: 10) ✅ Resolved 2026-08-15

**Resolution:** Added `npm run verify`, validating all five machine-readable artefacts — the three
OpenAPI 3.1 contracts via `@redocly/cli` (`recommended` ruleset, `redocly.yaml`), the AsyncAPI 3.0 event
contract via `@asyncapi/parser`, and the Gherkin acceptance criteria via the real `@cucumber/gherkin`
parser. Wired as `.github/workflows/ci.yml` on push, PR and manual dispatch. All three legs were
negative-tested against deliberately broken copies to confirm each fails rather than passing vacuously.
Errors fail the gate; style warnings (25 across the OpenAPI files, all `operation-4xx-response` and
description rules) are reported and tolerated, so the linter does not reshape the deliverable.

Substantially discharges `AUTH-006` ("Wire spec linting and validation into CI") — close them together.
The remaining `AS-03` success criterion, naming the gate in the registry row, belongs to `AS-04`.
**See:** PR #1.

---

## Risk Summary

| Priority | Count | Total Effort | Status Distribution |
|---|---|---|---|
| HIGH (20–30) | 0 | — | — |
| MEDIUM (10–19) | 0 | — | — |
| LOW (0–9) | 0 | — | — |
| **Total Outstanding (`AS-nn`)** | **0** | **—** | none |
| Resolved | 6 | 8–12 hrs completed | `AS-01`..`AS-06` |

Implementation programme (`AUTH-nnn`), counted separately and unestimated:

| Priority | Count | Ready | Blocked |
|---|---|---:|---:|
| P0 (HIGH) | 36 | 1 (`AUTH-001`) | 35 |
| P1 (MEDIUM) | 13 | 0 | 13 |
| P2 (LOW) | 2 | 0 | 2 |
| **Total** | **51** | **1** | **50** |

---

## Migration Plans

### Migration Plan — Implement the spec set (8-phase programme)

**Status:** PLANNED
**Effort Estimate:** not estimated — see deviation 2
**Priority:** HIGH

Build the three services the spec set describes, phase by phase, so that a running stack satisfies the
README's 10-item production-readiness checklist. The phase order is fixed by the dependency graph, not by
preference: `AUTH-001` is the only ticket with no blocker, and it unblocks four others. Every ticket's full
acceptance criteria live in the Kanban.

**Working rules that constrain every ticket below** (from the spec set — do not relitigate per ticket):
generated stubs are never hand-edited; the AsyncAPI events are the only sanctioned cross-service channel;
no shared database; the architecture document is read before the API specs; spec versions stay encoded in
filenames, with `info.version` bumped for backwards-compatible additions.

**Stepwise plan:**

### Phase 0 — Foundations (6 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-001` | Set up monorepo and CI/CD scaffolding | Foundation | P0 | HIGH | — | **Ready** |
| `AUTH-002` | Provision KMS and root signing keys | Infrastructure | P0 | HIGH | `AUTH-001` | Backlog |
| `AUTH-003` | Establish service mesh PKI for mTLS | Infrastructure | P0 | HIGH | `AUTH-001`, `AUTH-002` | Backlog |
| `AUTH-004` | Stand up message bus | Infrastructure | P0 | HIGH | `AUTH-003` | Backlog |
| `AUTH-005` | Set up observability stack | Infrastructure | P1 | MEDIUM | `AUTH-001` | Backlog |
| `AUTH-006` | Wire spec linting and validation into CI | Foundation | P1 | MEDIUM | `AUTH-001` | Backlog |

### Phase 1 — Databases (7 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-010` | Provision credential database (AuthN) | Infrastructure | P0 | HIGH | `AUTH-002` | Backlog |
| `AUTH-011` | Migrate credential schema | Feature | P0 | HIGH | `AUTH-010` | Backlog |
| `AUTH-012` | Provision authorisation database (AuthZ) | Infrastructure | P0 | HIGH | `AUTH-002` | Backlog |
| `AUTH-013` | Migrate authorisation schema | Feature | P0 | HIGH | `AUTH-012` | Backlog |
| `AUTH-014` | Provision decision audit log store | Infrastructure | P0 | HIGH | `AUTH-002` | Backlog |
| `AUTH-015` | Provision profile database (User Info) | Infrastructure | P0 | HIGH | `AUTH-002` | Backlog |
| `AUTH-016` | Migrate profile schema with column encryption | Feature | P0 | HIGH | `AUTH-015` | Backlog |

### Phase 2 — Service skeletons (4 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-020` | Generate AuthN server stub from OpenAPI | Feature | P0 | HIGH | `AUTH-006` | Backlog |
| `AUTH-021` | Generate AuthZ server stub from OpenAPI | Feature | P0 | HIGH | `AUTH-006` | Backlog |
| `AUTH-022` | Generate User Info server stub from OpenAPI | Feature | P0 | HIGH | `AUTH-006` | Backlog |
| `AUTH-023` | Generate event publishers and consumers from AsyncAPI | Feature | P0 | HIGH | `AUTH-004`, `AUTH-006` | Backlog |

### Phase 3 — Core implementation (15 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-030` | Implement AuthN registration | Feature | P0 | HIGH | `AUTH-011`, `AUTH-020`, `AUTH-023` | Backlog |
| `AUTH-031` | Implement AuthN login and token issuance | Feature | P0 | HIGH | `AUTH-030`, `AUTH-002` | Backlog |
| `AUTH-032` | Implement JWKS endpoint | Feature | P0 | HIGH | `AUTH-002`, `AUTH-020` | Backlog |
| `AUTH-033` | Implement refresh token rotation | Feature | P0 | HIGH | `AUTH-031` | Backlog |
| `AUTH-034` | Implement password change and reset | Feature | P1 | MEDIUM | `AUTH-031` | Backlog |
| `AUTH-035` | Implement MFA enrolment and challenge | Feature | P1 | MEDIUM | `AUTH-031` | Backlog |
| `AUTH-036` | Implement AuthZ decision check | Feature | P0 | HIGH | `AUTH-013`, `AUTH-021` | Backlog |
| `AUTH-037` | Implement AuthZ role CRUD and assignment | Feature | P0 | HIGH | `AUTH-013`, `AUTH-021` | Backlog |
| `AUTH-038` | Implement AuthZ policy CRUD and evaluation | Feature | P1 | MEDIUM | `AUTH-036` | Backlog |
| `AUTH-039` | Implement decision audit logging with hash chain | Feature | P0 | HIGH | `AUTH-014`, `AUTH-036` | Backlog |
| `AUTH-040` | Implement User Info profile read/write | Feature | P0 | HIGH | `AUTH-016`, `AUTH-022` | Backlog |
| `AUTH-041` | Implement User Info preferences | Feature | P2 | LOW | `AUTH-040` | Backlog |
| `AUTH-042` | Implement User Info consent CRUD | Feature | P0 | HIGH | `AUTH-040` | Backlog |
| `AUTH-043` | Implement User Info data export | Compliance | P1 | MEDIUM | `AUTH-040` | Backlog |
| `AUTH-044` | Implement User Info account deletion | Compliance | P0 | HIGH | `AUTH-040`, `AUTH-023` | Backlog |

### Phase 4 — Cross-service integration (4 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-050` | Implement JWT verification middleware | Feature | P0 | HIGH | `AUTH-032` | Backlog |
| `AUTH-051` | Wire event bus publishing and consumption | Feature | P0 | HIGH | `AUTH-023`, `AUTH-031`, `AUTH-037`, `AUTH-040` | Backlog |
| `AUTH-052` | Implement AuthZ cache layer with event-driven invalidation | Feature | P1 | MEDIUM | `AUTH-036`, `AUTH-051` | Backlog |
| `AUTH-053` | Implement coordinated account deletion | Compliance | P0 | HIGH | `AUTH-044`, `AUTH-051` | Backlog |

### Phase 5 — Compliance hardening (4 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-060` | Implement key rotation procedures | Compliance | P1 | MEDIUM | `AUTH-002`, `AUTH-031`, `AUTH-040` | Backlog |
| `AUTH-061` | Author breach response runbooks | Compliance | P1 | MEDIUM | `AUTH-031`, `AUTH-040`, `AUTH-037` | Backlog |
| `AUTH-062` | Implement decision audit log immutable forwarding | Compliance | P0 | HIGH | `AUTH-039` | Backlog |
| `AUTH-063` | Implement quarterly access review automation | Compliance | P2 | LOW | `AUTH-005` | Backlog |

### Phase 6 — Testing & verification (6 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-070` | Implement Gherkin acceptance suite | Test | P0 | HIGH | `AUTH-053` | Backlog |
| `AUTH-071` | Wire Schemathesis contract verification | Test | P0 | HIGH | `AUTH-031`, `AUTH-036`, `AUTH-040` | Backlog |
| `AUTH-072` | Implement AsyncAPI event payload verification | Test | P1 | MEDIUM | `AUTH-051`, `AUTH-070` | Backlog |
| `AUTH-073` | Load test AuthN login storm | Test | P1 | MEDIUM | `AUTH-031` | Backlog |
| `AUTH-074` | Load test AuthZ decision endpoint | Test | P0 | HIGH | `AUTH-052` | Backlog |
| `AUTH-075` | External penetration test | Test | P1 | MEDIUM | `AUTH-070` | Backlog |

### Phase 7 — Production readiness (5 tickets)

| ID | Ticket | Type | Priority | Tier | Blocked by | Status |
|---|---|---|---|---|---|---|
| `AUTH-080` | Deploy chosen topology | Infrastructure | P0 | HIGH | `AUTH-070`, `AUTH-071` | Backlog |
| `AUTH-081` | Multi-zone redundancy | Infrastructure | P0 | HIGH | `AUTH-080` | Backlog |
| `AUTH-082` | DR runbook and drill | Compliance | P1 | MEDIUM | `AUTH-081` | Backlog |
| `AUTH-083` | Security review and sign-off | Compliance | P0 | HIGH | `AUTH-070`, `AUTH-071`, `AUTH-072`, `AUTH-075`, `AUTH-082` | Backlog |
| `AUTH-084` | Production launch and smoke tests | Feature | P0 | HIGH | `AUTH-083` | Backlog |

**Success Criteria:**
- [ ] All 51 tickets closed.
- [ ] Every item of the README production-readiness checklist demonstrably satisfied (`AUTH-084`).
- [ ] Contract verification runs in CI and a drift fails the build (`AUTH-071`).

---

## Potential Next Steps

### HIGH Priority

1. **`AUTH-001` monorepo and CI/CD scaffolding** — the only Ready ticket on the board and the root of the
   dependency graph. It shrank once `AS-01` and `AS-03` landed: the repository, licence, CI workflow and
   spec-lint step already exist, so what remains is the `services/` layout, branch protection and
   code-owners.

### LOW Priority

None. Portfolio integration is complete; everything remaining is the `AUTH-nnn` programme.

---

## Sprint Planning Summary

| Sprint | Priority | Items | Total Effort | Start | End |
|---|---|---|---|---|---|
| Sprint 1 — portfolio integration | MEDIUM/LOW | ~~`AS-01`~~..~~`AS-06`~~ (all closed) | 0 hrs remaining | 2026-08-14 | 2026-08-15 |
| Sprint 2 — Phase 0 foundations | HIGH | `AUTH-001`..`AUTH-006` | not estimated | TBD | TBD |
| Sprint 3+ — Phases 1–7 | HIGH | `AUTH-010`..`AUTH-084` | not estimated | TBD | TBD |

---

## Maintenance Notes

- Include links/paths to affected files when adding new items.
- Update the version number at the top when items change status.
- This file is the source of truth for **status**; the Kanban HTML is the source of truth for **ticket
  content** (description, acceptance criteria, spec rationale). Keep the split — do not fork the detail.
- Cross-reference code review findings in `.review/` once reviews begin.
- Mark completion dates when items move to ✅ Resolved, and record effort actuals against the
  `AUTH-nnn` tickets as they close, since the source carries no estimates.
