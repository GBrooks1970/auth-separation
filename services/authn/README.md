# services/authn — Authentication service

**Empty by design.** Nothing is implemented here yet. This repository's value is that the
specification demonstrably preceded the code, so this directory stays empty until the
implementation programme reaches it.

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
