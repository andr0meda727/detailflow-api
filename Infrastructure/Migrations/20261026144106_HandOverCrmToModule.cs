using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandOverCrmToModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The client and vehicle tables are not dropped: ownership moves to the
            // CRM module, whose CrmDbContext migrations take them over (and move them to the "crm" schema).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty, see Up.
        }
    }
}
