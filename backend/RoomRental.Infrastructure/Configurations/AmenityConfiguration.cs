using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity Amenity
/// </summary>
public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        // Tên bảng
        builder.ToTable("Amenities");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Icon)
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .HasMaxLength(255);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index - Name phải unique
        builder.HasIndex(a => a.Name)
            .IsUnique();

        // Relationships
        // Amenity -> PostAmenities
        builder.HasMany(a => a.PostAmenities)
            .WithOne(pa => pa.Amenity)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
