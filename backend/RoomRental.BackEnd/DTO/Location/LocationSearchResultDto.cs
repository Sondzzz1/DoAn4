namespace RoomRental.BackEnd.DTO.Location;

/// <summary>
/// Kết quả tìm kiếm địa điểm từ Nominatim
/// </summary>
public class LocationSearchResultDto
{
    public string DisplayName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
