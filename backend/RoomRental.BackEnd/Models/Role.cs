namespace RoomRental.BackEnd.Models;

/// <summary>
/// Role helper giữ lại để không phá vỡ code cũ. Database mới lưu role trực tiếp ở TaiKhoan.VaiTro.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
