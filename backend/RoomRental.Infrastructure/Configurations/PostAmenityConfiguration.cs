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
        builder.ToTable("PhongTro_TienNghi");

        builder.HasKey(pa => new { pa.RoomId, pa.AmenityId });

        builder.Property(pa => pa.RoomId).HasColumnName("PhongTroId");
        builder.Property(pa => pa.AmenityId).HasColumnName("TienNghiId");

        builder.HasOne(pa => pa.Room)
            .WithMany(r => r.RoomAmenities)
            .HasForeignKey(pa => pa.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Amenity)
            .WithMany(a => a.RoomAmenities)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
