#!/usr/bin/env bash
# PreToolUse hook: blocks `git commit` when the staged diff matches a secret pattern.
# Reads the Claude Code tool-call JSON from stdin. Exit 0 = allow, exit 2 = block (message fed back to Claude).
set -euo pipefail

input=$(cat)
command=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')

if [[ "$command" != *"git commit"* ]]; then
    exit 0
fi

# Dev-only config intentionally checked in with local, non-production secrets.
staged=$(git diff --cached --unified=0 \
    -- . \
    ':(exclude,glob)**/appsettings.Development.json' \
    ':(exclude,glob)**/launchSettings.json' \
    || true)

patterns=(
    'password\s*='
    'Server=.*;Password='
    'apikey'
    'BEGIN (RSA )?PRIVATE KEY'
)

for pattern in "${patterns[@]}"; do
    if printf '%s' "$staged" | grep -qiE "$pattern"; then
        {
            echo "Blocked: staged changes match secret pattern '$pattern'."
            echo "Move the value to user secrets or environment variables, then commit."
        } >&2
        exit 2
    fi
done

exit 0
