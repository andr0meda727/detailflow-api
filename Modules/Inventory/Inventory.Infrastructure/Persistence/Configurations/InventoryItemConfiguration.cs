using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("inventory_pkey");

        builder
            .ToTable("inventory", InventoryDbContext.Schema);

        // Every query is filtered by studio
        builder.HasIndex(e => e.StudioId, "IX_inventory_studio_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.CategoryId)
            .HasColumnName("category_id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.MinStock)
            .HasColumnName("min_stock");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.Stock)
            .HasColumnName("stock");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.Unit)
            .HasColumnName("unit");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne(d => d.Category)
            .WithMany(p => p.Items)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("inventory_category_id_fkey");
    }
}