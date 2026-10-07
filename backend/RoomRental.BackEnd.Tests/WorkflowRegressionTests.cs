using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using RoomRental.BackEnd.Services;
using System.Security.Claims;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class WorkflowRegressionTests
{
    internal static async Task SeedWorkflowAsync(ApplicationDbContext db)
    {
        await PostBLLTests.SeedPostsAsync(db);
        db.Users.Add(new User { Id = 10, UserName = "tenant", Email = "tenant@test", FullName = "Tenant", PasswordHash = "test", RoleId = 1 });
        db.TenantProfiles.Add(new TenantProfile { Id = 10, AccountId = 10 });
        db.RentalRequests.Add(new RentalRequest { Id = 1, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1, Status = RentalRequestStatus.Approved });
        db.Deposits.Add(new Deposit { Id = 1, RentalRequestId = 1, TenantAccountId = 10, LandlordAccountId = 1, Amount = 1_000_000, DueAt = DateTime.Now.AddDays(2), Status = DepositStatus.Confirmed });
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Reserved;
        await db.SaveChangesAsync();
    }
    private static RentalContractBLL Contracts(ApplicationDbContext db) => new(db, null!, NullLogger<RentalContractBLL>.Instance);
    private static MonthlyBillBLL Bills(ApplicationDbContext db) => new(db, null!, NullLogger<MonthlyBillBLL>.Instance);
    private static PaymentBLL Payments(ApplicationDbContext db) => new(db, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["VnPay:TmnCode"] = "TEST", ["VnPay:HashSecret"] = "test-only-secret", ["VnPay:BaseUrl"] = "https://example.test/pay", ["VnPay:ReturnUrl"] = "https://example.test/return"
    }).Build(), null!, NullLogger<PaymentBLL>.Instance);
    private static RentalContract AddContract(ApplicationDbContext db, int status, DateTime start, DateTime end)
    {
        var contract = new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1, Status = status,
            StartDate = start, EndDate = end, MonthlyRent = 3_000_000, ElectricityPrice = 3000, WaterPrice = 10000, ServiceFee = 50000 };
        db.RentalContracts.Add(contract);
        db.RentalRequests.Single(r => r.Id == 1).Status = RentalRequestStatus.ConvertedToContract;
        return contract;
    }

    [Fact]
    public async Task Radius_excludes_missing_and_outside_coordinates_and_counts_before_paging()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        var room = db.Rooms.Single(r => r.Id == 1);
        room.Latitude = 21; room.Longitude = 105;
        db.Rooms.Single(r => r.Id == 2).Status = RoomStatus.Available;
        await db.SaveChangesAsync();
        var result = await new PostBLL(db).SearchPageAsync(new PostQueryParameters { Latitude = 21, Longitude = 105, RadiusInKm = 1, PageSize = 1 });
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, Assert.Single(result.Items).Id);
        room.Latitude = 22; await db.SaveChangesAsync();
        Assert.Equal(0, (await new PostBLL(db).SearchPageAsync(new PostQueryParameters { Latitude = 21, Longitude = 105, RadiusInKm = 1 })).TotalCount);
    }

    [Fact]
    public async Task Pagination_returns_real_count_and_empty_page_keeps_total()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Rooms.Single(r => r.Id == 2).Status = RoomStatus.Available;
        await db.SaveChangesAsync();
        var service = new PostBLL(db);
        var result = await service.SearchPageAsync(new PostQueryParameters { PageSize = 1, PageNumber = 2 });
        Assert.Single(result.Items); Assert.Equal(2, result.TotalCount); Assert.Equal(2, result.TotalPages);
        Assert.Empty((await service.SearchPageAsync(new PostQueryParameters { PageNumber = 3, PageSize = 1 })).Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deposit_expiry_is_idempotent_and_does_not_release_effective_contract(bool activeContract)
    {
        await using var db = PostBLLTests.CreateContext();
        await SeedWorkflowAsync(db);
        var deposit = db.Deposits.Single();
        deposit.Status = DepositStatus.Pending; deposit.DueAt = DateTime.Now.AddMinutes(-1);
        if (activeContract) AddContract(db, RentalContractStatus.Active, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(10));
        // Includes an inconsistent legacy Reserved room with an Active contract.
        db.RentalRequests.Single().Status = RentalRequestStatus.Approved;
        await db.SaveChangesAsync();
        var service = new DepositBLL(db, null!);
        await service.ReconcileExpiredDepositsAsync(); await service.ReconcileExpiredDepositsAsync();
        Assert.Equal(DepositStatus.Expired, deposit.Status);
        Assert.Equal(activeContract ? RoomStatus.Reserved : RoomStatus.Available, db.Rooms.Single(r => r.Id == 1).Status);
    }

    [Fact]
    public async Task Future_contract_waits_then_activates_and_overlap_is_blocked()
    {
        await using var db = PostBLLTests.CreateContext();
        await SeedWorkflowAsync(db);
        var service = Contracts(db);
        var dto = await service.TaoAsync(1, new TaoHopDongDto { YeuCauThueId = 1, NgayBatDau = DateTime.Now.AddDays(1), NgayKetThuc = DateTime.Now.AddMonths(1), TienThueHangThang = 3_000_000 });
        await service.XacNhanAsync(1, dto.Id); await service.XacNhanAsync(10, dto.Id);
        var contract = db.RentalContracts.Single();
        Assert.Equal(RentalContractStatus.PendingStart, contract.Status);
        Assert.Equal(RoomStatus.Reserved, db.Rooms.Single(r => r.Id == 1).Status);
        db.RentalRequests.Add(new RentalRequest { Id = 2, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1, Status = RentalRequestStatus.Approved });
        db.Deposits.Add(new Deposit { Id = 2, RentalRequestId = 2, Status = DepositStatus.Confirmed, Amount = 1000000 });
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, new TaoHopDongDto { YeuCauThueId = 2, NgayBatDau = contract.StartDate, NgayKetThuc = contract.EndDate, TienThueHangThang = 3000000 }))).StatusCode);
        contract.StartDate = DateTime.Now.AddMinutes(-1); await db.SaveChangesAsync();
        await service.ReconcileContractLifecycleAsync(72);
        Assert.Equal(RentalContractStatus.Active, contract.Status);
        Assert.Equal(RoomStatus.Rented, db.Rooms.Single(r => r.Id == 1).Status);
    }

    [Fact]
    public async Task Contract_creation_requires_reserved_room()
    {
        await using var db = PostBLLTests.CreateContext(); await SeedWorkflowAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Available; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Contracts(db).TaoAsync(1, new TaoHopDongDto { YeuCauThueId = 1, NgayBatDau = DateTime.Now, NgayKetThuc = DateTime.Now.AddMonths(1), TienThueHangThang = 3000000 }));
    }

    [Fact]
    public async Task Bills_enforce_period_meter_continuity_snapshots_and_paid_immutability()
    {
        await using var db = PostBLLTests.CreateContext(); await SeedWorkflowAsync(db);
        AddContract(db, RentalContractStatus.Active, new DateTime(2026, 10, 1), new DateTime(2027, 3, 31)); await db.SaveChangesAsync();
        var service = Bills(db);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, new TaoHoaDonDto { HopDongId = 1, Thang = 1, Nam = 2025 }));
        var first = await service.TaoAsync(1, new TaoHoaDonDto { HopDongId = 1, Thang = 10, Nam = 2026, SoDienCu = 100, SoDienMoi = 150, SoNuocCu = 10, SoNuocMoi = 20, GiaDien = 99999 });
        var second = await service.TaoAsync(1, new TaoHoaDonDto { HopDongId = 1, Thang = 11, Nam = 2026, SoDienCu = 0, SoDienMoi = 180, SoNuocCu = 0, SoNuocMoi = 25 });
        Assert.Equal(150, second.SoDienCu); Assert.Equal(20, second.SoNuocCu); Assert.Equal(3000, second.GiaDien);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CapNhatAsync(1, first.Id, new CapNhatHoaDonDto { SoDienMoi = 160, SoNuocMoi = 20 }));
        await service.ThanhToanAsync(1, second.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CapNhatAsync(1, second.Id, new CapNhatHoaDonDto()));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.XoaAsync(1, second.Id));
    }

    [Fact]
    public async Task Payment_timeout_allows_retry_without_expiring_deposit_and_callback_is_idempotent()
    {
        await using var db = PostBLLTests.CreateContext(); await SeedWorkflowAsync(db);
        db.Deposits.Single().Status = DepositStatus.Pending;
        db.PaymentTransactions.Add(new PaymentTransaction { Id = 1, DepositId = 1, TargetType = "Deposit", OrderId = "stale", Amount = 1000000, Status = PaymentTransactionStatus.Pending, CreatedAt = DateTime.Now.AddMinutes(-31) });
        await db.SaveChangesAsync();
        var service = Payments(db);
        var attempt = await service.CreatePaymentUrlAsync(10, new CreatePaymentRequestDto { DepositId = 1 }, "127.0.0.1");
        Assert.Equal(PaymentTransactionStatus.Expired, db.PaymentTransactions.Single(p => p.Id == 1).Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
        var signer = new VnPayHelper();
        signer.AddRequestData("vnp_TxnRef", attempt.OrderId); signer.AddRequestData("vnp_Amount", "100000000");
        signer.AddRequestData("vnp_ResponseCode", "00"); signer.AddRequestData("vnp_TransactionStatus", "00"); signer.AddRequestData("vnp_TransactionNo", "txn-test");
        var query = new QueryCollection(QueryHelpers.ParseQuery(new Uri(signer.CreateRequestUrl("https://example.test/return", "test-only-secret")).Query));
        Assert.True((await service.ProcessPaymentReturnAsync(query)).Success);
        var paidAt = db.Deposits.Single().PaidAt;
        Assert.True((await service.ProcessPaymentReturnAsync(query)).Success);
        Assert.Equal(paidAt, db.Deposits.Single().PaidAt);
        Assert.Single(db.PaymentTransactions.Where(p => p.Status == PaymentTransactionStatus.Succeeded));
    }

    [Fact]
    public async Task Monthly_bill_attempt_timeout_restores_overdue_and_worker_reconciles_unpaid()
    {
        await using var db = PostBLLTests.CreateContext(); await SeedWorkflowAsync(db);
        var contract = AddContract(db, RentalContractStatus.Active, DateTime.Now.AddMonths(-1), DateTime.Now.AddMonths(2));
        db.MonthlyBills.Add(new MonthlyBill { Id = 1, Contract = contract, ContractId = 1, Status = MonthlyBillStatus.PendingPayment, DueDate = DateTime.Now.AddDays(-1) });
        db.PaymentTransactions.Add(new PaymentTransaction { MonthlyBillId = 1, TargetType = "MonthlyBill", OrderId = "bill-stale", Status = PaymentTransactionStatus.Pending, CreatedAt = DateTime.Now.AddMinutes(-31) });
        await db.SaveChangesAsync(); await Payments(db).ReconcilePendingPaymentsAsync();
        Assert.Equal(MonthlyBillStatus.Overdue, db.MonthlyBills.Single().Status);
        db.MonthlyBills.Single().Status = MonthlyBillStatus.Unpaid; await db.SaveChangesAsync();
        await Bills(db).ReconcileOverdueAsync();
        Assert.Equal(MonthlyBillStatus.Overdue, db.MonthlyBills.Single().Status);
    }

    [Fact]
    public async Task Blocked_account_is_rejected_by_actual_JWT_validation_event()
    {
        await using var db = PostBLLTests.CreateContext(); await SeedWorkflowAsync(db);
        using var provider = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider };
        TokenValidatedContext Context() => new(http, new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions()) {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("UserId", "10"), new Claim(ClaimTypes.Role, "Tenant") }, "Bearer"))
        };
        var accepted = Context(); await AccountTokenValidation.ValidateAsync(accepted); Assert.Null(accepted.Result?.Failure);
        db.Users.Single(u => u.Id == 10).IsActive = false; await db.SaveChangesAsync();
        var blocked = Context(); await AccountTokenValidation.ValidateAsync(blocked); Assert.NotNull(blocked.Result?.Failure);
    }

    [Fact]
    public async Task Appointment_cannot_complete_before_scheduled_time_or_confirm_unavailable_room()
    {
        await using var db = PostBLLTests.CreateContext(); await SeedWorkflowAsync(db);
        db.ViewingAppointments.Add(new ViewingAppointment { Id = 1, PostId = 1, LandlordId = 1, TenantId = 10, ScheduledAt = DateTime.Now.AddDays(1), Status = AppointmentStatus.Confirmed });
        await db.SaveChangesAsync(); var service = new ViewingAppointmentBLL(db);
        db.Posts.Single(p => p.Id == 1).DisplayPrice = 1;
        await db.SaveChangesAsync();
        Assert.Equal(3_000_000m, Assert.Single(await service.GetLandlordAppointmentsAsync(1)).PostPrice);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.CompleteAppointmentAsync(1, 1))).StatusCode);
        db.ViewingAppointments.Single().Status = AppointmentStatus.Pending; await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConfirmAppointmentAsync(1, 1))).StatusCode);
    }
}
