using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DAL.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("TinNhan");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderId).HasColumnName("NguoiGuiId").IsRequired();
        builder.Property(x => x.ReceiverId).HasColumnName("NguoiNhanId").IsRequired();
        builder.Property(x => x.PostId).HasColumnName("BaiDangId");
        builder.Property(x => x.Message).HasColumnName("NoiDung").IsRequired().HasMaxLength(2000);
        builder.Property(x => x.IsRead).HasColumnName("DaDoc").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("NgayTao").IsRequired().HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Receiver)
            .WithMany()
            .HasForeignKey(x => x.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Post)
            .WithMany()
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.SenderId, x.ReceiverId });
        builder.HasIndex(x => x.CreatedAt);
    }
}
