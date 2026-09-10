using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;
using RoomRental.Domain.Enums;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity Room
/// </summary>
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        // Tên bảng
        builder.ToTable("Rooms");

        // Primary Key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Area)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(r => r.MaxOccupants)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasDefaultValue(RoomStatus.Available)
            .HasConversion<int>(); // Lưu Enum dưới dạng int

        builder.Property(r => r.Province)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.District)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Ward)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Address)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index cho tìm kiếm theo địa điểm
        builder.HasIndex(r => r.Province);
        builder.HasIndex(r => r.District);
        builder.HasIndex(r => r.Status);

        // Relationships
        // Room -> Post (1-1)
        builder.HasOne(r => r.Post)
            .WithOne(p => p.Room)
            .HasForeignKey<Room>(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
