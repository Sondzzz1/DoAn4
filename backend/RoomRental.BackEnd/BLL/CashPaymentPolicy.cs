using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public static class CashPaymentSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        if (!db.Database.IsRelational()) return;
        var ready = await db.Database.SqlQueryRaw<int>("SELECT CAST(CASE WHEN OBJECT_ID(N'dbo.PhieuThuTienMat', N'U') IS NOT NULL THEN 1 ELSE 0 END AS int) AS [Value]").SingleAsync();
        if (ready != 1)
            throw new BusinessRuleException("Chức năng thu tiền mặt cần cập nhật SQL 11_CashPayments.sql trước khi sử dụng.", StatusCodes.Status503ServiceUnavailable);
    }
}

public static class CashPaymentPolicy
{
    public static async Task EnsureNoOnlineConflictAsync(ApplicationDbContext db, int? depositId, int? monthlyBillId)
    {
        await PaymentSafetySchema.EnsureAsync(db);
        if (await db.PaymentTransactions.AnyAsync(p =>
            ((depositId.HasValue && p.DepositId == depositId) || (monthlyBillId.HasValue && p.MonthlyBillId == monthlyBillId)) &&
            (p.Status == PaymentTransactionStatus.Pending || p.Status == PaymentTransactionStatus.Succeeded)))
            throw BusinessRuleException.Conflict("Khoản thu có giao dịch VNPay đang xử lý hoặc đã thành công; chưa được ghi nhận tiền mặt.");
        if (await db.PaymentCallbackEvents.AnyAsync(e => e.ProcessingState == PaymentEventState.RequiresReview && e.PaymentTransaction != null &&
            ((depositId.HasValue && e.PaymentTransaction.DepositId == depositId) ||
             (monthlyBillId.HasValue && e.PaymentTransaction.MonthlyBillId == monthlyBillId))))
            throw BusinessRuleException.Conflict("Khoản thu có giao dịch cần đối soát; chưa được ghi nhận tiền mặt.");
    }

    public static async Task EnsureNoCashReceiptAsync(ApplicationDbContext db, int? depositId, int? monthlyBillId)
    {
        await CashPaymentSchema.EnsureAsync(db);
        if (await db.CashPaymentReceipts.AnyAsync(r =>
            ((depositId.HasValue && r.DepositId == depositId) || (monthlyBillId.HasValue && r.MonthlyBillId == monthlyBillId)) &&
            (r.Status == CashPaymentStatus.Selected || r.Status == CashPaymentStatus.Received)))
            throw BusinessRuleException.Conflict("Khoản thu đã chọn hoặc đã thanh toán tiền mặt. Hãy hủy lựa chọn tiền mặt trước khi thanh toán VNPay.");
    }

    // Read-only preview for a cash choice; settlement consent is invalidated only when money is actually received.
    public static async Task EnsureBillCollectionAllowedAsync(ApplicationDbContext db, int contractId)
    {
        await ContractClosurePolicy.EnsureSchemaAsync(db);
        var closure = await db.ContractClosures.AsNoTracking().FirstOrDefaultAsync(h => h.ContractId == contractId);
        if (closure == null) return;
        var plan = await db.ContractSettlements.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClosureId == closure.Id && p.Status != SettlementState.Superseded);
        if (closure.ReleasedAtUtc != null || plan?.Status == SettlementState.Completed || plan?.RefundReportedAtUtc != null || plan?.ExtraReportedAtUtc != null)
            throw BusinessRuleException.Conflict("Công nợ đã tất toán hoặc đã báo chuyển tiền; cần đối soát trước khi thu hóa đơn.");
    }
}
