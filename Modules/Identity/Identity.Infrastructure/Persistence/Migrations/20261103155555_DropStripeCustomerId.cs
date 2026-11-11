using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropStripeCustomerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Billing was removed (the app is free for now). Its Stripe customer ID on the studio was never part of
            // this model; dropping the column also drops its unique index studio_stripe_customer_id_key.
            migrationBuilder.Sql("ALTER TABLE identity.studio DROP COLUMN IF EXISTS stripe_customer_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE identity.studio ADD COLUMN IF NOT EXISTS stripe_customer_id text;
                CREATE UNIQUE INDEX IF NOT EXISTS studio_stripe_customer_id_key ON identity.studio (stripe_customer_id);
                """);
        }
    }
}
