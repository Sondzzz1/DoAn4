using Microsoft.EntityFrameworkCore;
using RoomRental.Application.DTOs.Category;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Entities;
using RoomRental.Infrastructure.Data;

namespace RoomRental.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync(bool activeOnly = false)
    {
        var query = _context.RoomCategories.Include(c => c.Rooms).AsQueryable();

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                RoomCount = c.Rooms.Count
            })
            .ToListAsync();
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(int id)
    {
        var category = await _context.RoomCategories
            .Include(c => c.Rooms)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            throw new Exception("Không tìm thấy danh mục phòng");
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            RoomCount = category.Rooms.Count
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto)
    {
        if (string.IsNullOrWhiteSpace(createDto.Name))
        {
            throw new Exception("Tên danh mục không được để trống");
        }

        var exists = await _context.RoomCategories.AnyAsync(c => c.Name == createDto.Name.Trim());
        if (exists)
        {
            throw new Exception("Tên danh mục này đã tồn tại");
        }

        var category = new RoomCategory
        {
            Name = createDto.Name.Trim(),
            Description = createDto.Description,
            ImageUrl = createDto.ImageUrl,
            IsActive = createDto.IsActive
        };

        _context.RoomCategories.Add(category);
        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(category.Id);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto)
    {
        var category = await _context.RoomCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            throw new Exception("Không tìm thấy danh mục phòng");
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Name))
        {
            category.Name = updateDto.Name.Trim();
        }

        category.Description = updateDto.Description;
        category.ImageUrl = updateDto.ImageUrl;
        category.IsActive = updateDto.IsActive;

        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(id);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.RoomCategories
            .Include(c => c.Rooms)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            throw new Exception("Không tìm thấy danh mục phòng");
        }

        if (category.Rooms.Any())
        {
            // Nếu có phòng thuộc danh mục này, chuyển sang IsActive = false
            category.IsActive = false;
        }
        else
        {
            _context.RoomCategories.Remove(category);
        }

        await _context.SaveChangesAsync();
    }
}
