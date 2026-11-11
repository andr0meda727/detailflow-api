using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBaysAndSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bay",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("bay_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studio_schedule",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    slot_step_minutes = table.Column<int>(type: "integer", nullable: false),
                    min_lead_hours = table.Column<int>(type: "integer", nullable: false),
                    small_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    medium_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    large_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    xl_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("studio_schedule_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "closure",
                schema: "scheduling",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("closure_pkey", x => new { x.schedule_id, x.date });
                    table.ForeignKey(
                        name: "closure_schedule_id_fkey",
                        column: x => x.schedule_id,
                        principalSchema: "scheduling",
                        principalTable: "studio_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opening_period",
                schema: "scheduling",
                columns: table => new
                {
                    day = table.Column<string>(type: "text", nullable: false),
                    opens = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closes = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("opening_period_pkey", x => new { x.schedule_id, x.day, x.opens });
                    table.CheckConstraint("CK_opening_period_order", "closes > opens");
                    table.ForeignKey(
                        name: "opening_period_schedule_id_fkey",
                        column: x => x.schedule_id,
                        principalSchema: "scheduling",
                        principalTable: "studio_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bay_studio_id",
                schema: "scheduling",
                table: "bay",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "IX_studio_schedule_studio_id",
                schema: "scheduling",
                table: "studio_schedule",
                column: "studio_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bay",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "closure",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "opening_period",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "studio_schedule",
                schema: "scheduling");
        }
    }
}
