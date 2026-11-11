using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment",
                schema: "d");

            migrationBuilder.DropTable(
                name: "stripe_event",
                schema: "d");

            migrationBuilder.DropTable(
                name: "studio_subscription",
                schema: "d");

            migrationBuilder.DropTable(
                name: "subscription_plan",
                schema: "d");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "d");

            migrationBuilder.CreateTable(
                name: "subscription_plan",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    currency = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'pln'::text"),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    features = table.Column<string>(type: "jsonb", nullable: true),
                    interval = table.Column<string>(type: "text", nullable: false),
                    interval_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    max_bookings_per_month = table.Column<int>(type: "integer", nullable: true),
                    max_users = table.Column<int>(type: "integer", nullable: true),
                    max_vehicles = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    stripe_price_id = table.Column<string>(type: "text", nullable: false),
                    stripe_product_id = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscription_plan_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studio_subscription",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stripe_status = table.Column<string>(type: "text", nullable: false),
                    stripe_subscription_id = table.Column<string>(type: "text", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trial_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("studio_subscription_pkey", x => x.id);
                    table.ForeignKey(
                        name: "studio_subscription_plan_id_fkey",
                        column: x => x.plan_id,
                        principalSchema: "d",
                        principalTable: "subscription_plan",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payment",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    currency = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'pln'::text"),
                    failure_code = table.Column<string>(type: "text", nullable: true),
                    failure_message = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_method_type = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    stripe_charge_id = table.Column<string>(type: "text", nullable: true),
                    stripe_invoice_id = table.Column<string>(type: "text", nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "text", nullable: true),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_pkey", x => x.id);
                    table.ForeignKey(
                        name: "payment_subscription_id_fkey",
                        column: x => x.subscription_id,
                        principalSchema: "d",
                        principalTable: "studio_subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stripe_event",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stripe_event_id = table.Column<string>(type: "text", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stripe_event_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stripe_event_subscription_id_fkey",
                        column: x => x.subscription_id,
                        principalSchema: "d",
                        principalTable: "studio_subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_studio_id",
                schema: "d",
                table: "payment",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_subscription_id",
                schema: "d",
                table: "payment",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "payment_stripe_payment_intent_id_key",
                schema: "d",
                table: "payment",
                column: "stripe_payment_intent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_event_studio_id",
                schema: "d",
                table: "stripe_event",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_event_subscription_id",
                schema: "d",
                table: "stripe_event",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "stripe_event_stripe_event_id_key",
                schema: "d",
                table: "stripe_event",
                column: "stripe_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studio_subscription_plan_id",
                schema: "d",
                table: "studio_subscription",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_studio_subscription_studio_id",
                schema: "d",
                table: "studio_subscription",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "studio_subscription_stripe_subscription_id_key",
                schema: "d",
                table: "studio_subscription",
                column: "stripe_subscription_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "subscription_plan_stripe_price_id_key",
                schema: "d",
                table: "subscription_plan",
                column: "stripe_price_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "subscription_plan_stripe_product_id_key",
                schema: "d",
                table: "subscription_plan",
                column: "stripe_product_id",
                unique: true);
        }
    }
}
