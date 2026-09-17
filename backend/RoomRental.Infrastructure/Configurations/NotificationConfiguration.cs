using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("ThongBao");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountId).HasColumnName("TaiKhoanId").IsRequired();
        builder.Property(x => x.Title).HasColumnName("TieuDe").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Content).HasColumnName("NoiDung").IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Type).HasColumnName("LoaiThongBao");
        builder.Property(x => x.Link).HasColumnName("LienKet").HasMaxLength(500);
        builder.Property(x => x.IsRead).HasColumnName("DaDoc").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").IsRequired().HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
