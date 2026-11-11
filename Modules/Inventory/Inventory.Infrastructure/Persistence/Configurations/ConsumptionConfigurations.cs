using Inventory.Domain;
using Inventory.Domain.Consumption;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

// The stock ledger and what visits use. Quantities are numeric(12,3): a thousandth of the unit (a millilitre in
// litres) and room for any studio's stock.

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement", InventoryDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("stock_movement_pkey");

        // An item's history, newest first; the forecast reads consumption by time
        builder.HasIndex(e => new { e.ItemId, e.CreatedAt }).HasDatabaseName("stock_movement_item_id_created_at_idx");
        builder.HasIndex(e => new { e.StudioId, e.Type, e.CreatedAt }).HasDatabaseName("stock_movement_studio_type_created_at_idx");

        // The domain sets the ID; movements reach EF through the item's collection, and a key EF thinks the database
        // generates would make a new movement look like an existing row
        builder.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.ItemId).HasColumnName("item_id");
        builder.Property(e => e.Type).HasConversion<string>().HasColumnName("type");
        builder.Property(e => e.Quantity).HasPrecision(12, 3).HasColumnName("quantity");
        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.Note).HasMaxLength(StockMovement.MaxNoteLength).HasColumnName("note");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.HasOne<InventoryItem>()
            .WithMany(i => i.Movements)
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("stock_movement_item_id_fkey");
    }
}

public class ServiceRecipeConfiguration : IEntityTypeConfiguration<ServiceRecipe>
{
    public void Configure(EntityTypeBuilder<ServiceRecipe> builder)
    {
        builder.ToTable("service_recipe", InventoryDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("service_recipe_pkey");

        // One recipe per service; the service lives in Scheduling, so there is no foreign key
        builder.HasIndex(e => new { e.StudioId, e.ServiceId }).IsUnique().HasDatabaseName("service_recipe_studio_id_service_id_key");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.ServiceId).HasColumnName("service_id");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.OwnsMany(e => e.Lines, line =>
        {
            line.ToTable("service_recipe_line", InventoryDbContext.Schema, tb =>
                tb.HasCheckConstraint("CK_service_recipe_line_quantity", "quantity_for_medium > 0"));

            line.WithOwner().HasForeignKey("RecipeId").HasConstraintName("service_recipe_line_recipe_id_fkey");
            line.Property<Guid>("RecipeId").HasColumnName("recipe_id");
            line.HasKey("RecipeId", nameof(RecipeLine.ItemId)).HasName("service_recipe_line_pkey");

            line.Property(l => l.ItemId).HasColumnName("item_id");
            line.Property(l => l.QuantityForMedium).HasPrecision(12, 3).HasColumnName("quantity_for_medium");

            line.HasOne<InventoryItem>().WithMany().HasForeignKey(l => l.ItemId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("service_recipe_line_item_id_fkey");
        });

        builder.Navigation(e => e.Lines).HasField("_lines");
    }
}

public class MaterialSizeFactorsConfiguration : IEntityTypeConfiguration<MaterialSizeFactors>
{
    public void Configure(EntityTypeBuilder<MaterialSizeFactors> builder)
    {
        builder.ToTable("material_size_factors", InventoryDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("material_size_factors_pkey");
        builder.HasIndex(e => e.StudioId).IsUnique().HasDatabaseName("material_size_factors_studio_id_key");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Small).HasPrecision(4, 2).HasColumnName("small");
        builder.Property(e => e.Medium).HasPrecision(4, 2).HasColumnName("medium");
        builder.Property(e => e.Large).HasPrecision(4, 2).HasColumnName("large");
        builder.Property(e => e.Xl).HasPrecision(4, 2).HasColumnName("xl");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
    }
}

public class ConsumptionCalibrationConfiguration : IEntityTypeConfiguration<ConsumptionCalibration>
{
    public void Configure(EntityTypeBuilder<ConsumptionCalibration> builder)
    {
        builder.ToTable("consumption_calibration", InventoryDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("consumption_calibration_pkey");
        builder.HasIndex(e => new { e.StudioId, e.ServiceId, e.ItemId }).IsUnique()
            .HasDatabaseName("consumption_calibration_studio_service_item_key");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.ServiceId).HasColumnName("service_id");
        builder.Property(e => e.ItemId).HasColumnName("item_id");
        builder.Property(e => e.Factor).HasPrecision(8, 4).HasColumnName("factor");
        builder.Property(e => e.SampleCount).HasColumnName("sample_count");
        builder.Property(e => e.AbsolutePercentageErrorSum).HasPrecision(14, 6).HasColumnName("ape_sum");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.Ignore(e => e.EffectiveFactor);
        builder.Ignore(e => e.MeanAbsolutePercentageError);

        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("consumption_calibration_item_id_fkey");
    }
}

public class BookingConsumptionConfiguration : IEntityTypeConfiguration<BookingConsumption>
{
    public void Configure(EntityTypeBuilder<BookingConsumption> builder)
    {
        builder.ToTable("booking_consumption", InventoryDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("booking_consumption_pkey");

        // One per booking: a repeated BookingCompleted can't deduct twice. The booking lives in Scheduling (plain ID).
        builder.HasIndex(e => e.BookingId).IsUnique().HasDatabaseName(InventoryDbContext.BookingConsumptionKey);

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.VehicleSize).HasConversion<string>().HasColumnName("vehicle_size");
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.OwnsMany(e => e.Lines, line =>
        {
            line.ToTable("booking_consumption_line", InventoryDbContext.Schema);

            line.WithOwner().HasForeignKey("ConsumptionId").HasConstraintName("booking_consumption_line_consumption_id_fkey");
            line.Property<Guid>("ConsumptionId").HasColumnName("consumption_id");
            line.HasKey("ConsumptionId", nameof(ConsumptionLine.ServiceId), nameof(ConsumptionLine.ItemId))
                .HasName("booking_consumption_line_pkey");

            line.Property(l => l.ServiceId).HasColumnName("service_id");
            line.Property(l => l.ItemId).HasColumnName("item_id");
            line.Property(l => l.BaseQuantity).HasPrecision(12, 3).HasColumnName("base_quantity");
            line.Property(l => l.EstimatedQuantity).HasPrecision(12, 3).HasColumnName("estimated_quantity");
            line.Property(l => l.ActualQuantity).HasPrecision(12, 3).HasColumnName("actual_quantity");
        });

        builder.Navigation(e => e.Lines).HasField("_lines");
    }
}
