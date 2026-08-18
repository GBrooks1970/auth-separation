# Risks and Issues

[<- Previous: Executive Summary](01_EXECUTIVE_SUMMARY.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Project Reviews ->](03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md)

---

Ordered high to low. Every finding was verified against the working tree or the live GitHub API on
2026-08-18; inferences are labelled as such.

## R-01 (HIGH) - The "generated files cannot be hand-edited" guarantee is not enforced at the merge boundary

**Risk.** The repository's central engineering guarantee is that everything under
`services/*/src/*/Generated/` is output, never edited. It is enforced by `npm run lint:generated`, which
regenerates and fails on any difference. That command runs **only** in the `Build and test services` CI job,
and that job is **not a required status check**. A pull request whose drift check fails is therefore still
mergeable.

**Evidence.**

- The `main` ruleset requires exactly one check:

```text
$ gh api repos/GBrooks1970/auth-separation/rulesets/<id>
main: PR + passing CI
["Validate specifications"]
```

- The drift check lives in the other job:
  [ci.yml](.github/workflows/ci.yml) (lines 69-70) - `- name: Generated stubs match their specifications` /
  `run: npm run lint:generated`, inside the job named `Build and test services` ([ci.yml](.github/workflows/ci.yml), line 46).
- `npm run lint:generated` is deliberately **not** part of `npm run verify`:
  [package.json](package.json) (line 24) - `verify` chains the six `lint:*` legs and excludes it.
- Three files state the opposite:
  - [services/authn/README.md](services/authn/README.md) (line 26): "CI regenerates and fails on any
    difference, so a hand-edit to a generated file **cannot merge**."
  - [services/authz/README.md](services/authz/README.md) (line 27): same sentence.
  - [services/userinfo/README.md](services/userinfo/README.md) (line 27): same sentence.
- The repository contradicts itself: [docs/project-contract.md](docs/project-contract.md) (line 26) states
  correctly that "Only `Validate specifications` gates merges", and
  [auth-separation_README_v1.md](auth-separation_README_v1.md) (line 152) agrees.

**Impact.** Two distinct harms, and the second is worse than the first.

1. *Mechanical:* the rule that the whole slice depends on can be broken and merged. The blast radius today
   is small - the project is closed and single-maintainer - but it is exactly the scenario the gate exists
   for, and any reopening inherits it.
2. *Credibility:* this is a repository whose distinguishing quality is that it gates its own claims. It
   added `lint:kanban` when the board could drift, and `lint:kanban-content` when ticket prose could quietly
   falsify - the latter specifically because a claim had rotted unnoticed. A false enforcement claim in the
   three files a reader opens first is the same defect class, in the project's own voice.

**Remediation.** Two options; they are not equivalent.

- **(a) Make the claim true.** Add `Build and test services` to the ruleset's required checks:

```bash
gh api -X PUT repos/GBrooks1970/auth-separation/rulesets/<id> --input ruleset-with-both-checks.json
```

  This is repository administration, was explicitly identified as an owner decision in `ADR-0006` and
  `docs/project-contract.md`, and has not been taken. It is the stronger fix: it makes 101 tests and the
  drift check load-bearing.

- **(b) Make the documentation true.** Amend the three service READMEs to "CI regenerates and fails on any
  difference; that job is not a required check, so it reports drift rather than blocking it." Cheaper,
  immediately correct, and leaves the mechanical gap open.

**Recommended: (a), then (b) is unnecessary.** If the owner declines to promote the check, (b) becomes
mandatory rather than optional - the current state, where the repository asserts an enforcement it does not
have, is the one outcome that should not persist.

## R-02 (MEDIUM) - The 101-test suite cannot block a merge either

**Risk.** The same root cause as R-01, with a different consequence: `dotnet test AuthSeparation.sln` runs
only in `Build and test services`, so all 101 tests can fail and the pull request remains mergeable. The
`Validate specifications` job never compiles C# or runs a test.

**Evidence.** [ci.yml](.github/workflows/ci.yml) (lines 73, 75, 78) places `dotnet build`, `dotnet test` and
`dotnet publish` in the unrequired job. `AUTH-001`'s inherited criterion 2 - "CI runs a test step and
produces a build artefact" - is satisfied in the literal sense that the steps exist and run.

**Impact.** The criterion is met to the letter and weakened in spirit: a test step that cannot fail a merge
is a report, not a gate. The backlog's closure record for `AUTH-020`
([docs/backlog.md](docs/backlog.md), criterion 5) presents this as discharged without noting the
distinction, though `docs/project-contract.md` does note it.

**Remediation.** Resolved by R-01 option (a). If it is not taken, record the limitation in the `AUTH-020`
closure record so the criterion is not read as stronger than it is.

## R-03 (MEDIUM) - 21 Gherkin scenarios are validated but never executed

**Risk.** `features/` contains seven feature files and 21 scenarios, described in the README as "the
executable acceptance layer". Nothing executes them. There is no Cucumber-family runner, no step
definitions, and no test project that binds them.

**Evidence.**

- [validate-gherkin.mjs](scripts/validate-gherkin.mjs) (lines 21, 29, 74-78) parses the files with the real
  Gherkin parser and asserts `EXPECTED_SCENARIOS = 21`. It checks *grammar and count*, not behaviour.
- [package.json](package.json) (lines 29-30) carries `@cucumber/gherkin` and `@cucumber/messages` as the
  parser only; there is no `cucumber.js`, no `@cucumber/cucumber` runner dependency, and no step-definition
  directory anywhere in `git ls-files` (87 tracked files).
- The ticket that would stand them up, `AUTH-070` ("Implement Gherkin acceptance suite"), is
  **Parked** under `ADR-0005` - [docs/backlog.md](docs/backlog.md), Phase 6 table.

**Impact.** This is *correctly scoped*, not an oversight - the slice explicitly excludes it, and the
repository is consistent about that. The risk is one of reader expectation: `features/README.md` and the
main README describe an "executable acceptance layer", and a reviewer skimming for evidence of BDD practice
may reasonably infer a running suite. The 21 scenarios are also the largest body of unexercised assertion in
the repository, and their cost of decay is invisible - they can drift from the contracts without any gate
noticing, because nothing binds a scenario to an endpoint.

**Remediation.** No code change is warranted while `AUTH-070` is parked. Two cheap documentation fixes:

- In [features/README.md](features/README.md), state plainly that these are validated for grammar and count
  but not executed, and name `AUTH-070` as the ticket that would run them.
- In the main README, change "The Gherkin files are the executable acceptance layer" to "are the acceptance
  layer, to be executed by `AUTH-070`" - preserving the intent without implying current execution.

## R-04 (MEDIUM) - Three moderate advisories in the specification-validation toolchain

**Risk.** The dev toolchain that implements the project's primary gate carries known advisories, and the
affected package is nine major versions behind.

**Evidence.**

```text
$ npm audit
@cucumber/gherkin: moderate | via @cucumber/messages | range <=34.0.0 | fix: 42.0.1 (semver-major)
@cucumber/messages: moderate | via uuid            | range <=28.1.0 | fix: 34.2.1 (semver-major)
uuid:              moderate | via uuid            | range <11.1.1  | fix via @cucumber/messages 34.2.1

$ npm audit --omit=dev
found 0 vulnerabilities

$ npm outdated
@cucumber/gherkin   33.1.0 -> 42.0.1
@cucumber/messages  28.1.0 -> 34.2.1
```

**Impact.** Nothing ships from this repository - `npm audit --omit=dev` is clean, and the packages are used
only by `npm run lint:gherkin` in CI and locally. The practical risk is therefore low. The reputational risk
is not: a portfolio repository whose headline is machine-checked rigour reporting three moderate advisories
invites the obvious question, and the fix is a dev-only major bump with a single consumer.

**Remediation.** `npm install --save-dev @cucumber/gherkin@42 @cucumber/messages@34`, then re-run
`npm run lint:gherkin` and confirm it still reports 21 scenarios across 7 files. The consumer is one file
([validate-gherkin.mjs](scripts/validate-gherkin.mjs), lines 17-18, 29) using `AstBuilder`,
`GherkinClassicTokenMatcher`, `Parser` and `IdGenerator.uuid()` - a small API surface, so the major bump is
likely mechanical. **Inference:** I did not attempt the upgrade, so "likely mechanical" is a judgement from
the API surface, not a tested result.

## R-05 (LOW) - Per-service test suites are near-duplicates

**Risk.** The three services' test files differ only in counts, type names and a handful of service-specific
cases. `AuthSeparation.Testing` extracted the contract *reader* but not the *assertions*.

**Evidence.** 574 lines across six files:

```text
95 services/authn/tests/.../DtoFidelityTests.cs        73 .../StubServesContractTests.cs
90 services/authz/tests/.../DtoFidelityTests.cs       115 .../StubServesContractTests.cs
84 services/userinfo/tests/.../DtoFidelityTests.cs    117 .../StubServesContractTests.cs
```

A comment-stripped diff of the AuthN and AuthZ `DtoFidelityTests.cs` yields 30 differing lines out of ~90 -
the remainder is identical logic. The `Each_schema_generates_a_type_whose_properties_match_it` theory is
character-for-character the same in all three but for the namespace constant.

**Impact.** Low today - the duplication is bounded at three copies and the project is closed. It becomes a
maintenance cost only if a fourth contract appears or the fidelity rules change, at which point three
identical edits are needed and drift between them is silent.

**Remediation.** Promote the shared assertions into `AuthSeparation.Testing` as an abstract base class
parameterised by spec path, namespace and assembly - for example
`abstract class DtoFidelityContract { protected abstract Contract Spec { get; } ... }` - leaving each
service's file to declare its counts and its genuinely service-specific cases (AuthZ's templated-path guard,
User Info's route-overlap test). Expected reduction is roughly 574 lines to ~250.

## R-06 (LOW) - `Program.cs` and `nswag.json` are triplicated

**Risk.** Three host files and three generator configurations differ only in identifiers.

**Evidence.** `md5sum services/*/src/*/Program.cs` returns three distinct hashes, but the files differ only
in the service name in the comment and the `AUTH-0nn` reference; the executable body -
`AddControllers().AddNewtonsoftJson()`, `MapControllers()`, `public partial class Program` - is identical.
The three `nswag.json` files differ in `className`, `namespace`, input path and output path only.

**Impact.** Minimal. This is arguably correct for a project demonstrating three *independent* services -
sharing host wiring would couple deployables that the architecture insists are separate. The real risk is
that a future change (for example adding a health endpoint) must be made three times.

**Remediation.** Leave `Program.cs` alone; the independence is deliberate and defensible. For the
configurations, if a fourth service ever appears, consider generating `nswag.json` from a single template
keyed by service name rather than copying - the copy step is where the path-asymmetry trap documented in
`docs/project-contract.md` bites hardest.

## R-07 (LOW) - The drift gate repairs an uncommitted hand-edit instead of reporting it

**Risk.** `npm run lint:generated` chains `npm run generate` *before* the check, so a hand-edit that has not
been committed is silently overwritten and the gate reports success. Only a **committed** hand-edit fails.

**Evidence.** [package.json](package.json) (line 23) -
`"lint:generated": "npm run generate && node scripts/check-generated.mjs"`. The behaviour is documented in
[check-generated.mjs](scripts/check-generated.mjs) (lines 7-9): "Regenerating in place is safe: generation is
deterministic, and a developer who has hand-edited a generated file wants it reverted anyway."

**Impact.** Low and arguably correct - the developer's intent is served either way, and CI checks out
committed state so the merge-boundary case behaves as intended. The subtlety matters for anyone
*negative-testing* the gate: an uncommitted planted edit will not reproduce a failure, which is a genuine
trap for a future engineer verifying the gate still works.

**Remediation.** None required. The behaviour is documented at the point of definition. Optionally add one
line to `docs/project-contract.md` noting that the gate must be negative-tested with a *committed* edit.

## R-08 (INFO) - The published board shows four empty columns without explanation

**Risk.** The live board at https://gbrooks1970.github.io/auth-separation/ renders Backlog, Ready, In
Progress and In Review all at zero, then Done at 5 and Parked at 46.

**Evidence.** Verified live on 2026-08-18: stats bar reads
`51 TOTAL  0 BACKLOG  0 READY  0 IN PROGRESS  0 IN REVIEW  5 DONE  46 PARKED`.

**Impact.** The rendering is *correct and deliberate* - `AS-09` moved Parked out of Backlog precisely so a
visitor would not read 46 queued tickets as an unfinished project, and the empty flow columns are the honest
picture. But a first-time visitor arriving from the portfolio landing page has no context for why a board is
mostly empty, and the board itself carries no note explaining the bounded slice.

**Remediation.** Add a single line under the board's header - it is already React with a `<p>` beneath the
title at [auth-separation_implementation-kanban_v1.html](auth-separation_implementation-kanban_v1.html)
(line 253) - along the lines of: "Bounded to three generated server stubs by ADR-0005; the remaining 46
tickets are parked, not queued." One sentence closes the gap between the board and the decision it
represents.

## Non-findings, checked and cleared

Recorded so a future reviewer does not repeat the work.

- **Vendored library licensing.** `vendor/` redistributes React, ReactDOM and Babel, and is now published to
  GitHub Pages. All three minified bundles **retain their inline `@license` and `Copyright` banners**
  (verified by `grep` on each file), so the MIT attribution requirement is satisfied. `vendor/README.md`
  documents provenance but not licences; adding a line naming them would be tidy, not required.
- **Committed secrets.** `npm run lint:secrets` scans all 80 tracked files against 8 shape-based rules and
  reports clean. The rules are deliberately shape-based rather than keyword-based, which is correct for a
  repository whose specifications discuss passwords and tokens constantly (`ADR-0002`).
- **Runtime dependency risk.** `npm audit --omit=dev` reports 0 vulnerabilities; the repository ships no
  runtime JavaScript. The .NET side pins `Microsoft.AspNetCore.Mvc.NewtonsoftJson` to `9.0.*` and resolves
  9.0.19.
- **Licence declaration.** `LICENSE` is present and MIT; the registry row and README agree.

---

[<- Previous: Executive Summary](01_EXECUTIVE_SUMMARY.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Project Reviews ->](03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md)
