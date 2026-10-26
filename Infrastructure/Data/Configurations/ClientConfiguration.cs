using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(e => e.Id)
            .HasName("client_pkey");

        builder
            .ToTable("client", "d");

        // A phone number identifies a client within one studio; the same person can be a client of many studios.
        // Soft-deleted clients are excluded so their number can be used again.
        builder
            .HasIndex(e => new { e.StudioId, e.Phone }, "client_studio_id_phone_key")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.Email)
            .HasColumnName("email");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(e => e.Name)
            .HasColumnName("name");

        builder.Property(e => e.Nip)
            .HasColumnName("nip");

        builder.Property(e => e.Phone)
            .HasColumnName("phone");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne(d => d.Studio)
            .WithMany(p => p.Clients)
            .HasForeignKey(d => d.StudioId)
            .HasConstraintName("client_studio_id_fkey");
    }
}