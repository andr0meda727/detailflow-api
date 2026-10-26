using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptSchedulingTables : Migration
    {
        private static readonly string[] Tables = ["booking_status", "service", "booking", "booking_service"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The tables already exist in schema "d" (created by ApplicationDbContext's migrations).
            // Instead of creating them, move them into the module's own schema. ALTER TABLE ... SET SCHEMA
            // keeps data, indexes and constraints, including the FKs to d.studio and d."user" that this
            // model doesn't map.
            migrationBuilder.EnsureSchema(name: SchedulingDbContext.Schema);

            foreach (var table in Tables)
            {
                migrationBuilder.RenameTable(name: table, schema: "d", newName: table, newSchema: SchedulingDbContext.Schema);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables.Reverse())
            {
                migrationBuilder.RenameTable(name: table, schema: SchedulingDbContext.Schema, newName: table, newSchema: "d");
            }
        }
    }
}
