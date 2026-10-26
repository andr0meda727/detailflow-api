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

        builder
            .HasOne(d => d.Client)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.ClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_client_id_fkey");

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

        builder
            .HasOne(d => d.Vehicle)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_vehicle_id_fkey");
    }
}