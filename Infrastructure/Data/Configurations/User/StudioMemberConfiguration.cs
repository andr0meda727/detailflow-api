using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.User;

public class StudioMemberConfiguration : IEntityTypeConfiguration<StudioMember>
{
    public void Configure(EntityTypeBuilder<StudioMember> builder)
    {
        builder
            .HasKey(e => new { e.UserId, e.StudioId })
            .HasName("studio_member_pkey");

        builder
            .ToTable("studio_member", "d");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.StudioId)
            .HasColumnName("studio_id");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(e => e.RoleId)
            .HasColumnName("role_id");

        builder
            .HasOne(d => d.Role)
            .WithMany(p => p.StudioMembers)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("studio_member_role_id_fkey");

        builder
            .HasOne(d => d.Studio)
            .WithMany(p => p.StudioMembers)
            .HasForeignKey(d => d.StudioId)
            .HasConstraintName("studio_member_studio_id_fkey");

        builder
            .HasOne(d => d.User)
            .WithMany(p => p.StudioMembers)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("studio_member_user_id_fkey");
    }
}