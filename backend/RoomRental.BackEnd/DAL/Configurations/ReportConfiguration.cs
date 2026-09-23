using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("BaoCaoTinDang");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PostId).HasColumnName("TinDangId").IsRequired();
        builder.Property(x => x.ReporterAccountId).HasColumnName("NguoiBaoCaoId").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("LyDo").IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnName("TrangThai").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayBaoCao").IsRequired().HasDefaultValueSql("SYSDATETIME()");
        builder.Property(x => x.ResolvedAt).HasColumnName("NgayXuLy");
        builder.Property(x => x.ResolvedByAccountId).HasColumnName("NguoiXuLyId");

        builder.HasOne(x => x.Post)
            .WithMany(p => p.Reports)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ResolvedBy)
            .WithMany()
            .HasForeignKey(x => x.ResolvedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
