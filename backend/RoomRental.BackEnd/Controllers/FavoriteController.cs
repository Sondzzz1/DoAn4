using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Favorite;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.BLL.Interfaces;
using System.Security.Claims;

namespace RoomRental.BackEnd.Controllers;

/// <summary>
/// Controller xử lý Yêu thích phòng trọ (Mục 6)
/// </summary>
[Route("api/yeu-thich")]
[ApiController]
[Authorize(Roles = "Tenant")]
public class FavoriteController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;
    private readonly ILogger<FavoriteController> _logger;

    public FavoriteController(IFavoriteService favoriteService, ILogger<FavoriteController> logger)
    {
        _favoriteService = favoriteService;
        _logger = logger;
    }

    /// <summary>
    /// Thêm phòng vào danh sách yêu thích (Mục 6: POST /api/favorites)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> AddFavorite([FromBody] CreateFavoriteDto favoriteDto)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var result = await _favoriteService.AddFavoriteAsync(accountId, favoriteDto.PostId);

            return Ok(ApiResponse<bool>.SuccessResponse(result, "Đã thêm vào danh sách yêu thích"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm bài đăng yêu thích");
            return this.BusinessError<bool>(ex);
        }
    }

    /// <summary>
    /// Xóa phòng khỏi danh sách yêu thích (Mục 6: DELETE /api/favorites/{postId})
    /// </summary>
    [HttpDelete("{postId:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> RemoveFavorite(int postId)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var result = await _favoriteService.RemoveFavoriteAsync(accountId, postId);

            return Ok(ApiResponse<bool>.SuccessResponse(result, "Đã bỏ yêu thích"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa bài đăng yêu thích");
            return this.BusinessError<bool>(ex);
        }
    }

    /// <summary>
    /// Xem danh sách phòng đã yêu thích của Tenant (Mục 6: GET /api/favorites/my)
    /// </summary>
    [HttpGet("cua-toi")]
    [ProducesResponseType(typeof(ApiResponse<List<PostListDto>>), 200)]
    public async Task<IActionResult> GetMyFavorites()
    {
        try
        {
            var accountId = GetCurrentUserId();
            var favorites = await _favoriteService.GetMyFavoritesAsync(accountId);

            return Ok(ApiResponse<List<PostListDto>>.SuccessResponse(favorites, "Lấy danh sách yêu thích thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách yêu thích");
            return this.BusinessError<List<PostListDto>>(ex);
        }
    }

    /// <summary>
    /// Kiểm tra bài đăng đã được yêu thích chưa (GET /api/favorites/check/{postId})
    /// </summary>
    [HttpGet("kiem-tra/{postId:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> CheckFavorite(int postId)
    {
        try
        {
            var accountId = GetCurrentUserId();
            var isFavorited = await _favoriteService.IsFavoritedAsync(accountId, postId);

            return Ok(ApiResponse<bool>.SuccessResponse(isFavorited, "Kiểm tra thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái yêu thích");
            return this.BusinessError<bool>(ex);
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new Exception("Không thể xác thực người dùng");
        }

        return userId;
    }
}
