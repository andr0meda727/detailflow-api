using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<Core.Models.User>
{
    public void Configure(EntityTypeBuilder<Core.Models.User> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("user_pkey");

        builder
            .ToTable("user", "d");

        builder
            .HasIndex(e => e.Email, "user_email_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.Email)
            .HasColumnName("email");

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(e => e.LastName)
            .HasColumnName("last_name");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}