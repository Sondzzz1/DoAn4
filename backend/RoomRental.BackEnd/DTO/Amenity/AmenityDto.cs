namespace RoomRental.BackEnd.DTO.Amenity;

public class AmenityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAmenityDto
{
    public string? Name { get; set; }
    public string? TenTienIch { get; set; }
    public string? Icon { get; set; }
    public string? BieuTuong { get; set; }
    public string? Description { get; set; }
    public string? MoTa { get; set; }
    public bool? IsActive { get; set; }
    public bool? DangHoatDong { get; set; }

    public string GetName() => !string.IsNullOrWhiteSpace(Name) ? Name : (TenTienIch ?? string.Empty);
    public string? GetIcon() => !string.IsNullOrWhiteSpace(Icon) ? Icon : BieuTuong;
    public string? GetDescription() => !string.IsNullOrWhiteSpace(Description) ? Description : MoTa;
    public bool GetIsActive() => IsActive ?? DangHoatDong ?? true;
}

public class UpdateAmenityDto : CreateAmenityDto
{
}
