namespace RoomRental.Domain.Entities;

/// <summary>
/// Entity BaoCaoTinDang.
/// </summary>
public class Report
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int ReporterAccountId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByAccountId { get; set; }

    public virtual Post Post { get; set; } = null!;
    public virtual User Reporter { get; set; } = null!;
    public virtual User? ResolvedBy { get; set; }
}
