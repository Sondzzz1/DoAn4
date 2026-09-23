namespace RoomRental.BackEnd.DTO.Post;

/// <summary>
/// Query Parameters cho tìm kiếm bài đăng với đầy đủ bộ lọc
/// </summary>
public class PostQueryParameters
{
    public string? Keyword { get; set; }
    public string? TuKhoa { get => Keyword; set => Keyword = value; }
    public string? Province { get; set; }
    public string? ThanhPho { get => Province; set => Province = value; }
    public string? District { get; set; }
    public string? Quan { get => District; set => District = value; }
    public string? Ward { get; set; }
    public string? Phuong { get => Ward; set => Ward = value; }
    public decimal? MinPrice { get; set; }
    public decimal? GiaThapNhat { get => MinPrice; set => MinPrice = value; }
    public decimal? MaxPrice { get; set; }
    public decimal? GiaCaoNhat { get => MaxPrice; set => MaxPrice = value; }
    public decimal? MinArea { get; set; }
    public decimal? DienTichToiThieu { get => MinArea; set => MinArea = value; }
    public decimal? MaxArea { get; set; }
    public decimal? DienTichToiDa { get => MaxArea; set => MaxArea = value; }
    public int? CategoryId { get; set; }
    public int? DanhMucId { get => CategoryId; set => CategoryId = value; }
    public int? MaxOccupants { get; set; }
    public int? SoNguoiToiDa { get => MaxOccupants; set => MaxOccupants = value; }
    public List<int>? AmenityIds { get; set; }
    public List<int>? TienIchIds { get => AmenityIds; set => AmenityIds = value; }
    public int? Status { get; set; }
    public int? TrangThai { get => Status; set => Status = value; }
    public int? LandlordId { get; set; }
    public int? ChuTroId { get => LandlordId; set => LandlordId = value; }
    public string? SortBy { get; set; } // new, price_asc, price_desc, area_asc, area_desc, views
    public string? SapXepTheo { get => SortBy; set => SortBy = value; }
    public int PageNumber { get; set; } = 1;
    public int SoTrang { get => PageNumber; set => PageNumber = value; }
    public int PageSize { get; set; } = 20;
    public int KichThuocTrang { get => PageSize; set => PageSize = value; }

    // Map Geolocation & Radius Search
    public decimal? Latitude { get; set; }
    public decimal? ViDo { get => Latitude; set => Latitude = value; }
    public decimal? Longitude { get; set; }
    public decimal? KinhDo { get => Longitude; set => Longitude = value; }
    public double? RadiusInKm { get; set; }
    public double? BanKinhKm { get => RadiusInKm; set => RadiusInKm = value; }
}
