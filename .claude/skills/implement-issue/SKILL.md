---
name: gh:implement-issue
description: Pick a GitHub issue, plan it, get approval, implement + test, then report done. Trigger for any request to pick up, work on, or implement a GitHub issue in this repo, not just the explicit /implement-issue invocation.
---

## Usage
```
/implement-issue <issue-number>
```

# Implement Issue Workflow

Follow this workflow exactly, one step at a time. Do not skip the approval step.

## Step 1 — Select an issue
If an issue number was given, use it directly.

Otherwise run `gh issue list --state open --limit 30 --json number,title,url,closedByPullRequestsReferences`
to get open issues along with any PRs already linked to them. For any issue
whose `closedByPullRequestsReferences` isn't empty, check each referenced PR
number with `gh pr view <n> --json state` and exclude the issue from the list
if at least one linked PR is still `OPEN` — it's already being worked on. If
a PR lookup fails, don't let it block the rest of the list; just skip that
one issue from the filtering step and show it as-is.

Show me the filtered list and ask which issue number to work on.

## Step 2 — Understand the issue
Run `gh issue view <number> --comments` to read the full description and discussion.
Also skim the relevant part of the codebase so the plan is grounded in how the
project actually works, not just the issue text.

## Step 3 — Write a plan
A plan is not a restatement of the issue. It should show you've actually
looked at the code. Include:

- **Root cause / current behavior** — what's actually happening today, in
  the relevant files, not just what the issue reporter observed
- **Proposed approach** — the specific change, and why this approach over
  alternatives if there's a non-obvious choice
- **Files touched** — a real list, not "relevant files"
- **Edge cases** — at least the ones a reviewer would ask about (empty
  input, concurrent access, backwards compatibility, etc. as applicable)
- **Test plan** — what new tests will be added and what they verify

Keep it short. A plan is a few bullet points per section, not an essay.
If the issue is ambiguous or under-specified, say what assumption you're
making rather than guessing silently. Do NOT write any code yet. Present the
plan and stop.

## Step 4 — Wait for approval
Wait for me to respond. I will either:
- approve as-is, or
- give more context / ask for changes to the plan

Do not start implementing until I explicitly approve. If I give feedback,
revise the plan and present it again before proceeding.

## Step 5 — Implement
Once approved:
- Use the EnterWorktree tool to create an isolated git worktree for this work,
  naming it `issue-<number>-<short-slug>` — NEVER implement directly in the current workspace.
- Implement the change
- Write or update tests covering the new behavior
- Run the project's test suite and fix any failures before moving on

## Step 6 — Show a summary before committing
Once implementation is complete and tests pass, show me a summary of what
changed (files touched, diff highlights, test results) — this gives me an
actual diff to review, beyond the bare commit/push permission prompt.

## Step 7 — Commit + push, do not open a PR
Commit with a message referencing the issue, and push the branch (`git
commit`/`git push` already require my explicit approval via settings.json
permissions). Do NOT run `gh pr create` or otherwise open a pull request —
that decision is mine. Only open a PR if I explicitly ask for one in a later
message.
