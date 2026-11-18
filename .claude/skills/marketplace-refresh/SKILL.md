---
name: marketplace-refresh
description: Check the plugin marketplaces (dotnet/skills, azure-skills, claude-plugins-official) for new versions, apply updates, and report what changed. If any update was applied, end with a prompt to paste into a fresh session so it starts with the new plugin set. Use before planning or building anything that reuses marketplace skills or agents. Usage: /marketplace-refresh
---

# Marketplace refresh

Update every plugin marketplace and installed plugin, then say plainly what changed. Plugin skills load at session start, so a session that ran an update still works from the old set. The last output hands the next session what it needs.

## Step 1. Record the starting state

Before updating, note for each marketplace in `~/.claude/plugins/known_marketplaces.json`:

- the install path and, if it is a git clone, its commit: `git -C <installLocation> log -1 --format='%h %ci %s'` (some marketplaces are not git clones; record "not a git repo" and move on)
- every installed plugin and version: `claude plugin list`

## Step 2. Update

1. `claude plugin marketplace update`
2. For each installed plugin from `claude plugin list`: `claude plugin update <plugin>@<marketplace>`. The output says either "already at the latest version" or "updated from X to Y".

Do not install new plugins, enable or disable plugins, or edit `.claude/settings.json` here. Report those as suggestions.

## Step 3. Read what changed

For each marketplace whose commit moved, and each plugin whose version moved:

- `git -C <installLocation> log <old>..<new> --oneline` (limit to the plugin's folder with `-- plugins/<name>` where the layout allows)
- skim the plugin's README or release notes for new, renamed or removed skills and agents
- flag overlap with this repo's `.claude/skills/` and `.claude/agents/`, and anything that contradicts CLAUDE.md or `docs/learnings/`

## Step 4. Report

One short table: marketplace or plugin, old, new, one-line note. Then list any new plugins worth a look and any overlap found. If nothing moved, say "No updates" and stop. Do not print a prompt.

## Step 5. Final output: the handoff prompt

Only if at least one update was applied (a marketplace commit moved or a plugin version changed), end the response with a fenced block the user can paste into a new session. Nothing may come after it. Fill it from this run; never leave a placeholder.

````text
Plugins were updated in the last session; start from this state.

Updated:
- <plugin or marketplace>: <old> -> <new> (<one-line note>)

Unchanged: <list>

Read first:
- <changed skill or agent files worth reading, with paths>

Overlap or conflicts to resolve:
- <item, or "none found">

Task to continue: <the task in progress, from this conversation, or "none">
Plan file: <path, if one applies, e.g. docs/plans/claude-agents.md>

Confirm the plugin skills loaded (they appear under their plugin names), then continue.
````

Say before the block that the session must be restarted for the updates to load.
