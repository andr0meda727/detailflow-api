using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Bookings;
using Shared.Kernel.Vehicles;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("booking_pkey");

        builder
            .ToTable("booking", SchedulingDbContext.Schema);

        // Serves the default list order; it also covers studio_id lookups
        builder
            .HasIndex(e => new { e.StudioId, e.ScheduledAt })
            .HasDatabaseName("booking_studio_id_scheduled_at_idx");

        // Client, vehicle and author live in other modules: plain IDs, no foreign keys or navigations.
        // The indexes stay for lookups when their data changes.
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.VehicleId);
        builder.HasIndex(e => e.CreatedBy);

        // A customer's bookings across studios
        builder.HasIndex(e => e.CustomerUserId).HasDatabaseName("IX_booking_customer_user_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.StudioId).HasColumnName("studio_id");

        builder.Property(e => e.ClientId).HasColumnName("client_id");
        builder.Property(e => e.ClientName).HasColumnName("client_name");
        builder.Property(e => e.ClientPhone).HasColumnName("client_phone");
        builder.Property(e => e.ClientEmail).HasColumnName("client_email");

        builder.Property(e => e.VehicleId).HasColumnName("vehicle_id");
        builder.Property(e => e.VehicleLicensePlate).HasColumnName("vehicle_license_plate");
        builder.Property(e => e.VehicleBrand).HasColumnName("vehicle_brand");
        builder.Property(e => e.VehicleModel).HasColumnName("vehicle_model");

        // By name, like the CRM vehicle; bookings from before sizes existed are Medium
        builder.Property(e => e.VehicleSize)
            .HasConversion<string>()
            .HasDefaultValue(VehicleSize.Medium)
            .HasSentinel((VehicleSize)0)
            .HasColumnName("vehicle_size");

        builder.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(e => e.EndsAt).HasColumnName("ends_at");
        builder.Property(e => e.BayId).HasColumnName("bay_id");

        builder.ToTable(tb => tb.HasCheckConstraint("CK_booking_ends_after_start", "ends_at > scheduled_at"));

        // The planner reads a bay's bookings by time; the exclusion constraint (raw SQL in AddBayExclusion) has its own index
        builder.HasIndex(e => new { e.BayId, e.ScheduledAt }).HasDatabaseName("booking_bay_id_scheduled_at_idx");

        // Bays live in this module, so this one is a real foreign key; bays are only ever deactivated
        builder
            .HasOne(e => e.Bay)
            .WithMany()
            .HasForeignKey(e => e.BayId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_bay_id_fkey");

        builder.Ignore(e => e.Range);
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.Property(e => e.StatusId).HasColumnName("status_id");
        builder.Property(e => e.TotalPrice).HasColumnName("total_price");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.CustomerUserId).HasColumnName("customer_user_id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder.Ignore(e => e.IsFinished);

        builder
            .HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_status_id_fkey");

        builder
            .HasMany(e => e.Services)
            .WithOne()
            .HasForeignKey(line => line.BookingId)
            .HasConstraintName("booking_service_booking_id_fkey");
    }
}
