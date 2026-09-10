using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;
using RoomRental.Domain.Enums;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity Post
/// </summary>
public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        // Tên bảng
        builder.ToTable("Posts");

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasDefaultValue(PostStatus.Pending)
            .HasConversion<int>(); // Lưu Enum dưới dạng int

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.LandlordId);

        // Relationships
        // Post -> Landlord (User)
        builder.HasOne(p => p.Landlord)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        // Post -> Room (1-1)
        builder.HasOne(p => p.Room)
            .WithOne(r => r.Post)
            .HasForeignKey<Room>(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Post -> PostImages
        builder.HasMany(p => p.Images)
            .WithOne(pi => pi.Post)
            .HasForeignKey(pi => pi.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Post -> PostAmenities
        builder.HasMany(p => p.PostAmenities)
            .WithOne(pa => pa.Post)
            .HasForeignKey(pa => pa.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Post -> Favorites
        builder.HasMany(p => p.Favorites)
            .WithOne(f => f.Post)
            .HasForeignKey(f => f.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Post -> Appointments
        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Post)
            .HasForeignKey(a => a.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
