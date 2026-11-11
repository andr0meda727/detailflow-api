using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "bay_id",
                schema: "scheduling",
                table: "booking",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ends_at",
                schema: "scheduling",
                table: "booking",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing bookings last as long as their services (an hour each until studios set durations), at least an hour
            migrationBuilder.Sql("""
                UPDATE scheduling.booking b
                SET ends_at = b.scheduled_at + make_interval(mins => GREATEST(60, COALESCE((
                    SELECT SUM(s.duration_minutes)
                    FROM scheduling.booking_service bs
                    JOIN scheduling.service s ON s.id = bs.service_id
                    WHERE bs.booking_id = b.id), 60))::int);
                """);

            migrationBuilder.CreateIndex(
                name: "booking_bay_id_scheduled_at_idx",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "bay_id", "scheduled_at" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_booking_ends_after_start",
                schema: "scheduling",
                table: "booking",
                sql: "ends_at > scheduled_at");

            migrationBuilder.AddForeignKey(
                name: "booking_bay_id_fkey",
                schema: "scheduling",
                table: "booking",
                column: "bay_id",
                principalSchema: "scheduling",
                principalTable: "bay",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Two bookings that aren't cancelled can't hold one bay at the same time. Ranges are half-open, so a visit may
            // start when the previous one ends; bookings without a bay never conflict. btree_gist lets the gist index
            // compare the bay's uuid with "=". The ID is BookingStatus.Cancelled.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
                ALTER TABLE scheduling.booking
                ADD CONSTRAINT booking_no_bay_overlap
                EXCLUDE USING gist (bay_id WITH =, tstzrange(scheduled_at, ends_at, '[)') WITH &&)
                WHERE (bay_id IS NOT NULL AND status_id <> 'd89cf9d3-ee24-481a-ac76-c5cb224f1c1a');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The extension stays: other objects may use it
            migrationBuilder.Sql("ALTER TABLE scheduling.booking DROP CONSTRAINT IF EXISTS booking_no_bay_overlap;");

            migrationBuilder.DropForeignKey(
                name: "booking_bay_id_fkey",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "booking_bay_id_scheduled_at_idx",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropCheckConstraint(
                name: "CK_booking_ends_after_start",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "bay_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "ends_at",
                schema: "scheduling",
                table: "booking");
        }
    }
}
