namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity YeuThich.
/// </summary>
public class Favorite
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PostId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual TenantProfile Tenant { get; set; } = null!;
    public virtual Post Post { get; set; } = null!;
}
