namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity NguoiDung - hồ sơ người thuê.
/// </summary>
public class TenantProfile
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Occupation { get; set; }
    public string? CurrentAddress { get; set; }
    public string? Introduction { get; set; }

    public virtual User Account { get; set; } = null!;
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<ViewingAppointment> Appointments { get; set; } = new List<ViewingAppointment>();
}
