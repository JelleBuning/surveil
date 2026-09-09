---
name: gh:refine-issue
description: Read a GitHub issue plus the relevant code, then propose and apply improvements to the issue itself (clearer description, acceptance criteria, missing context) — not the implementation. Trigger for "take a look at issue X and improve it", "refine/clean up issue X", or any request to improve an issue's quality rather than implement it.
---

## Usage
```
"take a look at issue 11 and improve it"
```

# Refine a GitHub Issue

This skill improves the *issue itself* — title, description, acceptance
criteria — not the code. For implementing an issue, use the
gh:implement-issue skill instead.

## Step 1 — Read the issue
Run `gh issue view <number> --comments` to get the full title, body, labels,
and discussion.

## Step 2 — Ground it in the codebase
Skim the relevant part of the codebase so you understand what the issue is
actually asking for, including anything the reporter got wrong or left out
(wrong file/behavior assumptions, missing edge cases they didn't consider).

## Step 3 — Identify what's weak
Look for:
- Vague or missing acceptance criteria
- Missing repro steps (for bugs) or missing motivation (for features)
- Ambiguous scope — could be read multiple ways
- Missing context that's visible in the code but not stated in the issue
- Stale info (references code/behavior that's since changed)

Every refined issue ends up with an `## Acceptance criteria` section — if it
has none, write one; if it has vague ones, make them checkable.

## Step 4 — Propose the rewrite
Show me the proposed new title (if it needs changing) and body, plus the
labels you'd apply (see Step 5) and a short note on what changed and why. Do
not touch the issue yet.

## Step 5 — Apply on approval
Once I approve, apply the rewrite with `gh issue edit <number> --title "..."
--body "..."` (only pass `--title` if it changed), and in the same edit apply
exactly one label from each dimension, chosen from the issue's content:

- `type:feature` / `type:bug` / `type:chore`
- `area:*` — the module the work lands in (`area:patients`, `area:auth`,
  `area:infra`; run `gh label list` since new modules get new `area:*` labels)
- `size:s` / `size:m` / `size:l` — `s` = a single slice or file, `m` = a few
  files across one module, `l` = multiple modules or cross-cutting

Never touch `status:*` labels — `status:refined` is applied by hand by me when
an issue is ready for `/dev-cycle`, and the pipeline owns the rest. Don't
add/remove any other labels or close/reopen the issue unless I've asked for
that separately.
