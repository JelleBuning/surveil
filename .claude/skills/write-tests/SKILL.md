---
name: write-tests
description: Write or update tests for the currently changed code, following this repo's test project conventions. Trigger for "create tests for my current changes", "add tests for this", or similar requests to add test coverage.
---

## Usage
```
"create tests for my current changes"
```

# Write Tests For Current Changes

## Step 1 — Find what changed
Run `git status` and `git diff` (staged + unstaged) to see exactly what
changed. Test the behavior that changed, not the whole file.

## Step 2 — Find or create the test project
Tests live in a `{Module}.UnitTests` project referencing `Api.Core.{Module}`
and `Api.EntityFramework`. If one doesn't exist yet for the module you're
touching, create it and wire it into `MarYor.AudiologicX.slnx` — there's no
test framework package pinned yet in `Directory.Packages.props`, so add one
there (central package management: never a `Version` attribute on the
`PackageReference` itself).

## Step 3 — Write the tests
- Every behavioral change gets a test. For a bug fix, the test must fail on
  the old code and pass on the new code — verify this if you can (e.g. stash
  the fix, run the test, confirm it fails, then pop the stash).
- Match existing test naming/structure if other test projects already exist
  in the repo; don't invent a new style for one change.
- `Nullable` is enabled repo-wide; keep tests warning-free.

## Step 4 — Run them
`dotnet test <project>` (or `--filter` for a single test) and fix any
failures before reporting done.
