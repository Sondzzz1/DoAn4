using RoomRental.Domain.Enums;

namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity PhongTro - thông tin phòng trọ thực tế.
/// </summary>
public class Room
{
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public int CategoryId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public int CurrentOccupants { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? ElectricityPrice { get; set; }
    public decimal? WaterPrice { get; set; }
    public decimal? ServiceFee { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public virtual LandlordProfile Landlord { get; set; } = null!;
    public virtual RoomCategory Category { get; set; } = null!;
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    public virtual ICollection<PostAmenity> RoomAmenities { get; set; } = new List<PostAmenity>();
}
