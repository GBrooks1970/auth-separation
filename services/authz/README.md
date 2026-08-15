# services/authz — Authorisation service

**Empty by design.** Nothing is implemented here yet. This repository's value is that the
specification demonstrably preceded the code, so this directory stays empty until the
implementation programme reaches it.

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
