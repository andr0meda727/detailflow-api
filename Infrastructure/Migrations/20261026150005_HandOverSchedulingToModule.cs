using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandOverSchedulingToModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The booking, booking_service, booking_status and service tables are not
            // dropped: ownership moves to the Scheduling module, whose SchedulingDbContext migrations take
            // them over (and move them to the "scheduling" schema).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty, see Up.
        }
    }
}
