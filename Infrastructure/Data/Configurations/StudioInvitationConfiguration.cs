using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class StudioInvitationConfiguration : IEntityTypeConfiguration<StudioInvitation>
{
    public void Configure(EntityTypeBuilder<StudioInvitation> builder)
    {
        builder.HasKey(e => e.Id).HasName("studio_invitation_pkey");
        builder.ToTable("studio_invitation", "d");

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => new { e.StudioId, e.Email }).IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Email).HasColumnName("email");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.Token).HasDefaultValueSql("gen_random_uuid()").HasColumnName("token");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.InvitedBy).HasColumnName("invited_by");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder
            .HasOne(e => e.Studio)
            .WithMany()
            .HasForeignKey(e => e.StudioId)
            .HasConstraintName("studio_invitation_studio_id_fkey");

        builder
            .HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .HasConstraintName("studio_invitation_role_id_fkey");

        builder
            .HasOne(e => e.InvitedByUser)
            .WithMany()
            .HasForeignKey(e => e.InvitedBy)
            .HasConstraintName("studio_invitation_invited_by_fkey");
    }
}