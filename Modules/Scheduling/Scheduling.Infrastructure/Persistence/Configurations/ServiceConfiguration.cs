using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Contracts;
using Scheduling.Domain.Catalog;
using Shared.Kernel.Vehicles;

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
            {
                tb.HasCheckConstraint("CK_service_duration_minutes", "duration_minutes BETWEEN 5 AND 720");
                tb.HasComment("Table contains services that studio can choose for booking purposes. Every studio is filled with default services (studio_id null). But there is option to add its own service, in this situation the studio_id is equal to id of the studio that add this service.");
            });

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

        // Existing services take an hour until the studio says otherwise
        builder.Property(e => e.DurationMinutes)
            .HasDefaultValue(Service.DefaultDurationMinutes)
            .HasSentinel(0)
            .HasColumnName("duration_minutes");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        // Stored by name so the table reads well and reordering the enum changes nothing
        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasDefaultValue(ServiceCategory.Other)
            .HasSentinel(ServiceCategory.Other)
            .HasColumnName("category");

        // One row per vehicle size that has a price; the list is replaced as a whole on update
        builder.OwnsMany(e => e.Prices, price =>
        {
            price.ToTable("service_price", SchedulingDbContext.Schema, tb =>
                tb.HasCheckConstraint("CK_service_price_amount_cents", "amount_cents >= 0"));

            price.WithOwner().HasForeignKey("ServiceId").HasConstraintName("service_price_service_id_fkey");
            price.Property<Guid>("ServiceId").HasColumnName("service_id");
            price.HasKey("ServiceId", nameof(ServicePrice.Size)).HasName("service_price_pkey");

            price.Property(p => p.Size).HasConversion<string>().HasColumnName("vehicle_size");
            price.Property(p => p.AmountInCents).HasColumnName("amount_cents");
        });

        builder.Navigation(e => e.Prices).HasField("_prices");
    }
}
