using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DAL.Configurations;

/// <summary>
/// Configuration cho Entity Room
/// </summary>
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("PhongTro");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.LandlordId)
            .HasColumnName("ChuTroId")
            .IsRequired();

        builder.Property(r => r.CategoryId)
            .HasColumnName("DanhMucId")
            .IsRequired();

        builder.Property(r => r.RoomName)
            .HasColumnName("TenPhong")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasColumnName("MoTa");

        builder.Property(r => r.Price)
            .HasColumnName("GiaThue")
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.Area)
            .HasColumnName("DienTich")
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(r => r.MaxOccupants)
            .HasColumnName("SoNguoiToiDa")
            .IsRequired();

        builder.Property(r => r.CurrentOccupants)
            .HasColumnName("SoNguoiHienTai")
            .IsRequired();

        builder.Property(r => r.Bedrooms).HasColumnName("SoPhongNgu");
        builder.Property(r => r.Bathrooms).HasColumnName("SoPhongTam");
        builder.Property(r => r.Floor).HasColumnName("Tang");

        builder.Property(r => r.Address)
            .HasColumnName("DiaChi")
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.Ward)
            .HasColumnName("PhuongXa")
            .HasMaxLength(100);

        builder.Property(r => r.District)
            .HasColumnName("QuanHuyen")
            .HasMaxLength(100);

        builder.Property(r => r.Province)
            .HasColumnName("TinhThanh")
            .HasMaxLength(100);

        builder.Property(r => r.Latitude)
            .HasColumnName("ViDo")
            .HasColumnType("decimal(10,7)");

        builder.Property(r => r.Longitude)
            .HasColumnName("KinhDo")
            .HasColumnType("decimal(10,7)");

        builder.Property(r => r.ElectricityPrice)
            .HasColumnName("TienDien")
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.WaterPrice)
            .HasColumnName("TienNuoc")
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.ServiceFee)
            .HasColumnName("PhiDichVu")
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.Status)
            .HasColumnName("TrangThai")
            .IsRequired()
            .HasDefaultValue(RoomStatus.Available)
            .HasConversion<int>();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("NgayTao")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("NgayCapNhat");

        builder.HasIndex(r => r.Province);
        builder.HasIndex(r => r.District);
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Landlord)
            .WithMany(l => l.Rooms)
            .HasForeignKey(r => r.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Category)
            .WithMany(c => c.Rooms)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
