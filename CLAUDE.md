# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

.NET 10 minimal API split across five projects (`src/`), PostgreSQL or SQL Server via EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL` / `Microsoft.EntityFrameworkCore.SqlServer`, provider chosen at runtime), FluentValidation, a `Result`/`Error` pattern instead of exceptions for expected failures (`SharedKernel/Results`), a hand-rolled command/query dispatcher (`Api/Infrastructure/Messaging`) — not MediatR — JWT bearer authentication against EntraId or Keycloak, ASP.NET Core rate limiting, Serilog + OpenTelemetry, Scalar for API docs, xUnit v3 for tests.

## Commands

```bash
dotnet test                       # runs all three test projects via VerticalSliceArchitecture.slnx
dotnet test --filter "FullyQualifiedName~ClientTests"              # run a single test class
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"     # run a single test method

dotnet tool restore                # once per clone, before any `dotnet ef` command (pinned in dotnet-tools.json)
dotnet ef migrations add <Name> --project src/VerticalSliceArchitecture.Persistence --startup-project src/VerticalSliceArchitecture.Api --output-dir Migrations

docker compose up --build          # full stack: Postgres + Keycloak + OTel collector + API (http://localhost:5149)
docker compose up postgres -d      # Postgres only, then:
dotnet run --project src/VerticalSliceArchitecture.Api
```

- `Api.IntegrationTests` hosts the real API in-process (`WebApplicationFactory<Program>`) against a disposable Postgres via **Testcontainers** — Docker must be running. `Domain.Tests` and `ArchitectureTests` hit no database/Docker.
- Migrations apply automatically on startup when `Persistence:MigrateOnStartup` is `true` (default in `appsettings.Development.json`, `false` otherwise) — see `Program.cs` → `ApplyMigrationsAsync()`.
- Connection string: `Persistence:ConnectionString`, overridable via the `Section__Key` env var convention (e.g. `Persistence__ConnectionString`). Base `appsettings.json` leaves it blank on purpose — supply via user-secrets or env vars, never commit it.

## Architecture

### Five projects, not one

The template used to be a single `Api` project; it is now split so the dependency direction is enforced by the compiler (and by `ArchitectureTests`), not by convention alone:

- **`VerticalSliceArchitecture.SharedKernel`** — no project references. Generic building blocks reused by every layer: `Result`/`Result<T>`/`Error`/`ErrorType` (`Results/`), `AggregateRoot<TId>`/`Entity<TId>`/`ValueObject`/`IDomainEvent`/`IStronglyTypedId<TSelf>` (`Domain/`), and `IAuditable`/`IDateTimeProvider`/`IUnitOfWork` (`Abstractions/`).
- **`VerticalSliceArchitecture.Domain`** — the aggregates (`Clients/Client.cs`, `Users/User.cs`), their value objects, domain events, error catalogs (`ClientErrors`, `UserErrors`), and repository interfaces (`IClientRepository`, `IUserRepository`). References only `SharedKernel`. Must never reference `Microsoft.EntityFrameworkCore` or `Microsoft.AspNetCore.*` — `ArchitectureTests` fails the build if it does.
- **`VerticalSliceArchitecture.Persistence`** — `ApplicationDbContext`, EF Core entity configurations, strongly-typed-id value converters, interceptors, and the repository implementations. References `Domain` + `SharedKernel`.
- **`VerticalSliceArchitecture.Integrations`** — reserved for future third-party/external-system adapters (email, message bus, SAP, ...). Currently an empty scaffold, not referenced by anything.
- **`VerticalSliceArchitecture.Api`** — the composition root (`Program.cs`) and every vertical slice (`Features/`), plus cross-cutting web infrastructure (`Infrastructure/`: `Configuration`, `Endpoints`, `Messaging`, `Middleware`, `Observability`, `RateLimiting`, `Security`).

### Vertical slices

Each feature lives under `Features/<Area>/<Slice>/` (e.g. `Features/Clients/RegisterClient/`) and owns its request, handler, optional validator, and endpoint — nothing is spread across horizontal layers. Two calling styles are demonstrated side by side, and both are correct; pick per slice:

- **Direct handler injection** (`RegisterClient`, `DeactivateClient`) — the endpoint injects `ICommandHandler<TCommand[, TResponse]>` straight from DI. No reflection, "go to implementation" lands on the real code. Default choice for a slice with one caller.
- **`IDispatcher`** (`GetClientById`, `SearchClients`) — the endpoint resolves the request type only through `IDispatcher.SendAsync`/`QueryAsync`. Use when a handler has multiple callers, or the request type is only known at runtime.

To add a new slice:
1. **Command/query** — a record implementing `ICommand`, `ICommand<TResponse>`, or `IQuery<TResponse>` (`Api/Infrastructure/Messaging/Contracts.cs`).
2. **Handler** — `internal sealed class` implementing `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`, or `IQueryHandler<TQuery, TResponse>`; returns `Result`/`Result<TResponse>`. Naming and accessibility (`internal sealed`, name ending in `Handler`) are enforced by `ArchitectureTests`.
3. **Validator** (optional) — `FluentValidation.AbstractValidator<TRequest>`, wired in automatically via the `.WithValidation<TRequest>()` endpoint filter (`Infrastructure/Endpoints/Filters/ValidationFilter.cs`) — shape validation only; the domain's own value-object factories (`CompanyName.Create`, `EmailAddress.Create`, ...) re-validate independently and are the actual invariant.
4. **Endpoint** — `internal sealed class` implementing `IEndpoint` (`Infrastructure/Endpoints/IEndpoint.cs`), under the `Features` namespace. Maps its route, calls the handler/dispatcher, and converts the `Result` via `.ToOk()`/`.ToCreated(...)`/`.ToNoContent()` (`Infrastructure/Endpoints/ResultExtensions.cs`).

Handlers, validators, and `IEndpoint`s are all discovered via assembly scanning in `Program.cs` (`AddMessaging`, `AddDomainEventHandlers`, `AddEndpoints`, `AddValidatorsFromAssembly`) — no central registration file to keep in sync when adding a slice.

**Query slices bypass the repository and query `ApplicationDbContext.<Entity>View` directly** (e.g. `context.ClientsView`, backed by a database view over the same table) rather than loading the aggregate — `Client.Name`/`ContactEmail` are value-converted (`CompanyName`/`EmailAddress`), which EF cannot translate into a `WHERE`/`ORDER BY`/`LIKE` against the write-side `DbSet<Client>`. Writes still go through the aggregate + `IClientRepository`/`IUserRepository` so invariants hold.

### Messaging (`Api/Infrastructure/Messaging`)

Hand-rolled, not MediatR — `Dispatcher` resolves handlers via a reflection-cached per-request-type wrapper (`ConcurrentDictionary<Type, object>`, built once per request type via `Activator.CreateInstance`), not `dynamic`. `ICommand`/`ICommand<TResponse>`/`IQuery<TResponse>` are marker interfaces; `ICommandHandler<>`/`ICommandHandler<,>`/`IQueryHandler<,>` declare the `HandleAsync` contract. There are **no pipeline behaviors** — the old template's `LoggingBehavior`/`ValidationBehavior` are gone; validation is the `ValidationFilter<T>` endpoint filter, and logging inside handlers uses `[LoggerMessage]` source-generated partial methods (required — see Analyzers below). `AddDomainEventHandlers` scans for `IDomainEventHandler<>` implementations the same way `AddMessaging` scans for command/query handlers.

### Result pattern (`SharedKernel/Results`)

`Result`/`Result<TValue>`, `Error`/`ErrorType`, and `ValidationError` (aggregates several `Error`s — built via `Result.AllOrValidationError(...)`, used when a factory like `Client.Register` wants to report every problem at once instead of one at a time). `SharedKernel.Results.ResultExtensions` (`Map`/`Bind`/`BindAsync`/`Tap`/`Match`/`ToResult`) is deliberately transport-agnostic. HTTP mapping is a **separate, same-named** `VerticalSliceArchitecture.Api.Infrastructure.Endpoints.ResultExtensions` (`ToOk`/`ToCreated`/`ToNoContent`/`ToProblemDetails`, mapping `ErrorType` → status code and flattening `ValidationError.Errors` into a `ProblemDetails` extension) — watch for `using` collisions between the two when writing tests that touch both.

### Domain aggregates (`Domain/Clients`, `Domain/Users`)

`Client` and `User` both: `AggregateRoot<TId>` + `IAuditable`, private setters, a static `Result<T>` factory (`Client.Register`, `User.Provision`) instead of a public throwing constructor, and every mutator returns `Result` and raises its own domain event via `Raise(...)`. IDs are `readonly record struct XxxId(Guid Value) : IStronglyTypedId<XxxId>` using UUIDv7 (`Guid.CreateVersion7()`) for index locality. **Cross-aggregate uniqueness (e.g. a client's contact email) is not an aggregate invariant** — one instance can't see the others — so it's checked by the handler (`RegisterClientHandler.ExistsWithEmailAsync`) and actually enforced under concurrency by a unique index; adding a new aggregate that needs a similar uniqueness rule should follow the same split.

### Persistence (`Persistence/`)

`ApplicationDbContext` (schema `"app"`) supports both Postgres and SQL Server, chosen at runtime by `Persistence:Provider` (`PersistenceExtensions.ConfigureProvider`). Postgres gets `UseSnakeCaseNamingConvention()` and an `xmin`-based shadow concurrency token; SQL Server gets a `rowversion` shadow column instead (`Configurations/ConcurrencyTokenConfiguration.cs` — applies to every `AggregateRoot<>`-derived entity automatically). `AuditableEntityInterceptor` stamps `IAuditable` columns; `DomainEventDispatchInterceptor` harvests pending domain events while the change tracker still holds them (`SavingChangesAsync`) and dispatches them only after the transaction commits (`SavedChangesAsync`) — **at-most-once, in-process, no outbox** — add a transactional outbox before wiring a domain event to anything external. Adding a new strongly-typed ID requires a manual line in `Converters/StronglyTypedIdConventions.cs` (deliberate — fails loudly if forgotten rather than silently via a scan).

### Security (`Api/Infrastructure/Security`)

JWT bearer auth, provider-switchable via `Security:Provider` (`EntraId` default, or `Keycloak` for local dev — see `docker-compose.yml`). Endpoints require a **named policy** (`AuthorizationPolicies.ReadClients`/`ManageClients`), never a raw role — roles are an implementation detail of the identity provider and differ between EntraId app roles and Keycloak realm roles. `ICurrentUser` abstracts the authenticated principal; `IUserProvisioningService` just-in-time provisions a local `User` row on first authenticated request (`RequireProvisionedUser()` endpoint filter, used by `GetCurrentUser`) rather than requiring an admin-driven account creation flow.

### Testing conventions

- **`ArchitectureTests`** (NetArchTest) — layering rules (`Domain`/`SharedKernel` must not depend on `Persistence`/`Api`/`Integrations`, `Domain` must not reference EF Core or ASP.NET Core), plus handler/endpoint naming and accessibility conventions (`internal sealed`, name ending in `Handler`/`Endpoint`, endpoints under `Features`).
- **`Domain.Tests`** — pure unit tests for aggregates, value objects, and the `Result` pattern. No database, no Docker.
- **`Api.IntegrationTests`** — `ApiFactory : WebApplicationFactory<Program>` boots the real API against a disposable **Testcontainers** Postgres, one container shared per test class via `ApiCollection`/`[Collection(ApiCollection.Name)]`. The JWT bearer scheme is swapped for `TestAuthHandler`, a header-driven fake principal (`ApiFactory.CreateAuthenticatedClient(params string[] roles)`) — tests authenticate without minting real tokens. Requires Docker.
- Test method names use `Method_Scenario_Expectation` underscores; `CA1707` is suppressed for every test project via `tests/Directory.Build.props` rather than per-project — don't re-suppress it locally.
- EF-generated files under `Persistence/Migrations/` are marked `generated_code = true` via a folder-scoped `.editorconfig` — don't hand-edit them to satisfy analyzers; regenerate with `dotnet ef migrations add`/`remove` instead.
- `public partial class Program;` at the bottom of `Program.cs` is required for `WebApplicationFactory<Program>` — don't remove it.

### Analyzers

`Directory.Build.props` sets `TreatWarningsAsErrors` + `AnalysisLevel=latest-recommended` from day one across every project. Two consequences that show up often enough to call out:
- **CA1873** ("avoid potentially expensive logging") fires on any `logger.LogXxx(...)` call whose arguments aren't trivial locals/literals — the fix used throughout this codebase is `[LoggerMessage]` source-generated partial logging methods (see `ClientRegisteredLogger`, `GlobalExceptionHandler`, `UserProvisioningService`), not suppression.
- **IDE0055** (formatting) is treated as an error; when it fires, run `dotnet format` rather than hand-fixing whitespace.
- `CA1716`/`CA1711` are suppressed globally (see the comment in `Directory.Build.props`) because this codebase's `Error`/`IStronglyTypedId<T>.New()`/`IDomainEventHandler<T>` names are the established vocabulary of its Result/strongly-typed-id patterns — don't rename them to satisfy the analyzer.
