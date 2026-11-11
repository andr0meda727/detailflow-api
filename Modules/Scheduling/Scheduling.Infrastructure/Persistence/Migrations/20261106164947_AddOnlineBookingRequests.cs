using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineBookingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "customer_user_id",
                schema: "scheduling",
                table: "booking",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_request",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    vehicle_license_plate = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    vehicle_brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vehicle_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vehicle_size = table.Column<string>(type: "text", nullable: false),
                    preferred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estimated_price_cents = table.Column<long>(type: "bigint", nullable: false),
                    has_unpriced_services = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_request_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "booking_request_service",
                schema: "scheduling",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    price_cents = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_request_service_pkey", x => new { x.request_id, x.service_id });
                    table.ForeignKey(
                        name: "booking_request_service_request_id_fkey",
                        column: x => x.request_id,
                        principalSchema: "scheduling",
                        principalTable: "booking_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_customer_user_id",
                schema: "scheduling",
                table: "booking",
                column: "customer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_customer_user_id_created_at",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "customer_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_studio_id_status_created_at",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "studio_id", "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_request_service",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "booking_request",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_booking_customer_user_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "customer_user_id",
                schema: "scheduling",
                table: "booking");
        }
    }
}
