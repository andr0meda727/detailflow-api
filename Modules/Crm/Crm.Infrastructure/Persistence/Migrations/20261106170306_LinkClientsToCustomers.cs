using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkClientsToCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "customer_user_id",
                schema: "crm",
                table: "client",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_studio_id_customer_user_id",
                schema: "crm",
                table: "client",
                columns: new[] { "studio_id", "customer_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_client_studio_id_customer_user_id",
                schema: "crm",
                table: "client");

            migrationBuilder.DropColumn(
                name: "customer_user_id",
                schema: "crm",
                table: "client");
        }
    }
}
