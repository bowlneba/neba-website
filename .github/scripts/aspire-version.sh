#!/usr/bin/env bash
# Prints the Aspire version: the Aspire.Hosting.AppHost PackageVersion in Directory.Packages.props.
# CI installs the Aspire CLI at this version, so the CLI always matches the AppHost packages.
set -euo pipefail

props="$(dirname "${BASH_SOURCE[0]}")/../../Directory.Packages.props"
version="$(sed -n 's/.*Include="Aspire\.Hosting\.AppHost"[[:space:]]*Version="\([^"]*\)".*/\1/p' "$props")"

if [ -z "$version" ]; then
  echo "Aspire.Hosting.AppHost PackageVersion not found in Directory.Packages.props" >&2
  exit 1
fi

echo "$version"
