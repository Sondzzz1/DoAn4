namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// Query Parameters cho tìm kiếm bài đăng với đầy đủ bộ lọc
/// </summary>
public class PostQueryParameters
{
    public string? Keyword { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public int? CategoryId { get; set; }
    public int? MaxOccupants { get; set; }
    public List<int>? AmenityIds { get; set; }
    public int? Status { get; set; }
    public int? LandlordId { get; set; }
    public string? SortBy { get; set; } // new, price_asc, price_desc, area_asc, area_desc, views
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
