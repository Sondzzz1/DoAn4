using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;
using RoomRental.Domain.Enums;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity ViewingAppointment
/// </summary>
public class ViewingAppointmentConfiguration : IEntityTypeConfiguration<ViewingAppointment>
{
    public void Configure(EntityTypeBuilder<ViewingAppointment> builder)
    {
        builder.ToTable("LichHenXemPhong");

        builder.HasKey(va => va.Id);

        builder.Property(va => va.TenantId)
            .HasColumnName("NguoiDungId")
            .IsRequired();

        builder.Property(va => va.PostId)
            .HasColumnName("TinDangId")
            .IsRequired();

        builder.Property(va => va.LandlordId)
            .HasColumnName("ChuTroId")
            .IsRequired();

        builder.Property(va => va.ScheduledAt)
            .HasColumnName("ThoiGianHen")
            .IsRequired();

        builder.Property(va => va.Status)
            .HasColumnName("TrangThai")
            .IsRequired()
            .HasDefaultValue(AppointmentStatus.Pending)
            .HasConversion<int>();

        builder.Property(va => va.TenantNote)
            .HasColumnName("NoiDung")
            .HasMaxLength(500);

        builder.Property(va => va.LandlordResponse)
            .HasColumnName("LyDoTuChoi")
            .HasMaxLength(500);

        builder.Property(va => va.CreatedAt)
            .HasColumnName("NgayTao")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(va => va.UpdatedAt)
            .HasColumnName("NgayCapNhat");

        builder.HasIndex(va => va.TenantId);
        builder.HasIndex(va => va.LandlordId);
        builder.HasIndex(va => va.PostId);
        builder.HasIndex(va => va.Status);
        builder.HasIndex(va => va.ScheduledAt);

        builder.HasOne(va => va.Tenant)
            .WithMany(u => u.Appointments)
            .HasForeignKey(va => va.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(va => va.Landlord)
            .WithMany(l => l.Appointments)
            .HasForeignKey(va => va.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(va => va.Post)
            .WithMany(p => p.Appointments)
            .HasForeignKey(va => va.PostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
