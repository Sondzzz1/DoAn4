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

    public async Task<RoomDto> GetAccessibleRoomAsync(int accountId, int roomId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == accountId)
            ?? throw BusinessRuleException.Forbidden("Tài khoản không khả dụng.");
        if (user.IsBlocked) throw BusinessRuleException.Forbidden("Tài khoản đã bị khóa.");
        if (user.RoleId != 0 && (user.RoleId != 2 ||
            !await _context.Rooms.AnyAsync(r => r.Id == roomId && r.Landlord.AccountId == accountId)))
            throw BusinessRuleException.Forbidden("Bạn không có quyền đọc phòng này.");
        return await GetRoomByIdAsync(roomId);
    }

    /// <summary>
    /// Tạo phòng trọ mới
    /// </summary>
    public async Task<RoomDto> CreateRoomAsync(int landlordAccountId, CreateRoomDto createDto)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var roomName = createDto.GetRoomName();
        if (string.IsNullOrWhiteSpace(roomName))
        {
            throw new Exception("Tên phòng không được để trống");
        }

        var room = new Room
        {
            LandlordId = landlord.Id,
            CategoryId = createDto.GetCategoryId(),
            RoomName = roomName,
            Description = createDto.GetDescription(),
            Price = createDto.GetPrice(),
            Area = createDto.Area.HasValue || createDto.DienTich.HasValue ? createDto.GetArea() : 20,
            MaxOccupants = createDto.GetMaxOccupants(),
            CurrentOccupants = 0,
            Bedrooms = createDto.GetBedrooms(),
            Bathrooms = createDto.GetBathrooms(),
            Floor = createDto.GetFloor(),
            Address = createDto.GetAddress(),
            Ward = createDto.GetWard(),
            District = createDto.GetDistrict(),
            Province = createDto.GetProvince(),
            Latitude = createDto.GetLatitude(),
            Longitude = createDto.GetLongitude(),
            ElectricityPrice = createDto.GetElectricityPrice(),
            WaterPrice = createDto.GetWaterPrice(),
            ServiceFee = createDto.GetServiceFee(),
            Status = RoomStatus.Available,
            CreatedAt = DateTime.Now
        };

        if (createDto.AmenityIds != null && createDto.AmenityIds.Any())
        {
            var amenities = createDto.AmenityIds.Distinct().Select(aId => new PostAmenity
            {
                Room = room,
                AmenityId = aId
            });
            room.RoomAmenities = amenities.ToList();
        }

        if (createDto.ImageUrls != null && createDto.ImageUrls.Any())
        {
            var images = createDto.ImageUrls.Select((url, index) => new PostImage
            {
                Room = room,
                ImageUrl = url,
                IsThumbnail = index == 0,
                DisplayOrder = index,
                CreatedAt = DateTime.Now
            });
            room.Images = images.ToList();
        }

        await ValidateRoomAsync(room);
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return await GetRoomByIdAsync(room.Id);
    }

    /// <summary>
    /// Cập nhật thông tin phòng
    /// </summary>
    public async Task<RoomDto> UpdateRoomAsync(int landlordAccountId, int roomId, UpdateRoomDto updateDto)
    {
        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        if (transaction != null) await WorkflowLock.AcquireAsync(_context);
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
            throw BusinessRuleException.Forbidden("Bạn không có quyền chỉnh sửa phòng này");
        }

        var publicSnapshot = RoomPublicationPolicy.Capture(room);
        var updateName = updateDto.GetRoomName();
        if (!string.IsNullOrWhiteSpace(updateName))
        {
            room.RoomName = updateName;
        }

        if (updateDto.Description != null || updateDto.MoTa != null)
        {
            room.Description = updateDto.GetDescription();
        }

        if (updateDto.Price.HasValue || updateDto.Gia.HasValue)
        {
            room.Price = updateDto.GetPrice();
        }

        if (updateDto.Area.HasValue || updateDto.DienTich.HasValue)
        {
            room.Area = updateDto.GetArea();
        }

        if (updateDto.MaxOccupants.HasValue || updateDto.SoNguoiToiDa.HasValue)
        {
            room.MaxOccupants = updateDto.GetMaxOccupants();
        }

        if (updateDto.CategoryId.HasValue || updateDto.DanhMucId.HasValue)
        {
            room.CategoryId = updateDto.GetCategoryId();
        }

        room.Bedrooms = updateDto.GetBedrooms() ?? room.Bedrooms;
        room.Bathrooms = updateDto.GetBathrooms() ?? room.Bathrooms;
        room.Floor = updateDto.GetFloor() ?? room.Floor;
        var oldLocation = (room.Address, room.Province, room.District, room.Ward);
        if (!string.IsNullOrWhiteSpace(updateDto.GetAddress())) room.Address = updateDto.GetAddress();
        if (updateDto.Ward != null || updateDto.Phuong != null) room.Ward = updateDto.Ward ?? updateDto.Phuong;
        if (updateDto.District != null || updateDto.Quan != null) room.District = updateDto.District ?? updateDto.Quan;
        if (updateDto.Province != null || updateDto.ThanhPho != null) room.Province = updateDto.Province ?? updateDto.ThanhPho;
        var locationChanged = oldLocation != (room.Address, room.Province, room.District, room.Ward);
        room.Latitude = updateDto.GetLatitude() ?? (locationChanged ? null : room.Latitude);
        room.Longitude = updateDto.GetLongitude() ?? (locationChanged ? null : room.Longitude);
        room.ElectricityPrice = updateDto.GetElectricityPrice() ?? room.ElectricityPrice;
        room.WaterPrice = updateDto.GetWaterPrice() ?? room.WaterPrice;
        room.ServiceFee = updateDto.GetServiceFee() ?? room.ServiceFee;
        room.UpdatedAt = DateTime.Now;

        if (updateDto.AmenityIds != null)
        {
            var selected = updateDto.AmenityIds.Distinct().ToHashSet();
            var removed = room.RoomAmenities.Where(a => !selected.Contains(a.AmenityId)).ToList();
            _context.PostAmenities.RemoveRange(removed);
            foreach (var item in removed) room.RoomAmenities.Remove(item);
            var added = selected.Where(id => !room.RoomAmenities.Any(a => a.AmenityId == id)).Select(aId => new PostAmenity
            {
                RoomId = room.Id,
                AmenityId = aId
            }).ToList();
            _context.PostAmenities.AddRange(added);
            foreach (var item in added) room.RoomAmenities.Add(item);
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
            }).ToList();
            _context.PostImages.AddRange(images);
            room.Images = images.ToList();
        }

        await ValidateRoomAsync(room);
        if (!publicSnapshot.SequenceEqual(RoomPublicationPolicy.Capture(room)))
            await RoomPublicationPolicy.ReapproveAsync(_context, room);
        await RoomPublicationPolicy.SaveAsync(_context);
        if (transaction != null) await transaction.CommitAsync();

        return await GetRoomByIdAsync(roomId);
    }

    /// <summary>
    /// Cập nhật trạng thái phòng (Mục 16: Còn trống ↔ Đã thuê / Tạm ngưng)
    /// </summary>
    public async Task<RoomDto> UpdateRoomStatusAsync(int landlordAccountId, int roomId, RoomStatus status)
    {
        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        if (transaction != null) await WorkflowLock.AcquireAsync(_context);
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null)
        {
            throw new Exception("Không tìm thấy phòng");
        }

        if (room.LandlordId != landlord.Id)
        {
            throw BusinessRuleException.Forbidden("Bạn không có quyền thay đổi trạng thái phòng này");
        }

        var hasActiveContract = await _context.RentalContracts
            .Include(c => c.Post)
            .AnyAsync(c => c.Post!.RoomId == roomId && RentalContractStatus.EffectiveStatuses.Contains(c.Status));

        if (hasActiveContract)
            throw BusinessRuleException.Conflict("Phòng đang có hợp đồng hiệu lực nên không thể đổi trạng thái.");

        if (room.Status == RoomStatus.Reserved)
            throw BusinessRuleException.Conflict("Phòng đang được hệ thống giữ chỗ cho yêu cầu thuê đã duyệt.");

        if (status == RoomStatus.Rented || status == RoomStatus.Reserved)
            throw new BusinessRuleException("Trạng thái đã thuê được hệ thống cập nhật từ hợp đồng, không thể đặt thủ công.");

        if (status != RoomStatus.Available && status != RoomStatus.TemporarilyUnavailable)
            throw new BusinessRuleException("Trạng thái phòng không hợp lệ.");

        room.Status = status;
        room.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();

        return await GetRoomByIdAsync(roomId);
    }

    /// <summary>
    /// Xóa phòng
    /// </summary>
    public async Task DeleteRoomAsync(int landlordAccountId, int roomId)
    {
        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        if (transaction != null) await WorkflowLock.AcquireAsync(_context);
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
            throw BusinessRuleException.Forbidden("Bạn không có quyền xóa phòng này");
        }

        var hasActiveContract = await _context.RentalContracts
            .Include(c => c.Post)
            .AnyAsync(c => c.Post!.RoomId == roomId && RentalContractStatus.EffectiveStatuses.Contains(c.Status));
        if (hasActiveContract)
            throw BusinessRuleException.Conflict("Phòng đang có hợp đồng hiệu lực nên không thể tạm ngưng.");

        if (room.Status == RoomStatus.Reserved)
            throw BusinessRuleException.Conflict("Phòng đang được hệ thống giữ chỗ cho yêu cầu thuê đã duyệt.");

        // Đánh dấu tạm ngưng / ẩn
        room.Status = RoomStatus.TemporarilyUnavailable;
        room.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
    }

    private async Task ValidateRoomAsync(Room room)
    {
        if (room.Price <= 0 || room.Area <= 0 || room.MaxOccupants < 1 || string.IsNullOrWhiteSpace(room.Address))
            throw new BusinessRuleException("Giá, diện tích, số người và địa chỉ phòng phải hợp lệ.");
        if (room.Latitude.HasValue != room.Longitude.HasValue || room.Latitude is < -90 or > 90 || room.Longitude is < -180 or > 180)
            throw new BusinessRuleException("Tọa độ phòng không hợp lệ.");
        if (room.ElectricityPrice is < 0 || room.WaterPrice is < 0 || room.ServiceFee is < 0 || room.Bedrooms is < 0 || room.Bathrooms is < 0)
            throw new BusinessRuleException("Đơn giá và số phòng không được âm.");
        if (!await _context.RoomCategories.AnyAsync(c => c.Id == room.CategoryId))
            throw new BusinessRuleException("Loại phòng không tồn tại.");
        var ids = room.RoomAmenities.Select(a => a.AmenityId).Distinct().ToArray();
        if (await _context.Amenities.CountAsync(a => ids.Contains(a.Id)) != ids.Length)
            throw new BusinessRuleException("Tiện ích phòng không tồn tại.");
        if (room.Images.Any(i => string.IsNullOrWhiteSpace(i.ImageUrl)))
            throw new BusinessRuleException("Đường dẫn ảnh không được để trống.");
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
            ActivePostId = r.Posts?.Where(p => p.Status == PostStatus.Pending || p.Status == PostStatus.Approved).OrderByDescending(p => p.CreatedAt).Select(p => (int?)p.Id).FirstOrDefault(),
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

        if (user.RoleId != 2)
        {
            throw BusinessRuleException.Forbidden("Chỉ chủ trọ mới có quyền truy cập.");
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
