using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptIdentityTables : Migration
    {
        private static readonly string[] Tables = ["role", "user", "studio", "studio_member", "studio_invitation"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The tables already exist in schema "d" (created by ApplicationDbContext's migrations).
            // Instead of creating them, move them into the module's own schema. ALTER TABLE ... SET SCHEMA
            // keeps data, indexes and constraints, including the foreign keys other modules' tables have to
            // the studio and user, and the user's foreign key to auth.users in Supabase.
            migrationBuilder.EnsureSchema(name: IdentityDbContext.Schema);

            foreach (var table in Tables)
            {
                migrationBuilder.RenameTable(name: table, schema: "d", newName: table, newSchema: IdentityDbContext.Schema);
            }

            // In Supabase, the on_auth_user_created trigger on auth.users runs d.handle_new_user(), which inserts the
            // profile row into d."user" by name. Point it at the moved table and move it along, in the same
            // transaction, so registration keeps working. Databases without the function (tests) skip this.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regprocedure('d.handle_new_user()') IS NOT NULL THEN
                        CREATE OR REPLACE FUNCTION d.handle_new_user()
                            RETURNS trigger
                            LANGUAGE plpgsql
                            SECURITY DEFINER
                            SET search_path TO ''
                        AS $function$
                        BEGIN
                            INSERT INTO identity."user" (id, email, first_name, last_name)
                            VALUES (new.id, new.email, new.raw_user_meta_data->>'first_name', new.raw_user_meta_data->>'last_name');
                            RETURN new;
                        END;
                        $function$;

                        ALTER FUNCTION d.handle_new_user() SET SCHEMA identity;
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: IdentityDbContext.Schema,
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("outbox_message_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "outbox_message_pending_idx",
                schema: IdentityDbContext.Schema,
                table: "outbox_message",
                column: "occurred_at",
                filter: "processed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_message", schema: IdentityDbContext.Schema);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regprocedure('identity.handle_new_user()') IS NOT NULL THEN
                        ALTER FUNCTION identity.handle_new_user() SET SCHEMA d;

                        CREATE OR REPLACE FUNCTION d.handle_new_user()
                            RETURNS trigger
                            LANGUAGE plpgsql
                            SECURITY DEFINER
                            SET search_path TO ''
                        AS $function$
                        BEGIN
                            INSERT INTO d."user" (id, email, first_name, last_name)
                            VALUES (new.id, new.email, new.raw_user_meta_data->>'first_name', new.raw_user_meta_data->>'last_name');
                            RETURN new;
                        END;
                        $function$;
                    END IF;
                END $$;
                """);

            foreach (var table in Tables.Reverse())
            {
                migrationBuilder.RenameTable(name: table, schema: IdentityDbContext.Schema, newName: table, newSchema: "d");
            }
        }
    }
}
