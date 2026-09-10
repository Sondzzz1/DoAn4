using Microsoft.EntityFrameworkCore;
using RoomRental.Domain.Entities;
using RoomRental.Domain.Enums;

namespace RoomRental.Infrastructure.Data;

/// <summary>
/// ApplicationDbContext - DbContext chính của hệ thống
/// Quản lý tất cả Entity và kết nối với SQL Server
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSet cho các Entity
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<PostImage> PostImages { get; set; } = null!;
    public DbSet<Amenity> Amenities { get; set; } = null!;
    public DbSet<PostAmenity> PostAmenities { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;
    public DbSet<ViewingAppointment> ViewingAppointments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Áp dụng tất cả các Entity Configuration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Seed dữ liệu mặc định cho Role
        SeedRoles(modelBuilder);
    }

    /// <summary>
    /// Seed 3 Role mặc định: Tenant, Landlord, Admin
    /// </summary>
    private void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = 1,
                Name = "Tenant",
                Description = "Người tìm trọ - Có thể tìm kiếm, xem phòng và đặt lịch xem phòng"
            },
            new Role
            {
                Id = 2,
                Name = "Landlord",
                Description = "Chủ nhà trọ - Có thể đăng tin cho thuê phòng và quản lý tin đăng"
            },
            new Role
            {
                Id = 3,
                Name = "Admin",
                Description = "Quản trị viên - Quản lý toàn bộ hệ thống, duyệt tin đăng"
            }
        );
    }

    /// <summary>
    /// Override SaveChangesAsync để tự động cập nhật UpdatedAt
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
