#!/usr/bin/env node
/**
 * Fails if the generated stubs differ from what their specifications produce.
 *
 * This is the only way to enforce `AUTH-020` criterion 4 — generated files are
 * never hand-edited. Review cannot tell a regenerated file from an edited one, so
 * the check regenerates and requires the result to be identical to what is
 * committed. Regenerating in place is safe: generation is deterministic, and a
 * developer who has hand-edited a generated file wants it reverted anyway.
 *
 * Run it as `npm run lint:generated`, which regenerates first. This script only
 * inspects the result — deliberately, because spawning `npm` from Node is
 * platform-specific (Node 24 on Windows refuses to `execFileSync` a `.cmd`), and
 * npm already chains commands portably.
 *
 * Scoped to the generated directories rather than the whole tree, so unrelated
 * work in progress does not fail the gate. Untracked files count as drift too —
 * a generator that starts emitting a new file must not slip through.
 */
import { execFileSync } from 'node:child_process';

const GENERATED = 'services/*/src/*/Generated/';

const git = (...args) =>
  execFileSync('git', args, { encoding: 'utf8' })
    .split(/\r?\n/)
    .filter((line) => line.includes('/Generated/'));

// Two questions, two commands, and the choice of each matters.
//
// `git diff` for tracked files, NOT `git status`: only diff applies the
// `.gitattributes` eol filter. NSwag writes CRLF on Windows and LF on the Linux
// runner, so `git status` reports every regenerated file as modified on Windows
// even when the content is identical — a gate that cries wolf locally is a gate
// that gets ignored.
//
// `ls-files --others` for untracked files, because a diff cannot see a file git
// does not know about, and a generator that starts emitting a new one must not
// slip through.
//
// Filtered in JS rather than by a git pathspec: a glob pathspec must match the
// whole path, so `services/*/src/*/Generated` matches the directory and none of
// the files inside it — and the symptom is a gate that silently passes.
const modified = git('diff', '--name-only', '--', 'services');
const untracked = git('ls-files', '--others', '--exclude-standard', '--', 'services');
const drift = [...modified, ...untracked].filter(Boolean);

if (drift.length > 0) {
  console.error(
    `${GENERATED}: FAILED — generated stubs differ from their specifications.\n\n` +
      drift.map((file) => `  [drift] ${file}`).join('\n') +
      '\n\n' +
      'Generated files are never hand-edited (AUTH-020 criterion 4). If the stub is wrong, the contract ' +
      'is wrong: fix the specification and run `npm run generate`.',
  );
  process.exit(1);
}

console.log(`${GENERATED}: in step with the specifications.`);
