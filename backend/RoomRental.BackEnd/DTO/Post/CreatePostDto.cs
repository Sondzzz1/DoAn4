using System.ComponentModel.DataAnnotations;

namespace RoomRental.BackEnd.DTO.Post;

public class CreatePostDto
{
    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }
    public string? Title { get; set; }
    public string? TieuDe { get; set; }
    public string? Description { get; set; }
    public string? MoTa { get; set; }
    public string GetTitle() => Title ?? TieuDe ?? string.Empty;
    public string GetDescription() => Description ?? MoTa ?? string.Empty;
}
