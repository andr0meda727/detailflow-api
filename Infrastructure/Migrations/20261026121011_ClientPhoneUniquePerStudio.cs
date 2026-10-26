using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClientPhoneUniquePerStudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "client_phone_key",
                schema: "d",
                table: "client");

            migrationBuilder.DropIndex(
                name: "IX_client_studio_id",
                schema: "d",
                table: "client");

            migrationBuilder.CreateIndex(
                name: "client_studio_id_phone_key",
                schema: "d",
                table: "client",
                columns: new[] { "studio_id", "phone" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "client_studio_id_phone_key",
                schema: "d",
                table: "client");

            migrationBuilder.CreateIndex(
                name: "client_phone_key",
                schema: "d",
                table: "client",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_studio_id",
                schema: "d",
                table: "client",
                column: "studio_id");
        }
    }
}
