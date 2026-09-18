namespace RoomRental.Application.DTOs.Amenity;

using System.Text.Json.Serialization;

public class AmenityDto
{
    public int Id { get; set; }
    [JsonIgnore] public string Name { get; set; } = string.Empty;
    [JsonIgnore] public string? Icon { get; set; }
    [JsonIgnore] public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAmenityDto
{
    public string Name { get; set; } = string.Empty;
    public string TenTienIch { get => Name; set => Name = value; }
    public string? Icon { get; set; }
    public string? BieuTuong { get => Icon; set => Icon = value; }
    public string? Description { get; set; }
    public string? MoTa { get => Description; set => Description = value; }
    [JsonIgnore] public bool IsActive { get; set; } = true;
    public bool DangHoatDong { get => IsActive; set => IsActive = value; }
}

public class UpdateAmenityDto : CreateAmenityDto
{
}
