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
| [0005](0005-bounded-implementation-slice.md) | A bounded implementation slice, not the full programme | Accepted | 2026-08-15 |
| [0006](0006-implementation-stack-dotnet.md) | C# / .NET 9 and ASP.NET Core for the generated stubs | Accepted | 2026-08-15 |

The first two were written to close `AUTH-001`: why the review half of its branch-protection criterion
is deferred rather than bypassed, and why its secrets criterion is met with an enforced gate rather
than by having nothing to protect. The third closes `AUTH-006`, recording which of its named tools
were substituted and which single criterion was **declined** — a house-style ruleset that would have
made a linter's opinion a merge blocker over hand-reviewed specification prose.

The fourth settles a question the other three kept raising: the Kanban carried a `status` field on every
ticket, duplicating the authority the sync rule gives to the backlog. Status is now generated and
guarded by the gate.

The fifth and sixth decide what this repository is *for*. `0005` commits to a bounded slice — three
generated server stubs to prove the specifications produce working code — and parks the other 46 tickets
rather than leaving them to advertise themselves as startable. `0006` chooses the stack that slice is
built on, and records that the choice is provisional until a generation spike proves DTO fidelity.

A theme runs through all six: each exists because the honest answer differed from the one that would
have let a box be ticked.
