# services/userinfo — User Info service

**Empty by design.** Nothing is implemented here yet. This repository's value is that the
specification demonstrably preceded the code, so this directory stays empty until the
implementation programme reaches it.

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
