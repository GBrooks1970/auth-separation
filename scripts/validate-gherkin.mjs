#!/usr/bin/env node
/**
 * Parses the acceptance criteria with the real Cucumber Gherkin parser.
 *
 * The feature file is the executable acceptance layer (AUTH-070 stands it up as
 * the suite). Parsing it here means a malformed scenario is caught now rather
 * than when someone first tries to run it against services that do not exist yet.
 *
 * KNOWN STRUCTURAL ISSUE (AS-05): the file bundles several `Feature:` blocks.
 * The Gherkin grammar permits exactly one Feature per file, so no Cucumber-family
 * runner can execute it as it stands — it must be split first. Until that
 * decision is taken, this validator splits the file on `Feature:` boundaries and
 * parses each block independently, so the scenario syntax is still genuinely
 * checked rather than waved through.
 *
 * When AS-05 is resolved, set ACCEPT_BUNDLED_FEATURES to false. The validator
 * then requires one Feature per file and this file fails until it is split.
 */
import { readFile } from 'node:fs/promises';
import { AstBuilder, GherkinClassicTokenMatcher, Parser } from '@cucumber/gherkin';
import { IdGenerator } from '@cucumber/messages';

const SPEC = 'auth-separation_acceptance_v1.feature';
const ACCEPT_BUNDLED_FEATURES = true; // flip to false once AS-05 splits the file

const source = await readFile(SPEC, 'utf8');
const lines = source.split(/\r?\n/);

/**
 * Split into one chunk per `Feature:` at column 0, keeping the leading comment
 * header with the first chunk so line-ish context survives.
 */
const starts = lines.reduce((acc, line, i) => (/^Feature:/.test(line) ? [...acc, i] : acc), []);
if (starts.length === 0) {
  console.error(`${SPEC}: FAILED — no Feature declared.`);
  process.exit(1);
}
const blocks = starts.map((start, i) => ({
  firstLine: start + 1,
  text: lines.slice(i === 0 ? 0 : start, starts[i + 1] ?? lines.length).join('\n'),
}));

const parser = new Parser(new AstBuilder(IdGenerator.uuid()), new GherkinClassicTokenMatcher());
let scenarios = 0;
let failed = false;

for (const block of blocks) {
  let document;
  try {
    document = parser.parse(block.text);
  } catch (error) {
    console.error(`  [error] ${SPEC}:${block.firstLine} — ${error.message}`);
    failed = true;
    continue;
  }
  const children = document.feature?.children ?? [];
  const count = children.filter((c) => c.scenario).length;
  scenarios += count;
  console.log(`  ${SPEC}:${block.firstLine} — "${document.feature.name}" (${count} scenario(s))`);
}

if (failed) {
  console.error(`\n${SPEC}: FAILED — one or more Feature blocks did not parse.`);
  process.exit(1);
}

if (blocks.length > 1) {
  const message =
    `${SPEC}: bundles ${blocks.length} Feature blocks in one file. ` +
    'Gherkin allows one Feature per file, so this is not runnable by any ' +
    'Cucumber-family runner until it is split (AS-05).';
  if (!ACCEPT_BUNDLED_FEATURES) {
    console.error(`\n${SPEC}: FAILED — ${message}`);
    process.exit(1);
  }
  console.log(`\n  [known issue] ${message}`);
}

console.log(
  `${SPEC}: valid Gherkin — ${scenarios} scenario(s) across ${blocks.length} Feature block(s).`,
);
