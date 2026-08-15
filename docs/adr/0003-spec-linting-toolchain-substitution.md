# 0003. Closing AUTH-006 on a substituted linting toolchain

**Status:** Accepted
**Date:** 2026-08-15

## Context

`AUTH-006` ("Wire spec linting and validation into CI") was written in April 2026, before any tooling
existed, and its acceptance criteria name **specific tools**:

1. Spectral runs on every PR that touches `specs/*.yaml`
2. `openapi-spec-validator` confirms 3.1 conformance
3. AsyncAPI CLI validates the events file
4. House style ruleset catches missing examples and vague descriptions
5. Broken specs block merge

The gate that actually exists was built under `AS-03` (and extended under `AUTH-001`) with a different
toolchain: `@redocly/cli` for the three OpenAPI 3.1 contracts, `@asyncapi/parser` for the event
contract, `@cucumber/gherkin` for the acceptance criteria, and an in-repo scanner for committed
secrets.

Closing the ticket therefore requires stating honestly what was substituted, what is equivalent, and
what was **not** done — rather than ticking five boxes because a gate exists.

## Decision

**Close `AUTH-006` on the existing toolchain**, with the substitutions and one refusal recorded here.

### Criterion-by-criterion

| # | Criterion | Assessment |
|---|---|---|
| 1 | Spectral on every PR touching `specs/*.yaml` | **Met, partly literally.** `@asyncapi/parser@3.6.3` bundles `@stoplight/spectral-core`, so Spectral genuinely executes against the event contract. The OpenAPI files are linted by Redocly's own rules engine, which is not Spectral. The gate also runs on **every** PR, not only those touching `specs/*.yaml` — stronger than asked. |
| 2 | `openapi-spec-validator` confirms 3.1 conformance | **Met by substitution.** `redocly lint` performs OpenAPI 3.1 structural validation against the `recommended` ruleset. Different tool, same assertion. Adding a second Python-based validator to a Node toolchain for the sake of a tool name would add a dependency and a language runtime without adding a check. |
| 3 | AsyncAPI CLI validates the events file | **Met by substitution.** `@asyncapi/parser` is the library the AsyncAPI CLI wraps. Calling the library directly gives the same validation with one fewer layer, and lets the script assert channel and operation counts, which the CLI does not. |
| 4 | House style ruleset catches missing examples and vague descriptions | **Deliberately NOT adopted.** See below. |
| 5 | Broken specs block merge | **Met — and only since `AUTH-001`.** Before the `main` ruleset existed, CI ran on pull requests but nothing stopped a merge over a red check. The required status check introduced by `ADR-0001` is what made this criterion true. |

### Criterion 4 is declined, not overlooked

A house-style ruleset that fails the build on missing examples and vague descriptions **directly
contradicts a standing decision of this project**, recorded since `AS-03` in `redocly.yaml`, the
README, and the backlog: structural errors fail the gate; style warnings are reported and tolerated,
because *the specifications are the reviewed deliverable and are not reshaped to satisfy a linter's
house style*.

There are 25 such warnings today, all `operation-4xx-response` and description rules. Promoting them
to errors would mean editing hand-authored, human-reviewed specification prose to satisfy a generic
ruleset — inverting the relationship this whole repository exists to demonstrate, in which the
specification is authoritative and the tooling serves it.

The warnings are **not suppressed**: every run prints all 25, so the information the criterion wanted
is on screen at every build. What is refused is making a linter's opinion a merge blocker over a
document a human has reviewed.

## Consequences

- **`AUTH-006` closes with four of five criteria met and one explicitly declined**, rather than
  closing on a technicality. A reader comparing the Kanban card to the repository will find the
  difference explained here instead of discovering it themselves.
- **It unblocks `AUTH-020`, `AUTH-021` and `AUTH-022`** — the server-stub generation phase.
  `AUTH-023` stays blocked; it also depends on `AUTH-004` (message bus).
- **The substitution is now the project's baseline.** A future ticket proposing Spectral for the
  OpenAPI files, or `openapi-spec-validator`, should treat this as the decision it must overturn, with
  a reason better than matching a four-month-old ticket's wording.
- **If criterion 4 is ever wanted for real**, the honest route is a *separate* decision to adopt a
  house style for the specs, applied by editing the specs to comply — not by flipping the linter to
  error and letting the build dictate prose.
- Trade-off, stated plainly: closing a ticket against tools it did not name is a small precedent for
  interpreting acceptance criteria by intent rather than letter. That is defensible where the intent
  is unambiguous — "the specs are linted and a broken one blocks merge" — and it is why criterion 4,
  where intent and standing decision genuinely conflict, is called out rather than reinterpreted away.
