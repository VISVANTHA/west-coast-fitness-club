#!/usr/bin/env bash
# Stage 2 - Test: xUnit with coverlet (Cobertura) and Vitest with v8 coverage (Cobertura).
set -euo pipefail
cd "$(dirname "$0")/.."

out="${COVERAGE_OUTPUT:-artifacts/coverage}"
rm -rf "$out"
mkdir -p "$out"

dotnet restore --locked-mode
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory "$out/backend" \
  --logger "trx;LogFileName=test-results.trx" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

npm --prefix frontend ci
npm --prefix frontend run test:coverage
mkdir -p "$out/frontend"
cp frontend/coverage/cobertura-coverage.xml "$out/frontend/"

node ci/coverage-summary.mjs "$out/backend" "${BACKEND_COVERAGE_MIN:-80}" backend
node ci/coverage-summary.mjs "$out/frontend" "${FRONTEND_COVERAGE_MIN:-80}" frontend
