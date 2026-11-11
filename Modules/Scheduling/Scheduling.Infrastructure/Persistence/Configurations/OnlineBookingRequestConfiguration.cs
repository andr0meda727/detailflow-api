using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Requests;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public class OnlineBookingRequestConfiguration : IEntityTypeConfiguration<OnlineBookingRequest>
{
    public void Configure(EntityTypeBuilder<OnlineBookingRequest> builder)
    {
        builder.ToTable("booking_request", SchedulingDbContext.Schema);

        builder.HasKey(e => e.Id).HasName("booking_request_pkey");
        builder.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");

        // A studio's queue (pending first, newest first), and a customer's history across studios
        builder.HasIndex(e => new { e.StudioId, e.Status, e.CreatedAt }, "IX_booking_request_studio_id_status_created_at");
        builder.HasIndex(e => new { e.CustomerUserId, e.CreatedAt }, "IX_booking_request_customer_user_id_created_at");

        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.StudioName).HasMaxLength(100).HasColumnName("studio_name");
        builder.Property(e => e.CustomerUserId).HasColumnName("customer_user_id");
        builder.Property(e => e.CustomerName).HasMaxLength(200).HasColumnName("customer_name");
        builder.Property(e => e.CustomerPhone).HasMaxLength(30).HasColumnName("customer_phone");
        builder.Property(e => e.CustomerEmail).HasMaxLength(254).HasColumnName("customer_email");
        builder.Property(e => e.VehicleLicensePlate).HasMaxLength(15).HasColumnName("vehicle_license_plate");
        builder.Property(e => e.VehicleBrand).HasMaxLength(50).HasColumnName("vehicle_brand");
        builder.Property(e => e.VehicleModel).HasMaxLength(50).HasColumnName("vehicle_model");
        builder.Property(e => e.VehicleSize).HasConversion<string>().HasColumnName("vehicle_size");
        builder.Property(e => e.PreferredAt).HasColumnName("preferred_at");
        builder.Property(e => e.Notes).HasMaxLength(OnlineBookingRequest.MaxNotesLength).HasColumnName("notes");
        builder.Property(e => e.EstimatedPriceInCents).HasColumnName("estimated_price_cents");
        builder.Property(e => e.HasUnpricedServices).HasColumnName("has_unpriced_services");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnName("status");
        builder.Property(e => e.RejectionReason).HasMaxLength(500).HasColumnName("rejection_reason");
        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.DecidedAt).HasColumnName("decided_at");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.Ignore(e => e.IsPending);

        builder.OwnsMany(e => e.Services, service =>
        {
            service.ToTable("booking_request_service", SchedulingDbContext.Schema);
            service.WithOwner().HasForeignKey("RequestId").HasConstraintName("booking_request_service_request_id_fkey");
            service.Property<Guid>("RequestId").HasColumnName("request_id");
            service.HasKey("RequestId", nameof(RequestedService.ServiceId)).HasName("booking_request_service_pkey");

            // A copy, not a foreign key: the service may be renamed or removed later
            service.Property(s => s.ServiceId).HasColumnName("service_id");
            service.Property(s => s.Name).HasColumnName("name");
            service.Property(s => s.PriceInCents).HasColumnName("price_cents");
        });

        builder.Navigation(e => e.Services).HasField("_services");
    }
}
