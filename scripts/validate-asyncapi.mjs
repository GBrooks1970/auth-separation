#!/usr/bin/env node
/**
 * Validates the AsyncAPI event contract.
 *
 * The events file is the only sanctioned cross-service channel in this
 * architecture, so a structural break here is a break in every service at once.
 * Errors fail the gate; warnings and below are reported and tolerated, matching
 * how `redocly lint` treats the OpenAPI files.
 */
import { readFile } from 'node:fs/promises';
import { Parser } from '@asyncapi/parser';

const SPEC = 'specs/auth-separation_events_v1.yaml';

// Spectral severities: 0 = error, 1 = warning, 2 = information, 3 = hint.
const SEVERITY = ['error', 'warning', 'info', 'hint'];

const source = await readFile(SPEC, 'utf8');
const { document, diagnostics } = await new Parser().parse(source);

const errors = diagnostics.filter((d) => d.severity === 0);
const warnings = diagnostics.filter((d) => d.severity === 1);

for (const d of [...errors, ...warnings]) {
  const line = d.range?.start?.line;
  const at = line === undefined ? SPEC : `${SPEC}:${line + 1}`;
  console.log(`  [${SEVERITY[d.severity]}] ${at} — ${d.message}`);
}

if (errors.length > 0 || document === undefined) {
  console.error(`\n${SPEC}: FAILED — ${errors.length} error(s).`);
  process.exit(1);
}

const channels = document.channels().all().length;
const operations = document.operations().all().length;
console.log(
  `${SPEC}: valid AsyncAPI ${document.version()} — ` +
    `${channels} channel(s), ${operations} operation(s), ${warnings.length} warning(s).`,
);
