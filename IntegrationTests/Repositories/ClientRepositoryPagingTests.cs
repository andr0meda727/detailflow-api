using Core.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Pagination;

namespace IntegrationTests.Repositories;

[TestFixture]
public class ClientRepositoryPagingTests
{
    private TwoStudiosSeed _seed = null!;
    private readonly string _token = Guid.NewGuid().ToString("N")[..8];

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();

        await using var db = DbForStudioA();
        foreach (var suffix in new[] { "C", "A", "B" })
        {
            db.Clients.Add(new Client
            {
                Name = $"{_token} {suffix}",
                Phone = Random.Shared.Next(100_000_000, 999_999_999).ToString()
            });
        }
        await db.SaveChangesAsync();
    }

    private ApplicationDbContext DbForStudioA()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(_seed.StudioA.StudioId, _seed.StudioA.OwnerUserId);
        return PostgresFixture.CreateDbContext(tenant);
    }

    [Test]
    public async Task Search_IsCountedBeforePaging()
    {
        await using var db = DbForStudioA();
        var repository = new ClientRepository(db);

        var first = await repository.GetAllWithVehiclesAsync(new PageRequest { Page = 1, PageSize = 2 }, _token);
        var second = await repository.GetAllWithVehiclesAsync(new PageRequest { Page = 2, PageSize = 2 }, _token);

        Assert.That(first.TotalCount, Is.EqualTo(3));
        Assert.That(first.Items.Concat(second.Items).Select(c => c.Name),
            Is.EqualTo(new[] { $"{_token} A", $"{_token} B", $"{_token} C" }));
    }
}
