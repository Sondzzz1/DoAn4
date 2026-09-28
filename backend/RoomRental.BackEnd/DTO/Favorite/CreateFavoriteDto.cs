namespace RoomRental.BackEnd.DTO.Favorite;

public class CreateFavoriteDto
{
    public int PostId { get; set; }
    public int BaiDangId { get => PostId; set => PostId = value; }

    public int GetPostId() => PostId > 0 ? PostId : BaiDangId;
}
