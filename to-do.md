# To-Do: Getting this template to a runnable state

This template compiles the skeleton of a Vertical Slice Architecture. The EF
Core + PostgreSQL wiring and domain-event dispatch are now done (see below);
what's left is mostly unfinished feature slices, local dev ergonomics, tests,
and docs.

## Done

- [x] **`AppDbContext` registered with DI**, using `Npgsql.EntityFrameworkCore.PostgreSQL`
  (`Program.cs`), with a `ConnectionStrings:Database` entry in
  `appsettings.Development.json` (a real local default) and an empty
  placeholder in `appsettings.json` (production should supply this via
  user-secrets/env vars — a `UserSecretsId` is already set in the csproj).
- [x] **Initial EF Core migration generated** (`Common/Database/Migrations/`).
  This also surfaced and fixed a few latent bugs blocking it: `AppDbContext`
  had no constructor accepting `DbContextOptions<AppDbContext>`; `OrderItemId`
  had no value converter registered; and `OnModelCreating` never called
  `ApplyConfigurationsFromAssembly`, so `OrderConfiguration`/`ProductConfiguration`
  were silently never applied.
- [x] **`MigrateDatabaseAsync` implemented** (`Common/Database/DatabaseMigrationExtensions.cs`)
  and re-enabled in `Program.cs` for the Development environment.
- [x] **`DispatchDomainEventsInterceptor` registered** on `AppDbContext` via
  `AddInterceptors(...)`. Getting this to actually compile and work required:
  a custom `IPublisher`/`INotificationHandler<>` pair implemented on the
  existing hand-rolled `Mediator` (there was no publish mechanism before, only
  `Send`); and a new non-generic `IHasDomainEvents` marker interface on
  `AggregateRoot<TId>`, because `ChangeTracker.Entries<AggregateRoot<object>>()`
  can never match a real entity (C# class generics aren't covariant). No
  `INotificationHandler<>` implementations exist yet — the plumbing works but
  nothing currently reacts to `ProductCreatedEvent`/`OrderPaidEvent`/etc.
- [x] **`ICorrelationContext`/`ICorrelationIdSetter` implemented**
  (`Common/Middleware/CorrelationContext.cs`) — `CorrelationIdMiddleware`
  referenced these but they didn't exist anywhere, so the project failed to
  build for a reason unrelated to the database work above.
- [x] **`ValidationBehavior` implemented** (`Common/Messaging/Behavior/ValidationBehavior.cs`)
  and registered in `AddMediator()` (runs inside `LoggingBehavior`, before the
  handler). Runs all matching `IValidator<TRequest>`s and, on failure, builds a
  failed `Result`/`Result<TValue>` via reflection instead of calling `next()`.
  This required two more fixes to actually take effect: FluentValidation
  validators were never scanned into DI (`AddMediatorHandlers` now also
  registers `IValidator<>` implementations), and `AddMediatorHandlers()` was
  being called in `Program.cs` with **zero assemblies**, so no handler,
  validator, or notification handler was ever registered at all — every
  `mediator.Send()` call would have thrown at runtime. Also fixed a separate,
  unrelated startup crash found while smoke-testing this: `app.UseAuthorization()`
  was called without `AddAuthorization()` ever being registered.
  Smoke-tested end-to-end against a real Postgres: an invalid `POST /api/products`
  now short-circuits with a `Validation` error before the handler/DB is ever
  reached, and a valid request still persists correctly.
- [x] **`IProductRepository` stub removed** (was an empty placeholder class,
  unused — deleted rather than implemented, since `CreateProductHandler`
  already talks to `AppDbContext` directly).
- [x] **Test project scaffolded** — `tests/VerticalSliceArchitecture.Api.Tests/`
  now exists and is wired into `VerticalSliceArchitecture.slnx`, still just the
  default template `UnitTest1.cs`.

- [x] **`CreateProductEndpoint` now checks `Result.IsFailure`.** Added a
  shared `Result.ToProblem()` extension (`Common/ResultPattern/ResultExtensions.cs`)
  that maps `ErrorType` → HTTP status (`Validation`/default → 400, `NotFound`
  → 404, `Conflict` → 409) and returns a `ProblemDetails` response, so future
  endpoints (Orders, `GetProductById`) don't have to repeat this mapping.
  `CreateProductEndpoint` now branches on `result.IsSuccess` and, on success,
  returns `result.Value` instead of the whole `Result` wrapper — the response
  body previously included the `isSuccess`/`error` envelope fields even on a
  201, which didn't match the declared `.Produces<CreateProductResponse>()`
  contract. Re-verified end-to-end against a real Postgres: invalid input now
  returns a clean `400` with the validation messages in `detail`, valid input
  still returns `201` with just the DTO.

- [x] **"Get Product By Id" feature implemented.** `GetProductByIdQuery`/
  `ProductDetailsDto`/`GetProductByIdHandler`/`GetProductByIdEndpoint` all
  filled in (`GET /api/products/{id:guid}`). The handler projects straight to
  the DTO with `AsNoTracking()` and returns a `Product.NotFound` /
  `ErrorType.NotFound` failure when missing, which `Result.ToProblem()` turns
  into a 404. Also filled in the previously-empty `ProductsConstants` stub
  (`BaseRoute`/`Tag`) and pointed both product endpoints at it instead of
  duplicating the `"/api/products"` string. Verified end-to-end: create →
  200 GET by id, 404 with problem details for a valid-but-missing id.

- [x] **Orders feature slices implemented** — `CreateOrder`
  (`POST /api/orders`) and `CancelOrder` (`POST /api/orders/{id:guid}/cancel`).
  `CreateOrderHandler` looks up all referenced products in one query, fails
  with `Product.NotFound` (aggregating every missing id into one message) if
  any are missing, then builds the `Order` aggregate via `Order.Create`/`AddItem`
  using each product's current price. `CancelOrderHandler` checks
  `OrderStatus.Shipped` itself before calling `order.Cancel()` (rather than
  catching the `InvalidOperationException` the domain method throws for that
  case) so an already-shipped order comes back as a 409, not a 500; cancelling
  an already-cancelled order is a no-op 204 (idempotent), matching
  `Order.Cancel()`'s own behavior. Added a shared `OrdersConstants`
  (`BaseRoute`/`Tag`), matching `ProductsConstants`. Removed the now-redundant
  empty `<Folder Include>` csproj entries for these two directories now that
  they contain real files. No new migration was needed (`Order`/`OrderItem`
  were already mapped). Verified end-to-end against a real Postgres: valid
  order creation with correct computed totals, `Product.NotFound` (404) for an
  unknown product, validation 400s, cancel → 204, double-cancel → still 204,
  cancelling an unknown order → 404.

## Blocking — still needed

- _Nothing outstanding from earlier passes — see "Needed to actually
  develop/run locally", "Testing", and "Docs / repo setup" below._

## Scaffolded but not built out

- _Orders slices are done — see "Done" above. Nothing else currently
  scaffolded but empty._

- [x] **`dotnet-ef` tool manifest created** (`dotnet-tools.json` at repo root
  — this SDK's `dotnet new tool-manifest` puts it there by default rather than
  under `.config/`, confirmed empirically, and `dotnet tool restore`/`dotnet ef`
  both resolve it fine from that location). Pinned to `10.0.10` to match the
  `Microsoft.EntityFrameworkCore*` package versions already in the csproj.
  Contributors now just need `dotnet tool restore` once, instead of a global
  `dotnet-ef` install, before running `dotnet ef migrations add`/`database update`.
  **Not yet committed** — `dotnet-tools.json` is untracked; it should be added
  to source control so this is actually shared with other contributors.

- [x] **Docker Compose setup created** (`docker-compose.yml` at repo root).
  A `postgres` service matching `appsettings.Development.json` exactly
  (db/user/password, port 5432, named volume, healthcheck), plus an `api`
  service that builds the existing `Dockerfile` and overrides
  `ConnectionStrings__Database` to use `Host=postgres` (the compose service
  name) instead of `localhost`, since containers can't reach each other via
  `localhost`. `ASPNETCORE_ENVIRONMENT=Development` is set on the `api`
  service so the startup `MigrateDatabaseAsync()` call still runs. Verified
  end-to-end with `docker compose up --build` (using temporary port overrides
  during testing since host `5432`/`8080` were already in use by other
  running projects on this machine): image builds, Postgres becomes healthy,
  API starts, migrates, and successfully serves create/get product and create
  order requests. One non-fatal warning observed in the container logs:
  `libgssapi_krb5.so.2` missing (Npgsql's optional Kerberos/GSSAPI probe on
  the slim base image) — doesn't affect password-based auth, not fixed.
  **Not yet committed** — `docker-compose.yml` is untracked.

## Needed to actually develop/run locally

- _Nothing outstanding — see "Done" above._

## Testing

- [x] **Real test coverage added**, replacing both projects' default
  `UnitTest1.cs` templates:
  - `VerticalSliceArchitecture.Api.Tests` (50 tests, no DB) — domain logic
    (`Order` state machine including the Shipped/Cancelled/merge-quantity edge
    cases, `Product`, `Money`), `Result`/`Error` invariants, `ResultExtensions.ToProblem()`
    status-code mapping, and the messaging infrastructure built this session
    (`ValidationBehavior`, `LoggingBehavior`, `Mediator.Send`/`Publish`).
  - `VerticalSliceArchitecture.Api.IntegrationTests` (12 tests) — a new
    `ApiFactory : WebApplicationFactory<Program>` spins up a real disposable
    Postgres via **Testcontainers** per test run and points the app at it, so
    these exercise the real HTTP pipeline end-to-end (EF mappings, migrations,
    mediator, validation, endpoints) rather than mocking any of it. Covers
    create/get product, create/cancel order, validation 400s, not-found 404s,
    and — since there's no "ship order" feature yet to reach that state
    naturally — a cancel-already-shipped 409 test that sets the order's status
    directly via `AppDbContext` before hitting the endpoint.
  - Required a few small production-side changes to make testing possible:
    `public partial class Program;` in `Program.cs` (standard requirement for
    `WebApplicationFactory<Program>` with top-level statements), and
    `[assembly: InternalsVisibleTo(...)]` for both test projects so the
    `internal` `ValidationBehavior`/`LoggingBehavior` classes could be tested
    directly (`src/VerticalSliceArchitecture.Api/AssemblyInfo.cs`).
  - One real bug caught along the way (in the tests, not production code):
    `Mediator`'s `dynamic` dispatch can't bind to `Handle` on `private` nested
    test-double classes, since the DLR's accessibility check can't see a
    private type from another assembly. Production handlers are always
    top-level `public` classes, so this never surfaces there — fixed by making
    the fake handler/behavior/event types in `MediatorTests` public.
  - All 62 tests pass via `dotnet test VerticalSliceArchitecture.slnx`.
  - **Not yet committed** — all of the above (including the two test
    `.csproj` changes and `Program.cs`/`AssemblyInfo.cs`) is untracked/unstaged.

## Docs / repo setup

- [x] **README written.** Covers: stack overview, prerequisites, two ways to
  run it (full Docker Compose, or API on the host against a Docker Postgres),
  connection-string/user-secrets configuration, migrations (including the
  `dotnet tool restore` step for the pinned `dotnet-ef`), running tests, where
  Scalar/OpenAPI are served, project structure, and a guide for adding a new
  vertical slice. The Scalar/OpenAPI routes (`/scalar`, `/openapi/v1.json`)
  and the bare `dotnet test` auto-discovering `VerticalSliceArchitecture.slnx`
  were both verified directly rather than assumed.
- [x] **CI pipeline added** (`.github/workflows/ci.yml` — the file/folder had
  already been created, empty, presumably in parallel; filled in the content).
  Single `build-and-test` job on `ubuntu-latest`, triggered on push to
  `master` and on pull requests: `actions/setup-dotnet@v4` (`10.0.x`, with
  NuGet caching keyed on `**/*.csproj`), then `dotnet restore` → `build
  --no-restore` → `test --no-build`, all against `VerticalSliceArchitecture.slnx`
  so both test projects run. The integration tests' Testcontainers-managed
  Postgres needs Docker, which GitHub-hosted `ubuntu-latest` runners have
  preinstalled — no extra setup required. Validated the YAML syntax with `yq`;
  didn't do a full `act` dry-run since the underlying `dotnet restore/build/test`
  commands were already exercised directly against this exact solution
  earlier this session.
