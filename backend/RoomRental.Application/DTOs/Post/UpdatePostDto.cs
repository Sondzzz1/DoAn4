namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO để cập nhật Post (Landlord sửa tin)
/// Hỗ trợ cả property tiếng Anh và tiếng Việt
/// </summary>
public class UpdatePostDto
{
    public string? Title { get; set; }
    public string? TieuDe { get; set; }

    public string? Description { get; set; }
    public string? MoTa { get; set; }

    public decimal? Price { get; set; }
    public decimal? Gia { get; set; }

    public int? CategoryId { get; set; }
    public int? LoaiPhongId { get; set; }

    public decimal? Area { get; set; }
    public decimal? DienTich { get; set; }

    public int? MaxOccupants { get; set; }
    public int? SoNguoiToiDa { get; set; }

    public string? Province { get; set; }
    public string? ThanhPho { get; set; }

    public string? District { get; set; }
    public string? Quan { get; set; }

    public string? Ward { get; set; }
    public string? Phuong { get; set; }

    public string? Address { get; set; }
    public string? DiaChi { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? ViDo { get; set; }

    public decimal? Longitude { get; set; }
    public decimal? KinhDo { get; set; }

    public decimal? ElectricityPrice { get; set; }
    public decimal? WaterPrice { get; set; }
    public decimal? ServiceFee { get; set; }

    public List<int>? AmenityIds { get; set; }
    public List<string>? ImageUrls { get; set; }

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
