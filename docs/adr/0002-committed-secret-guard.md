# 0002. A committed-secret guard in the verify gate

**Status:** Accepted
**Date:** 2026-08-15

## Context

`AUTH-001` requires that "secrets management for CI is via the chosen secrets store, not committed
config". This repository consumes **no secrets at all**: the CI workflow validates specifications,
runs with `permissions: contents: read`, and contains no `secrets.*` reference.

The criterion is therefore satisfiable by doing nothing, which is precisely the problem. A control
that passes because there is nothing to control provides no signal, and gives way silently the first
time a service is wired up. This portfolio has been bitten by exactly that shape before — a
published report that was green and hollow for weeks because nothing asserted its contents.

## Decision

Convert the criterion from a claim into an assertion, in three parts:

1. **Name the store.** `infra/README.md` records GitHub Actions encrypted secrets as the CI secrets
   store, injected at step scope, with OIDC federation preferred over long-lived credentials once a
   cloud provider is chosen under `AUTH-002`.
2. **Add a gate leg.** `npm run lint:secrets` (`scripts/validate-secrets.mjs`) becomes the fourth leg
   of `npm run verify`, failing the build if committed content carries a secret.
3. **Scan what is committed, not what is present.** The guard enumerates files via `git ls-files`,
   so it asserts against the repository's actual contents and needs no ignore list for
   `node_modules`.

## Detection scope — deliberately narrow

The scanner matches only patterns that are secrets *by their shape*: PEM private key blocks, AWS
access key IDs, GitHub tokens, Stripe live keys, Slack tokens, Google service-account key blobs,
PuTTY key files, and committed `.env` files (`.env.example` and friends allowed).

**Keyword matching is deliberately absent.** These specifications describe an authentication system:
`password`, `token`, `credential`, and `secret` appear throughout the OpenAPI contracts as legitimate
schema fields and prose. A guard that flagged them would fire constantly, be muted within a week, and
protect nothing. A narrow guard that never cries wolf is worth more than a broad one that is ignored.

**Explicitly out of scope**, and not to be added later without a reason:

- **Entropy heuristics** on arbitrary string literals — the false-positive rate against example
  values, JWT samples, and hashes in the spec set is not worth the catch rate.
- **Real-hostname detection.** The spec set forbids real hostnames, but the documents legitimately
  cite `github.com`, `redocly.com`, and similar. Distinguishing a cited URL from a leaked internal
  endpoint is a judgement call, and it belongs to code-owner review, not a regex.
- **Git history scanning.** The guard asserts the current tree. A secret that reached history is a
  rotation event, not a lint failure — recorded as rule 4 of the policy.

## Consequences

- **Criterion 5 closes with evidence rather than by absence.** The claim is no longer "we have no
  secrets" but "no committed file *can* carry one of these classes, and the gate proves it on every
  push and pull request".
- **It is a second line, not the first.** GitHub secret scanning and push protection are enabled on
  this repository and catch known provider formats before they reach the remote. The gate leg adds
  coverage for the classes those miss (a stray `.env`, a private key pasted into a design document)
  and makes the control visible in the repository rather than only in its settings.
- **Negative-tested before being trusted**, as with the other three legs. Planted fixtures — a PEM
  private key block, an AWS key ID, a GitHub token, and a `.env` file — produced four findings with
  correct file and line references, `lint:secrets` exited 1, and the failure propagated through
  `npm run verify`. Removing the fixtures restored a clean scan of 35 tracked files.
- **The guard runs against the whole repository**, not just `infra/`, so a secret pasted into a
  compliance document or a feature file fails the same way.
- Trade-off: narrow detection means a bespoke credential format — a bare connection string, an
  internal API key with no distinctive prefix — passes. That gap is accepted knowingly and is what
  code-owner review on `/infra/` and `/.github/` is for.
