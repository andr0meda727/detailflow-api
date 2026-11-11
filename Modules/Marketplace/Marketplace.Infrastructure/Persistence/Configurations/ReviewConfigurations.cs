using Marketplace.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.Infrastructure.Persistence.Configurations;

public class ReviewableVisitConfiguration : IEntityTypeConfiguration<ReviewableVisit>
{
    public void Configure(EntityTypeBuilder<ReviewableVisit> builder)
    {
        builder.ToTable("reviewable_visit", MarketplaceDbContext.Schema);

        // The booking (in Scheduling, so a plain ID) is the key: a repeated BookingCompleted records nothing new
        builder.HasKey(e => e.BookingId).HasName(MarketplaceDbContext.ReviewableVisitKey);
        builder.HasIndex(e => new { e.CustomerUserId, e.CompletedAt }).HasDatabaseName("reviewable_visit_customer_completed_idx");

        builder.Property(e => e.BookingId).ValueGeneratedNever().HasColumnName("booking_id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.CustomerUserId).HasColumnName("customer_user_id");
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("review", MarketplaceDbContext.Schema, tb =>
            tb.HasCheckConstraint("CK_review_rating", $"rating BETWEEN {Review.MinRating} AND {Review.MaxRating}"));
        builder.HasKey(e => e.Id).HasName("review_pkey");

        // One review per visit; a studio's page lists and averages its reviews, newest first
        builder.HasIndex(e => e.BookingId).IsUnique().HasDatabaseName("review_booking_id_key");
        builder.HasIndex(e => new { e.StudioId, e.CreatedAt }).HasDatabaseName("review_studio_id_created_at_idx");

        builder.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(e => e.StudioId).HasColumnName("studio_id");
        builder.Property(e => e.BookingId).HasColumnName("booking_id");
        builder.Property(e => e.CustomerUserId).HasColumnName("customer_user_id");
        builder.Property(e => e.AuthorName).HasMaxLength(120).HasColumnName("author_name");
        builder.Property(e => e.Rating).HasColumnName("rating");
        builder.Property(e => e.Comment).HasMaxLength(Review.MaxCommentLength).HasColumnName("comment");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

        builder.HasOne<ReviewableVisit>().WithOne().HasForeignKey<Review>(e => e.BookingId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("review_booking_id_fkey");
    }
}
