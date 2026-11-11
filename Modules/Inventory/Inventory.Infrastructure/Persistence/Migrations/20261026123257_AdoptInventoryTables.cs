using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptInventoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The tables already exist in schema "d" (created by ApplicationDbContext's Baseline migration).
            // Instead of creating them, move them into the module's own schema. ALTER TABLE ... SET SCHEMA
            // keeps data, indexes and constraints, including the FK to d.studio that this model doesn't map.
            migrationBuilder.EnsureSchema(name: InventoryDbContext.Schema);

            migrationBuilder.RenameTable(name: "inventory_category", schema: "d", newName: "inventory_category", newSchema: InventoryDbContext.Schema);
            migrationBuilder.RenameTable(name: "inventory", schema: "d", newName: "inventory", newSchema: InventoryDbContext.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "inventory", schema: InventoryDbContext.Schema, newName: "inventory", newSchema: "d");
            migrationBuilder.RenameTable(name: "inventory_category", schema: InventoryDbContext.Schema, newName: "inventory_category", newSchema: "d");
        }
    }
}
