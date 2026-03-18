#!/usr/bin/env node
// Usage:
//   npm run mutation:ai              → summary table of surviving mutations by file
//   npm run mutation:ai -- NavMenu   → full LLM-ready output for files matching "NavMenu"

import { readFileSync, existsSync, statSync } from 'node:fs';
import { resolve, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(fileURLToPath(new URL('.', import.meta.url)), '..');
const REPORT_PATH = resolve(ROOT, 'reports/mutation/mutation.json');
const fileFilter = process.argv[2]?.toLowerCase();

if (!existsSync(REPORT_PATH)) {
  console.error('❌  No JSON mutation report found at: ' + REPORT_PATH);
  console.error('   Run: npm run test:mutation:local   (generates both html + json)');
  process.exit(1);
}

const reportMtime = statSync(REPORT_PATH).mtime;
const report = JSON.parse(readFileSync(REPORT_PATH, 'utf-8'));

// ── helpers ───────────────────────────────────────────────────────────────────

function extractFragment(source, { start, end }) {
  const lines = source.split('\n');
  if (start.line === end.line) {
    return lines[start.line - 1].slice(start.column, end.column);
  }
  const parts = [lines[start.line - 1].slice(start.column)];
  for (let l = start.line + 1; l < end.line; l++) parts.push(lines[l - 1]);
  parts.push(lines[end.line - 1].slice(0, end.column));
  return parts.join(' ↵ ');
}

function contextBlock(source, { start, end }) {
  const lines = source.split('\n');
  const from = Math.max(0, start.line - 4);
  const to = Math.min(lines.length - 1, end.line + 2);
  const numW = String(to + 1).length;
  return lines.slice(from, to + 1).map((content, i) => {
    const lineNum = from + i + 1;
    const arrow = lineNum >= start.line && lineNum <= end.line ? '→' : ' ';
    return `${String(lineNum).padStart(numW)} ${arrow} ${content}`;
  }).join('\n');
}

// ── collect ───────────────────────────────────────────────────────────────────

let grandTotal = 0;
let grandSurvived = 0;
const allFiles = [];   // [{ relPath, survived, source, mutants }]

for (const [absPath, fileData] of Object.entries(report.files)) {
  grandTotal += fileData.mutants.length;
  const survived = fileData.mutants.filter(m => m.status === 'Survived');
  grandSurvived += survived.length;
  if (survived.length === 0) continue;
  allFiles.push({
    relPath: relative(ROOT, absPath),
    source: fileData.source,
    mutants: survived,
  });
}

allFiles.sort((a, b) => b.mutants.length - a.mutants.length);

const score = grandTotal === 0
  ? null
  : ((grandTotal - grandSurvived) / grandTotal * 100).toFixed(2);
const scoreLabel = score === null ? 'N/A' : `${score}%`;

// ── output ────────────────────────────────────────────────────────────────────

const out = [];

out.push('# Surviving Mutations Report', '', `**Score**: ${scoreLabel}  |  **Survived**: ${grandSurvived} / ${grandTotal}  |  **Report**: ${reportMtime.toLocaleString()}`, '');

if (!fileFilter) {
  // Summary mode — list files with counts
  out.push('## Files with surviving mutations', '', '| File | Survived |', '|---|---:|');
  for (const { relPath, mutants } of allFiles) {
    const name = relPath.split('/').pop();
    out.push(`| \`${name}\` | ${mutants.length} |`);
  }
  out.push('', '**Tip**: Drill into a single file:', '```', 'npm run mutation:ai -- NavMenu', 'npm run mutation:ai -- NebaDocument', 'npm run mutation:ai -- telemetry', '```');
  console.log(out.join('\n'));
  process.exit(0);
}

// Detailed mode — full output for matched file(s)
const matched = allFiles.filter(f => f.relPath.toLowerCase().includes(fileFilter));

if (matched.length === 0) {
  console.error(`❌  No files with surviving mutations matched "${fileFilter}"`);
  process.exit(1);
}

out.push('> **Task**: Add or update tests in the test file to kill each surviving mutation.', '> A mutation is **killed** when at least one test *fails* on the mutated code.', '> **"not covered"** → needs a new test that exercises this code path.', '> **"covered, survived"** → needs a sharper assertion on the existing test path.', '', '---', '');

for (const { relPath, source, mutants } of matched) {
  const testFile = relPath.replace(/\.js$/, '.tests.js');
  const sorted = [...mutants].sort((a, b) => a.location.start.line - b.location.start.line);

  out.push(`## \`${relPath}\``, `**Test file**: \`${testFile}\`  |  **Survived**: ${mutants.length}`, '');

  sorted.forEach((m, idx) => {
    const { start, end } = m.location;
    const lineLabel = start.line === end.line ? `Line ${start.line}` : `Lines ${start.line}–${end.line}`;
    const original = extractFragment(source, m.location);
    const covered = m.coveredBy?.length > 0
      ? `covered by ${m.coveredBy.length} test(s) — needs sharper assertion`
      : 'not covered — needs new test path';

    out.push(`### ${idx + 1}. ${m.mutatorName} — ${lineLabel}`, `- **Original**: \`${original}\``, `- **Mutant**:   \`${m.replacement}\``, `- **Coverage**: ${covered}`, '', '```js', contextBlock(source, m.location), '```', '');
  });

  out.push('---', '');
}

console.log(out.join('\n'));
