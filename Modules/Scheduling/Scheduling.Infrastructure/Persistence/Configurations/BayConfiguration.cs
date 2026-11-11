using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Planning;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public class BayConfiguration : IEntityTypeConfiguration<Bay>
{
    public void Configure(EntityTypeBuilder<Bay> builder)
    {
        builder.ToTable("bay", SchedulingDbContext.Schema);
        builder.HasKey(e => e.Id).HasName("bay_pkey");

        // Every query is filtered by studio
        builder.HasIndex(e => e.StudioId, "IX_bay_studio_id");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Name).HasMaxLength(Bay.MaxNameLength).HasColumnName("name");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
    }
}
