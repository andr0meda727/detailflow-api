using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCategoriesAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                schema: "scheduling",
                table: "service",
                type: "text",
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.CreateTable(
                name: "service_price",
                schema: "scheduling",
                columns: table => new
                {
                    vehicle_size = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_price_pkey", x => new { x.service_id, x.vehicle_size });
                    table.CheckConstraint("CK_service_price_amount_cents", "amount_cents >= 0");
                    table.ForeignKey(
                        name: "service_price_service_id_fkey",
                        column: x => x.service_id,
                        principalSchema: "scheduling",
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_price",
                schema: "scheduling");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "scheduling",
                table: "service");
        }
    }
}
