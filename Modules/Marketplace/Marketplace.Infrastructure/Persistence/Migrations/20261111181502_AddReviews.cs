using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reviewable_visit",
                schema: "marketplace",
                columns: table => new
                {
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("reviewable_visit_pkey", x => x.booking_id);
                });

            migrationBuilder.CreateTable(
                name: "review",
                schema: "marketplace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("review_pkey", x => x.id);
                    table.CheckConstraint("CK_review_rating", "rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "review_booking_id_fkey",
                        column: x => x.booking_id,
                        principalSchema: "marketplace",
                        principalTable: "reviewable_visit",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "review_booking_id_key",
                schema: "marketplace",
                table: "review",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "review_studio_id_created_at_idx",
                schema: "marketplace",
                table: "review",
                columns: new[] { "studio_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "reviewable_visit_customer_completed_idx",
                schema: "marketplace",
                table: "reviewable_visit",
                columns: new[] { "customer_user_id", "completed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review",
                schema: "marketplace");

            migrationBuilder.DropTable(
                name: "reviewable_visit",
                schema: "marketplace");
        }
    }
}
