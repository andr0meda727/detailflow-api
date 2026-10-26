using Core.Constants;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IntegrationTests;

/// <summary>
/// Starts a single Postgres container for the whole test run and creates the schema from the EF model.
/// (EnsureCreated rather than migrations: the migrations folder is behind the model.)
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
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();

        await _container.StartAsync();

        await using var db = CreateDbContext(new TenantContext());
        await db.Database.EnsureCreatedAsync();
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

    private static async Task SeedLookupsAsync(ApplicationDbContext db)
    {
        db.Roles.AddRange(
            new Role { Id = Roles.Owner, RoleName = "Owner" },
            new Role { Id = Roles.Manager, RoleName = "Manager" },
            new Role { Id = Roles.Employee, RoleName = "Employee" });

        db.BookingStatuses.AddRange(
            new BookingStatus { Id = BookingStatusIds.Created, Name = "Created", SortOrder = 1 },
            new BookingStatus { Id = BookingStatusIds.Completed, Name = "Completed", SortOrder = 2 },
            new BookingStatus { Id = BookingStatusIds.Cancelled, Name = "Cancelled", SortOrder = 3 });

        await db.SaveChangesAsync();
    }
}
