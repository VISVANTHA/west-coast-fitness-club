#!/usr/bin/env bash
# Installs the pinned Node.js toolchain into a dotnet SDK image (GitLab CI and Bitbucket Pipelines).
set -euo pipefail

NODE_VERSION="22.14.0"
NPM_VERSION="10.9.2"

if command -v node > /dev/null && [ "$(node --version)" = "v${NODE_VERSION}" ]; then
  echo "Node ${NODE_VERSION} already installed."
else
  curl -fsSL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz" -o /tmp/node.tar.xz
  tar -xJf /tmp/node.tar.xz -C /usr/local --strip-components=1
  rm /tmp/node.tar.xz
fi

npm install --global "npm@${NPM_VERSION}"
node --version
npm --version
