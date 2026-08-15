# Acceptance criteria

These files were `auth-separation_acceptance_v1.feature`, a single file containing seven `Feature:`
blocks. Gherkin permits exactly one Feature per file, so that file could not be executed by any
Cucumber-family runner — the parser stops at the second `Feature:` keyword (`AS-05`). Splitting it is a
pure restructuring: all 21 scenarios are preserved verbatim, and concatenating these seven files
reproduces the original body byte for byte.

The header comment from the original file, unchanged:

> Acceptance criteria for the Auth Separation example
>
> These scenarios are the executable definition of "production-ready" for a skeleton built from the specs
> in this folder. Every scenario must pass end-to-end against a running stack of the three services and
> the event bus before the skeleton can be considered complete.

## Index

| File | Feature | Scenarios |
|---|---|---:|
| `auth-separation_acceptance-registration-and-first-login_v1.feature` | Account registration and first login | 4 |
| `auth-separation_acceptance-authorisation-independence_v1.feature` | Authorisation decisions are independent of tokens | 3 |
| `auth-separation_acceptance-user-info-access-control_v1.feature` | User Info ownership and access control | 3 |
| `auth-separation_acceptance-multi-factor-authentication_v1.feature` | Multi-factor authentication | 3 |
| `auth-separation_acceptance-token-lifecycle_v1.feature` | Token lifecycle | 3 |
| `auth-separation_acceptance-consent-and-lifecycle_v1.feature` | Consent and lifecycle | 3 |
| `auth-separation_acceptance-contract-conformance_v1.feature` | Contract conformance | 2 |
| | **Total** | **21** |

Filenames keep the spec set's versioning convention: the version is encoded in the filename, so a
breaking change to a feature produces a new `_v2` file rather than an in-place rewrite.

## Open question for whoever implements AUTH-070

The `Background` — the four "…service is running" steps — was written under the first Feature only, and
the split preserves that exactly rather than inventing content. The other six features assume the same
running stack but do not state it. Deciding whether to repeat the `Background` in each file, or to handle
it once in the runner's hooks, is a spec-content decision left to the owner (`AS-06`), not something the
mechanical split should have taken.

## Validation

`npm run lint:gherkin` parses every file here with the real `@cucumber/gherkin` parser and fails if any
file declares more or fewer than one Feature.
