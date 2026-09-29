#!/usr/bin/env bash
# Stage 3 - Scan: vulnerable packages (NuGet and npm), duplication (jscpd) and lint (eslint).
#
# SCAN_MODE=strict  (default) exits 1 when any scanner reports a finding.
# SCAN_MODE=report  always exits 0 after every scanner has run; findings are recorded in
#                   scan-results/summary.md. Used by the negative branches, whose seeded
#                   findings are the point of the exercise.
set -uo pipefail
cd "$(dirname "$0")/.."

mode="${SCAN_MODE:-strict}"
out="${SCAN_OUTPUT:-scan-results}"
rm -rf "$out"
mkdir -p "$out"
: > "$out/summary.md"
failures=0

record() { # name, exit code, detail
  local status="clean"
  if [ "$2" -ne 0 ]; then
    status="FINDINGS"
    failures=$((failures + 1))
  fi
  echo "| $1 | $status | $3 |" >> "$out/summary.md"
  echo ">>> $1: $status ($3)"
}

echo "| Scanner | Result | Detail |" >> "$out/summary.md"
echo "| --- | --- | --- |" >> "$out/summary.md"

# Restore first so every scanner sees the same dependency graph as the build.
dotnet restore --locked-mode > "$out/dotnet-restore.txt" 2>&1 || { cat "$out/dotnet-restore.txt"; exit 1; }
npm --prefix frontend ci > "$out/npm-ci.txt" 2>&1 || { cat "$out/npm-ci.txt"; exit 1; }

# 1. NuGet: `dotnet list package --vulnerable` exits 0 even when it finds something, so read its output.
dotnet list package --vulnerable --include-transitive > "$out/nuget-vulnerable.txt" 2>&1
cat "$out/nuget-vulnerable.txt"
if grep -q "has the following vulnerable packages" "$out/nuget-vulnerable.txt"; then
  record "dotnet list package --vulnerable" 1 "$(grep -cE '^\s+> ' "$out/nuget-vulnerable.txt") vulnerable package reference(s)"
else
  record "dotnet list package --vulnerable" 0 "no vulnerable packages"
fi

# 2. npm audit: high and critical advisories are findings; moderate and low are listed only.
npm --prefix frontend audit --json > "$out/npm-audit.json" 2>/dev/null
npm --prefix frontend audit > "$out/npm-audit.txt" 2>&1
cat "$out/npm-audit.txt"
npm --prefix frontend audit --audit-level=high > /dev/null 2>&1
record "npm audit --audit-level=high" $? "$(grep -E 'vulnerabilit' "$out/npm-audit.txt" | tail -1 | sed 's/^ *//' || true)"

# 3. jscpd: duplication across backend and frontend, configured by .jscpd.json (threshold in percent).
frontend/node_modules/.bin/jscpd . --output "$out/jscpd" > "$out/jscpd.txt" 2>&1
code=$?
cat "$out/jscpd.txt"
record "jscpd" $code "$(grep -E 'Found [0-9]+ clones' "$out/jscpd.txt" | sed 's/\x1b\[[0-9;]*m//g' | tail -1)"

# 4. eslint: flat config in frontend/eslint.config.js.
npm --prefix frontend run lint -- --format json --output-file "../$out/eslint-report.json" > /dev/null 2>&1
lint_code=$?
npm --prefix frontend run lint > "$out/eslint.txt" 2>&1
cat "$out/eslint.txt"
record "eslint" $lint_code "$(grep -E 'problems?' "$out/eslint.txt" | tail -1 | sed 's/^ *//' || true)"

echo
cat "$out/summary.md"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  { echo "### Scan results ($mode mode)"; cat "$out/summary.md"; } >> "$GITHUB_STEP_SUMMARY"
fi

if [ "$failures" -gt 0 ] && [ "$mode" = "strict" ]; then
  echo "Scan stage failed: $failures scanner(s) reported findings."
  exit 1
fi
echo "Scan stage completed ($failures scanner(s) with findings, mode=$mode)."
exit 0
