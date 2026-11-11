using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "d");

            migrationBuilder.CreateTable(
                name: "booking_status",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_status_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("role_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studio",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    stripe_customer_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("studio_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plan",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    stripe_product_id = table.Column<string>(type: "text", nullable: false),
                    stripe_price_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'pln'::text"),
                    interval = table.Column<string>(type: "text", nullable: false),
                    interval_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_users = table.Column<int>(type: "integer", nullable: true),
                    max_vehicles = table.Column<int>(type: "integer", nullable: true),
                    max_bookings_per_month = table.Column<int>(type: "integer", nullable: true),
                    features = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscription_plan_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: false),
                    nip = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_category",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("inventory_category_pkey", x => x.id);
                    table.ForeignKey(
                        name: "inventory_category_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_pkey", x => x.id);
                    table.ForeignKey(
                        name: "service_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Table contains services that studio can choose for booking purposes. Every studio is filled with default services (studio_id null). But there is option to add its own service, in this situation the studio_id is equal to id of the studio that add this service.");

            migrationBuilder.CreateTable(
                name: "studio_subscription",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_subscription_id = table.Column<string>(type: "text", nullable: false),
                    stripe_status = table.Column<string>(type: "text", nullable: false),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
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
                    table.ForeignKey(
                        name: "studio_subscription_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "studio_invitation",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    status = table.Column<string>(type: "text", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("studio_invitation_pkey", x => x.id);
                    table.ForeignKey(
                        name: "studio_invitation_invited_by_fkey",
                        column: x => x.invited_by,
                        principalSchema: "d",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "studio_invitation_role_id_fkey",
                        column: x => x.role_id,
                        principalSchema: "d",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "studio_invitation_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "studio_member",
                schema: "d",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("studio_member_pkey", x => new { x.user_id, x.studio_id });
                    table.ForeignKey(
                        name: "studio_member_role_id_fkey",
                        column: x => x.role_id,
                        principalSchema: "d",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "studio_member_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "studio_member_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "d",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    license_plate = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("vehicle_pkey", x => x.id);
                    table.ForeignKey(
                        name: "vehicle_client_id_fkey",
                        column: x => x.client_id,
                        principalSchema: "d",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "vehicle_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "text", nullable: false),
                    stock = table.Column<decimal>(type: "numeric", nullable: false),
                    min_stock = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("inventory_pkey", x => x.id);
                    table.ForeignKey(
                        name: "inventory_category_id_fkey",
                        column: x => x.category_id,
                        principalSchema: "d",
                        principalTable: "inventory_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "inventory_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "text", nullable: true),
                    stripe_invoice_id = table.Column<string>(type: "text", nullable: true),
                    stripe_charge_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'pln'::text"),
                    status = table.Column<string>(type: "text", nullable: false),
                    payment_method_type = table.Column<string>(type: "text", nullable: true),
                    failure_code = table.Column<string>(type: "text", nullable: true),
                    failure_message = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_pkey", x => x.id);
                    table.ForeignKey(
                        name: "payment_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    stripe_event_id = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("stripe_event_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stripe_event_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "stripe_event_subscription_id_fkey",
                        column: x => x.subscription_id,
                        principalSchema: "d",
                        principalTable: "studio_subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "booking",
                schema: "d",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_price = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_pkey", x => x.id);
                    table.ForeignKey(
                        name: "booking_client_id_fkey",
                        column: x => x.client_id,
                        principalSchema: "d",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "booking_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "d",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "booking_status_id_fkey",
                        column: x => x.status_id,
                        principalSchema: "d",
                        principalTable: "booking_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "booking_studio_id_fkey",
                        column: x => x.studio_id,
                        principalSchema: "d",
                        principalTable: "studio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "booking_vehicle_id_fkey",
                        column: x => x.vehicle_id,
                        principalSchema: "d",
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_service",
                schema: "d",
                columns: table => new
                {
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_service_pkey", x => new { x.booking_id, x.service_id });
                    table.ForeignKey(
                        name: "booking_service_booking_id_fkey",
                        column: x => x.booking_id,
                        principalSchema: "d",
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "booking_service_service_id_fkey",
                        column: x => x.service_id,
                        principalSchema: "d",
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_client_id",
                schema: "d",
                table: "booking",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_created_by",
                schema: "d",
                table: "booking",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_id",
                schema: "d",
                table: "booking",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_studio_id",
                schema: "d",
                table: "booking",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_vehicle_id",
                schema: "d",
                table: "booking",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_service_service_id",
                schema: "d",
                table: "booking_service",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "booking_status_name_key",
                schema: "d",
                table: "booking_status",
                column: "name",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_inventory_category_id",
                schema: "d",
                table: "inventory",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_studio_id",
                schema: "d",
                table: "inventory",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_category_studio_id",
                schema: "d",
                table: "inventory_category",
                column: "studio_id");

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
                name: "role_role_name_key",
                schema: "d",
                table: "role",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_studio_id",
                schema: "d",
                table: "service",
                column: "studio_id");

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
                name: "studio_stripe_customer_id_key",
                schema: "d",
                table: "studio",
                column: "stripe_customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studio_invitation_invited_by",
                schema: "d",
                table: "studio_invitation",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "IX_studio_invitation_role_id",
                schema: "d",
                table: "studio_invitation",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_studio_invitation_studio_id_email",
                schema: "d",
                table: "studio_invitation",
                columns: new[] { "studio_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studio_invitation_token",
                schema: "d",
                table: "studio_invitation",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studio_member_role_id",
                schema: "d",
                table: "studio_member",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_studio_member_studio_id",
                schema: "d",
                table: "studio_member",
                column: "studio_id");

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

            migrationBuilder.CreateIndex(
                name: "user_email_key",
                schema: "d",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_client_id",
                schema: "d",
                table: "vehicle",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_studio_id",
                schema: "d",
                table: "vehicle",
                column: "studio_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_service",
                schema: "d");

            migrationBuilder.DropTable(
                name: "inventory",
                schema: "d");

            migrationBuilder.DropTable(
                name: "payment",
                schema: "d");

            migrationBuilder.DropTable(
                name: "stripe_event",
                schema: "d");

            migrationBuilder.DropTable(
                name: "studio_invitation",
                schema: "d");

            migrationBuilder.DropTable(
                name: "studio_member",
                schema: "d");

            migrationBuilder.DropTable(
                name: "booking",
                schema: "d");

            migrationBuilder.DropTable(
                name: "service",
                schema: "d");

            migrationBuilder.DropTable(
                name: "inventory_category",
                schema: "d");

            migrationBuilder.DropTable(
                name: "studio_subscription",
                schema: "d");

            migrationBuilder.DropTable(
                name: "role",
                schema: "d");

            migrationBuilder.DropTable(
                name: "user",
                schema: "d");

            migrationBuilder.DropTable(
                name: "booking_status",
                schema: "d");

            migrationBuilder.DropTable(
                name: "vehicle",
                schema: "d");

            migrationBuilder.DropTable(
                name: "subscription_plan",
                schema: "d");

            migrationBuilder.DropTable(
                name: "client",
                schema: "d");

            migrationBuilder.DropTable(
                name: "studio",
                schema: "d");
        }
    }
}
