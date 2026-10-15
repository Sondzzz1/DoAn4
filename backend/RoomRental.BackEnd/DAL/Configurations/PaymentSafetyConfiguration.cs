using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class PaymentCallbackEventConfiguration : IEntityTypeConfiguration<PaymentCallbackEvent>
{
    public void Configure(EntityTypeBuilder<PaymentCallbackEvent> b)
    {
        b.ToTable("SuKienThanhToan", t =>
        {
            t.HasCheckConstraint("CK_PaymentEvent_Source", "[Source] IN (0,1)");
            t.HasCheckConstraint("CK_PaymentEvent_State", "[ProcessingState] BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_PaymentEvent_Amount", "[ReportedAmount] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.ProviderOrderId).HasMaxLength(100).IsRequired();
        b.Property(x => x.ProviderTransactionCode).HasMaxLength(100);
        b.Property(x => x.DeduplicationKey).HasColumnType("char(64)").IsRequired();
        b.Property(x => x.ReportedAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.ResponseCode).HasColumnType("char(2)").IsRequired();
        b.Property(x => x.ProviderStatus).HasColumnType("char(2)").IsRequired();
        b.Property(x => x.ReceivedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.ProcessingState).HasDefaultValue((byte)0);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.ResolutionNote).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.DeduplicationKey).IsUnique();
        b.HasIndex(x => new { x.ProcessingState, x.ReceivedAtUtc });
        b.HasOne(x => x.PaymentTransaction).WithMany().HasForeignKey(x => x.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ResolvedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepositRefundRequestConfiguration : IEntityTypeConfiguration<DepositRefundRequest>
{
    public void Configure(EntityTypeBuilder<DepositRefundRequest> b)
    {
        b.ToTable("YeuCauHoanCoc", t =>
        {
            t.HasCheckConstraint("CK_DepositRefund_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_DepositRefund_PreviousStatus", "[PreviousDepositStatus] IN (1,2)");
            t.HasCheckConstraint("CK_DepositRefund_Status", "[Status] BETWEEN 0 AND 5");
            t.HasCheckConstraint("CK_DepositRefund_Evidence", "[Status] NOT IN (2,3) OR ([RefundReference] IS NOT NULL AND LEN(LTRIM(RTRIM([RefundReference]))) > 0 AND [RefundReportedByAccountId] IS NOT NULL AND [RefundReportedAtUtc] IS NOT NULL)");
            t.HasCheckConstraint("CK_DepositRefund_Completed", "[Status] <> 3 OR ([CompletedByAccountId] IS NOT NULL AND [CompletedAtUtc] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasDefaultValue((byte)0);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.RequestedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.DecisionNote).HasMaxLength(1000);
        b.Property(x => x.RefundReference).HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.DepositId).IsUnique().HasFilter("[Status] IN (0,1,2)");
        b.HasIndex(x => new { x.RequestedByAccountId, x.Status });
        b.HasIndex(x => new { x.ReviewedByAccountId, x.Status });
        b.HasOne(x => x.Deposit).WithMany().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RefundReportedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CompletedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
