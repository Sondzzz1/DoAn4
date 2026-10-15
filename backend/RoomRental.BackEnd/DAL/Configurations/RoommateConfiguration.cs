using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class RoommatePostConfiguration : IEntityTypeConfiguration<RoommatePost>
{
    public void Configure(EntityTypeBuilder<RoommatePost> b)
    {
        b.ToTable("TinTimNguoiOGhep", t => {
            t.HasCheckConstraint("CK_RoommatePost_Slots", "[SoNguoiCanTim] >= 1");
            t.HasCheckConstraint("CK_RoommatePost_Share", "[ChiPhiDuKien] IS NULL OR [ChiPhiDuKien] >= 0");
            t.HasCheckConstraint("CK_RoommatePost_Status", "[TrangThai] BETWEEN 0 AND 5");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasColumnName("TieuDe").HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(4000).IsRequired();
        b.Property(x => x.RequestedSlots).HasColumnName("SoNguoiCanTim");
        b.Property(x => x.EstimatedShare).HasColumnName("ChiPhiDuKien").HasColumnType("decimal(18,2)");
        b.Property(x => x.Requirements).HasColumnName("YeuCau").HasMaxLength(2000);
        b.Property(x => x.RejectionReason).HasColumnName("LyDoTuChoi").HasMaxLength(500);
        b.Property(x => x.Status).HasColumnName("TrangThai");
        b.Property(x => x.CreatedAt).HasColumnName("NgayTao");
        b.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        b.Property(x => x.ApprovedAt).HasColumnName("NgayDuyet");
        b.Property(x => x.ClosedAt).HasColumnName("NgayDong");
        b.HasIndex(x => x.ContractId).IsUnique().HasFilter("[TrangThai] IN (0, 1)").HasDatabaseName("UX_RoommatePost_ActiveContract");
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorTenantAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class RoommateRequestConfiguration : IEntityTypeConfiguration<RoommateRequest>
{
    public void Configure(EntityTypeBuilder<RoommateRequest> b)
    {
        b.ToTable("YeuCauOGhep", t => t.HasCheckConstraint("CK_RoommateRequest_Status", "[TrangThai] BETWEEN 0 AND 4"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Message).HasColumnName("LoiNhan").HasMaxLength(1000);
        b.Property(x => x.Status).HasColumnName("TrangThai");
        b.Property(x => x.CreatedAt).HasColumnName("NgayTao");
        b.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        b.HasIndex(x => new { x.RoommatePostId, x.ApplicantTenantAccountId }).IsUnique()
            .HasFilter("[TrangThai] IN (0, 1)").HasDatabaseName("UX_RoommateRequest_ActiveApplicant");
        b.HasIndex(x => x.ApplicantTenantAccountId);
        b.HasOne(x => x.Post).WithMany(x => x.Requests).HasForeignKey(x => x.RoommatePostId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Applicant).WithMany().HasForeignKey(x => x.ApplicantTenantAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class ContractOccupantConfiguration : IEntityTypeConfiguration<ContractOccupant>
{
    public void Configure(EntityTypeBuilder<ContractOccupant> b)
    {
        b.ToTable("NguoiOCungHopDong", t => {
            t.HasCheckConstraint("CK_ContractOccupant_Status", "[TrangThai] IN (0, 1)");
            t.HasCheckConstraint("CK_ContractOccupant_Dates", "([TrangThai] = 0 AND [NgayRoi] IS NULL) OR ([TrangThai] = 1 AND [NgayRoi] IS NOT NULL AND [NgayRoi] >= [NgayVao])");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasColumnName("TrangThai");
        b.Property(x => x.JoinedAt).HasColumnName("NgayVao");
        b.Property(x => x.LeftAt).HasColumnName("NgayRoi");
        b.HasIndex(x => new { x.ContractId, x.TenantAccountId }).IsUnique().HasFilter("[TrangThai] = 0").HasDatabaseName("UX_ContractOccupant_ActiveTenant");
        b.HasIndex(x => x.TenantAccountId);
        b.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
