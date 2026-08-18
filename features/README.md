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

## Status: validated, not executed

**Nothing runs these scenarios today.** `npm run lint:gherkin` parses every file with the real Cucumber
Gherkin parser and asserts one `Feature:` per file and 21 scenarios in total - that is a check on grammar
and count, not on behaviour. There is no runner, no step definitions, and no service to run them against.

`AUTH-070` ("Implement Gherkin acceptance suite") is the ticket that would stand them up, and it is
**Parked** under [`../docs/adr/0005-bounded-implementation-slice.md`](../docs/adr/0005-bounded-implementation-slice.md)
along with the rest of the programme. The header comment quoted above describes the intended end state, not
the current one.

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

## Environment readiness is the runner's job, not a Background

**Every scenario in every file here assumes the full stack is up.** That precondition is stated once, here,
and must be asserted once by the runner — in a `BeforeAll`-style hook — rather than repeated as a
`Background` in seven files (`AS-06`).

The suite must not begin until all four are true:

- the AuthN service is running
- the AuthZ service is running
- the User Info service is running
- the event bus is running and consumers are connected

These four steps previously sat as a `Background` under the first Feature only — a leftover from the
pre-split bundled file, which meant one file declared the precondition and six relied on it silently.
The `Background` has been removed so all seven files are consistent and readiness lives in exactly one
place. The requirement itself is unchanged; only its home has moved.

`AUTH-070` is the ticket that implements the hook, and its acceptance criteria record this.

## Validation

`npm run lint:gherkin` parses every file here with the real `@cucumber/gherkin` parser and fails if any
file declares more or fewer than one Feature.
