using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

/// <summary>
/// Configuration cho Entity PostImage
/// </summary>
public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.ToTable("HinhAnhPhong");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.RoomId)
            .HasColumnName("PhongTroId")
            .IsRequired();

        builder.Property(pi => pi.ImageUrl)
            .HasColumnName("DuongDan")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(pi => pi.IsThumbnail)
            .HasColumnName("LaAnhDaiDien")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pi => pi.DisplayOrder)
            .HasColumnName("ThuTu")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pi => pi.CreatedAt)
            .HasColumnName("NgayTao")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasIndex(pi => pi.RoomId);
        builder.HasIndex(pi => pi.DisplayOrder);

        builder.HasOne(pi => pi.Room)
            .WithMany(r => r.Images)
            .HasForeignKey(pi => pi.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
