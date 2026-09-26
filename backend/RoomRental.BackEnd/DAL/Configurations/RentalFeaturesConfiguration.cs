using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class RentalRequestConfiguration : IEntityTypeConfiguration<RentalRequest>
{
    public void Configure(EntityTypeBuilder<RentalRequest> builder)
    {
        builder.ToTable("YeuCauThuePhong");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnName("TrangThai").HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        builder.HasIndex(x => new { x.TenantAccountId, x.PostId });
        builder.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.ToTable("DatCoc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnName("SoTien").HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnName("TrangThai").HasDefaultValue(0);
        builder.Property(x => x.PaidAt).HasColumnName("NgayThanhToan");
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        builder.HasIndex(x => x.RentalRequestId);
    }
}

public class RentalContractConfiguration : IEntityTypeConfiguration<RentalContract>
{
    public void Configure(EntityTypeBuilder<RentalContract> builder)
    {
        builder.ToTable("HopDongThue");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MonthlyRent).HasColumnName("TienThueHangThang").HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnName("TrangThai").HasDefaultValue(0);
        builder.Property(x => x.TenantConfirmed).HasColumnName("NguoiThueDaXacNhan");
        builder.Property(x => x.LandlordConfirmed).HasColumnName("ChuTroDaXacNhan");
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        builder.HasIndex(x => new { x.TenantAccountId, x.LandlordAccountId });
    }
}

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("SuCo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasColumnName("TieuDe").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasColumnName("TrangThai").HasDefaultValue(0);
        builder.Property(x => x.Resolution).HasColumnName("HuongXuLy").HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        builder.HasIndex(x => x.ContractId);
    }
}

public class RoomReviewConfiguration : IEntityTypeConfiguration<RoomReview>
{
    public void Configure(EntityTypeBuilder<RoomReview> builder)
    {
        builder.ToTable("DanhGiaPhong");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Rating).HasColumnName("SoSao").IsRequired();
        builder.Property(x => x.Comment).HasColumnName("NhanXet").HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.HasIndex(x => new { x.ContractId, x.TenantAccountId }).IsUnique();
        builder.HasIndex(x => x.PostId);
    }
}

public class MonthlyBillConfiguration : IEntityTypeConfiguration<MonthlyBill>
{
    public void Configure(EntityTypeBuilder<MonthlyBill> builder)
    {
        builder.ToTable("HoaDonHangThang");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContractId).HasColumnName("HopDongId").IsRequired();
        builder.Property(x => x.Month).HasColumnName("Thang").IsRequired();
        builder.Property(x => x.Year).HasColumnName("Nam").IsRequired();
        builder.Property(x => x.OldElectricity).HasColumnName("SoDienCu").HasColumnType("decimal(18,2)");
        builder.Property(x => x.NewElectricity).HasColumnName("SoDienMoi").HasColumnType("decimal(18,2)");
        builder.Property(x => x.ElectricityPrice).HasColumnName("GiaDien").HasColumnType("decimal(18,2)");
        builder.Property(x => x.OldWater).HasColumnName("SoNuocCu").HasColumnType("decimal(18,2)");
        builder.Property(x => x.NewWater).HasColumnName("SoNuocMoi").HasColumnType("decimal(18,2)");
        builder.Property(x => x.WaterPrice).HasColumnName("GiaNuoc").HasColumnType("decimal(18,2)");
        builder.Property(x => x.RoomPrice).HasColumnName("TienPhong").HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtherFees).HasColumnName("ChiPhiKhac").HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(x => x.OtherFeesNote).HasColumnName("GhiChuChiPhiKhac").HasMaxLength(500);
        builder.Property(x => x.TotalAmount).HasColumnName("TongTien").HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnName("TrangThai").HasDefaultValue(0);
        builder.Property(x => x.DueDate).HasColumnName("HanThanhToan");
        builder.Property(x => x.PaidAt).HasColumnName("NgayThanhToan");
        builder.Property(x => x.PaymentMethod).HasColumnName("PhuongThucThanhToan").HasMaxLength(100);
        builder.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").HasDefaultValueSql("SYSDATETIME()");
        builder.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");

        builder.HasIndex(x => new { x.ContractId, x.Month, x.Year }).IsUnique();
        builder.HasOne(x => x.Contract)
            .WithMany(c => c.MonthlyBills)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

