using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL;

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
        const string adminEmail = "admin@roomrental.com";
        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail || u.UserName == adminEmail);

        if (adminUser != null)
        {
            var changed = false;

            if (adminUser.RoleId != 0)
            {
                adminUser.RoleId = 0;
                changed = true;
            }

            if (!adminUser.IsActive)
            {
                adminUser.IsActive = true;
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync();
            }

            return;
        }

        adminUser = new User
        {
            UserName = adminEmail,
            FullName = "Administrator",
            Email = adminEmail,
            Phone = "0123456789",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            RoleId = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }

    public static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.RoomCategories.AnyAsync()) return;

        context.RoomCategories.AddRange(
            new RoomCategory { Name = "Phòng trọ", Description = "Phòng trọ phổ thông" },
            new RoomCategory { Name = "Ở ghép", Description = "Phòng ở ghép" },
            new RoomCategory { Name = "Nhà nguyên căn", Description = "Nhà nguyên căn cho thuê" },
            new RoomCategory { Name = "Căn hộ", Description = "Căn hộ dịch vụ, căn hộ mini" },
            new RoomCategory { Name = "Chung cư mini", Description = "Chung cư mini cho thuê" }
        );

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
            new Amenity { Name = "Wifi", Icon = "wifi", Description = "Wifi miễn phí" },
            new Amenity { Name = "Điều hòa", Icon = "air-conditioner", Description = "Điều hòa nhiệt độ" },
            new Amenity { Name = "Nóng lạnh", Icon = "water-heater", Description = "Máy nước nóng lạnh" },
            new Amenity { Name = "Máy giặt", Icon = "washing-machine", Description = "Máy giặt chung" },
            new Amenity { Name = "Tủ lạnh", Icon = "refrigerator", Description = "Tủ lạnh" },
            new Amenity { Name = "Chỗ để xe", Icon = "parking", Description = "Chỗ để xe miễn phí" },
            new Amenity { Name = "WC riêng", Icon = "toilet", Description = "Nhà vệ sinh riêng" },
            new Amenity { Name = "Ban công", Icon = "balcony", Description = "Ban công" },
            new Amenity { Name = "Bếp", Icon = "kitchen", Description = "Bếp nấu ăn" },
            new Amenity { Name = "An ninh", Icon = "security", Description = "Camera an ninh" }
        };

        context.Amenities.AddRange(amenities);
        await context.SaveChangesAsync();
    }
}
