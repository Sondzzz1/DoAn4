using Microsoft.EntityFrameworkCore;
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
    public async Task<List<PostListDto>> SearchPostsAsync(PostQueryParameters q)
    {
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

        // Mặc định chỉ lấy Approved cho tìm kiếm công khai, trừ khi có filter status cụ thể
        if (q.Status.HasValue)
        {
            query = query.Where(p => (int)p.Status == q.Status.Value);
        }
        else
        {
            query = query.Where(p => p.Status == PostStatus.Approved && p.Room.Status == RoomStatus.Available);
        }

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
            query = query.Where(p => p.DisplayPrice >= q.MinPrice.Value);
        }

        if (q.MaxPrice.HasValue)
        {
            query = query.Where(p => p.DisplayPrice <= q.MaxPrice.Value);
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

        // Sắp xếp
        query = q.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.DisplayPrice),
            "price_desc" => query.OrderByDescending(p => p.DisplayPrice),
            "area_asc" => query.OrderBy(p => p.Room.Area),
            "area_desc" => query.OrderByDescending(p => p.Room.Area),
            "views" => query.OrderByDescending(p => p.ViewCount),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // Phân trang
        var page = q.PageNumber > 0 ? q.PageNumber : 1;
        var size = q.PageSize > 0 ? Math.Min(q.PageSize, 100) : 20;

        var postQuery = query
            .Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.DisplayPrice,
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
                    post.DistanceInKm = Math.Round(CalculateDistance(targetLat, targetLng, (double)post.Latitude.Value, (double)post.Longitude.Value), 2);
                }
            }

            var filtered = allCandidates.Where(p => !p.DistanceInKm.HasValue || p.DistanceInKm.Value <= maxRadius);

            if (q.SortBy?.ToLowerInvariant() == "distance" || string.IsNullOrWhiteSpace(q.SortBy))
            {
                filtered = filtered.OrderBy(p => p.DistanceInKm ?? double.MaxValue);
            }

            posts = filtered
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();
        }
        else
        {
            posts = await postQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        return posts;
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
    public async Task<PostDto> GetPostByIdAsync(int postId, bool incrementView = true)
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

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng");
        }

        if (incrementView)
        {
            post.ViewCount++;
            await _context.SaveChangesAsync();
        }

        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Content,
            Price = post.DisplayPrice,
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

        var title = createDto.GetTitle();
        var description = createDto.GetDescription();
        var price = createDto.GetPrice();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("Tiêu đề không được để trống");
        }

        if (price <= 0)
        {
            throw new Exception("Giá thuê phòng phải lớn hơn 0");
        }

        // Tạo Room tương ứng
        var room = new Room
        {
            LandlordId = landlord.Id,
            CategoryId = createDto.GetCategoryId(),
            RoomName = title,
            Description = description,
            Price = price,
            Area = createDto.GetArea() > 0 ? createDto.GetArea() : 20,
            MaxOccupants = createDto.GetMaxOccupants() > 0 ? createDto.GetMaxOccupants() : 2,
            CurrentOccupants = 0,
            Address = createDto.GetAddress(),
            Ward = createDto.GetWard(),
            District = createDto.GetDistrict(),
            Province = createDto.GetProvince(),
            Latitude = createDto.GetLatitude(),
            Longitude = createDto.GetLongitude(),
            ElectricityPrice = createDto.ElectricityPrice ?? createDto.TienDien,
            WaterPrice = createDto.WaterPrice ?? createDto.TienNuoc,
            ServiceFee = createDto.ServiceFee ?? createDto.PhiDichVu,
            Status = RoomStatus.Available,
            CreatedAt = DateTime.Now
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        // Thêm Amenities
        if (createDto.AmenityIds != null && createDto.AmenityIds.Any())
        {
            var amenities = createDto.AmenityIds.Distinct().Select(aId => new PostAmenity
            {
                RoomId = room.Id,
                AmenityId = aId
            });
            _context.PostAmenities.AddRange(amenities);
        }

        // Thêm Images
        if (createDto.ImageUrls != null && createDto.ImageUrls.Any())
        {
            var images = createDto.ImageUrls.Select((url, index) => new PostImage
            {
                RoomId = room.Id,
                ImageUrl = url,
                IsThumbnail = index == 0,
                DisplayOrder = index,
                CreatedAt = DateTime.Now
            });
            _context.PostImages.AddRange(images);
        }

        // Tạo Post ở trạng thái Pending
        var post = new Post
        {
            RoomId = room.Id,
            LandlordId = landlord.Id,
            Title = title,
            Content = description,
            DisplayPrice = price,
            Status = PostStatus.Pending, // Chờ Admin duyệt
            ViewCount = 0,
            CreatedAt = DateTime.Now
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

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
                Price = p.DisplayPrice,
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

        // Cập nhật Post
        var title = updateDto.GetTitle();
        if (!string.IsNullOrWhiteSpace(title))
        {
            post.Title = title;
            post.Room.RoomName = title;
        }

        var description = updateDto.GetDescription();
        if (description != null)
        {
            post.Content = description;
            post.Room.Description = description;
        }

        var price = updateDto.GetPrice();
        if (price.HasValue && price.Value > 0)
        {
            post.DisplayPrice = price.Value;
            post.Room.Price = price.Value;
        }

        var area = updateDto.GetArea();
        if (area.HasValue && area.Value > 0)
        {
            post.Room.Area = area.Value;
        }

        var maxOccupants = updateDto.GetMaxOccupants();
        if (maxOccupants.HasValue && maxOccupants.Value > 0)
        {
            post.Room.MaxOccupants = maxOccupants.Value;
        }

        var categoryId = updateDto.GetCategoryId();
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            post.Room.CategoryId = categoryId.Value;
        }

        var address = updateDto.GetAddress();
        if (!string.IsNullOrWhiteSpace(address))
        {
            post.Room.Address = address;
        }

        var province = updateDto.GetProvince();
        if (!string.IsNullOrWhiteSpace(province))
        {
            post.Room.Province = province;
        }

        var district = updateDto.GetDistrict();
        if (!string.IsNullOrWhiteSpace(district))
        {
            post.Room.District = district;
        }

        var ward = updateDto.GetWard();
        if (!string.IsNullOrWhiteSpace(ward))
        {
            post.Room.Ward = ward;
        }

        if (updateDto.GetLatitude().HasValue) post.Room.Latitude = updateDto.GetLatitude();
        if (updateDto.GetLongitude().HasValue) post.Room.Longitude = updateDto.GetLongitude();

        if (updateDto.ElectricityPrice.HasValue) post.Room.ElectricityPrice = updateDto.ElectricityPrice;
        if (updateDto.WaterPrice.HasValue) post.Room.WaterPrice = updateDto.WaterPrice;
        if (updateDto.ServiceFee.HasValue) post.Room.ServiceFee = updateDto.ServiceFee;

        // Nếu bài đăng đang Rejected hoặc Approved, cập nhật sẽ đưa về Pending để Admin kiểm duyệt lại
        post.Status = PostStatus.Pending;
        post.RejectionReason = null;
        post.UpdatedAt = DateTime.Now;
        post.Room.UpdatedAt = DateTime.Now;

        // Cập nhật Amenities
        if (updateDto.AmenityIds != null)
        {
            _context.PostAmenities.RemoveRange(post.Room.RoomAmenities);
            var newAmenities = updateDto.AmenityIds.Distinct().Select(aId => new PostAmenity
            {
                RoomId = post.Room.Id,
                AmenityId = aId
            });
            _context.PostAmenities.AddRange(newAmenities);
        }

        // Cập nhật Images
        if (updateDto.ImageUrls != null)
        {
            _context.PostImages.RemoveRange(post.Room.Images);
            var newImages = updateDto.ImageUrls.Select((url, index) => new PostImage
            {
                RoomId = post.Room.Id,
                ImageUrl = url,
                IsThumbnail = index == 0,
                DisplayOrder = index,
                CreatedAt = DateTime.Now
            });
            _context.PostImages.AddRange(newImages);
        }

        await _context.SaveChangesAsync();

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

        await _context.SaveChangesAsync();
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

        post.Status = status;
        post.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

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

        if (user.RoleId != 2 && user.RoleId != 0) // 2: Landlord, 0: Admin
        {
            throw new Exception("Chỉ tài khoản Chủ trọ (Landlord) mới có quyền thực hiện thao tác này");
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
        await _context.SaveChangesAsync();

        return profile;
    }
}
