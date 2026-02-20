using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("client");
        builder.HasKey(x => x.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.Name).HasColumnName("name");
        builder.Property(e => e.Email).HasColumnName("email");
        builder.Property(e => e.Phone).HasColumnName("phone");
        builder.Property(e => e.Nip).HasColumnName("nip");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");

        builder.HasOne(x => x.Studio)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.StudioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}