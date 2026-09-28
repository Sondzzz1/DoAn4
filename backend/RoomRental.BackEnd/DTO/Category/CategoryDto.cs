namespace RoomRental.BackEnd.DTO.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int RoomCount { get; set; }
}

public class CreateCategoryDto
{
    public string? Name { get; set; }
    public string? TenDanhMuc { get; set; }
    public string? Description { get; set; }
    public string? MoTa { get; set; }
    public string? ImageUrl { get; set; }
    public string? DuongDanAnh { get; set; }
    public bool? IsActive { get; set; }
    public bool? DangHoatDong { get; set; }

    public string GetName() => !string.IsNullOrWhiteSpace(Name) ? Name : (TenDanhMuc ?? string.Empty);
    public string? GetDescription() => !string.IsNullOrWhiteSpace(Description) ? Description : MoTa;
    public string? GetImageUrl() => !string.IsNullOrWhiteSpace(ImageUrl) ? ImageUrl : DuongDanAnh;
    public bool GetIsActive() => IsActive ?? DangHoatDong ?? true;
}

public class UpdateCategoryDto : CreateCategoryDto
{
}
