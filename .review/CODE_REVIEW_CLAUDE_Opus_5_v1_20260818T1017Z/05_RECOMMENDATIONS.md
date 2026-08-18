# Recommendations

[<- Previous: Cross-Project Analysis](04_CROSS_PROJECT_ANALYSIS.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Architecture Assessment ->](06_ARCHITECTURE_ASSESSMENT.md)

---

**Context: the project is closed.** Every recommendation below is sized against that. Nothing here argues
for reopening the parked programme, and the first two items are chosen precisely because they are cheap
enough not to reopen anything meaningful.

## Recommended Refactors

Priority order. Effort is a rough estimate, stated as an inference rather than a measurement.

1. **Close R-01 - make the enforcement claim true, or make the claim match reality.** (~10 minutes either
   way.) Promote `Build and test services` to a required status check on the `main` ruleset, which makes
   both the drift gate and the 101 tests load-bearing; or amend the three service READMEs. **Do one of
   them.** The current state - a repository that asserts an enforcement it does not have - is the only
   outcome that should not persist, because self-gating is this repository's distinguishing quality.

2. **Bump `@cucumber/gherkin` to 42.x and `@cucumber/messages` to 34.x** (R-04). (~30 minutes.) Dev-only,
   one consumer ([validate-gherkin.mjs](scripts/validate-gherkin.mjs)), small API surface. Clears three
   moderate advisories and removes an obvious question from a reviewer's mind. Re-run `npm run lint:gherkin`
   and confirm it still reports 21 scenarios across 7 files.

3. **Correct the "executable acceptance layer" wording** (R-03). (~10 minutes.) Two sentences, in
   `features/README.md` and the main README, naming `AUTH-070` as the ticket that would execute them. This
   is the cheapest credibility fix in the list.

4. **Add a scope line to the published board** (R-08). (~10 minutes.) One `<p>` under the board's title at
   [auth-separation_implementation-kanban_v1.html](auth-separation_implementation-kanban_v1.html) (line 253)
   explaining that 46 tickets are parked by decision. The board is now the project's public face; it should
   carry the framing the backlog carries.

5. **Promote the shared test assertions into `AuthSeparation.Testing`** (R-05). (~2 hours.) An abstract
   base class parameterised by spec path, namespace and assembly, leaving each service to declare its counts
   and its genuinely service-specific cases. **Only worth doing if a fourth contract ever appears** - at
   three copies the duplication is visible and harmless; at four it becomes a maintenance tax.

## Next Steps

Immediate, in the order I would do them:

1. Decide R-01 (owner decision - it is repository administration either way).
2. Apply items 2-4 above as a single small pull request; all three are documentation or dependency changes
   and none touches a contract, a stub or a test assertion.
3. Re-run the three gates (`npm run verify`, `dotnet test AuthSeparation.sln`, `npm run lint:generated`) and
   confirm the board still renders 51 cards with zero external requests.
4. Record the result against a new `AS-10` in `docs/backlog.md`, following the `AS-09` precedent of noting
   the reopening explicitly rather than editing a closed project quietly.

## Future Project Ideas

Long-term, and deliberately framed as *separate projects* rather than as arguments to unpark this one.

- **A contract-verification lane against the stubs.** Point Schemathesis or an equivalent at the three
  running stubs and assert that every response - including the 501s - conforms to the declared contract.
  This is `AUTH-071` in miniature, would run without unparking any implementation ticket, and would close
  the "no adversarial contract testing" gap noted in the cross-cutting analysis.
- **Execute the 21 Gherkin scenarios against the stubs as pending/undefined steps.** A Cucumber-family
  runner that binds the scenarios and reports every step as undefined would make the acceptance layer
  genuinely executable while proving nothing false - and it would gate scenario drift, which nothing
  currently does.
- **Generalise `generate-stub-impl.mjs` into a small published utility.** The problem it solves - NSwag's
  abstract controller style leaves the discovered class unwritten - is not specific to this repository, and
  the script is 90 lines with a clear contract. It is the most reusable artefact the project produced.
- **A second implementation of the same contracts in a different stack.** The strongest possible evidence
  for the project's central claim would be generating a fourth stub in TypeScript or Java from the same
  unchanged specifications. `ADR-0006` already documents why TypeScript was the closest call; doing it would
  turn a well-argued decision into a demonstrated one.

---

[<- Previous: Cross-Project Analysis](04_CROSS_PROJECT_ANALYSIS.md) | [Back to Index](00_CODE_REVIEW_CLAUDE_Opus_5_v1_20260818T1017Z.md) | [Next: Architecture Assessment ->](06_ARCHITECTURE_ASSESSMENT.md)
