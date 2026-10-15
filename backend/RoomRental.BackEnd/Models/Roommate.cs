namespace RoomRental.BackEnd.Models;

public class RoommatePost
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int CreatorTenantAccountId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int RequestedSlots { get; set; }
    public decimal? EstimatedShare { get; set; }
    public string? Requirements { get; set; }
    public string? RejectionReason { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public RentalContract Contract { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<RoommateRequest> Requests { get; set; } = new List<RoommateRequest>();
}

public class RoommateRequest
{
    public int Id { get; set; }
    public int RoommatePostId { get; set; }
    public int ApplicantTenantAccountId { get; set; }
    public string? Message { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public RoommatePost Post { get; set; } = null!;
    public User Applicant { get; set; } = null!;
}

public class ContractOccupant
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int TenantAccountId { get; set; }
    public int Status { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public RentalContract Contract { get; set; } = null!;
    public User Tenant { get; set; } = null!;
}
