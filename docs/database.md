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

**Order matters:** apply `ApplicationDbContext` first, then the modules. Module migrations may adopt tables
that the core baseline created; for example, Inventory moves `d.inventory*` into the `inventory` schema and CRM moves `d.client` and
`d.vehicle` into `crm`. The order between modules doesn't matter.

The integration tests apply all migrations in that order to a fresh Postgres container. If an EF model has changes
that no migration covers, they fail.

## Adding a migration

```bash
dotnet ef migrations add <Name> -p Infrastructure -s Api -c ApplicationDbContext
dotnet ef migrations add <Name> -p Modules/Inventory/Inventory.Infrastructure -s Api -c InventoryDbContext -o Persistence/Migrations
dotnet ef migrations add <Name> -p Modules/Crm/Crm.Infrastructure -s Api -c CrmDbContext -o Persistence/Migrations
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
```

Open Supabase → SQL Editor, then paste and run the files in order.

**B. Apply directly from your machine**

```bash
dotnet ef database update -p Infrastructure -s Api -c ApplicationDbContext --connection "<connection string>"
dotnet ef database update -p Modules/Inventory/Inventory.Infrastructure -s Api -c InventoryDbContext --connection "<connection string>"
dotnet ef database update -p Modules/Crm/Crm.Infrastructure -s Api -c CrmDbContext --connection "<connection string>"
```

Use the **direct / session** connection string (port `5432`) from Supabase → Project Settings → Database.
Don't use the transaction pooler (port `6543`): migrations need a session connection.

## One-time: adopting the baseline on the existing database

The old `InitialSync` migration was behind the real schema, because `studio_invitation`,
`studio_member.is_active/updated_at` and `booking_service.updated_at` were added by hand.
It was replaced by a single `Baseline` migration that matches the current model.
The existing database already has these tables, so the baseline must be **marked as applied, not run**:

1. Check that the database matches the baseline. Compare
   `dotnet ef migrations script 0 20261026120857_Baseline -p Infrastructure -s Api -c ApplicationDbContext`
   with the live schema (Supabase → Database → Tables, or `pg_dump --schema-only --schema=d`).
2. Record the baseline as applied:

```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL PRIMARY KEY,
    "ProductVersion" character varying(32)  NOT NULL
);

DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260301134918_InitialSync';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261026120857_Baseline', '10.0.3');
```

After that, apply every later migration with option A or B above.

## Data that crosses modules

Modules never join each other's tables, and there are no foreign keys between module schemas (the FKs to
`d.studio` are a temporary exception until studios move into their own module). A module that needs another
module's data keeps its own copy and updates it from integration events. For example, `d.booking` stores the
client's name/phone/email and the vehicle's plate/brand/model, updated by `BookingCopiesConsumer` when CRM
publishes `ClientDetailsChanged` / `VehicleDetailsChanged`.
