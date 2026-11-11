# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

DetailFlow is a multi-tenant SaaS API for car-detailing studios (ASP.NET Core 10, EF Core 10, Supabase Postgres + Supabase Auth).
A public, Booksy-like client marketplace is planned. The codebase is mid-migration from a layered
`Api` / `Core` / `Infrastructure` design to a **modular monolith** whose modules can later be split into services.
Identity (sign-in, tenancy, studios, staff, invitations), Inventory, CRM (clients, vehicles) and Scheduling (bookings,
services) are modules, Notifications sends email and the bookings PDF report, and Marketplace holds the studios'
public listings for the customer map, and Customers holds customer accounts' profiles and garages. `Core` is gone; the legacy `Infrastructure`
project only holds `ApplicationDbContext` (no entities) and the historical core migrations. Billing was removed: the app
is free for now.

## Commands

```bash
dotnet build detailflow_api.sln
dotnet test                                   # all suites
dotnet test UnitTests
dotnet test ArchitectureTests
dotnet test IntegrationTests                  # needs Docker running (Testcontainers, postgres:17-alpine)
dotnet test IntegrationTests --filter "FullyQualifiedName~InventoryHandlerTests"
dotnet test UnitTests --filter "Name=GetAllBookingsAsync_PassesFiltersToRepository"

dotnet run --project Api                      # Scalar UI at /scalar in Development
docker compose up -d                          # Mailpit (SMTP 1025, UI 8025) for local email
```

Migrations: one per DbContext, core first, then modules. See `docs/database.md`.

```bash
dotnet ef migrations add <Name> -p Infrastructure -s Api -c ApplicationDbContext
dotnet ef migrations add <Name> -p Modules/Inventory/Inventory.Infrastructure -s Api -c InventoryDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Crm/Crm.Infrastructure -s Api -c CrmDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Scheduling/Scheduling.Infrastructure -s Api -c SchedulingDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Identity/Identity.Infrastructure -s Api -c IdentityDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Marketplace/Marketplace.Infrastructure -s Api -c MarketplaceDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Customers/Customers.Infrastructure -s Api -c CustomersDbContext -o Persistence/Migrations
```

**Never run EF commands that connect to a database** (`database update`, `migrations remove`, `script --idempotent`):
the Api's Development config points at the real Supabase database. To undo a migration, delete its files and
revert the model snapshot by hand.

## Architecture

### Projects
- `Api`: the host. It has `Program.cs` and the exception handler. It composes everything,
  e.g. `AddIdentityModule`, `AddInventoryModule`, `AddCrmModule`, `AddSchedulingModule`, `AddNotificationsModule`, `UseTenantContext()`, and `AddMessaging` (one MassTransit bus; pass every assembly that has consumers, e.g. `SchedulingModule.ConsumersAssembly`).
- `Infrastructure`: `ApplicationDbContext` with the core migrations only (the Baseline and the hand-over steps). Add no
  code here; new tables belong to a module.
- `Shared/`:
  - `Shared.Kernel` has `Result`/`Result<T>`/`Error`, `ITenantContext`/`ITenantEntity`, `IAuditableEntity`, and `PageRequest`/`PagedResponse<T>`. It must not reference EF or ASP.NET.
  - `Shared.Application` has the EF query helpers, e.g. `ToPagedResponseAsync`, and `IOutbox`.
  - `Shared.Infrastructure` has `TenantDbContext`, `TenantContext`, and messaging: `AddMessaging` (MassTransit on the PostgreSQL transport) and the per-module outbox (`AddOutbox<TContext>()`, `OutboxDispatcher`).
  - `Shared.Api` has `ToProblem(Error)`, the policy names `Policies.OwnerOnly` and `RateLimits.Public` (any module may
    require them), and `[AllowWithoutStudio]`.
- `Modules/<Name>/{Contracts, Domain, Application, Infrastructure, Api}`: the target shape. Inventory, CRM, Scheduling and Identity are the reference implementations; Notifications has no Domain/Application (it stores nothing).
  - Domain entities have private setters and factory/behaviour methods. State rules return `Result` errors from the domain (e.g. `Booking.Complete` → `BOOKING_ALREADY_FINISHED`).
  - Application uses vertical slices (`Items/CreateItem/CreateItemHandler.cs`). Handlers are plain classes over `I<Module>DbContext` (no repositories, no mediator) and return `Result<T>`.
  - Infrastructure has its own DbContext, Postgres schema and migrations history table.
  - Api has the controllers and `<Module>Module.Add<Module>Module()`, which registers the handlers and validators and calls `AddApplicationPart`.
  - Controllers inject handlers with `[FromServices]`.

### Coupling between modules
- `<Module>.Contracts` is the only public surface. It holds plain records and interfaces with no package dependencies, e.g. `Crm.Contracts.IClientDirectory` and `ClientDetailsChanged`. Everything else in a module is internal to it, and that includes the legacy projects.
- There are no cross-module EF navigations or foreign keys; other modules hold plain IDs.
- A module that needs another module's data **keeps its own copy** and updates it from integration events. Publish by calling `db.Enqueue(event)` on the module context (it implements `IOutbox`) before `SaveChanges`, so the event is saved in the same transaction. The module registers `AddOutbox<TContext>()` and calls `modelBuilder.AddOutbox()`, which needs a migration for its `outbox_message` table. For example, bookings copy the client's name and the vehicle's plate, and Scheduling's `BookingCopiesConsumer` keeps them current.
- Synchronous lookups go through a Contracts interface (`IClientDirectory`) or `IBookingReportSource`, never through another module's DbContext.
- Consumers run outside a request, so there is no tenant. They bypass the tenant filter by name and scope by the `StudioId` carried in the event.
- Delivery is at least once: consumers must be idempotent. MassTransit 8.x only, because 9.x needs a commercial licence.

`ArchitectureTests` (NetArchTest) enforce the boundaries:
- Dependencies point inwards.
- Modules never reference `Core`, `Infrastructure` or `Api`. The legacy projects and other modules may use a module only through its `Contracts`.
- Module Application layers use EF Core but not `Relational`/Npgsql.

A new module gets added to `ModuleBoundaryTests.Modules`, so every rule applies to it.

### Multi-tenancy (fails closed)
- Identity's `TenantMiddleware` resolves the caller's studio from their **active** memberships (and the optional
  `X-Studio-Id` header) and fills the scoped `ITenantContext`; errors are ProblemDetails (`USER_PROFILE_NOT_FOUND`,
  `STUDIO_ACCESS_DENIED`, `STUDIO_REQUIRED`). Mark endpoints that work without a studio with
  `[AllowWithoutStudio]` (`Shared.Api.Tenancy`), never by path.
- Every DbContext derives from `TenantDbContext`. It applies a named query filter `"Tenant"` (`StudioId == CurrentStudioId`) to every `ITenantEntity`, so with no tenant **no rows** are returned. On `SaveChanges` it stamps `StudioId` and audit timestamps, and it throws if a tenant entity is added without a studio.
- Override `ConfigureModel`, not `OnModelCreating` (it's sealed).
- CRM adds a named `"SoftDelete"` filter (Client, Vehicle). Scheduling has no filter on inactive services, so past bookings still show them; queries for new bookings filter on `IsActive`.
- Bypass a filter only deliberately, by name, e.g. `IgnoreQueryFilters([TenantDbContext.TenantFilter])` for the invitation token lookup.
- Validators must check that referenced IDs (services, clients, categories) belong to the current studio.

### API conventions (`docs/api-conventions.md`)
- Errors are RFC 7807 ProblemDetails with a stable `code` extension. Return `this.ToProblem(Error.NotFound("X_NOT_FOUND", …))`; don't return bare strings or anonymous objects.
- Unbounded lists are paged: bind `[FromQuery] PageRequest paging` and return `PagedResponse<T>`. Don't name the parameter `page`, because MVC then treats `?page=` as a prefix and ignores it.
- Order paged queries and end with the id as a tie-breaker.
- Money is stored as `long` cents (`Booking.TotalPrice`).
- Use `TimeProvider` for the current time in module code (tests use `FakeTimeProvider`).
- Moments are UTC everywhere. Each studio has an IANA `TimeZone` (Identity; other modules read it through
  `IStudioDirectory` or get it in events). Calendar-day inputs are `DateOnly` in the studio's zone; convert with
  `Shared.Kernel.Time.TimeZones` and query half-open ranges (`>= from && < toExclusive`).

## Gotchas
- `Api/Dockerfile` copies every `.csproj` for the restore layer, so add new projects there. Also add them to `detailflow_api.sln` under the `src` solution folder.
- Moving tables into a module: generate the module's first migration, then replace its body with `RenameTable(..., newSchema)` (see `AdoptCrmTables`, `AdoptSchedulingTables`). Generate an empty hand-over migration on the core context (see `HandOverCrmToModule`).
- Commit with explicit file paths. The working tree often has unrelated local edits.
- Secrets never go in tracked files. The dev connection string and Supabase keys are .NET user secrets
  (`dotnet user-secrets list --project Api`), which override `appsettings.Development.json`. The `.http` files use
  `{{variables}}`: shared ones in `Api/Endpoints/http-client.env.json`, personal ones (logins, studio IDs) in the
  git-ignored `http-client.private.env.json`; run them with the `dev` environment. `thesis/` is excluded locally.
- Integration tests share one Postgres container (`PostgresFixture`, SetUpFixture in the root `IntegrationTests` namespace).
  - Each fixture seeds its own studios through `TwoStudiosSeed`, so assert on seeded IDs, not on global counts.
  - API tests use `ApiFactory` with `TestAuthHandler`. Authenticate with `CreateClientFor(userId)`, which sets the `X-Test-User-Id` header.
  - `ApiFactory.Auth` is a `FakeAuthProvider` in place of Supabase; signing up inserts the profile row like the trigger does.
  - `ApiFactory.Email` is a `FakeEmailSender` that records emails instead of using SMTP. Endpoints that read the
    caller's email (reports) need `CreateClientFor(userId, email)`. Bus consumers run asynchronously, so poll with a deadline.
  - Pure logic (domain, validators, email templates, the PDF, consumers with NSubstitute) belongs in `UnitTests`;
    handlers are tested against real Postgres in `IntegrationTests`, not with a mocked DbContext.
- Profile rows in `identity."user"` come from Supabase's `handle_new_user()` trigger on `auth.users`, not from this code.
  Its SQL names the table, so moving that table must rewrite the function (see `AdoptIdentityTables`).
- Production still needs the pending upgrade in `docs/database.md` ("Upgrading production"). The Supabase MCP is
  read-only unless the user switches it; ask before any write.
- The customer marketplace (map search, customer accounts, booking requests, reviews) follows
  `docs/marketplace-roadmap.md`; steps 1 (service categories, prices per `VehicleSize`), 2 (Marketplace listings,
  public map search), 3 (Customers: profile and garage under `/api/me`) and 4 (online booking requests in Scheduling:
  `OnlineBookingRequest`, customer endpoints under `/api/me/booking-requests`, staff under `/api/booking-requests`,
  emails via `BookingRequestEmailConsumer`) are done. Work done for a customer has no tenant: Scheduling reads across
  studios only through the explicitly named `*OfAnyStudio` sets on `ISchedulingDbContext` and always matches a studio or
  customer itself. Customer rows are per account,
  not per studio: `CustomersDbContext` has an `Owner` filter on `ICustomerOwned` (`UserId == ITenantContext.UserId`),
  failing closed like the tenant filter. Shared input rules live in `Shared.Application.Validation`. Marketplace listings are public, cross-studio data: not `ITenantEntity`, so owner
  endpoints match `ITenantContext.StudioId` themselves. Public endpoints use `[AllowAnonymous]`, `[AllowWithoutStudio]`
  and `[EnableRateLimiting(RateLimits.Public)]`. The host trusts `X-Forwarded-For` only from private addresses.
  `VehicleSize` lives in `Shared.Kernel.Vehicles` and travels between modules by name; don't add a second one.
- Planning (Scheduling, `Domain/Planning`): a studio plans once it has opening hours and an active bay. `SlotPlanner`
  is pure (unit-tested); `BookingPlanner` loads its input through the `*OfAnyStudio` sets. A booking's `EndsAt` is
  always computed (services' `DurationMinutes` × size multiplier, rounded to the grid). Save bookings whose time or bay
  changes with `SaveChangesCheckedAsync`: it takes a per-bay advisory lock and maps the `booking_no_bay_overlap`
  exclusion violation to `BAY_TAKEN` (without the lock, racing inserts deadlock instead).
- Inventory stock changes only through `InventoryItem` methods that add a `StockMovement` (stock = sum of movements).
  Completing a booking anywhere must call `EnqueueIfCompleted`, which publishes `BookingCompleted`; Inventory's
  `BookingConsumptionConsumer` deducts the estimate once per booking (unique `booking_id`).
- Billing was deleted on purpose (the app is free for now). Paid plans would come back as a new Billing module fed by Stripe webhooks.
