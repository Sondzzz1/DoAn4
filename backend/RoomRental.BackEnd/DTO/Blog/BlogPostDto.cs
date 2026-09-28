namespace RoomRental.BackEnd.DTO.Blog;

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
    public string? Title { get; set; }
    public string? TieuDe { get; set; }
    public string? Summary { get; set; }
    public string? TomTat { get; set; }
    public string? Content { get; set; }
    public string? NoiDung { get; set; }
    public string? ImageUrl { get; set; }
    public string? DuongDanAnh { get; set; }
    public int? Status { get; set; }
    public int? TrangThai { get; set; }

    public string GetTitle() => !string.IsNullOrWhiteSpace(Title) ? Title : (TieuDe ?? string.Empty);
    public string? GetSummary() => !string.IsNullOrWhiteSpace(Summary) ? Summary : TomTat;
    public string GetContent() => !string.IsNullOrWhiteSpace(Content) ? Content : (NoiDung ?? string.Empty);
    public string? GetImageUrl() => !string.IsNullOrWhiteSpace(ImageUrl) ? ImageUrl : DuongDanAnh;
    public int GetStatus() => Status ?? TrangThai ?? 1; // Published by default
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
    public string? Content { get; set; }
    public string? NoiDung { get; set; }

    public string GetContent() => !string.IsNullOrWhiteSpace(Content) ? Content : (NoiDung ?? string.Empty);
}
