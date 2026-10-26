using Core.Constants;
using Shared.Kernel.Tenancy;
using Core.Models;
using Crm.Infrastructure.Persistence;
using Infrastructure.Data;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests;

/// <summary>
/// Starts a single Postgres container for the whole test run and applies the EF migrations.
/// Migrating fails on pending model changes, so CI catches model edits that lack a migration.
/// Lives in the root namespace so NUnit runs it before every fixture in the assembly.
/// </summary>
[SetUpFixture]
public class PostgresFixture
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString =>
        _container?.GetConnectionString() ?? throw new InvalidOperationException("Postgres container is not running.");

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .Build();

        await _container.StartAsync();

        // Same order as in production: the core baseline creates the tables that modules then adopt
        await using var db = CreateDbContext(new TenantContext());
        await db.Database.MigrateAsync();
        await using var inventoryDb = CreateInventoryDbContext(new TenantContext());
        await inventoryDb.Database.MigrateAsync();
        await using var crmDb = CreateCrmDbContext(new TenantContext());
        await crmDb.Database.MigrateAsync();

        await SeedLookupsAsync(db);
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (_container != null) await _container.DisposeAsync();
    }

    public static ApplicationDbContext CreateDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options, tenantContext);
    }

    public static InventoryDbContext CreateInventoryDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", InventoryDbContext.Schema))
            .Options;

        return new InventoryDbContext(options, tenantContext);
    }

    public static CrmDbContext CreateCrmDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CrmDbContext.Schema))
            .Options;

        return new CrmDbContext(options, tenantContext);
    }

    private static async Task SeedLookupsAsync(ApplicationDbContext db)
    {
        db.Roles.AddRange(
            new Role { Id = Roles.Owner, RoleName = "Owner" },
            new Role { Id = Roles.Manager, RoleName = "Manager" },
            new Role { Id = Roles.Employee, RoleName = "Employee" });

        db.BookingStatuses.AddRange(
            new BookingStatus { Id = BookingStatusIds.Created, Name = "Created", Color = "#3b82f6", SortOrder = 1 },
            new BookingStatus { Id = BookingStatusIds.Completed, Name = "Completed", Color = "#22c55e", SortOrder = 2 },
            new BookingStatus { Id = BookingStatusIds.Cancelled, Name = "Cancelled", Color = "#ef4444", SortOrder = 3 });

        await db.SaveChangesAsync();
    }
}
