# Code Review: auth-separation

**Reviewer:** AI assistant (CLAUDE Opus 5)
**Date (UTC):** 2026-08-18T10:17Z
**Version:** v1
**Repository:** `auth-separation` (https://github.com/GBrooks1970/auth-separation)
**Commit reviewed:** `652305e` (`main`, working tree clean)
**Backlog state:** v18 - 5 Done / 0 Ready / 46 Parked; `AS-01`..`AS-09` closed

---

## Table of Contents

1. [Executive Summary](01_EXECUTIVE_SUMMARY.md)
2. [Risks and Issues](02_RISKS_AND_ISSUES.md)
3. [Project Reviews](03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md)
4. [Cross-Project Analysis](04_CROSS_PROJECT_ANALYSIS.md)
5. [Recommendations](05_RECOMMENDATIONS.md)
6. [Architecture Assessment](06_ARCHITECTURE_ASSESSMENT.md)
7. [Migration Plans](07_MIGRATION_PLANS.md)

---

## Structure Summary

This is a **single-repository review**. Per the template's single-repository customisation,
`03_PROJECT_REVIEWS/` carries one file, and `04_CROSS_PROJECT_ANALYSIS.md` is read as cross-cutting
analysis *within* the repository - specifications versus generated code versus tests versus CI versus
documentation.

The repository is unusual for this portfolio and the review is calibrated to what it actually claims to
be. It is a **Specification Driven Development exemplar**: a complete, technology-agnostic specification
set whose first commit is the specifications alone, plus a deliberately bounded implementation slice of
three generated server stubs that return `501 Not Implemented` for all 36 operations. It is **not** a
working authentication system and does not present itself as one.

The project was **closed on 2026-08-17** and briefly reopened on 2026-08-18 for `AS-09` (publishing the
Kanban board). This review is therefore a post-closure assessment: findings are written for a reader
deciding whether to trust the repository, and for any future engineer who reopens it.

## Key Findings

| # | Severity | Finding |
|---|---|---|
| R-01 | **HIGH** | The central guarantee - "generated files are never hand-edited" - is **not enforced at the merge boundary**, and three READMEs claim it is |
| R-02 | **MEDIUM** | The `dotnet test` suite (101 tests) also cannot block a merge, for the same reason |
| R-03 | **MEDIUM** | 21 Gherkin scenarios are parsed but never executed; no runner or step definitions exist |
| R-04 | **MEDIUM** | Three moderate-severity advisories in the specification-validation toolchain; `@cucumber/gherkin` is nine majors behind |
| R-05 | **LOW** | Per-service test suites are near-duplicates; the shared helper extracted the contract reader but not the assertions |
| R-06 | **LOW** | `Program.cs` and `nswag.json` are triplicated with only identifiers differing |
| R-07 | **LOW** | `scripts/check-generated.mjs` silently repairs an uncommitted hand-edit rather than reporting it |
| R-08 | **INFO** | The published board renders four empty columns; correct, but unexplained to a first-time visitor |

Full evidence, impact and remediation for each: [02_RISKS_AND_ISSUES.md](02_RISKS_AND_ISSUES.md).

**The headline is R-01**, and it matters more here than the same finding would elsewhere. This repository's
distinguishing quality is that it gates its own claims - it added `lint:kanban` when the board could drift,
and `lint:kanban-content` when ticket prose could quietly falsify. R-01 is the same class of defect the
project built those gates to catch, sitting in the project's own enforcement story.

## Navigation Guide

- **Portfolio reviewer or hiring manager:** read [01_EXECUTIVE_SUMMARY.md](01_EXECUTIVE_SUMMARY.md), then
  the strengths section of [03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md](03_PROJECT_REVIEWS/PROJECT_001_auth-separation.md).
- **Engineer reopening the repository:** read [02_RISKS_AND_ISSUES.md](02_RISKS_AND_ISSUES.md) and
  [05_RECOMMENDATIONS.md](05_RECOMMENDATIONS.md), then `docs/project-contract.md` in the repository itself.
- **Architect assessing the approach:** read [06_ARCHITECTURE_ASSESSMENT.md](06_ARCHITECTURE_ASSESSMENT.md)
  and [04_CROSS_PROJECT_ANALYSIS.md](04_CROSS_PROJECT_ANALYSIS.md).

## Validation performed for this review

All commands were run against `652305e` on 2026-08-18. No implementation changes were made.

| Command | Result |
|---|---|
| `npm run verify` | PASS - six legs; 21 Gherkin scenarios across 7 files; 80 tracked files scanned for secrets; board in sync at 5 Done / 46 Parked |
| `dotnet test AuthSeparation.sln` | PASS - **101 passed, 0 failed** (AuthN 37, AuthZ 38, User Info 26) |
| `npm run lint:generated` | PASS - generated stubs in step with the specifications |
| `npm audit` | **3 moderate** (dev-only); `npm audit --omit=dev` reports 0 |
| `npm outdated` | `@cucumber/gherkin` 33.1.0 -> 42.0.1; `@cucumber/messages` 28.1.0 -> 34.2.1 |
| `gh api .../rulesets` | `main: PR + passing CI` requires exactly one check: `Validate specifications` |
| Live board fetch | HTTP 200, 51 cards, 0 console errors, only host contacted is `gbrooks1970.github.io` |

No heavyweight infrastructure was started; none is required by this repository.

---

[Next: Executive Summary ->](01_EXECUTIVE_SUMMARY.md)
