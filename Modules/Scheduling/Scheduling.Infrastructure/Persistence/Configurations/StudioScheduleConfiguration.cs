using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Planning;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public class StudioScheduleConfiguration : IEntityTypeConfiguration<StudioSchedule>
{
    public void Configure(EntityTypeBuilder<StudioSchedule> builder)
    {
        builder.ToTable("studio_schedule", SchedulingDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("studio_schedule_pkey");

        // One per studio
        builder.HasIndex(e => e.StudioId, "IX_studio_schedule_studio_id").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.BufferMinutes).HasColumnName("buffer_minutes");
        builder.Property(e => e.SlotStepMinutes).HasColumnName("slot_step_minutes");
        builder.Property(e => e.MinLeadHours).HasColumnName("min_lead_hours");
        builder.Property(e => e.SmallMultiplier).HasPrecision(4, 2).HasColumnName("small_multiplier");
        builder.Property(e => e.MediumMultiplier).HasPrecision(4, 2).HasColumnName("medium_multiplier");
        builder.Property(e => e.LargeMultiplier).HasPrecision(4, 2).HasColumnName("large_multiplier");
        builder.Property(e => e.XlMultiplier).HasPrecision(4, 2).HasColumnName("xl_multiplier");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.Ignore(e => e.HasOpeningHours);
        builder.Ignore(e => e.Multipliers);

        // Both lists are replaced as a whole and always loaded with the schedule
        builder.OwnsMany(e => e.OpeningHours, period =>
        {
            period.ToTable("opening_period", SchedulingDbContext.Schema, tb =>
                tb.HasCheckConstraint("CK_opening_period_order", "closes > opens"));

            period.WithOwner().HasForeignKey("ScheduleId").HasConstraintName("opening_period_schedule_id_fkey");
            period.Property<Guid>("ScheduleId").HasColumnName("schedule_id");
            period.HasKey("ScheduleId", nameof(OpeningPeriod.Day), nameof(OpeningPeriod.Opens)).HasName("opening_period_pkey");

            // By name, like the other enums in this schema
            period.Property(p => p.Day).HasConversion<string>().HasColumnName("day");
            period.Property(p => p.Opens).HasColumnName("opens");
            period.Property(p => p.Closes).HasColumnName("closes");
        });

        builder.OwnsMany(e => e.Closures, closure =>
        {
            closure.ToTable("closure", SchedulingDbContext.Schema);

            closure.WithOwner().HasForeignKey("ScheduleId").HasConstraintName("closure_schedule_id_fkey");
            closure.Property<Guid>("ScheduleId").HasColumnName("schedule_id");
            closure.HasKey("ScheduleId", nameof(Closure.Date)).HasName("closure_pkey");

            closure.Property(c => c.Date).HasColumnName("date");
            closure.Property(c => c.Reason).HasMaxLength(StudioSchedule.MaxClosureReasonLength).HasColumnName("reason");
        });

        builder.Navigation(e => e.OpeningHours).HasField("_openingHours");
        builder.Navigation(e => e.Closures).HasField("_closures");
    }
}
