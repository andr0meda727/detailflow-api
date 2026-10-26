using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CopyClientAndVehicleOntoBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_email",
                schema: "d",
                table: "booking",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_name",
                schema: "d",
                table: "booking",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "client_phone",
                schema: "d",
                table: "booking",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "vehicle_brand",
                schema: "d",
                table: "booking",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_license_plate",
                schema: "d",
                table: "booking",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "vehicle_model",
                schema: "d",
                table: "booking",
                type: "text",
                nullable: true);

            // Backfill existing bookings, including ones whose client or vehicle was soft-deleted
            migrationBuilder.Sql("""
                UPDATE d.booking b
                SET client_name = c.name, client_phone = c.phone, client_email = c.email
                FROM d.client c
                WHERE c.id = b.client_id;

                UPDATE d.booking b
                SET vehicle_license_plate = v.license_plate, vehicle_brand = v.brand, vehicle_model = v.model
                FROM d.vehicle v
                WHERE v.id = b.vehicle_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_email",
                schema: "d",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "client_name",
                schema: "d",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "client_phone",
                schema: "d",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "vehicle_brand",
                schema: "d",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "vehicle_license_plate",
                schema: "d",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "vehicle_model",
                schema: "d",
                table: "booking");
        }
    }
}
