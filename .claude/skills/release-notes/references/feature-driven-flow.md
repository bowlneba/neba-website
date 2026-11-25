# Feature-driven flow (major and minor releases)

Read this when the new version raises the major or minor number. The standard flow in `SKILL.md` covers patches.

## 3a. Infer the new features (major/minor only)

Do not stop at PR titles. A feature is something a member, bowler, or admin can now do that they could not do on the last release. Work it out from the evidence:

- Read each PR body for its stated purpose.
- Diff the user-facing surface against the previous tag: new or changed routable pages (`git diff <previous-release-tag>..<target> --stat -- 'src/Neba.Website*/**/*.razor'`), new endpoints, and new help docs under `docs/help/`.
- Group PRs that build one capability (an API PR, a UI PR, and a fix-up PR are one feature).
- Note who benefits: public visitors, members, bowlers, or admins and staff.

Produce a candidate list: a short name, one line on what the user can do, the audience, and the PR numbers. Mark each as **headline** (worth announcing to members), **smaller improvement**, or **under the hood**. Flag anything you are unsure about, such as a feature that looks unfinished, admin-only, or behind a flag.

## 3b. Talk it through with the user (major/minor only)

Do not draft yet. Show the candidate list and ask the user, in a short back and forth:

- Which items are headlines, and which should drop to smaller improvements or out of the notes?
- Is the name and one-line description right in the words members use?
- Did you miss a feature, or group two that should stand apart?
- Is anything not ready to announce?

Revise the list from their answers and show it again. Repeat until they say it is right. Then draft. Tell them they can keep editing after the draft too.

## 4a. Feature-driven structure (major/minor only)

Use the agreed list from step 3b. The same rules as above apply (no H1, no intro sentence, PR numbers on every section, no deferred-items section, same compare link) except as changed here.

```markdown
### <Headline feature> (#<pr>[, #<pr>...])

<Who it is for, then what they can do now, in one to three sentences. Use the words a member would use.>

- <Concrete thing they can do>
- <Concrete thing they can do>

### <Headline feature 2> (#<pr>)

...

### Also in this release

- **<Smaller improvement or fix>** (#<pr>) — <one line>

### Under the hood

- **<Item>** (#<pr>) — <what it does and why it matters, kept brief>

**Full Changelog**: https://github.com/<owner>/<repo>/compare/<previous-release-tag>...<new-version>
```

Rules for this structure:
- Headline features come first, in the order the user chose. Each gets its own `###` section, even if it is a single PR.
- Write what the user can do ("Check whether a team is eligible before you enter"), not what the system does ("Added an eligibility evaluator").
- Name the audience when it is not everyone (members, bowlers, tournament staff, admins).
- Keep each headline short enough to lift into an email or post with no edits.
- **Also in this release** holds smaller improvements and bug fixes in the standard bullet format. Omit the section if empty. **Under the hood** works as in the standard structure.
- Do not mention a feature the user dropped or marked not ready.

After drafting, show the user the headline sections and ask for edits before you finish step 6.
