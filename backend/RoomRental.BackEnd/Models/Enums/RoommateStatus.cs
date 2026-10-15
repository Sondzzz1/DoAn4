namespace RoomRental.BackEnd.Models.Enums;

public static class RoommatePostStatus
{
    public const int PendingLandlordApproval = 0, Open = 1, Rejected = 2, Full = 3, Closed = 4, Expired = 5;
}
public static class RoommateRequestStatus
{
    public const int Pending = 0, AcceptedByPrimaryTenant = 1, Approved = 2, Rejected = 3, Cancelled = 4;
}
public static class ContractOccupantStatus
{
    public const int Active = 0, Left = 1;
}
