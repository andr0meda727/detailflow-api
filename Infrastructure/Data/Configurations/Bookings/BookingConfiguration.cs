using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Bookings;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("booking_pkey");

        builder
            .ToTable("booking", "d");

        // Serves the default list order; it also covers the studio_id foreign key
        builder
            .HasIndex(e => new { e.StudioId, e.ScheduledAt })
            .HasDatabaseName("booking_studio_id_scheduled_at_idx");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.ClientId)
            .HasColumnName("client_id");

        builder.Property(e => e.ClientName)
            .HasColumnName("client_name");

        builder.Property(e => e.ClientPhone)
            .HasColumnName("client_phone");

        builder.Property(e => e.ClientEmail)
            .HasColumnName("client_email");

        builder.Property(e => e.VehicleLicensePlate)
            .HasColumnName("vehicle_license_plate");

        builder.Property(e => e.VehicleBrand)
            .HasColumnName("vehicle_brand");

        builder.Property(e => e.VehicleModel)
            .HasColumnName("vehicle_model");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        builder.Property(e => e.ScheduledAt)
            .HasColumnName("scheduled_at");

        builder.Property(e => e.StatusId)
            .HasColumnName("status_id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.TotalPrice)
            .HasColumnName("total_price");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder.Property(e => e.VehicleId)
            .HasColumnName("vehicle_id");

        // Client and vehicle belong to the CRM module: plain IDs, no foreign keys or navigations.
        // The indexes stay for lookups when CRM data changes.
        builder.HasIndex(e => e.ClientId);

        builder.HasIndex(e => e.VehicleId);

        builder
            .HasOne(d => d.CreatedByNavigation)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_created_by_fkey");

        builder
            .HasOne(d => d.Status)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.StatusId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_status_id_fkey");

        builder
            .HasOne(d => d.Studio)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.StudioId)
            .HasConstraintName("booking_studio_id_fkey");
    }
}