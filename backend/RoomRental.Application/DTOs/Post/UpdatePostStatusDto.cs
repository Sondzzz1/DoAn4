using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RoomRental.Domain.Enums;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO để update trạng thái Post (Landlord)
/// </summary>
public class UpdatePostStatusDto
{
    [Required]
    [JsonIgnore] public PostStatus Status { get; set; }
    public PostStatus TrangThai { get => Status; set => Status = value; }
}
