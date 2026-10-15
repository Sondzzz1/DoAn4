using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class ContractReadSafetyTests
{
    [Theory]
    [InlineData(RentalContractStatus.Active, 1, false)]
    [InlineData(RentalContractStatus.Active, 2, false)]
    [InlineData(RentalContractStatus.Active, 1, true)]
    [InlineData(RentalContractStatus.Active, 2, true)]
    [InlineData(RentalContractStatus.PendingStart, 1, false)]
    [InlineData(RentalContractStatus.PendingStart, 2, false)]
    [InlineData(RentalContractStatus.PendingStart, 1, true)]
    [InlineData(RentalContractStatus.PendingStart, 2, true)]
    public async Task Contract_reads_do_not_run_lifecycle_or_change_possession_and_finances(int status, int actor, bool detail)
    {
        await using var db = PostBLLTests.CreateContext();
        await SeedAsync(db, status);
        var before = await SnapshotAsync(db);
        var service = Service(db);

        var result = detail ? await service.LayChiTietAsync(actor, 1) : Assert.Single(await service.LayCuaToiAsync(actor));

        Assert.Equal(status, result.TrangThai);
        Assert.Equal(before, await SnapshotAsync(db));
    }

    [Theory]
    [InlineData(3, 1, 403)]
    [InlineData(6, 1, 403)]
    [InlineData(2, 999, 404)]
    public async Task Denied_or_missing_contract_reads_do_not_mutate_other_contracts(int actor, int id, int statusCode)
    {
        await using var db = PostBLLTests.CreateContext();
        await SeedAsync(db, RentalContractStatus.Active);
        var before = await SnapshotAsync(db);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => Service(db).LayChiTietAsync(actor, id));

        Assert.Equal(statusCode, error.StatusCode);
        Assert.Equal(before, await SnapshotAsync(db));
    }

    internal static RentalContractBLL Service(ApplicationDbContext db) =>
        new(db, null!, NullLogger<RentalContractBLL>.Instance,
            new FixedClock(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)));

    internal static async Task SeedAsync(ApplicationDbContext db, int status)
    {
        await RoommateTests.SeedAsync(db);
        var contract = await db.RentalContracts.SingleAsync();
        contract.Status = status;
        contract.StartDate = new DateTime(2026, 10, 1);
        contract.EndDate = status == RentalContractStatus.Active ? new DateTime(2026, 10, 31) : new DateTime(2026, 12, 31);
        var room = await db.Rooms.SingleAsync();
        room.Status = status == RentalContractStatus.Active ? RoomStatus.Rented : RoomStatus.Reserved;
        room.CurrentOccupants = status == RentalContractStatus.Active ? 2 : 0;
        db.ContractOccupants.Add(new ContractOccupant { ContractId = 1, TenantAccountId = 3, JoinedAt = new DateTime(2026, 10, 2) });
        db.RoommatePosts.Add(new RoommatePost { ContractId = 1, CreatorTenantAccountId = 2, Title = "Contract read QA", Description = "QA", RequestedSlots = 1, Status = RoommatePostStatus.Open });
        var deposit = new Deposit { RentalRequestId = 1, TenantAccountId = 2, LandlordAccountId = 1, Amount = 1000000,
            Status = DepositStatus.Confirmed, PaidAt = new DateTime(2026, 9, 30) };
        db.Deposits.Add(deposit);
        db.MonthlyBills.Add(new MonthlyBill { ContractId = 1, Month = 10, Year = 2026, TotalAmount = 3000000,
            Status = MonthlyBillStatus.Unpaid, DueDate = new DateTime(2026, 10, 10) });
        await db.SaveChangesAsync();
        db.PaymentTransactions.Add(new PaymentTransaction { DepositId = deposit.Id, TargetType = "Deposit", OrderId = "contract-read-paid",
            TransactionCode = "contract-read-transaction", Amount = deposit.Amount, Status = PaymentTransactionStatus.Succeeded,
            ProcessedAt = deposit.PaidAt });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    internal static async Task<string> SnapshotAsync(ApplicationDbContext db) => JsonSerializer.Serialize(new
    {
        Contracts = await db.RentalContracts.AsNoTracking().OrderBy(c => c.Id).Select(c => new { c.Id, c.Status, c.UpdatedAt, c.StartDate, c.EndDate, c.MonthlyRent, c.DepositAmount, c.TenantConfirmed, c.LandlordConfirmed }).ToListAsync(),
        Rooms = await db.Rooms.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Id, r.Status, r.CurrentOccupants, r.UpdatedAt }).ToListAsync(),
        Requests = await db.RentalRequests.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Id, r.Status, r.UpdatedAt }).ToListAsync(),
        Occupants = await db.ContractOccupants.AsNoTracking().OrderBy(o => o.Id).Select(o => new { o.Id, o.Status, o.LeftAt }).ToListAsync(),
        RoommatePosts = await db.RoommatePosts.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.Status, p.ClosedAt }).ToListAsync(),
        Deposits = await db.Deposits.AsNoTracking().OrderBy(d => d.Id).Select(d => new { d.Id, d.Status, d.Amount, d.PaidAt, d.UpdatedAt }).ToListAsync(),
        Bills = await db.MonthlyBills.AsNoTracking().OrderBy(b => b.Id).Select(b => new { b.Id, b.Status, b.TotalAmount, b.PaidAt, b.UpdatedAt }).ToListAsync(),
        Payments = await db.PaymentTransactions.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.Status, p.Amount, p.TransactionCode, p.ProcessedAt }).ToListAsync()
    });

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

[Collection("SqlServerWorkflow")]
public class ContractReadSafetySqlTests
{
    [SqlServerFact]
    public async Task Sql_contract_reads_are_read_only_even_when_expired_unpaid_and_occupied()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        builder.InitialCatalog += "_ContractReads";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await ContractReadSafetyTests.SeedAsync(db, RentalContractStatus.Active);
        var before = await ContractReadSafetyTests.SnapshotAsync(db);

        await using (var reader = new ApplicationDbContext(options))
        {
            var service = ContractReadSafetyTests.Service(reader);
            Assert.Single(await service.LayCuaToiAsync(1));
            Assert.Single(await service.LayCuaToiAsync(2));
            Assert.Empty(await service.LayCuaToiAsync(3));
            await service.LayChiTietAsync(1, 1);
            await service.LayChiTietAsync(2, 1);
            Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.LayChiTietAsync(6, 1))).StatusCode);
            Assert.Equal(404, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.LayChiTietAsync(2, 999))).StatusCode);
        }

        Assert.Equal(before, await ContractReadSafetyTests.SnapshotAsync(db));
    }
}
