namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity PhongTro_TienNghi - tiện nghi gắn với phòng.
/// </summary>
public class PostAmenity
{
    public int RoomId { get; set; }
    public int AmenityId { get; set; }

    public virtual Room Room { get; set; } = null!;
    public virtual Amenity Amenity { get; set; } = null!;
}
