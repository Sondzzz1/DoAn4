using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.Domain.Entities;

namespace RoomRental.Infrastructure.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BaiViet");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasColumnName("TieuDe").IsRequired().HasMaxLength(300);
        builder.Property(x => x.Slug).HasColumnName("Slug").IsRequired().HasMaxLength(350);
        builder.Property(x => x.Summary).HasColumnName("TomTat").HasMaxLength(1000);
        builder.Property(x => x.Content).HasColumnName("NoiDung").IsRequired();
        builder.Property(x => x.ImageUrl).HasColumnName("AnhDaiDien").HasMaxLength(1000);
        builder.Property(x => x.AuthorAccountId).HasColumnName("TacGiaId").IsRequired();
        builder.Property(x => x.ViewCount).HasColumnName("LuotXem").IsRequired();
        builder.Property(x => x.Status).HasColumnName("TrangThai").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.PublishedAt).HasColumnName("NgayDang");
        builder.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");

        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
