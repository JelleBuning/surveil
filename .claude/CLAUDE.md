# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MarYor.AudiologicX is a **modular monolith** (.NET 10 / C# 13) that collects patient data from various audiology source systems and converts it into a single, uniform domain model. It's currently a PoC: an ASP.NET Core Minimal API backend plus a Blazor WebAssembly frontend, backed by SQL Server.

## Workflow

Use /implement-issue <number> to pick up a GitHub issue: plan → wait for approval → implement + tests → report done. Opening a PR is a separate, explicit step the user decides on — never done automatically.

### Automated dev-cycle

`/dev-cycle` picks the oldest `status:refined` issue and drives it through plan → implement → test → review via the subagents in `.claude/agents/`, stopping for approval before code and before any PR; it never merges. Run it on a loop: `/loop 5m /dev-cycle`.

`status:refined` is applied **by hand**; /dev-cycle moves it to `status:in-progress` → `status:in-review`. An aborted run leaves it on `status:in-progress` so the loop won't re-pick it.

### Branch naming

Every branch name must match `(main|master|(features|bugs|hotfix)\/[0-9]+-.+)`:
- `main` / `master` — the default branches.
- `features/<issue-number>-<slug>` — new functionality (e.g. `features/12-serilog-logging`).
- `bugs/<issue-number>-<slug>` — fixing incorrect/unwanted existing behavior (e.g. `bugs/13-hide-stacktrace`).
- `hotfix/<issue-number>-<slug>` — urgent fixes.

The `EnterWorktree` tool always prefixes branch names with `worktree-`, which breaks this convention. After creating a worktree, immediately rename the branch with `git branch -m <features|bugs|hotfix>/<issue-number>-<slug>` before making any commits.

## Commands

```bash
# Build the whole solution
dotnet build MarYor.AudiologicX.slnx

# Run the API (Minimal API host, Scalar UI at /scalar in Development)
dotnet run --project src/MarYor.AudiologicX.Api

# Run the Blazor WASM client (expects the API at https://localhost:6002, see wwwroot/appsettings.json)
dotnet run --project src/MarYor.AudiologicX.Web

# Run a single test project / single test (once a *.UnitTests project exists)
dotnet test src/MarYor.AudiologicX.Api.Core.Patients.UnitTests
dotnet test --filter "FullyQualifiedName~CreatePatientHandlerTests.Handle__WhenCommandIsValid__ShouldPersistPatient"
```

There is no test project yet — `tests/` is an empty solution folder, and no test framework package is pinned in `Directory.Packages.props`. When adding the first tests, create a `{Module}.UnitTests` project referencing `Api.Core.{Module}` and `Api.EntityFramework`, and wire its `.csproj` into `MarYor.AudiologicX.slnx`.

`Nullable` is enabled on every project. `TreatWarningsAsErrors` is currently only set on `Api.Core.Patients` — match that pattern (nullable-clean, warning-free) when adding new module projects even where the flag isn't yet set.

Package versions are centrally managed in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) — never add a `Version` attribute to a `PackageReference` in a `.csproj`; add/bump the version in `Directory.Packages.props` instead.

## Architecture

**Vertical Slice Architecture with CQRS**, wrapped in a modular-monolith project layout. Each module (currently `Patients` and the cross-cutting `Auth` module) gets its own project, and modules never reference each other's projects — they only share `Api.Core` and, if they need persistence, `Api.EntityFramework`.

```
Api.Core                      ← no project deps: IEndpoint, Result<T>, PagedResult<T>, endpoint auto-discovery
Api.Core.{Module}             ← depends on: Api.Core, + Api.EntityFramework only if the module persists data   (commands/queries/handlers/validators + IEndpoint dispatchers, per use case)
Api.EntityFramework           ← depends on: EF Core only                   (AppDbContext, entities, configurations)
Api (Host)                    ← depends on: Api.Core, Api.EntityFramework, Api.Core.{Module} for every module
Web                           ← standalone Blazor WASM client, no project references to any Api.* project — talks to the Api host purely over HttpClient
```

A module's endpoints live in the same project as its command/query handlers — there is no separate `.Endpoints` project per module. The Host (`src/MarYor.AudiologicX.Api`) references each `Api.Core.{Module}` project directly so its `IEndpoint` implementations get loaded; the Host itself contains no business logic, just `Program.cs` composition. This applies even to infrastructure/cross-cutting concerns with no CQRS use case behind them — e.g. `Api.Core.Auth`'s `LoginEndpoint`/`LogoutEndpoint` just call `Results.Challenge`/`Results.SignOut` directly (no command/handler needed), but they are still `IEndpoint` implementations in their own module project, never `app.MapGet`/`app.MapPost` calls inline in `Program.cs`.

`Web` intentionally does not share a contracts project with the API: it redeclares its own `Result`/`PagedResult`/DTO types under `Web/Models/`. Keep them in sync by hand when API contracts change — there is no shared assembly to update instead.

### Request flow

1. `IEndpoint` implementation (in `{Module}/{UseCase}/v{n}/`) receives the HTTP request and sends a command/query via `IMediator` — no logic here.
2. `Mediator` (source-generated package by Martin Othamar, **not MediatR**) dispatches to the handler. Handlers are auto-registered by the source generator — never register them manually.
3. FluentValidation validators are colocated per command/query (e.g. `CreatePatientValidator`) — handlers can assume input is already valid. Note: no pipeline behavior is currently registered in `Program.cs`, so double-check a validator actually runs before relying on it.
4. The handler (in `{Module}/{UseCase}/v{n}/`) talks to `AppDbContext` directly and returns `Result<T>` — never throws for business-rule failures.
5. The endpoint maps `Result<T>` to an HTTP response (`Results.Ok`, `Results.BadRequest`, etc.).

### Endpoint auto-discovery & API versioning

`Api.Core/DependencyInjection.cs` (`ApplicationBuilderExtensions`) reflects over loaded assemblies for `IEndpoint` implementations, registers them as singletons, and groups them into versioned route groups (`/api/v{apiVersion}`) based on each endpoint's `Version` property (via Asp.Versioning). In Development, it also maps OpenAPI docs and a Scalar UI per API version. Endpoints are never mapped by hand in `Program.cs` — just `app.MapEndpoints()`.

### One module, one vertical slice per use case

Inside `Api.Core.Patients`, each use case (`Create`, `Update`, `Delete`, `GetById`, `GetAll`) is its own folder containing exactly that use case's command/query, handler, validator, DTO, and endpoint — never combine two use cases in one folder. The endpoint lives one level deeper, under a `v{n}/` subfolder (e.g. `Create/v1/CreatePatientEndpoint.cs`, namespace `MarYor.AudiologicX.Api.Core.Patients.Create.v1`), so a use case can gain a `v2/` endpoint later without moving the command/handler/validator it still shares.

### EF Core conventions

Entities are `sealed class` with an `IEntityTypeConfiguration<T>` (Fluent API, no data-annotation attributes). `Patient` follows a soft-delete pattern (`Deleted` bool + global `HasQueryFilter`) and hardcoded audit columns (`CreateDate/CreateUserId/ModifyDate/ModifyUserId`) — there is no real auth/user context wired up yet, so `UserId` is currently hardcoded to `1`. Follow the same soft-delete + audit-column shape for new entities until real auth lands.

### Domain language — use these terms verbatim in code, comments, and tests

- **Patient** — the individual receiving audiological care
- **SourceSystem** — an external system delivering patient data
- **Intake** — a raw data payload received from a SourceSystem
- **PatientRecord** — the normalized, uniform representation of a patient in the platform
- **Mapping** — the transformation logic from a SourceSystem format to the domain model
- **Audiogram** — a structured representation of a patient's hearing test result

### Key conventions

- `sealed` on every class not designed for inheritance; `record` for commands/queries/DTOs.
- Mediator handlers return `ValueTask<T>`; all async methods take a `CancellationToken`.
- No controllers (Minimal API + `IEndpoint` only), no MediatR, no DataAnnotations (FluentValidation only), no manual Mediator/endpoint registration.
- Never add `app.MapGet`/`app.MapPost`/etc. directly in `Program.cs`. Every HTTP endpoint — including auth/infra endpoints with no CQRS command behind them — is an `IEndpoint` implementation living in its own module project (e.g. `Api.Core.Auth`), picked up by the same auto-discovery as `Api.Core.Patients`. `Program.cs` only does service/middleware composition and `app.MapEndpoints()`.
- Prefer `var` by default; no suppressing nullable warnings.
- In the Web project's HTTP layer (`MarYor.AudiologicX.Web.Http`), each REST client's interface sits directly next to its implementation under `RestClients/{Resource}/` (e.g. `RestClients/Patient/IPatientRestClient.cs` beside `PatientRestClient.cs`) — no separate `Interfaces` subfolder.
- Never add comments. Code must be self-explanatory through naming and structure alone — no XML doc summaries, no inline explanations.

## Local secrets

`src/MarYor.AudiologicX.Api/appsettings.Development.json` holds a live SQL Server connection string with a plaintext password. It's covered by `.gitignore` but the whole `src/` tree is currently untracked (only `.gitignore` and `README.md` are committed) — never `git add -A`/force-add this file, and double-check `git status` before broad `git add` commands in this repo.
