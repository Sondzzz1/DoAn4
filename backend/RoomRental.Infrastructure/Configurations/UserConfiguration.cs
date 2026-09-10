using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

/// <summary>
/// Configuration cho Entity User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Tên bảng
        builder.ToTable("Users");

        // Primary Key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.IsBlocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index - Email phải unique
        builder.HasIndex(u => u.Email)
            .IsUnique();

        // Relationships
        // User -> Role
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // User (Landlord) -> Posts
        builder.HasMany(u => u.Posts)
            .WithOne(p => p.Landlord)
            .HasForeignKey(p => p.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        // User (Tenant) -> Favorites
        builder.HasMany(u => u.Favorites)
            .WithOne(f => f.User)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User (Tenant) -> Appointments
        builder.HasMany(u => u.Appointments)
            .WithOne(a => a.Tenant)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
