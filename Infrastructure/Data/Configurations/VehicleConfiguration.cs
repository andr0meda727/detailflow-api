using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
     public void Configure(EntityTypeBuilder<Vehicle> builder)
     {
          builder.ToTable("vehicle");
          builder.HasKey(x => x.Id);

          builder.Property(e => e.Id).HasColumnName("id");
          builder.Property(e => e.ClientId).HasColumnName("client_id");
          builder.Property(e => e.StudioId).HasColumnName("studio_id");
          builder.Property(e => e.Brand).HasColumnName("brand");
          builder.Property(e => e.Model).HasColumnName("model");
          builder.Property(e => e.LicensePlate).HasColumnName("license_plate");
          builder.Property(e => e.CreatedAt).HasColumnName("created_at");
          builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

          builder.HasOne(x => x.Studio)
               .WithMany(x => x.Vehicles)
               .HasForeignKey(x => x.StudioId)
               .OnDelete(DeleteBehavior.Restrict);

          builder.HasOne(x => x.Client)
               .WithMany(x => x.Vehicles)
               .HasForeignKey(x => x.ClientId)
               .OnDelete(DeleteBehavior.Restrict);
     }
}