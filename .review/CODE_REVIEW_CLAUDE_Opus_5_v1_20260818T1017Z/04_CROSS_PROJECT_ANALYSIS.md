# Cross-Cutting Analysis

[<- Previous: Project Reviews](03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Recommendations ->](05_RECOMMENDATIONS.md)

---

Single-repository review: the axes below are read *within* the repository - specifications versus generated
code versus tests versus CI versus documentation versus the published board.

## Tool-Agnostic Tests

- **Strong by construction at the specification layer.** The contracts name no language, framework or
  generator, and `ADR-0006` demonstrates this was load-bearing rather than aspirational: the stack was
  chosen months after the specifications were written, and the specifications did not change to accommodate
  it.
- **Weak at the test layer, by choice.** The 101 tests are xUnit and .NET-specific, and assert against
  reflection over generated C# types. Porting the suite to another stack would mean rewriting it, though
  the *approach* - derive cases from the contract, guard against vacuous passing - transfers directly.
- The 21 Gherkin scenarios are the genuinely tool-agnostic assertions in the repository, and they are the
  ones nothing runs (R-03).

## Code-Agnostic Tests

- `Contract.cs` reads YAML and asserts against the contract's own vocabulary - paths, methods, schema names,
  property names - not against C# identifiers. That half is code-agnostic.
- `DtoFidelityTests` is unavoidably code-aware: it reflects over generated types and reads
  `JsonPropertyAttribute` to recover contract property names. This is the correct trade - proving fidelity
  *requires* inspecting what the generator produced - but it means the fidelity suite is the least portable
  part of the repository.
- The scalar-alias assertion is a good example of code-awareness done well: it asserts that a type is
  **absent**, encoding the knowledge that C# inlines a `string`/`uuid` alias to a primitive rather than
  boxing it.

## Single Source of Truth

- **Explicitly modelled, and the best-executed idea here.** `docs/backlog.md` owns ticket *status*; the
  Kanban payload owns ticket *content*; Ready-versus-Backlog is owned by neither and is derived from the
  dependency graph (`ADR-0004`). Where two documents could disagree, one was made generated.
- Enforced by two gates: `npm run lint:kanban` compares derived status against the board, and
  `npm run lint:kanban-content` requires that any ADR amending a ticket declares `**Amends tickets:**` and
  that the named tickets cite it back.
- **The limits are documented rather than discovered later.** `lint:kanban-content` gates the *link*, not
  the semantics: citing an ADR satisfies it whether or not the criterion is correct.
- Residual gap: the three `nswag.json` files are a de facto shared configuration held as three copies
  (R-06). They are not derived from anything, so they can drift silently.

## API Contract Compliance

- Three OpenAPI 3.1 documents, validated by `@redocly/cli` under the `recommended` ruleset on every push and
  pull request. Style warnings are reported and tolerated - a decision recorded in `ADR-0003` on the grounds
  that specifications are the reviewed deliverable and are not reshaped to satisfy a linter.
- **The stubs are proven to match the contracts, not assumed to.** DTO fidelity is asserted for all 47
  schemas across the three services, and route coverage for all 36 operations.
- **What is not verified:** that a *running* service conforms to its contract under adversarial input.
  `AUTH-071` (Schemathesis contract verification) is parked. For a stub returning 501 this is moot; it
  becomes the first thing to add if the slice is ever extended.
- The contracts declare `openapi: 3.1.0` but use no 3.1-only constructs, which `ADR-0006` records as a
  measured finding rather than an assumption - it is what made the generator field wide.

## Screenplay Parity

**N/A** - this project implements no Screenplay pattern and makes no claim to. It is a specification and
code-generation exercise, not a UI or journey-automation suite.

## Batch File Design

**N/A** - the repository ships no batch or shell entry points. All task orchestration is npm scripts
([package.json](package.json), lines 12-24) and `dotnet` CLI invocations, both cross-platform. This is the
right choice for a repository that must run identically on a Windows workstation and a Linux runner, and it
is what made the CRLF/LF trap visible rather than mysterious.

## Documentation Alignment

Assessed against `docs/backlog.md` v18 as the source of truth.

- **Aligned:** the backlog, `docs/project-contract.md`, `auth-separation_README_v1.md` and the closure
  banner agree on programme state (5 Done / 0 Ready / 46 Parked), on the scope boundary, and on what the
  repository must not be described as. The `AS-09` reopening is recorded at the top of the backlog rather
  than performed quietly, which is the correct handling of post-closure work.
- **Misaligned (R-01):** three service READMEs claim the drift check blocks merges;
  `docs/project-contract.md` (line 26) and the main README (line 152) correctly say only
  `Validate specifications` gates merges. The repository contradicts itself, and the incorrect statement is
  in the file a reader opens first when looking at a service.
- **Overstated (R-03):** "the executable acceptance layer" describes 21 scenarios that nothing executes.
- **Well handled:** the backlog header carries a full version history (v18 back to v7), so a reader can
  reconstruct how the project's understanding changed. This is unusual and valuable.

## Logging Alignment

- **Application logging: N/A.** The stubs return 501 and log nothing beyond the ASP.NET Core defaults in
  `appsettings.json`. There is nothing to align.
- **Tooling output is consistently designed**, which matters more here because the tooling *is* the product.
  Every validator ends with a single summary line in the same shape - subject, verdict, counts - for example
  `features/: valid Gherkin - 21 scenario(s) across 7 file(s).` and
  `services/*/src/*/Generated/: in step with the specifications.` Failures print `[error]`/`[mismatch]`/
  `[drift]` prefixed lines before the verdict.
- Failure messages tell the reader what to *do*, not just what went wrong -
  [check-generated.mjs](scripts/check-generated.mjs) ends with "If the stub is wrong, the contract is wrong:
  fix the specification and run `npm run generate`."

## Test Coverage Metrics

| Layer | Count | Verified |
|---|---:|---|
| Contract-derived route tests (501 per operation) | 36 | Yes - one theory case per operation |
| DTO fidelity assertions (one theory case per schema) | 47 | Yes |
| Guard tests (counts, brace-free paths, route precedence, negative paths) | 18 | Yes |
| **Total xUnit cases** | **101** | **0 failures**, local and CI |
| Gherkin scenarios | 21 | Parsed and counted only - **not executed** |
| Specification documents machine-validated | 4 | 3 OpenAPI + 1 AsyncAPI, every push and PR |
| Tracked files scanned for secrets | 80 | 8 shape-based rules, clean |

- **Coverage of what exists is effectively total** - every operation and every schema in all three contracts
  is asserted.
- **Coverage of what is specified is near zero**, by decision. 21 scenarios describe behaviour that no code
  implements; the gap is the parked programme, not an omission.
- No coverage instrumentation (`coverlet.collector` is present in the test projects but no threshold is
  configured or enforced). For a stub suite this is reasonable - line coverage of generated 501 returns
  would measure nothing useful.

---

[<- Previous: Project Reviews](03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Recommendations ->](05_RECOMMENDATIONS.md)
