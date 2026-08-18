# Architecture Assessment

[<- Previous: Recommendations](05_RECOMMENDATIONS.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Migration Plans ->](07_MIGRATION_PLANS.md)

---

## Test Pyramid

**Assessment: deliberately inverted, and correctly so for what exists.**

```text
        E2E / acceptance     21 Gherkin scenarios - NOT EXECUTED (AUTH-070 parked)
     Integration (in-process) 36 route tests via WebApplicationFactory
   "Unit" (reflection)        47 DTO fidelity + 18 guard assertions
```

- The bulk of the suite sits at what is nominally the integration layer - real HTTP through a real ASP.NET
  Core pipeline via `WebApplicationFactory`. For a stub with no logic behind it, this is the *only* layer
  where a meaningful assertion can be made: there is nothing to unit-test, because there is no unit.
- The reflection assertions (`DtoFidelityTests`) are unit-shaped but test the **generator's output**, not
  behaviour. Classifying them as a unit layer would flatter the pyramid; they are better read as
  build-artefact verification.
- **The apex is empty.** The 21 scenarios that would form the acceptance layer are parsed but never run
  (R-03). The pyramid therefore has no top, and the repository is honest about that.
- **Judgement:** a conventional pyramid would be wrong here. Criticising the shape without noting that
  behaviour does not exist would be criticising the project for being what it says it is.

## SOLID Principles

- **SRP - strong.** Each of the six Node scripts does exactly one thing, and the separation was reasoned
  about rather than inherited: `validate-kanban-content.mjs` was kept out of `sync-kanban-status.mjs`
  specifically because "status" and "content" are different concerns, and mixing them would muddy both.
  `Program.cs` wires the host and nothing else.
- **OCP - good, by generation.** Adding an operation to a contract requires no code change: regeneration
  extends both the abstract base and the derived implementation. The extension point is the specification,
  which is the correct place for this architecture.
- **LSP - the one place it is load-bearing, and it holds.** `AuthNController` and its siblings substitute for
  their abstract bases with no strengthened preconditions or weakened postconditions - every override
  returns `501` and the base contract promises only an `ActionResult<T>` or `IActionResult`. The generator
  cannot produce a violating override because it derives the signature verbatim.
- **ISP - N/A in the conventional sense**; there are no consumer-facing interfaces. The nearest analogue is
  the contract set, which is well segregated: three services, three documents, no shared schema file, and no
  service obliged to know another's types.
- **DIP - partially applicable.** `Contract` is depended on by all three test suites as a concrete class
  rather than an abstraction. That is proportionate - it is a test helper with one implementation, and
  introducing an interface would be ceremony (see YAGNI below).

## KISS

- **Strong.** The whole implementation is three thin `Program.cs` files, three generator configurations and
  two generation scripts. There is no dependency-injection container beyond the framework default, no
  layering, no repository pattern, no abstraction over the generator.
- The gate scripts prefer the simplest mechanism that works and *say why* when the simplest was rejected:
  [check-generated.mjs](scripts/check-generated.mjs) filters paths in JavaScript rather than in a git
  pathspec, and records that the simpler pathspec silently matched nothing.
- **The one place complexity crept in is justified**: `sync-kanban-status.mjs` derives readiness from the
  whole dependency graph rather than reading a ticket's `blocks` list. The simpler approach was tried, was
  wrong twice, and the ADR records the counter-example (closing `AUTH-002` would promote five tickets across
  two phase tables).

## YAGNI

- **Exemplary, and the project's defining discipline.** `ADR-0005` is a 46-ticket YAGNI decision with a
  recorded trigger for revisiting it, and the tooling enforces it - a parked ticket never derives to
  `Ready`.
- No premature abstraction in the test helper: `Contract` takes a spec path and does the four things the
  suites need. No provider interface, no factory, no configuration object.
- The stubs implement nothing behind the 501s. A weaker project would have added an in-memory repository
  "for demonstration"; this one did not, and says so.
- **Minor counter-example:** `coverlet.collector` is referenced in all three test projects
  ([AuthSeparation.AuthN.Tests.csproj](services/authn/tests/AuthSeparation.AuthN.Tests/AuthSeparation.AuthN.Tests.csproj))
  but no coverage threshold is configured or collected. It arrived with the `dotnet new xunit` template and
  is unused - the one piece of scaffolding that could be removed on YAGNI grounds.

## REST + OpenAPI

- **Contracts are resource-oriented and consistent.** AuthN uses `/users` and `/sessions` rather than
  `/register` and `/login` - a distinction `ADR-0006` records as having cost real time to discover, because
  habit suggests the verbs.
- Correct method semantics throughout: `DELETE /sessions` for logout, `PUT /credentials/password` for
  replacement, `POST /sessions/refresh` for a state-changing exchange, `PATCH /users/me` for partial update.
- Pagination is consistent (`page_size`, `page_cursor` as shared `components/parameters`), and idempotency
  is modelled explicitly where it matters (`Idempotency-Key` on role assignment).
- Error shapes are shared and composed - `ValidationError` extends `Error` via `allOf`, and the generator
  correctly rendered that as inheritance rather than flattening it, which the tests assert.
- **One documented wrinkle:** the contracts declare `3.1.0` but use no 3.1-only constructs. `ADR-0006`
  measured this rather than assuming it, and it is what made the generator field wide.
- **Not verified:** runtime conformance under adversarial input (`AUTH-071`, parked).

## ISTQB Strategies

- **Specification-based (black-box) testing dominates, appropriately.** Every route case is derived from the
  contract - the specification is literally the test basis, which is the textbook definition.
- **Equivalence partitioning** appears in the DTO-fidelity suite's three-way split: schemas with properties
  must generate a matching type; scalar aliases must generate *no* type; composed schemas must inherit.
  Each partition has an assertion, including the negative one.
- **Boundary and negative testing** is present but thin: a path outside the contract must 404, an undefined
  method must 405, a wrong-typed path parameter still reaches the stub. There is no malformed-body,
  oversized-payload or wrong-content-type case - reasonable for a stub, and the first thing to add if
  behaviour ever lands.
- **Risk-based prioritisation** is visible in the ADRs rather than the tests: the DTO-fidelity spike was run
  *first*, before anything was built, because 47 schemas across 19 `allOf` compositions was identified as
  the highest-risk unknown.
- **Missing:** decision/condition coverage, state-transition testing (the token lifecycle in the
  specifications is a state machine and nothing exercises it), and any performance or security testing. All
  are parked programme work, not oversights.

## Pedagogical Comments

- **Among the best-commented code in the portfolio.** Comments explain *why*, name the alternative that was
  rejected, and state limits. Representative:
  [generate-stub-impl.mjs](scripts/generate-stub-impl.mjs) records that it deliberately does not reproduce
  routing attributes because they are inherited and copying them would create a second, divergable source
  for the route table.
- **Failure messages teach.** `check-generated.mjs` ends with the rule and the remedy: "Generated files are
  never hand-edited (AUTH-020 criterion 4). If the stub is wrong, the contract is wrong: fix the
  specification and run `npm run generate`."
- **Tests carry their reasoning.** The route-overlap and wrong-type tests each explain why the surprising
  behaviour is correct under `ADR-0005`, so a future reader does not "fix" the code to match an assumption.
- **The ADRs are the teaching layer.** `ADR-0004` (status is generated) and `ADR-0005` (bounded slice) are
  both readable as standalone essays on a decision most projects make implicitly.
- **Gap:** nothing in the code explains the *system* the specifications describe - a reader learns how the
  stubs were generated, not how AuthN, AuthZ and User Info interact. That is the architecture document's
  job, and the READMEs correctly redirect there, but the code layer is silent on it.

---

[<- Previous: Recommendations](05_RECOMMENDATIONS.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Migration Plans ->](07_MIGRATION_PLANS.md)
