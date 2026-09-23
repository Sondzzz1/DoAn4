using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class LandlordProfileConfiguration : IEntityTypeConfiguration<LandlordProfile>
{
    public void Configure(EntityTypeBuilder<LandlordProfile> builder)
    {
        builder.ToTable("ChuTro");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountId).HasColumnName("TaiKhoanId").IsRequired();
        builder.Property(x => x.Introduction).HasColumnName("GioiThieu").HasMaxLength(500);
        builder.Property(x => x.Address).HasColumnName("DiaChi").HasMaxLength(300);
        builder.Property(x => x.CitizenId).HasColumnName("SoCanCuoc").HasMaxLength(20);
        builder.Property(x => x.IsVerified).HasColumnName("DaXacThuc").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.VerifiedAt).HasColumnName("NgayXacThuc");

        builder.HasIndex(x => x.AccountId).IsUnique();
        builder.HasIndex(x => x.CitizenId)
            .IsUnique()
            .HasFilter("[SoCanCuoc] IS NOT NULL");
    }
}
