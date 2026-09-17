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
        builder.ToTable("YeuThich");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.TenantId)
            .HasColumnName("NguoiDungId")
            .IsRequired();

        builder.Property(f => f.PostId)
            .HasColumnName("TinDangId")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("NgayLuu")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasIndex(f => new { f.TenantId, f.PostId })
            .IsUnique();

        builder.HasOne(f => f.Tenant)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Post)
            .WithMany(p => p.Favorites)
            .HasForeignKey(f => f.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
