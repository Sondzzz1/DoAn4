using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

public class RoomCategoryConfiguration : IEntityTypeConfiguration<RoomCategory>
{
    public void Configure(EntityTypeBuilder<RoomCategory> builder)
    {
        builder.ToTable("DanhMucPhong");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasColumnName("TenDanhMuc").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(500);
        builder.Property(x => x.ImageUrl).HasColumnName("AnhDaiDien").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("TrangThai").IsRequired().HasDefaultValue(true);
    }
}
