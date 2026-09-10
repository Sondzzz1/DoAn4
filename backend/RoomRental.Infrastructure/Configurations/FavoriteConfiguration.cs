using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity Favorite
/// </summary>
public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        // Tên bảng
        builder.ToTable("Favorites");

        // Primary Key
        builder.HasKey(f => f.Id);

        // Properties
        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index - Đảm bảo 1 User không thể yêu thích 1 Post nhiều lần
        builder.HasIndex(f => new { f.UserId, f.PostId })
            .IsUnique();

        // Relationships
        // Favorite -> User (Tenant)
        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Favorite -> Post
        builder.HasOne(f => f.Post)
            .WithMany(p => p.Favorites)
            .HasForeignKey(f => f.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
