using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class ContractClosureConfiguration : IEntityTypeConfiguration<ContractClosure>, IEntityTypeConfiguration<ContractEndRequest>,
    IEntityTypeConfiguration<ContractSettlement>, IEntityTypeConfiguration<ContractSettlementLine>, IEntityTypeConfiguration<ContractClosureEvent>
{
    private static void Common<T>(EntityTypeBuilder<T> b) where T : class
    {
        b.HasKey("Id");
        foreach (var p in typeof(T).GetProperties())
        {
            if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?)) b.Property(p.Name).HasColumnType("decimal(18,2)");
            if (p.Name.EndsWith("AccountId")) b.HasOne<User>().WithMany().HasForeignKey(p.Name).OnDelete(DeleteBehavior.Restrict);
        }
        if (typeof(T).GetProperty("RowVersion") != null) b.Property<byte[]>("RowVersion").IsRowVersion();
    }
    public void Configure(EntityTypeBuilder<ContractClosure> b)
    {
        Common(b);
        b.ToTable("HoSoKetThucHopDong", t => {
            t.HasCheckConstraint("CK_Closure_State", "[HandoverStatus] BETWEEN 0 AND 4 AND ([EndKind] IS NULL OR [EndKind] BETWEEN 0 AND 2) AND ([HandoverKind] IS NULL OR [HandoverKind] IN (0,1)) AND [HandoverRevision]>=0 AND [BillingRevision]>=0");
            t.HasCheckConstraint("CK_Closure_Meters", "([ClosingElectricity] IS NULL OR [ClosingElectricity]>=0) AND ([ClosingWater] IS NULL OR [ClosingWater]>=0)");
            t.HasCheckConstraint("CK_Closure_Confirmed", "[HandoverStatus]<>3 OR ([TenantHandoverConfirmedByAccountId] IS NOT NULL AND [TenantHandoverConfirmedAtUtc] IS NOT NULL AND [LandlordHandoverConfirmedByAccountId] IS NOT NULL AND [LandlordHandoverConfirmedAtUtc] IS NOT NULL AND [HandoverKind] IS NOT NULL AND ([HandoverKind]=1 OR ([ActualHandoverAtUtc] IS NOT NULL AND [ClosingElectricity] IS NOT NULL AND [ClosingWater] IS NOT NULL)))");
            t.HasCheckConstraint("CK_Closure_Json", "[EvidenceJson] IS NULL OR ISJSON([EvidenceJson])=1");
            t.HasCheckConstraint("CK_Closure_Release", "[ReleasedAtUtc] IS NULL OR ([ReleasedByAccountId] IS NOT NULL AND [EffectiveEndAtUtc] IS NOT NULL AND [HandoverStatus]=3 AND [BillingFinalizedAtUtc] IS NOT NULL)");
        });
        b.HasIndex(x => x.ContractId).IsUnique();
        b.HasOne(x => x.Contract).WithOne().HasForeignKey<ContractClosure>(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.LastRentalDate).HasColumnType("date");
        b.Property(x => x.Condition).HasMaxLength(2000); b.Property(x => x.DamageNotes).HasMaxLength(2000);
    }
    public void Configure(EntityTypeBuilder<ContractEndRequest> b)
    {
        Common(b);
        b.ToTable("YeuCauKetThucHopDong", t => {
            t.HasCheckConstraint("CK_EndRequest_State", "[Status] BETWEEN 0 AND 5 AND LEN(LTRIM(RTRIM([Reason])))>=5");
            t.HasCheckConstraint("CK_EndRequest_Agreed", "[Status] NOT IN (1,5) OR ([RespondedByAccountId] IS NOT NULL AND [RespondedAtUtc] IS NOT NULL AND [AgreedEffectiveEndAtUtc] IS NOT NULL)");
            t.HasCheckConstraint("CK_EndRequest_Effective", "[Status]<>5 OR [EffectiveAtUtc] IS NOT NULL");
        });
        b.HasOne<ContractClosure>().WithMany().HasForeignKey(x => x.ClosureId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ClosureId).IsUnique().HasFilter("[Status] IN (0,1)");
        b.Property(x => x.Reason).HasMaxLength(1000); b.Property(x => x.ResponseNote).HasMaxLength(1000);
    }
    public void Configure(EntityTypeBuilder<ContractSettlement> b)
    {
        Common(b);
        b.ToTable("PhuongAnTatToanHopDong", t => {
            t.HasCheckConstraint("CK_Settlement_State", "[Status] BETWEEN 0 AND 6 AND [Revision]>0 AND [BillingRevisionSnapshot]>=0");
            t.HasCheckConstraint("CK_Settlement_Money", "[DepositAmountSnapshot]>=0 AND [TotalCharges]>=0 AND [RefundDue]>=0 AND [ExtraDue]>=0 AND ([RefundDue]=0 OR [ExtraDue]=0) AND [DepositAmountSnapshot]+[ExtraDue]=[TotalCharges]+[RefundDue]");
            t.HasCheckConstraint("CK_Settlement_Agreed", "[Status] NOT IN (2,3,4) OR ([TenantAgreedByAccountId] IS NOT NULL AND [TenantAgreedAtUtc] IS NOT NULL AND [LandlordAgreedByAccountId] IS NOT NULL AND [LandlordAgreedAtUtc] IS NOT NULL)");
            t.HasCheckConstraint("CK_Settlement_Completed", "[Status]<>4 OR ([CompletedAtUtc] IS NOT NULL AND [CompletedByAccountId] IS NOT NULL AND ([RefundDue]=0 OR ([RefundReference] IS NOT NULL AND [RefundReportedAtUtc] IS NOT NULL AND [RefundReportedByAccountId] IS NOT NULL AND [RefundReceivedAtUtc] IS NOT NULL AND [RefundReceivedByAccountId] IS NOT NULL)) AND ([ExtraDue]=0 OR ([ExtraReference] IS NOT NULL AND [ExtraReportedAtUtc] IS NOT NULL AND [ExtraReportedByAccountId] IS NOT NULL AND [ExtraReceivedAtUtc] IS NOT NULL AND [ExtraReceivedByAccountId] IS NOT NULL)))");
        });
        b.HasOne<ContractClosure>().WithMany().HasForeignKey(x => x.ClosureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Deposit>().WithMany().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClosureId, x.Revision }).IsUnique();
        b.HasIndex(x => x.ClosureId).IsUnique().HasFilter("[Status]<>6"); b.HasIndex(x => x.DepositId).IsUnique().HasFilter("[Status]<>6");
        b.Property(x => x.RefundReference).HasMaxLength(500); b.Property(x => x.ExtraReference).HasMaxLength(500); b.Property(x => x.Note).HasMaxLength(2000);
    }
    public void Configure(EntityTypeBuilder<ContractSettlementLine> b)
    {
        Common(b);
        b.ToTable("KhoanMucTatToanHopDong", t => {
            t.HasCheckConstraint("CK_SettlementLine_Amount", "[Amount]>0 AND [DepositAppliedAmount]>=0 AND [DepositAppliedAmount]<=[Amount]");
            t.HasCheckConstraint("CK_SettlementLine_Kind", "([Kind]=0 AND [MonthlyBillId] IS NOT NULL AND [BillStatusSnapshot] IS NOT NULL AND [BillTotalSnapshot] IS NOT NULL) OR ([Kind]=1 AND [MonthlyBillId] IS NULL AND [EvidenceJson] IS NOT NULL)");
            t.HasCheckConstraint("CK_SettlementLine_Json", "[EvidenceJson] IS NULL OR ISJSON([EvidenceJson])=1");
        });
        b.HasOne<MonthlyBill>().WithMany().HasForeignKey(x => x.MonthlyBillId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SettlementId, x.MonthlyBillId }).IsUnique().HasFilter("[MonthlyBillId] IS NOT NULL"); b.Property(x => x.Description).HasMaxLength(1000);
    }
    public void Configure(EntityTypeBuilder<ContractClosureEvent> b)
    {
        Common(b);
        b.ToTable("LichSuKetThucHopDong", t => {
            t.HasCheckConstraint("CK_ClosureEvent_Source", "[Source] IN (0,1) AND ([Source]=1 OR [ActorAccountId] IS NOT NULL)");
            t.HasCheckConstraint("CK_ClosureEvent_Json", "[SnapshotJson] IS NULL OR ISJSON([SnapshotJson])=1");
        });
        b.HasOne<ContractClosure>().WithMany().HasForeignKey(x => x.ClosureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ContractEndRequest>().WithMany().HasForeignKey(x => x.EndRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ContractSettlement>().WithMany().HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.EventType).HasMaxLength(80); b.Property(x => x.FromState).HasMaxLength(60); b.Property(x => x.ToState).HasMaxLength(60); b.Property(x => x.Reason).HasMaxLength(2000);
        b.Property(x => x.IdempotencyKey).HasMaxLength(120); b.HasIndex(x => x.IdempotencyKey).IsUnique(); b.HasIndex(x => new { x.ClosureId, x.OccurredAtUtc, x.Id });
    }
}
