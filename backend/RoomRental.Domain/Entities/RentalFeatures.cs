namespace RoomRental.Domain.Entities;

public class RentalRequest
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int TenantAccountId { get; set; }
    public int LandlordAccountId { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public virtual Post Post { get; set; } = null!;
}

public class Deposit
{
    public int Id { get; set; }
    public int RentalRequestId { get; set; }
    public int TenantAccountId { get; set; }
    public int LandlordAccountId { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class RentalContract
{
    public int Id { get; set; }
    public int RentalRequestId { get; set; }
    public int PostId { get; set; }
    public int TenantAccountId { get; set; }
    public int LandlordAccountId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public int Status { get; set; }
    public bool TenantConfirmed { get; set; }
    public bool LandlordConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class Incident
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int ReporterAccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class RoomReview
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int PostId { get; set; }
    public int TenantAccountId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
