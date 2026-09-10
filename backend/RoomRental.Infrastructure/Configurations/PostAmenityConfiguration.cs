using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity PostAmenity (Bảng trung gian Many-to-Many)
/// </summary>
public class PostAmenityConfiguration : IEntityTypeConfiguration<PostAmenity>
{
    public void Configure(EntityTypeBuilder<PostAmenity> builder)
    {
        // Tên bảng
        builder.ToTable("PostAmenities");

        // Composite Primary Key (PostId, AmenityId)
        builder.HasKey(pa => new { pa.PostId, pa.AmenityId });

        // Properties
        builder.Property(pa => pa.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        // PostAmenity -> Post
        builder.HasOne(pa => pa.Post)
            .WithMany(p => p.PostAmenities)
            .HasForeignKey(pa => pa.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // PostAmenity -> Amenity
        builder.HasOne(pa => pa.Amenity)
            .WithMany(a => a.PostAmenities)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
