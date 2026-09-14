using System.ComponentModel.DataAnnotations;
using RoomRental.Domain.Enums;

namespace RoomRental.Application.DTOs.Post;

/// <summary>
/// DTO để update trạng thái Post (Landlord)
/// </summary>
public class UpdatePostStatusDto
{
    [Required]
    public PostStatus Status { get; set; }
}
