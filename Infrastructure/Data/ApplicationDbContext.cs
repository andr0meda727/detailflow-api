using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Infrastructure.Data;

/// <summary>
/// Holds the core migrations: the Baseline that created the original "d" schema, and the steps that hand its
/// tables over to the modules. It maps no entities any more; every table belongs to a module.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
    }
}
