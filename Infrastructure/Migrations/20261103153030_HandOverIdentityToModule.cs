using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandOverIdentityToModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The role, user, studio, studio_member and studio_invitation tables are not
            // dropped: ownership moves to the Identity module, whose IdentityDbContext migrations take them over
            // (and move them to the "identity" schema). Billing's foreign keys to the studio stay in the database.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty, see Up.
        }
    }
}
