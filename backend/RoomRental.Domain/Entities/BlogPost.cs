namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity BaiViet.
/// </summary>
public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int AuthorAccountId { get; set; }
    public int ViewCount { get; set; }
    public int Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual User Author { get; set; } = null!;
    public virtual ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
}
