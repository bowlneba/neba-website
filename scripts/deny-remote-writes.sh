#!/usr/bin/env bash
# PreToolUse hook for the devops agent. Blocks commands that push, tag, dispatch or rerun a
# workflow, open or merge a PR, or write through gh, az, azd or curl. The agent prints those
# commands for the caller to run. Reads the Claude Code tool-call JSON from stdin.
# Exit 0 = allow, exit 2 = block.
set -euo pipefail

input=$(cat)
command=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')
[[ -z "$command" ]] && exit 0

S='[[:space:]]'
patterns=(
    "(^|[;&|[:space:]])git($S+[^;&|]*)?$S+push($S|$)"
    "(^|[;&|[:space:]])git($S+[^;&|]*)?$S+remote$S+(add|set-url|remove|rename)"
    "(^|[;&|[:space:]])gh$S+workflow$S+(run|enable|disable)"
    "(^|[;&|[:space:]])gh$S+run$S+(rerun|cancel|delete)"
    "(^|[;&|[:space:]])gh$S+pr$S+(create|merge|close|reopen|edit|comment|review|ready)"
    "(^|[;&|[:space:]])gh$S+issue$S+(create|close|reopen|edit|comment|delete)"
    "(^|[;&|[:space:]])gh$S+release$S+(create|delete|edit|upload)"
    "(^|[;&|[:space:]])gh$S+(secret|variable)$S+(set|delete|remove)"
    "(^|[;&|[:space:]])gh$S+api$S+.*(-X$S*(POST|PUT|PATCH|DELETE)|--method|-f$S|-F$S|--field|--raw-field|--input)"
    "(^|[;&|[:space:]])azd$S+(up|deploy|provision|down|pipeline|env$S+set)"
    "(^|[;&|[:space:]])az$S+.*(create|set|delete|update|deploy|add|remove|purge|start|stop|restart|import|assign|role)($S|$)"
    "(^|[;&|[:space:]])curl$S+.*(-X$S*(POST|PUT|PATCH|DELETE)|--data|--form|--upload-file|-d$S|-F$S|-T$S)"
)

# git tag creates a tag unless it only lists or inspects (bare `git tag` lists).
tag_any="(^|[;&|[:space:]])git($S+[^;&|]*)?$S+tag($S|$)"
tag_read="(^|[;&|[:space:]])git($S+[^;&|]*)?$S+tag($S*$|$S*[;&|]|$S+(-l|--list|--sort|--contains|--points-at|--merged|--no-merged|-n|-v|--verify)($S|=|$))"
if [[ "$command" =~ $tag_any && ! "$command" =~ $tag_read ]]; then
    echo "Blocked: the devops agent never creates a tag. Print this command for the caller to run instead." >&2
    exit 2
fi

for p in "${patterns[@]}"; do
    if [[ "$command" =~ $p ]]; then
        echo "Blocked: the devops agent never pushes, tags, dispatches a workflow, opens or merges a PR, or writes to Azure or GitHub. Print this command for the caller to run instead." >&2
        exit 2
    fi
done
exit 0
