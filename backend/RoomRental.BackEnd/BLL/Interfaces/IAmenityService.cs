using RoomRental.BackEnd.DTO.Amenity;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IAmenityService
{
    Task<List<AmenityDto>> GetAllAmenitiesAsync(bool activeOnly = false);
    Task<AmenityDto> GetAmenityByIdAsync(int id);
    Task<AmenityDto> CreateAmenityAsync(CreateAmenityDto createDto);
    Task<AmenityDto> UpdateAmenityAsync(int id, UpdateAmenityDto updateDto);
    Task DeleteAmenityAsync(int id);
}
