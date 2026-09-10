using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity PostImage
/// </summary>
public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        // Tên bảng
        builder.ToTable("PostImages");

        // Primary Key
        builder.HasKey(pi => pi.Id);

        // Properties
        builder.Property(pi => pi.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pi => pi.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pi => pi.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index
        builder.HasIndex(pi => pi.PostId);
        builder.HasIndex(pi => pi.DisplayOrder);

        // Relationships
        // PostImage -> Post
        builder.HasOne(pi => pi.Post)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
