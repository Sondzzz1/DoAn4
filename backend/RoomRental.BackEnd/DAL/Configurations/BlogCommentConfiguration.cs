using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class BlogCommentConfiguration : IEntityTypeConfiguration<BlogComment>
{
    public void Configure(EntityTypeBuilder<BlogComment> builder)
    {
        builder.ToTable("BinhLuan");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BlogPostId).HasColumnName("BaiVietId").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("TaiKhoanId").IsRequired();
        builder.Property(x => x.Content).HasColumnName("NoiDung").IsRequired().HasMaxLength(1000);
        builder.Property(x => x.IsActive).HasColumnName("TrangThai").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").IsRequired().HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(x => x.BlogPost)
            .WithMany(p => p.Comments)
            .HasForeignKey(x => x.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
