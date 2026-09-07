#!/usr/bin/env node
/**
 * Guards the Kanban's ticket CONTENT against decisions that have moved past it.
 *
 * WHY THIS EXISTS, AND WHY IT IS NOT PART OF THE STATUS DRIFT-GATE
 *
 * The shared generator (portfolio-kanban-generator, run by `npm run lint:kanban`)
 * gates ticket *status*, which is gateable because it is derived from two places:
 * the backlog authors Done/Parked and the dependency graph, and the committed
 * board can be diffed against a fresh generation. Content — acceptance criteria,
 * descriptions, spec notes — has a single authoritative home (`docs/kanban-content.json`,
 * embedded verbatim into the board), and a single source cannot be diffed against
 * itself.
 *
 * The failure this catches is also the harder direction. Checksums and
 * reviewed-at stamps detect content that changed when it should not have.
 * `AUTH-020` drifted the other way: it kept four acceptance criteria after a
 * decision gave it a fifth, and its spec note went on offering a generator
 * choice that `ADR-0006` had already settled. Detecting *absent* change needs an
 * external expectation, so this script requires decisions to declare one.
 *
 * TWO CHECKS
 *
 * 1. Declared amendments resolve. An ADR that changes a ticket's content says so
 *    explicitly:
 *
 *        **Amends tickets:** AUTH-020, AUTH-021
 *
 *    and every ticket it names must cite that ADR in its payload. Declaration is
 *    deliberately opt-in rather than inferred from prose: ADRs mention ticket ids
 *    constantly as illustration — `ADR-0004` names eight and amends none — so
 *    inferring amendments would produce a false-positive machine that gets muted
 *    within a week.
 *
 * 2. Cited ADRs exist. Any `ADR-nnnn` appearing in the payload must resolve to a
 *    file in docs/adr/, catching typos, renumbering and deletions.
 *
 * WHAT THIS DOES NOT DO — stated so nobody mistakes a green run for more than it
 * is. Check 1 gates the *link*, not the *semantics*: citing `ADR-0006` in a spec
 * note satisfies it whether or not the criterion was actually added. It makes
 * forgetting loud; it cannot make you correct. And nothing here would have
 * caught the second `AUTH-020` defect — a sentence quietly falsified by a
 * decision that never declared it. That needs a human reading the ticket, which
 * is what the convention in docs/adr/README.md is for.
 */
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';

const KANBAN = 'auth-separation_implementation-kanban_v1.html';
const ADR_DIR = 'docs/adr';

const html = readFileSync(KANBAN, 'utf8');
const match = html.match(/id="payload-tickets" type="application\/json">([\s\S]*?)<\/script>/);
if (!match) {
  console.error(`${KANBAN}: FAILED — no <script id="payload-tickets"> payload found.`);
  process.exit(1);
}
const tickets = JSON.parse(match[1]);
const byId = new Map(tickets.map((t) => [t.id, JSON.stringify(t)]));

const problems = [];
const adrFiles = readdirSync(ADR_DIR).filter((f) => /^\d{4}-.*\.md$/.test(f));

// --- Check 1: declared amendments resolve ------------------------------------
let declared = 0;
for (const file of adrFiles) {
  const adrId = `ADR-${file.slice(0, 4)}`;
  const text = readFileSync(join(ADR_DIR, file), 'utf8');
  const line = text.match(/^\*\*Amends tickets:\*\*\s*(.+)$/m);
  if (!line) continue;

  const ids = line[1].match(/AUTH-\d{3}/g) ?? [];
  if (ids.length === 0) {
    problems.push(`${file}: "Amends tickets:" declared but names no AUTH-nnn ticket`);
    continue;
  }
  declared += ids.length;

  for (const id of ids) {
    const payload = byId.get(id);
    if (payload === undefined) {
      problems.push(`${file}: declares it amends ${id}, which is not a ticket in the board`);
    } else if (!payload.includes(adrId)) {
      problems.push(
        `${adrId} declares it amends ${id}, but ${id}'s payload does not cite ${adrId} — ` +
          `the decision was recorded and the ticket was not updated`,
      );
    }
  }
}

// --- Check 2: cited ADRs exist -----------------------------------------------
const cited = new Map();
for (const t of tickets) {
  for (const ref of JSON.stringify(t).match(/ADR-\d{4}/g) ?? []) {
    if (!cited.has(ref)) cited.set(ref, []);
    if (!cited.get(ref).includes(t.id)) cited.get(ref).push(t.id);
  }
}
for (const [ref, ids] of cited) {
  const exists = adrFiles.some((f) => f.startsWith(ref.slice(4)));
  if (!exists) {
    problems.push(`${ids.join(', ')} cite ${ref}, which has no file in ${ADR_DIR}/`);
  }
}

// --- Report -------------------------------------------------------------------
for (const p of problems) console.log(`  [content] ${p}`);

if (problems.length > 0) {
  console.error(`\n${KANBAN}: FAILED — ${problems.length} content inconsistency(ies).`);
  process.exit(1);
}

console.log(
  `${KANBAN}: content consistent — ${declared} declared amendment(s) across ` +
    `${adrFiles.length} ADR(s); ${cited.size} cited ADR(s) all resolve.`,
);
