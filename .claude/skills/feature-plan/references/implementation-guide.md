# Implementation guide and handoff check

Read this in Step 13. It is not optional: a plan with no implementation guide is unfinished.

Once the commit plan is confirmed, make the plan usable by an agent in a fresh session.

**1. Consistency pass.** Fix problems in the plan itself before writing the guide:
- List every rename and every decision that changed after a draft existed. Grep the plan for each old name and fix the code and prose. Reconciliation notes may stay only for conflicts too big to rewrite, and each says which commit applies it.
- Check the names the drafts assume against the repo, with one grep for each riskiest one: helpers, extension methods, namespaces, existing types. Agents guess these. (A guessed `Ulid` user id turned out to be right in one place and wrong in another, and only a grep showed which.)
- Check each "not verified" item an agent listed is in the phase's notes, and say which commit will surface it.
- Check the plan has no pointer to a temp file, an agent id, or the chat.
- Check "Decisions locked in during scoping" covers every answer the user gave and every default taken.
- Check every code-draft section is used by some commit, and every commit has code or text to work from.

**2. Write `## Implementing this plan`.** Replace the Step 6 placeholder. It sits right after the intro, before the decisions. It holds:
- **Read first**, in order: that section, the decisions and the confirmed UI flow, the reconciliation or phase notes, the commit plan, then only the sections the commit needs. Say plainly: do not read the whole file, and give its length.
- **Precedence** when two parts disagree: decisions, notes, code drafts, then the source ticket and docs. The repo wins over the plan on a name that does not match; fix the plan when you find one.
- **State of the drafts:** whether the code was compiled, what is assumed, and what cannot build until a named commit lands.
- **Per-commit procedure:** read the entry and its sections; hand it to the agent it names, passing the plan path and the entry; run the build and the tests for that area (with the actual commands); fix failures; update the plan if the code drifted; stage files by name, commit with the subject and the attribution trailer; tick the box.
- **Do not** list, from the decisions (things the user ruled out, such as a flag, a deferred pattern, a banned word).
- **Before specific commits:** data checks, contract confirmations, secrets or licenses needed.
- **Commit to section map:** a table from each commit number to the sections it needs, using heading names and layer labels, not line numbers (lines drift).
- **Mockups:** where they are, which one is chosen, which commits use them.
- **Progress:** a checkbox list that repeats each commit subject, so a fresh session resumes at the first unchecked box.

**3. Tell the user how to start a fresh session.** Give a short paste-ready prompt, for example: "Implement `docs/plans/{feature-name}.md`. Start with its 'Implementing this plan' section. Do the next unchecked commit in the progress list: hand it to the agent the commit names, run the build and tests, update the plan if the code drifted, commit by file name, tick the box, then continue with the next commit. Ask me before anything the plan marks as a decision I still owe."

**4.** Report the plan is complete: **"The plan is ready for a fresh session. The full plan is in `docs/plans/{feature-name}.md`. The prompt above starts the work. I'll commit each step as it is done."**
