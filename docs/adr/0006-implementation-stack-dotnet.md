# 0006. C# / .NET 9 and ASP.NET Core for the generated stubs

**Status:** Accepted
**Date:** 2026-08-15

## Context

`ADR-0005` committed the project to generating three server stubs. The specifications name no language,
framework or generator — "choice of database, runtime, or cloud" is listed under **Out of scope** in the
README, deliberately — so a stack had to be chosen before `AUTH-020` could start. This is an additive
decision about the *implementation*; it changes no contract.

Four things shaped it, and the first two were surprises.

**The specifications are smaller and plainer than the 51-ticket programme implies.**

| Service | Paths | Operations | Schemas |
|---|---|---:|---:|
| AuthN | 11 | 12 | 21 |
| AuthZ | 9 | 14 | 17 |
| User Info | 6 | 10 | 9 |
| **Total** | **26** | **36** | **47** |

Composition is mild: 19 `allOf`, one `oneOf`, **no** `discriminator` and so no polymorphic dispatch; three
security schemes, 16 enums, 31 `date-time` formats. No generator is being asked to do anything exotic.

**The OpenAPI 3.1 risk is largely illusory.** All three contracts declare `openapi: 3.1.0`, but they use
**no 3.1-only constructs** — no `webhooks`, `$schema`, `prefixItems`, `const`, `if`/`then`,
`unevaluatedProperties`, no type-array nullables, no plural `examples`. The content is 3.0-shaped. Generator
maturity on 3.1, normally the dominant concern, is therefore close to irrelevant here, which widens the
field considerably.

**What is actually installed** on the development machine: Node 24.18.0, npm 11.16.0, **.NET 9.0.317**,
Python 3.13.1, Docker 29.0.1. **No JDK and no Go.** That puts openapi-generator (Java) behind a Docker
image or an install, and rules out oapi-codegen without a Go toolchain.

**One option in the ticket does not apply.** `AUTH-020`'s spec note suggests `kiota`, but kiota generates
API *clients*, not server stubs; it cannot satisfy "stub compiles and serves all endpoints".

## Decision

**C# / .NET 9 with ASP.NET Core**, generating one project per contract inside the existing
`services/{authn,authz,userinfo}/` layout.

### Why

- **`AUTH-001`'s inherited criterion 2 falls out for free.** That criterion — CI must run a **test** step
  and produce a **build artefact** — was deferred to `AUTH-020` precisely because nothing existed to test
  or build. `dotnet build` and `dotnet test` answer both unambiguously. Under Node or Python, "build
  artefact" needs an argument; here it needs none. This was the deciding factor.
- **The generated shape matches the acceptance criteria literally.** ASP.NET Core generation produces
  controllers whose unimplemented actions return `501 Not Implemented` — which is exactly what `AUTH-020`
  asks for, rather than something to be hand-arranged.
- **It is installed natively.** No Docker dependency for a routine build, no JDK to provision.
- **Three services map cleanly to three projects in one solution**, mirroring the directory layout the
  monorepo already has.
- **Portfolio precedent exists** — `gb.automation.smoketests.sudoku.poc` already carries a C# stack
  (`demoapp003`), so this is not an isolated technology.

### What was rejected

- **TypeScript / Node** was the portfolio-consistent choice and the closest call. Rejected because
  server-stub generation is TypeScript's genuine weak spot: the mainstream approach generates *types* and
  hand-wires routes, which covers less of the surface than "regeneration is one command" implies, and
  openapi-generator's Node server generator has long been marked experimental. Choosing a weak generator
  to demonstrate that specifications generate code would undercut the demonstration.
- **Java / Spring Boot** has the most mature generator and the best 3.1 support, but no JDK is installed,
  it is the heaviest stack, the poorest portfolio fit, and its 3.1 advantage is moot for these specs.
- **Python / FastAPI** runs the spec relationship backwards — FastAPI produces OpenAPI rather than
  consuming it — and has no compile step, weakening both "stub compiles" and "build artefact".

## Consequences

- **CI gains a second toolchain.** The workflow currently runs `npm ci` + `npm run verify` on Node 24
  alone; `AUTH-020` must add a `setup-dotnet` step. The `main` ruleset pins the required check by name
  (`Validate specifications`), so **adding** a job will not break it — but promoting a build job to
  *required* would need the ruleset updated. Recorded here so it is not discovered at merge time.
- **The five-leg `npm run verify` gate stays exactly as it is.** It validates specifications, which remains
  a separate concern from building services. The .NET build is an additional lane, not a replacement.
- **This decision is provisional until proven by generation.** No generator has yet been run against
  these contracts. 47 schemas across 19 `allOf` compositions is precisely where generators produce
  plausible-looking but wrong DTOs, so **`AUTH-020`'s first step is a fidelity spike**: generate the AuthN
  stub, verify the DTOs match `components.schemas` exactly, and only then build on it. If the spike fails,
  the fallback is TypeScript with a thinner generation story, and this ADR is superseded rather than
  quietly worked around.
- **The standing rule applies from the first generated file:** generated stubs are never hand-edited. If
  the stub is wrong, the contract is wrong — fix the spec and regenerate.
- Trade-off: this is the largest technology divergence in a Node-centric portfolio, and it means a reader
  of this repository meets C# where the rest of the portfolio is TypeScript. Accepted because the
  criterion-2 argument is concrete and the alternative was a demonstrably weaker demonstration.
