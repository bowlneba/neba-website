#!/usr/bin/env bash
# Prints the Aspire version: the Aspire.Hosting.AppHost PackageVersion in Directory.Packages.props.
# CI installs the Aspire CLI at this version, so the CLI always matches the AppHost packages.
# Usage: aspire-version.sh [path-to-Directory.Packages.props]
set -euo pipefail

props="${1:-$(dirname "${BASH_SOURCE[0]}")/../../Directory.Packages.props}"
version="$(sed -n 's/.*Include="Aspire\.Hosting\.AppHost"[[:space:]]*Version="\([^"]*\)".*/\1/p' "$props")"

if [[ -z "$version" ]]; then
  echo "Aspire.Hosting.AppHost PackageVersion not found in Directory.Packages.props" >&2
  exit 1
fi

# aspire.config.json pins the SDK too. It must match, so a bump in Directory.Packages.props cannot leave it stale.
config="$(dirname "$props")/aspire.config.json"
if [[ -f "$config" ]]; then
  pinned="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$config")"
  if [[ -n "$pinned" && "$pinned" != "$version" ]]; then
    echo "aspire.config.json sdk.version ($pinned) differs from Aspire.Hosting.AppHost in Directory.Packages.props ($version)" >&2
    exit 1
  fi
fi

echo "$version"
