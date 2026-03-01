using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.User;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("role_pkey");

        builder
            .ToTable("role", "d");

        builder
            .HasIndex(e => e.RoleName, "role_role_name_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.RoleName)
            .HasColumnName("role_name");
    }
}