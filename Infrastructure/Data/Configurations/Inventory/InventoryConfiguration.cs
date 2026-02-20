using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Inventory;

public class InventoryConfiguration : IEntityTypeConfiguration<Core.Models.Inventory>
{
    public void Configure(EntityTypeBuilder<Core.Models.Inventory> builder)
    {
        builder.ToTable("inventory");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.CategoryId).HasColumnName("category_id");
        builder.Property(e => e.Name).HasColumnName("name");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Unit).HasColumnName("unit");
        builder.Property(e => e.Stock).HasColumnName("stock");
        builder.Property(e => e.MinStock).HasColumnName("min_stock");
        builder.Property(e => e.Capacity).HasColumnName("capacity");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Studio)
            .WithMany(x => x.InventoryItems)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.InventoryItems)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}