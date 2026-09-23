using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class RoomBLL : IRoomService
{
    private readonly ApplicationDbContext _context;

    public RoomBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Chủ trọ xem danh sách phòng thuộc sở hữu (Mục 15)
    /// </summary>
    public async Task<List<RoomDto>> GetLandlordRoomsAsync(int landlordAccountId, RoomStatus? status = null)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var query = _context.Rooms
            .Include(r => r.Category)
            .Include(r => r.Images)
            .Include(r => r.RoomAmenities)
            .Include(r => r.Posts)
            .Include(r => r.Landlord)
                .ThenInclude(l => l.Account)
            .Where(r => r.LandlordId == landlord.Id);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    /// <summary>
    /// Lấy chi tiết phòng
    /// </summary>
    public async Task<RoomDto> GetRoomByIdAsync(int roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.Category)
            .Include(r => r.Images)
            .Include(r => r.RoomAmenities)
            .Include(r => r.Posts)
            .Include(r => r.Landlord)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            throw new Exception("Không tìm thấy thông tin phòng trọ");
        }

        return MapToDto(room);
    }

    /// <summary>
    /// Tạo phòng trọ mới
    /// </summary>
    public async Task<RoomDto> CreateRoomAsync(int landlordAccountId, CreateRoomDto createDto)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        if (string.IsNullOrWhiteSpace(createDto.RoomName))
        {
            throw new Exception("Tên phòng không được để trống");
        }

        var room = new Room
        {
            LandlordId = landlord.Id,
            CategoryId = createDto.CategoryId > 0 ? createDto.CategoryId : 1,
            RoomName = createDto.RoomName,
            Description = createDto.Description,
            Price = createDto.Price,
            Area = createDto.Area > 0 ? createDto.Area : 20,
            MaxOccupants = createDto.MaxOccupants > 0 ? createDto.MaxOccupants : 2,
            CurrentOccupants = 0,
            Bedrooms = createDto.Bedrooms,
            Bathrooms = createDto.Bathrooms,
            Floor = createDto.Floor,
            Address = createDto.Address,
            Ward = createDto.Ward,
            District = createDto.District,
            Province = createDto.Province,
            Latitude = createDto.Latitude,
            Longitude = createDto.Longitude,
            ElectricityPrice = createDto.ElectricityPrice,
            WaterPrice = createDto.WaterPrice,
            ServiceFee = createDto.ServiceFee,
            Status = RoomStatus.Available,
            CreatedAt = DateTime.Now
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        if (createDto.AmenityIds != null && createDto.AmenityIds.Any())
        {
            var amenities = createDto.AmenityIds.Distinct().Select(aId => new PostAmenity
            {
                RoomId = room.Id,
                AmenityId = aId
            });
            _context.PostAmenities.AddRange(amenities);
        }

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

        await _context.SaveChangesAsync();

        return await GetRoomByIdAsync(room.Id);
    }

    /// <summary>
    /// Cập nhật thông tin phòng
    /// </summary>
    public async Task<RoomDto> UpdateRoomAsync(int landlordAccountId, int roomId, UpdateRoomDto updateDto)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var room = await _context.Rooms
            .Include(r => r.Images)
            .Include(r => r.RoomAmenities)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            throw new Exception("Không tìm thấy phòng");
        }

        if (room.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền chỉnh sửa phòng này");
        }

        room.RoomName = updateDto.RoomName;
        room.Description = updateDto.Description;
        room.Price = updateDto.Price;
        room.Area = updateDto.Area;
        room.MaxOccupants = updateDto.MaxOccupants;
        room.CategoryId = updateDto.CategoryId;
        room.Bedrooms = updateDto.Bedrooms;
        room.Bathrooms = updateDto.Bathrooms;
        room.Floor = updateDto.Floor;
        room.Address = updateDto.Address;
        room.Ward = updateDto.Ward;
        room.District = updateDto.District;
        room.Province = updateDto.Province;
        room.Latitude = updateDto.Latitude;
        room.Longitude = updateDto.Longitude;
        room.ElectricityPrice = updateDto.ElectricityPrice;
        room.WaterPrice = updateDto.WaterPrice;
        room.ServiceFee = updateDto.ServiceFee;
        room.UpdatedAt = DateTime.Now;

        if (updateDto.AmenityIds != null)
        {
            _context.PostAmenities.RemoveRange(room.RoomAmenities);
            var amenities = updateDto.AmenityIds.Distinct().Select(aId => new PostAmenity
            {
                RoomId = room.Id,
                AmenityId = aId
            });
            _context.PostAmenities.AddRange(amenities);
        }

        if (updateDto.ImageUrls != null)
        {
            _context.PostImages.RemoveRange(room.Images);
            var images = updateDto.ImageUrls.Select((url, index) => new PostImage
            {
                RoomId = room.Id,
                ImageUrl = url,
                IsThumbnail = index == 0,
                DisplayOrder = index,
                CreatedAt = DateTime.Now
            });
            _context.PostImages.AddRange(images);
        }

        await _context.SaveChangesAsync();

        return await GetRoomByIdAsync(roomId);
    }

    /// <summary>
    /// Cập nhật trạng thái phòng (Mục 16: Còn trống ↔ Đã thuê / Tạm ngưng)
    /// </summary>
    public async Task<RoomDto> UpdateRoomStatusAsync(int landlordAccountId, int roomId, RoomStatus status)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null)
        {
            throw new Exception("Không tìm thấy phòng");
        }

        if (room.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền thay đổi trạng thái phòng này");
        }

        room.Status = status;
        room.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return await GetRoomByIdAsync(roomId);
    }

    /// <summary>
    /// Xóa phòng
    /// </summary>
    public async Task DeleteRoomAsync(int landlordAccountId, int roomId)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var room = await _context.Rooms
            .Include(r => r.Posts)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            throw new Exception("Không tìm thấy phòng");
        }

        if (room.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền xóa phòng này");
        }

        // Đánh dấu tạm ngưng / ẩn
        room.Status = RoomStatus.TemporarilyUnavailable;
        room.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    private static RoomDto MapToDto(Room r)
    {
        return new RoomDto
        {
            Id = r.Id,
            LandlordId = r.LandlordId,
            LandlordName = r.Landlord?.Account?.FullName ?? string.Empty,
            CategoryId = r.CategoryId,
            CategoryName = r.Category?.Name ?? string.Empty,
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
            ImageUrls = r.Images?.OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList() ?? new(),
            AmenityIds = r.RoomAmenities?.Select(ra => ra.AmenityId).ToList() ?? new(),
            ActivePostId = r.Posts?.OrderByDescending(p => p.CreatedAt).Select(p => (int?)p.Id).FirstOrDefault(),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
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

        if (user.RoleId != 2 && user.RoleId != 0)
        {
            throw new Exception("Chỉ chủ trọ mới có quyền truy cập");
        }

        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa");
        }

        if (user.LandlordProfile != null)
        {
            return user.LandlordProfile;
        }

        var profile = new LandlordProfile { AccountId = user.Id };
        _context.LandlordProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile;
    }
}
