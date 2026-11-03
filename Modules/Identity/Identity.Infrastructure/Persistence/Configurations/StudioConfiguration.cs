using Identity.Domain.Studios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("studio_pkey");

        // stripe_customer_id (and its unique index) belongs to billing, which still maps it; Identity leaves it alone
        builder
            .ToTable("studio", IdentityDbContext.Schema);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.Name).HasColumnName("name");
        builder.Property(e => e.Phone).HasColumnName("phone");
        builder.Property(e => e.Email).HasColumnName("email");
        builder.Property(e => e.Address).HasColumnName("address");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasMany(e => e.Members)
            .WithOne()
            .HasForeignKey(m => m.StudioId)
            .HasConstraintName("studio_member_studio_id_fkey");
    }
}

public class StudioMemberConfiguration : IEntityTypeConfiguration<StudioMember>
{
    public void Configure(EntityTypeBuilder<StudioMember> builder)
    {
        builder
            .HasKey(e => new { e.UserId, e.StudioId })
            .HasName("studio_member_pkey");

        builder
            .ToTable("studio_member", IdentityDbContext.Schema);

        builder.HasIndex(e => e.StudioId, "IX_studio_member_studio_id");
        builder.HasIndex(e => e.RoleId, "IX_studio_member_role_id");

        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("studio_member_role_id_fkey");

        builder
            .HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("studio_member_user_id_fkey");
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("role_pkey");

        builder
            .ToTable("role", IdentityDbContext.Schema);

        builder
            .HasIndex(e => e.Name, "role_role_name_key")
            .IsUnique();

        // The rows are fixed (Roles.Owner/Manager/Employee), so the IDs are always given
        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(e => e.Name).HasColumnName("role_name");
    }
}
