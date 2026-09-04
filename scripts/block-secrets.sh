#!/usr/bin/env bash
# PreToolUse hook: blocks `git commit` when the staged diff matches a secret pattern.
# Reads the Claude Code tool-call JSON from stdin. Exit 0 = allow, exit 2 = block (message fed back to Claude).
set -euo pipefail

input=$(cat)
command=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')

# Word-boundary match on the `commit` subcommand so `git -C <path> commit ...`,
# `git --no-pager commit ...`, etc. are still caught (a plain substring check
# on "git commit" misses any flag inserted between `git` and `commit`).
if ! [[ "$command" =~ (^|[[:space:]])git([[:space:]]+[^[:space:]]+)*[[:space:]]+commit([[:space:]]|$) ]]; then
    exit 0
fi

exclude_paths=(
    ':(exclude,glob)**/appsettings.Development.json'
    ':(exclude,glob)**/launchSettings.json'
)

# `git diff HEAD` (not `--cached`) so tracked-file changes are caught whether
# they're staged, unstaged, or about to be staged by this same command via
# `commit -a`/`--all` — a hook that only checked `--cached` would miss a
# secret added to a tracked file and committed with `git commit -am ...`.
staged=$(git diff HEAD --unified=0 -- . "${exclude_paths[@]}" || true)

# Also scan untracked files this command is about to add+commit in one shot
# (`git add newfile && git commit ...`) — at hook time they aren't staged
# yet, so `git diff` alone would never see their content.
while IFS= read -r path; do
    [[ -f "$path" ]] || continue
    if [[ "$path" == *"appsettings.Development.json" || "$path" == *"launchSettings.json" ]]; then
        continue
    fi
    staged+=$'\n'"$(cat -- "$path")"
done < <(git ls-files --others --exclude-standard)

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
