using RoomRental.Application.DTOs.Amenity;

namespace RoomRental.Application.Interfaces;

public interface IAmenityService
{
    Task<List<AmenityDto>> GetAllAmenitiesAsync(bool activeOnly = false);
    Task<AmenityDto> GetAmenityByIdAsync(int id);
    Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto createDto);
    Task<AmenityDto> UpdateAmenityAsync(int id, UpdateAmenityDto updateDto);
    Task DeleteAmenityAsync(int id);
}
