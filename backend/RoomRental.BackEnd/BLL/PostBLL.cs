using Microsoft.EntityFrameworkCore;
using System.Data;
using RoomRental.BackEnd.DTO.Amenity;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

/// <summary>
/// Service xử lý Tin đăng (Posts) cho cả Tenant, Landlord và Public
/// </summary>
public class PostBLL : IPostService
{
    private readonly ApplicationDbContext _context;

    public PostBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tìm kiếm và lọc bài đăng công khai
    /// </summary>
    public async Task<List<PostListDto>> SearchPostsAsync(PostQueryParameters q) =>
        (await SearchPageAsync(q)).Items;

    public async Task<PostSearchResult> SearchPageAsync(PostQueryParameters q)
    {
        if (q.Latitude.HasValue != q.Longitude.HasValue ||
            q.Latitude is < -90 or > 90 || q.Longitude is < -180 or > 180)
            throw new BusinessRuleException("Cần tọa độ hợp lệ: latitude [-90,90], longitude [-180,180].");
        if (q.RadiusInKm.HasValue && (!double.IsFinite(q.RadiusInKm.Value) || q.RadiusInKm.Value <= 0 || q.RadiusInKm.Value > 500))
            throw new BusinessRuleException("Bán kính phải lớn hơn 0 và không quá 500 km.");
        if ((q.RadiusInKm.HasValue || q.SortBy == "distance") && !q.Latitude.HasValue)
            throw new BusinessRuleException("Cần chọn vị trí khi tìm theo bán kính hoặc khoảng cách.");
        if (q.MinPrice is < 0 || q.MaxPrice is < 0 || q.MinArea is < 0 || q.MaxArea is < 0 ||
            q.MinPrice > q.MaxPrice || q.MinArea > q.MaxArea)
            throw new BusinessRuleException("Khoảng giá hoặc diện tích không hợp lệ.");
        var query = _context.Posts
            .Include(p => p.Room)
                .ThenInclude(r => r.Category)
            .Include(p => p.Room)
                .ThenInclude(r => r.Images)
            .Include(p => p.Room)
                .ThenInclude(r => r.RoomAmenities)
            .Include(p => p.Landlord)
                .ThenInclude(l => l.Account)
            .AsNoTracking();

        // Core search only includes rooms that can currently be rented.
        query = query.Where(p => p.Status == PostStatus.Approved && p.Room.Status == RoomStatus.Available);

        // Lọc theo Landlord
        if (q.LandlordId.HasValue)
        {
            query = query.Where(p => p.LandlordId == q.LandlordId.Value || p.Landlord.AccountId == q.LandlordId.Value);
        }

        // Lọc theo từ khóa
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(keyword) ||
                p.Content.ToLower().Contains(keyword) ||
                p.Room.Address.ToLower().Contains(keyword) ||
                (p.Room.Ward != null && p.Room.Ward.ToLower().Contains(keyword)) ||
                (p.Room.District != null && p.Room.District.ToLower().Contains(keyword)) ||
                (p.Room.Province != null && p.Room.Province.ToLower().Contains(keyword)));
        }

        // Lọc theo địa điểm
        if (!string.IsNullOrWhiteSpace(q.Province))
        {
            var province = q.Province.Trim().ToLower();
            query = query.Where(p => p.Room.Province != null && p.Room.Province.ToLower().Contains(province));
        }

        if (!string.IsNullOrWhiteSpace(q.District))
        {
            var district = q.District.Trim().ToLower();
            query = query.Where(p => p.Room.District != null && p.Room.District.ToLower().Contains(district));
        }

        if (!string.IsNullOrWhiteSpace(q.Ward))
        {
            var ward = q.Ward.Trim().ToLower();
            query = query.Where(p => p.Room.Ward != null && p.Room.Ward.ToLower().Contains(ward));
        }

        // Lọc theo khoảng giá
        if (q.MinPrice.HasValue)
        {
            query = query.Where(p => p.Room.Price >= q.MinPrice.Value);
        }

        if (q.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Room.Price <= q.MaxPrice.Value);
        }

        // Lọc theo diện tích
        if (q.MinArea.HasValue)
        {
            query = query.Where(p => p.Room.Area >= q.MinArea.Value);
        }

        if (q.MaxArea.HasValue)
        {
            query = query.Where(p => p.Room.Area <= q.MaxArea.Value);
        }

        // Lọc theo danh mục
        if (q.CategoryId.HasValue && q.CategoryId.Value > 0)
        {
            query = query.Where(p => p.Room.CategoryId == q.CategoryId.Value);
        }

        // Lọc theo số người tối đa
        if (q.MaxOccupants.HasValue && q.MaxOccupants.Value > 0)
        {
            query = query.Where(p => p.Room.MaxOccupants >= q.MaxOccupants.Value);
        }

        // Lọc theo tiện ích (phòng phải có tất cả tiện ích được chọn)
        if (q.AmenityIds != null && q.AmenityIds.Any())
        {
            foreach (var amenityId in q.AmenityIds)
            {
                query = query.Where(p => p.Room.RoomAmenities.Any(ra => ra.AmenityId == amenityId));
            }
        }

        if (q.Latitude.HasValue)
        {
            var delta = (decimal)((q.RadiusInKm ?? 50) / 6371.0 * 180 / Math.PI);
            var lower = q.Latitude.Value - delta;
            var upper = q.Latitude.Value + delta;
            // Latitude band is a conservative bounding box, including polar/dateline searches.
            query = query.Where(p => p.Room.Latitude != null && p.Room.Longitude != null &&
                p.Room.Latitude >= lower && p.Room.Latitude <= upper &&
                p.Room.Latitude >= -90 && p.Room.Latitude <= 90 && p.Room.Longitude >= -180 && p.Room.Longitude <= 180);
        }

        // Sắp xếp
        query = q.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.Room.Price).ThenBy(p => p.Id),
            "price_desc" => query.OrderByDescending(p => p.Room.Price).ThenBy(p => p.Id),
            "area_asc" => query.OrderBy(p => p.Room.Area),
            "area_desc" => query.OrderByDescending(p => p.Room.Area),
            "views" => query.OrderByDescending(p => p.ViewCount),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        // Phân trang
        var page = q.PageNumber > 0 ? q.PageNumber : 1;
        var size = q.PageSize > 0 ? Math.Min(q.PageSize, 100) : 20;

        var postQuery = query
            .Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Room.Price,
                Status = p.Status,
                Area = p.Room.Area,
                MaxOccupants = p.Room.MaxOccupants,
                RoomStatus = p.Room.Status,
                Province = p.Room.Province ?? string.Empty,
                District = p.Room.District ?? string.Empty,
                Ward = p.Room.Ward ?? string.Empty,
                Address = p.Room.Address,
                Latitude = p.Room.Latitude,
                Longitude = p.Room.Longitude,
                CategoryId = p.Room.CategoryId,
                CategoryName = p.Room.Category.Name,
                LandlordId = p.LandlordId,
                LandlordName = p.Landlord.Account.FullName,
                LandlordPhone = p.Landlord.Account.Phone,
                ViewCount = p.ViewCount,
                ThumbnailUrl = p.Room.Images
                    .OrderByDescending(i => i.IsThumbnail)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                PostedAt = p.PostedAt,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });

        List<PostListDto> posts;
        int totalCount;

        // Nếu có tìm kiếm theo tọa độ và bán kính (Geolocation / Radius Search)
        if (q.Latitude.HasValue && q.Longitude.HasValue)
        {
            var targetLat = (double)q.Latitude.Value;
            var targetLng = (double)q.Longitude.Value;
            var maxRadius = q.RadiusInKm.HasValue && q.RadiusInKm.Value > 0 ? q.RadiusInKm.Value : 50.0;

            var allCandidates = await postQuery.ToListAsync();

            foreach (var post in allCandidates)
            {
                if (post.Latitude.HasValue && post.Longitude.HasValue)
                {
                    post.DistanceInKm = CalculateDistance(targetLat, targetLng, (double)post.Latitude.Value, (double)post.Longitude.Value);
                }
            }

            var filtered = allCandidates.Where(p => p.DistanceInKm.HasValue && p.DistanceInKm.Value <= maxRadius);

            if (q.SortBy?.ToLowerInvariant() == "distance" || string.IsNullOrWhiteSpace(q.SortBy))
            {
                filtered = filtered.OrderBy(p => p.DistanceInKm).ThenBy(p => p.Id);
            }

            totalCount = filtered.Count();
            posts = filtered
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();
        }
        else
        {
            totalCount = await query.CountAsync();
            posts = await postQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        foreach (var post in posts)
            if (post.DistanceInKm.HasValue) post.DistanceInKm = Math.Round(post.DistanceInKm.Value, 2);
        return new PostSearchResult { Items = posts, TotalCount = totalCount, PageNumber = page, PageSize = size };
    }

    /// <summary>
    /// Tính khoảng cách giữa 2 tọa độ theo công thức Haversine (km)
    /// </summary>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Bán kính trái đất tính bằng km
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return R * c;
    }

    /// <summary>
    /// Lấy chi tiết bài đăng
    /// </summary>
    public Task<PostDto> GetPostByIdAsync(int postId, bool incrementView = true) =>
        GetPostByIdInternalAsync(postId, incrementView, requirePublicVisibility: false);

    public Task<PostDto> GetPublicPostByIdAsync(int postId) =>
        GetPostByIdInternalAsync(postId, incrementView: true, requirePublicVisibility: true);

    public async Task<PostDto> GetMyPostByIdAsync(int accountId, int postId)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(accountId);
        var ownsPost = await _context.Posts.AnyAsync(p => p.Id == postId && p.LandlordId == landlord.Id);

        if (!ownsPost)
            throw BusinessRuleException.NotFound("Không tìm thấy tin đăng của bạn.");

        return await GetPostByIdAsync(postId, incrementView: false);
    }

    private async Task<PostDto> GetPostByIdInternalAsync(int postId, bool incrementView, bool requirePublicVisibility)
    {
        var post = await _context.Posts
            .Include(p => p.Landlord)
                .ThenInclude(l => l.Account)
            .Include(p => p.Room)
                .ThenInclude(r => r.Category)
            .Include(p => p.Room)
                .ThenInclude(r => r.Images)
            .Include(p => p.Room)
                .ThenInclude(r => r.RoomAmenities)
                    .ThenInclude(ra => ra.Amenity)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null || (requirePublicVisibility &&
            (post.Status != PostStatus.Approved ||
             post.Room.Status is not (RoomStatus.Available or RoomStatus.Rented or RoomStatus.Reserved))))
            throw BusinessRuleException.NotFound("Không tìm thấy tin đăng");

        if (incrementView)
        {
            post.ViewCount++;
            await RoomPublicationPolicy.SaveAsync(_context);
        }

        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Content,
            Price = post.Room.Price,
            Status = post.Status,
            RejectionReason = post.RejectionReason,
            ViewCount = post.ViewCount,

            CategoryId = post.Room.CategoryId,
            CategoryName = post.Room.Category?.Name ?? string.Empty,

            LandlordId = post.LandlordId,
            LandlordAccountId = post.Landlord.AccountId,
            LandlordName = post.Landlord.Account.FullName,
            LandlordPhone = post.Landlord.Account.Phone ?? string.Empty,
            LandlordAvatar = post.Landlord.Account.AvatarUrl,

            RoomId = post.Room.Id,
            RoomName = post.Room.RoomName,
            Area = post.Room.Area,
            MaxOccupants = post.Room.MaxOccupants,
            CurrentOccupants = post.Room.CurrentOccupants,
            Bedrooms = post.Room.Bedrooms,
            Bathrooms = post.Room.Bathrooms,
            Floor = post.Room.Floor,
            RoomStatus = post.Room.Status,

            Province = post.Room.Province ?? string.Empty,
            District = post.Room.District ?? string.Empty,
            Ward = post.Room.Ward ?? string.Empty,
            Address = post.Room.Address,
            Latitude = post.Room.Latitude,
            Longitude = post.Room.Longitude,

            ElectricityPrice = post.Room.ElectricityPrice,
            WaterPrice = post.Room.WaterPrice,
            ServiceFee = post.Room.ServiceFee,

            Amenities = post.Room.RoomAmenities
                .Where(ra => ra.Amenity != null && ra.Amenity.IsActive)
                .Select(ra => new AmenityDto
                {
                    Id = ra.Amenity.Id,
                    Name = ra.Amenity.Name,
                    Icon = ra.Amenity.Icon,
                    Description = ra.Amenity.Description
                }).ToList(),

            ImageUrls = post.Room.Images
                .OrderByDescending(i => i.IsThumbnail)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .ToList(),

            PostedAt = post.PostedAt,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }

    /// <summary>
    /// Landlord đăng bài mới
    /// </summary>
    public async Task<PostDto> CreatePostAsync(int accountId, CreatePostDto createDto)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(accountId);

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (transaction != null) await WorkflowLock.AcquireAsync(_context);
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == createDto.RoomId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy phòng.");
        if (room.LandlordId != landlord.Id)
            throw BusinessRuleException.Forbidden("Bạn không thể đăng tin cho phòng của chủ trọ khác.");
        if (room.Status != RoomStatus.Available)
            throw BusinessRuleException.Conflict("Chỉ có thể đăng tin cho phòng còn trống.");
        await RoomPublicationPolicy.EnsureSlotAsync(_context, room.Id);
        var title = createDto.GetTitle().Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 300)
            throw new BusinessRuleException("Tiêu đề phải có từ 1 đến 300 ký tự.");
        var post = new Post
        {
            RoomId = room.Id, LandlordId = landlord.Id, Title = title,
            Content = createDto.GetDescription().Trim(), DisplayPrice = room.Price,
            Status = PostStatus.Pending, CreatedAt = DateTime.Now
        };
        _context.Posts.Add(post);
        await RoomPublicationPolicy.SaveAsync(_context);
        if (transaction != null) await transaction.CommitAsync();

        return await GetPostByIdAsync(post.Id, incrementView: false);
    }

    /// <summary>
    /// Lấy danh sách tin của Landlord
    /// </summary>
    public async Task<List<PostListDto>> GetMyPostsAsync(int accountId)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(accountId);

        var posts = await _context.Posts
            .Include(p => p.Room)
                .ThenInclude(r => r.Category)
            .Include(p => p.Room)
                .ThenInclude(r => r.Images)
            .Include(p => p.Landlord)
                .ThenInclude(l => l.Account)
            .Where(p => p.LandlordId == landlord.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Room.Price,
                Status = p.Status,
                Area = p.Room.Area,
                MaxOccupants = p.Room.MaxOccupants,
                RoomStatus = p.Room.Status,
                Province = p.Room.Province ?? string.Empty,
                District = p.Room.District ?? string.Empty,
                Ward = p.Room.Ward ?? string.Empty,
                Address = p.Room.Address,
                Latitude = p.Room.Latitude,
                Longitude = p.Room.Longitude,
                CategoryId = p.Room.CategoryId,
                CategoryName = p.Room.Category.Name,
                LandlordId = p.LandlordId,
                LandlordName = p.Landlord.Account.FullName,
                LandlordPhone = p.Landlord.Account.Phone,
                ViewCount = p.ViewCount,
                ThumbnailUrl = p.Room.Images
                    .OrderByDescending(i => i.IsThumbnail)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                PostedAt = p.PostedAt,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return posts;
    }

    /// <summary>
    /// Cập nhật tin đăng
    /// </summary>
    public async Task<PostDto> UpdatePostAsync(int accountId, int postId, UpdatePostDto updateDto)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(accountId);

        var post = await _context.Posts
            .Include(p => p.Room)
                .ThenInclude(r => r.Images)
            .Include(p => p.Room)
                .ThenInclude(r => r.RoomAmenities)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (post.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền chỉnh sửa tin đăng này");
        }

        // Room fields are edited through RoomBLL so every publication follows the same moderation policy.
        if (updateDto.GetPrice().HasValue || updateDto.GetArea().HasValue || updateDto.GetCategoryId().HasValue ||
            updateDto.GetMaxOccupants().HasValue || updateDto.Address != null || updateDto.DiaChi != null ||
            updateDto.Province != null || updateDto.ThanhPho != null || updateDto.District != null || updateDto.Quan != null ||
            updateDto.Ward != null || updateDto.Phuong != null || updateDto.GetLatitude().HasValue ||
            updateDto.GetLongitude().HasValue || updateDto.ElectricityPrice.HasValue || updateDto.WaterPrice.HasValue ||
            updateDto.ServiceFee.HasValue || updateDto.AmenityIds != null || updateDto.ImageUrls != null)
            throw new BusinessRuleException("Vui lòng chỉnh sửa dữ liệu phòng tại Quản lý phòng.");
        var title = updateDto.GetTitle()?.Trim() ?? post.Title;
        var content = updateDto.GetDescription()?.Trim() ?? post.Content;
        if (string.IsNullOrWhiteSpace(title) || title.Length > 300)
            throw new BusinessRuleException("Tiêu đề phải có từ 1 đến 300 ký tự.");
        if (title != post.Title || content != post.Content)
        {
            await RoomPublicationPolicy.EnsureSlotAsync(_context, post.RoomId, post.Id);
            post.Title = title;
            post.Content = content;
            post.DisplayPrice = post.Room.Price;
            post.Status = PostStatus.Pending;
            post.ApprovedAt = null;
            post.RejectionReason = null;
            post.UpdatedAt = DateTime.Now;
            await RoomPublicationPolicy.SaveAsync(_context);
        }

        return await GetPostByIdAsync(postId, incrementView: false);
    }

    /// <summary>
    /// Xóa tin đăng (Mục 14: Xóa tin / Ẩn tin)
    /// </summary>
    public async Task DeletePostAsync(int accountId, int postId)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(accountId);

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (post.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền xóa tin đăng này");
        }

        // Đổi trạng thái sang Hidden thay vì xóa cứng khỏi Database
        post.Status = PostStatus.Hidden;
        post.UpdatedAt = DateTime.Now;

        await RoomPublicationPolicy.SaveAsync(_context);
    }

    /// <summary>
    /// Cập nhật trạng thái post bởi Landlord
    /// </summary>
    public async Task<PostDto> UpdatePostStatusAsync(int accountId, int postId, PostStatus status)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(accountId);

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (post.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền đổi trạng thái tin này");
        }

        // Landlord có thể ẩn tin (Hidden) hoặc gửi duyệt lại (Pending)
        if (status != PostStatus.Hidden && status != PostStatus.Pending)
        {
            throw new Exception("Chủ trọ chỉ có thể ẩn tin hoặc gửi yêu cầu duyệt lại");
        }

        if (status == PostStatus.Pending)
        {
            if (!await _context.Rooms.AnyAsync(r => r.Id == post.RoomId && r.Status == RoomStatus.Available))
                throw BusinessRuleException.Conflict("Chỉ gửi duyệt tin khi phòng còn trống.");
            await RoomPublicationPolicy.EnsureSlotAsync(_context, post.RoomId, post.Id);
            post.ApprovedAt = null;
            post.RejectionReason = null;
        }
        post.Status = status;
        post.UpdatedAt = DateTime.Now;

        await RoomPublicationPolicy.SaveAsync(_context);

        return await GetPostByIdAsync(postId, incrementView: false);
    }

    private async Task<LandlordProfile> GetOrCreateLandlordProfileAsync(int accountId)
    {
        var user = await _context.Users
            .Include(u => u.LandlordProfile)
            .FirstOrDefaultAsync(u => u.Id == accountId);

        if (user == null)
        {
            throw new Exception("Người dùng không tồn tại");
        }

        if (user.RoleId != 2)
        {
            throw BusinessRuleException.Forbidden("Chỉ chủ trọ mới có quyền thực hiện thao tác này.");
        }

        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa");
        }

        if (user.LandlordProfile != null)
        {
            return user.LandlordProfile;
        }

        var profile = new LandlordProfile
        {
            AccountId = user.Id
        };

        _context.LandlordProfiles.Add(profile);
        await RoomPublicationPolicy.SaveAsync(_context);

        return profile;
    }
}
