using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                schema: "scheduling",
                table: "service",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddCheckConstraint(
                name: "CK_service_duration_minutes",
                schema: "scheduling",
                table: "service",
                sql: "duration_minutes BETWEEN 5 AND 720");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_service_duration_minutes",
                schema: "scheduling",
                table: "service");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                schema: "scheduling",
                table: "service");
        }
    }
}
