namespace RoomRental.Application.DTOs.Admin;

public class AdminUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsBlocked => !IsActive;
    public DateTime CreatedAt { get; set; }
    public int PostCount { get; set; }
    public int RoomCount { get; set; }
}

public class AdminLandlordDto
{
    public int LandlordId { get; set; }
    public int AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? CitizenId { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public int TotalRooms { get; set; }
    public int TotalPosts { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RejectPostDto
{
    public string? Reason { get; set; }
    public string? LyDo { get; set; }

    public string GetReason() => !string.IsNullOrWhiteSpace(LyDo) ? LyDo : (!string.IsNullOrWhiteSpace(Reason) ? Reason : "Thông tin bài đăng không hợp lệ.");
}
