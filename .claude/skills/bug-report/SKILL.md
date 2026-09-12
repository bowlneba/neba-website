---
name: bug-report
description: After fixing a bug in this session, write two paste-ready markdown files under docs/bug-reports/<slug>/ — a QA-style issue description (user/product-facing, zero implementation detail) and a closing comment (technical root cause, fix, and rationale). Checks the repo's GitHub Issue Type field and label set to recommend how to classify each. Usage: /bug-report [slug]
---

Produce the two documents that bookend a GitHub issue for a bug just fixed in this conversation: the opening description (as a QA reporter would file it) and the closing comment (the technical postmortem). This skill never calls `gh issue create`, `gh issue comment`, or any command that sets issue type/labels — the user copy-pastes both files by hand and classifies the issue themselves. It only reads GitHub issue-type/label data, and only writes local files.

## Arguments

- **slug** (optional) — kebab-case short name for the bug, used as the folder name `docs/bug-reports/<slug>/`. If omitted, derive one from the bug's subject (e.g. from the branch name or a short paraphrase of the fix) and confirm it with the user before writing anything.

## When to run

At the end of fixing a bug in the current conversation, after the fix is made (and usually tested/committed). This skill relies on the conversation's own memory of the original symptom — it does not go looking for a bug to document.

## Step 1 — Establish the source of truth for the fix

Identify the actual code change so Step 3 and the label suggestions are grounded in fact, not paraphrase:

- If a commit was made this session: `git show <sha> --stat` and `git diff <sha>~1 <sha>` (or `git diff <base-branch>...HEAD` if the fix spans multiple commits).
- If nothing is committed yet: `git diff` against the working tree.
- Note every file touched — this drives the label suggestions in Step 4.

## Step 2 — Write the bug report (`docs/bug-reports/<slug>/issue.md`)

This is the opening issue description, written the way a QA reporter or end user would file it. **Zero implementation detail** — no class names, no interceptors, no DbContext/pooling/EF/library names, no design patterns, no file paths. Describe only what was observed from outside the system: what someone did, what should have happened, what happened instead.

Pull the actual symptom from the conversation — what was the user looking at, what action triggered it, what did they expect vs. see. If the conversation doesn't clearly establish concrete repro steps, ask the user to confirm/fill them in rather than inventing plausible-sounding ones.

**Scope this to the originally reported issue only, even when resolving it turned up other bugs.** Investigating and fixing one reported bug often surfaces related-but-distinct bugs along the way (same class of defect in other files, something only found once new test coverage existed, etc.). `issue.md` always stays bound to what was actually reported — never expand it to cover everything that got fixed in the session. The broader story (everything found and fixed during the investigation) belongs in the closing comment (Step 3), not here.

Structure:

```md
# {Short, plain-language title}

## Summary
{1-3 sentences, plain language}

## Steps to Reproduce
1. ...
2. ...
3. ...

## Expected Result
{what should have happened}

## Actual Result
{what actually happened}

## Additional Notes
{optional — frequency (every time vs. intermittent), any known workaround, who/what surfaced it}
```

Frame everything around the feature or screen the user actually interacts with (e.g. "the audit log," "a bowler's record," "the admin tool") — never the internal system that implements it. If the bug was found through investigation rather than a direct user report, still write it as the externally observable symptom, as if a QA reporter had seen it.

## Step 3 — Write the closing comment (`docs/bug-reports/<slug>/closing-comment.md`)

This file *is* the literal text that gets pasted as the GitHub issue's closing comment — write it as a complete, self-contained explanation for someone reading the issue later, not as a chat recap. This is the one place implementation detail belongs.

Unlike `issue.md`, this document covers the full scope of what was actually done to resolve the report — including other bugs of the same or a related kind that were found and fixed along the way (e.g. the same defect pattern discovered in other files, or an unrelated bug that new test coverage happened to surface). Note clearly which parts address the original report and which are additional fixes found during the investigation, so a reader isn't confused about why the diff is bigger than the reported symptom.

Structure:

```md
## Root Cause
{what was actually wrong, technically — the real mechanism}

## Fix
{what changed — name the actual files/types, e.g. "Added `PooledAuditSaveChangesInterceptor` (`src/.../Foo.cs`), registered in place of ..."}

## Why This Approach
{rationale — alternatives considered and why this one was chosen, if there were real alternatives}

## Verification
{tests added/updated, what was run to confirm the fix, what still passed}
```

Do not put label suggestions inside this file — it must paste cleanly into GitHub as-is with no extra content to strip out first.

## Step 4 — Suggest labels (report in chat, not in either file)

The opening issue's "bug" classification belongs on GitHub's native Issue Type field (Task/Bug/Feature), not a label — check once per repo with the GraphQL query below, since not every repo has issue types enabled:

```
gh api graphql -f query='
query($owner:String!, $repo:String!) {
  repository(owner:$owner, name:$repo) {
    issueTypes(first: 20) { nodes { name isEnabled } }
  }
}' -f owner=$(gh repo view --json owner -q .owner.login) -f repo=$(gh repo view --json name -q .name)
```

- **If a "Bug" issue type exists and is enabled**: that's how the opening issue gets classified — tell the user to set Issue Type to "Bug" when filing it, and don't suggest a `bug` label for it.
- **If no issue types are configured (or none named "Bug")**: fall back to suggesting the repo's `bug` label for the opening issue, if one exists.

Then, for both documents' remaining (non-bug) classification:

1. Run `gh label list --limit 100` to get this repo's actual current label set. Never guess at label names.
2. **Closing comment**: based on the files touched in Step 1, map to the closest existing label(s) from Step 4.1 (e.g. a change under `src/Neba.Api/Database/**` or `src/Neba.Api/Auditing/**` might match `database` or `api`; a UI change might match `website`/`management`/`mobile`/`desktop`). Only include a label if it's a genuinely good fit — zero extra labels is a fine outcome if nothing fits well.
3. If nothing existing fits but a label clearly *should* exist for this kind of change, do not create it yourself — tell the user what you'd propose (name + short rationale) and let them decide whether to actually create it in GitHub.
4. Report the suggested classification/label(s) directly in chat, separately from the two files — never write label suggestions into `issue.md` or `closing-comment.md`.

## Step 5 — Report

Tell the user:

- Where both files were written (`docs/bug-reports/<slug>/issue.md`, `docs/bug-reports/<slug>/closing-comment.md`).
- How to classify the opening issue (Issue Type "Bug" if enabled, otherwise the `bug` label) and the suggested label(s) for the closing comment, including any proposed-new-label rationale from Step 4.3.
- A reminder that these are meant to be copy-pasted by hand (`issue.md` → the issue description, `closing-comment.md` → the comment that closes it) — this skill never creates or comments on the GitHub issue itself.

## Rules

- Never blend the two documents' concerns: `issue.md` must not mention the fix or any internal mechanism; `closing-comment.md` should read as a technical postmortem, not a repro guide (a brief nod to the symptom for context is fine, but don't re-explain reproduction steps there).
- Never fabricate reproduction steps, expected/actual results, or timelines the conversation didn't actually establish — ask rather than guess.
- Never call `gh issue create`, `gh issue comment`, `gh label create`, or any other mutating GitHub command — this skill only ever reads issue types/labels (`gh api graphql` for issue types, `gh label list` for labels) and writes local files.
- Never invent a label name that doesn't exist in the repo's real label list — if nothing fits, say so and propose rather than assume.
