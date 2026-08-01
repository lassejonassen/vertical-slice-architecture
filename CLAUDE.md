# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

.NET 10 minimal API, PostgreSQL via EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`), a small hand-rolled mediator with pipeline behaviors (`Common/Messaging`), FluentValidation, a `Result`/`Error` pattern instead of exceptions for expected failures (`Common/ResultPattern`), Serilog + OpenTelemetry, Scalar for API docs, xUnit for tests.

## Commands

```bash
dotnet test                       # runs both test projects (unit + integration) via VerticalSliceArchitecture.slnx
dotnet test --filter "FullyQualifiedName~CreateOrderHandlerTests"   # run a single test class
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"     # run a single test method

dotnet tool restore               # once per clone, before any `dotnet ef` command (pinned in dotnet-tools.json)
dotnet ef migrations add <Name> --project src/VerticalSliceArchitecture.Api --startup-project src/VerticalSliceArchitecture.Api --output-dir Common/Database/Migrations

docker compose up --build         # API + Postgres, everything in Docker (http://localhost:5149)
docker compose up postgres -d      # Postgres only, then:
dotnet run --project src/VerticalSliceArchitecture.Api
```

- Integration tests (`VerticalSliceArchitecture.Api.IntegrationTests`) spin up a real disposable Postgres via **Testcontainers** — Docker must be running.
- Unit tests (`VerticalSliceArchitecture.Api.Tests`) hit no database/Docker.
- Migrations apply automatically on startup when `ASPNETCORE_ENVIRONMENT=Development` (`Program.cs` → `MigrateDatabaseAsync`) — nothing to run by hand locally.
- Connection string: `ConnectionStrings:Database`, overridable via `Section__Key` env var convention. `appsettings.json` (non-Development) leaves it blank on purpose — supply via user-secrets or env vars, never commit it.

## Architecture

### Vertical slices, not layers

Code is organized by feature, not by technical layer. Each feature under `Features/<Area>/<Slice>/` (e.g. `Features/Products/CreateProduct/`) is self-contained: command/query, handler, optional validator, and endpoint all live together, instead of being spread across controllers/services/repositories. `Common/` holds cross-cutting infrastructure (DB, mediator, middleware, Result pattern, endpoint auto-registration); `Domain/` holds entities, value objects, strongly-typed ids, and domain events.

To add a new slice, copy the shape of `Features/Products/CreateProduct/`:
1. **Command/query** — a record implementing `IRequest<Result<TResponse>>` (or `IRequest<Result>`).
2. **Handler** — implements `IRequestHandler<TRequest, TResponse>`, talks to `AppDbContext`/domain entities directly, returns a `Result`.
3. **Validator** (optional) — `FluentValidation.AbstractValidator<TRequest>`; picked up automatically by `ValidationBehavior`.
4. **Endpoint** — implements `IEndpoint`, maps the route, calls `IMediator.Send(...)`, converts `Result` via `result.ToProblem()` on failure.

Handlers, validators, and `IEndpoint`s are all discovered via assembly scanning in `Program.cs` (`AddMediatorHandlers`, `AddEndpoints`) — **no central registration file** to keep in sync when adding a slice.

### Mediator (`Common/Messaging`)

Hand-rolled, not MediatR — `Mediator.Send`/`Publish` resolve handlers via `dynamic` dispatch against `IServiceProvider`. Because of the `dynamic` binding, test-double handlers/behaviors/events must be `public` (not `private` nested classes) or the DLR can't see them across assemblies — production handlers are always top-level `public` so this never bites there. `IPipelineBehavior<,>` implementations wrap `Send` in registration order reversed (last-registered runs innermost, closest to the handler); currently `LoggingBehavior` then `ValidationBehavior`. `Publish` fans a domain event out to all matching `INotificationHandler<>`s.

### Result pattern (`Common/ResultPattern`)

Expected failures (validation, not-found, conflict) return `Result`/`Result<TValue>` rather than throwing. `ResultExtensions.ToProblem()` maps `ErrorType` → HTTP status (`Validation`/default → 400, `NotFound` → 404, `Conflict` → 409) into a `ProblemDetails` response — endpoints should branch on `result.IsSuccess`/`IsFailure` and use this rather than re-implementing the mapping.

### Domain events

`AggregateRoot<TId>` implements a non-generic `IHasDomainEvents` marker (needed because `ChangeTracker.Entries<AggregateRoot<object>>()` can't match a concrete closed generic — C# generics aren't covariant here). `DispatchDomainEventsInterceptor`, registered on `AppDbContext` via `AddInterceptors`, dispatches pending domain events through the mediator's `Publish` around `SaveChanges`. Entity configurations (`Common/Database/Configurations/`) are applied via `ApplyConfigurationsFromAssembly` in `OnModelCreating` — a new entity needs its `IEntityTypeConfiguration<T>` there to actually take effect, and a matching value converter if it uses a strongly-typed id.

### Testing conventions

- `WebApplicationFactory<Program>` requires the `public partial class Program;` declaration at the bottom of `Program.cs` — don't remove it.
- `[assembly: InternalsVisibleTo(...)]` in `AssemblyInfo.cs` lets tests exercise `internal` types like `ValidationBehavior`/`LoggingBehavior` directly.
- Integration tests use `ApiCollection`/`ApiFactory` to share one Testcontainers Postgres instance across a test class.
