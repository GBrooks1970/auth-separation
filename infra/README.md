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
- **Secrets come from a secrets store, never from committed config.** Nothing in this directory
  may contain a credential, key, or real hostname — the spec set names algorithms and contracts,
  never vendors or secrets.
- The decision audit store is **separate** from the AuthZ service database.

## Tickets that land here

`AUTH-002` (KMS and root signing keys), `AUTH-003` (service mesh PKI for mTLS), `AUTH-004` (message
bus), `AUTH-005` (observability stack) — see `auth-separation_implementation-kanban_v1.html` for
content and `docs/backlog.md` for status.
