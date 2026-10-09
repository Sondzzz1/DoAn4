using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Category;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class CategoryBLL : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryBLL(ApplicationDbContext context)
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
            throw BusinessRuleException.NotFound("Không tìm thấy danh mục phòng");
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
        var name = createDto.GetName();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("Tên danh mục không được để trống");
        }

        var exists = await _context.RoomCategories.AnyAsync(c => c.Name == name);
        if (exists)
        {
            throw BusinessRuleException.Conflict("Tên danh mục này đã tồn tại");
        }

        var category = new RoomCategory
        {
            Name = name,
            Description = createDto.GetDescription(),
            ImageUrl = createDto.GetImageUrl(),
            IsActive = createDto.GetIsActive()
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
            throw BusinessRuleException.NotFound("Không tìm thấy danh mục phòng");
        }

        var name = updateDto.GetName();
        if (!string.IsNullOrWhiteSpace(name))
        {
            category.Name = name;
        }

        if (updateDto.Description != null || updateDto.MoTa != null)
        {
            category.Description = updateDto.GetDescription();
        }

        if (updateDto.ImageUrl != null || updateDto.DuongDanAnh != null)
        {
            category.ImageUrl = updateDto.GetImageUrl();
        }

        if (updateDto.IsActive.HasValue || updateDto.DangHoatDong.HasValue)
        {
            category.IsActive = updateDto.GetIsActive();
        }

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
            throw BusinessRuleException.NotFound("Không tìm thấy danh mục phòng");
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
