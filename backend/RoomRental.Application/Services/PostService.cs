using Microsoft.EntityFrameworkCore;
using RoomRental.Application.DTOs.Post;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Entities;
using RoomRental.Domain.Enums;
using RoomRental.Infrastructure.Data;

namespace RoomRental.Application.Services;

/// <summary>
/// Service xử lý Post Management
/// </summary>
public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;

    public PostService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo post mới
    /// </summary>
    public async Task<PostDto> CreatePostAsync(int landlordId, CreatePostDto createDto)
    {
        // Verify user is Landlord
        var landlord = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == landlordId);

        if (landlord == null || landlord.Role.Name != "Landlord")
        {
            throw new Exception("Chỉ Landlord mới có thể đăng tin");
        }

        if (landlord.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa");
        }

        // Tạo Post
        var post = new Post
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Price = createDto.Price,
            LandlordId = landlordId,
            Status = PostStatus.Pending, // Mặc định Pending, chờ Admin duyệt
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Tạo Room
        var room = new Room
        {
            PostId = post.Id,
            Area = createDto.Area,
            MaxOccupants = createDto.MaxOccupants,
            Status = RoomStatus.Available,
            Province = createDto.Province,
            District = createDto.District,
            Ward = createDto.Ward,
            Address = createDto.Address,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        // Thêm Amenities
        if (createDto.AmenityIds.Any())
        {
            var postAmenities = createDto.AmenityIds.Select(amenityId => new PostAmenity
            {
                PostId = post.Id,
                AmenityId = amenityId
            }).ToList();

            _context.PostAmenities.AddRange(postAmenities);
        }

        // Thêm Images
        if (createDto.ImageUrls.Any())
        {
            var postImages = createDto.ImageUrls.Select((url, index) => new PostImage
            {
                PostId = post.Id,
                ImageUrl = url,
                DisplayOrder = index
            }).ToList();

            _context.PostImages.AddRange(postImages);
        }

        await _context.SaveChangesAsync();

        // Trả về PostDto
        return await GetPostByIdAsync(post.Id);
    }

    /// <summary>
    /// Lấy danh sách post của landlord
    /// </summary>
    public async Task<List<PostListDto>> GetMyPostsAsync(int landlordId)
    {
        var posts = await _context.Posts
            .Include(p => p.Room)
            .Include(p => p.Images)
            .Where(p => p.LandlordId == landlordId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Status = p.Status,
                Area = p.Room.Area,
                MaxOccupants = p.Room.MaxOccupants,
                RoomStatus = p.Room.Status,
                Province = p.Room.Province,
                District = p.Room.District,
                Ward = p.Room.Ward,
                ThumbnailUrl = p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null 
                    ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl 
                    : null,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt ?? p.CreatedAt
            })
            .ToListAsync();

        return posts;
    }

    /// <summary>
    /// Lấy chi tiết post
    /// </summary>
    public async Task<PostDto> GetPostByIdAsync(int postId)
    {
        var post = await _context.Posts
            .Include(p => p.Landlord)
            .Include(p => p.Room)
            .Include(p => p.PostAmenities)
                .ThenInclude(pa => pa.Amenity)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Description,
            Price = post.Price,
            Status = post.Status,
            RejectionReason = post.RejectionReason,
            LandlordId = post.LandlordId,
            LandlordName = post.Landlord.FullName,
            LandlordPhone = post.Landlord.Phone ?? string.Empty,
            RoomId = post.Room.Id,
            Area = post.Room.Area,
            MaxOccupants = post.Room.MaxOccupants,
            RoomStatus = post.Room.Status,
            Province = post.Room.Province,
            District = post.Room.District,
            Ward = post.Room.Ward,
            Address = post.Room.Address,
            Amenities = post.PostAmenities.Select(pa => new AmenityDto
            {
                Id = pa.Amenity.Id,
                Name = pa.Amenity.Name,
                Icon = pa.Amenity.Icon,
                Description = pa.Amenity.Description
            }).ToList(),
            ImageUrls = post.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .ToList(),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt ?? post.CreatedAt
        };
    }

    /// <summary>
    /// Cập nhật post
    /// </summary>
    public async Task<PostDto> UpdatePostAsync(int landlordId, int postId, UpdatePostDto updateDto)
    {
        var post = await _context.Posts
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (post.LandlordId != landlordId)
        {
            throw new Exception("Bạn không có quyền chỉnh sửa tin này");
        }

        // Không cho phép chỉnh sửa nếu đang Approved
        if (post.Status == PostStatus.Approved)
        {
            throw new Exception("Không thể chỉnh sửa tin đã được duyệt. Vui lòng ẩn tin trước khi chỉnh sửa");
        }

        // Update Post
        post.Title = updateDto.Title;
        post.Description = updateDto.Description;
        post.Price = updateDto.Price;
        post.UpdatedAt = DateTime.UtcNow;

        // Nếu bị Rejected, khi update thì chuyển về Pending
        if (post.Status == PostStatus.Rejected)
        {
            post.Status = PostStatus.Pending;
            post.RejectionReason = null;
        }

        // Update Room
        post.Room.Area = updateDto.Area;
        post.Room.MaxOccupants = updateDto.MaxOccupants;
        post.Room.Province = updateDto.Province;
        post.Room.District = updateDto.District;
        post.Room.Ward = updateDto.Ward;
        post.Room.Address = updateDto.Address;
        post.Room.UpdatedAt = DateTime.UtcNow;

        // Update Amenities
        var existingAmenities = await _context.PostAmenities
            .Where(pa => pa.PostId == postId)
            .ToListAsync();

        _context.PostAmenities.RemoveRange(existingAmenities);

        if (updateDto.AmenityIds.Any())
        {
            var newAmenities = updateDto.AmenityIds.Select(amenityId => new PostAmenity
            {
                PostId = postId,
                AmenityId = amenityId
            }).ToList();

            _context.PostAmenities.AddRange(newAmenities);
        }

        // Update Images
        var existingImages = await _context.PostImages
            .Where(pi => pi.PostId == postId)
            .ToListAsync();

        _context.PostImages.RemoveRange(existingImages);

        if (updateDto.ImageUrls.Any())
        {
            var newImages = updateDto.ImageUrls.Select((url, index) => new PostImage
            {
                PostId = postId,
                ImageUrl = url,
                DisplayOrder = index
            }).ToList();

            _context.PostImages.AddRange(newImages);
        }

        await _context.SaveChangesAsync();

        return await GetPostByIdAsync(postId);
    }

    /// <summary>
    /// Xóa post
    /// </summary>
    public async Task DeletePostAsync(int landlordId, int postId)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (post.LandlordId != landlordId)
        {
            throw new Exception("Bạn không có quyền xóa tin này");
        }

        // Không cho phép xóa nếu đang Approved (phải ẩn trước)
        if (post.Status == PostStatus.Approved)
        {
            throw new Exception("Không thể xóa tin đang được duyệt. Vui lòng ẩn tin trước");
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Cập nhật trạng thái post (Landlord chỉ được Hidden hoặc Pending)
    /// </summary>
    public async Task<PostDto> UpdatePostStatusAsync(int landlordId, int postId, PostStatus status)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (post.LandlordId != landlordId)
        {
            throw new Exception("Bạn không có quyền thay đổi trạng thái tin này");
        }

        // Landlord chỉ được phép chuyển sang Hidden hoặc Pending
        if (status != PostStatus.Hidden && status != PostStatus.Pending)
        {
            throw new Exception("Bạn chỉ có thể ẩn tin hoặc gửi duyệt lại");
        }

        // Nếu đang Hidden, có thể chuyển sang Pending để gửi duyệt lại
        // Nếu đang Approved, có thể chuyển sang Hidden để tạm ẩn
        post.Status = status;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPostByIdAsync(postId);
    }

    /// <summary>
    /// Lấy danh sách post công khai (cho Tenant)
    /// </summary>
    public async Task<List<PostListDto>> GetPublicPostsAsync(string? province = null, string? district = null)
    {
        var query = _context.Posts
            .Include(p => p.Room)
            .Include(p => p.Images)
            .Where(p => p.Status == PostStatus.Approved && p.Room.Status == RoomStatus.Available);

        if (!string.IsNullOrWhiteSpace(province))
        {
            query = query.Where(p => p.Room.Province.Contains(province));
        }

        if (!string.IsNullOrWhiteSpace(district))
        {
            query = query.Where(p => p.Room.District.Contains(district));
        }

        var posts = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Status = p.Status,
                Area = p.Room.Area,
                MaxOccupants = p.Room.MaxOccupants,
                RoomStatus = p.Room.Status,
                Province = p.Room.Province,
                District = p.Room.District,
                Ward = p.Room.Ward,
                ThumbnailUrl = p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null 
                    ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl 
                    : null,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt ?? p.CreatedAt
            })
            .ToListAsync();

        return posts;
    }
}
