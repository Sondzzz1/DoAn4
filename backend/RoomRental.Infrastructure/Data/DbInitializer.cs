using Microsoft.EntityFrameworkCore;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Data;

/// <summary>
/// Khởi tạo dữ liệu mặc định cho database
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Seed dữ liệu Admin mặc định
    /// </summary>
    public static async Task SeedAdminAsync(ApplicationDbContext context)
    {
        // Kiểm tra đã có Admin chưa
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null) return;

        var adminExists = await context.Users.AnyAsync(u => u.RoleId == adminRole.Id);
        if (adminExists) return;

        // Tạo Admin mặc định
        // Password: Admin@123
        var adminUser = new User
        {
            FullName = "Administrator",
            Email = "admin@roomrental.com",
            Phone = "0123456789",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            RoleId = adminRole.Id,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed một số Amenity mặc định
    /// </summary>
    public static async Task SeedAmenitiesAsync(ApplicationDbContext context)
    {
        if (await context.Amenities.AnyAsync()) return;

        var amenities = new List<Amenity>
        {
            new Amenity { Name = "Wifi", Icon = "wifi", Description = "Wifi miễn phí", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Điều hòa", Icon = "air-conditioner", Description = "Điều hòa nhiệt độ", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Nóng lạnh", Icon = "water-heater", Description = "Máy nước nóng lạnh", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Máy giặt", Icon = "washing-machine", Description = "Máy giặt chung", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Tủ lạnh", Icon = "refrigerator", Description = "Tủ lạnh", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Chỗ để xe", Icon = "parking", Description = "Chỗ để xe miễn phí", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "WC riêng", Icon = "toilet", Description = "Nhà vệ sinh riêng", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Ban công", Icon = "balcony", Description = "Ban công", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "Bếp", Icon = "kitchen", Description = "Bếp nấu ăn", CreatedAt = DateTime.UtcNow },
            new Amenity { Name = "An ninh", Icon = "security", Description = "Camera an ninh", CreatedAt = DateTime.UtcNow }
        };

        context.Amenities.AddRange(amenities);
        await context.SaveChangesAsync();
    }
}
