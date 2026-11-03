using Shared.Kernel.Tenancy;
using Identity.Domain.Studios;
using Identity.Infrastructure.Persistence;
using Crm.Infrastructure.Persistence;
using BookingStatus = Scheduling.Domain.Bookings.BookingStatus;
using Scheduling.Infrastructure.Persistence;
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
        await using var schedulingDb = CreateSchedulingDbContext(new TenantContext());
        await schedulingDb.Database.MigrateAsync();
        await using var identityDb = CreateIdentityDbContext(new TenantContext());
        await identityDb.Database.MigrateAsync();

        await SeedLookupsAsync(identityDb, schedulingDb);
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

    public static SchedulingDbContext CreateSchedulingDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SchedulingDbContext.Schema))
            .Options;

        return new SchedulingDbContext(options, tenantContext);
    }

    public static IdentityDbContext CreateIdentityDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema))
            .Options;

        return new IdentityDbContext(options, tenantContext);
    }

    private static async Task SeedLookupsAsync(IdentityDbContext identityDb, SchedulingDbContext schedulingDb)
    {
        identityDb.Roles.AddRange(
            new Role(Roles.Owner, "Owner"),
            new Role(Roles.Manager, "Manager"),
            new Role(Roles.Employee, "Employee"));

        await identityDb.SaveChangesAsync();

        schedulingDb.BookingStatuses.AddRange(
            new BookingStatus(BookingStatus.Created, "Created", "#3b82f6", 1),
            new BookingStatus(BookingStatus.Completed, "Completed", "#22c55e", 2),
            new BookingStatus(BookingStatus.Cancelled, "Cancelled", "#ef4444", 3));

        await schedulingDb.SaveChangesAsync();
    }
}
