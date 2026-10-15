using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

internal static class ContractOccupancyPolicy
{
    // Call after persisting contract/member state, inside the caller's workflow transaction.
    public static async Task<int> RecalculateCurrentOccupantsAsync(ApplicationDbContext db, int roomId)
    {
        var count = await CountAsync(db, roomId);
        var room = await db.Rooms.FirstAsync(r => r.Id == roomId);
        if (count > room.MaxOccupants)
            throw BusinessRuleException.Conflict("Số người đang cư trú vượt sức chứa của phòng.");
        room.CurrentOccupants = count;
        return count;
    }

    public static async Task<int> CountAsync(ApplicationDbContext db, int roomId)
    {
        await ContractClosurePolicy.EnsureSchemaAsync(db);
        var contracts = db.RentalContracts.Where(c => c.Post!.RoomId == roomId &&
            !db.ContractClosures.Any(h => h.ContractId == c.Id && h.HandoverStatus == HandoverState.Confirmed) &&
            (c.Status == RentalContractStatus.Active ||
                ((c.Status == RentalContractStatus.Terminated || c.Status == RentalContractStatus.Expired) &&
                db.ContractClosureEvents.Any(e => e.EventType == "contract-ended" && e.FromState == "1" && db.ContractClosures.Any(h => h.Id == e.ClosureId && h.ContractId == c.Id)))));
        return await contracts.CountAsync() + await db.ContractOccupants.CountAsync(o =>
            o.Status == ContractOccupantStatus.Active && contracts.Any(c => c.Id == o.ContractId));
    }

    public static async Task EndAsync(ApplicationDbContext db, int contractId, bool expired)
    {
        var now = DateTime.UtcNow;
        var posts = await db.RoommatePosts.Where(p => p.ContractId == contractId &&
            (p.Status == RoommatePostStatus.PendingLandlordApproval || p.Status == RoommatePostStatus.Open || p.Status == RoommatePostStatus.Full)).ToListAsync();
        foreach (var post in posts)
        {
            post.Status = expired ? RoommatePostStatus.Expired : RoommatePostStatus.Closed;
            post.ClosedAt = now;
        }
        foreach (var request in await db.RoommateRequests.Where(r => r.Post.ContractId == contractId &&
            (r.Status == RoommateRequestStatus.Pending || r.Status == RoommateRequestStatus.AcceptedByPrimaryTenant)).ToListAsync())
            request.Status = RoommateRequestStatus.Cancelled;
    }
}
