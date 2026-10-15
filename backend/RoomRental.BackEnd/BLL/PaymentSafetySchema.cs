using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

internal static class PaymentSafetySchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        if (!db.Database.IsRelational()) return;
        var ready = await db.Database.SqlQueryRaw<int>("SELECT CAST(CASE WHEN OBJECT_ID(N'dbo.SuKienThanhToan', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.YeuCauHoanCoc', N'U') IS NOT NULL THEN 1 ELSE 0 END AS int) AS [Value]").SingleAsync();
        if (ready != 1) throw new BusinessRuleException("Chức năng thanh toán/hoàn cọc cần cập nhật SQL 08_PaymentSafety.sql trước khi sử dụng.", 503);
    }
}
