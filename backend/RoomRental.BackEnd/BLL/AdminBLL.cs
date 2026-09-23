using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Admin;
using RoomRental.BackEnd.DTO.Dashboard;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class AdminBLL : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IPostService _postService;

    public AdminBLL(ApplicationDbContext context, IPostService postService)
    {
        _context = context;
        _postService = postService;
    }

    /// <summary>
    /// Dashboard Quản trị hệ thống (Mục 20)
    /// </summary>
    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalTenants = await _context.Users.CountAsync(u => u.RoleId == 1);
        var totalLandlords = await _context.Users.CountAsync(u => u.RoleId == 2);

        var totalRooms = await _context.Rooms.CountAsync();
        var totalPosts = await _context.Posts.CountAsync();
        var pendingPosts = await _context.Posts.CountAsync(p => p.Status == PostStatus.Pending);
        var approvedPosts = await _context.Posts.CountAsync(p => p.Status == PostStatus.Approved);
        var rejectedPosts = await _context.Posts.CountAsync(p => p.Status == PostStatus.Rejected);
        var hiddenPosts = await _context.Posts.CountAsync(p => p.Status == PostStatus.Hidden);

        var totalAppointments = await _context.ViewingAppointments.CountAsync();
        var totalReports = await _context.Reports.CountAsync();
        var pendingReports = await _context.Reports.CountAsync(r => r.Status == 0);
        var totalBlogs = await _context.BlogPosts.CountAsync();

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalTenants = totalTenants,
            TotalLandlords = totalLandlords,
            TotalRooms = totalRooms,
            TotalPosts = totalPosts,
            PendingPosts = pendingPosts,
            ApprovedPosts = approvedPosts,
            RejectedPosts = rejectedPosts,
            HiddenPosts = hiddenPosts,
            TotalAppointments = totalAppointments,
            TotalReports = totalReports,
            PendingReports = pendingReports,
            TotalBlogs = totalBlogs
        };
    }

    /// <summary>
    /// Dashboard dành cho Chủ trọ (Mục 18)
    /// </summary>
    public async Task<LandlordDashboardDto> GetLandlordDashboardAsync(int landlordAccountId)
    {
        var landlord = await _context.LandlordProfiles.FirstOrDefaultAsync(l => l.AccountId == landlordAccountId);
        if (landlord == null)
        {
            return new LandlordDashboardDto();
        }

        var roomsQuery = _context.Rooms.Where(r => r.LandlordId == landlord.Id);
        var totalRooms = await roomsQuery.CountAsync();
        var availableRooms = await roomsQuery.CountAsync(r => r.Status == RoomStatus.Available);
        var rentedRooms = await roomsQuery.CountAsync(r => r.Status == RoomStatus.Rented);
        var maintenanceRooms = await roomsQuery.CountAsync(r => r.Status == RoomStatus.TemporarilyUnavailable);

        var postsQuery = _context.Posts.Where(p => p.LandlordId == landlord.Id);
        var totalPosts = await postsQuery.CountAsync();
        var pendingPosts = await postsQuery.CountAsync(p => p.Status == PostStatus.Pending);
        var approvedPosts = await postsQuery.CountAsync(p => p.Status == PostStatus.Approved);
        var rejectedPosts = await postsQuery.CountAsync(p => p.Status == PostStatus.Rejected);

        var totalViews = await postsQuery.SumAsync(p => (int?)p.ViewCount) ?? 0;

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var appQuery = _context.ViewingAppointments.Where(a => a.LandlordId == landlord.Id);
        var totalAppointments = await appQuery.CountAsync();
        var pendingAppointments = await appQuery.CountAsync(a => a.Status == AppointmentStatus.Pending);
        var todayAppointments = await appQuery.CountAsync(a => a.ScheduledAt >= today && a.ScheduledAt < tomorrow);

        return new LandlordDashboardDto
        {
            TotalRooms = totalRooms,
            AvailableRooms = availableRooms,
            RentedRooms = rentedRooms,
            MaintenanceRooms = maintenanceRooms,
            TotalPosts = totalPosts,
            PendingPosts = pendingPosts,
            ApprovedPosts = approvedPosts,
            RejectedPosts = rejectedPosts,
            TodayAppointments = todayAppointments,
            PendingAppointments = pendingAppointments,
            TotalAppointments = totalAppointments,
            TotalViewCount = totalViews
        };
    }

    /// <summary>
    /// Quản lý danh sách người dùng (Mục 21)
    /// </summary>
    public async Task<List<AdminUserDto>> GetAllUsersAsync(string? keyword = null, int? roleId = null)
    {
        var query = _context.Users
            .Include(u => u.LandlordProfile)
                .ThenInclude(l => l!.Rooms)
            .Include(u => u.LandlordProfile)
                .ThenInclude(l => l!.Posts)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(k) ||
                (u.Email != null && u.Email.ToLower().Contains(k)) ||
                (u.Phone != null && u.Phone.ToLower().Contains(k)));
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Phone = u.Phone,
                AvatarUrl = u.AvatarUrl,
                RoleId = u.RoleId,
                RoleName = u.RoleName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                PostCount = u.LandlordProfile != null ? u.LandlordProfile.Posts.Count : 0,
                RoomCount = u.LandlordProfile != null ? u.LandlordProfile.Rooms.Count : 0
            })
            .ToListAsync();
    }

    /// <summary>
    /// Quản lý danh sách chủ trọ (Mục 22)
    /// </summary>
    public async Task<List<AdminLandlordDto>> GetAllLandlordsAsync(string? keyword = null)
    {
        var query = _context.LandlordProfiles
            .Include(l => l.Account)
            .Include(l => l.Rooms)
            .Include(l => l.Posts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(l =>
                l.Account.FullName.ToLower().Contains(k) ||
                (l.Account.Email != null && l.Account.Email.ToLower().Contains(k)) ||
                (l.Account.Phone != null && l.Account.Phone.ToLower().Contains(k)));
        }

        return await query
            .OrderByDescending(l => l.Account.CreatedAt)
            .Select(l => new AdminLandlordDto
            {
                LandlordId = l.Id,
                AccountId = l.AccountId,
                FullName = l.Account.FullName,
                Email = l.Account.Email ?? string.Empty,
                Phone = l.Account.Phone,
                AvatarUrl = l.Account.AvatarUrl,
                Address = l.Address,
                CitizenId = l.CitizenId,
                IsVerified = l.IsVerified,
                IsActive = l.Account.IsActive,
                TotalRooms = l.Rooms.Count,
                TotalPosts = l.Posts.Count,
                CreatedAt = l.Account.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Khóa tài khoản
    /// </summary>
    public async Task<AdminUserDto> LockUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new Exception("Không tìm thấy người dùng");
        }

        if (user.RoleId == 0)
        {
            throw new Exception("Không thể khóa tài khoản Admin");
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            RoleId = user.RoleId,
            RoleName = user.RoleName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Mở khóa tài khoản
    /// </summary>
    public async Task<AdminUserDto> UnlockUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new Exception("Không tìm thấy người dùng");
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            RoleId = user.RoleId,
            RoleName = user.RoleName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Quản lý tất cả bài đăng (Mục 23)
    /// </summary>
    public async Task<List<PostListDto>> GetAllPostsAsync(PostStatus? status = null, string? keyword = null)
    {
        var query = _context.Posts
            .Include(p => p.Room)
                .ThenInclude(r => r.Category)
            .Include(p => p.Room)
                .ThenInclude(r => r.Images)
            .Include(p => p.Landlord)
                .ThenInclude(l => l.Account)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(k) ||
                p.Room.Address.ToLower().Contains(k) ||
                p.Landlord.Account.FullName.ToLower().Contains(k));
        }

        return await query
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
    }

    /// <summary>
    /// Duyệt bài đăng (Mục 24)
    /// </summary>
    public async Task<PostDto> ApprovePostAsync(int postId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
        {
            throw new Exception("Không tìm thấy bài đăng");
        }

        post.Status = PostStatus.Approved;
        post.ApprovedAt = DateTime.Now;
        post.PostedAt = DateTime.Now;
        post.RejectionReason = null;
        post.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return await _postService.GetPostByIdAsync(postId, incrementView: false);
    }

    /// <summary>
    /// Từ chối bài đăng kèm lý do (Mục 25)
    /// </summary>
    public async Task<PostDto> RejectPostAsync(int postId, string reason)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
        {
            throw new Exception("Không tìm thấy bài đăng");
        }

        post.Status = PostStatus.Rejected;
        post.RejectionReason = reason;
        post.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return await _postService.GetPostByIdAsync(postId, incrementView: false);
    }

    /// <summary>
    /// Ẩn bài đăng
    /// </summary>
    public async Task<PostDto> HidePostAsync(int postId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
        {
            throw new Exception("Không tìm thấy bài đăng");
        }

        post.Status = PostStatus.Hidden;
        post.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return await _postService.GetPostByIdAsync(postId, incrementView: false);
    }

    /// <summary>
    /// Admin xem toàn bộ phòng trong hệ thống (Mục 26)
    /// </summary>
    public async Task<List<RoomDto>> GetAllRoomsAsync(string? keyword = null, RoomStatus? status = null)
    {
        var query = _context.Rooms
            .Include(r => r.Category)
            .Include(r => r.Images)
            .Include(r => r.RoomAmenities)
            .Include(r => r.Posts)
            .Include(r => r.Landlord)
                .ThenInclude(l => l.Account)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(r =>
                r.RoomName.ToLower().Contains(k) ||
                r.Address.ToLower().Contains(k) ||
                r.Landlord.Account.FullName.ToLower().Contains(k));
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                LandlordId = r.LandlordId,
                LandlordName = r.Landlord.Account.FullName,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                RoomName = r.RoomName,
                Description = r.Description,
                Price = r.Price,
                Area = r.Area,
                MaxOccupants = r.MaxOccupants,
                CurrentOccupants = r.CurrentOccupants,
                Bedrooms = r.Bedrooms,
                Bathrooms = r.Bathrooms,
                Floor = r.Floor,
                Address = r.Address,
                Ward = r.Ward,
                District = r.District,
                Province = r.Province,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                ElectricityPrice = r.ElectricityPrice,
                WaterPrice = r.WaterPrice,
                ServiceFee = r.ServiceFee,
                Status = r.Status,
                ImageUrls = r.Images.OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
                AmenityIds = r.RoomAmenities.Select(ra => ra.AmenityId).ToList(),
                ActivePostId = r.Posts.OrderByDescending(p => p.CreatedAt).Select(p => (int?)p.Id).FirstOrDefault(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }
}
