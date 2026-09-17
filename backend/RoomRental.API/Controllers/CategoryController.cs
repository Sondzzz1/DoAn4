using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Category;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.Interfaces;

namespace RoomRental.API.Controllers;

/// <summary>
/// Controller xử lý Danh mục phòng trọ (Mục 28)
/// </summary>
[Route("api/danh-muc")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách danh mục phòng (Mục 28: GET /api/categories)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync(activeOnly);
            return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories, "Lấy danh mục thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh mục phòng");
            return BadRequest(ApiResponse<List<CategoryDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy chi tiết 1 danh mục (GET /api/categories/{id})
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category, "Lấy chi tiết danh mục thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết danh mục ID: {Id}", id);
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin tạo danh mục mới (POST /api/categories)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto createDto)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(createDto);
            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category, "Tạo danh mục mới thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo danh mục mới");
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin cập nhật danh mục (PUT /api/categories/{id})
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto updateDto)
    {
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, updateDto);
            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category, "Cập nhật danh mục thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật danh mục ID: {Id}", id);
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Admin xóa danh mục (DELETE /api/categories/{id})
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id);
            return Ok(ApiResponse<object?>.SuccessResponse(null, "Xóa danh mục thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa danh mục ID: {Id}", id);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}
