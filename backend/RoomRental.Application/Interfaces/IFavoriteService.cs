using RoomRental.Application.DTOs.Favorite;
using RoomRental.Application.DTOs.Post;

namespace RoomRental.Application.Interfaces;

public interface IFavoriteService
{
    Task<bool> AddFavoriteAsync(int accountId, int postId);
    Task<bool> RemoveFavoriteAsync(int accountId, int postId);
    Task<List<PostListDto>> GetMyFavoritesAsync(int accountId);
    Task<bool> IsFavoritedAsync(int accountId, int postId);
}
