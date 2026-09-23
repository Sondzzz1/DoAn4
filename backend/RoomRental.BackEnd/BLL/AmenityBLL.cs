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
            throw new Exception("Không tìm thấy tiện ích");
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
        if (string.IsNullOrWhiteSpace(createDto.Name))
        {
            throw new Exception("Tên tiện ích không được để trống");
        }

        var exists = await _context.Amenities.AnyAsync(a => a.Name == createDto.Name.Trim());
        if (exists)
        {
            throw new Exception("Tiện ích này đã tồn tại");
        }

        var amenity = new Amenity
        {
            Name = createDto.Name.Trim(),
            Icon = createDto.Icon,
            Description = createDto.Description,
            IsActive = createDto.IsActive
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
            throw new Exception("Không tìm thấy tiện ích");
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Name))
        {
            amenity.Name = updateDto.Name.Trim();
        }

        amenity.Icon = updateDto.Icon;
        amenity.Description = updateDto.Description;
        amenity.IsActive = updateDto.IsActive;

        await _context.SaveChangesAsync();

        return await GetAmenityByIdAsync(id);
    }

    public async Task DeleteAmenityAsync(int id)
    {
        var amenity = await _context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
        if (amenity == null)
        {
            throw new Exception("Không tìm thấy tiện ích");
        }

        amenity.IsActive = false;
        await _context.SaveChangesAsync();
    }
}
