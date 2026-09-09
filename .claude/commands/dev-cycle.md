---
description: Pick up the oldest refined issue and drive it through plan -> implement -> test -> review, with approval gates before implementation and before any PR.
argument-hint: "[issue-number]"
---

# Dev Cycle

Drive **one** issue from `refined` to a reviewed, ready-to-PR branch,
using the `planner`, `developer`, `tester`, and `reviewer` subagents.

One invocation handles at most one issue and does not resume a previous
invocation's work. Designed to be run on a loop for live visibility:

```
/loop 5m /dev-cycle
```

If `$1` is given, use that issue number instead of picking one.

## Step 1 — Pick the issue

`gh issue list --state open --label "refined" --json number,title,createdAt --limit 50`

Take the single **oldest by `createdAt`**; break ties by lowest issue number.

If nothing is labeled `refined`, say so in one line and stop — that's a
normal outcome, not an error.

## Step 2 — Claim it

Move the issue to in-progress so no later invocation picks it up again:

`gh issue edit <n> --remove-label "refined" --add-label "in-progress"`

## Step 3 — Plan

Dispatch the `planner` subagent with the issue number. It posts the plan as an
issue comment and returns it.

Print the plan in full, then **stop and ask** (AskUserQuestion):

- **Approve** — continue to implementation
- **Revise** — take the user's feedback, re-dispatch `planner` with it, print
  the new plan, ask again
- **Abort** — stop here

On abort, leave the issue on `in-progress`, say that it stays there so
the loop won't re-pick it, and tell the user to relabel it `refined`
themselves if they want it queued again.

No code is written before an explicit Approve.

## Step 4 — Isolate the work

Only after approval:

1. Create the worktree with `EnterWorktree`, named `issue-<n>-<slug>`.
2. Immediately rename the branch to match the repo convention in
   `.claude/CLAUDE.md`, picking the prefix from the issue's `type:*` label —
   `type:feature` → `features/`, `type:bug` → `bugs/`, `type:chore` →
   `features/` unless the issue says it's urgent (`hotfix/`):

   `git branch -m <prefix>/<n>-<slug>`

Never implement in the main checkout.

## Step 5 — Implement

Dispatch `developer` with the issue number, the approved plan, and the worktree
path. It implements and builds, but writes no tests.

## Step 6 — Test

Dispatch `tester`. It adds/updates tests and runs the suite.

If the suite is still red, **ask the user how to proceed** (AskUserQuestion) —
never retry silently and never move on with a red suite:

- **Retry** — re-dispatch `tester` with the user's guidance
- **I'll take over** — stop and hand back, leaving the worktree as-is
- **Abort** — stop, per the abort behavior in Step 3

## Step 7 — Review

Dispatch `reviewer`. It returns `VERDICT: PASS` or `VERDICT: FAIL` with blocking
items.

On `FAIL`, print the blocking list and **ask the user how to proceed**:

- **Fix** — re-dispatch `developer` (then `tester`, then `reviewer` again) with
  the reviewer's blocking items
- **Accept anyway** — continue to Step 8 with the findings recorded in the
  summary
- **I'll take over** / **Abort** — as in Step 6

Same rule as Step 6: never loop on this unattended.

## Step 8 — Mark in review

`gh issue edit <n> --remove-label "in-progress" --add-label "in-review"`

## Step 9 — Summarize, then ask about the PR

Show the user: files changed (`git status`, `git diff --stat`), diff highlights,
the final test output, and the reviewer's verdict.

Then **stop and ask** (AskUserQuestion): open a PR?

- **Yes** — follow the `gh:open-pr` skill (push the branch, then `gh pr create`
  with its description structure, referencing the issue with `Fixes #<n>`).
  Return the PR URL.
- **No** — stop. The branch and worktree stay as they are for the user to
  continue with.

Never run `gh pr create` without an explicit Yes in this run, and **never merge
a PR** — merging is always manual.

## Notes

- Commits are the user's call as well: `git commit` / `git push` are permission-
  gated in `.claude/settings.json`, so they surface as prompts.
- `refined` is applied by the user by hand — `/dev-cycle` and the
  `gh:refine-issue` skill never set it.
- Worktrees are left on disk after the run; clean them up with `ExitWorktree`
  or `git worktree remove` when the branch is done.
