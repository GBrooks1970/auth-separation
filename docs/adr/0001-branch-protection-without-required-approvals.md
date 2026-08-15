# 0001. Branch protection without required approvals

**Status:** Accepted
**Date:** 2026-08-15

## Context

`AUTH-001` requires that "branch protection requires PR review and passing CI before merge". The CI
half is straightforward. The **review** half collides with a fact about this repository: it has a
single maintainer.

GitHub does not permit a user to approve their own pull request. On a single-maintainer repository,
setting "require N approving reviews" with N ≥ 1 makes every pull request permanently unmergeable
except by bypassing the rule. The requirement cannot be *satisfied*; it can only be *bypassed* or
*deferred*.

Two further facts shaped the decision:

- **No other repository in this portfolio has branch protection or a ruleset.** All twelve were
  checked; the one non-empty protection object (`hand-baked-screenplay-pattern`) has every control
  disabled bar force-push and deletion blocking. Whatever is chosen here is a first, not a
  convention being followed.
- **This repository's entire evidentiary claim rests on its git history.** The first commit
  (`7d4dbcd`) is the specification set alone — that is the proof that specification preceded
  implementation. A force-push or branch deletion would destroy the thing the project exists to
  demonstrate.

## Decision

Enable a ruleset on `main` that requires:

- **a pull request before merging, with zero required approvals**;
- **the `Validate specifications` status check passing**;
- **no force-pushes and no branch deletion**.

Do **not** require approving reviews, and do **not** configure a bypass list. The rules apply to the
owner exactly as they apply to anyone else.

The approving-review requirement is **deferred, with a recorded trigger: a second maintainer gaining
write access to this repository.** At that point `AUTH-001`'s review criterion becomes satisfiable
and the ruleset should be amended to require one approval and code-owner review.

## Consequences

- **Criterion 3 is met in substance for the half that is enforceable and honestly deferred for the
  half that is not.** `main` can no longer receive a direct push; every change is a pull request
  whose CI must be green. What is missing is a second pair of eyes, which no configuration can
  conjure.
- **Zero workflow friction.** Every change to this repository already went through a pull request
  (PRs #1–#6); the ruleset formalises existing practice rather than changing it.
- **It closes a trap this project has already fallen into twice.** Work was silently lost twice by
  pushing to a branch whose base had already merged. Mandatory pull requests plus a required status
  check make that failure visible instead of silent.
- **The first commit becomes tamper-evident.** Force-push and deletion blocking protect the
  specification-before-code evidence directly.
- **`.github/CODEOWNERS` requests review but does not gate it.** The file is deliberate documentation
  of ownership; enabling "require review from Code Owners" is part of the deferred change above, not
  of this decision. This is stated in the file itself so no reader mistakes intent for enforcement.
- Trade-off, stated plainly: a reviewer-free merge path means a mistake reaches `main` if CI does not
  catch it. The mitigation is that the gate is genuinely load-bearing — each leg is negative-tested
  against a deliberately broken copy before being trusted — not that review is happening.

## Alternatives considered

- **Require one approval plus code-owner review, with the owner on the ruleset bypass list.**
  Literal compliance with the ticket's wording. Rejected: the owner would bypass on every single
  merge, producing a rule that exists to be ignored. In a repository whose entire purpose is to
  demonstrate that documented intent matches reality, a decorative control is worse than an absent
  one, because it invites the reader to believe something untrue.
- **Enable nothing and record the whole criterion as not applicable.** Honest, but forfeits the CI
  gate and the force-push protection, both of which are fully enforceable and both of which have
  concrete value here. Strictly worse than what was chosen.
- **Adopt the same ruleset across all twelve portfolio repositories at once.** Rejected as scope: it
  is a portfolio-level decision with its own trade-offs per project, and folding it into `AUTH-001`
  would smuggle a cross-cutting change in under a single ticket. Recorded as a candidate for
  separate evaluation.
