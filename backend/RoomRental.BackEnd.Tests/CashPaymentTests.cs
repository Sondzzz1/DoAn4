using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.Controllers;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class CashPaymentTests
{
    private static CashPaymentBLL Cash(ApplicationDbContext db) => new(db, null!, NullLogger<CashPaymentBLL>.Instance);
    private static CashPaymentSelectDto Target(bool bill) => bill ? new() { MonthlyBillId = 1 } : new() { DepositId = 1 };

    private static async Task<ApplicationDbContext> Fixture(bool bill = false)
    {
        var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Users.AddRange(
            new User { Id = 30, UserName = "cash-admin", Email = "admin@cash.test", FullName = "Admin", PasswordHash = "test", RoleId = 0 },
            new User { Id = 40, UserName = "cash-other", Email = "other@cash.test", FullName = "Other tenant", PasswordHash = "test", RoleId = 1 },
            new User { Id = 50, UserName = "cash-other-owner", Email = "owner@cash.test", FullName = "Other owner", PasswordHash = "test", RoleId = 2 });
        var deposit = await db.Deposits.SingleAsync();
        deposit.Status = bill ? DepositStatus.Confirmed : DepositStatus.Pending;
        if (bill)
        {
            var contract = new RentalContract
            {
                Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1,
                StartDate = DateTime.Today.AddMonths(-1), EndDate = DateTime.Today.AddMonths(1),
                MonthlyRent = 3_000_000, DepositAmount = deposit.Amount, Status = RentalContractStatus.Active
            };
            db.RentalContracts.Add(contract);
            db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
            db.MonthlyBills.Add(new MonthlyBill
            {
                Id = 1, Contract = contract, Month = DateTime.Today.Month, Year = DateTime.Today.Year,
                RoomPrice = 1_000_000, TotalAmount = 1_000_000, Status = MonthlyBillStatus.Unpaid,
                DueDate = DateTime.Now.AddDays(3)
            });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Choosing_cash_is_idempotent_and_does_not_pay_or_extend_the_deadline(bool bill)
    {
        await using var db = await Fixture(bill);
        var deadline = bill ? db.MonthlyBills.Single().DueDate : db.Deposits.Single().DueAt;
        var first = await Cash(db).SelectAsync(10, Target(bill));
        var duplicate = await Cash(db).SelectAsync(10, Target(bill));

        Assert.Equal(first.Id, duplicate.Id);
        Assert.Single(db.CashPaymentReceipts);
        Assert.Equal(CashPaymentStatus.Selected, first.Status);
        Assert.Null(first.ReceiptNumber);
        Assert.Null(first.ConfirmedAtUtc);
        Assert.Equal(1_000_000m, first.Amount);
        Assert.Equal(deadline, bill ? db.MonthlyBills.Single().DueDate : db.Deposits.Single().DueAt);
        Assert.Equal(bill ? MonthlyBillStatus.Unpaid : DepositStatus.Pending,
            bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
        Assert.Null(bill ? db.MonthlyBills.Single().PaidAt : db.Deposits.Single().PaidAt);
        Assert.Empty(db.PaymentTransactions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Owner_confirmation_collects_full_amount_once_and_keeps_an_auditable_receipt(bool bill)
    {
        await using var db = await Fixture(bill);
        var selected = await Cash(db).SelectAsync(10, Target(bill));
        var received = await Cash(db).ConfirmAsync(1, selected.Id, new() { Note = "  Đã nhận đủ tiền trực tiếp  " });
        var confirmedAt = received.ConfirmedAtUtc;
        var repeated = await Cash(db).ConfirmAsync(1, selected.Id, new() { Note = "Không được sửa phiếu đã thu" });

        Assert.Equal(CashPaymentStatus.Received, received.Status);
        Assert.Equal(1_000_000m, received.Amount);
        Assert.Equal(confirmedAt, repeated.ConfirmedAtUtc);
        Assert.Equal(received.ReceiptNumber, repeated.ReceiptNumber);
        Assert.False(string.IsNullOrWhiteSpace(received.ReceiptNumber));
        Assert.Equal("Đã nhận đủ tiền trực tiếp", repeated.Note);
        Assert.Equal(1, db.CashPaymentReceipts.Single().ConfirmedByAccountId);
        Assert.Equal(bill ? MonthlyBillStatus.Paid : DepositStatus.Confirmed,
            bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
        Assert.NotNull(bill ? db.MonthlyBills.Single().PaidAt : db.Deposits.Single().PaidAt);
        if (bill) Assert.Equal("Tiền mặt", db.MonthlyBills.Single().PaymentMethod);
        Assert.Single(db.CashPaymentReceipts);
        Assert.Empty(db.PaymentTransactions);
        Assert.Equal(1, Assert.Single(await Cash(db).ListAsync(10)).LandlordAccountId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(40)]
    public async Task Only_the_actual_tenant_can_choose_cash(int actor)
    {
        await using var db = await Fixture();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).SelectAsync(actor, Target(false)));
        Assert.Empty(db.CashPaymentReceipts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(50)]
    public async Task Tenant_admin_and_other_owners_cannot_confirm_receipt(int actor)
    {
        await using var db = await Fixture();
        var selected = await Cash(db).SelectAsync(10, Target(false));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).ConfirmAsync(actor, selected.Id));
        Assert.Equal(CashPaymentStatus.Selected, db.CashPaymentReceipts.Single().Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Fact]
    public async Task Receipt_lists_are_scoped_to_the_participants_or_admin()
    {
        await using var db = await Fixture();
        await Cash(db).SelectAsync(10, Target(false));
        Assert.Single(await Cash(db).ListAsync(10));
        Assert.Single(await Cash(db).ListAsync(1));
        Assert.Single(await Cash(db).ListAsync(30));
        Assert.Empty(await Cash(db).ListAsync(40));
        Assert.Empty(await Cash(db).ListAsync(50));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(1, 1)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    public async Task Selection_requires_exactly_one_positive_target(int? depositId, int? billId)
    {
        await using var db = await Fixture();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).SelectAsync(10,
            new() { DepositId = depositId, MonthlyBillId = billId }));
        Assert.Empty(db.CashPaymentReceipts);
    }

    [Theory]
    [InlineData(DepositStatus.Paid)]
    [InlineData(DepositStatus.Confirmed)]
    [InlineData(DepositStatus.RefundRequested)]
    [InlineData(DepositStatus.Refunded)]
    [InlineData(DepositStatus.Cancelled)]
    [InlineData(DepositStatus.Expired)]
    public async Task Cash_cannot_be_selected_for_a_deposit_that_is_not_pending(int status)
    {
        await using var db = await Fixture();
        db.Deposits.Single().Status = status;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).SelectAsync(10, Target(false)));
        Assert.Empty(db.CashPaymentReceipts);
    }

    [Theory]
    [InlineData(MonthlyBillStatus.Paid)]
    [InlineData(MonthlyBillStatus.Cancelled)]
    [InlineData(MonthlyBillStatus.PendingPayment)]
    public async Task Cash_cannot_be_selected_for_a_paid_cancelled_or_online_pending_bill(int status)
    {
        await using var db = await Fixture(bill: true);
        db.MonthlyBills.Single().Status = status;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).SelectAsync(10, Target(true)));
        Assert.Empty(db.CashPaymentReceipts);
    }

    [Theory]
    [InlineData("deadline")]
    [InlineData("request")]
    [InlineData("room")]
    [InlineData("other-reservation")]
    [InlineData("changed-amount")]
    public async Task Receipt_confirmation_rechecks_deadline_room_entitlement_and_full_amount(string change)
    {
        await using var db = await Fixture();
        var selected = await Cash(db).SelectAsync(10, Target(false));
        var deposit = db.Deposits.Single();
        if (change == "deadline") deposit.DueAt = DateTime.Now.AddMinutes(-1);
        if (change == "request") db.RentalRequests.Single().Status = RentalRequestStatus.Cancelled;
        if (change == "room") db.Rooms.Single(x => x.Id == 1).Status = RoomStatus.Available;
        if (change == "changed-amount") deposit.Amount += 100_000;
        if (change == "other-reservation") db.RentalRequests.Add(new RentalRequest
        {
            Id = 2, PostId = 1, TenantAccountId = 40, LandlordAccountId = 1, Status = RentalRequestStatus.Approved
        });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).ConfirmAsync(1, selected.Id));
        Assert.Equal(CashPaymentStatus.Selected, db.CashPaymentReceipts.Single().Status);
        Assert.Null(deposit.PaidAt);
        Assert.Null(db.CashPaymentReceipts.Single().ReceiptNumber);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Confirmation_rechecks_changed_bill_amount_or_locked_contract_settlement(bool locked)
    {
        await using var db = await Fixture(bill: true);
        var selected = await Cash(db).SelectAsync(10, Target(true));
        if (locked)
        {
            db.ContractClosures.Add(new ContractClosure
            {
                Id = 1, ContractId = 1, RoomId = 1, ReleasedAtUtc = DateTime.UtcNow
            });
        }
        else db.MonthlyBills.Single().TotalAmount += 100_000;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).ConfirmAsync(1, selected.Id));
        Assert.Equal(MonthlyBillStatus.Unpaid, db.MonthlyBills.Single().Status);
        Assert.Equal(CashPaymentStatus.Selected, db.CashPaymentReceipts.Single().Status);
    }

    [Theory]
    [InlineData(false, PaymentTransactionStatus.Pending)]
    [InlineData(false, PaymentTransactionStatus.Succeeded)]
    [InlineData(true, PaymentTransactionStatus.Pending)]
    [InlineData(true, PaymentTransactionStatus.Succeeded)]
    public async Task An_online_attempt_blocks_cash_selection_and_confirmation(bool bill, int status)
    {
        await using var db = await Fixture(bill);
        var selected = await Cash(db).SelectAsync(10, Target(bill));
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            DepositId = bill ? null : 1, MonthlyBillId = bill ? 1 : null,
            TargetType = bill ? "MonthlyBill" : "Deposit", OrderId = "existing-online", Amount = 1_000_000, Status = status
        });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).ConfirmAsync(1, selected.Id));
        await Cash(db).CancelAsync(10, selected.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).SelectAsync(10, Target(bill)));
        Assert.All(db.CashPaymentReceipts, x => Assert.NotEqual(CashPaymentStatus.Received, x.Status));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unresolved_provider_events_block_cash_even_when_online_attempt_has_expired(bool bill)
    {
        await using var db = await Fixture(bill);
        var selected = await Cash(db).SelectAsync(10, Target(bill));
        var payment = new PaymentTransaction
        {
            DepositId = bill ? null : 1, MonthlyBillId = bill ? 1 : null,
            TargetType = bill ? "MonthlyBill" : "Deposit", OrderId = "needs-review", Amount = 1_000_000,
            Status = PaymentTransactionStatus.Expired
        };
        db.PaymentTransactions.Add(payment);
        db.PaymentCallbackEvents.Add(new PaymentCallbackEvent
        {
            PaymentTransaction = payment, ProviderOrderId = payment.OrderId, DeduplicationKey = "cash-review-event",
            ProcessingState = PaymentEventState.RequiresReview, ReportedAmount = 1_000_000
        });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).ConfirmAsync(1, selected.Id));
        await Cash(db).CancelAsync(10, selected.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).SelectAsync(10, Target(bill)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tenant_can_cancel_an_unpaid_choice_and_then_switch_to_vnpay(bool bill)
    {
        await using var db = await Fixture(bill);
        var selected = await Cash(db).SelectAsync(10, Target(bill));
        await Assert.ThrowsAsync<BusinessRuleException>(() => PaymentIpnTests.Service(db).CreatePaymentUrlAsync(10,
            new() { DepositId = bill ? null : 1, MonthlyBillId = bill ? 1 : null }, "127.0.0.1"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).CancelAsync(1, selected.Id));
        await Cash(db).CancelAsync(10, selected.Id);
        Assert.Equal(CashPaymentStatus.Cancelled, db.CashPaymentReceipts.Single().Status);
        var url = await PaymentIpnTests.Service(db).CreatePaymentUrlAsync(10,
            new() { DepositId = bill ? null : 1, MonthlyBillId = bill ? 1 : null }, "127.0.0.1");
        Assert.False(string.IsNullOrWhiteSpace(url.PaymentUrl));
        Assert.Single(db.PaymentTransactions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Received_cash_cannot_be_cancelled_or_paid_again_online(bool bill)
    {
        await using var db = await Fixture(bill);
        var selected = await Cash(db).SelectAsync(10, Target(bill));
        await Cash(db).ConfirmAsync(1, selected.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cash(db).CancelAsync(10, selected.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => PaymentIpnTests.Service(db).CreatePaymentUrlAsync(10,
            new() { DepositId = bill ? null : 1, MonthlyBillId = bill ? 1 : null }, "127.0.0.1"));
        Assert.Empty(db.PaymentTransactions);
        Assert.Equal(CashPaymentStatus.Received, db.CashPaymentReceipts.Single().Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_late_online_success_after_cash_collection_is_held_for_review_without_double_credit(bool bill)
    {
        await using var db = await Fixture(bill);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            DepositId = bill ? null : 1, MonthlyBillId = bill ? 1 : null,
            TargetType = bill ? "MonthlyBill" : "Deposit", OrderId = "ipn-order", Amount = 1_000_000,
            Status = PaymentTransactionStatus.Expired, CreatedAt = DateTime.Now.AddMinutes(-31)
        });
        await db.SaveChangesAsync();
        var selected = await Cash(db).SelectAsync(10, Target(bill));
        var received = await Cash(db).ConfirmAsync(1, selected.Id);
        var paidAt = bill ? db.MonthlyBills.Single().PaidAt : db.Deposits.Single().PaidAt;

        var callback = PaymentIpnTests.Callback();
        var result = await PaymentIpnTests.Service(db).ProcessPaymentReturnAsync(callback);

        Assert.True(result.RequiresReview);
        Assert.False(result.Success);
        Assert.Equal(PaymentEventState.RequiresReview, db.PaymentCallbackEvents.Single().ProcessingState);
        Assert.Equal(PaymentTransactionStatus.Expired, db.PaymentTransactions.Single().Status);
        Assert.Equal(paidAt, bill ? db.MonthlyBills.Single().PaidAt : db.Deposits.Single().PaidAt);
        Assert.Equal(bill ? MonthlyBillStatus.Paid : DepositStatus.Confirmed,
            bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
        Assert.Equal(received.ReceiptNumber, db.CashPaymentReceipts.Single().ReceiptNumber);
        Assert.Single(db.CashPaymentReceipts);
    }

    [Fact]
    public async Task Cash_deposit_can_create_contract_without_a_second_confirmation_or_fake_gateway_transaction()
    {
        await using var db = await Fixture();
        var selected = await Cash(db).SelectAsync(10, Target(false));
        await Cash(db).ConfirmAsync(1, selected.Id);
        var contract = await new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance).TaoAsync(1,
            new() { YeuCauThueId = 1, NgayBatDau = DateTime.Today, NgayKetThuc = DateTime.Today.AddMonths(1), TienThueHangThang = 3_000_000 });
        Assert.Equal(DepositStatus.Confirmed, db.Deposits.Single().Status);
        Assert.Equal(1_000_000m, db.RentalContracts.Single().DepositAmount);
        Assert.Equal(RentalRequestStatus.ConvertedToContract, db.RentalRequests.Single().Status);
        Assert.True(contract.Id > 0);
        Assert.Empty(db.PaymentTransactions);
    }

    [Fact]
    public async Task Cash_deposit_refund_preserves_the_original_receipt_and_releases_room_only_after_receipt_confirmation()
    {
        await using var db = await Fixture();
        var selected = await Cash(db).SelectAsync(10, Target(false));
        var received = await Cash(db).ConfirmAsync(1, selected.Id);
        var refunds = PaymentSafetyTests.Refunds(db);
        var refund = await refunds.CreateAsync(10, new() { DepositId = 1, Reason = "Hai bên thống nhất hoàn cọc đã thu tiền mặt" });
        await refunds.ActAsync(1, refund.Id, "approve", new());
        await refunds.ActAsync(1, refund.Id, "report", new() { Reference = "PHIEU-HOAN-TIEN-MAT-001" });
        Assert.Equal(RoomStatus.Reserved, db.Rooms.Single(x => x.Id == 1).Status);
        await refunds.ActAsync(10, refund.Id, "confirm", new());
        Assert.Equal(DepositStatus.Refunded, db.Deposits.Single().Status);
        Assert.Equal(RoomStatus.Available, db.Rooms.Single(x => x.Id == 1).Status);
        Assert.Equal(CashPaymentStatus.Received, db.CashPaymentReceipts.Single().Status);
        Assert.Equal(received.ReceiptNumber, db.CashPaymentReceipts.Single().ReceiptNumber);
        Assert.Empty(db.PaymentTransactions);
    }

    [Fact]
    public async Task Contract_settlement_uses_cash_deposit_and_preserves_its_receipt()
    {
        await using var db = await Fixture();
        var selected = await Cash(db).SelectAsync(10, Target(false));
        var received = await Cash(db).ConfirmAsync(1, selected.Id);
        var contract = new RentalContract
        {
            Id = 1, RentalRequestId = 1, PostId = 1, LandlordAccountId = 1, TenantAccountId = 10,
            Status = RentalContractStatus.Terminated, StartDate = DateTime.Today.AddMonths(-1), EndDate = DateTime.Today,
            MonthlyRent = 3_000_000, DepositAmount = 1_000_000
        };
        db.RentalContracts.Add(contract);
        db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
        db.ContractClosures.Add(new ContractClosure
        {
            Id = 1, Contract = contract, RoomId = 1, EffectiveEndAtUtc = DateTime.UtcNow,
            LastRentalDate = DateTime.Today, HandoverStatus = HandoverState.Confirmed,
            BillingFinalizedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var closure = new ContractClosureBLL(db, null!, NullLogger<ContractClosureBLL>.Instance);
        var view = await closure.ExecuteAsync(1, 1, "create-settlement", new() { IdempotencyKey = Guid.NewGuid().ToString() });

        var settlement = Assert.Single(view.Settlements);
        Assert.Equal(1_000_000m, settlement.DepositAmountSnapshot);
        Assert.Equal(1_000_000m, settlement.RefundDue);
        Assert.Equal(1, settlement.DepositId);
        Assert.Equal(received.ReceiptNumber, db.CashPaymentReceipts.Single().ReceiptNumber);
        Assert.Empty(db.PaymentTransactions);
    }

    [Fact]
    public async Task Existing_manual_bill_endpoint_records_a_cash_receipt()
    {
        await using var db = await Fixture(bill: true);
        var bill = await new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance)
            .ThanhToanAsync(1, 1, new() { GhiChu = "Nhận tiền trực tiếp tại phòng" });
        Assert.Equal(MonthlyBillStatus.Paid, bill.TrangThai);
        var receipt = Assert.Single(db.CashPaymentReceipts);
        Assert.Equal(CashPaymentStatus.Received, receipt.Status);
        Assert.Equal(1, receipt.ConfirmedByAccountId);
        Assert.Equal(1_000_000m, receipt.Amount);
        Assert.False(string.IsNullOrWhiteSpace(receipt.ReceiptNumber));
    }

    private static MonthlyBillController BillController(ApplicationDbContext db)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("UserId", "1"), new Claim(ClaimTypes.Role, "Landlord")], "test"))
        };
        return new MonthlyBillController(new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task Stale_displayed_amount_cannot_silently_collect_a_changed_bill()
    {
        await using var db = await Fixture(bill: true);
        var displayedAmount = db.MonthlyBills.Single().TotalAmount;
        db.MonthlyBills.Single().TotalAmount += 500_000;
        await db.SaveChangesAsync();

        var response = Assert.IsType<ObjectResult>(await BillController(db).ThanhToan(1,
            new() { ExpectedAmount = displayedAmount, PhuongThucThanhToan = "Tiền mặt" }));

        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        Assert.Equal(MonthlyBillStatus.Unpaid, db.MonthlyBills.Single().Status);
        Assert.Null(db.MonthlyBills.Single().PaidAt);
        Assert.Empty(db.CashPaymentReceipts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Manual_bill_api_requires_the_amount_confirmed_by_the_landlord(int? expectedAmount)
    {
        await using var db = await Fixture(bill: true);
        var response = Assert.IsType<ObjectResult>(await BillController(db).ThanhToan(1,
            new() { ExpectedAmount = expectedAmount }));

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(MonthlyBillStatus.Unpaid, db.MonthlyBills.Single().Status);
        Assert.Null(db.MonthlyBills.Single().PaidAt);
        Assert.Empty(db.CashPaymentReceipts);
    }

    [Fact]
    public async Task Manual_bill_api_accepts_the_confirmed_current_amount_and_issues_one_receipt()
    {
        await using var db = await Fixture(bill: true);
        var dto = new XacNhanThanhToanHoaDonDto { ExpectedAmount = 1_000_000, PhuongThucThanhToan = "Tiền mặt" };

        Assert.IsType<OkObjectResult>(await BillController(db).ThanhToan(1, dto));
        Assert.IsType<OkObjectResult>(await BillController(db).ThanhToan(1, dto));

        Assert.Equal(MonthlyBillStatus.Paid, db.MonthlyBills.Single().Status);
        Assert.Equal(CashPaymentStatus.Received, Assert.Single(db.CashPaymentReceipts).Status);
    }
}

[Collection("SqlServerWorkflow")]
public class CashPaymentSqlTests
{
    [SqlServerFact]
    public async Task Concurrent_cash_selection_and_online_creation_cannot_both_succeed()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        builder.InitialCatalog += "_CashRace_" + Guid.NewGuid().ToString("N")[..8];
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        try
        {
            var owner = new User { UserName = "cash-race-owner", Email = "owner@cash-race.test", FullName = "Owner", PasswordHash = "test", RoleId = 2 };
            var tenant = new User { UserName = "cash-race-tenant", Email = "tenant@cash-race.test", FullName = "Tenant", PasswordHash = "test", RoleId = 1 };
            var landlord = new LandlordProfile { Account = owner };
            var room = new Room { Landlord = landlord, Category = new RoomCategory { Name = "Cash race" }, RoomName = "Cash race room", Address = "Test", Price = 3_000_000, Area = 25, MaxOccupants = 2, Status = RoomStatus.Reserved };
            var post = new Post { Room = room, Landlord = landlord, Title = "Cash race", Content = "Test", DisplayPrice = room.Price, Status = PostStatus.Approved };
            db.AddRange(post, tenant);
            await db.SaveChangesAsync();
            var request = new RentalRequest { PostId = post.Id, TenantAccountId = tenant.Id, LandlordAccountId = owner.Id, Status = RentalRequestStatus.Approved };
            db.RentalRequests.Add(request);
            await db.SaveChangesAsync();
            var deposit = new Deposit { RentalRequestId = request.Id, TenantAccountId = tenant.Id, LandlordAccountId = owner.Id, Amount = 1_000_000, DueAt = DateTime.Now.AddDays(1), Status = DepositStatus.Pending };
            db.Deposits.Add(deposit);
            await db.SaveChangesAsync();

            async Task<int> ChooseCash()
            {
                await using var context = new ApplicationDbContext(options);
                try { await new CashPaymentBLL(context, null!, NullLogger<CashPaymentBLL>.Instance).SelectAsync(tenant.Id, new() { DepositId = deposit.Id }); return 200; }
                catch (BusinessRuleException ex) { return ex.StatusCode; }
            }
            async Task<int> ChooseOnline()
            {
                await using var context = new ApplicationDbContext(options);
                try { await PaymentIpnTests.Service(context).CreatePaymentUrlAsync(tenant.Id, new() { DepositId = deposit.Id }, "127.0.0.1"); return 200; }
                catch (BusinessRuleException ex) { return ex.StatusCode; }
            }

            Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(ChooseCash(), ChooseOnline())).Order().ToArray());
            db.ChangeTracker.Clear();
            Assert.Equal(1, await db.PaymentTransactions.CountAsync() + await db.CashPaymentReceipts.CountAsync());
            Assert.Equal(DepositStatus.Pending, (await db.Deposits.SingleAsync()).Status);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
