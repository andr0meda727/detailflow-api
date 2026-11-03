# Database and migrations

The database is the Supabase Postgres instance. Supabase owns the `auth` schema (users, sessions, sign-in).
Everything else is managed **only** through EF Core migrations. Don't change these schemas by hand in the
Supabase dashboard: put every change in a migration.

Each module owns a schema and has its own `DbContext` and migrations:

| Context | Schema | Migrations | History table |
|---|---|---|---|
| `ApplicationDbContext` (legacy core) | `d` | `Infrastructure/Migrations` | `public.__EFMigrationsHistory` |
| `InventoryDbContext` | `inventory` | `Modules/Inventory/Inventory.Infrastructure/Persistence/Migrations` | `inventory.__EFMigrationsHistory` |
| `CrmDbContext` | `crm` | `Modules/Crm/Crm.Infrastructure/Persistence/Migrations` | `crm.__EFMigrationsHistory` |
| `SchedulingDbContext` | `scheduling` | `Modules/Scheduling/Scheduling.Infrastructure/Persistence/Migrations` | `scheduling.__EFMigrationsHistory` |

**Order matters:** apply `ApplicationDbContext` first, then the modules. Module migrations may adopt tables
that the core baseline created; for example, Inventory moves `d.inventory*` into the `inventory` schema and CRM moves `d.client` and
`d.vehicle` into `crm`, and Scheduling moves `d.booking`, `d.booking_service`, `d.booking_status` and `d.service`
into `scheduling`. The order between modules doesn't matter.

The integration tests apply all migrations in that order to a fresh Postgres container. If an EF model has changes
that no migration covers, they fail.

## Adding a migration

```bash
dotnet ef migrations add <Name> -p Infrastructure -s Api -c ApplicationDbContext
dotnet ef migrations add <Name> -p Modules/Inventory/Inventory.Infrastructure -s Api -c InventoryDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Crm/Crm.Infrastructure -s Api -c CrmDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Scheduling/Scheduling.Infrastructure -s Api -c SchedulingDbContext -o Persistence/Migrations
```

Review the generated `Up`/`Down` code before committing it.
To undo a migration that was never applied, delete its files instead of running `ef migrations remove`
(that command connects to the database configured for the Api project).

## Applying migrations to Supabase

Pick one of these:

**A. Generate SQL and paste it into the Supabase SQL editor** (simplest)

```bash
# idempotent: safe to run on a database that already has some of the migrations
dotnet ef migrations script -p Infrastructure -s Api -c ApplicationDbContext --idempotent -o 1-core.sql
dotnet ef migrations script -p Modules/Inventory/Inventory.Infrastructure -s Api -c InventoryDbContext --idempotent -o 2-inventory.sql
dotnet ef migrations script -p Modules/Crm/Crm.Infrastructure -s Api -c CrmDbContext --idempotent -o 3-crm.sql
dotnet ef migrations script -p Modules/Scheduling/Scheduling.Infrastructure -s Api -c SchedulingDbContext --idempotent -o 4-scheduling.sql
```

Open Supabase → SQL Editor, then paste and run the files in order.

**B. Apply directly from your machine**

```bash
dotnet ef database update -p Infrastructure -s Api -c ApplicationDbContext --connection "<connection string>"
dotnet ef database update -p Modules/Inventory/Inventory.Infrastructure -s Api -c InventoryDbContext --connection "<connection string>"
dotnet ef database update -p Modules/Crm/Crm.Infrastructure -s Api -c CrmDbContext --connection "<connection string>"
dotnet ef database update -p Modules/Scheduling/Scheduling.Infrastructure -s Api -c SchedulingDbContext --connection "<connection string>"
```

Use the **direct / session** connection string (port `5432`) from Supabase → Project Settings → Database.
Don't use the transaction pooler (port `6543`): migrations need a session connection.

## One-time: adopting the baseline on the existing database

The old `InitialSync` migration was behind the real schema, because `studio_invitation`,
`studio_member.is_active/updated_at` and `booking_service.updated_at` were added by hand.
It was replaced by a single `Baseline` migration that matches the current model.
The existing database already has these tables, so the baseline must be **marked as applied, not run**.

The live schema was compared with the Baseline on 2026-11-03 (through the Supabase MCP). It was built by hand,
so its indexes and unique constraints differ; `docs/sql/01-reconcile-live-schema-with-baseline.sql` lists the
differences, fixes them and records the Baseline, all in one transaction. Run it once, then apply every later
migration with option A or B above. The whole sequence was rehearsed on a copy shaped like production.

Not managed by EF: Supabase's `on_auth_user_created` trigger on `auth.users` calls `d.handle_new_user()`, which
inserts the `d."user"` profile row for every new account. Registration depends on it, and the function names the
table with its schema, so moving `user` to another schema must update the function in the same migration.

## Data that crosses modules

Modules never join each other's tables, and there are no foreign keys between module schemas (the FKs to
`d.studio` and `d."user"` are a temporary exception until they move into their own module). A module that needs another
module's data keeps its own copy and updates it from integration events. For example, `scheduling.booking` stores
the client's name/phone/email and the vehicle's plate/brand/model, updated by Scheduling's `BookingCopiesConsumer` when CRM
publishes `ClientDetailsChanged` / `VehicleDetailsChanged`.

## Messaging tables

The message bus runs on MassTransit's PostgreSQL transport in the same database:

- The `transport` schema holds queues, topics and messages. MassTransit creates it on startup if it is missing,
  together with a `detailflow_transport` role, so it is not part of the EF migrations. The connection user
  needs permission to create a schema and a role (Supabase's `postgres` user has both).
- Each module that publishes events has an `outbox_message` table in its own schema (e.g. `crm.outbox_message`),
  created by that module's migrations. Events are written there in the same transaction as the data change,
  and a background dispatcher publishes them about once a second.

Delivery is at least once, so consumers must be safe to run twice for the same event. A message that fails
to publish is retried; after 10 attempts it stays in `outbox_message` with `last_error` set, for inspection.
Consumer failures are retried three times, then the message moves to the endpoint's `_error` queue in the
`transport` schema.
