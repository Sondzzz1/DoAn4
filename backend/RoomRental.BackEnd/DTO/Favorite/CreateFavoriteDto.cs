namespace RoomRental.BackEnd.DTO.Favorite;

using System.Text.Json.Serialization;

public class CreateFavoriteDto
{
    [JsonIgnore] public int PostId { get; set; }
    public int BaiDangId { get => PostId; set => PostId = value; }
}
