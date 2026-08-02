# Vertical Slice Architecture

A .NET 10 minimal API template organized around **vertical slices**: each
feature (`RegisterClient`, `DeactivateClient`, ...) owns its own
command/query, handler, validator, and endpoint in one folder, instead of
being spread across horizontal layers (controllers/services/repositories).

The domain code, persistence, and web API live in separate projects so the
dependency direction is enforced by the compiler — and by a dedicated
architecture-test project — not just by convention.

## Stack

- **.NET 10** minimal APIs
- **PostgreSQL or SQL Server** via **EF Core**, provider chosen at runtime
  (`Persistence:Provider`)
- A small hand-rolled command/query dispatcher
  (`Api/Infrastructure/Messaging`) — not MediatR — supporting both direct
  handler injection and dispatcher-based resolution
- **FluentValidation** for request shape validation
- A `Result`/`Error` pattern instead of throwing for expected failures
  (`SharedKernel/Results`)
- **JWT bearer authentication** against Microsoft Entra ID or Keycloak,
  policy-based authorization, and ASP.NET Core rate limiting
- **Serilog** + **OpenTelemetry** for logging/tracing/metrics
- **Scalar** for interactive API docs (over ASP.NET Core's built-in OpenAPI
  document generation)
- **xUnit v3** for tests: architecture tests (NetArchTest), domain unit
  tests, and API integration tests against a **Testcontainers**-managed
  Postgres

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) — for the local Postgres/Keycloak/OTel
  stack (via Docker Compose) and for the integration tests (via
  Testcontainers)

## Getting started

Clone the repo, then pick one of two ways to run it.

### Option A — everything in Docker

```bash
docker compose up --build
```

This starts Postgres, a local Keycloak instance (pre-seeded with a realm and
three test users), an OpenTelemetry collector, and the API — which applies
EF Core migrations automatically on startup (see [Database](#database)
below). The API is reachable at `http://localhost:5149`.

### Option B — API on the host, everything else in Docker

```bash
docker compose up postgres keycloak otel-collector -d
dotnet run --project src/VerticalSliceArchitecture.Api
```

The default `appsettings.Development.json` already points at Postgres
(`localhost:5432`) and Keycloak (`http://localhost:8080/realms/acme`), so no
further configuration is needed. The API listens on `http://localhost:5149`
(see `src/VerticalSliceArchitecture.Api/Properties/launchSettings.json` for
the `http`/`https` profiles).

Either way, once it's running:

- `GET /health` / `GET /alive` → health checks (anonymous)
- `GET /scalar` → interactive API docs (Development only)
- `GET /openapi/v1.json` → the raw OpenAPI document (Development only)

### Trying it out end to end

The seeded Keycloak realm (`deploy/keycloak/acme-realm.json`) has three
users — `reader`/`reader`, `client-manager`/`client-manager`, and
`admin`/`admin` — matching the three application roles
(`acme.reader`, `acme.client-manager`, `acme.administrator`). Get a token via
the password grant and call the API:

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/realms/acme/protocol/openid-connect/token \
  -d grant_type=password -d client_id=acme-api \
  -d username=client-manager -d password=client-manager \
  | jq -r .access_token)

curl -X POST http://localhost:5149/api/v1/clients \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"companyName":"Acme Corp","contactEmail":"someone@acme-corp.test"}'
```

A `reader`-role token can `GET /api/v1/clients` but gets `403 Forbidden` on
the same `POST` — that's the policy-based authorization described in
[CLAUDE.md](CLAUDE.md).

## Configuration

Settings are bound from `appsettings.json` sections and validated at
startup (a misconfigured value fails the process immediately instead of
surfacing as a puzzling runtime error later):

| Section                 | Purpose                                                              |
| ----------------------- | -------------------------------------------------------------------- |
| `Persistence`           | Database provider, connection string, migration/logging behavior     |
| `Security`              | Identity provider (`EntraId`/`Keycloak`), authority, audience, roles |
| `Observability`         | Service name/version, OTLP endpoint, sampling                        |
| `RateLimiting`          | Per-policy limits (`PerUser`, `Sensitive`, `Burst`)                  |
| `AzureAppConfiguration` | Optional centralized config source; no-ops if unset                  |

`appsettings.json` (used outside Development) leaves secrets blank on
purpose — supply the real values via environment variables or
[user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
(a `UserSecretsId` is already configured in the Api csproj), not by
committing credentials. Any setting can be overridden with an environment
variable using the `Section__Key` convention, e.g.:

```bash
Persistence__ConnectionString="Host=localhost;Port=5432;Database=vertical_slice_architecture;Username=postgres;Password=postgres"
```

## Database

Migrations live in `src/VerticalSliceArchitecture.Persistence/Migrations/`
— note the `DbContext` lives in the **Persistence** project, not the Api
project, so both `--project` and `--startup-project` are needed:

```bash
dotnet tool restore   # once per clone — pins the dotnet-ef version in dotnet-tools.json

dotnet ef migrations add <Name> \
  --project src/VerticalSliceArchitecture.Persistence \
  --startup-project src/VerticalSliceArchitecture.Api \
  --output-dir Migrations
```

The API applies pending migrations automatically on startup when
`Persistence:MigrateOnStartup` is `true` (the default in
`appsettings.Development.json`); there's nothing to run by hand for local
development.

## Tests

```bash
dotnet test
```

This runs all three test projects under `tests/`:

- **`VerticalSliceArchitecture.ArchitectureTests`** — layering rules
  (`Domain`/`SharedKernel` can't depend on `Persistence`/`Api`, no EF
  Core/ASP.NET Core leaking into `Domain`) and handler/endpoint naming
  conventions, via [NetArchTest](https://github.com/BenMorris/NetArchTest).
  No database, no Docker.
- **`VerticalSliceArchitecture.Domain.Tests`** — plain unit tests for the
  `Client`/`User` aggregates, value objects, and the `Result` pattern. No
  database, no Docker.
- **`VerticalSliceArchitecture.Api.IntegrationTests`** — spins up a real,
  disposable Postgres via Testcontainers and runs the actual API against it
  end to end (`ApiFactory : WebApplicationFactory<Program>`), authenticating
  through a test-only header-driven identity instead of a real Keycloak/
  EntraId token. Requires Docker to be running.

## Project structure

```text
src/
  VerticalSliceArchitecture.SharedKernel/   # Result/Error, AggregateRoot, ValueObject,
                                             # IStronglyTypedId — no project references
  VerticalSliceArchitecture.Domain/
    Clients/                                # Client aggregate, value objects, events, errors
    Users/                                  # User aggregate, value objects, events, errors
  VerticalSliceArchitecture.Persistence/
    ApplicationDbContext.cs
    Configurations/                         # IEntityTypeConfiguration<T> per entity
    Converters/                             # strongly-typed-id value converters
    Interceptors/                           # audit stamping, domain event dispatch
    Repositories/
    Migrations/
  VerticalSliceArchitecture.Integrations/   # reserved for future external adapters
  VerticalSliceArchitecture.Api/
    Program.cs
    Features/
      Clients/
        RegisterClient/                     # Command + Handler + Validator + Endpoint + Response
        GetClientById/                      # Query + Handler + Endpoint + Response
        DeactivateClient/
        SearchClients/
      Users/
        GetCurrentUser/
    Infrastructure/
      Endpoints/                            # IEndpoint, auto-registration, filters
      Messaging/                            # IDispatcher, command/query contracts
      Security/                             # JWT bearer, policies, JIT user provisioning
      RateLimiting/
      Observability/
      Middleware/
tests/
  VerticalSliceArchitecture.ArchitectureTests/
  VerticalSliceArchitecture.Domain.Tests/
  VerticalSliceArchitecture.Api.IntegrationTests/
deploy/
  keycloak/acme-realm.json                  # local dev realm: roles, client, seeded users
  otel-collector/config.yaml                # local dev OTLP collector (debug exporter)
```

### Adding a new vertical slice

Using `Features/Clients/RegisterClient/` as the template:

1. **Command or query** — a record implementing `ICommand`,
   `ICommand<TResponse>`, or `IQuery<TResponse>`.
2. **Handler** — `internal sealed class` implementing
   `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`, or
   `IQueryHandler<TQuery, TResponse>`; talks to the aggregate/repository (for
   writes) or the read-model view (for reads), and returns a `Result`.
3. **Validator** (optional) — a `FluentValidation.AbstractValidator<TRequest>`,
   wired in via `.WithValidation<TRequest>()` on the endpoint. Shape
   validation only — the domain's value objects re-validate independently.
4. **Endpoint** — `internal sealed class` implementing `IEndpoint`, under the
   `Features` namespace, mapping the route and converting the `Result` to an
   HTTP response (`.ToOk()` / `.ToCreated(...)` / `.ToNoContent()`).

Handlers, validators, and `IEndpoint` implementations are all picked up via
assembly scanning (`Program.cs`), so a new slice needs no other wiring — and
`ArchitectureTests` will fail the build if the naming/accessibility
conventions above aren't followed.

See [CLAUDE.md](CLAUDE.md) for a deeper architectural walkthrough (the
dispatcher, the `Result` pattern, persistence conventions, security, and
testing conventions).

## CI

`.github/workflows/ci.yml` restores, builds, and runs all three test
projects on every push to `master` and on pull requests. GitHub-hosted
runners ship Docker preinstalled, so the Testcontainers-backed integration
tests work without any extra setup.
