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
        // Tên bảng
        builder.ToTable("ViewingAppointments");

        // Primary Key
        builder.HasKey(va => va.Id);

        // Properties
        builder.Property(va => va.ScheduledAt)
            .IsRequired();

        builder.Property(va => va.Status)
            .IsRequired()
            .HasDefaultValue(AppointmentStatus.Pending)
            .HasConversion<int>(); // Lưu Enum dưới dạng int

        builder.Property(va => va.TenantNote)
            .HasMaxLength(500);

        builder.Property(va => va.LandlordResponse)
            .HasMaxLength(500);

        builder.Property(va => va.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index
        builder.HasIndex(va => va.TenantId);
        builder.HasIndex(va => va.PostId);
        builder.HasIndex(va => va.Status);
        builder.HasIndex(va => va.ScheduledAt);

        // Relationships
        // ViewingAppointment -> Tenant (User)
        builder.HasOne(va => va.Tenant)
            .WithMany(u => u.Appointments)
            .HasForeignKey(va => va.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // ViewingAppointment -> Post
        builder.HasOne(va => va.Post)
            .WithMany(p => p.Appointments)
            .HasForeignKey(va => va.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
