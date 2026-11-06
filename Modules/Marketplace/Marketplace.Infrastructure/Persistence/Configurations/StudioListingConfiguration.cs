using Marketplace.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class StudioListingConfiguration : IEntityTypeConfiguration<StudioListing>
{
    public void Configure(EntityTypeBuilder<StudioListing> builder)
    {
        builder.ToTable("studio_listing", tb =>
        {
            tb.HasCheckConstraint("CK_studio_listing_latitude", "latitude BETWEEN -90 AND 90");
            tb.HasCheckConstraint("CK_studio_listing_longitude", "longitude BETWEEN -180 AND 180");
        });

        // One listing per studio; the ID is Identity's studio ID (no foreign key across modules)
        builder.HasKey(e => e.StudioId).HasName("studio_listing_pkey");
        builder.Property(e => e.StudioId).ValueGeneratedNever().HasColumnName("studio_id");

        builder.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(e => e.Address).HasMaxLength(200).HasColumnName("address");
        builder.Property(e => e.City).HasMaxLength(100).HasColumnName("city");
        builder.Property(e => e.Phone).HasMaxLength(30).HasColumnName("phone");
        builder.Property(e => e.Latitude).HasColumnName("latitude");
        builder.Property(e => e.Longitude).HasColumnName("longitude");
        builder.Property(e => e.IsPublished).HasColumnName("is_published");
        builder.Property(e => e.CatalogChangedAt).HasColumnName("catalog_changed_at");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        // The map search: published studios inside the visible area
        builder.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_studio_listing_location")
            .HasFilter("is_published");

        builder.Ignore(e => e.Categories);

        builder.OwnsMany(e => e.Services, service =>
        {
            service.ToTable("listing_service");
            service.WithOwner().HasForeignKey("StudioId").HasConstraintName("listing_service_studio_id_fkey");
            service.Property<Guid>("StudioId").HasColumnName("studio_id");
            service.HasKey("StudioId", nameof(ListedService.ServiceId)).HasName("listing_service_pkey");

            service.Property(s => s.ServiceId).HasColumnName("service_id");
            service.Property(s => s.Name).HasColumnName("name");
            service.Property(s => s.Category).HasConversion<string>().HasColumnName("category");

            // "Studios that do interior"
            service.HasIndex(nameof(ListedService.Category), "StudioId").HasDatabaseName("IX_listing_service_category");
        });

        builder.OwnsMany(e => e.Prices, price =>
        {
            price.ToTable("listing_price");
            price.WithOwner().HasForeignKey("StudioId").HasConstraintName("listing_price_studio_id_fkey");
            price.Property<Guid>("StudioId").HasColumnName("studio_id");
            price.HasKey("StudioId", nameof(ListedPrice.ServiceId), nameof(ListedPrice.Size)).HasName("listing_price_pkey");

            price.Property(p => p.ServiceId).HasColumnName("service_id");
            price.Property(p => p.Size).HasConversion<string>().HasColumnName("vehicle_size");
            price.Property(p => p.AmountInCents).HasColumnName("amount_cents");
        });

        builder.Navigation(e => e.Services).HasField("_services");
        builder.Navigation(e => e.Prices).HasField("_prices");
    }
}
