using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingStudioScheduleIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The composite index covers the studio_id foreign key. IF EXISTS because databases
            // adopted from the hand-made schema may never have had the convention index.
            migrationBuilder.Sql("DROP INDEX IF EXISTS d.\"IX_booking_studio_id\";");

            migrationBuilder.CreateIndex(
                name: "booking_studio_id_scheduled_at_idx",
                schema: "d",
                table: "booking",
                columns: new[] { "studio_id", "scheduled_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "booking_studio_id_scheduled_at_idx",
                schema: "d",
                table: "booking");

            migrationBuilder.CreateIndex(
                name: "IX_booking_studio_id",
                schema: "d",
                table: "booking",
                column: "studio_id");
        }
    }
}
