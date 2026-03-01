using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Bookings;

public class BookingStatusConfiguration : IEntityTypeConfiguration<BookingStatus>
{
    public void Configure(EntityTypeBuilder<BookingStatus> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("booking_status_pkey");

        builder
            .ToTable("booking_status", "d");

        builder
            .HasIndex(e => e.Name, "booking_status_name_key")
            .IsUnique();

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.Color)
            .HasColumnName("color");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order");
    }
}