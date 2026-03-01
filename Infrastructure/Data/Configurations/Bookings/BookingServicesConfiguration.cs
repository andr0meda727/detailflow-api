using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Bookings;

public class BookingServicesConfiguration : IEntityTypeConfiguration<BookingServices>
{
    public void Configure(EntityTypeBuilder<BookingServices> builder)
    {
        builder
            .HasKey(e => new { e.BookingId, e.ServiceId })
            .HasName("booking_service_pkey");

        builder
            .ToTable("booking_service", "d");

        builder.Property(e => e.BookingId)
            .HasColumnName("booking_id");

        builder.Property(e => e.ServiceId)
            .HasColumnName("service_id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne(d => d.Booking)
            .WithMany(p => p.BookingServices)
            .HasForeignKey(d => d.BookingId)
            .HasConstraintName("booking_service_booking_id_fkey");

        builder
            .HasOne(d => d.Service)
            .WithMany(p => p.BookingServices)
            .HasForeignKey(d => d.ServiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_service_service_id_fkey");
    }
}