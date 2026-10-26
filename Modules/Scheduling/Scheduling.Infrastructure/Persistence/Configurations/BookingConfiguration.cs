using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Bookings;

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

        builder.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.Property(e => e.StatusId).HasColumnName("status_id");
        builder.Property(e => e.TotalPrice).HasColumnName("total_price");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");

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
