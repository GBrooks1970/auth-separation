# specs/ — The machine-readable contracts

These four files are the normative, reviewed deliverable of this project. Everything else in the
repository either explains them, validates them, or is generated from them.

| File | Format | Governs |
|---|---|---|
| `auth-separation_authn-api_v1.yaml` | OpenAPI 3.1 | `services/authn` — login, token refresh, MFA, password reset |
| `auth-separation_authz-api_v1.yaml` | OpenAPI 3.1 | `services/authz` — permission checks, role assignment, policy evaluation |
| `auth-separation_userinfo-api_v1.yaml` | OpenAPI 3.1 | `services/userinfo` — profile CRUD, preferences, consent |
| `auth-separation_events_v1.yaml` | AsyncAPI 3.0 | The cross-service event channel — the only sanctioned one |

## Rules

- **Read `auth-separation_architecture_v1.md` first.** Without it these read as three unrelated
  documents instead of one coherent system.
- **Generated stubs are never hand-edited.** If the generated output is wrong, the contract is
  wrong — fix it here and regenerate.
- **The version is encoded in the filename.** Backwards-compatible additions bump `info.version`
  in place; a breaking change needs a new versioned filename and a coordinated update to every
  dependent spec.
- Style warnings from the linters are tolerated deliberately (25 at the time of writing, all
  `operation-4xx-response` and description rules). These specifications are the reviewed
  deliverable and are not reshaped to satisfy a linter's house style. **Structural errors fail the
  gate.**

## Validation

`npm run verify` at the repository root checks all four files plus the Gherkin acceptance criteria,
and runs in CI on every push and pull request:

```bash
npm ci
npm run verify
```
