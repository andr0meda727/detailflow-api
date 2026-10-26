# Database and migrations

The database is the Supabase Postgres instance. Supabase owns the `auth` schema (users, sessions, sign-in).
The application tables live in schema `d` and are managed **only** through EF Core migrations
in `Infrastructure/Migrations`. Don't change `d` by hand in the Supabase dashboard: put every change in a migration.

The integration tests apply all migrations to a fresh Postgres container. If the EF model has changes
that no migration covers, they fail.

## Adding a migration

```bash
dotnet ef migrations add <Name> -p Infrastructure -s Api
```

Review the generated `Up`/`Down` code before committing it.

## Applying migrations to Supabase

Pick one of these:

**A. Generate SQL and paste it into the Supabase SQL editor** (simplest)

```bash
# idempotent: safe to run on a database that already has some of the migrations
dotnet ef migrations script -p Infrastructure -s Api --idempotent -o migrate.sql
```

Open Supabase → SQL Editor, paste `migrate.sql` and run it.

**B. Apply directly from your machine**

```bash
dotnet ef database update -p Infrastructure -s Api --connection "<connection string>"
```

Use the **direct / session** connection string (port `5432`) from Supabase → Project Settings → Database.
Don't use the transaction pooler (port `6543`): migrations need a session connection.

## One-time: adopting the baseline on the existing database

The old `InitialSync` migration was behind the real schema, because `studio_invitation`,
`studio_member.is_active/updated_at` and `booking_service.updated_at` were added by hand.
It was replaced by a single `Baseline` migration that matches the current model.
The existing database already has these tables, so the baseline must be **marked as applied, not run**:

1. Check that the database matches the baseline. Compare
   `dotnet ef migrations script 0 20261026120857_Baseline -p Infrastructure -s Api`
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
