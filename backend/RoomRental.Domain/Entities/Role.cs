namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity Role - Vai trò người dùng
/// Chỉ có 3 role: Tenant (Người tìm trọ), Landlord (Chủ trọ), Admin (Quản trị viên)
/// </summary>
public class Role
{
    /// <summary>
    /// ID của Role - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tên Role: Tenant, Landlord, Admin
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả về Role
    /// </summary>
    public string? Description { get; set; }

    // Navigation Properties
    /// <summary>
    /// Danh sách User thuộc Role này
    /// </summary>
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
