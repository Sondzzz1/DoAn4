using RoomRental.Application.DTOs.Post;

namespace RoomRental.Application.DTOs.Favorite;

public class FavoriteDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public PostListDto Post { get; set; } = null!;
}
