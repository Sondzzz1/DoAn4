using Microsoft.EntityFrameworkCore;
using RoomRental.Application.DTOs.Blog;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Entities;
using RoomRental.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace RoomRental.Application.Services;

public class BlogService : IBlogService
{
    private readonly ApplicationDbContext _context;

    public BlogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BlogPostDto>> GetPublishedBlogPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Comments)
            .Where(b => b.Status == 1) // 1: Published
            .OrderByDescending(b => b.PublishedAt ?? b.UpdatedAt)
            .Select(b => MapToDto(b, false))
            .ToListAsync();
    }

    public async Task<List<BlogPostDto>> GetAllBlogPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Comments)
            .OrderByDescending(b => b.Id)
            .Select(b => MapToDto(b, false))
            .ToListAsync();
    }

    public async Task<BlogPostDto> GetBlogPostByIdAsync(int id, bool incrementView = true)
    {
        var blog = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Comments)
                .ThenInclude(c => c.Account)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blog == null)
        {
            throw new Exception("Không tìm thấy bài viết");
        }

        if (incrementView)
        {
            blog.ViewCount++;
            await _context.SaveChangesAsync();
        }

        return MapToDto(blog, true);
    }

    public async Task<BlogPostDto> GetBlogPostBySlugAsync(string slug, bool incrementView = true)
    {
        var blog = await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Comments)
                .ThenInclude(c => c.Account)
            .FirstOrDefaultAsync(b => b.Slug == slug);

        if (blog == null)
        {
            throw new Exception("Không tìm thấy bài viết");
        }

        if (incrementView)
        {
            blog.ViewCount++;
            await _context.SaveChangesAsync();
        }

        return MapToDto(blog, true);
    }

    public async Task<BlogPostDto> CreateBlogPostAsync(int authorAccountId, CreateBlogPostDto createDto)
    {
        if (string.IsNullOrWhiteSpace(createDto.Title))
        {
            throw new Exception("Tiêu đề bài viết không được để trống");
        }

        var slug = GenerateSlug(createDto.Title);

        var blog = new BlogPost
        {
            Title = createDto.Title.Trim(),
            Slug = slug,
            Summary = createDto.Summary,
            Content = createDto.Content,
            ImageUrl = createDto.ImageUrl,
            AuthorAccountId = authorAccountId,
            Status = createDto.Status,
            PublishedAt = createDto.Status == 1 ? DateTime.Now : null,
            ViewCount = 0
        };

        _context.BlogPosts.Add(blog);
        await _context.SaveChangesAsync();

        return await GetBlogPostByIdAsync(blog.Id, incrementView: false);
    }

    public async Task<BlogPostDto> UpdateBlogPostAsync(int id, UpdateBlogPostDto updateDto)
    {
        var blog = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == id);
        if (blog == null)
        {
            throw new Exception("Không tìm thấy bài viết");
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Title))
        {
            blog.Title = updateDto.Title.Trim();
            blog.Slug = GenerateSlug(updateDto.Title);
        }

        blog.Summary = updateDto.Summary;
        blog.Content = updateDto.Content;
        blog.ImageUrl = updateDto.ImageUrl;
        blog.Status = updateDto.Status;
        if (blog.Status == 1 && blog.PublishedAt == null)
        {
            blog.PublishedAt = DateTime.Now;
        }
        blog.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return await GetBlogPostByIdAsync(id, incrementView: false);
    }

    public async Task DeleteBlogPostAsync(int id)
    {
        var blog = await _context.BlogPosts
            .Include(b => b.Comments)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blog == null)
        {
            throw new Exception("Không tìm thấy bài viết");
        }

        _context.BlogComments.RemoveRange(blog.Comments);
        _context.BlogPosts.Remove(blog);
        await _context.SaveChangesAsync();
    }

    public async Task<BlogCommentDto> AddCommentAsync(int blogPostId, int accountId, CreateBlogCommentDto createDto)
    {
        var blog = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == blogPostId);
        if (blog == null)
        {
            throw new Exception("Không tìm thấy bài viết");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == accountId);
        if (user == null)
        {
            throw new Exception("Người dùng không tồn tại");
        }

        var comment = new BlogComment
        {
            BlogPostId = blogPostId,
            AccountId = accountId,
            Content = createDto.Content,
            CreatedAt = DateTime.Now
        };

        _context.BlogComments.Add(comment);
        await _context.SaveChangesAsync();

        return new BlogCommentDto
        {
            Id = comment.Id,
            BlogPostId = comment.BlogPostId,
            AccountId = comment.AccountId,
            AuthorName = user.FullName,
            AuthorAvatar = user.AvatarUrl,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task DeleteCommentAsync(int commentId, int accountId, bool isAdmin = false)
    {
        var comment = await _context.BlogComments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null)
        {
            throw new Exception("Không tìm thấy bình luận");
        }

        if (comment.AccountId != accountId && !isAdmin)
        {
            throw new Exception("Bạn không có quyền xóa bình luận này");
        }

        _context.BlogComments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    private static BlogPostDto MapToDto(BlogPost b, bool includeComments)
    {
        return new BlogPostDto
        {
            Id = b.Id,
            Title = b.Title,
            Slug = b.Slug,
            Summary = b.Summary,
            Content = b.Content,
            ImageUrl = b.ImageUrl,
            AuthorAccountId = b.AuthorAccountId,
            AuthorName = b.Author?.FullName ?? string.Empty,
            ViewCount = b.ViewCount,
            Status = b.Status,
            PublishedAt = b.PublishedAt,
            UpdatedAt = b.UpdatedAt,
            CommentCount = b.Comments?.Count ?? 0,
            Comments = includeComments && b.Comments != null
                ? b.Comments.OrderByDescending(c => c.CreatedAt).Select(c => new BlogCommentDto
                {
                    Id = c.Id,
                    BlogPostId = c.BlogPostId,
                    AccountId = c.AccountId,
                    AuthorName = c.Account?.FullName ?? string.Empty,
                    AuthorAvatar = c.Account?.AvatarUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).ToList()
                : new()
        };
    }

    private static string GenerateSlug(string title)
    {
        var str = title.ToLowerInvariant().Trim();
        str = Regex.Replace(str, @"\s+", "-");
        str = Regex.Replace(str, @"[^\w\-]", "");
        return $"{str}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
