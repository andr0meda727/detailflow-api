using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleBookingsFromCrm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "booking_client_id_fkey",
                schema: "d",
                table: "booking");

            migrationBuilder.DropForeignKey(
                name: "booking_vehicle_id_fkey",
                schema: "d",
                table: "booking");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "booking_client_id_fkey",
                schema: "d",
                table: "booking",
                column: "client_id",
                principalSchema: "d",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "booking_vehicle_id_fkey",
                schema: "d",
                table: "booking",
                column: "vehicle_id",
                principalSchema: "d",
                principalTable: "vehicle",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
