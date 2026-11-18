#!/usr/bin/env bash
# PreToolUse hook: before `git commit`, runs the architecture tests when the commit touches
# backend feature code. Reads the Claude Code tool-call JSON from stdin.
# Exit 0 = allow, exit 2 = block (message fed back to Claude).
#
# Only commits that change src/Neba.Api/**/*.cs or the architecture tests pay the cost.
# Docs, skills, Blazor and test-only commits pass straight through.
set -euo pipefail

input=$(cat)
command=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')

# Same `commit` subcommand match as block-secrets.sh (catches `git -C <path> commit`, etc.).
if ! [[ "$command" =~ (^|[[:space:]])git([[:space:]]+[^[:space:]]+)*[[:space:]]+commit([[:space:]]|$) ]]; then
    exit 0
fi

# `git diff HEAD` covers staged and unstaged tracked changes, so `commit -a` is caught too.
changed=$(git diff HEAD --name-only -- 'src/Neba.Api/**/*.cs' 'tests/Neba.Api.Tests/Architecture/**' || true)
if [[ -z "$changed" ]]; then
    exit 0
fi

if output=$(dotnet test --project tests/Neba.Api.Tests --filter-class "*FeatureBoundaryTests" 2>&1); then
    exit 0
fi

{
    echo "Architecture tests failed, so this commit is blocked. Fix the boundary violation, then commit again."
    echo "Rule: a use-case folder must not depend on another feature's use-case code (CLAUDE.md, Feature Boundaries)."
    echo
    printf '%s\n' "$output" | tail -40
} >&2
exit 2
