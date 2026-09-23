namespace RoomRental.BackEnd.DTO.Category;

using System.Text.Json.Serialization;

public class CategoryDto
{
    public int Id { get; set; }
    [JsonIgnore] public string Name { get; set; } = string.Empty;
    [JsonIgnore] public string? Description { get; set; }
    [JsonIgnore] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int RoomCount { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string TenDanhMuc { get => Name; set => Name = value; }
    public string? Description { get; set; }
    public string? MoTa { get => Description; set => Description = value; }
    public string? ImageUrl { get; set; }
    public string? DuongDanAnh { get => ImageUrl; set => ImageUrl = value; }
    [JsonIgnore] public bool IsActive { get; set; } = true;
    public bool DangHoatDong { get => IsActive; set => IsActive = value; }
}

public class UpdateCategoryDto : CreateCategoryDto
{
}
