namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity User - Người dùng hệ thống
/// Bao gồm: Tenant (Người tìm trọ), Landlord (Chủ trọ), Admin (Quản trị viên)
/// </summary>
public class User
{
    /// <summary>
    /// ID của User - Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Họ và tên đầy đủ
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email - Dùng để đăng nhập (Unique)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu đã được hash bằng BCrypt
    /// KHÔNG lưu password dạng plaintext
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// URL ảnh đại diện
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Foreign Key - ID của Role
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Trạng thái tài khoản có bị khóa không
    /// true = Bị khóa, false = Hoạt động bình thường
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>
    /// Ngày tạo tài khoản
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cập nhật thông tin lần cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// Role của User này
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Danh sách tin đăng của Landlord (chỉ áp dụng khi RoleId = Landlord)
    /// </summary>
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    /// <summary>
    /// Danh sách phòng yêu thích của Tenant (chỉ áp dụng khi RoleId = Tenant)
    /// </summary>
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    /// <summary>
    /// Danh sách lịch hẹn xem phòng của Tenant (chỉ áp dụng khi RoleId = Tenant)
    /// </summary>
    public virtual ICollection<ViewingAppointment> Appointments { get; set; } = new List<ViewingAppointment>();
}
