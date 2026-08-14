<!--
  AUDIENCE: Engineers, AI agents, and project leads maintaining work-in-progress tracking.
  PURPOSE:  Single source of truth for outstanding work, risks, and delivery planning
            for the auth-separation project.
  LOCATION: docs/backlog.md
  TEMPLATE: ../../templates/backlog.template.md (portfolio root)
-->

# auth-separation — Backlog

**Version:** 1 — first backlog; derived from the implementation Kanban at repository creation
**Last Updated:** 2026-08-14
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

🟡 **Specification complete, nothing implemented.** The 13-artefact SDD spec set is in place and
diff-verified against its canonical source. No service code, no CI, no deployment. The repository's first
commit (`7d4dbcd`) is the spec set alone — deliberate SDD evidence that the specification preceded the code.

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

#### Risk #AS-02: Kanban board depends on three CDN scripts, contradicting its "self-contained" claim — Score: 12

**Priority Score:** Security Impact (3) + Breakage Probability (7) + Maintenance Burden (2) = **12 points**
**Impact:** The implementation Kanban — the plan of record for all 51 tickets — renders a blank page with no
network, yet the README advertises it as self-contained.
**Effort:** 1–2 hours
**Status:** READY TO START
**Affected Stacks:** documentation / `auth-separation_implementation-kanban_v1.html`

**Problem:**
`auth-separation_implementation-kanban_v1.html` lines 7–9 load React 18.2.0, ReactDOM 18.2.0 and
babel-standalone 7.23.9 from `cdnjs.cloudflare.com`, with no `integrity` attribute. The README file index
(line 37) describes the file as "Self-contained: open in any browser." Both cannot be true. Offline, on a
restricted corporate network, or if cdnjs changes those paths, the board shows nothing — and the ticket
payload is only reachable by reading the raw HTML. The handover v1 claim of "zero outward dependencies"
was assessed on cross-*folder* file references and did not catch these.

**Impact Analysis:**
- **Security (3/10):** Three remote scripts execute with no Subresource Integrity pin. The content is a
  static local document with no secrets, which caps the blast radius, but an unpinned CDN script is still
  an uncontrolled execution path.
- **Breakage (7/10):** Deterministic total failure offline. The portfolio has already solved this exact
  problem once, by vendoring, in `markdown-renderer` (decision DR-MR-02).
- **Maintenance (2/10):** Low; vendored files are pinned and static.

**Refactor Strategy:**
1. Vendor the three libraries into `vendor/` beside the HTML, pinned at the versions already referenced.
2. Repoint the three `<script src>` tags at the local copies.
3. Alternatively, pre-compile the JSX and drop babel-standalone entirely — it is a ~1 MB dev-only
   transformer being shipped to render a static board.
4. Verify by loading the file with the network disabled.

**Success Criteria:**
- [ ] The board renders all 51 tickets with no network access.
- [ ] No `http(s)://` script or style source remains in the HTML.
- [ ] The README's "self-contained" claim in the file index is true as written, or amended.

---

#### Risk #AS-03: No gate command, so the project cannot be orchestrated — Score: 10

**Priority Score:** Security Impact (0) + Breakage Probability (4) + Maintenance Burden (6) = **10 points**
**Impact:** Every portfolio registry row names a gate command; without one, the orchestration prompts have
nothing to run and spec drift is caught by nobody.
**Effort:** 2–4 hours
**Status:** READY TO START
**Affected Stacks:** repository root / CI

**Problem:**
The registry contract requires a `Gates:` entry per project (for example `npm run verify`). This repository
has no runnable command at all. The three OpenAPI 3.1 files, the AsyncAPI 3.0 file and the Gherkin file are
all machine-checkable *today*, before any service exists — an invalid spec would otherwise be discovered
only when `AUTH-020` tries to generate a stub from it.

**Impact Analysis:**
- **Security (0/10):** No runtime surface.
- **Breakage (4/10):** Undetected spec drift propagates into generated stubs later in the programme.
- **Maintenance (6/10):** Without a gate the project is invisible to `portfolio-status`,
  `loop-worklist` and the fan-out orchestrators.

**Refactor Strategy:**
1. Add a minimal `package.json` with a `verify` script running a spec linter over the three OpenAPI files,
   the AsyncAPI file, and a Gherkin parse of the acceptance feature.
2. Wire the same command as a CI workflow on push and PR.
3. Record the gate in the registry row (see `AS-04`).

**Note on overlap:** this is the portfolio-gate slice of `AUTH-006` ("Wire spec linting and validation into
CI"), pulled forward because the registry needs it now. Doing `AS-03` substantially discharges `AUTH-006`;
close them together and say so.

**Success Criteria:**
- [ ] A single documented command validates all five machine-readable specs.
- [ ] The command runs in CI on every push and pull request.
- [ ] The registry row's `Gates:` field names it.

---

### LOW Priority (Score: 0–9)

#### Risk #AS-01: Repository is local-only and unlicensed — Score: 8

**Priority Score:** Security Impact (1) + Breakage Probability (2) + Maintenance Burden (5) = **8 points**
**Impact:** The spec set exists in exactly one place, on one machine, under no licence — so it cannot be
shared, referenced from the portfolio landing page, or recovered if the disk fails.
**Effort:** 1 hour
**Status:** READY TO START
**Affected Stacks:** repository root

**Problem:**
`git init` has been run and the specs are committed, but there is no GitHub remote and no `LICENSE` file.
Every sibling portfolio project is a public repository under MIT. Until this one is published it is
excluded from the portfolio's public evidence, and the "no backup" exposure is real.

**Impact Analysis:**
- **Security (1/10):** Publishing is a deliberate disclosure step; the spec set is a technology-agnostic
  worked example and contains no secrets, real hostnames, or keys (`*.example.internal` throughout).
- **Breakage (2/10):** Nothing breaks, but single-copy loss is unrecoverable.
- **Maintenance (5/10):** Blocks the registry row, the landing-page link, and CI.

**Refactor Strategy:**
1. Add `LICENSE` (MIT, matching the portfolio) and confirm the canonical source example permits
   redistribution before publishing.
2. Create the public GitHub repository and push `main`.
3. Add the repository to `.gitignore` at the portfolio root as a nested repo (already done).

**Success Criteria:**
- [ ] `LICENSE` present and referenced from the README.
- [ ] Public GitHub repository exists with `main` pushed and protected.

---

#### Risk #AS-04: Project is absent from the portfolio registry — Score: 7

**Priority Score:** Security Impact (0) + Breakage Probability (2) + Maintenance Burden (5) = **7 points**
**Impact:** No registry row means the project is invisible to every portfolio orchestration prompt.
**Effort:** 1 hour
**Status:** READY TO START
**Affected Stacks:** `portfolio-prompts/README.md`, `portfolio-prompts/registry.yml`, portfolio root

**Problem:**
`auth-separation` appears in neither the registry table in `portfolio-prompts/README.md` nor
`portfolio-prompts/registry.yml`, and there is no `WORKLIST_auth-separation.md` at the portfolio root —
the only project of twelve without one. `resume-session` reached Step 2 with no backlog path to resolve,
which is what produced this file.

**Impact Analysis:**
- **Security (0/10):** None.
- **Breakage (2/10):** Fan-outs silently skip the project rather than failing.
- **Maintenance (5/10):** Every future prompt invocation has to be told the project's conventions by hand.

**Refactor Strategy:**
1. Add the registry row (status, discipline, gates, and the deviations recorded above).
2. Add the `registry.yml` orchestration entry.
3. Derive `WORKLIST_auth-separation.md` from this backlog via `derive-worklist`.

**Success Criteria:**
- [ ] Registry row and `registry.yml` entry merged.
- [ ] `portfolio-status` reports the project without a drift warning.

---

### Resolved Risks

_None yet. Resolved items are kept, never deleted._

---

## Risk Summary

| Priority | Count | Total Effort | Status Distribution |
|---|---|---|---|
| HIGH (20–30) | 0 | — | — |
| MEDIUM (10–19) | 2 | 3–6 hrs | 2 READY TO START |
| LOW (0–9) | 2 | 2 hrs | 2 READY TO START |
| **Total Outstanding (`AS-nn`)** | **4** | **5–8 hrs** | 4 READY TO START |
| Resolved | 0 | — | |

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

1. **`AS-03` gate command** — 2–4 hrs, READY, unblocks orchestration and pre-validates the specs before any
   generator consumes them.
2. **`AUTH-001` monorepo and CI/CD scaffolding** — the only Ready ticket on the board and the root of the
   dependency graph. Note it partially overlaps `AS-01`/`AS-03`: doing those first shrinks it.

### MEDIUM Priority

1. **`AS-02` vendor the Kanban's CDN dependencies** — 1–2 hrs, restores the self-contained claim.

### LOW Priority

1. **`AS-01` publish and license** — 1 hr, gated on confirming redistribution of the canonical example.
2. **`AS-04` registry row and worklist** — 1 hr, follows `AS-01` (the row needs the GitHub URL).

---

## Sprint Planning Summary

| Sprint | Priority | Items | Total Effort | Start | End |
|---|---|---|---|---|---|
| Sprint 1 — portfolio integration | MEDIUM/LOW | `AS-01`, `AS-02`, `AS-03`, `AS-04` | 5–8 hrs | TBD | TBD |
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
