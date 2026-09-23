using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public string Title { get; set; } = string.Empty;
    public string TieuDe { get => Title; set => Title = value; }
    [JsonIgnore]
    public string? Summary { get; set; }
    public string? TomTat { get => Summary; set => Summary = value; }
    [JsonIgnore]
    public string Content { get; set; } = string.Empty;
    public string NoiDung { get => Content; set => Content = value; }
    [JsonIgnore]
    public string? ImageUrl { get; set; }
    public string? DuongDanAnh { get => ImageUrl; set => ImageUrl = value; }
    [JsonIgnore]
    public int Status { get; set; } = 1; // Published by default
    public int TrangThai { get => Status; set => Status = value; }
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
    [JsonIgnore]
    public string Content { get; set; } = string.Empty;
    public string NoiDung { get => Content; set => Content = value; }
}
