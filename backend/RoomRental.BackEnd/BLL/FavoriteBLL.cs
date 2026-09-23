using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class FavoriteBLL : IFavoriteService
{
    private readonly ApplicationDbContext _context;

    public FavoriteBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AddFavoriteAsync(int accountId, int postId)
    {
        var tenant = await GetOrCreateTenantProfileAsync(accountId);

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
        {
            throw new Exception("Tin đăng không tồn tại");
        }

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.TenantId == tenant.Id && f.PostId == postId);

        if (existing != null)
        {
            return true; // Đã yêu thích rồi
        }

        var favorite = new Favorite
        {
            TenantId = tenant.Id,
            PostId = postId,
            CreatedAt = DateTime.Now
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(int accountId, int postId)
    {
        var tenant = await GetOrCreateTenantProfileAsync(accountId);

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.TenantId == tenant.Id && f.PostId == postId);

        if (existing == null)
        {
            return false;
        }

        _context.Favorites.Remove(existing);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<PostListDto>> GetMyFavoritesAsync(int accountId)
    {
        var tenant = await GetOrCreateTenantProfileAsync(accountId);

        var favorites = await _context.Favorites
            .Include(f => f.Post)
                .ThenInclude(p => p.Room)
                    .ThenInclude(r => r.Category)
            .Include(f => f.Post)
                .ThenInclude(p => p.Room)
                    .ThenInclude(r => r.Images)
            .Include(f => f.Post)
                .ThenInclude(p => p.Landlord)
                    .ThenInclude(l => l.Account)
            .Where(f => f.TenantId == tenant.Id)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new PostListDto
            {
                Id = f.Post.Id,
                Title = f.Post.Title,
                Price = f.Post.DisplayPrice,
                Status = f.Post.Status,
                Area = f.Post.Room.Area,
                MaxOccupants = f.Post.Room.MaxOccupants,
                RoomStatus = f.Post.Room.Status,
                Province = f.Post.Room.Province ?? string.Empty,
                District = f.Post.Room.District ?? string.Empty,
                Ward = f.Post.Room.Ward ?? string.Empty,
                Address = f.Post.Room.Address,
                Latitude = f.Post.Room.Latitude,
                Longitude = f.Post.Room.Longitude,
                CategoryId = f.Post.Room.CategoryId,
                CategoryName = f.Post.Room.Category.Name,
                LandlordId = f.Post.LandlordId,
                LandlordName = f.Post.Landlord.Account.FullName,
                LandlordPhone = f.Post.Landlord.Account.Phone,
                ViewCount = f.Post.ViewCount,
                ThumbnailUrl = f.Post.Room.Images
                    .OrderByDescending(i => i.IsThumbnail)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                PostedAt = f.Post.PostedAt,
                CreatedAt = f.Post.CreatedAt,
                UpdatedAt = f.Post.UpdatedAt
            })
            .ToListAsync();

        return favorites;
    }

    public async Task<bool> IsFavoritedAsync(int accountId, int postId)
    {
        var tenant = await _context.TenantProfiles.FirstOrDefaultAsync(t => t.AccountId == accountId);
        if (tenant == null) return false;

        return await _context.Favorites.AnyAsync(f => f.TenantId == tenant.Id && f.PostId == postId);
    }

    private async Task<TenantProfile> GetOrCreateTenantProfileAsync(int accountId)
    {
        var user = await _context.Users
            .Include(u => u.TenantProfile)
            .FirstOrDefaultAsync(u => u.Id == accountId);

        if (user == null)
        {
            throw new Exception("Người dùng không tồn tại");
        }

        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa");
        }

        if (user.TenantProfile != null)
        {
            return user.TenantProfile;
        }

        var profile = new TenantProfile
        {
            AccountId = user.Id
        };

        _context.TenantProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile;
    }
}
