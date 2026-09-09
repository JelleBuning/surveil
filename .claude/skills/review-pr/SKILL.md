---
name: gh:review-pr
description: Review an open GitHub pull request against this repo's standards. Trigger for "review PR X", "take a look at this PR", or similar requests to review someone's pull request.
---

## Usage
```
"review PR 22"
```

# Review a Pull Request

Prioritize in this order:
1. **Correctness** — does it do what it claims, including edge cases
2. **Test quality** — do the tests actually exercise the change, or just
   assert trivial things
3. **Consistency** — does it match existing conventions in the codebase
4. **Scope** — is it doing more than the issue asked for; flag scope creep

## Steps
1. `gh pr view <number> --comments` and `gh pr diff <number>` to read the
   description and full diff.
2. If the PR references an issue, `gh issue view <number>` to check the PR
   actually satisfies it.
3. Read the changed files in context, not just the diff hunks, when the
   change touches logic you can't fully judge from the diff alone.

## Giving feedback
Give feedback as specific, actionable comments tied to lines/files, not
general impressions. Distinguish "this must change before merge" from
"nit / optional" so the author can triage quickly. Only submit via
`gh pr review <number> --comment` / `--request-changes` / `--approve` once
I've confirmed the feedback — don't submit a review that requests changes or
blocks the PR without checking with me first.
