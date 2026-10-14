#!/usr/bin/env bash
# Checks <Version> in Directory.Build.props.
#   --base <branch>  PR target. release/vX.Y.Z: version must equal X.Y.Z and be greater
#                    than the latest v* tag. Any other branch: version must be greater
#                    than the version on origin/<branch> and not lower than the latest tag.
#   --tag <tag>      Version must be greater than that release tag (hotfixes).
#   (no arguments)   Version must be greater than the latest v* tag.
# Needs branches and tags fetched (actions/checkout with fetch-depth: 0).
set -euo pipefail

props="Directory.Build.props"
mode="latest"
arg=""
case "${1:-}" in
  --base) mode="base"; arg="${2:?--base needs a branch}" ;;
  --tag) mode="tag"; arg="${2:?--tag needs a tag}" ;;
  "") ;;
  *) echo "Unknown argument: ${1}" >&2; exit 2 ;;
esac

read_version() { sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' | head -1; }
newer() {
  local candidate="$1" other="$2"
  [[ "$(printf '%s\n%s\n' "$candidate" "$other" | sort -V | tail -1)" == "$candidate" && "$candidate" != "$other" ]]
}
fail() { echo "::error::$*" >&2; exit 1; }

version="$(read_version < "$props")"
[[ -n "$version" ]] || fail "No <Version> found in $props."

# Release tags only (vX.Y.Z); prerelease or ad-hoc tags such as v1.2.0-rc1 would break the version sort.
latest_tag="$(git tag --list 'v*' --sort=-v:refname | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | head -1 || true)"
latest="${latest_tag#v}"

if [[ "$mode" == "tag" ]]; then
  newer "$version" "${arg#v}" || fail "Version $version must be greater than release tag $arg. Bump <Version> in $props."
  echo "Version $version is greater than $arg."
  exit 0
fi

if [[ "$mode" == "base" && "$arg" == release/v* ]]; then
  expected="${arg#release/v}"
  [[ "$version" == "$expected" ]] || fail "Branch $arg releases $expected, but <Version> in $props is $version."
  if [[ -n "$latest" ]]; then
    newer "$version" "$latest" || fail "Release $version must be greater than the latest tag $latest_tag."
  fi
  echo "Version $version matches $arg."
  exit 0
fi

if [[ "$mode" == "base" ]]; then
  base_version="$(git show "origin/$arg:$props" 2>/dev/null | read_version || true)"
  if [[ -n "$base_version" ]]; then
    newer "$version" "$base_version" || fail "Version $version must be greater than $base_version on $arg. Bump <Version> in $props."
    if [[ -n "$latest" && "$version" != "$latest" ]]; then
      newer "$version" "$latest" || fail "Version $version is lower than the latest tag $latest_tag."
    fi
    echo "Version $version is greater than $base_version on $arg."
    exit 0
  fi
  echo "::notice::$arg has no <Version> yet; comparing with the latest tag."
fi

if [[ -z "$latest" ]]; then
  echo "::notice::No v* tag found; skipping (version is $version)."
  exit 0
fi
newer "$version" "$latest" || fail "Version $version must be greater than the latest tag $latest_tag."
echo "Version $version is greater than $latest_tag."
