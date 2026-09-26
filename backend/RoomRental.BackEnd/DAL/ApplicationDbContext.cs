using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL;

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
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TenantProfile> TenantProfiles { get; set; } = null!;
    public DbSet<LandlordProfile> LandlordProfiles { get; set; } = null!;
    public DbSet<RoomCategory> RoomCategories { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<PostImage> PostImages { get; set; } = null!;
    public DbSet<Amenity> Amenities { get; set; } = null!;
    public DbSet<PostAmenity> PostAmenities { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;
    public DbSet<ViewingAppointment> ViewingAppointments { get; set; } = null!;
    public DbSet<Report> Reports { get; set; } = null!;
    public DbSet<BlogPost> BlogPosts { get; set; } = null!;
    public DbSet<BlogComment> BlogComments { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<RentalRequest> RentalRequests { get; set; } = null!;
    public DbSet<Deposit> Deposits { get; set; } = null!;
    public DbSet<RentalContract> RentalContracts { get; set; } = null!;
    public DbSet<MonthlyBill> MonthlyBills { get; set; } = null!;
    public DbSet<Incident> Incidents { get; set; } = null!;
    public DbSet<RoomReview> RoomReviews { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Áp dụng tất cả các Entity Configuration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

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
