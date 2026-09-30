using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DAL.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("GiaoDichThanhToan", table => table.HasCheckConstraint(
            "CK_GiaoDichThanhToan_ExactlyOneTarget",
            "([DepositId] IS NOT NULL AND [MonthlyBillId] IS NULL AND [TargetType] = N'Deposit') OR " +
            "([DepositId] IS NULL AND [MonthlyBillId] IS NOT NULL AND [TargetType] = N'MonthlyBill')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TransactionCode).HasMaxLength(100);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasDefaultValue(PaymentTransactionStatus.Pending);
        builder.Property(x => x.ResponseCode).HasMaxLength(20);
        builder.Property(x => x.OrderInfo).HasMaxLength(500);
        builder.Property(x => x.TargetType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.TransactionCode).IsUnique().HasFilter("[TransactionCode] IS NOT NULL");
        builder.HasIndex(x => x.MonthlyBillId);
        builder.HasOne(x => x.Deposit).WithMany().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MonthlyBill).WithMany().HasForeignKey(x => x.MonthlyBillId).OnDelete(DeleteBehavior.Restrict);
    }
}
