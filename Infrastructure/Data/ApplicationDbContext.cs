using Core.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext)
{
    public DbSet<Payment> Payments { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<StripeEvent> StripeEvents { get; set; }

    public DbSet<Studio> Studios { get; set; }

    public DbSet<StudioMember> StudioMembers { get; set; }

    public DbSet<StudioSubscription> StudioSubscriptions { get; set; }

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<StudioInvitation> StudioInvitations { get; set; }

    // Studio scoping is added by TenantDbContext
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
