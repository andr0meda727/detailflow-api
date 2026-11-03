using Identity.Domain.Invitations;
using Identity.Domain.Studios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class StudioInvitationConfiguration : IEntityTypeConfiguration<StudioInvitation>
{
    public void Configure(EntityTypeBuilder<StudioInvitation> builder)
    {
        builder
            .HasKey(e => e.Id)
            .HasName("studio_invitation_pkey");

        builder
            .ToTable("studio_invitation", IdentityDbContext.Schema);

        // One invitation per email and studio (see StudioInvitation.Reissue)
        builder.HasIndex(e => new { e.StudioId, e.Email }, "IX_studio_invitation_studio_id_email").IsUnique();
        builder.HasIndex(e => e.Token, "IX_studio_invitation_token").IsUnique();
        builder.HasIndex(e => e.InvitedBy, "IX_studio_invitation_invited_by");
        builder.HasIndex(e => e.RoleId, "IX_studio_invitation_role_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");

        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Email).HasColumnName("email");
        builder.Property(e => e.RoleId).HasColumnName("role_id");

        builder.Property(e => e.Token)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("token");

        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.InvitedBy).HasColumnName("invited_by");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");

        builder
            .HasOne<Studio>()
            .WithMany()
            .HasForeignKey(e => e.StudioId)
            .HasConstraintName("studio_invitation_studio_id_fkey");

        builder
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .HasConstraintName("studio_invitation_role_id_fkey");

        builder
            .HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.InvitedBy)
            .HasConstraintName("studio_invitation_invited_by_fkey");
    }
}
