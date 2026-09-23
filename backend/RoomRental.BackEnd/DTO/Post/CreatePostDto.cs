using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoomRental.BackEnd.DTO.Post;

/// <summary>
/// DTO để tạo Post mới (Landlord đăng tin)
/// Hỗ trợ cả property tiếng Anh và tiếng Việt
/// </summary>
public class CreatePostDto
{
    [JsonIgnore] public string? Title { get; set; }
    public string? TieuDe { get; set; }

    [JsonIgnore] public string? Description { get; set; }
    public string? MoTa { get; set; }

    [JsonIgnore] public decimal? Price { get; set; }
    public decimal? Gia { get; set; }

    // Room Details
    [JsonIgnore] public int? CategoryId { get; set; }
    public int? LoaiPhongId { get; set; }

    [JsonIgnore] public decimal? Area { get; set; }
    public decimal? DienTich { get; set; }

    [JsonIgnore] public int? MaxOccupants { get; set; }
    public int? SoNguoiToiDa { get; set; }

    // Address
    [JsonIgnore] public string? Province { get; set; }
    public string? ThanhPho { get; set; }

    [JsonIgnore] public string? District { get; set; }
    public string? Quan { get; set; }

    [JsonIgnore] public string? Ward { get; set; }
    public string? Phuong { get; set; }

    [JsonIgnore] public string? Address { get; set; }
    public string? DiaChi { get; set; }

    // Coordinates (Google Maps)
    [JsonIgnore] public decimal? Latitude { get; set; }
    public decimal? ViDo { get; set; }

    [JsonIgnore] public decimal? Longitude { get; set; }
    public decimal? KinhDo { get; set; }

    // Additional room details
    [JsonIgnore] public decimal? ElectricityPrice { get; set; }
    public decimal? TienDien { get; set; }

    [JsonIgnore] public decimal? WaterPrice { get; set; }
    public decimal? TienNuoc { get; set; }

    [JsonIgnore] public decimal? ServiceFee { get; set; }
    public decimal? PhiDichVu { get; set; }

    // Amenities & Images
    [JsonIgnore] public List<int> AmenityIds { get; set; } = new();
    [JsonIgnore] public List<string> ImageUrls { get; set; } = new();
    public List<int> TienIchIds
    {
        get => AmenityIds;
        set => AmenityIds = value ?? new();
    }
    public List<string> DanhSachAnh
    {
        get => ImageUrls;
        set => ImageUrls = value ?? new();
    }

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
