namespace RoomRental.BackEnd.Models.Enums;

public static class RentalRequestStatus
{
    public const int Pending = 0;
    public const int Approved = 1;
    public const int Rejected = 2;
    public const int Cancelled = 3;
    public const int ConvertedToContract = 4;
    public const int Expired = 5;
}

public static class DepositStatus
{
    public const int Pending = 0;
    public const int Paid = 1;
    public const int Confirmed = 2;
    public const int RefundRequested = 3;
    public const int Refunded = 4;
    public const int Cancelled = 5;
    public const int Expired = 6;
}

public static class RentalContractStatus
{
    public const int PendingSignature = 0;
    public const int Active = 1;
    public const int Terminated = 2;
    public const int Expired = 3;
    public const int Cancelled = 4;

    public static readonly int[] EffectiveStatuses = [PendingSignature, Active];
}

public static class PaymentTransactionStatus
{
    public const int Pending = 0;
    public const int Succeeded = 1;
    public const int Failed = 2;
}

public static class MonthlyBillStatus
{
    public const int Unpaid = 0;
    public const int Paid = 1;
    public const int Cancelled = 2;
    public const int PendingPayment = 3;
    public const int Overdue = 4;
}

public static class ReportStatus
{
    public const int Pending = 0;
    public const int Processing = 1;
    public const int Resolved = 2;
    public const int Rejected = 3;
}

public static class IncidentStatus
{
    public const int Pending = 0;
    public const int InProgress = 1;
    public const int Resolved = 2;
    public const int Rejected = 3;
}
