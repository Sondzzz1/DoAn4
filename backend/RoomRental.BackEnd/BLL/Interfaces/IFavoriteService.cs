using RoomRental.BackEnd.DTO.Favorite;
using RoomRental.BackEnd.DTO.Post;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IFavoriteService
{
    Task<bool> AddFavoriteAsync(int accountId, int postId);
    Task<bool> RemoveFavoriteAsync(int accountId, int postId);
    Task<List<PostListDto>> GetMyFavoritesAsync(int accountId);
    Task<bool> IsFavoritedAsync(int accountId, int postId);
}
