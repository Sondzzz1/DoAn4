using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

internal static class RoomPublicationPolicy
{
    public static object?[] Capture(Room r) => new object?[]
    {
        r.RoomName, r.Description ?? "", r.Price, r.Area, r.CategoryId, r.MaxOccupants,
        r.Bedrooms, r.Bathrooms, r.Floor, r.Address, r.Province ?? "", r.District ?? "", r.Ward ?? "",
        r.Latitude, r.Longitude, r.ElectricityPrice, r.WaterPrice, r.ServiceFee,
        string.Join(",", r.RoomAmenities.Select(a => a.AmenityId).Distinct().Order()),
        string.Join("\n", r.Images.OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl))
    };

    public static async Task EnsureSlotAsync(ApplicationDbContext db, int roomId, int? exceptPostId = null)
    {
        if (await db.Posts.AnyAsync(p => p.RoomId == roomId && p.Id != exceptPostId &&
            (p.Status == PostStatus.Pending || p.Status == PostStatus.Approved)))
            throw BusinessRuleException.Conflict("Phòng đã có tin đang chờ duyệt hoặc đã được duyệt. Hãy chỉnh sửa hoặc ẩn tin hiện tại.");
    }

    public static async Task ReapproveAsync(ApplicationDbContext db, Room room)
    {
        var approved = await db.Posts.Where(p => p.RoomId == room.Id && p.Status == PostStatus.Approved).ToListAsync();
        foreach (var post in approved)
        {
            post.Status = PostStatus.Pending;
            post.ApprovedAt = null;
            post.RejectionReason = null;
            post.DisplayPrice = room.Price;
            post.UpdatedAt = DateTime.Now;
        }
    }

    public static async Task SaveAsync(ApplicationDbContext db)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627 or 1205)
        {
            throw BusinessRuleException.Conflict("Phòng đang có tin hoạt động hoặc vừa được xử lý bởi một yêu cầu khác. Vui lòng tải lại.");
        }
    }
}
