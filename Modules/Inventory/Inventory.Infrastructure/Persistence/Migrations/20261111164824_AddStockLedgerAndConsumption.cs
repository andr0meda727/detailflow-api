using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLedgerAndConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_consumption",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_size = table.Column<string>(type: "text", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_consumption_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consumption_calibration",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    sample_count = table.Column<int>(type: "integer", nullable: false),
                    ape_sum = table.Column<decimal>(type: "numeric(14,6)", precision: 14, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("consumption_calibration_pkey", x => x.id);
                    table.ForeignKey(
                        name: "consumption_calibration_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_size_factors",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    small = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    medium = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    large = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    xl = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("material_size_factors_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_recipe",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_recipe_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_movement_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stock_movement_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_consumption_line",
                schema: "inventory",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumption_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    estimated_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_consumption_line_pkey", x => new { x.consumption_id, x.service_id, x.item_id });
                    table.ForeignKey(
                        name: "booking_consumption_line_consumption_id_fkey",
                        column: x => x.consumption_id,
                        principalSchema: "inventory",
                        principalTable: "booking_consumption",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_recipe_line",
                schema: "inventory",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_for_medium = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_recipe_line_pkey", x => new { x.recipe_id, x.item_id });
                    table.CheckConstraint("CK_service_recipe_line_quantity", "quantity_for_medium > 0");
                    table.ForeignKey(
                        name: "service_recipe_line_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "service_recipe_line_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalSchema: "inventory",
                        principalTable: "service_recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "booking_consumption_booking_id_key",
                schema: "inventory",
                table: "booking_consumption",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "consumption_calibration_studio_service_item_key",
                schema: "inventory",
                table: "consumption_calibration",
                columns: new[] { "studio_id", "service_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consumption_calibration_item_id",
                schema: "inventory",
                table: "consumption_calibration",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "material_size_factors_studio_id_key",
                schema: "inventory",
                table: "material_size_factors",
                column: "studio_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "service_recipe_studio_id_service_id_key",
                schema: "inventory",
                table: "service_recipe",
                columns: new[] { "studio_id", "service_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_recipe_line_item_id",
                schema: "inventory",
                table: "service_recipe_line",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "stock_movement_item_id_created_at_idx",
                schema: "inventory",
                table: "stock_movement",
                columns: new[] { "item_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "stock_movement_studio_type_created_at_idx",
                schema: "inventory",
                table: "stock_movement",
                columns: new[] { "studio_id", "type", "created_at" });

            // Every item's current stock becomes its opening movement, so stock = sum of movements from the start
            migrationBuilder.Sql("""
                INSERT INTO inventory.stock_movement (id, studio_id, item_id, type, quantity, note, created_at, updated_at)
                SELECT gen_random_uuid(), studio_id, id, 'Initial', stock, 'Stan przy wprowadzeniu historii', now(), now()
                FROM inventory.inventory
                WHERE stock <> 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_consumption_line",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "consumption_calibration",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "material_size_factors",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "service_recipe_line",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_movement",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "booking_consumption",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "service_recipe",
                schema: "inventory");
        }
    }
}
