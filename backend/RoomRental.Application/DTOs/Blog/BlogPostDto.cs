namespace RoomRental.Application.DTOs.Blog;

public class BlogPostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int AuthorAccountId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int Status { get; set; } // 0: Draft, 1: Published, 2: Hidden
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CommentCount { get; set; }
    public List<BlogCommentDto> Comments { get; set; } = new();
}

public class CreateBlogPostDto
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Status { get; set; } = 1; // Published by default
}

public class UpdateBlogPostDto : CreateBlogPostDto
{
}

public class BlogCommentDto
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public int AccountId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBlogCommentDto
{
    public string Content { get; set; } = string.Empty;
}
