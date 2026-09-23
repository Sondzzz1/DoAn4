using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

/// <summary>
/// Configuration cho Entity Amenity
/// </summary>
public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("TienNghi");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasColumnName("TenTienNghi")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Icon)
            .HasColumnName("Icon")
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasColumnName("MoTa")
            .HasMaxLength(300);

        builder.Property(a => a.IsActive)
            .HasColumnName("TrangThai")
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(a => a.Name)
            .IsUnique();

        builder.HasMany(a => a.RoomAmenities)
            .WithOne(pa => pa.Amenity)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
