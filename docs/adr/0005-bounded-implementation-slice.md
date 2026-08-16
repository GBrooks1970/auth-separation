# 0005. A bounded implementation slice, not the full programme

**Status:** Accepted
**Date:** 2026-08-15

## Context

`AUTH-001` and `AUTH-006` closed, leaving 49 tickets and an open question the backlog could not answer:
**does this project implement the system its specifications describe?**

The Kanban reported five tickets as Ready, which made the answer look settled. It was not. "Ready" on that
board means *dependency-unblocked*; reading the acceptance criteria against the repository showed every one
of the five blocked on a decision the spec set **deliberately refused to make**:

- `AUTH-020`/`021`/`022` require "the target language" and "the chosen generator". Neither existed.
- `AUTH-002` requires "a KMS instance reachable from each service's deployment environment". There is no
  deployment environment, and the ticket involves real key material and a paid cloud account.
- `AUTH-005` requires a deployed log store, Prometheus-compatible metrics and an OpenTelemetry collector,
  with per-service dashboards. Nothing exists to observe.

The README lists "choice of database, runtime, or cloud" under **Out of scope**, by design. The programme
therefore could not start without first deciding what this repository is *for*.

Two facts framed the decision. The project is registered `presentation_role: methodology` — contracts only,
where the first commit being the spec set alone is the evidence that specification preceded code. And the
full programme is a production authentication system: KMS, mTLS service mesh, message bus, three databases,
observability, and PCI/GDPR/SOC 2 controls across 49 tickets.

## Decision

**Implement a bounded slice: the three generated server stubs (`AUTH-020`, `AUTH-021`, `AUTH-022`), and
nothing further.** Park the remaining 46 tickets.

### The slice boundary

**In scope** — generating a server stub per OpenAPI contract, such that each compiles, serves every
endpoint (501 at first), carries DTOs matching `components.schemas`, and regenerates with one command.

**Explicitly out of scope**, and not to be crept into ticket by ticket:

- persistence of any kind — no databases, no migrations, no schemas
- business logic behind the generated interfaces
- infrastructure — KMS, PKI, service mesh, message bus, observability
- the AsyncAPI event implementation (the contract stays validated, not realised)
- compliance controls as running code (the compliance documents remain the deliverable)
- deployment of anything, anywhere

### Why

- **(b) build the whole programme** was rejected as scope. It requires a paid cloud account, real key
  material and a running observability stack. More importantly, **a half-built authentication system is
  weaker portfolio evidence than a complete, validated specification set** — and half-built is the honest
  forecast for a 49-ticket production programme carried as a portfolio project.
- **(a) remain contracts-only** was rejected as leaving the central claim unproven. The repository asserts
  that these specifications are complete enough to generate a working skeleton. Until something is
  generated from them, that is an assertion.
- **(c) the slice** buys exactly the missing evidence at the smallest possible cost. Generating three stubs
  either demonstrates the contracts are implementable or exposes gaps in them — and both outcomes are
  worth having.

## Consequences

- **The 46 parked tickets are marked `Parked` in `docs/backlog.md`**, which the board's status generator
  now understands: a parked ticket never derives to `Ready`, however its dependencies resolve. Without
  this, `AUTH-002` and `AUTH-005` would have gone on advertising themselves as startable work that the
  project has decided not to do — precisely the misleading board that `ADR-0004` exists to prevent.
- **Trigger for resuming.** The parked tickets return to the graph only on an explicit, recorded decision
  to extend the slice or resume the full programme, superseding this ADR. Nothing about a parked ticket
  becoming dependency-unblocked constitutes such a trigger.
- **The specifications do not change.** This is a decision about what gets built from them, not about
  their content. All five `verify` legs continue to apply to the whole spec set.
- **`AUTH-084`** (assert the README's 10-item production-readiness checklist) is parked with the rest, so
  the project's stated definition of done is explicitly **not** being pursued. That is the honest reading
  of a bounded slice and is recorded here rather than left for a reader to infer.
- **The slice completes at `AUTH-022`.** At that point the project returns to a resting state with its
  central claim evidenced, and closing it becomes a reasonable next step.
- Trade-off, stated plainly: three stubs that return 501 are not a working system, and this ADR must not
  be read as a claim that they are. What they prove is narrower and worth exactly what it is — that the
  contracts are machine-consumable and internally coherent enough to produce a compiling service skeleton.
