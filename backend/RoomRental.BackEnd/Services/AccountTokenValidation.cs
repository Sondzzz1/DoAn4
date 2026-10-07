using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using System.Security.Claims;

namespace RoomRental.BackEnd.Services;

public static class AccountTokenValidation
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var id = context.Principal?.FindFirst("UserId")?.Value;
        var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
        if (!int.TryParse(id, out var accountId)) { context.Fail("Token không hợp lệ."); return; }
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == accountId);
        if (user == null || !user.IsActive || user.RoleName != role)
            context.Fail("Tài khoản đã bị khóa hoặc quyền đã thay đổi.");
    }
}
