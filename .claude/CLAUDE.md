# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Surveil is a lightweight Windows desktop client (.NET 10 / C# 13, WinUI 3) that connects to a
UniFi Protect controller to receive real-time doorbell notifications and view live/RTSPS camera
feeds, with an eye toward supporting other camera/NVR providers later.

## Workflow

Use /implement-issue <number> to pick up a GitHub issue: plan → wait for approval → implement + tests → report done. Opening a PR is a separate, explicit step the user decides on — never done automatically.

### Automated dev-cycle

`/dev-cycle` picks the oldest `refined`-labeled issue and drives it through plan → implement → test → review via the subagents in `.claude/agents/`, stopping for approval before code and before any PR; it never merges. Run it on a loop: `/loop 5m /dev-cycle`.

`refined` is applied **by hand**; /dev-cycle moves it to `in-progress` → `in-review`. An aborted run leaves it on `in-progress` so the loop won't re-pick it.

This repo's actual labels are: `bug`, `documentation`, `enhancement`, `duplicate`, `good first issue`, `help wanted`, `invalid`, `question`, `wontfix`, `dependencies`, `github_actions`, plus bare `refined`/`in-progress`/`in-review` (no `status:`/`type:`/`area:`/`size:` prefixes). Don't invent a label scheme that doesn't exist here — check `gh label list` before applying labels to an issue.

### Branch naming

Branches follow `(main|(features|bugs|hotfix)\/[0-9]+-.+)` when tied to an issue:
- `main` — the default branch.
- `features/<issue-number>-<slug>` — new functionality.
- `bugs/<issue-number>-<slug>` — fixing incorrect/unwanted existing behavior.
- `hotfix/<issue-number>-<slug>` — urgent fixes.

Some existing branches predate this convention and don't carry an issue number (e.g.
`features/improve-workflow`) — that's historical, not something to imitate for new work tied to
an issue.

The `EnterWorktree` tool always prefixes branch names with `worktree-`, which breaks this convention. After creating a worktree, immediately rename the branch with `git branch -m <features|bugs|hotfix>/<issue-number>-<slug>` before making any commits.

## Commands

```bash
# Build the whole solution
dotnet build Surveil.slnx

# Run the WinUI app
dotnet run --project src/Surveil

# Run the tests
dotnet test tests/Surveil.Core.Tests

# Run a single test
dotnet test --filter "FullyQualifiedName~ProtectEventStreamTests.SomeMethod"
```

Tests use **MSTest** + **Moq** (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`, `MSTest.TestAdapter`, `Moq`), not xUnit/NUnit. Each `Surveil.Core` internal type under test is exposed to the test project via `InternalsVisibleTo` in `Surveil.Core.csproj` (also `InternalsVisibleTo` to `DynamicProxyGenAssembly2` for Moq's dynamic proxies) rather than making everything public.

Package versions are centrally managed in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) — never add a `Version` attribute to a `PackageReference` in a `.csproj`; add/bump the version in `Directory.Packages.props` instead.

`Nullable` is enabled and `ImplicitUsings` is **disabled** (explicit `using`s only) on every project — match this in new projects.

## Architecture

**Layered, provider-oriented modular structure.** Currently two projects, with a provider module
(`Surveil.Unifi`) planned:

```
Surveil.Core     ← Domain/Application/Infrastructure/Services layers (see below); provider-agnostic ports live here
Surveil          ← WinUI 3 host: Views (XAML), ViewModels, composition (App.xaml.cs); depends on Surveil.Core
Surveil.Unifi    ← (planned) UniFi Protect-specific provider implementation, depends on Surveil.Core's ports
```

The intent going forward: `Surveil.Core` stays provider-agnostic (ports + domain + generic
services), and each concrete camera/NVR provider gets its own project (starting with
`Surveil.Unifi`) that implements those ports. `Surveil` (the app) references `Surveil.Core` and
every provider module directly. If a provider module later grows too large for one project (e.g.
it needs its own internal layering), split out a `{Provider}.Core` the same way `Surveil.Core`
was split from `Surveil` — don't do that split preemptively.

### `Surveil.Core` internal layering

```
Domain/          ← plain entities, no dependencies on anything else (Cameras/, Events/)
Application/     ← the use-case layer: Ports/ (interfaces like ICameraProvider, IProtectEventStream —
                   what the app needs, not how), plus Options/ and Settings/ (config shapes/records)
Infrastructure/  ← concrete implementations of Application/Ports/ interfaces (Http/, WebSocket/,
                   Settings/) — e.g. UnifiProtectApiClient implements ICameraProvider over HTTP
Services/        ← app-level services with no port/interface split (VLC playback, notifications,
                   snapshotting) — used directly by the app, not swapped via DI abstraction
```

Dependency rule: `Infrastructure` depends on and implements `Application/Ports` interfaces, never
the other way around. A type belongs in `Application/Options` or `Application/Settings` if it's a
config *contract* the application layer depends on; it belongs in `Infrastructure` if it's a
concrete, swappable implementation detail.

`ReloadableCameraProvider` (`Infrastructure/Http`) is the one exception worth knowing: it wraps
whatever `ICameraProvider` matches the current `VideoProviderType` setting and hot-swaps it when
settings change, so callers never need the app restarted after a settings save.

### `Surveil` (the WinUI host)

`Views/` (XAML + code-behind), `ViewModels/` (CommunityToolkit.Mvvm), and `App.xaml.cs` for
composition — DI registration, settings loading, and host startup. `App.xaml.cs` is the only
place services get registered; there's no separate composition-root project.

## Key conventions

- `sealed` on every class not designed for inheritance; `record` for domain entities, settings, and options types (e.g. `Camera`, `RtspsStream`, `ProtectEvent`, `AppSettings`, `VideoProviderOption`).
- Async methods take a `CancellationToken` where cancellation is meaningful (network/IO calls).
- Interfaces for anything DI needs to swap or mock (`ICameraProvider`, `IProtectEventStream`, `IAppSettingsRepository`, `ISettingsChangeNotifier`, `IDesktopNotifier`, `IVlcPlayerFactory`) — colocated with their implementation, not in a separate `Interfaces/` folder except the existing `Services/Interfaces/` (don't add new interfaces there; colocate with the concrete class instead, matching the newer `Application/Ports` pattern).
- Prefer `var` by default; no suppressing nullable warnings.
- `ImplicitUsings` disabled — write explicit `using` directives.

## Local secrets / hooks

A `PreToolUse` hook (`.claude/hooks/block-secrets.py`) and a `Stop` hook
(`.claude/hooks/verify-no-secrets.sh`) already guard against committing secrets in this repo —
don't bypass or disable them. `git commit` and `git push` are permission-gated (`ask`) in
`.claude/settings.json`, so they'll always prompt rather than run silently.
