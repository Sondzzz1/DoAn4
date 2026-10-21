using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class SavedPaymentMethodConfiguration : IEntityTypeConfiguration<SavedPaymentMethod>
{
    public void Configure(EntityTypeBuilder<SavedPaymentMethod> b)
    {
        b.ToTable("PhuongThucThanhToanDaLuu", t =>
        {
            t.HasCheckConstraint("CK_SavedPaymentMethod_CardType", "[CardType] IN ('01','02')");
            t.HasCheckConstraint("CK_SavedPaymentMethod_ActiveToken", "([IsActive] = 1 AND [TokenCiphertext] IS NOT NULL AND LEN([TokenCiphertext]) > 0 AND [UnlinkedAtUtc] IS NULL) OR ([IsActive] = 0 AND [TokenCiphertext] IS NULL AND [UnlinkedAtUtc] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.MerchantCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.TokenCiphertext).HasMaxLength(2048);
        b.Property(x => x.TokenFingerprint).HasColumnType("char(64)").IsRequired();
        b.Property(x => x.BankCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.MaskedCardNumber).HasMaxLength(12).IsRequired();
        b.Property(x => x.CardType).HasColumnType("char(2)").IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.MerchantCode, x.TokenFingerprint }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.IsActive });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VnPayCardLinkConfiguration : IEntityTypeConfiguration<VnPayCardLink>
{
    public void Configure(EntityTypeBuilder<VnPayCardLink> b)
    {
        b.ToTable("LienKetTheVNPay", t =>
        {
            t.HasCheckConstraint("CK_VnPayCardLink_Status", "[Status] IN (0,1,2)");
            t.HasCheckConstraint("CK_VnPayCardLink_CardType", "[CardType] IN ('01','02')");
            t.HasCheckConstraint("CK_VnPayCardLink_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
            t.HasCheckConstraint("CK_VnPayCardLink_Result", "([Status] = 0 AND [CompletedAtUtc] IS NULL AND [SavedPaymentMethodId] IS NULL AND [CallbackFingerprint] IS NULL) OR ([Status] = 1 AND [CompletedAtUtc] IS NOT NULL AND [SavedPaymentMethodId] IS NOT NULL AND [CallbackFingerprint] IS NOT NULL) OR ([Status] = 2 AND [CompletedAtUtc] IS NOT NULL AND [SavedPaymentMethodId] IS NULL AND [CallbackFingerprint] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(100).IsRequired();
        b.Property(x => x.MerchantCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.CardType).HasColumnType("char(2)").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CallbackFingerprint).HasColumnType("char(64)");
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Reference).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status, x.ExpiresAtUtc });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SavedPaymentMethod).WithMany().HasForeignKey(x => x.SavedPaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}
