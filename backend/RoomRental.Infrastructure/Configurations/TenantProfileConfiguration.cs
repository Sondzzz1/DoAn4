using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

public class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
{
    public void Configure(EntityTypeBuilder<TenantProfile> builder)
    {
        builder.ToTable("NguoiDung");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountId).HasColumnName("TaiKhoanId").IsRequired();
        builder.Property(x => x.BirthDate).HasColumnName("NgaySinh");
        builder.Property(x => x.Gender).HasColumnName("GioiTinh").HasMaxLength(20);
        builder.Property(x => x.Occupation).HasColumnName("NgheNghiep").HasMaxLength(100);
        builder.Property(x => x.CurrentAddress).HasColumnName("DiaChiHienTai").HasMaxLength(300);
        builder.Property(x => x.Introduction).HasColumnName("GioiThieu").HasMaxLength(500);

        builder.HasIndex(x => x.AccountId).IsUnique();
    }
}
