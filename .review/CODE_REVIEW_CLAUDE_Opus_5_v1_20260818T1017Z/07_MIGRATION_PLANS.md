# Migration Strategy and Plans

[<- Previous: Architecture Assessment](06_ARCHITECTURE_ASSESSMENT.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md)

---

The template's three standard plans are addressed below. Two are largely already done; one is genuinely not
applicable and is recorded as such rather than padded.

## 1. Single Source of Truth for Features

**Status: already implemented, with one residual gap.**

- **Current state.** Authority is split by design and enforced by tooling: `docs/backlog.md` owns ticket
  status, the Kanban payload owns ticket content, and Ready-versus-Backlog is derived from the dependency
  graph rather than authored anywhere (`ADR-0004`). Two `verify` legs keep the three in step.
- **What made it work** was identifying the field that *two* documents wanted to own and making it a
  computed value instead of picking a winner. That is the transferable lesson for any project facing the
  same problem.
- **Residual gap (R-06).** The three `nswag.json` files are a de facto shared configuration held as three
  copies, differing only in class name, namespace and paths. Nothing derives them and nothing compares them,
  so they can drift silently - the one place in the repository where the single-source-of-truth principle is
  asserted in prose but not enforced.
- **Consolidation strategy, if a fourth service ever appears:** generate each `nswag.json` from one template
  keyed by service name, as a step inside `npm run generate`, and let the drift gate cover the result. Below
  four services this is not worth the indirection.
- **Do not consolidate the Gherkin features into the contracts.** They deliberately describe cross-service
  journeys that no single OpenAPI document can express; that separation is correct.

## 2. Docker Compose for Local Development

**N/A - and the absence is a positive finding, not a gap.**

- The repository has no runtime dependencies: no database, no message bus, no KMS, no external service. Every
  gate runs from a clean checkout with Node 24 and the .NET 9 SDK, and `docs/project-contract.md` states this
  explicitly ("No JDK, no Go, no Docker, no services").
- All the infrastructure a Compose file would stand up - the KMS (`AUTH-002`), the message bus
  (`AUTH-004`), the three databases (`AUTH-010`/`012`/`015`), the observability stack (`AUTH-005`) - is
  **parked** under `ADR-0005`. Adding Compose now would be scaffolding for a system the project has decided
  not to build, and would contradict the YAGNI discipline that is one of its strengths.
- **If the programme is ever resumed**, Compose becomes relevant at `AUTH-010` (the first provisioned
  database), not before. The deployment topology it should implement is already specified in
  [auth-separation_deployment-topology_v1.md](auth-separation_deployment-topology_v1.md) - one service per
  container per host - so the design work is done and only the wiring would be new.

## 3. GitHub Actions / Workflow

**Status: implemented across two workflows; one structural change recommended.**

**Current state.**

| Workflow | Job | Trigger | Required? |
|---|---|---|---|
| `ci.yml` | `Validate specifications` | push to `main`, all PRs, dispatch | **Yes** - pinned by name in the `main` ruleset |
| `ci.yml` | `Build and test services` | same | **No** |
| `pages.yml` | `Publish the Kanban board` | push to `main` on board/vendor paths, dispatch | N/A - deployment |

**What is done well.**

- **Least privilege.** `ci.yml` declares `permissions: contents: read`; `pages.yml` narrows to exactly
  `contents: read`, `pages: write`, `id-token: write`.
- **Deterministic toolchain.** `global.json` pins the SDK to the 9.x line, which is what stopped the runner's
  preinstalled .NET 10 from resolving the NSwag tool's `net10.0` asset and refusing the config's `Net90`
  runtime. Node is pinned to 24, and `npm ci` is used rather than `npm install`.
- **A publication guard.** `pages.yml` asserts the ticket payload and all three vendored runtime files exist
  in the assembled `_site` before deploying, so an empty shell fails the build rather than going live - a
  failure mode this portfolio has hit elsewhere.
- **Concurrency is correct for deployment**: `group: pages`, `cancel-in-progress: false`, so a running
  deploy is never cancelled mid-flight.
- **Path filtering** means the board only redeploys when the board or its vendored runtime changes.
- **Local reproducibility is genuine.** Every CI step maps to a command a developer can run:
  `npm ci` / `npm run verify` / `npm run lint:generated` / `dotnet build` / `dotnet test`. The drift check in
  particular was deliberately moved out of inline YAML into `npm run lint:generated` so the two cannot
  diverge.
- **Secrets:** the repository consumes none (`ADR-0002`), and `npm run lint:secrets` gates against committed
  ones on every run. `infra/README.md` names GitHub Actions encrypted secrets as the store for when the
  programme needs them.

**The structural change (R-01/R-02).** `Build and test services` runs the drift gate, the 101 tests and the
artefact publication, and none of it can block a merge. Plan:

1. Confirm the job name is stable - the ruleset matches on the **check name**, `Build and test services`, not
   the job id.
2. Add it to `required_status_checks` on ruleset `main: PR + passing CI` alongside `Validate specifications`.
3. Negative-test: open a throwaway PR with a committed hand-edit to a generated file, confirm the merge
   button is blocked, then close it.
4. Update `docs/project-contract.md` (line 26) and the three service READMEs so the documentation and the
   ruleset agree - whichever way the decision goes.

**Caveat, stated because it is the reason the job was left unrequired.** Promoting a check makes the build
toolchain a merge dependency: a transient NuGet or `setup-dotnet` outage would block documentation-only pull
requests. For a single-maintainer, closed repository that is a real trade-off rather than a formality, and it
is legitimately the owner's call. What is not legitimate is the current gap between what the READMEs claim
and what the ruleset enforces.

---

[<- Previous: Architecture Assessment](06_ARCHITECTURE_ASSESSMENT.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md)
