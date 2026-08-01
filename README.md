# Vertical Slice Architecture

A .NET 10 minimal API template organized around **vertical slices**: each
feature (`CreateProduct`, `CancelOrder`, ...) owns its own command/query,
handler, validator, and endpoint in one folder, instead of being spread across
horizontal layers (controllers/services/repositories).

## Stack

- **.NET 10** minimal APIs
- **PostgreSQL** via **EF Core** (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- A small hand-rolled mediator (`Common/Messaging`) with pipeline behaviors
  for logging and **FluentValidation**
- A `Result`/`Error` pattern instead of throwing for expected failures
  (`Common/ResultPattern`)
- **Serilog** + **OpenTelemetry** for logging/tracing/metrics
- **Scalar** for interactive API docs (over ASP.NET Core's built-in OpenAPI
  document generation)
- **xUnit** for tests: a plain unit test project, and an integration test
  project that runs the real app against a **Testcontainers**-managed
  Postgres

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) — for local Postgres (via Docker Compose)
  and for the integration tests (via Testcontainers)

## Getting started

Clone the repo, then pick one of two ways to run it:

### Option A — everything in Docker

```bash
docker compose up --build
```

This builds the API image and starts both the API and a Postgres instance.
The API applies EF Core migrations automatically on startup (see
[Database](#database) below) and is reachable at `http://localhost:5149`.

### Option B — API on the host, Postgres in Docker

```bash
docker compose up postgres -d
dotnet run --project src/VerticalSliceArchitecture.Api
```

The default `appsettings.Development.json` connection string already points
at the Postgres container started above (`localhost:5432`), so no further
configuration is needed. The API listens on `http://localhost:5149` (see
`Properties/launchSettings.json` for the `http`/`https` profiles).

Either way, once it's running:

- `GET /` → `"Always on"` (basic liveness check)
- `GET /scalar` → interactive API docs (Scalar UI)
- `GET /openapi/v1.json` → the raw OpenAPI document

## Configuration

The connection string lives under `ConnectionStrings:Database`:

- `appsettings.Development.json` has a real local default matching the
  `docker-compose.yml` Postgres service (db `vertical_slice_architecture`,
  user/password `postgres`).
- `appsettings.json` (used outside Development) has it blank on purpose —
  supply the real value via environment variables or
  [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
  (a `UserSecretsId` is already configured in the csproj), not by committing
  credentials.

Any setting can be overridden with an environment variable using the
`Section__Key` convention, e.g.:

```bash
ConnectionStrings__Database="Host=localhost;Port=5432;Database=vertical_slice_architecture;Username=postgres;Password=postgres"
```

## Database

Migrations live in `src/VerticalSliceArchitecture.Api/Common/Database/Migrations/`.
The API applies pending migrations automatically on startup when
`ASPNETCORE_ENVIRONMENT=Development` (see `Program.cs`); there's nothing to
run by hand for local development.

To add a new migration after changing the model, first restore the pinned
`dotnet-ef` CLI tool (once per clone — it's tracked in `dotnet-tools.json` so
everyone uses the same version instead of relying on a global install):

```bash
dotnet tool restore
```

Then, from the repo root:

```bash
dotnet ef migrations add <Name> \
  --project src/VerticalSliceArchitecture.Api \
  --startup-project src/VerticalSliceArchitecture.Api \
  --output-dir Common/Database/Migrations
```

## Tests

```bash
dotnet test
```

This runs both test projects under `tests/`:

- **`VerticalSliceArchitecture.Api.Tests`** — plain unit tests (domain logic,
  the mediator, pipeline behaviors, the `Result` pattern). No database, no
  Docker.
- **`VerticalSliceArchitecture.Api.IntegrationTests`** — spins up a real,
  disposable Postgres via Testcontainers and runs the actual API against it
  end to end (`ApiFactory : WebApplicationFactory<Program>`). Requires Docker
  to be running.

## Project structure

```text
src/VerticalSliceArchitecture.Api/
  Common/           # Cross-cutting infrastructure: DB, mediator, middleware,
                     # the Result pattern, endpoint auto-registration
  Domain/            # Entities, value objects, strongly-typed ids, domain events
  Features/
    Products/
      CreateProduct/   # Command + Handler + Validator + Endpoint + Response
      GetProductById/  # Query  + Handler +           Endpoint + Dto
      ProductsConstants.cs
    Orders/
      CreateOrder/
      CancelOrder/
      OrdersConstants.cs
```

Each slice under `Features/` is self-contained: a request (command/query), a
handler, an optional FluentValidation validator, and an `IEndpoint` that maps
the HTTP route. `IEndpoint` implementations are discovered and mapped
automatically (`Common/Endpoints/EndpointExtensions.cs`) — no central routing
file to keep in sync.

### Adding a new vertical slice

Using `Features/Products/CreateProduct/` as the template, a new slice
typically needs:

1. **Command or query** — a record implementing `IRequest<Result<TResponse>>`
   (or `IRequest<Result>` if there's no return value).
2. **Handler** — implements `IRequestHandler<TRequest, TResponse>`; talks to
   `AppDbContext` and/or domain entities directly, returns a `Result`.
3. **Validator** (optional) — a `FluentValidation.AbstractValidator<TRequest>`.
   Discovered and run automatically by `ValidationBehavior` — nothing to
   register by hand.
4. **Endpoint** — implements `IEndpoint`, maps the route, calls
   `IMediator.Send(...)`, and converts the `Result` to an HTTP response
   (`Results.Ok(result.Value)` / `result.ToProblem()` on failure —
   see `Common/ResultPattern/ResultExtensions.cs`).

Handlers, validators, and `IEndpoint` implementations are all picked up via
assembly scanning (`Program.cs`), so a new slice needs no other wiring.

## CI

`.github/workflows/ci.yml` restores, builds, and runs both test projects on
every push to `master` and on pull requests.
