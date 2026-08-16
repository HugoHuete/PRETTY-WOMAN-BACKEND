# Repository Guidelines

## Project Structure & Module Organization

Production code is under `src/`; tests are under `tests/`.

- `PrettyWoman.Domain`: entities, enums, and core business concepts.
- `PrettyWoman.Application`: DTOs, interfaces, mappings, and services.
- `PrettyWoman.Infrastructure`: EF Core persistence, migrations, identity, and external storage implementations.
- `PrettyWoman.Api`: ASP.NET Core controllers, middleware, health checks, and application startup.
- `PrettyWoman.Workers`: background-service host.
- `tests/PrettyWoman.Application.Tests`: service tests using EF Core InMemory.
- `tests/PrettyWoman.Api.IntegrationTests`: API tests backed by PostgreSQL Testcontainers.
- `Docs/`: business rules, use cases, database notes, and frontend contracts.

## Build, Test, and Development Commands

The solution targets .NET 10:

- `make restore` — restore NuGet packages.
- `make build` — compile the complete solution.
- `make test` — run all xUnit test projects.
- `make api` — start the API on the HTTP launch profile (`http://localhost:5064`).
- `make workers` — start the background worker.
- `make migration name=AddFeature` — create an EF Core migration.
- `make migrate` — apply migrations to the configured database.
- `make pending-model-changes` — verify that the EF model and migrations agree.

Integration tests require Docker for Testcontainers.

## Coding Style & Naming Conventions

Follow existing C# conventions: four-space indentation, file-scoped namespaces, nullable reference types, and implicit usings. Use `PascalCase` for types and public members, `camelCase` for locals and parameters, `I` prefixes for interfaces, and `Async` suffixes for asynchronous methods. Preserve the `DTO` suffix convention (for example, `CreateOrderDTO`). Keep controllers thin; business rules belong in Application, while database and external-system details belong in Infrastructure. Run `dotnet format --verify-no-changes` before submitting.

## Testing Guidelines

Use xUnit `[Fact]` tests and descriptive names such as `CreateAsync_UsesProvidedPurchaseDate`. Mirror the source area under `tests/.../Services/<Feature>/`. Cover success paths, validation failures, authorization, and financial/inventory side effects. Run focused tests with `dotnet test --filter FullyQualifiedName~OrderServiceTests`, then `make test`. No coverage threshold is configured; add regression tests for every bug fix.

## Commit & Pull Request Guidelines

Recent history follows Conventional Commits: `feat:`, `fix:`, `docs:`, and `refactor:`, with optional scopes such as `fix(api):`. Keep commits focused and use an imperative summary. Pull requests should explain the behavior change, link the issue or use case, note migrations/configuration changes, and include test evidence. Include request/response examples for API contract changes and update `Docs/` when business rules change.

## Local Agent Artifacts

Files under `docs/superpowers/` and `.superpowers/` are local planning and execution artifacts. Keep them ignored and local; never add them with `git add -f`, include them in commits, or change the ignore rules to track them. If an agent skill asks to commit these artifacts, this repository rule takes precedence.

## Security & Configuration

Never commit credentials. Supply JWT, seed-admin, database, and R2 storage settings through user secrets or environment-specific configuration. Treat `appsettings.json` values as safe defaults only.
