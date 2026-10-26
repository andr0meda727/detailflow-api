using Core.Constants;
using Core.Models;
using Inventory.Domain;
using Shared.Infrastructure.Tenancy;
using CrmClient = Crm.Domain.Client;

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

            db.Services.Add(new Service { Id = studio.ServiceId, Name = "Polishing" });

            await db.SaveChangesAsync();

            // Module tables still reference d.studio, so they are seeded after the studio exists
            await using var crmDb = PostgresFixture.CreateCrmDbContext(tenant);
            var client = CrmClient.Create("Jan", SharedClientPhone, email: null, nip: null);
            var vehicle = client.AddVehicle("WA12345", brand: null, model: null);
            crmDb.Clients.Add(client);
            studio.ClientId = client.Id;
            studio.VehicleId = vehicle.Id;
            await crmDb.SaveChangesAsync();

            db.Bookings.Add(new Booking
            {
                Id = studio.BookingId,
                ClientId = studio.ClientId,
                VehicleId = studio.VehicleId,
                ClientName = "Jan",
                ClientPhone = SharedClientPhone,
                VehicleLicensePlate = "WA12345",
                StatusId = BookingStatusIds.Created,
                TotalPrice = 20000,
                ScheduledAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            await using var inventoryDb = PostgresFixture.CreateInventoryDbContext(tenant);
            var category = InventoryCategory.Create("Chemicals");
            var item = InventoryItem.Create("Wax", "l", stock: 1, minStock: 5, description: null, categoryId: category.Id);
            inventoryDb.Categories.Add(category);
            inventoryDb.Items.Add(item);
            studio.InventoryCategoryId = category.Id;
            studio.InventoryId = item.Id;
            await inventoryDb.SaveChangesAsync();
        }
    }

    public sealed record StudioData(Guid StudioId)
    {
        public Guid OwnerUserId { get; } = Guid.NewGuid();
        public Guid InventoryCategoryId { get; set; }
        public Guid InventoryId { get; set; }
        public Guid ServiceId { get; } = Guid.NewGuid();
        public Guid ClientId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid BookingId { get; } = Guid.NewGuid();
    }
}
