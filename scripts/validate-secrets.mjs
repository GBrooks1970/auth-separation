#!/usr/bin/env node
/**
 * Fails the gate if committed configuration carries a secret.
 *
 * `AUTH-001` requires that CI secrets come from a secrets store rather than
 * committed config. Nothing in this repository consumes a secret today, so that
 * requirement is easy to satisfy by accident and just as easy to break silently
 * the first time a service is wired up. This turns it into an assertion that can
 * fail.
 *
 * It scans exactly what is committed — `git ls-files` — so it cannot be fooled
 * by an untracked file and needs no ignore list for `node_modules`.
 *
 * This is a second line of defence. GitHub's secret scanning and push
 * protection are enabled on the repository and catch known provider formats at
 * push time; this catches the same classes in the gate, plus the ones specific
 * to this spec set (a stray `.env`, a private key pasted into a design doc).
 *
 * Detection is deliberately narrow. The specifications are full of the words
 * `password`, `token`, and `credential` — they describe an authentication
 * system — so keyword matching alone would be useless. Only high-signal
 * patterns fail the gate; see `docs/adr/0002-committed-secret-guard.md` for what
 * is deliberately out of scope.
 */
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

/** Binary and vendored paths where a scan is meaningless or guaranteed noisy. */
const SKIP = [/^vendor\//, /\.docx$/, /\.png$/, /\.jpg$/, /\.gif$/, /\.pdf$/, /^package-lock\.json$/];

/**
 * High-signal patterns only. Each must be something that is a secret by its very
 * shape, not something that merely sits near a secret-sounding word.
 */
const RULES = [
  {
    id: 'private-key',
    description: 'PEM private key block',
    pattern: /-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP )?PRIVATE KEY-----/,
  },
  { id: 'aws-access-key', description: 'AWS access key ID', pattern: /\bAKIA[0-9A-Z]{16}\b/ },
  {
    id: 'github-token',
    description: 'GitHub personal access token',
    pattern: /\b(?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{36,}\b|\bgithub_pat_[A-Za-z0-9_]{22,}\b/,
  },
  { id: 'stripe-key', description: 'Stripe live secret key', pattern: /\bsk_live_[A-Za-z0-9]{16,}\b/ },
  { id: 'slack-token', description: 'Slack token', pattern: /\bxox[abprs]-[A-Za-z0-9-]{10,}\b/ },
  {
    id: 'gcp-key',
    description: 'Google service-account private key blob',
    pattern: /"type"\s*:\s*"service_account"/,
  },
  {
    id: 'putty-key',
    description: 'PuTTY private key file',
    pattern: /PuTTY-User-Key-File-\d/,
  },
];

/** A committed `.env` is a secrets-in-config smell regardless of its contents. */
const ENV_FILE = /(^|\/)\.env(\.|$)/;
const ENV_ALLOWED = /(^|\/)\.env\.(example|sample|template)$/;

const files = execFileSync('git', ['ls-files'], { encoding: 'utf8' })
  .split('\n')
  .filter(Boolean)
  .filter((f) => !SKIP.some((s) => s.test(f)));

const findings = [];

for (const file of files) {
  if (ENV_FILE.test(file) && !ENV_ALLOWED.test(file)) {
    findings.push({ file, line: 0, rule: 'env-file', description: 'committed .env file' });
  }

  let content;
  try {
    content = readFileSync(file, 'utf8');
  } catch {
    continue; // unreadable or binary — nothing to assert
  }

  const lines = content.split(/\r?\n/);
  for (const [i, line] of lines.entries()) {
    for (const rule of RULES) {
      if (rule.pattern.test(line)) {
        findings.push({ file, line: i + 1, rule: rule.id, description: rule.description });
      }
    }
  }
}

for (const f of findings) {
  const at = f.line === 0 ? f.file : `${f.file}:${f.line}`;
  console.log(`  [secret] ${at} — ${f.description} (${f.rule})`);
}

if (findings.length > 0) {
  console.error(
    `\ncommitted secrets: FAILED — ${findings.length} finding(s). ` +
      `Secrets belong in the CI secrets store, never in committed config.`,
  );
  process.exit(1);
}

console.log(
  `committed files: no secrets — ${files.length} tracked file(s) scanned against ` +
    `${RULES.length + 1} rule(s).`,
);
