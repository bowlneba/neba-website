#!/usr/bin/env bash
# Fails unless <Version> in Directory.Build.props is greater than a release tag.
# Usage: check-version-bump.sh [tag]   (default: latest v* tag)
# Needs tags fetched (actions/checkout with fetch-depth: 0).
set -euo pipefail

props="Directory.Build.props"
tag="${1:-$(git tag --list 'v*' --sort=-v:refname | head -1)}"

version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$props" | head -1)"
if [ -z "$version" ]; then
  echo "::error::No <Version> found in $props."
  exit 1
fi

if [ -z "$tag" ]; then
  echo "::notice::No v* tag found; skipping the version bump check (version is $version)."
  exit 0
fi

released="${tag#v}"
if [ "$version" = "$released" ]; then
  echo "::error::Version $version in $props matches release tag $tag. Bump <Version> before merging."
  exit 1
fi

highest="$(printf '%s\n%s\n' "$version" "$released" | sort -V | tail -1)"
if [ "$highest" != "$version" ]; then
  echo "::error::Version $version in $props is lower than release tag $tag."
  exit 1
fi

echo "Version $version is greater than $tag."
