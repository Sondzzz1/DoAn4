namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity BinhLuan blog.
/// </summary>
public class BlogComment
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public int AccountId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual BlogPost BlogPost { get; set; } = null!;
    public virtual User Account { get; set; } = null!;
}
