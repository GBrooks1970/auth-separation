# Project Review: auth-separation

[<- Previous: Risks and Issues](../02_RISKS_AND_ISSUES.md) | [Back to Index](../00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Cross-Project Analysis ->](../04_CROSS_PROJECT_ANALYSIS.md)

---

**Stack:** OpenAPI 3.1 / AsyncAPI 3.0 / Gherkin specifications; C# / .NET 9 + ASP.NET Core generated stubs;
Node 24 dev toolchain for validation; NSwag 14.7.1 as the generator; xUnit + `WebApplicationFactory` for
tests.

**Intent, from [auth-separation_README_v1.md](auth-separation_README_v1.md) (lines 3-5):** a
technology-agnostic specification set that "is the source of truth from which a working skeleton can be
generated", deliberately naming no platform, framework, language or vendor.

## 1. Architecture and design patterns

The architecture is the specification set, and the code exists to prove it. Three services - AuthN (PCI),
AuthZ (SOC 2), User Info (GDPR) - are separated by risk profile rather than by convenience, with the
AsyncAPI event contract as the only sanctioned cross-service channel and no shared database. Both
constraints are stated as spec constraints rather than preferences, and repeated in every service README.

The implementation follows the specification rather than the reverse. Each service is one ASP.NET Core
project whose entire API surface is generated: an abstract routed controller from NSwag, and a concrete
501-returning subclass derived from that base by
[generate-stub-impl.mjs](scripts/generate-stub-impl.mjs). The hand-written surface is a single thin
`Program.cs` per service (24 lines) that wires controllers and Newtonsoft JSON binding, plus a
`public partial class Program` declaration so `WebApplicationFactory` can host the real application rather
than a re-declared copy.

The most interesting design decision is the one that produced the second generator. NSwag's abstract
controller style stops at the base class, so the class ASP.NET Core actually discovers still has to exist.
The obvious move - hand-writing twelve overrides - would have placed hand-authored code inside a stub whose
own acceptance criteria forbid it. Deriving the subclass mechanically from the abstract signatures keeps
criterion 4 true of the whole artefact, and the same script served all three services without modification.

## 2. Code quality and maintainability

The C# is almost entirely generated, so code quality is really a question about the four Node scripts and
the tests, and both are strong. The scripts share a house style that is worth naming: each opens with a
comment explaining *why it exists and what it does not cover*, which is the half most projects omit.
[check-generated.mjs](scripts/check-generated.mjs) (lines 29-38) is the clearest example - it explains that
`git diff` must be used rather than `git status` because only diff applies the `.gitattributes` end-of-line
filter, and that the path filter is done in JavaScript because a glob pathspec matches the directory and
none of the files inside it.

Maintainability weakens across the three services. `Program.cs`, `nswag.json` and both test files are
near-clones (R-05, R-06). The shared `AuthSeparation.Testing` project extracted the contract reader, which
was the right first move, but stopped short of the assertions - so a change to the DTO-fidelity rules would
need three identical edits. At three services this is tolerable; it is the shape that rots at four.

One genuine subtlety is well handled: `Contract.PathParameters` reads path parameters from **both** the path
item and each operation ([Contract.cs](services/testing/AuthSeparation.Testing/Contract.cs), lines 130-165),
with a comment recording that reading only the operations misses them *silently*, because a URL containing a
literal `{roleKey}` still matches the route template `roles/{roleKey}`.

## 3. Test coverage and approach

**101 tests, 0 failures** (AuthN 37, AuthZ 38, User Info 26), verified locally and on the Linux runner.

The approach is the repository's strongest technical idea: tests read the contract at runtime and derive
their cases from it, rather than encoding a copied list. `Contract.Operations()` yields every method and
path from the YAML, so an operation added to a specification without regenerating the stub fails the suite
instead of passing unnoticed. `DtoFidelityTests` does the same for schemas, making the one-off spike
measurement permanent: 17 object schemas checked property-for-property for AuthN, plus assertions that
scalar aliases generate **no** type (correctly inlined to primitives) and that `allOf` compositions become
inheritance rather than being flattened.

Three guard tests deserve specific credit, because they defend against the failure mode that derived tests
introduce - a suite that passes while testing nothing:

- `The_contract_declares_the_*_operations_*` pins the expected count, so a contract that shrank cannot leave
  every derived case passing on a smaller set.
- `Every_templated_path_is_actually_exercised`
  ([StubServesContractTests.cs](services/authz/tests/AuthSeparation.AuthZ.Tests/StubServesContractTests.cs),
  lines 59-70) asserts no request path still contains a brace. This is the guard that caught a real reader
  bug during development: all fourteen AuthZ cases passed while six exercised nothing.
- `A_path_parameter_of_the_wrong_type_still_reaches_the_stub` (same file, lines 93-113) pins genuinely
  surprising behaviour - `System.Guid` in the signature does not constrain routing, because NSwag emits no
  `:guid` constraint and no `[ApiController]` is applied - and explains why 501 is the correct expectation
  under `ADR-0005` rather than "fixing" the code to match an assumption.

**Weaknesses.** The shape is narrow by necessity: every test is an in-process HTTP or reflection assertion
against a stub with no behaviour behind it. There is no unit layer (there is no logic to unit-test), no
contract-verification tooling such as Schemathesis (`AUTH-071`, parked), and no executed acceptance layer
(R-03). Coverage is therefore *complete for what exists* and says nothing about the system the
specifications describe - which the repository states plainly, and this review agrees is the right framing.

## 4. Documentation quality

Excellent in substance, with the one contradiction recorded as R-01.

- **Six ADRs** record decisions *and refusals*. `ADR-0003` declines a build-failing house-style ruleset and
  explains that it would contradict the standing decision that specifications are not reshaped to satisfy a
  linter. Recording a deliberate non-action is rarer and more useful than recording an action.
- **`docs/backlog.md`** is dense but authoritative, carries a version history in its header, and states the
  scope boundary in both directions. Its closure records for `AUTH-020`, `AUTH-021` and `AUTH-022` are
  criterion-by-criterion against evidence rather than tick-marks.
- **`docs/project-contract.md`** is the best single artefact for a successor: three gates, nine norms, the
  scope boundary, and four toolchain pins with the reason each exists. It exists precisely because handovers
  rotate and a closed project should not depend on someone finding the latest one.
- **Service READMEs** carry the governing contract, the compliance scope and the working rules - and the
  R-01 overclaim.

## 5. Strengths

- The scope decision (`ADR-0005`) and its enforcement in tooling: a `Parked` ticket never derives to `Ready`
  however its blockers resolve.
- Contract-derived tests with explicit guards against vacuous passing.
- A generated stub that is generated *all the way down*, including the concrete implementation.
- Documentation that states what it does not prove as prominently as what it does.
- Toolchain traps captured with their reasons, so the next engineer does not pay for them twice.

## 6. Weaknesses

- R-01: the drift gate and the test suite cannot block a merge, and three READMEs say the former can.
- R-05/R-06: triplication across services, with only the contract reader shared.
- R-03: 21 scenarios validated for grammar and count but never executed, described as "executable".
- R-04: three moderate advisories in the validation toolchain; the affected package is nine majors behind.

## 7. Verdict for a portfolio reviewer

This repository demonstrates judgement more than it demonstrates code, and it is honest about that. The
skill on display is knowing what not to build, recording the decision so it survives, and making tooling
enforce the parts that would otherwise rot. For a reviewer assessing senior automation judgement, the
`ADR-0005`/`ADR-0006` pair and the guard tests are the two things worth reading in full.

---

[<- Previous: Risks and Issues](../02_RISKS_AND_ISSUES.md) | [Back to Index](../00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Cross-Project Analysis ->](../04_CROSS_PROJECT_ANALYSIS.md)
