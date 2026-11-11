using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Bookings;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public class BookingServiceLineConfiguration : IEntityTypeConfiguration<BookingServiceLine>
{
    public void Configure(EntityTypeBuilder<BookingServiceLine> builder)
    {
        builder
            .HasKey(e => new { e.BookingId, e.ServiceId })
            .HasName("booking_service_pkey");

        builder
            .ToTable("booking_service", SchedulingDbContext.Schema);

        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.ServiceId).HasColumnName("service_id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne(e => e.Service)
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("booking_service_service_id_fkey");
    }
}
