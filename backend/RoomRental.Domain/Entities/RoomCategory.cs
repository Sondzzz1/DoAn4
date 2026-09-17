namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity DanhMucPhong.
/// </summary>
public class RoomCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
