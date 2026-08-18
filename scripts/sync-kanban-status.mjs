#!/usr/bin/env node
/**
 * Keeps the Kanban board's status in step with the backlog.
 *
 * WHY THIS EXISTS
 *
 * The board does not update itself, and it is stale in a particularly
 * misleading way. Column placement reads a literal `status` string per ticket
 * (`out[t.status].push(t)`), while the blocked-by badges are *derived* at render
 * time from `dep.status === "Done"`. Close a ticket without editing the payload
 * and you get cards whose badges show unblocked while they still sit in the
 * Backlog column — a half-updated board is worse than an obviously stale one.
 *
 * Two closures (`AUTH-001`, `AUTH-006`) were done by hand before this existed.
 * Both times the same trap appeared: reading the closing ticket's `blocks` list
 * promotes the wrong tickets. `AUTH-001` lists four, but only three became
 * Ready — `AUTH-003` carries a second blocker. Readiness is a property of the
 * whole graph and must be computed, never transcribed.
 *
 * WHERE AUTHORITY LIVES — this script does not invent a third source of truth.
 *
 *   docs/backlog.md   owns which tickets are DONE and which are PARKED. Both are
 *                     human decisions.
 *   the Kanban payload owns ticket CONTENT, including the dependency graph.
 *   Ready vs Backlog  is authored by NEITHER — it is derived from the two above.
 *
 * PARKED is scope, not progress (ADR-0005). A parked ticket never derives to
 * Ready however its dependencies resolve, because dependency-readiness and
 * being-in-scope are different questions and the board previously conflated
 * them: AUTH-002 and AUTH-005 advertised themselves as startable work the
 * project had decided not to do. The board carries a Parked column of its own,
 * placed after Done: these tickets are out of scope, not queued, and rendering
 * them in Backlog made a deliberately bounded project read as an unfinished one.
 *
 * That split is the existing sync rule ("the backlog owns status, the Kanban
 * owns content") made executable, and it removes the duplicate authority that
 * caused the drift: the Kanban's per-ticket `status` field is now generated.
 *
 * USAGE
 *   node scripts/sync-kanban-status.mjs            # rewrite the payloads
 *   node scripts/sync-kanban-status.mjs --check    # fail if they are stale
 *
 * `--check` is the fourth-wall guard: it runs in `npm run verify`, so the board
 * cannot silently fall behind the backlog again.
 */
import { readFileSync, writeFileSync } from 'node:fs';

const KANBAN = 'auth-separation_implementation-kanban_v1.html';
const BACKLOG = 'docs/backlog.md';
const check = process.argv.includes('--check');

/** Statuses a human sets deliberately on the board; never overwritten here. */
const IN_FLIGHT = new Set(['In Progress', 'In Review']);
/** Column order, mirroring COLUMNS in the board's own render script. */
const COLUMNS = ['Backlog', 'Ready', 'In Progress', 'In Review', 'Done', 'Parked'];

const payload = (html, id) => {
  const m = html.match(
    new RegExp(`(id="${id}" type="application/json">)([\\s\\S]*?)(</script>)`),
  );
  if (!m) throw new Error(`${KANBAN}: no <script id="${id}"> payload found`);
  return { json: JSON.parse(m[2]), start: m.index + m[1].length, end: m.index + m[1].length + m[2].length };
};

// --- Load both documents -----------------------------------------------------
const html = readFileSync(KANBAN, 'utf8');
const tickets = payload(html, 'payload-tickets').json;
const stats = payload(html, 'payload-stats').json;
const byId = new Map(tickets.map((t) => [t.id, t]));

/**
 * Parse the phase tables. A row is a ticket row only if its first cell is a
 * backticked AUTH id and it has the full seven columns — which keeps us clear of
 * the closure-record tables, the blocked-by column, and prose mentions.
 */
const backlogStatus = new Map();
for (const line of readFileSync(BACKLOG, 'utf8').split(/\r?\n/)) {
  const m = line.match(/^\|\s*`(AUTH-\d+)`\s*\|/);
  if (!m) continue;
  const cells = line.trim().replace(/^\||\|$/g, '').split('|').map((c) => c.trim());
  if (cells.length !== 7) continue;
  const cell = cells[6];
  // Keyword classification, not exact match: these cells carry dates, ADR
  // references and explanatory clauses alongside the status word. Parked is
  // tested before Done so "Parked (…, see ADR-0005)" cannot be misread.
  const state = /\bParked\b/.test(cell)
    ? 'Parked'
    : /\bDone\b/.test(cell)
      ? 'Done'
      : /\bReady\b/.test(cell)
        ? 'Ready'
        : 'Backlog';
  backlogStatus.set(m[1], { state, cell });
}

// --- Cross-check that the two documents describe the same programme ----------
const problems = [];
const onlyKanban = tickets.filter((t) => !backlogStatus.has(t.id)).map((t) => t.id);
const onlyBacklog = [...backlogStatus.keys()].filter((id) => !byId.has(id));
if (onlyKanban.length) problems.push(`in the Kanban but not the backlog phase tables: ${onlyKanban.join(', ')}`);
if (onlyBacklog.length) problems.push(`in the backlog but not the Kanban: ${onlyBacklog.join(', ')}`);

// --- Derive ------------------------------------------------------------------
const done = new Set([...backlogStatus].filter(([, v]) => v.state === 'Done').map(([id]) => id));
const parked = new Set([...backlogStatus].filter(([, v]) => v.state === 'Parked').map(([id]) => id));

const derived = new Map();
for (const t of tickets) {
  if (done.has(t.id)) derived.set(t.id, 'Done');
  // Scope beats dependency-readiness: an out-of-scope ticket is not startable
  // no matter what its blockers have done (ADR-0005).
  else if (parked.has(t.id)) derived.set(t.id, 'Parked');
  else if (IN_FLIGHT.has(t.status)) derived.set(t.id, t.status); // a human moved this card
  else derived.set(t.id, t.blockedBy.every((d) => done.has(d)) ? 'Ready' : 'Backlog');
}

/**
 * The backlog's own Ready/Backlog wording is prose this script cannot rewrite —
 * those cells carry explanatory clauses a generator has no business authoring.
 * A disagreement with the graph is therefore reported, never silently corrected.
 *
 * This is NOT the same class of problem as an id mismatch. Closing a ticket
 * legitimately makes the backlog's other rows stale in the same edit, and
 * resolving that is precisely what a sync run is for — so in write mode this is
 * a to-do list, and only in `--check` mode is it a failure.
 */
const staleProse = [];
for (const [id, { state, cell }] of backlogStatus) {
  const want = derived.get(id);
  if (state === 'Parked') continue; // scope is authored here, not derived
  if (state !== 'Done' && want !== undefined && !IN_FLIGHT.has(want) && state !== want) {
    staleProse.push(
      `${id}: backlog says "${cell}" but the graph makes it ${want} ` +
        `(blocked by ${byId.get(id)?.blockedBy.join(', ') || 'nothing'})`,
    );
  }
}

// --- Apply -------------------------------------------------------------------
const changes = tickets
  .filter((t) => t.status !== derived.get(t.id))
  .map((t) => `${t.id}: ${t.status} -> ${derived.get(t.id)}`);
for (const t of tickets) t.status = derived.get(t.id);

const tally = (key) =>
  tickets.reduce((acc, t) => ((acc[t[key]] = (acc[t[key]] ?? 0) + 1), acc), {});
const counts = tally('status');
const next = {
  ...stats,
  total: tickets.length,
  byStatus: Object.fromEntries(COLUMNS.filter((c) => counts[c]).map((c) => [c, counts[c]])),
  byPhase: tally('phase'),
  byPriority: tally('priority'),
  byType: tally('type'),
};
const statsStale = JSON.stringify({ ...next, generatedAt: null }) !== JSON.stringify({ ...stats, generatedAt: null });

// --- Report and write --------------------------------------------------------
for (const p of problems) console.log(`  [mismatch] ${p}`);
for (const s of staleProse) console.log(`  [backlog] ${s}`);
for (const c of changes) console.log(`  [status] ${c}`);

// A structural mismatch means the two documents disagree about which tickets
// exist. Nothing derived from them can be trusted, so stop in either mode.
if (problems.length) {
  console.error(
    `\n${KANBAN}: FAILED — ${problems.length} structural mismatch(es); ` +
      `the backlog and the board disagree about which tickets exist.`,
  );
  process.exit(1);
}

if (check) {
  if (changes.length || statsStale || staleProse.length) {
    console.error(
      `\n${KANBAN}: FAILED — board and backlog are out of step. ` +
        `Run \`npm run kanban:sync\`, update any [backlog] rows listed above, and commit.`,
    );
    process.exit(1);
  }
  console.log(
    `${KANBAN}: in sync — ${tickets.length} ticket(s), ` +
      `${Object.entries(next.byStatus).map(([k, v]) => `${v} ${k}`).join(' / ')}` +
      `${parked.size ? ' — Parked is out of scope under ADR-0005, not queued work' : ''}.`,
  );
  process.exit(0);
}

if (!changes.length && !statsStale) {
  console.log(`${KANBAN}: already in sync — nothing to write.`);
  process.exit(0);
}

next.generatedAt = new Date().toISOString().replace('T', ' ').replace(/\.\d+Z$/, 'Z');

// Offsets are recomputed against the partially-rewritten string on each pass,
// so an edit to one payload cannot shift the other out from under us.
let out = html;
for (const [id, value] of [['payload-stats', next], ['payload-tickets', tickets]]) {
  const at = payload(out, id);
  out = out.slice(0, at.start) + JSON.stringify(value) + out.slice(at.end);
}
writeFileSync(KANBAN, out);

console.log(
  `${KANBAN}: updated — ${changes.length} status change(s), ` +
    `${Object.entries(next.byStatus).map(([k, v]) => `${v} ${k}`).join(' / ')}.`,
);

if (staleProse.length) {
  console.log(
    `\n${BACKLOG}: ${staleProse.length} row(s) listed above still need their Status cell updated ` +
      `by hand. The board is now correct; the backlog prose is not.`,
  );
}
