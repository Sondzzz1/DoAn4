using System.ComponentModel.DataAnnotations;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO để tạo Post mới (Landlord đăng tin)
/// </summary>
public class CreatePostDto
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả không được để trống")]
    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá không được để trống")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    // Room Information
    [Required(ErrorMessage = "Diện tích không được để trống")]
    [Range(1, 1000, ErrorMessage = "Diện tích phải từ 1-1000 m²")]
    public decimal Area { get; set; }

    [Required(ErrorMessage = "Số người tối đa không được để trống")]
    [Range(1, 20, ErrorMessage = "Số người phải từ 1-20")]
    public int MaxOccupants { get; set; }

    // Address
    [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống")]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quận/Huyện không được để trống")]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phường/Xã không được để trống")]
    [StringLength(100)]
    public string Ward { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ cụ thể không được để trống")]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    // Amenities
    public List<int> AmenityIds { get; set; } = new();

    // Images URLs
    public List<string> ImageUrls { get; set; } = new();
}
