# infra/ — Deployment and infrastructure definitions

**Empty by design.** Nothing is provisioned here yet. The specification set deliberately names no
platform, runtime, or cloud, so there is nothing to commit until the implementation programme picks
one.

## Governing document

`auth-separation_deployment-topology_v1.md` — the design doc for running each service in its own
container instance per host, with the positives, negatives, and alternatives already weighed.
`auth-separation_database-spec_v1.md` states the per-service storage capabilities that any chosen
technology must satisfy.

## Constraints any implementation must hold

- **Each service is independently deployable, with no shared database.** This is production-
  readiness checklist item 10 and a spec constraint, not a preference.
- **The message bus is the only cross-service channel.** Provisioning it (`AUTH-004`) is what makes
  the AsyncAPI contract real; nothing may route around it.
- **Secrets come from a secrets store, never from committed config.** See the policy below.
- The decision audit store is **separate** from the AuthZ service database.

## Secrets policy (`AUTH-001`, criterion 5)

**CI consumes no secrets today.** The gate validates specifications; it needs no credential, and the
workflow runs with `permissions: contents: read` and no `secrets.*` reference at all. This policy
therefore states what happens the moment that stops being true, rather than describing a
configuration that already exists.

**The store is GitHub Actions encrypted secrets**, referenced as `${{ secrets.NAME }}` and injected
at step scope — never at workflow scope, so a secret is visible only to the step that needs it.
Once a cloud provider is chosen (`AUTH-002`, KMS and root signing keys), **OIDC federation is
preferred over any long-lived credential**: the workflow exchanges its identity token for a
short-lived cloud credential and no static key is stored at all. A long-lived key is a fallback
requiring a recorded reason.

**Rules, in force now:**

1. No credential, private key, token, or connection string is ever committed — in this directory or
   any other. The spec set names algorithms and contracts, never vendors, keys, or real hostnames.
2. No `.env` file is committed. `.env.example` documenting *names* with placeholder values is fine.
3. Secrets are never echoed, logged, or written into a build artefact.
4. A secret that has ever been committed is treated as compromised and rotated, not merely deleted —
   git history keeps it.

**Enforcement, in three layers:**

| Layer | Control | Catches |
|---|---|---|
| Push time | GitHub secret scanning + push protection (enabled on this repository) | Known provider token formats, before they reach the remote |
| Gate | `npm run lint:secrets` — see [`scripts/validate-secrets.mjs`](../scripts/validate-secrets.mjs) | Private key blocks, provider tokens, committed `.env` files, across every tracked file |
| Review | [`.github/CODEOWNERS`](../.github/CODEOWNERS) routes `/infra/` and `/.github/` changes to an owner | Design-level mistakes a pattern cannot see |

The scope and deliberate limits of the gate leg are recorded in
[`docs/adr/0002-committed-secret-guard.md`](../docs/adr/0002-committed-secret-guard.md).

## Tickets that land here

`AUTH-002` (KMS and root signing keys), `AUTH-003` (service mesh PKI for mTLS), `AUTH-004` (message
bus), `AUTH-005` (observability stack) — see `auth-separation_implementation-kanban_v1.html` for
content and `docs/backlog.md` for status.
