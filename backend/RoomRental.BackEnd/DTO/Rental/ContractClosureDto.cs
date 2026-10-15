using System.ComponentModel.DataAnnotations;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.DTO.Rental;

public class ClosureCommandDto
{
    [Required, StringLength(100, MinimumLength = 16)] public string IdempotencyKey { get; set; } = "";
    public string? RowVersion { get; set; }
    public int? TargetId { get; set; }
    [StringLength(2000)] public string? Note { get; set; }
    public DateTimeOffset? EffectiveEndAt { get; set; }
    public byte? HandoverKind { get; set; }
    public DateTimeOffset? ActualHandoverAt { get; set; }
    public decimal? ClosingElectricity { get; set; }
    public decimal? ClosingWater { get; set; }
    [StringLength(2000)] public string? Condition { get; set; }
    [StringLength(2000)] public string? DamageNotes { get; set; }
    [Required] public List<string> EvidenceUrls { get; set; } = [];
    [Required] public List<SettlementDamageDto> Damages { get; set; } = [];
    [StringLength(500)] public string? Reference { get; set; }
}
public class SettlementDamageDto
{
    public decimal Amount { get; set; }
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    [Required] public List<string> EvidenceUrls { get; set; } = [];
}
public class ContractClosureView
{
    public int ContractId { get; set; }
    public int ContractStatus { get; set; }
    public int TenantAccountId { get; set; }
    public int LandlordAccountId { get; set; }
    public bool NeedsReview { get; set; }
    public ContractClosure? Closure { get; set; }
    public List<ContractEndRequest> EndRequests { get; set; } = [];
    public List<ContractSettlement> Settlements { get; set; } = [];
    public List<ContractClosureEvent> Events { get; set; } = [];
    public List<ClosureBillView> Bills { get; set; } = [];
    public List<string> ReleaseBlockers { get; set; } = [];
    public List<string> AllowedOperations { get; set; } = [];
}
public record ClosureBillView(int Id, int Month, int Year, int Status, decimal TotalAmount, decimal NewElectricity, decimal NewWater);
