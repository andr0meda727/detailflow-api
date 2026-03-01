using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("studio_pkey");

        builder
            .ToTable("studio", "d");

        builder
            .HasIndex(e => e.StripeCustomerId, "studio_stripe_customer_id_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.Address)
            .HasColumnName("address");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.Email)
            .HasColumnName("email");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.Phone)
            .HasColumnName("phone");

        builder.Property(e => e.StripeCustomerId)
            .HasColumnName("stripe_customer_id");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}