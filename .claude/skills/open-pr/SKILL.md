---
name: gh:open-pr
description: Open a pull request for the current branch with a description matching this repo's conventions. Trigger for "open a pull request", "create a PR", "let's PR this", or similar explicit requests to open a PR.
---

## Usage
```
"go ahead and open a pull request"
```

# Open a Pull Request

## Step 1 — Check state
Run `git status`, `git diff HEAD`, and check whether the branch already
tracks a remote. Run `git log --oneline` and `git diff <base>...HEAD` to see
every commit that will be in the PR, not just the latest one.

## Step 2 — Push if needed
Push the branch (with `-u` if it doesn't yet track a remote) before creating
the PR.

## Step 3 — Write the description
Structure:

```
## What
One or two sentences on the change.

## Why
Link the issue. One or two sentences on the motivation if it's not obvious
from the issue title.

## How
Bullet points on the approach, especially anything non-obvious.

## Testing
What tests were added/updated, and how it was verified manually if relevant.
```

Reference the issue with `Fixes #<number>` (or `Refs #<number>` if it doesn't
fully close it) so GitHub links them automatically.

## Step 4 — Create it
Use `gh pr create` with the title and body above. Return the PR URL.

Note: this skill only fires once I've explicitly asked to open a PR — never
run `gh pr create` proactively as part of another workflow (e.g. right after
implementing an issue). Pushing additional commits to an already-open PR
doesn't need this skill again — only the initial `gh pr create` does.
