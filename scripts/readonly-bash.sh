#!/usr/bin/env bash
# PreToolUse hook for read-only review agents (convention-reviewer, ui-design).
# Allows only read-only commands in a Bash call and blocks everything else.
# Reads the Claude Code tool-call JSON from stdin. Exit 0 = allow, exit 2 = block.
set -euo pipefail

input=$(cat)
command=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')
[[ -z "$command" ]] && exit 0

reason=$(COMMAND="$command" python3 -I "$(dirname "$0")/readonly-bash-check.py")

if [[ -n "$reason" ]]; then
    echo "Blocked: this agent is read-only, and $reason. Use rg, git diff, git show, git log, ls or cat, and report the finding instead of changing anything." >&2
    exit 2
fi
exit 0
