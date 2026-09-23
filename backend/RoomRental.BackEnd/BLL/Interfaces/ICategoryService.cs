using RoomRental.BackEnd.DTO.Category;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync(bool activeOnly = false);
    Task<CategoryDto> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto);
    Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto);
    Task DeleteCategoryAsync(int id);
}
