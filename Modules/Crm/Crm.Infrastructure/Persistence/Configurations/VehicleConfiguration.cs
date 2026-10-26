using Crm.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crm.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("vehicle_pkey");

        builder
            .ToTable("vehicle", CrmDbContext.Schema);

        // Every query is filtered by studio
        builder.HasIndex(e => e.StudioId, "IX_vehicle_studio_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.ClientId)
            .HasColumnName("client_id");

        builder.Property(e => e.LicensePlate)
            .HasColumnName("license_plate");

        builder.Property(e => e.Brand)
            .HasColumnName("brand");

        builder.Property(e => e.Model)
            .HasColumnName("model");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}
