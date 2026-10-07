namespace RoomRental.BackEnd.DTO.Room;

public class CreateRoomDto
{
    public int? CategoryId { get; set; }
    public int? DanhMucId { get; set; }

    public string? RoomName { get; set; }
    public string? TenPhong { get; set; }

    public string? Description { get; set; }
    public string? MoTa { get; set; }

    public decimal? Price { get; set; }
    public decimal? Gia { get; set; }

    public decimal? Area { get; set; }
    public decimal? DienTich { get; set; }

    public int? MaxOccupants { get; set; }
    public int? SoNguoiToiDa { get; set; }

    public int? Bedrooms { get; set; }
    public int? SoPhongNgu { get; set; }

    public int? Bathrooms { get; set; }
    public int? SoPhongTam { get; set; }

    public int? Floor { get; set; }
    public int? Tang { get; set; }

    public string? Address { get; set; }
    public string? DiaChi { get; set; }

    public string? Ward { get; set; }
    public string? Phuong { get; set; }

    public string? District { get; set; }
    public string? Quan { get; set; }

    public string? Province { get; set; }
    public string? ThanhPho { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? ViDo { get; set; }

    public decimal? Longitude { get; set; }
    public decimal? KinhDo { get; set; }

    public decimal? ElectricityPrice { get; set; }
    public decimal? TienDien { get; set; }

    public decimal? WaterPrice { get; set; }
    public decimal? TienNuoc { get; set; }

    public decimal? ServiceFee { get; set; }
    public decimal? PhiDichVu { get; set; }

    public List<int>? AmenityIds { get; set; } = new();
    public List<int>? TienIchIds
    {
        get => AmenityIds;
        set { if (value != null) AmenityIds = value; }
    }

    public List<string>? ImageUrls { get; set; } = new();
    public List<string>? DanhSachAnh
    {
        get => ImageUrls;
        set { if (value != null) ImageUrls = value; }
    }

    public int GetCategoryId() => CategoryId ?? DanhMucId ?? 1;
    public string GetRoomName() => !string.IsNullOrWhiteSpace(RoomName) ? RoomName : (TenPhong ?? string.Empty);
    public string? GetDescription() => !string.IsNullOrWhiteSpace(Description) ? Description : MoTa;
    public decimal GetPrice() => Price ?? Gia ?? 0;
    public decimal GetArea() => Area ?? DienTich ?? 0;
    public int GetMaxOccupants() => MaxOccupants ?? SoNguoiToiDa ?? 2;
    public int? GetBedrooms() => Bedrooms ?? SoPhongNgu;
    public int? GetBathrooms() => Bathrooms ?? SoPhongTam;
    public int? GetFloor() => Floor ?? Tang;
    public string GetAddress() => !string.IsNullOrWhiteSpace(Address) ? Address : (DiaChi ?? string.Empty);
    public string? GetWard() => !string.IsNullOrWhiteSpace(Ward) ? Ward : Phuong;
    public string? GetDistrict() => !string.IsNullOrWhiteSpace(District) ? District : Quan;
    public string? GetProvince() => !string.IsNullOrWhiteSpace(Province) ? Province : ThanhPho;
    public decimal? GetLatitude() => Latitude ?? ViDo;
    public decimal? GetLongitude() => Longitude ?? KinhDo;
    public decimal? GetElectricityPrice() => ElectricityPrice ?? TienDien;
    public decimal? GetWaterPrice() => WaterPrice ?? TienNuoc;
    public decimal? GetServiceFee() => ServiceFee ?? PhiDichVu;
}

public class UpdateRoomDto : CreateRoomDto
{
    public UpdateRoomDto()
    {
        // Omitted collections preserve existing data; an explicit empty list clears it.
        AmenityIds = null;
        ImageUrls = null;
    }
}
