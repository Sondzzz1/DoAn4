using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class CashPaymentReceiptConfiguration : IEntityTypeConfiguration<CashPaymentReceipt>
{
    public void Configure(EntityTypeBuilder<CashPaymentReceipt> b)
    {
        b.ToTable("PhieuThuTienMat", t =>
        {
            t.HasCheckConstraint("CK_CashPayment_Target", "([DepositId] IS NOT NULL AND [MonthlyBillId] IS NULL) OR ([DepositId] IS NULL AND [MonthlyBillId] IS NOT NULL)");
            t.HasCheckConstraint("CK_CashPayment_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_CashPayment_Status", "[Status] IN (0,1,2)");
            t.HasCheckConstraint("CK_CashPayment_Actors", "[TenantAccountId] <> [LandlordAccountId]");
            t.HasCheckConstraint("CK_CashPayment_Received", "[Status] <> 1 OR ([ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedByAccountId] = [LandlordAccountId] AND [ReceiptNumber] IS NOT NULL AND LEN(LTRIM(RTRIM([ReceiptNumber]))) > 0 AND [CancelledAtUtc] IS NULL)");
            t.HasCheckConstraint("CK_CashPayment_Cancelled", "[Status] <> 2 OR [CancelledAtUtc] IS NOT NULL");
            t.HasCheckConstraint("CK_CashPayment_Unreceived", "[Status] = 1 OR ([ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ReceiptNumber] IS NULL)");
            t.HasCheckConstraint("CK_CashPayment_Selected", "[Status] <> 0 OR [CancelledAtUtc] IS NULL");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasDefaultValue(CashPaymentStatus.Selected);
        b.Property(x => x.SelectedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.ReceiptNumber).HasMaxLength(60);
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.DepositId).HasDatabaseName("UX_CashPayment_Deposit_Active").IsUnique().HasFilter("[DepositId] IS NOT NULL AND [Status] IN (0,1)");
        b.HasIndex(x => x.MonthlyBillId).HasDatabaseName("UX_CashPayment_Bill_Active").IsUnique().HasFilter("[MonthlyBillId] IS NOT NULL AND [Status] IN (0,1)");
        b.HasIndex(x => x.ReceiptNumber).HasDatabaseName("UX_CashPayment_ReceiptNumber").IsUnique().HasFilter("[ReceiptNumber] IS NOT NULL");
        b.HasIndex(x => new { x.TenantAccountId, x.Status });
        b.HasIndex(x => new { x.LandlordAccountId, x.Status });
        b.HasOne<Deposit>().WithMany().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MonthlyBill>().WithMany().HasForeignKey(x => x.MonthlyBillId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.TenantAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.LandlordAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ConfirmedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
