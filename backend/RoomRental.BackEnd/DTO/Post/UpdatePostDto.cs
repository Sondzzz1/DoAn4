namespace RoomRental.BackEnd.DTO.Post;

using System.Text.Json.Serialization;

/// <summary>
/// DTO để cập nhật Post (Landlord sửa tin)
/// Hỗ trợ cả property tiếng Anh và tiếng Việt
/// </summary>
public class UpdatePostDto
{
    [JsonIgnore] public string? Title { get; set; }
    public string? TieuDe { get; set; }

    [JsonIgnore] public string? Description { get; set; }
    public string? MoTa { get; set; }

    [JsonIgnore] public decimal? Price { get; set; }
    public decimal? Gia { get; set; }

    [JsonIgnore] public int? CategoryId { get; set; }
    public int? LoaiPhongId { get; set; }

    [JsonIgnore] public decimal? Area { get; set; }
    public decimal? DienTich { get; set; }

    [JsonIgnore] public int? MaxOccupants { get; set; }
    public int? SoNguoiToiDa { get; set; }

    [JsonIgnore] public string? Province { get; set; }
    public string? ThanhPho { get; set; }

    [JsonIgnore] public string? District { get; set; }
    public string? Quan { get; set; }

    [JsonIgnore] public string? Ward { get; set; }
    public string? Phuong { get; set; }

    [JsonIgnore] public string? Address { get; set; }
    public string? DiaChi { get; set; }

    [JsonIgnore] public decimal? Latitude { get; set; }
    public decimal? ViDo { get; set; }

    [JsonIgnore] public decimal? Longitude { get; set; }
    public decimal? KinhDo { get; set; }

    [JsonIgnore] public decimal? ElectricityPrice { get; set; }
    [JsonIgnore] public decimal? WaterPrice { get; set; }
    [JsonIgnore] public decimal? ServiceFee { get; set; }

    [JsonIgnore] public List<int>? AmenityIds { get; set; }
    [JsonIgnore] public List<string>? ImageUrls { get; set; }
    public List<int>? TienIchIds
    {
        get => AmenityIds;
        set => AmenityIds = value;
    }
    public List<string>? DanhSachAnh
    {
        get => ImageUrls;
        set => ImageUrls = value;
    }

    public string? GetTitle() => !string.IsNullOrWhiteSpace(Title) ? Title : TieuDe;
    public string? GetDescription() => !string.IsNullOrWhiteSpace(Description) ? Description : MoTa;
    public decimal? GetPrice() => Price ?? Gia;
    public int? GetCategoryId() => CategoryId ?? LoaiPhongId;
    public decimal? GetArea() => Area ?? DienTich;
    public int? GetMaxOccupants() => MaxOccupants ?? SoNguoiToiDa;
    public string? GetProvince() => !string.IsNullOrWhiteSpace(Province) ? Province : ThanhPho;
    public string? GetDistrict() => !string.IsNullOrWhiteSpace(District) ? District : Quan;
    public string? GetWard() => !string.IsNullOrWhiteSpace(Ward) ? Ward : Phuong;
    public string? GetAddress() => !string.IsNullOrWhiteSpace(Address) ? Address : DiaChi;
    public decimal? GetLatitude() => Latitude ?? ViDo;
    public decimal? GetLongitude() => Longitude ?? KinhDo;
}
