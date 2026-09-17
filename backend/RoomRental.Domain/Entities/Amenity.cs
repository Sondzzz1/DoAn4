namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity TienNghi.
/// </summary>
public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PostAmenity> RoomAmenities { get; set; } = new List<PostAmenity>();
}
