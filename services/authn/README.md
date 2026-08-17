# services/authn — Authentication service

**A generated stub, and nothing more** (`AUTH-020`). Every operation in the contract is routed and
returns **501 Not Implemented**. That is the whole of the bounded slice for this service: it proves
the specification generates a compiling, serving skeleton. Nothing behind it — credential storage,
token issuance, MFA — is built or in scope (`../../docs/adr/0005-bounded-implementation-slice.md`).

## Layout

| Path | What it is |
|---|---|
| `nswag.json` | The committed generator configuration. |
| `src/AuthSeparation.AuthN/Generated/` | **Generated. Never hand-edited.** The routed abstract controller and its 501 implementation. |
| `src/AuthSeparation.AuthN/Program.cs` | Hand-written host wiring, deliberately thin. |
| `tests/AuthSeparation.AuthN.Tests/` | Reads the contract and asserts the stub serves it. |

## Regenerating

From the **repository root** — one command, and the only supported way to change anything under
`Generated/`:

```bash
npm run generate
```

CI regenerates and fails on any difference, so a hand-edit to a generated file cannot merge.

> **A trap worth knowing before you move these files.** NSwag resolves the `documentGenerator` input
> path against the **current working directory**, but the `output` path against the **configuration
> file's own directory**. The two paths in `nswag.json` therefore look inconsistent and are not:
> the input is repo-root-relative because `npm run` always executes there, and the output is
> relative to `services/authn/`. Making them agree by inspection writes the generated file to a
> stray nested path while reporting success.

## Governing contract

`specs/auth-separation_authn-api_v1.yaml` (OpenAPI 3.1) — login, token refresh, MFA, password
reset. Read `auth-separation_architecture_v1.md` before that contract; without it the three API
specs read as unrelated documents rather than one system.

## Scope this service bears

Credentials and token issuance. It proves identity and does not know what the tokens it issues are
allowed to do. Its compliance scope is **PCI** (`auth-separation_pci-compliance_v1.md`).

## Working rules (from the spec set — not preferences)

- The server stub is **generated** from the OpenAPI contract and is **never hand-edited**. Business
  logic goes behind the generated interface, not inside it.
- The AsyncAPI events in `specs/auth-separation_events_v1.yaml` are the **only** sanctioned channel
  for cross-service state. No direct calls to AuthZ or User Info beyond what the contracts permit.
- **No shared database.** This service owns its own credential store
  (`auth-separation_database-spec_v1.md`).
- Backwards-compatible additions bump `info.version` in place; breaking changes require a new
  versioned filename and a coordinated update to dependent specs.

## Tickets that land here

`AUTH-010`, `AUTH-011`, `AUTH-020`, `AUTH-030`, `AUTH-031` onward — see
`auth-separation_implementation-kanban_v1.html` for content and `docs/backlog.md` for status.
