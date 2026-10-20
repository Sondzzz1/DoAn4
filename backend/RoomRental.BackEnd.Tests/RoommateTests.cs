using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Roommate;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class RoommateTests
{
    internal static RoommateBLL Service(ApplicationDbContext db) => new(db, null!, NullLogger<RoommateBLL>.Instance);
    internal static RentalContractBLL Contracts(ApplicationDbContext db) => new(db, null!, NullLogger<RentalContractBLL>.Instance);
    internal static CreateRoommatePostDto Draft(int slots = 2) => new() { ContractId = 1, Title = "Tìm bạn ở cùng", Description = "Phòng cho ba người", RequestedSlots = slots, EstimatedShare = 1000000 };
    internal static async Task SeedAsync(ApplicationDbContext db)
    {
        if (db.Database.IsSqlServer()) await db.Database.OpenConnectionAsync();
        var users = Enumerable.Range(1, 8).Select(i => new User { Id = i, UserName = "roommate-test-" + i, Email = $"roommate-{i}@example.test", FullName = "Người " + i, PasswordHash = "test", RoleId = i is 1 or 6 ? 2 : i == 8 ? 0 : 1, IsActive = true }).ToList();
        db.Users.AddRange(users);
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.TaiKhoan ON");
        await db.SaveChangesAsync();
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.TaiKhoan OFF");
        var landlord = new LandlordProfile { Id = 1, AccountId = 1 };
        var category = new RoomCategory { Id = 1, Name = "Phòng trọ", IsActive = true };
        var room = new Room { Id = 1, LandlordId = 1, CategoryId = 1, RoomName = "P101", Address = "Địa chỉ test", Price = 3000000, Area = 30, MaxOccupants = 3, Status = RoomStatus.Rented };
        var post = new Post { Id = 1, RoomId = 1, LandlordId = 1, Title = "Tin chủ trọ", Content = "Test", Status = PostStatus.Approved, DisplayPrice = 3000000 };
        var request = new RentalRequest { Id = 1, PostId = 1, TenantAccountId = 2, LandlordAccountId = 1, Status = RentalRequestStatus.ConvertedToContract };
        var contract = new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 2, LandlordAccountId = 1, Status = RentalContractStatus.Active, StartDate = DateTime.Today.AddDays(-1), EndDate = DateTime.Today.AddMonths(2), MonthlyRent = 3000000, DepositAmount = 1000000, ElectricityPrice = 3500, WaterPrice = 20000, ServiceFee = 150000, TenantConfirmed = true, LandlordConfirmed = true };
        async Task AddAsync(object entity, string table)
        {
            var on = "SET IDENTITY_INSERT dbo." + table + " ON";
            var off = "SET IDENTITY_INSERT dbo." + table + " OFF";
            if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync(on);
            db.Add(entity); await db.SaveChangesAsync();
            if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync(off);
        }
        await AddAsync(landlord, "ChuTro"); await AddAsync(category, "DanhMucPhong"); await AddAsync(room, "PhongTro");
        await AddAsync(post, "TinDang"); await AddAsync(request, "YeuCauThuePhong"); await AddAsync(contract, "HopDongThue");
    }
    private static async Task<ApplicationDbContext> Fixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var db = new ApplicationDbContext(options); await SeedAsync(db); return db;
    }
    internal static async Task<int> OpenAsync(ApplicationDbContext db, int slots = 2)
    {
        var service = Service(db); var p = await service.CreateAsync(2, Draft(slots));
        await service.ModerateAsync(1, p.Id, new() { Approve = true }); return p.Id;
    }
    internal static async Task<int> AcceptedAsync(ApplicationDbContext db, int postId, int applicant)
    {
        var service = Service(db); var r = await service.ApplyAsync(applicant, postId, new() { Message = "Xin ở ghép" });
        await service.DecideAsync(2, r.Id, false, new() { Approve = true }); return r.Id;
    }
    private static async Task Error(int status, Func<Task> action) => Assert.Equal(status, (await Assert.ThrowsAsync<BusinessRuleException>(action)).StatusCode);

    [Fact]
    public async Task Happy_path_full_leave_new_post_and_contract_end_preserve_financial_snapshots()
    {
        await using var db = await Fixture(); var service = Service(db);
        var p = await service.CreateAsync(2, Draft()); Assert.Equal(0, p.Status); Assert.Equal(1, p.CurrentOccupants);
        Assert.Empty((await service.SearchAsync(null, 1, 12)).Items);
        await service.ModerateAsync(1, p.Id, new() { Approve = true });
        var published = Assert.Single((await service.SearchAsync(null, 1, 12)).Items);
        Assert.Equal(p.Id, published.Id);
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.FindAsync(1))!.Status);
        Assert.Empty((await new PostBLL(db).SearchPageAsync(new())).Items);
        var publicPost = await service.PublicDetailAsync(p.Id); Assert.Null(publicPost.ContractId); Assert.Equal(2, publicPost.AvailableSlots);
        var b = await AcceptedAsync(db, p.Id, 3); await service.DecideAsync(1, b, true, new() { Approve = true });
        Assert.Equal(2, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
        var c = await AcceptedAsync(db, p.Id, 4); await service.DecideAsync(1, c, true, new() { Approve = true });
        Assert.Equal(3, (await db.Rooms.FindAsync(1))!.CurrentOccupants); Assert.Equal(RoommatePostStatus.Full, (await db.RoommatePosts.FindAsync(p.Id))!.Status);
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.FindAsync(1))!.Status); Assert.Empty((await service.SearchAsync(null, 1, 12)).Items);
        await Error(409, () => service.ApplyAsync(5, p.Id, new()));
        var o = await db.ContractOccupants.SingleAsync(x => x.TenantAccountId == 3);
        await Error(403, () => service.LeaveAsync(5, o.Id));
        await service.LeaveAsync(2, o.Id); Assert.Equal(2, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
        await Error(409, () => service.LeaveAsync(2, o.Id)); Assert.Equal(RoommatePostStatus.Full, (await db.RoommatePosts.FindAsync(p.Id))!.Status);
        var next = await service.CreateAsync(2, Draft(1)); Assert.Equal(0, next.Status);
        await service.ModerateAsync(1, next.Id, new() { Approve = true });
        await service.ApplyAsync(5, next.Id, new());
        var contract = (await db.RentalContracts.FindAsync(1))!;
        Assert.Equal(new decimal[] { 3000000, 1000000, 3500, 20000, 150000 }, new[] { contract.MonthlyRent, contract.DepositAmount, contract.ElectricityPrice, contract.WaterPrice, contract.ServiceFee });
        Assert.Empty(await db.Deposits.ToListAsync()); Assert.Empty(await db.MonthlyBills.ToListAsync()); Assert.Empty(await db.PaymentTransactions.ToListAsync());
        await ContractClosureTests.End(db, new ContractClosureTests.Clock());
        Assert.Equal(2, (await db.Rooms.FindAsync(1))!.CurrentOccupants); Assert.Equal(RoomStatus.Rented, (await db.Rooms.FindAsync(1))!.Status);
        Assert.Equal(ContractOccupantStatus.Active, (await db.ContractOccupants.SingleAsync(x => x.TenantAccountId == 4)).Status);
        Assert.Equal(ContractOccupantStatus.Left, (await db.ContractOccupants.SingleAsync(x => x.TenantAccountId == 3)).Status);
        Assert.All(await db.RoommatePosts.ToListAsync(), x => Assert.Equal(4, x.Status));
        Assert.Equal(4, (await db.RoommateRequests.SingleAsync(r => r.ApplicantTenantAccountId == 5)).Status);
        await service.ReconcileLifecycleAsync(); await service.ReconcileLifecycleAsync(); Assert.Equal(2, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
    }
    [Theory]
    [InlineData(0)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task Non_active_contract_cannot_publish(int status)
    {
        await using var db = await Fixture(); (await db.RentalContracts.FindAsync(1))!.Status = status; await db.SaveChangesAsync();
        await Error(409, () => Service(db).CreateAsync(2, Draft())); Assert.Empty(await db.RoommatePosts.ToListAsync());
    }
    [Theory]
    [InlineData(0)] [InlineData(2)] [InlineData(3)]
    public async Task Non_rented_room_cannot_publish(int status)
    {
        await using var db = await Fixture(); (await db.Rooms.FindAsync(1))!.Status = (RoomStatus)status; await db.SaveChangesAsync();
        await Error(409, () => Service(db).CreateAsync(2, Draft()));
    }
    [Theory]
    [InlineData(0, 400)] [InlineData(3, 409)] [InlineData(-1, 400)]
    public async Task Requested_slots_are_validated(int slots, int status)
    { await using var db = await Fixture(); await Error(status, () => Service(db).CreateAsync(2, Draft(slots))); }
    [Fact]
    public async Task Ownership_duplicate_and_state_guards()
    {
        await using var db = await Fixture(); var s = Service(db);
        await Error(403, () => s.CreateAsync(7, Draft())); await Error(403, () => s.CreateAsync(8, Draft()));
        var p = await s.CreateAsync(2, Draft()); await Error(409, () => s.CreateAsync(2, Draft()));
        await Error(403, () => s.UpdateAsync(7, p.Id, Draft())); await Error(403, () => s.ModerateAsync(6, p.Id, new() { Approve = true }));
        await Error(403, () => s.ModerateAsync(8, p.Id, new() { Approve = true }));
        await Error(409, () => s.ApplyAsync(3, p.Id, new()));
        await s.ModerateAsync(1, p.Id, new() { Approve = true }); await Error(403, () => s.ApplyAsync(2, p.Id, new()));
        var r = await s.ApplyAsync(3, p.Id, new()); await Error(409, () => s.ApplyAsync(3, p.Id, new()));
        await Error(403, () => s.DecideAsync(3, r.Id, false, new() { Approve = true }));
        await Error(409, () => s.DecideAsync(1, r.Id, true, new() { Approve = true }));
        await s.DecideAsync(2, r.Id, false, new() { Approve = true });
        await Error(403, () => s.DecideAsync(6, r.Id, true, new() { Approve = true }));
        await Error(403, () => s.RequestsAsync(5, p.Id)); await Error(403, () => s.ContractAsync(5, 1));
        await s.DecideAsync(1, r.Id, true, new() { Approve = true });
        await Error(409, () => s.ApplyAsync(3, p.Id, new())); Assert.Empty(await db.TenantProfiles.ToListAsync()); Assert.Single(await db.LandlordProfiles.ToListAsync());
    }
    [Fact]
    public async Task Expired_date_blocks_apply_and_approval_before_worker_runs()
    {
        await using var db = await Fixture(); var p = await OpenAsync(db); var b = await AcceptedAsync(db, p, 4);
        await Service(db).DecideAsync(1, b, true, new() { Approve = true });
        var r = await AcceptedAsync(db, p, 3);
        (await db.RentalContracts.FindAsync(1))!.EndDate = DateTime.Today.AddDays(-2); await db.SaveChangesAsync();
        await Error(409, () => Service(db).ApplyAsync(4, p, new())); await Error(409, () => Service(db).DecideAsync(1, r, true, new() { Approve = true }));
        Assert.Empty((await Service(db).SearchAsync(null, 1, 12)).Items);
        await Contracts(db).ReconcileContractLifecycleAsync(72);
        Assert.Equal(5, (await db.RoommatePosts.FindAsync(p))!.Status); Assert.Equal(4, (await db.RoommateRequests.FindAsync(r))!.Status); Assert.Equal(2, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
        Assert.Equal(ContractOccupantStatus.Active, (await db.ContractOccupants.SingleAsync()).Status);
    }
    [Fact]
    public async Task Pending_start_counts_zero_then_activation_and_retries_count_one()
    {
        await using var db = await Fixture(); var c = (await db.RentalContracts.FindAsync(1))!;
        c.Status = RentalContractStatus.PendingStart; c.StartDate = DateTime.Today.AddDays(2); (await db.Rooms.FindAsync(1))!.Status = RoomStatus.Reserved; await db.SaveChangesAsync();
        var s = Service(db); await s.ReconcileLifecycleAsync(); Assert.Equal(0, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
        c.StartDate = DateTime.Today.AddDays(-1); await db.SaveChangesAsync(); await Contracts(db).ReconcileContractLifecycleAsync(72);
        await s.ReconcileLifecycleAsync(); await s.ReconcileLifecycleAsync();
        Assert.Equal(1, (await db.Rooms.FindAsync(1))!.CurrentOccupants); Assert.Equal(RoomStatus.Rented, (await db.Rooms.FindAsync(1))!.Status);
    }
    [Fact]
    public async Task Editing_requeues_moderation_and_cancels_stale_applications()
    {
        await using var db = await Fixture(); var p = await OpenAsync(db); var r = await AcceptedAsync(db, p, 3);
        var d = Draft(); d.Description = "Điều kiện mới"; var updated = await Service(db).UpdateAsync(2, p, d);
        Assert.Equal(0, updated.Status); Assert.Equal(4, (await db.RoommateRequests.FindAsync(r))!.Status);
        Assert.Empty((await Service(db).SearchAsync(null, 1, 12)).Items);
    }
    [Fact]
    public async Task Requested_target_can_fill_before_room_capacity_and_close_cancels_pending()
    {
        await using var db = await Fixture(); var p = await OpenAsync(db, 1); var a = await AcceptedAsync(db, p, 3);
        var other = await Service(db).ApplyAsync(4, p, new());
        await Service(db).DecideAsync(1, a, true, new() { Approve = true }); Assert.Equal(3, (await db.RoommatePosts.FindAsync(p))!.Status);
        Assert.Equal(4, (await db.RoommateRequests.FindAsync(other.Id))!.Status);
        Assert.Equal(2, (await db.Rooms.FindAsync(1))!.CurrentOccupants); await Service(db).CloseAsync(2, p);
        Assert.Equal(4, (await db.RoommateRequests.FindAsync(other.Id))!.Status);
    }
    [Theory]
    [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task Non_open_post_cannot_receive_applications(int status)
    {
        await using var db = await Fixture(); var p = await OpenAsync(db);
        (await db.RoommatePosts.FindAsync(p))!.Status = status; await db.SaveChangesAsync();
        await Error(409, () => Service(db).ApplyAsync(3, p, new()));
    }
    [Theory]
    [InlineData("title")] [InlineData("description")] [InlineData("share")] [InlineData("requirements")]
    public async Task Invalid_post_fields_do_not_persist(string field)
    {
        await using var db = await Fixture(); var d = Draft();
        if (field == "title") d.Title = " ";
        if (field == "description") d.Description = new string('x', 4001);
        if (field == "share") d.EstimatedShare = -1;
        if (field == "requirements") d.Requirements = new string('x', 2001);
        await Error(400, () => Service(db).CreateAsync(2, d)); Assert.Empty(await db.RoommatePosts.ToListAsync());
    }
}

[Collection("SqlServerWorkflow")]
public class RoommateSqlServerTests
{
    [SqlServerFact]
    public async Task Sql_pending_start_activation_expiration_and_worker_retries_reconcile_residents()
    {
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", b.InitialCatalog); b.InitialCatalog += "_RoommatesLifecycle";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(b.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options); await db.Database.EnsureCreatedAsync(); await RoommateTests.SeedAsync(db);
        var c = (await db.RentalContracts.FindAsync(1))!; var room = (await db.Rooms.FindAsync(1))!;
        c.Status = RentalContractStatus.PendingStart; c.StartDate = DateTime.Today.AddDays(2); room.Status = RoomStatus.Reserved; await db.SaveChangesAsync();
        var service = RoommateTests.Service(db); var lifecycle = RoommateTests.Contracts(db);
        await lifecycle.ReconcileContractLifecycleAsync(72); await service.ReconcileLifecycleAsync(); Assert.Equal(0, room.CurrentOccupants);
        c.StartDate = DateTime.Today.AddDays(-2); await db.SaveChangesAsync();
        await lifecycle.ReconcileContractLifecycleAsync(72); await service.ReconcileLifecycleAsync();
        Assert.Equal(1, room.CurrentOccupants); Assert.Equal(RentalContractStatus.Active, c.Status);
        var p = await RoommateTests.OpenAsync(db); var r = await RoommateTests.AcceptedAsync(db, p, 3);
        await service.DecideAsync(1, r, true, new() { Approve = true }); await RoommateTests.AcceptedAsync(db, p, 4);
        await service.ReconcileLifecycleAsync(); await service.ReconcileLifecycleAsync(); Assert.Equal(2, room.CurrentOccupants);
        Assert.Empty((await new PostBLL(db).SearchPageAsync(new())).Items);
        Assert.Single((await service.SearchAsync(null, 1, 12)).Items);
        c.EndDate = DateTime.Today.AddDays(-1); await db.SaveChangesAsync();
        await lifecycle.ReconcileContractLifecycleAsync(72); await service.ReconcileLifecycleAsync();
        await lifecycle.ReconcileContractLifecycleAsync(72); await service.ReconcileLifecycleAsync();
        Assert.Equal(2, room.CurrentOccupants); Assert.Equal(RoomStatus.Rented, room.Status);
        Assert.Equal(RoommatePostStatus.Expired, (await db.RoommatePosts.FindAsync(p))!.Status);
        Assert.All(await db.ContractOccupants.ToListAsync(), o => { Assert.Equal(ContractOccupantStatus.Active, o.Status); Assert.Null(o.LeftAt); });
        Assert.Equal(RoommateRequestStatus.Cancelled, (await db.RoommateRequests.SingleAsync(x => x.ApplicantTenantAccountId == 4)).Status);
    }
    [SqlServerFact]
    public async Task Last_slot_concurrency_database_uniqueness_and_occupancy_are_atomic()
    {
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", b.InitialCatalog); b.InitialCatalog += "_Roommates";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(b.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options); await db.Database.EnsureCreatedAsync();
        await RoommateTests.SeedAsync(db); var p = await RoommateTests.OpenAsync(db);
        var first = await RoommateTests.AcceptedAsync(db, p, 3); await RoommateTests.Service(db).DecideAsync(1, first, true, new() { Approve = true });
        var a = await RoommateTests.AcceptedAsync(db, p, 4); var c = await RoommateTests.AcceptedAsync(db, p, 5);
        async Task<int> Approve(int id)
        {
            await using var context = new ApplicationDbContext(options);
            try { await RoommateTests.Service(context).DecideAsync(1, id, true, new() { Approve = true }); return 200; }
            catch (BusinessRuleException e) { return e.StatusCode; }
        }
        Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(Approve(a), Approve(c))).Order().ToArray());
        db.ChangeTracker.Clear(); Assert.Equal(3, (await db.Rooms.FindAsync(1))!.CurrentOccupants); Assert.Equal(2, await db.ContractOccupants.CountAsync(o => o.Status == 0));
        Assert.Equal(RoomStatus.Rented, (await db.Rooms.FindAsync(1))!.Status);
        var member = await db.ContractOccupants.FirstAsync();
        db.ContractOccupants.Add(new ContractOccupant { ContractId = 1, TenantAccountId = member.TenantAccountId });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
        db.RoommatePosts.Add(new RoommatePost { ContractId = 1, CreatorTenantAccountId = 2, Title = "New", Description = "Test", RequestedSlots = 1 }); await db.SaveChangesAsync();
        db.RoommatePosts.Add(new RoommatePost { ContractId = 1, CreatorTenantAccountId = 2, Title = "Duplicate", Description = "Test", RequestedSlots = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
        var pendingPost = await db.RoommatePosts.SingleAsync(p => p.Status == RoommatePostStatus.PendingLandlordApproval);
        db.RoommateRequests.Add(new RoommateRequest { RoommatePostId = pendingPost.Id, ApplicantTenantAccountId = 7 }); await db.SaveChangesAsync();
        db.RoommateRequests.Add(new RoommateRequest { RoommatePostId = pendingPost.Id, ApplicantTenantAccountId = 7 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
        await RoommateTests.Service(db).ReconcileLifecycleAsync(); await RoommateTests.Service(db).ReconcileLifecycleAsync(); Assert.Equal(3, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
        await ContractClosureTests.End(db, new ContractClosureTests.Clock()); Assert.Equal(3, (await db.Rooms.FindAsync(1))!.CurrentOccupants);
        Assert.All(await db.ContractOccupants.ToListAsync(), o => Assert.Equal(0, o.Status));
    }
}
