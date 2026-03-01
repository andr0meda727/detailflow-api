using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Bookings;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("service_pkey");

        builder
            .ToTable("service", "d", tb =>
            tb.HasComment("Table contains services that studio can choose for booking purposes. Every studio is filled with default services (studio_id null). But there is option to add its own service, in this situation the studio_id is equal to id of the studio that add this service."));

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne(d => d.Studio)
            .WithMany(p => p.Services)
            .HasForeignKey(d => d.StudioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("service_studio_id_fkey");
    }
}