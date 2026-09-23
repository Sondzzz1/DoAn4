using RoomRental.BackEnd.DTO.Post;

namespace RoomRental.BackEnd.DTO.Favorite;

public class FavoriteDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public PostListDto Post { get; set; } = null!;
}
