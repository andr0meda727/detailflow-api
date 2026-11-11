using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptCrmTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The tables already exist in schema "d" (created by ApplicationDbContext's Baseline migration).
            // Instead of creating them, move them into the module's own schema. ALTER TABLE ... SET SCHEMA
            // keeps data, indexes and constraints, including the FKs to d.studio that this model doesn't map.
            migrationBuilder.EnsureSchema(name: CrmDbContext.Schema);

            migrationBuilder.RenameTable(name: "client", schema: "d", newName: "client", newSchema: CrmDbContext.Schema);
            migrationBuilder.RenameTable(name: "vehicle", schema: "d", newName: "vehicle", newSchema: CrmDbContext.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "vehicle", schema: CrmDbContext.Schema, newName: "vehicle", newSchema: "d");
            migrationBuilder.RenameTable(name: "client", schema: CrmDbContext.Schema, newName: "client", newSchema: "d");
        }
    }
}
