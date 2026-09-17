namespace RoomRental.Application.DTOs.Report;

public class CreateReportDto
{
    public int PostId { get; set; }
    public string? Reason { get; set; }
    public string? LyDo { get; set; }
    public string? Description { get; set; }
    public string? MoTa { get; set; }

    public string GetReason() => !string.IsNullOrWhiteSpace(Reason) ? Reason : (LyDo ?? "Nội dung vi phạm");
    public string? GetDescription() => !string.IsNullOrWhiteSpace(Description) ? Description : MoTa;
}

public class ReportDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public int ReporterAccountId { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string? ReporterEmail { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; } // 0: Pending, 1: Processing, 2: Resolved, 3: Rejected
    public string StatusText => Status switch
    {
        0 => "Pending",
        1 => "Processing",
        2 => "Resolved",
        3 => "Rejected",
        _ => "Unknown"
    };
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByAccountId { get; set; }
    public string? ResolvedByName { get; set; }
}

public class UpdateReportStatusDto
{
    public int Status { get; set; } // 0: Pending, 1: Processing, 2: Resolved, 3: Rejected
    public string? StatusName { get; set; }

    public int GetResolvedStatus()
    {
        if (!string.IsNullOrWhiteSpace(StatusName))
        {
            return StatusName.ToLowerInvariant() switch
            {
                "pending" or "choduyet" => 0,
                "processing" or "dangxuly" => 1,
                "resolved" or "daxuly" => 2,
                "rejected" or "tuchoi" => 3,
                _ => Status
            };
        }

        return Status;
    }
}
