<!--
  AUDIENCE: Engineers, AI agents, and project leads maintaining work-in-progress tracking.
  PURPOSE:  Single source of truth for outstanding work, risks, and delivery planning
            for the auth-separation project.
  LOCATION: docs/backlog.md
  TEMPLATE: ../../templates/backlog.template.md (portfolio root)
-->

# auth-separation — Backlog

**Version:** 10 — **Kanban status is now generated** (`ADR-0004`): `npm run kanban:sync` recomputes every card's column from this file plus the dependency graph, and `npm run lint:kanban` guards it as the fifth `verify` leg. No item changed status; the closure procedure in Maintenance Notes did. v9 — **`AUTH-006` is Done** (`ADR-0003`): closed against the existing `npm run verify` toolchain rather than the specific tools its April criteria named. Four of five criteria met — one, a build-failing house-style ruleset, **deliberately declined** because it contradicts the standing decision that specifications are not reshaped to satisfy a linter. Unblocks `AUTH-020`, `AUTH-021`, `AUTH-022`. v8 — **`AUTH-001` is Done** (PR [#6](https://github.com/GBrooks1970/auth-separation/pull/6), merged `8322ea9`): monorepo layout with the contracts moved into `specs/`, `.github/CODEOWNERS`, a live branch ruleset on `main`, and a CI secrets policy enforced by a fourth `npm run verify` leg. Two decisions recorded as `ADR-0001` and `ADR-0002`; its "test / build artefact" criterion is deferred to `AUTH-020` by owner decision. This unblocks `AUTH-002`, `AUTH-005` and `AUTH-006`. v7 added the landing presence to the AS-04 record. **Zero outstanding `AS-nn` items.**
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

🟡 **Specification complete and validated; scaffolding in place, nothing implemented.** The 13-artefact SDD
spec set is in place and diff-verified against its canonical source. No service code and no deployment — but
the specs are now machine-checked by `npm run verify` in CI, and `AUTH-001` has laid the monorepo skeleton
around them (`specs/`, `services/{authn,authz,userinfo}/`, `infra/`, `docs/`). **The service directories are
deliberately empty.** The repository's first commit (`7d4dbcd`) is the spec set alone — deliberate SDD
evidence that the specification preceded the code, and now protected against force-push and deletion by the
`main` ruleset.

Published at **https://github.com/GBrooks1970/auth-separation** (public, MIT), registered in the portfolio
(`presentation_role: methodology`, gate `npm run verify`), listed on the public landing page, and with
`WORKLIST_auth-separation.md` tracked at the portfolio root.

**Portfolio integration is complete.** `AS-01`..`AS-06` are all closed, so the only work left is the
implementation programme itself, whose first ticket `AUTH-001` is now Done.

**Governance (from `AUTH-001`).** `main` carries an active ruleset — pull request required, the
`Validate specifications` check must pass, force-push and deletion blocked, **zero required approvals and no
bypass list**. An approving-review requirement is not satisfiable on a single-maintainer repository (GitHub
forbids self-approval), so it is deferred against a recorded trigger: a second maintainer gaining write
access. See [`adr/0001-branch-protection-without-required-approvals.md`](adr/0001-branch-protection-without-required-approvals.md).
CI secrets policy and its enforcement live in `infra/README.md` and
[`adr/0002-committed-secret-guard.md`](adr/0002-committed-secret-guard.md).

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
**Update (2026-08-15):** landing presence completed after the item was first written up. Both `showcase`
and `methodology` require a `portfolio-landing/data/presentation.json` entry — only `hidden` is exempt
(ADR-001; `project-layout.md` makes landing presence part of onboarding, not a later step), and the first
pass here missed it. `data/registry-lock.json` was refreshed from the merged canonical commit `7feaefd`
and the methodology entry added; `check_registry_parity.py` reports 10 showcase and 2 methodology projects.
**See:** portfolio-prompts PR #62, test-automation-portfolio PR #76, portfolio (landing) PR #33.

#### Risk #AS-03: No gate command, so the project cannot be orchestrated (Score: 10) ✅ Resolved 2026-08-15

**Resolution:** Added `npm run verify`, validating all five machine-readable artefacts — the three
OpenAPI 3.1 contracts via `@redocly/cli` (`recommended` ruleset, `redocly.yaml`), the AsyncAPI 3.0 event
contract via `@asyncapi/parser`, and the Gherkin acceptance criteria via the real `@cucumber/gherkin`
parser. Wired as `.github/workflows/ci.yml` on push, PR and manual dispatch. All three legs were
negative-tested against deliberately broken copies to confirm each fails rather than passing vacuously.
Errors fail the gate; style warnings (25 across the OpenAPI files, all `operation-4xx-response` and
description rules) are reported and tolerated, so the linter does not reshape the deliverable.

Substantially discharges `AUTH-006` ("Wire spec linting and validation into CI"). The remaining `AS-03`
success criterion, naming the gate in the registry row, belongs to `AS-04`.
**See:** PR #1. A fourth leg (`lint:secrets`) was added under `AUTH-001`, and `AUTH-006` was closed on this
toolchain on 2026-08-15 — see its closure record and `ADR-0003`.

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

| Priority | Count | Done | Ready | Blocked |
|---|---|---:|---:|---:|
| P0 (HIGH) | 36 | 1 (`AUTH-001`) | 4 (`AUTH-002`, `AUTH-020`, `AUTH-021`, `AUTH-022`) | 31 |
| P1 (MEDIUM) | 13 | 1 (`AUTH-006`) | 1 (`AUTH-005`) | 11 |
| P2 (LOW) | 2 | 0 | 0 | 2 |
| **Total** | **51** | **2** | **5** | **44** |

Note that `AUTH-001` is recorded in the Kanban as blocking four tickets, but only **three** became Ready
when it closed: `AUTH-003` carries a second blocker (`AUTH-002`) and stays in Backlog. Readiness is a
property of the whole dependency graph, not of the ticket that just closed — derive it, do not read it off
the `blocks` list.

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
| `AUTH-001` | Set up monorepo and CI/CD scaffolding | Foundation | P0 | HIGH | — | ✅ **Done** 2026-08-15 |
| `AUTH-002` | Provision KMS and root signing keys | Infrastructure | P0 | HIGH | `AUTH-001` | **Ready** |
| `AUTH-003` | Establish service mesh PKI for mTLS | Infrastructure | P0 | HIGH | `AUTH-001`, `AUTH-002` | Backlog (still blocked by `AUTH-002`) |
| `AUTH-004` | Stand up message bus | Infrastructure | P0 | HIGH | `AUTH-003` | Backlog |
| `AUTH-005` | Set up observability stack | Infrastructure | P1 | MEDIUM | `AUTH-001` | **Ready** |
| `AUTH-006` | Wire spec linting and validation into CI | Foundation | P1 | MEDIUM | `AUTH-001` | ✅ **Done** 2026-08-15 (`ADR-0003`) |

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
| `AUTH-020` | Generate AuthN server stub from OpenAPI | Feature | P0 | HIGH | `AUTH-006` | **Ready** — **also carries `AUTH-001` criterion 2** (see below) |
| `AUTH-021` | Generate AuthZ server stub from OpenAPI | Feature | P0 | HIGH | `AUTH-006` | **Ready** |
| `AUTH-022` | Generate User Info server stub from OpenAPI | Feature | P0 | HIGH | `AUTH-006` | **Ready** |
| `AUTH-023` | Generate event publishers and consumers from AsyncAPI | Feature | P0 | HIGH | `AUTH-004`, `AUTH-006` | Backlog (still blocked by `AUTH-004`) |

> **Inherited from `AUTH-001` (owner decision, 2026-08-15):** `AUTH-020` also carries `AUTH-001`'s second
> acceptance criterion — *"CI runs on every PR: lint, **test**, build artefact"*. Only the lint third was
> satisfiable at `AUTH-001`, because nothing existed to test or build. `AUTH-020` produces the first
> generated stub, so it is the first point at which a test lane and a build artefact are real. **Do not
> close `AUTH-020` until CI runs a test step and produces a build artefact for the AuthN stub**, in
> addition to the ticket's own acceptance criteria in the Kanban.

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

## `AUTH-001` closure record — Done 2026-08-15

Delivered on PR [#6](https://github.com/GBrooks1970/auth-separation/pull/6), merged as `8322ea9`.

| # | Acceptance criterion | Outcome |
|---|---|---|
| 1 | Top-level `services/authn`, `services/authz`, `services/userinfo`, `specs/`, `infra/`, `docs/` | ✅ Met by layout. The four contracts genuinely moved out of the repository root into `specs/`; 25 references repointed, including 19 inside the Kanban's own JSON payload. |
| 2 | CI runs on every PR: lint, **test**, build artefact | 🟦 **Deferred to `AUTH-020` by owner decision, 2026-08-15.** Lint runs on every PR and now has four legs. There is no test suite and no build artefact because no service exists — `AUTH-020` (generate the AuthN server stub) is the first ticket that produces something testable and buildable, so the criterion attaches there rather than being met vacuously here. |
| 3 | Branch protection requires PR review and passing CI | ✅ Met in substance; the review half deferred against a recorded trigger. See `ADR-0001`. |
| 4 | Code-owners file maps each service directory to its named owner | ✅ `.github/CODEOWNERS`, covering service directories, specs, and the three compliance documents. |
| 5 | CI secrets via a secrets store, not committed config | ✅ Store named in `infra/README.md`; enforced by `npm run lint:secrets`, the fourth `verify` leg. See `ADR-0002`. |

**Decisions taken:** [`ADR-0001`](adr/0001-branch-protection-without-required-approvals.md) (branch protection
without required approvals) and [`ADR-0002`](adr/0002-committed-secret-guard.md) (a committed-secret guard in
the verify gate). Both exist because the honest answer differed from the obvious one.

**Every new control was negative-tested** before being trusted: removing a spec made `lint:asyncapi` exit 1;
four planted secret fixtures produced four correctly-located findings and failed `verify`; a direct push to
`main` was rejected with `GH013`.

**Unblocked:** `AUTH-002`, `AUTH-005`, `AUTH-006` became Ready. `AUTH-003` did not — see the note under the
risk summary.

---

## `AUTH-006` closure record — Done 2026-08-15

Closed on the toolchain that already existed rather than on the specific tools its April 2026 criteria
named. The reasoning, and one refusal, are in
[`ADR-0003`](adr/0003-spec-linting-toolchain-substitution.md).

| # | Acceptance criterion | Outcome |
|---|---|---|
| 1 | Spectral runs on every PR touching `specs/*.yaml` | ✅ **Met, partly literally.** `@asyncapi/parser@3.6.3` bundles `@stoplight/spectral-core`, so Spectral genuinely runs against the event contract; the OpenAPI files use Redocly's own engine. The gate runs on *every* PR, not only spec-touching ones — stronger than asked. |
| 2 | `openapi-spec-validator` confirms 3.1 conformance | ✅ **Met by substitution** — `redocly lint` performs the 3.1 structural validation. Same assertion, no second language runtime. |
| 3 | AsyncAPI CLI validates the events file | ✅ **Met by substitution** — `@asyncapi/parser` is the library the CLI wraps; calling it directly also lets the script assert channel and operation counts. |
| 4 | House style ruleset catches missing examples and vague descriptions | 🚫 **Declined, deliberately.** It contradicts the standing decision, in force since `AS-03`, that structural errors fail the gate while style warnings are reported and tolerated — specifications are the reviewed deliverable and are not reshaped to satisfy a linter. All 25 warnings are printed on every run, not suppressed. |
| 5 | Broken specs block merge | ✅ **Met — and only since `AUTH-001`.** Before the `main` ruleset, CI ran on pull requests but nothing prevented merging over a red check. The required status check from `ADR-0001` is what made this true. |

**Unblocked:** `AUTH-020`, `AUTH-021`, `AUTH-022` are now Ready — the server-stub generation phase.
`AUTH-023` is not: it also depends on `AUTH-004` (message bus). The same graph rule as before applies.

**Phase 0 is now half closed** — `AUTH-001` and `AUTH-006` done, `AUTH-002` and `AUTH-005` Ready,
`AUTH-003` and `AUTH-004` still chained behind `AUTH-002`.

---

## Potential Next Steps

### HIGH Priority

1. **`AUTH-002` provision KMS and root signing keys** — the only Ready `P0`, and the gate to `AUTH-003`
   (service mesh PKI) and the whole Phase 0 infrastructure chain. It is also the point at which the CI
   secrets policy recorded in `infra/README.md` stops being theoretical: this is the first ticket that
   introduces a real credential, and `ADR-0002` says OIDC federation is preferred over any long-lived key.

2. **`AUTH-020` generate the AuthN server stub from OpenAPI** — Ready now that `AUTH-006` is closed, and
   the first ticket that produces *code*. It also **inherits `AUTH-001`'s criterion 2**: CI must gain a
   test step and a build artefact before it can close. `AUTH-021` and `AUTH-022` are its siblings and
   equally Ready; `AUTH-023` is not, needing `AUTH-004` as well. Remember the standing rule that generated
   stubs are never hand-edited.

### MEDIUM Priority

3. **`AUTH-005` set up observability stack** — Ready, but with nothing running to observe it is naturally
   sequenced after the first service exists.

### LOW Priority

None. Portfolio integration is complete; everything remaining is the `AUTH-nnn` programme.

---

## Sprint Planning Summary

| Sprint | Priority | Items | Total Effort | Start | End |
|---|---|---|---|---|---|
| Sprint 1 — portfolio integration | MEDIUM/LOW | ~~`AS-01`~~..~~`AS-06`~~ (all closed) | 0 hrs remaining | 2026-08-14 | 2026-08-15 |
| Sprint 2 — Phase 0 foundations | HIGH | ~~`AUTH-001`~~, `AUTH-002`..`AUTH-005`, ~~`AUTH-006`~~ | not estimated | 2026-08-15 | TBD |
| Sprint 3+ — Phases 1–7 | HIGH | `AUTH-010`..`AUTH-084` | not estimated | TBD | TBD |

---

## Maintenance Notes

- Include links/paths to affected files when adding new items.
- Update the version number at the top when items change status.
- This file is the source of truth for **status**; the Kanban HTML is the source of truth for **ticket
  content** (description, acceptance criteria, spec rationale). Keep the split — do not fork the detail.
- **Closing an `AUTH-nnn` ticket is a two-step edit.** Mark it Done in the phase table here, then run
  `npm run kanban:sync` from the repository root. The board's card columns and summary counts are
  **generated** from this file plus the dependency graph (`ADR-0004`); they are not hand-edited. The sync
  run prints any rows here whose Status cell it cannot reconcile — update those by hand, since they carry
  dates and ADR references a generator must not author. `npm run verify` fails while the two disagree.
- **Never promote tickets by reading the closed ticket's `blocks` list.** A ticket becomes Ready only
  when *every* entry in its `blockedBy` is Done, and the two differ often: closing `AUTH-001` promoted
  three of the four it lists, and closing `AUTH-002` promotes five tickets spread across two phase
  tables. Let the sync script derive it.
- Cross-reference code review findings in `.review/` once reviews begin.
- Mark completion dates when items move to ✅ Resolved, and record effort actuals against the
  `AUTH-nnn` tickets as they close, since the source carries no estimates.
