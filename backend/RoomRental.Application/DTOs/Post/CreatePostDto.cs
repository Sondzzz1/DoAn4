using System.ComponentModel.DataAnnotations;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO để tạo Post mới (Landlord đăng tin)
/// Hỗ trợ cả property tiếng Anh và tiếng Việt
/// </summary>
public class CreatePostDto
{
    public string? Title { get; set; }
    public string? TieuDe { get; set; }

    public string? Description { get; set; }
    public string? MoTa { get; set; }

    public decimal? Price { get; set; }
    public decimal? Gia { get; set; }

    // Room Details
    public int? CategoryId { get; set; }
    public int? LoaiPhongId { get; set; }

    public decimal? Area { get; set; }
    public decimal? DienTich { get; set; }

    public int? MaxOccupants { get; set; }
    public int? SoNguoiToiDa { get; set; }

    // Address
    public string? Province { get; set; }
    public string? ThanhPho { get; set; }

    public string? District { get; set; }
    public string? Quan { get; set; }

    public string? Ward { get; set; }
    public string? Phuong { get; set; }

    public string? Address { get; set; }
    public string? DiaChi { get; set; }

    // Coordinates (Google Maps)
    public decimal? Latitude { get; set; }
    public decimal? ViDo { get; set; }

    public decimal? Longitude { get; set; }
    public decimal? KinhDo { get; set; }

    // Additional room details
    public decimal? ElectricityPrice { get; set; }
    public decimal? TienDien { get; set; }

    public decimal? WaterPrice { get; set; }
    public decimal? TienNuoc { get; set; }

    public decimal? ServiceFee { get; set; }
    public decimal? PhiDichVu { get; set; }

    // Amenities & Images
    public List<int> AmenityIds { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();

    // Helper methods to resolve values
    public string GetTitle() => !string.IsNullOrWhiteSpace(Title) ? Title : (TieuDe ?? string.Empty);
    public string GetDescription() => !string.IsNullOrWhiteSpace(Description) ? Description : (MoTa ?? string.Empty);
    public decimal GetPrice() => Price ?? Gia ?? 0;
    public int GetCategoryId() => CategoryId ?? LoaiPhongId ?? 1;
    public decimal GetArea() => Area ?? DienTich ?? 0;
    public int GetMaxOccupants() => MaxOccupants ?? SoNguoiToiDa ?? 1;
    public string GetProvince() => !string.IsNullOrWhiteSpace(Province) ? Province : (ThanhPho ?? string.Empty);
    public string GetDistrict() => !string.IsNullOrWhiteSpace(District) ? District : (Quan ?? string.Empty);
    public string GetWard() => !string.IsNullOrWhiteSpace(Ward) ? Ward : (Phuong ?? string.Empty);
    public string GetAddress() => !string.IsNullOrWhiteSpace(Address) ? Address : (DiaChi ?? string.Empty);
    public decimal? GetLatitude() => Latitude ?? ViDo;
    public decimal? GetLongitude() => Longitude ?? KinhDo;
}
