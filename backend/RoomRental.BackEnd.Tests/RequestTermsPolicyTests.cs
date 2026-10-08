using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class RequestTermsPolicyTests
{
    private static RentalRequestBLL Requests(RoomRental.BackEnd.DAL.ApplicationDbContext db) => new(db, null!, NullLogger<RentalRequestBLL>.Instance);

    [Fact]
    public async Task Public_change_cancels_pending_request_preserves_note_and_requires_new_consent()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.RentalRequests.Single().Status = RentalRequestStatus.Pending;
        db.RentalRequests.Single().Note = new string('x', 1000);
        db.Deposits.RemoveRange(db.Deposits);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Available;
        await db.SaveChangesAsync();
        await new RoomBLL(db).UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 4_000_000 });
        var old = Assert.Single(await Requests(db).LayCuaToiAsync(10, false));
        Assert.Equal(RentalRequestStatus.Cancelled, old.TrangThai);
        Assert.Contains("thay đổi", old.LyDoHuy);
        Assert.Equal(new string('x', 1000), old.GhiChu);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Requests(db).CapNhatTrangThaiAsync(1, old.Id, RentalRequestStatus.Approved, soTienDatCoc: 1_000_000, hanThanhToanCoc: DateTime.Now.AddDays(1)));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Requests(db).TaoAsync(10, new TaoYeuCauThueDto { BaiDangId = 1 }));
        await new AdminBLL(db, new PostBLL(db)).ApprovePostAsync(1);
        Assert.Equal(RentalRequestStatus.Pending, (await Requests(db).TaoAsync(10, new TaoYeuCauThueDto { BaiDangId = 1 })).TrangThai);
    }

    [Fact]
    public async Task No_op_does_not_cancel_pending_request()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.RentalRequests.Single().Status = RentalRequestStatus.Pending;
        await db.SaveChangesAsync();
        await new RoomBLL(db).UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 3_000_000 });
        Assert.Equal(RentalRequestStatus.Pending, db.RentalRequests.Single().Status);
        Assert.Null(db.RentalRequests.Single().CancellationReason);
    }

    [Theory]
    [InlineData(DepositStatus.Pending)]
    [InlineData(DepositStatus.Paid)]
    [InlineData(DepositStatus.Confirmed)]
    public async Task Approved_reservation_freezes_public_terms_until_contract(int depositStatus)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Deposits.Single().Status = depositStatus;
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 4_000_000 }))).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(3_000_000m, (await db.Rooms.FindAsync(1))!.Price);
        Assert.Equal(PostStatus.Approved, (await db.Posts.FindAsync(1))!.Status);
        Assert.Equal(depositStatus, db.Deposits.Single().Status);
    }

    [Fact]
    public async Task Converted_request_and_contract_snapshot_do_not_change_with_room_price()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
        db.RentalContracts.Add(new RentalContract { RentalRequestId = 1, PostId = 1, MonthlyRent = 3_000_000, Status = RentalContractStatus.Active });
        await db.SaveChangesAsync();
        await new RoomBLL(db).UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 4_000_000 });
        Assert.Equal(RentalRequestStatus.ConvertedToContract, db.RentalRequests.Single().Status);
        Assert.Equal(3_000_000m, db.RentalContracts.Single().MonthlyRent);
        Assert.Equal(PostStatus.Pending, db.Posts.Single(p => p.Id == 1).Status);
    }

    [Fact]
    public async Task Legacy_repair_is_owned_reserves_room_and_never_duplicates_deposit()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Deposits.RemoveRange(db.Deposits);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Available;
        await db.SaveChangesAsync();
        var dto = new ThietLapDatCocDto { SoTien = 1_000_000, HanThanhToan = DateTime.Now.AddDays(1) };
#pragma warning disable CS0618
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => Requests(db).ThietLapDatCocAsync(30, 1, dto))).StatusCode);
        var deposit = await Requests(db).ThietLapDatCocAsync(1, 1, dto);
        Assert.Equal(DepositStatus.Pending, deposit.TrangThai);
        Assert.Equal(RoomStatus.Reserved, db.Rooms.Single(r => r.Id == 1).Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Requests(db).ThietLapDatCocAsync(1, 1, dto))).StatusCode);
#pragma warning restore CS0618
        Assert.Single(await db.Deposits.ToListAsync());
    }
}
