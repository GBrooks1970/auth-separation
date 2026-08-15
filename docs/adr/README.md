# Architecture Decision Records

Decisions about **how this repository is built and governed**. They are not decisions about the
system the specifications describe — those live in the spec set itself, with
`auth-separation_architecture_v1.md` as the entry point, and are not restated here.

An ADR is written when a choice would otherwise be invisible to a successor, or when the honest
answer differs from the obvious one and the reasoning needs to survive.

| ADR | Title | Status | Date |
|---|---|---|---|
| [0001](0001-branch-protection-without-required-approvals.md) | Branch protection without required approvals | Accepted | 2026-08-15 |
| [0002](0002-committed-secret-guard.md) | A committed-secret guard in the verify gate | Accepted | 2026-08-15 |
| [0003](0003-spec-linting-toolchain-substitution.md) | Closing AUTH-006 on a substituted linting toolchain | Accepted | 2026-08-15 |
| [0004](0004-kanban-status-is-generated.md) | Kanban ticket status is generated, not authored | Accepted | 2026-08-15 |

The first two were written to close `AUTH-001`: why the review half of its branch-protection criterion
is deferred rather than bypassed, and why its secrets criterion is met with an enforced gate rather
than by having nothing to protect. The third closes `AUTH-006`, recording which of its named tools
were substituted and which single criterion was **declined** — a house-style ruleset that would have
made a linter's opinion a merge blocker over hand-reviewed specification prose.

The fourth settles a question the other three kept raising: the Kanban carried a `status` field on every
ticket, duplicating the authority the sync rule gives to the backlog. Status is now generated and
guarded by the gate.

A theme runs through all four: each exists because the honest answer differed from the one that would
have let a box be ticked.
