using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Bookings;

public class BookingServicesConfiguration : IEntityTypeConfiguration<BookingServices>
{
    public void Configure(EntityTypeBuilder<BookingServices> builder)
    {
        builder.ToTable("booking_service");
        builder.HasKey(x => new { x.BookingId, x.ServiceId });

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.ServiceId).HasColumnName("service_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.BookingServices)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Service)
            .WithMany(x => x.BookingServices)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}