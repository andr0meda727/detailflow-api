using Crm.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crm.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("client_pkey");

        builder
            .ToTable("client", CrmDbContext.Schema);

        // A phone number identifies a client within one studio; the same person can be a client of many studios.
        // Deleted clients are excluded so their number can be used again.
        builder
            .HasIndex(e => new { e.StudioId, e.Phone }, "client_studio_id_phone_key")
            .IsUnique()
            .HasFilter("is_deleted = false");

        // Finding the client an online customer books as
        builder.HasIndex(e => new { e.StudioId, e.CustomerUserId }, "IX_client_studio_id_customer_user_id");

        builder.Property(e => e.CustomerUserId).HasColumnName("customer_user_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.Phone)
            .HasColumnName("phone");

        builder.Property(e => e.Email)
            .HasColumnName("email");

        builder.Property(e => e.Nip)
            .HasColumnName("nip");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasMany(e => e.Vehicles)
            .WithOne()
            .HasForeignKey(v => v.ClientId)
            .HasConstraintName("vehicle_client_id_fkey");
    }
}
