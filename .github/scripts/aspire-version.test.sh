#!/usr/bin/env bash
# Tests aspire-version.sh against fixture props files and the real Directory.Packages.props.
set -euo pipefail

script="$(dirname "${BASH_SOURCE[0]}")/aspire-version.sh"
repo_props="$(dirname "${BASH_SOURCE[0]}")/../../Directory.Packages.props"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
failures=0

fail() { echo "FAIL: $1" >&2; failures=$((failures + 1)); }

# Reads the AppHost version, ignoring other Aspire packages that may carry different versions.
cat > "$tmp/mixed.props" <<'PROPS'
<Project>
  <ItemGroup>
    <PackageVersion Include="Aspire.Hosting.Azure.Storage" Version="1.1.1" />
    <PackageVersion Include="Aspire.Hosting.AppHost" Version="17.2.3" />
    <PackageVersion Include="Aspire.Hosting.Testing" Version="9.9.9" />
  </ItemGroup>
</Project>
PROPS
actual="$("$script" "$tmp/mixed.props")"
[[ "$actual" = "17.2.3" ]] || fail "mixed props: expected 17.2.3, got '$actual'"

# Pre-release versions come back whole.
sed 's/17.2.3/17.0.0-preview.1/' "$tmp/mixed.props" > "$tmp/preview.props"
actual="$("$script" "$tmp/preview.props")"
[[ "$actual" = "17.0.0-preview.1" ]] || fail "preview props: expected 17.0.0-preview.1, got '$actual'"

# A missing entry exits non-zero.
grep -v 'Aspire.Hosting.AppHost' "$tmp/mixed.props" > "$tmp/missing.props"
if "$script" "$tmp/missing.props" > /dev/null 2>&1; then
  fail "missing entry: expected non-zero exit"
fi

# The real props file yields the version CI will install.
actual="$("$script" "$repo_props")"
[[ "$actual" =~ ^[0-9]+\.[0-9]+\.[0-9]+ ]] || fail "repo props: '$actual' is not a version"
expected="$(grep -o 'Include="Aspire.Hosting.AppHost" Version="[^"]*"' "$repo_props" | sed 's/.*Version="//;s/"//')"
[[ "$actual" = "$expected" ]] || fail "repo props: expected $expected, got '$actual'"

if [[ "$failures" -gt 0 ]]; then
  echo "$failures failure(s)" >&2
  exit 1
fi
echo "aspire-version.sh: all tests passed"
