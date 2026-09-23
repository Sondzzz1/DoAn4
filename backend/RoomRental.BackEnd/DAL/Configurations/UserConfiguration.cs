using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

/// <summary>
/// Configuration cho Entity User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("TaiKhoan");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
            .HasColumnName("TenDangNhap")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("MatKhauHash")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.FullName)
            .HasColumnName("HoTen")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Phone)
            .HasColumnName("SoDienThoai")
            .HasMaxLength(20);

        builder.Property(u => u.Email)
            .HasColumnName("Email")
            .HasMaxLength(150);

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("AnhDaiDien")
            .HasMaxLength(500);

        builder.Property(u => u.RoleId)
            .HasColumnName("VaiTro")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(u => u.IsActive)
            .HasColumnName("TrangThai")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("NgayTao")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("NgayCapNhat");

        builder.Ignore(u => u.IsBlocked);
        builder.Ignore(u => u.RoleName);

        builder.HasIndex(u => u.UserName)
            .IsUnique();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasOne(u => u.TenantProfile)
            .WithOne(p => p.Account)
            .HasForeignKey<TenantProfile>(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.LandlordProfile)
            .WithOne(p => p.Account)
            .HasForeignKey<LandlordProfile>(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
