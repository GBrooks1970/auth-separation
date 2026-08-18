# services/authz — Authorisation service

**A generated stub, and nothing more** (`AUTH-021`). All fourteen operations in the contract are
routed and return **501 Not Implemented**. That is the whole of the bounded slice for this
service: it proves the specification generates a compiling, serving skeleton. Nothing behind it —
role storage, policy evaluation, the decision audit log — is built or in scope
(`../../docs/adr/0005-bounded-implementation-slice.md`).

## Layout

| Path | What it is |
|---|---|
| `nswag.json` | The committed generator configuration. |
| `src/AuthSeparation.AuthZ/Generated/` | **Generated. Never hand-edited.** The routed abstract controller and its 501 implementation. |
| `src/AuthSeparation.AuthZ/Program.cs` | Hand-written host wiring, deliberately thin. |
| `tests/AuthSeparation.AuthZ.Tests/` | Reads the contract and asserts the stub serves it. |

## Regenerating

From the **repository root** — one command, and the only supported way to change anything under
`Generated/`:

```bash
npm run generate
```

CI regenerates and **reports** any difference. Note what that does and does not buy: the check runs
in the `Build and test services` job, which is **not a required status check**, so it surfaces a
hand-edit rather than blocking the merge. Treat a red drift check as a stop signal, not as a
guarantee that something else will stop you. The
same trap applies as for AuthN: NSwag resolves the **input** path against the working directory
but the **output** path against the configuration file's directory, so the two paths in
`nswag.json` look inconsistent and are not.

## Two things this contract has that AuthN's does not

- **Templated paths** — `/roles/{roleKey}`, `/users/{userId}/roles` and
  `/users/{userId}/roles/{roleKey}`, carrying six of the fourteen operations. Their parameters are
  declared at **path-item level**, a sibling of the verbs, rather than on each operation.
- **Optional query parameters.** `page_size` carries a schema `default` and `page_cursor` does not,
  so NSwag rendered one as a C# optional parameter and the other as required, in that order — which
  does not compile (`CS1737`). `generateOptionalParameters: true` makes it treat every non-required
  parameter as optional and order them last. The flag is set in both services' configurations so
  they stay copies of each other; it changes nothing for AuthN, which has no optional parameters.

Neither is a defect in the specification. Both were fixed in generator **configuration**, never in
generated output.

## Governing contract

`specs/auth-separation_authz-api_v1.yaml` (OpenAPI 3.1) — permission checks, role assignment,
policy evaluation. Read `auth-separation_architecture_v1.md` before that contract; without it the
three API specs read as unrelated documents rather than one system.

## Scope this service bears

Roles, permissions, and policy decisions. It decides what a proven identity may do, and it **never
sees credentials**. Its compliance scope is **SOC 2**
(`auth-separation_soc2-compliance_v1.md`), and its decisions are written to a **separated decision
audit store** — not to a service database shared with anything else.

## Working rules (from the spec set — not preferences)

- The server stub is **generated** from the OpenAPI contract and is **never hand-edited**. Business
  logic goes behind the generated interface, not inside it.
- The AsyncAPI events in `specs/auth-separation_events_v1.yaml` are the **only** sanctioned channel
  for cross-service state. No direct calls to AuthN or User Info beyond what the contracts permit.
- **No shared database.** This service owns its own authorisation store and the decision audit log
  is separated from it (`auth-separation_database-spec_v1.md`).
- Backwards-compatible additions bump `info.version` in place; breaking changes require a new
  versioned filename and a coordinated update to dependent specs.

## Tickets that land here

`AUTH-012`, `AUTH-013`, `AUTH-014`, `AUTH-021` onward — see
`auth-separation_implementation-kanban_v1.html` for content and `docs/backlog.md` for status.
