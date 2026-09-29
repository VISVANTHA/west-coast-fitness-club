// Merges every Cobertura report under a directory and prints line coverage.
// Usage: node ci/coverage-summary.mjs <directory> <minimum-percent> [label]
// Exits 1 when the merged line coverage is below the minimum and COVERAGE_ENFORCE is not "false".
import { readdirSync, readFileSync, statSync, appendFileSync } from 'node:fs';
import { join } from 'node:path';

const [directory, minimum = '0', label = 'coverage'] = process.argv.slice(2);
if (!directory) {
  console.error('Usage: node ci/coverage-summary.mjs <directory> <minimum-percent> [label]');
  process.exit(2);
}

function* reports(dir) {
  for (const entry of readdirSync(dir)) {
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) {
      yield* reports(path);
    } else if (/cobertura.*\.xml$/i.test(entry)) {
      yield path;
    }
  }
}

const hits = new Map();
let files = 0;
for (const report of reports(directory)) {
  files += 1;
  const xml = readFileSync(report, 'utf8');
  for (const cls of xml.matchAll(/<class\b[^>]*\bfilename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g)) {
    for (const line of cls[2].matchAll(/<line\b[^>]*\bnumber="(\d+)"[^>]*\bhits="(\d+)"/g)) {
      const key = `${cls[1]}:${line[1]}`;
      hits.set(key, Math.max(hits.get(key) ?? 0, Number(line[2])));
    }
  }
}

const total = hits.size;
const covered = [...hits.values()].filter((count) => count > 0).length;
const percent = total === 0 ? 0 : (covered / total) * 100;
const message = `${label}: ${percent.toFixed(1)}% line coverage (${covered}/${total} lines, ${files} report(s)), minimum ${minimum}%`;
console.log(message);
if (process.env.GITHUB_STEP_SUMMARY) {
  appendFileSync(process.env.GITHUB_STEP_SUMMARY, `- ${message}\n`);
}

if (files === 0 || (percent < Number(minimum) && process.env.COVERAGE_ENFORCE !== 'false')) {
  console.error(files === 0 ? 'No Cobertura reports found.' : `Coverage is below the ${minimum}% minimum.`);
  process.exit(1);
}
