namespace RoomRental.BackEnd.DTO.Roommate;

public class CreateRoommatePostDto
{
    public int ContractId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int RequestedSlots { get; set; }
    public decimal? EstimatedShare { get; set; }
    public string? Requirements { get; set; }
}
public class RoommateDecisionDto { public bool Approve { get; set; } public string? Reason { get; set; } }
public class CreateRoommateRequestDto { public string? Message { get; set; } }
public class RoommatePostDto
{
    public int Id { get; set; }
    public int? ContractId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Requirements { get; set; }
    public string? RejectionReason { get; set; }
    public decimal? EstimatedShare { get; set; }
    public int RequestedSlots { get; set; }
    public int RemainingRequestedSlots { get; set; }
    public int Status { get; set; }
    public string CreatorName { get; set; } = "";
    public string? CreatorAvatar { get; set; }
    public string RoomName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Province { get; set; } = "";
    public string District { get; set; } = "";
    public decimal Area { get; set; }
    public int MaxOccupants { get; set; }
    public int CurrentOccupants { get; set; }
    public int AvailableSlots { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Amenities { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
public class RoommateSearchResult
{
    public List<RoommatePostDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
public class RoommateRequestDto
{
    public int Id { get; set; }
    public int RoommatePostId { get; set; }
    public int ApplicantTenantAccountId { get; set; }
    public string ApplicantName { get; set; } = "";
    public string? ApplicantAvatar { get; set; }
    public string? Message { get; set; }
    public int Status { get; set; }
    public string PostTitle { get; set; } = "";
    public int CreatorTenantAccountId { get; set; }
    public string CreatorName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
public class ContractOccupantDto
{
    public int Id { get; set; }
    public int TenantAccountId { get; set; }
    public string TenantName { get; set; } = "";
    public int Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}
public class RoommateContractDto
{
    public int ContractId { get; set; }
    public string RoomName { get; set; } = "";
    public int ContractStatus { get; set; }
    public int MaxOccupants { get; set; }
    public int CurrentOccupants { get; set; }
    public int AvailableSlots { get; set; }
    public bool CanCreatePost { get; set; }
    public List<ContractOccupantDto> Occupants { get; set; } = new();
}
