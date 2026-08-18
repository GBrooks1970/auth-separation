# services/userinfo — User Info service

**A generated stub, and nothing more** (`AUTH-022`). All ten operations in the contract are routed
and return **501 Not Implemented**. This is the stub that **closes the bounded slice**
(`../../docs/adr/0005-bounded-implementation-slice.md`): with it, all three specifications are
demonstrably able to generate a compiling, serving skeleton. Nothing behind it — profile storage,
consent records, data export, account deletion — is built or in scope.

## Layout

| Path | What it is |
|---|---|
| `nswag.json` | The committed generator configuration. |
| `src/AuthSeparation.UserInfo/Generated/` | **Generated. Never hand-edited.** The routed abstract controller and its 501 implementation. |
| `src/AuthSeparation.UserInfo/Program.cs` | Hand-written host wiring, deliberately thin. |
| `tests/AuthSeparation.UserInfo.Tests/` | Reads the contract and asserts the stub serves it. |

## Regenerating

From the **repository root** — one command, and the only supported way to change anything under
`Generated/`:

```bash
npm run generate
```

CI regenerates and **reports** any difference. Note what that does and does not buy: the check runs
in the `Build and test services` job, which is **not a required status check**, so it surfaces a
hand-edit rather than blocking the merge. Treat a red drift check as a stop signal, not as a
guarantee that something else will stop you.

## One thing this contract has that the other two do not

**Overlapping routes.** `users/me` and `users/{userId}` both match `GET /v1/users/me`, and NSwag
emits no route constraint that would separate them. ASP.NET Core prefers the literal segment, so
this resolves rather than raising `AmbiguousMatchException` — but nothing in the generated output
says so, and it is decided by routing precedence rather than by anything in the specification. A
test pins it, so a change in that behaviour surfaces as a failure rather than as a profile served
to the wrong caller once there is anything behind the stub.

## Governing contract

`specs/auth-separation_userinfo-api_v1.yaml` (OpenAPI 3.1) — profile CRUD, preferences, consent.
Read `auth-separation_architecture_v1.md` before that contract; without it the three API specs read
as unrelated documents rather than one system.

## Scope this service bears

Profile attributes, preferences, and consent state. It neither authenticates nor authorises — it
serves data once a caller has been authenticated and authorised by the other two services. Its
compliance scope is **GDPR** (`auth-separation_gdpr-compliance_v1.md`), which is why every
personally identifiable field is encrypted at rest and data-subject rights are first-class in the
contract.

## Working rules (from the spec set — not preferences)

- The server stub is **generated** from the OpenAPI contract and is **never hand-edited**. Business
  logic goes behind the generated interface, not inside it.
- The AsyncAPI events in `specs/auth-separation_events_v1.yaml` are the **only** sanctioned channel
  for cross-service state. No direct calls to AuthN or AuthZ beyond what the contracts permit.
- **No shared database.** This service owns its own profile store, with column encryption
  (`auth-separation_database-spec_v1.md`).
- Backwards-compatible additions bump `info.version` in place; breaking changes require a new
  versioned filename and a coordinated update to dependent specs.

## Tickets that land here

`AUTH-015`, `AUTH-016`, `AUTH-022` onward — see
`auth-separation_implementation-kanban_v1.html` for content and `docs/backlog.md` for status.
