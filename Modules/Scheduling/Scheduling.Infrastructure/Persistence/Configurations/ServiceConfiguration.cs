using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Catalog;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("service_pkey");

        // The comment is kept as it is in the database, so adopting the table changes nothing
        builder
            .ToTable("service", SchedulingDbContext.Schema, tb =>
                tb.HasComment("Table contains services that studio can choose for booking purposes. Every studio is filled with default services (studio_id null). But there is option to add its own service, in this situation the studio_id is equal to id of the studio that add this service."));

        // Every query is filtered by studio
        builder.HasIndex(e => e.StudioId, "IX_service_studio_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Name).HasColumnName("name");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}
