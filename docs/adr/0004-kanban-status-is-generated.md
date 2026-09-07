# 0004. Kanban ticket status is generated, not authored

**Status:** Accepted (amended 2026-09-07 — see Amendment below)
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

## Amendment (2026-09-07): the generator became a shared, published tool

The **decision above is unchanged** — status is generated, not authored; a ticket is Ready only when
every entry in its `blockedBy` is Done; nothing reads `blocks`; `In Progress`/`In Review` are preserved;
drift is a build failure. What changed is the *mechanism* and *where content lives*, as the generator was
generalised across the portfolio (decisions D5–D8) and this repository migrated onto it:

- **The bespoke `scripts/sync-kanban-status.mjs` is retired.** The board is now built by the shared,
  public npm package **`portfolio-kanban-generator`**, run via `npx` and pinned to `@1.0.0`. `npm run
  kanban:sync` and the `npm run lint:kanban` drift-gate (`--check`, still a `verify` leg) both invoke it.
  Nothing is vendored to run it.
- **Ticket content moved out of the board.** The Decision's authority table said "the Kanban owns
  content"; it no longer does. Ticket bodies (description, acceptance, spec, assignee) now live in
  **`docs/kanban-content.json`**, and ticket *headers* — including `phase`, now derived from the
  backlog's `Phase N` section headings, and `blockedBy` — are read from `docs/backlog.md` by the
  generator's `auth-table` adapter. The board is a pure output of those two files: its `status` field is
  still output-not-input, and now so is every other field.
- **The board is a single self-contained file (no vendored React/Babel).** The generator renders a
  framework-free board, so the `vendor/` directory and its ~2.9 MB of runtime were removed; `AS-02`'s
  "opens with no network" guarantee is now met by having no dependencies at all rather than by vendoring
  them.
- **`npm run lint:kanban-content` is unchanged** and still runs, guarding the declared-amendment
  convention the shared generator does not implement (an ADR's `**Amends tickets:**` line must be cited
  by every ticket it names). It reads the generated board, so a citation authored in
  `docs/kanban-content.json` reaches it after `npm run kanban:sync`.
