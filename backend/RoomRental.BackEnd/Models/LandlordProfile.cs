namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity ChuTro - hồ sơ chủ trọ.
/// </summary>
public class LandlordProfile
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string? Introduction { get; set; }
    public string? Address { get; set; }
    public string? CitizenId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public virtual User Account { get; set; } = null!;
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<ViewingAppointment> Appointments { get; set; } = new List<ViewingAppointment>();
}
