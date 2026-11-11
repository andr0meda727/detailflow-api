using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence.Configurations;

public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("customer_profile");

        // The Supabase user ID; no foreign key into Identity's tables (modules hold plain IDs)
        builder.HasKey(e => e.UserId).HasName("customer_profile_pkey");
        builder.Property(e => e.UserId).ValueGeneratedNever().HasColumnName("user_id");

        builder.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.Phone).HasMaxLength(30).HasColumnName("phone");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
    }
}

public class CustomerVehicleConfiguration : IEntityTypeConfiguration<CustomerVehicle>
{
    public void Configure(EntityTypeBuilder<CustomerVehicle> builder)
    {
        builder.ToTable("customer_vehicle");

        builder.HasKey(e => e.Id).HasName("customer_vehicle_pkey");
        builder.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");

        // One car per plate in a garage; also the index for "my vehicles"
        builder.HasIndex(e => new { e.UserId, e.LicensePlate }, "IX_customer_vehicle_user_id_license_plate").IsUnique();

        builder.Property(e => e.LicensePlate).HasMaxLength(15).HasColumnName("license_plate");
        builder.Property(e => e.Brand).HasMaxLength(50).HasColumnName("brand");
        builder.Property(e => e.Model).HasMaxLength(50).HasColumnName("model");
        builder.Property(e => e.Size).HasConversion<string>().HasColumnName("size");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
    }
}
