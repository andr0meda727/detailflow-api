using Shared.Infrastructure.Tenancy;

namespace Unit.Shared;

/// <summary>
/// The tenant is set once per request by the middleware; nothing later in the request may switch studios.
/// </summary>
[TestFixture]
public class TenantContextTests
{
    private static readonly Guid StudioA = Guid.NewGuid();
    private static readonly Guid StudioB = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    [Test]
    public void StartsWithoutStudioOrUser()
    {
        var tenant = new TenantContext();

        Assert.That(tenant.StudioId, Is.Null);
        Assert.That(tenant.UserId, Is.Null);
    }

    [Test]
    public void SetTenant_CannotBeOverwritten()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(StudioA, User);

        tenant.SetTenant(StudioB, Guid.NewGuid());
        tenant.SetUserOnly(Guid.NewGuid());

        Assert.That(tenant.StudioId, Is.EqualTo(StudioA));
        Assert.That(tenant.UserId, Is.EqualTo(User));
    }

    [Test]
    public void SetUserOnly_LeavesTheStudioEmpty()
    {
        var tenant = new TenantContext();

        tenant.SetUserOnly(User);

        Assert.That(tenant.StudioId, Is.Null);
        Assert.That(tenant.UserId, Is.EqualTo(User));
    }
}
