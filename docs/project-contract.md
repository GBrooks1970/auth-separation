<!--
  AUDIENCE: Any engineer or agent picking this repository up cold.
  PURPOSE:  The gates that must pass and the norms that must hold, pinned in the
            repository rather than in a handover that rotates each session.
  LOCATION: docs/project-contract.md
-->

# Project Contract — auth-separation

This project is **closed** (2026-08-17). Nothing here is a to-do list; it is what stays true, and what
anyone changing this repository must not break.

Read [`../auth-separation_architecture_v1.md`](../auth-separation_architecture_v1.md) **before** the three
API specifications. Without it they read as three unrelated documents rather than one system.

## Gates

All must pass. They are separate on purpose — see "Why the gates are separate" below.

```bash
npm run verify                    # six legs: the specifications and the board
dotnet test AuthSeparation.sln    # builds all three stubs and runs 101 tests
npm run lint:generated            # regenerates and fails if anything drifted
```

Only **`Validate specifications`** gates merges: the `main` ruleset pins that check by name. The
`Build and test services` lane runs `dotnet test` and `npm run lint:generated` and publishes a build
artefact, but is **not** a required check — promoting it is repository administration and an owner
decision that has not been taken.

### Why the gates are separate

`npm run verify` validates **specifications**. The .NET lane builds **code generated from them**. Folding
the second into the first would make spec validation require the .NET SDK and NSwag, and the two answer
different questions. This separation is recorded in
[`adr/0006-implementation-stack-dotnet.md`](adr/0006-implementation-stack-dotnet.md) and is not a
tidiness preference.

## Norms — from the spec set, not preferences

1. **The specifications are the source of truth.** If a stub is wrong, the contract is wrong: fix the
   specification and regenerate.
2. **Generated stubs are never hand-edited.** The easiest rule in this project to break, and the one
   `npm run lint:generated` exists to enforce — review cannot tell a regenerated file from an edited one.
   Everything under `services/*/src/*/Generated/` is output.
3. **The AsyncAPI events are the only sanctioned cross-service channel.** No direct calls between the
   three services beyond what the OpenAPI contracts permit.
4. **No shared database.** Each service owns its own store; the decision audit log is separated from
   AuthZ's.
5. **Versioning:** the version is encoded in the filename. Bump `info.version` in place for
   backwards-compatible additions; a breaking change needs a new versioned filename and a coordinated
   update to dependent specs.
6. **`docs/backlog.md` owns ticket *status* and headers; `docs/kanban-content.json` owns ticket *content*
   (description, acceptance, spec, assignee).** Ready-vs-Backlog is owned by neither — it is computed. The
   board is generated from both and never hand-edited; run `npm run kanban:sync`
   ([`adr/0004-kanban-status-is-generated.md`](adr/0004-kanban-status-is-generated.md)).
7. **A decision that changes a ticket's acceptance criteria must be an ADR** carrying an
   `**Amends tickets:**` line — only ADRs are gate-visible to `npm run lint:kanban-content`. This is
   exactly how `AUTH-020` came to sit with four criteria while the backlog said five.
8. **Never promote tickets by reading a closed ticket's `blocks` list.** A ticket is Ready only when
   *every* entry in its `blockedBy` is Done. Let the generator derive it.
9. **Scope beats dependency-readiness.** A Parked ticket never derives to Ready however its blockers
   resolve. Unparking requires an explicit decision superseding
   [`adr/0005-bounded-implementation-slice.md`](adr/0005-bounded-implementation-slice.md).

## Scope — what this repository is, and is not

Under `ADR-0005` this project implements **three generated 501 server stubs and nothing else**. All 46
remaining tickets are **Parked**: out of scope, not merely unstarted.

**It proves:** three specifications naming no language, framework or generator are complete and coherent
enough to generate compiling, serving service skeletons whose DTOs match their declared schemas exactly,
reproducibly from one command, with no hand-edits in the generated output.

**It is not** a working authentication system. Thirty-six endpoints return 501. There is no persistence,
token issuance, policy evaluation, consent handling, event implementation, infrastructure or deployment,
and the repository must not be described as though there were.

## Toolchain pins, and why each exists

Each of these cost real time to discover. None is optional.

| Pin | Reason |
|---|---|
| `global.json` pins the SDK to the 9.x line | The GitHub runner ships .NET 10 beside the 9.0.x `setup-dotnet` installs, so the NSwag tool resolved its `net10.0` asset and refused the config's `Net90` runtime |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` at `9.0.*` | A bare `dotnet add package` resolves 10.0.x, which is `net10.0`-only and fails `NU1202`. The package is required, not optional: generated DTOs carry Newtonsoft attributes |
| `generateOptionalParameters: true` in every `nswag.json` | Without it NSwag emits optional parameters before required ones in spec order, which does not compile (`CS1737`) |
| `*.cs text eol=lf` in `.gitattributes` | NSwag writes CRLF on Windows and LF on the runner; without normalisation the drift gate fails on platform rather than content |

**A trap in `nswag.json` itself:** NSwag resolves the `documentGenerator` input path against the **working
directory** but the `output` path against the **configuration file's directory**. The two paths therefore
look inconsistent and are not. Making them agree by inspection writes the generated file to a stray nested
path and still reports success.

## Where things live

| Artefact | Location |
|---|---|
| Decisions | [`adr/`](adr/) — six ADRs; `0005` and `0006` govern everything |
| Ticket status | [`backlog.md`](backlog.md) |
| Ticket content | `auth-separation_implementation-kanban_v1.html` (opens offline) |
| Handovers | `session-notes/` at the **portfolio root** — a separate repository |
| Worklist | `worklist-archive/` at the portfolio root, retired on closure |

The portfolio-root checkout is **shared with other sessions**. Use `git worktree add` for any edit there
and never switch its branch.
