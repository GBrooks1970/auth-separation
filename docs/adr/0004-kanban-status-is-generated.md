# 0004. Kanban ticket status is generated, not authored

**Status:** Accepted
**Date:** 2026-08-15

## Context

The project has a standing sync rule: **`docs/backlog.md` owns ticket status; the Kanban owns ticket
content.** The Kanban nevertheless carries a `status` string on every one of its 51 tickets, plus a
hand-maintained `payload-stats` blob with per-status counts. Those fields *are* status, sitting in the
document the rule says does not own it — a duplicate authority the rule forbids in prose but nothing
prevented in practice.

The board compounds this by being **half-derived**, which is the worst of both options:

- **Column placement is literal** — `for (const t of filtered) out[t.status].push(t)`. A card moves
  only when its `status` string is edited.
- **Blocked-by badges are derived** — `depMet()` evaluates `dep.status === "Done"` at render time.

So closing a ticket without editing the payload produces cards whose badges show *unblocked* while
they sit in the *Backlog* column. A wholly stale board would at least be obviously stale.

Two closures were done by hand before this decision (`AUTH-001`, `AUTH-006`). Both hit the same trap:
**reading the closing ticket's `blocks` list promotes the wrong tickets.** `AUTH-001` lists four, and
only three became Ready — `AUTH-003` carries a second blocker. Readiness is a property of the whole
dependency graph, and a human reading one field cannot see it.

The decisive evidence came from testing the generator: closing `AUTH-002` unblocks **five** tickets —
`AUTH-003`, and `AUTH-010`, `AUTH-012`, `AUTH-014`, `AUTH-015`, which live in a different phase table.
A manual closure would have found the first and silently missed four.

## Decision

**Generate the Kanban's status fields.** `scripts/sync-kanban-status.mjs` rewrites both payloads, and
`npm run lint:kanban` (its `--check` mode) becomes the fifth leg of `npm run verify`, so the board
cannot fall behind the backlog again without failing CI.

Authority is split three ways, and no new source of truth is introduced:

| Fact | Owner | Why |
|---|---|---|
| Which tickets are **Done** | `docs/backlog.md` | A human decision. This is exactly what the sync rule already said. |
| Ticket **content**, including the dependency graph | the Kanban payload | Also unchanged — the Kanban owns content. |
| **Ready vs Backlog** | *neither* — computed | It is a function of the two above. Authoring it anywhere is how it goes wrong. |

A ticket is Ready when **every** entry in its `blockedBy` is Done. Nothing reads `blocks`.

## Consequences

- **The duplicate authority is gone.** The Kanban's `status` field still exists, because the board's
  render script needs it, but it is now output rather than input.
- **Drift is now a build failure**, not something discovered months later by a reader.
- **`In Progress` and `In Review` are preserved.** Those represent a human deliberately moving a card
  and cannot be derived from the graph, so the generator never overwrites them.
- **Backlog prose is reported, never rewritten.** The backlog's own Status cells carry dates, ADR
  references and explanatory clauses; a generator has no business authoring those. When they disagree
  with the graph, a sync run lists the rows needing a human edit. This is a *to-do list* in write mode
  and a *failure* in `--check` mode — because closing a ticket legitimately makes other rows stale in
  the same edit, and resolving that is what a sync run is for.
- **Structural mismatches stop everything.** If a ticket exists in one document and not the other, the
  two disagree about what the programme *is*, and nothing derived from them can be trusted — so the
  script fails in both modes rather than guessing.
- **One-time reformat.** The payload JSON is now serialised canonically by `JSON.stringify`, so the
  first run reformats those two lines. Every subsequent run is byte-stable.
- Trade-off: the generator parses the backlog's Markdown phase tables, which couples it to their
  seven-column shape. That is deliberate — the alternative was a third machine-readable status file,
  which would have recreated the very problem this decision removes. The parser requires the full
  seven columns and a backticked id in the first cell, so prose mentions, blocked-by references and
  the closure-record tables cannot be mistaken for ticket rows.

## Alternatives considered

- **Keep editing by hand, more carefully.** Rejected on evidence: two hand closures, two near-misses,
  and a five-ticket cascade that no reasonable person would catch by eye.
- **Make the Kanban the source of truth for status and generate the backlog instead.** Rejected: the
  backlog is where humans record decisions, with dates and reasoning attached. Generating prose from a
  status enum would lose all of it, and it inverts the existing sync rule rather than enforcing it.
- **A third machine-readable status file** feeding both documents. Rejected: strictly more drift
  surface, and it would make neither existing document authoritative.
