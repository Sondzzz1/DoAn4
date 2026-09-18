using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

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
