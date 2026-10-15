using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
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
    public async Task Historical_terminal_contract_requires_verified_closure_and_invalid_target_does_not_persist()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.TemporarilyUnavailable;
        db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
        db.RentalContracts.Add(new RentalContract { PostId = 1, RentalRequestId = 1, LandlordAccountId = 1, TenantAccountId = 10, Status = RentalContractStatus.Terminated });
        await db.SaveChangesAsync();
        var rooms = new RoomBLL(db);
        await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomStatusAsync(1, 1, (RoomStatus)99));
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomStatusAsync(1, 1, RoomStatus.Available))).StatusCode);
        Assert.Equal(RoomStatus.TemporarilyUnavailable, db.Rooms.Single(r => r.Id == 1).Status);
    }
}

[Collection("SqlServerWorkflow")]
public class RoomStatusSqlTests
{
    [SqlServerFact]
    public async Task SqlServer_status_mapping_persists_and_stale_reservation_is_protected()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        builder.InitialCatalog += "_RoomStatus";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var password = BCrypt.Net.BCrypt.HashPassword("QaOnly-Bl001-2026!");
        User Account(string name, int role) => new() { UserName = name + "@bl001.test", Email = name + "@bl001.test", FullName = "QA " + name, PasswordHash = password, RoleId = role };
        var owner = new LandlordProfile { Account = Account("owner", 2) };
        var tenant = Account("tenant", 1);
        var room = new Room { Landlord = owner, Category = new RoomCategory { Name = "Phong tro QA" }, RoomName = "QA BL001", Address = "Dia chi QA", Price = 3000000, Area = 25, MaxOccupants = 2 };
        var post = new Post { Landlord = owner, Room = room, Title = "QA BL001 approved", Content = "QA", Status = PostStatus.Approved, DisplayPrice = room.Price };
        db.AddRange(post, tenant, Account("admin", 0));
        await db.SaveChangesAsync();
        var service = new RoomBLL(db);
        await service.UpdateRoomStatusAsync(owner.AccountId, room.Id, new UpdateRoomStatusDto { TrangThai = "3" }.GetResolvedStatus());
        db.ChangeTracker.Clear();
        Assert.Equal(RoomStatus.TemporarilyUnavailable, (await db.Rooms.AsNoTracking().SingleAsync()).Status);
        await service.UpdateRoomStatusAsync(owner.AccountId, room.Id, new UpdateRoomStatusDto { TrangThai = "0" }.GetResolvedStatus());
        var request = new RentalRequest { PostId = post.Id, TenantAccountId = tenant.Id, LandlordAccountId = owner.AccountId, Status = RentalRequestStatus.Approved };
        db.RentalRequests.Add(request);
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateRoomStatusAsync(owner.AccountId, room.Id, RoomStatus.TemporarilyUnavailable))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteRoomAsync(owner.AccountId, room.Id))).StatusCode);
        request.Status = RentalRequestStatus.Expired;
        db.Deposits.Add(new Deposit { RentalRequestId = request.Id, TenantAccountId = tenant.Id, LandlordAccountId = owner.AccountId, Amount = 1000000, Status = DepositStatus.Paid });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateRoomStatusAsync(owner.AccountId, room.Id, RoomStatus.TemporarilyUnavailable));
        Assert.Equal(RoomStatus.Available, (await db.Rooms.AsNoTracking().SingleAsync()).Status);

        // Keep isolated review fixtures in each state; never modify the user's database.
        foreach (var state in Enum.GetValues<RoomStatus>())
            db.Rooms.Add(new Room { LandlordId = owner.Id, CategoryId = room.CategoryId, RoomName = "QA status " + state, Address = "Dia chi QA", Price = 3000000, Area = 25, MaxOccupants = 2, Status = state });
        await db.SaveChangesAsync();
    }
}
