using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Bookings;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("service");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Name).HasColumnName("name");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsActive).HasColumnName("is_active");


        builder.HasOne(x => x.Studio)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}