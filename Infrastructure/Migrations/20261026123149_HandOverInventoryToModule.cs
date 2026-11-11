using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandOverInventoryToModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The inventory and inventory_category tables are not dropped:
            // ownership moves to the Inventory module, whose InventoryDbContext migrations
            // take them over (and move them to the "inventory" schema).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty, see Up.
        }
    }
}
