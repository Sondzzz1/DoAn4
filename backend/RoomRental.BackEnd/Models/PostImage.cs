namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity HinhAnhPhong.
/// </summary>
public class PostImage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsThumbnail { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Room Room { get; set; } = null!;
}
