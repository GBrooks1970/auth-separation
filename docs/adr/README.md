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

Both were written to close `AUTH-001`: the first records why the review half of its branch-protection
criterion is deferred rather than bypassed, the second why its secrets criterion is met with an
enforced gate rather than by having nothing to protect.
