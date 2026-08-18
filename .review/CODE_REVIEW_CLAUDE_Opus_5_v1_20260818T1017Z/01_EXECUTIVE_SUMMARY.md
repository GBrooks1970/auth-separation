# Executive Summary

[<- Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Risks and Issues ->](02_RISKS_AND_ISSUES.md)

---

## Verdict

**A strong, unusually honest repository with one real hole in its own enforcement story.**

`auth-separation` sets out to prove a single claim: that a technology-agnostic specification set can be
complete and coherent enough to build from, and that the specification demonstrably preceded the code. It
proves that claim, and it proves it in the one way a contracts-only repository cannot - by generating three
compiling, serving service skeletons from the contracts alone, reproducibly, with no hand-written code
inside the generated output.

What lifts it above a documentation exercise is that almost every claim it makes is machine-checked. Six
`npm run verify` legs validate the specifications, the acceptance criteria, committed secrets, and the
planning board's status *and* content. A seventh command regenerates the stubs and fails on drift. The
project repeatedly noticed that a claim could rot and built a gate for it.

The gap is that the most important of those gates cannot actually stop anything, and three files say it can.

## Design Quality

- **The scope decision is the best thing in the repository.** `ADR-0005` bounds the project to three
  generated stubs and parks 46 tickets, with an explicit reopening trigger. Deciding what *not* to build,
  recording why, and then enforcing that decision in tooling is senior judgement, and rare in a portfolio.
- **Authority is split deliberately and documented.** `docs/backlog.md` owns ticket status, the Kanban owns
  ticket content, and Ready-versus-Backlog is owned by neither - it is derived from the dependency graph
  (`ADR-0004`). Most projects would have let these drift; this one made the overlap a computed value.
- **The generated boundary is drawn correctly.** `Program.cs` is hand-written and thin; everything
  describing the API surface is generated. Where NSwag emitted only an abstract base,
  [generate-stub-impl.mjs](scripts/generate-stub-impl.mjs) derives the concrete subclass mechanically rather
  than letting twelve hand-written overrides sit inside a stub whose acceptance criteria forbid them.
- **Stack choice was argued from a criterion, not taste.** `ADR-0006` picks C# / .NET 9 because
  `dotnet build` and `dotnet test` discharge an inherited acceptance criterion unambiguously and ASP.NET
  Core returns 501 by default - and it was then proven by a spike before anything was built on it.
- **Weakness:** the three services are near-clones at every level - configuration, host wiring, and tests -
  and only the contract reader was extracted. See R-05 and R-06.

## Code Quality

- **Tests derive their expectations from the contracts**, not from copied lists. Adding an operation to a
  specification without regenerating fails the suite. That coupling is the point of the repository, and it
  is implemented rather than asserted.
- **Comments explain decisions, not mechanics.** The scripts are unusually well annotated: each says what
  it does *not* cover, which is the harder and more useful half.
  [check-generated.mjs](scripts/check-generated.mjs) (lines 29-38) explains why `git diff` is used instead
  of `git status`, and why the path filter lives in JavaScript rather than in a git pathspec.
- **Two tests pin surprising real behaviour instead of asserting an assumption** - notably
  [StubServesContractTests.cs](services/authz/tests/AuthSeparation.AuthZ.Tests/StubServesContractTests.cs)
  (lines 93-113), which records that a non-GUID path segment still reaches the action because NSwag emits no
  route constraint. That is more valuable than a test that quietly encodes a guess.
- **Gates were negative-tested before being trusted**, and the repository says so in its ADRs and commit
  messages. This is the difference between a green build and a meaningful one.
- **Weakness:** 574 lines of test code across six files are close to triplicated; the per-service
  assertions differ only in counts and type names.

## Main Highlights

1. **36 routed operations across three services, all returning 501, generated from three OpenAPI 3.1
   contracts** - 101 tests, 0 failures, verified on the Linux runner as well as locally.
2. **A drift gate that enforces "never hand-edit generated files"** rather than asking reviewers to spot it,
   with the honest note that review cannot distinguish a regenerated file from an edited one.
3. **A planning board that is published, interactive and offline-safe** - 51 tickets, vendored runtime, zero
   external requests even when served from GitHub Pages.
4. **Six ADRs that record refusals as well as decisions** - `ADR-0003` explicitly declines a build-failing
   house-style ruleset and says why, rather than quietly not doing it.
5. **A project contract** (`docs/project-contract.md`) that survives handover rotation and states the scope
   boundary in *both* directions, including what the repository must not be described as.

## Pedagogical Value

**High, and unusually so for a repository with no business logic.**

- It teaches something most portfolios cannot: how to bound scope deliberately, record the boundary, and
  make tooling enforce it. The `Parked` status - which never derives to `Ready` however its blockers
  resolve - is a small idea with a large lesson attached.
- It demonstrates the difference between validating a specification and *proving* it, and shows the reader
  the difference in evidence terms.
- The generator traps documented in `docs/project-contract.md` (NSwag's asymmetric path resolution, the
  runner's .NET 10 shadowing a pinned 9.x runtime, CRLF versus LF defeating a drift check) are exactly the
  sort of hard-won specifics that are usually lost when a project ends.
- **The main pedagogical risk is R-01**: a reader who trusts the service READMEs will learn that a
  non-required CI job blocks merges, which is untrue. A teaching repository that overstates its own
  enforcement teaches the wrong lesson twice.

## Portfolio Credibility

Credible, with one correction needed. The repository does not overclaim in the places that usually tempt
people - it says plainly and repeatedly that 36 endpoints returning 501 are not a working auth system, and
the backlog, README and closure record all state both halves. The single overclaim is narrow, technical, and
fixable in either of two ways described in R-01.

---

[<- Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Risks and Issues ->](02_RISKS_AND_ISSUES.md)
