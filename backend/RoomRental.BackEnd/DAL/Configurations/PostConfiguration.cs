using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DAL.Configurations;

/// <summary>
/// Configuration cho Entity Post
/// </summary>
public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("TinDang");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RoomId)
            .HasColumnName("PhongTroId")
            .IsRequired();

        builder.Property(p => p.LandlordId)
            .HasColumnName("ChuTroId")
            .IsRequired();

        builder.Property(p => p.Title)
            .HasColumnName("TieuDe")
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Content)
            .HasColumnName("NoiDung")
            .IsRequired();

        builder.Property(p => p.DisplayPrice)
            .HasColumnName("GiaHienThi")
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .HasColumnName("TrangThai")
            .IsRequired()
            .HasDefaultValue(PostStatus.Pending)
            .HasConversion<int>();

        builder.Property(p => p.RejectionReason)
            .HasColumnName("LyDoTuChoi")
            .HasMaxLength(500);

        builder.Property(p => p.ViewCount)
            .HasColumnName("LuotXem")
            .IsRequired();

        builder.Property(p => p.PostedAt).HasColumnName("NgayDang");
        builder.Property(p => p.ExpiredAt).HasColumnName("NgayHetHan");
        builder.Property(p => p.ApprovedAt).HasColumnName("NgayDuyet");
        builder.Property(p => p.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.Property(p => p.UpdatedAt).HasColumnName("NgayCapNhat");

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.LandlordId);
        builder.HasIndex(p => p.RoomId);

        builder.HasOne(p => p.Landlord)
            .WithMany(l => l.Posts)
            .HasForeignKey(p => p.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Room)
            .WithMany(r => r.Posts)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
