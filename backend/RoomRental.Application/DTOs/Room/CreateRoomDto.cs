using System.Text.Json.Serialization;

namespace RoomRental.Application.DTOs.Room;

public class CreateRoomDto
{
    [JsonIgnore] public int CategoryId { get; set; } = 1;
    public int DanhMucId { get => CategoryId; set => CategoryId = value; }
    [JsonIgnore] public string RoomName { get; set; } = string.Empty;
    public string TenPhong { get => RoomName; set => RoomName = value; }
    [JsonIgnore] public string? Description { get; set; }
    public string? MoTa { get => Description; set => Description = value; }
    [JsonIgnore] public decimal Price { get; set; }
    public decimal Gia { get => Price; set => Price = value; }
    [JsonIgnore] public decimal Area { get; set; }
    public decimal DienTich { get => Area; set => Area = value; }
    [JsonIgnore] public int MaxOccupants { get; set; } = 2;
    public int SoNguoiToiDa { get => MaxOccupants; set => MaxOccupants = value; }
    [JsonIgnore] public int? Bedrooms { get; set; }
    public int? SoPhongNgu { get => Bedrooms; set => Bedrooms = value; }
    [JsonIgnore] public int? Bathrooms { get; set; }
    public int? SoPhongTam { get => Bathrooms; set => Bathrooms = value; }
    [JsonIgnore] public int? Floor { get; set; }
    public int? Tang { get => Floor; set => Floor = value; }
    [JsonIgnore] public string Address { get; set; } = string.Empty;
    public string DiaChi { get => Address; set => Address = value; }
    [JsonIgnore] public string? Ward { get; set; }
    public string? Phuong { get => Ward; set => Ward = value; }
    [JsonIgnore] public string? District { get; set; }
    public string? Quan { get => District; set => District = value; }
    [JsonIgnore] public string? Province { get; set; }
    public string? ThanhPho { get => Province; set => Province = value; }
    [JsonIgnore] public decimal? Latitude { get; set; }
    public decimal? ViDo { get => Latitude; set => Latitude = value; }
    [JsonIgnore] public decimal? Longitude { get; set; }
    public decimal? KinhDo { get => Longitude; set => Longitude = value; }
    [JsonIgnore] public decimal? ElectricityPrice { get; set; }
    public decimal? TienDien { get => ElectricityPrice; set => ElectricityPrice = value; }
    [JsonIgnore] public decimal? WaterPrice { get; set; }
    public decimal? TienNuoc { get => WaterPrice; set => WaterPrice = value; }
    [JsonIgnore] public decimal? ServiceFee { get; set; }
    public decimal? PhiDichVu { get => ServiceFee; set => ServiceFee = value; }

    [JsonIgnore] public List<int> AmenityIds { get; set; } = new();
    public List<int> TienIchIds { get => AmenityIds; set => AmenityIds = value ?? new(); }
    [JsonIgnore] public List<string> ImageUrls { get; set; } = new();
    public List<string> DanhSachAnh { get => ImageUrls; set => ImageUrls = value ?? new(); }
}

public class UpdateRoomDto : CreateRoomDto
{
}
