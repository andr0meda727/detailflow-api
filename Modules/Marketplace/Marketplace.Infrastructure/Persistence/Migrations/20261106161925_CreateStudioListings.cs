using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateStudioListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketplace");

            migrationBuilder.CreateTable(
                name: "studio_listing",
                schema: "marketplace",
                columns: table => new
                {
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    catalog_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("studio_listing_pkey", x => x.studio_id);
                    table.CheckConstraint("CK_studio_listing_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_studio_listing_longitude", "longitude BETWEEN -180 AND 180");
                });

            migrationBuilder.CreateTable(
                name: "listing_price",
                schema: "marketplace",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_size = table.Column<string>(type: "text", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("listing_price_pkey", x => new { x.studio_id, x.service_id, x.vehicle_size });
                    table.ForeignKey(
                        name: "listing_price_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "marketplace",
                        principalTable: "studio_listing",
                        principalColumn: "studio_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_service",
                schema: "marketplace",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("listing_service_pkey", x => new { x.studio_id, x.service_id });
                    table.ForeignKey(
                        name: "listing_service_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "marketplace",
                        principalTable: "studio_listing",
                        principalColumn: "studio_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_service_category",
                schema: "marketplace",
                table: "listing_service",
                columns: new[] { "category", "studio_id" });

            migrationBuilder.CreateIndex(
                name: "IX_studio_listing_location",
                schema: "marketplace",
                table: "studio_listing",
                columns: new[] { "latitude", "longitude" },
                filter: "is_published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_price",
                schema: "marketplace");

            migrationBuilder.DropTable(
                name: "listing_service",
                schema: "marketplace");

            migrationBuilder.DropTable(
                name: "studio_listing",
                schema: "marketplace");
        }
    }
}
