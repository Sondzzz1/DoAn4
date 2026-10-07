using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

internal static class WorkflowLock
{
    // SQL Server transaction-owned lock serializes cross-target state changes and avoids lock-upgrade deadlocks.
    public static async Task AcquireAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlServer()) return;
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'RoomRental.Workflow',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 51020, 'Concurrent workflow update. Please retry.', 1;
            """, ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number == 51020)
        {
            throw BusinessRuleException.Conflict("Dữ liệu đang được xử lý bởi yêu cầu khác. Vui lòng thử lại.");
        }
    }
}
