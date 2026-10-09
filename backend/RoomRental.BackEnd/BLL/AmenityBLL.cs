using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Amenity;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class AmenityBLL : IAmenityService
{
    private readonly ApplicationDbContext _context;

    public AmenityBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AmenityDto>> GetAllAmenitiesAsync(bool activeOnly = false)
    {
        var query = _context.Amenities.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(a => a.IsActive);
        }

        return await query
            .OrderBy(a => a.Id)
            .Select(a => new AmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                Icon = a.Icon,
                Description = a.Description,
                IsActive = a.IsActive
            })
            .ToListAsync();
    }

    public async Task<AmenityDto> GetAmenityByIdAsync(int id)
    {
        var amenity = await _context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
        if (amenity == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy tiện ích");
        }

        return new AmenityDto
        {
            Id = amenity.Id,
            Name = amenity.Name,
            Icon = amenity.Icon,
            Description = amenity.Description,
            IsActive = amenity.IsActive
        };
    }

    public async Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto createDto)
    {
        var name = createDto.GetName();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("Tên tiện ích không được để trống");
        }

        var exists = await _context.Amenities.AnyAsync(a => a.Name == name);
        if (exists)
        {
            throw BusinessRuleException.Conflict("Tiện ích này đã tồn tại");
        }

        var amenity = new Amenity
        {
            Name = name,
            Icon = createDto.GetIcon(),
            Description = createDto.GetDescription(),
            IsActive = createDto.GetIsActive()
        };

        _context.Amenities.Add(amenity);
        await _context.SaveChangesAsync();

        return await GetAmenityByIdAsync(amenity.Id);
    }

    public async Task<AmenityDto> UpdateAmenityAsync(int id, UpdateAmenityDto updateDto)
    {
        var amenity = await _context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
        if (amenity == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy tiện ích");
        }

        var name = updateDto.GetName();
        if (!string.IsNullOrWhiteSpace(name))
        {
            amenity.Name = name;
        }

        if (updateDto.Icon != null || updateDto.BieuTuong != null)
        {
            amenity.Icon = updateDto.GetIcon();
        }

        if (updateDto.Description != null || updateDto.MoTa != null)
        {
            amenity.Description = updateDto.GetDescription();
        }

        if (updateDto.IsActive.HasValue || updateDto.DangHoatDong.HasValue)
        {
            amenity.IsActive = updateDto.GetIsActive();
        }

        await _context.SaveChangesAsync();

        return await GetAmenityByIdAsync(id);
    }

    public async Task DeleteAmenityAsync(int id)
    {
        var amenity = await _context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
        if (amenity == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy tiện ích");
        }

        amenity.IsActive = false;
        await _context.SaveChangesAsync();
    }
}
