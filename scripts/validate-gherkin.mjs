#!/usr/bin/env node
/**
 * Parses the acceptance criteria with the real Cucumber Gherkin parser.
 *
 * The feature files are the executable acceptance layer (AUTH-070 stands them up
 * as the suite). Parsing them here means a malformed scenario is caught now
 * rather than when someone first tries to run it against services that do not
 * exist yet.
 *
 * These were one file with seven bundled `Feature:` blocks, which no
 * Cucumber-family runner could execute (AS-05). Now that they are split, this
 * validator enforces the rule that made the split necessary: exactly one Feature
 * per file. A regression back to a bundled file fails the gate.
 */
import { readdir, readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { AstBuilder, GherkinClassicTokenMatcher, Parser } from '@cucumber/gherkin';
import { IdGenerator } from '@cucumber/messages';

const DIR = 'features';
const EXPECTED_SCENARIOS = 21; // the count carried over from the pre-split file

const files = (await readdir(DIR)).filter((f) => f.endsWith('.feature')).sort();
if (files.length === 0) {
  console.error(`${DIR}/: FAILED — no .feature files found.`);
  process.exit(1);
}

const parser = new Parser(new AstBuilder(IdGenerator.uuid()), new GherkinClassicTokenMatcher());
let scenarios = 0;
let failed = false;

for (const file of files) {
  const path = join(DIR, file);
  const source = await readFile(path, 'utf8');

  // One Feature per file is the Gherkin grammar's rule, but the parser reports a
  // second `Feature:` as a generic syntax error. Counting first gives a message
  // that names the actual problem.
  const declared = source.split(/\r?\n/).filter((l) => /^Feature:/.test(l)).length;
  if (declared !== 1) {
    console.error(`  [error] ${path} — declares ${declared} Feature blocks; exactly one is allowed.`);
    failed = true;
    continue;
  }

  let document;
  try {
    document = parser.parse(source);
  } catch (error) {
    console.error(`  [error] ${path} — ${error.message}`);
    failed = true;
    continue;
  }

  const children = document.feature?.children ?? [];
  const count = children.filter((c) => c.scenario).length;
  if (count === 0) {
    console.error(`  [error] ${path} — parsed cleanly but declares no scenarios.`);
    failed = true;
    continue;
  }
  scenarios += count;
  console.log(`  ${path} — "${document.feature.name}" (${count} scenario(s))`);
}

if (failed) {
  console.error(`\n${DIR}/: FAILED — see errors above.`);
  process.exit(1);
}

// Guards the split itself: a scenario silently lost in a future refactor is
// exactly the kind of regression a passing parse would otherwise hide.
if (scenarios !== EXPECTED_SCENARIOS) {
  console.error(
    `\n${DIR}/: FAILED — expected ${EXPECTED_SCENARIOS} scenarios, found ${scenarios}. ` +
      'If this change is intentional, update EXPECTED_SCENARIOS and say why in the commit.',
  );
  process.exit(1);
}

console.log(`${DIR}/: valid Gherkin — ${scenarios} scenario(s) across ${files.length} file(s).`);
