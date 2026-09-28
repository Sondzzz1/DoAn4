using System.ComponentModel.DataAnnotations;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.DTO.Post;

/// <summary>
/// DTO để update trạng thái Post (Landlord)
/// </summary>
public class UpdatePostStatusDto
{
    public PostStatus? Status { get; set; }
    public PostStatus? TrangThai { get; set; }

    public PostStatus GetStatus() => Status ?? TrangThai ?? PostStatus.Approved;
}
