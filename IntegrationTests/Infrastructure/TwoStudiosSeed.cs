using Core.Constants;
using Core.Models;
using Core.Services;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Seeds two independent studios, each with one record of every tenant-scoped entity.
/// </summary>
public sealed class TwoStudiosSeed
{
    public StudioData StudioA { get; } = new(Guid.NewGuid());
    public StudioData StudioB { get; } = new(Guid.NewGuid());

    // The same person is a client of both studios, which must be allowed
    public string SharedClientPhone { get; } = Random.Shared.Next(100_000_000, 999_999_999).ToString();

    public async Task SeedAsync()
    {
        foreach (var studio in new[] { StudioA, StudioB })
        {
            // Seeding is done with an explicit tenant so SaveChanges stamps the right StudioId.
            var tenant = new TenantContext();
            tenant.SetTenant(studio.StudioId, null);
            await using var db = PostgresFixture.CreateDbContext(tenant);

            db.Studios.Add(new Studio { Id = studio.StudioId, Name = $"Studio {studio.StudioId:N}" });

            db.Users.Add(new User { Id = studio.OwnerUserId, Email = $"owner-{studio.OwnerUserId:N}@test.pl", IsActive = true });
            db.StudioMembers.Add(new StudioMember
            {
                UserId = studio.OwnerUserId, StudioId = studio.StudioId, RoleId = Roles.Owner, IsActive = true
            });

            var category = new InventoryCategory { Id = studio.InventoryCategoryId, Name = "Chemicals" };
            db.InventoryCategories.Add(category);
            db.Inventories.Add(new Inventory
            {
                Id = studio.InventoryId, CategoryId = category.Id, Name = "Wax", Unit = "l", Stock = 1, MinStock = 5
            });

            db.Services.Add(new Service { Id = studio.ServiceId, Name = "Polishing" });

            db.Clients.Add(new Client { Id = studio.ClientId, Name = "Jan", Phone = SharedClientPhone });
            db.Vehicles.Add(new Vehicle { Id = studio.VehicleId, ClientId = studio.ClientId, LicensePlate = "WA12345" });

            db.Bookings.Add(new Booking
            {
                Id = studio.BookingId,
                ClientId = studio.ClientId,
                VehicleId = studio.VehicleId,
                StatusId = BookingStatusIds.Created,
                TotalPrice = 20000,
                ScheduledAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }

    public sealed record StudioData(Guid StudioId)
    {
        public Guid OwnerUserId { get; } = Guid.NewGuid();
        public Guid InventoryCategoryId { get; } = Guid.NewGuid();
        public Guid InventoryId { get; } = Guid.NewGuid();
        public Guid ServiceId { get; } = Guid.NewGuid();
        public Guid ClientId { get; } = Guid.NewGuid();
        public Guid VehicleId { get; } = Guid.NewGuid();
        public Guid BookingId { get; } = Guid.NewGuid();
    }
}
