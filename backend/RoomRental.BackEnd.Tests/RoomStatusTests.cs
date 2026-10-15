using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class RoomStatusTests
{
    [Theory]
    [InlineData("0", RoomStatus.Available)]
    [InlineData("1", RoomStatus.Rented)]
    [InlineData("2", RoomStatus.Reserved)]
    [InlineData("3", RoomStatus.TemporarilyUnavailable)]
    [InlineData(" Available ", RoomStatus.Available)]
    [InlineData("rented", RoomStatus.Rented)]
    [InlineData("Reserved", RoomStatus.Reserved)]
    [InlineData("temporarilyunavailable", RoomStatus.TemporarilyUnavailable)]
    [InlineData("maintenance", RoomStatus.TemporarilyUnavailable)]
    [InlineData("tamngung", RoomStatus.TemporarilyUnavailable)]
    [InlineData("còn trống", RoomStatus.Available)]
    [InlineData("đã thuê", RoomStatus.Rented)]
    [InlineData("đang giữ chỗ", RoomStatus.Reserved)]
    [InlineData("tạm ngưng", RoomStatus.TemporarilyUnavailable)]
    public void String_fields_resolve_to_the_actual_enum(string input, RoomStatus expected)
    {
        Assert.Equal(expected, new UpdateRoomStatusDto { TrangThai = input }.GetResolvedStatus());
        Assert.Equal(expected, new UpdateRoomStatusDto { Status = input }.GetResolvedStatus());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    [InlineData("-1")]
    [InlineData("4")]
    [InlineData("999")]
    [InlineData("3.0")]
    public void Invalid_or_missing_status_never_defaults_to_available(string? input)
    {
        Assert.Equal(400, Assert.Throws<BusinessRuleException>(() =>
            new UpdateRoomStatusDto { TrangThai = input }.GetResolvedStatus()).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Numeric_desired_status_uses_the_same_mapping(int status) =>
        Assert.Equal((RoomStatus)status, new UpdateRoomStatusDto { DesiredStatus = (RoomStatus)status }.GetResolvedStatus());

    [Fact]
    public void Invalid_numeric_and_conflicting_aliases_are_rejected()
    {
        Assert.Throws<BusinessRuleException>(() => new UpdateRoomStatusDto { DesiredStatus = (RoomStatus)99 }.GetResolvedStatus());
        Assert.Throws<BusinessRuleException>(() => new UpdateRoomStatusDto { Status = "0", TrangThai = "3" }.GetResolvedStatus());
        Assert.Throws<BusinessRuleException>(() => new UpdateRoomStatusDto { DesiredStatus = RoomStatus.Available, TrangThai = "3" }.GetResolvedStatus());
        Assert.Equal(RoomStatus.TemporarilyUnavailable, new UpdateRoomStatusDto { DesiredStatus = RoomStatus.TemporarilyUnavailable, Status = "maintenance", TrangThai = "3" }.GetResolvedStatus());
    }

    public static IEnumerable<object[]> Transitions =>
        from source in Enum.GetValues<RoomStatus>()
        from target in Enum.GetValues<RoomStatus>()
        select new object[] { source, target };

    [Theory]
    [MemberData(nameof(Transitions))]
    public async Task Only_available_and_suspended_rooms_allow_manual_transitions(RoomStatus source, RoomStatus target)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        var room = (await db.Rooms.FindAsync(1))!;
        room.Status = source;
        await db.SaveChangesAsync();
        var allowed = source is RoomStatus.Available or RoomStatus.TemporarilyUnavailable &&
            target is RoomStatus.Available or RoomStatus.TemporarilyUnavailable;
        if (allowed)
            Assert.Equal(target, (await new RoomBLL(db).UpdateRoomStatusAsync(1, 1, target)).Status);
        else
            await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).UpdateRoomStatusAsync(1, 1, target));
        db.ChangeTracker.Clear();
        Assert.Equal(allowed ? target : source, (await db.Rooms.FindAsync(1))!.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Approved_request_blocks_manual_release_even_if_room_status_is_stale(int target)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        var room = (await db.Rooms.FindAsync(1))!;
        room.Status = RoomStatus.TemporarilyUnavailable;
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new RoomBLL(db).UpdateRoomStatusAsync(1, 1, (RoomStatus)target))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).DeleteRoomAsync(1, 1))).StatusCode);
        Assert.Equal(RoomStatus.TemporarilyUnavailable, room.Status);
    }

    [Theory]
    [InlineData(DepositStatus.Paid, RentalRequestStatus.Cancelled)]
    [InlineData(DepositStatus.Confirmed, RentalRequestStatus.Expired)]
    public async Task Unsettled_paid_reservation_cannot_be_released_manually(int depositStatus, int requestStatus)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        var room = (await db.Rooms.FindAsync(1))!;
        room.Status = RoomStatus.TemporarilyUnavailable;
        db.RentalRequests.Single().Status = requestStatus;
        db.Deposits.Single().Status = depositStatus;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).UpdateRoomStatusAsync(1, 1, RoomStatus.Available));
        Assert.Equal(RoomStatus.TemporarilyUnavailable, room.Status);
    }

    [Theory]
    [InlineData(RentalContractStatus.PendingSignature)]
    [InlineData(RentalContractStatus.PendingStart)]
    [InlineData(RentalContractStatus.Active)]
    public async Task Effective_contract_blocks_manual_status_and_delete(int contractStatus)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Available;
        db.RentalContracts.Add(new RentalContract { PostId = 1, RentalRequestId = 1, LandlordAccountId = 1, TenantAccountId = 10, Status = contractStatus });
        await db.SaveChangesAsync();
        var rooms = new RoomBLL(db);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomStatusAsync(1, 1, RoomStatus.TemporarilyUnavailable))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.DeleteRoomAsync(1, 1))).StatusCode);
    }

    [Fact]
    public async Task Historical_converted_deposit_does_not_block_reletting_and_invalid_target_does_not_persist()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.TemporarilyUnavailable;
        db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
        db.RentalContracts.Add(new RentalContract { PostId = 1, RentalRequestId = 1, LandlordAccountId = 1, TenantAccountId = 10, Status = RentalContractStatus.Terminated });
        await db.SaveChangesAsync();
        var rooms = new RoomBLL(db);
        await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomStatusAsync(1, 1, (RoomStatus)99));
        Assert.Equal(RoomStatus.Available, (await rooms.UpdateRoomStatusAsync(1, 1, RoomStatus.Available)).Status);
    }
}
