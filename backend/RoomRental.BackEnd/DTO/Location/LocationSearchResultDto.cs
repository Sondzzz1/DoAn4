namespace RoomRental.BackEnd.DTO.Location;

/// <summary>
/// Kết quả tìm kiếm địa điểm từ Nominatim
/// </summary>
public class LocationSearchResultDto
{
    public string DisplayName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
}
