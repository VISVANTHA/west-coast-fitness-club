#!/usr/bin/env bash
# Stage 1 - Build: backend (warnings are errors) and frontend production bundle.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet --version
node --version
npm --version

# Locked mode fails if packages.lock.json is out of date with the project files.
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release

npm --prefix frontend ci
npm --prefix frontend run build
