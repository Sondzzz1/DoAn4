using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class TokenPaymentTests
{
    private static IConfiguration Config(bool enabled = true) => new ConfigurationBuilder()
        .AddConfiguration(PaymentIpnTests.Config()).AddConfiguration(VnPayTokenProtocolTests.Config(enabled: enabled)).Build();
    private static PaymentBLL Payments(ApplicationDbContext db, bool enabled = true)
    {
        var config = Config(enabled);
        return new(db, config, null!, NullLogger<PaymentBLL>.Instance, SavedPaymentMethodTests.Cards(db, config), new(config));
    }

    private static async Task<(ApplicationDbContext Db, int Method)> Fixture(bool bill)
    {
        var db = await SavedPaymentMethodTests.Fixture();
        db.Deposits.Single().Status = bill ? DepositStatus.Confirmed : DepositStatus.Pending;
        if (bill)
        {
            var contract = new RentalContract
            {
                Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1,
                StartDate = DateTime.Today.AddMonths(-1), EndDate = DateTime.Today.AddMonths(2),
                MonthlyRent = 1_000_000, DepositAmount = 1_000_000, Status = RentalContractStatus.Active
            };
            db.RentalContracts.Add(contract);
            db.MonthlyBills.Add(new MonthlyBill
            {
                Id = 1, Contract = contract, Month = DateTime.Today.Month, Year = DateTime.Today.Year,
                TotalAmount = 1_000_000, RoomPrice = 1_000_000, Status = MonthlyBillStatus.Unpaid,
                DueDate = DateTime.Now.AddDays(3)
            });
            db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
        }
        await db.SaveChangesAsync();
        return (db, (await SavedPaymentMethodTests.Link(db)).Id);
    }

    private static CreatePaymentRequestDto Target(bool bill, int method) => bill
        ? new() { MonthlyBillId = 1, SavedPaymentMethodId = method }
        : new() { DepositId = 1, SavedPaymentMethodId = method };

    private static IQueryCollection Callback(string order, Action<Dictionary<string, string>>? change = null)
    {
        var fields = VnPayTokenProtocolTests.LinkFields(order);
        fields["vnp_command"] = "token_pay";
        fields["vnp_amount"] = "100000000";
        fields["vnp_curr_code"] = "VND";
        fields["vnp_transaction_no"] = "13888001";
        change?.Invoke(fields);
        return VnPayTokenProtocolTests.Callback(fields);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Saved_card_payment_binds_the_owned_method_to_one_expected_target_and_never_stores_token_in_payment(bool bill)
    {
        var fixture = await Fixture(bill);
        await using var db = fixture.Db;
        var response = await Payments(db).CreatePaymentUrlAsync(10, Target(bill, fixture.Method), "127.0.0.1");
        var fields = QueryHelpers.ParseQuery(new Uri(response.PaymentUrl).Query);
        Assert.Equal("token_pay", fields["vnp_command"]);
        Assert.Equal("10", fields["vnp_app_user_id"]);
        Assert.Equal(VnPayTokenProtocolTests.Merchant, fields["vnp_tmn_code"]);
        Assert.Equal(VnPayTokenProtocolTests.Token, fields["vnp_token"]);
        Assert.Equal("100000000", fields["vnp_amount"]);
        Assert.Equal(1_000_000m, response.Amount);
        var attempt = Assert.Single(db.PaymentTransactions);
        Assert.Equal("VNPayToken", attempt.Provider);
        Assert.Equal(fixture.Method, attempt.SavedPaymentMethodId);
        Assert.Equal(response.OrderId, attempt.OrderId);
        Assert.Equal(PaymentTransactionStatus.Pending, attempt.Status);
        var storedStrings = typeof(PaymentTransaction).GetProperties().Where(p => p.PropertyType == typeof(string))
            .ToDictionary(p => p.Name, p => p.GetValue(attempt));
        Assert.DoesNotContain(VnPayTokenProtocolTests.Token, JsonSerializer.Serialize(storedStrings));
        Assert.Equal(bill ? MonthlyBillStatus.PendingPayment : DepositStatus.Pending,
            bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => SavedPaymentMethodTests.Cards(db).UnlinkAsync(10, fixture.Method))).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Verified_token_callback_pays_once_across_repeated_IPN_and_browser_return(bool bill)
    {
        var fixture = await Fixture(bill);
        await using var db = fixture.Db;
        var service = Payments(db);
        var payment = await service.CreatePaymentUrlAsync(10, Target(bill, fixture.Method), "127.0.0.1");
        var query = Callback(payment.OrderId);
        var first = await service.ProcessTokenPaymentReturnAsync(query);
        var paidAt = db.PaymentTransactions.Single().ProcessedAt;
        var again = await service.ProcessTokenPaymentReturnAsync(query);
        var browser = await service.ProcessTokenPaymentReturnAsync(query, PaymentEventSource.Return);
        Assert.True(first.Success);
        Assert.True(again.Success);
        Assert.True(browser.Success);
        Assert.Equal(PaymentCallbackOutcome.Recorded, first.CallbackOutcome);
        Assert.Equal(PaymentCallbackOutcome.AlreadyRecorded, again.CallbackOutcome);
        Assert.Equal(PaymentCallbackOutcome.AlreadyRecorded, browser.CallbackOutcome);
        Assert.Equal(PaymentTransactionStatus.Succeeded, db.PaymentTransactions.Single().Status);
        Assert.Equal(paidAt, db.PaymentTransactions.Single().ProcessedAt);
        Assert.Equal(bill ? MonthlyBillStatus.Paid : DepositStatus.Paid,
            bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
        Assert.Equal(2, db.PaymentCallbackEvents.Count());
        Assert.All(db.PaymentCallbackEvents, e => Assert.Equal(PaymentEventState.Applied, e.ProcessingState));
    }

    [Theory]
    [InlineData("other-owner")]
    [InlineData("unlinked")]
    [InlineData("disabled")]
    [InlineData("amount-limit")]
    public async Task Rejected_saved_method_request_leaves_bill_and_payment_records_unchanged(string scenario)
    {
        var fixture = await Fixture(true);
        await using var db = fixture.Db;
        if (scenario == "other-owner") db.SavedPaymentMethods.Single().UserId = 40;
        if (scenario == "unlinked") await SavedPaymentMethodTests.Cards(db).UnlinkAsync(10, fixture.Method);
        if (scenario == "amount-limit") db.MonthlyBills.Single().TotalAmount = 10_000_000_000m;
        await db.SaveChangesAsync();
        var due = db.MonthlyBills.Single().DueDate;
        await Assert.ThrowsAsync<BusinessRuleException>(() => Payments(db, scenario != "disabled")
            .CreatePaymentUrlAsync(10, Target(true, fixture.Method), "127.0.0.1"));
        db.ChangeTracker.Clear();
        Assert.Equal(MonthlyBillStatus.Unpaid, db.MonthlyBills.Single().Status);
        Assert.Null(db.MonthlyBills.Single().PaidAt);
        Assert.Equal(due, db.MonthlyBills.Single().DueDate);
        Assert.Empty(db.PaymentTransactions);
        Assert.Empty(db.PaymentCallbackEvents);
        Assert.Empty(db.ContractClosureEvents);
    }

    [Fact]
    public async Task Standard_gateway_signature_cannot_settle_a_saved_token_order()
    {
        var fixture = await Fixture(false);
        await using var db = fixture.Db;
        var service = Payments(db);
        var payment = await service.CreatePaymentUrlAsync(10, Target(false, fixture.Method), "127.0.0.1");
        var result = await service.ProcessPaymentReturnAsync(PaymentIpnTests.Callback(order: payment.OrderId));
        Assert.True(result.RequiresReview);
        Assert.False(result.Success);
        Assert.Equal(PaymentEventState.RequiresReview, db.PaymentCallbackEvents.Single().ProcessingState);
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Theory]
    [InlineData("vnp_app_user_id", "40")]
    [InlineData("vnp_token", "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB")]
    [InlineData("vnp_amount", "100000001")]
    public async Task Authenticated_but_mismatched_token_payment_needs_review_without_applying_money(string key, string value)
    {
        var fixture = await Fixture(false);
        await using var db = fixture.Db;
        var service = Payments(db);
        var payment = await service.CreatePaymentUrlAsync(10, Target(false, fixture.Method), "127.0.0.1");
        var result = await service.ProcessTokenPaymentReturnAsync(Callback(payment.OrderId, fields => fields[key] = value));
        Assert.True(result.RequiresReview);
        Assert.False(result.Success);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
        Assert.Equal(PaymentEventState.RequiresReview, db.PaymentCallbackEvents.Single().ProcessingState);
    }

    [Fact]
    public async Task An_invalid_signature_creates_no_payment_event_and_applies_no_money()
    {
        var fixture = await Fixture(false);
        await using var db = fixture.Db;
        var service = Payments(db);
        var payment = await service.CreatePaymentUrlAsync(10, Target(false, fixture.Method), "127.0.0.1");
        var values = Callback(payment.OrderId).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        values["vnp_secure_hash"] = new string('0', 128);
        var result = await service.ProcessTokenPaymentReturnAsync(new QueryCollection(values));
        Assert.Equal(PaymentCallbackOutcome.InvalidSignature, result.CallbackOutcome);
        Assert.False(result.Success);
        Assert.Empty(db.PaymentCallbackEvents);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Fact]
    public async Task A_late_token_success_after_cash_receipt_is_reviewed_without_counting_the_bill_twice()
    {
        var fixture = await Fixture(true);
        await using var db = fixture.Db;
        var service = Payments(db);
        var online = await service.CreatePaymentUrlAsync(10, Target(true, fixture.Method), "127.0.0.1");
        db.PaymentTransactions.Single().CreatedAt = DateTime.Now.AddMinutes(-31);
        await db.SaveChangesAsync();
        await service.ReconcilePendingPaymentsAsync();
        var cash = new CashPaymentBLL(db, null!, NullLogger<CashPaymentBLL>.Instance);
        var selection = await cash.SelectAsync(10, new() { MonthlyBillId = 1 });
        var receipt = await cash.ConfirmAsync(1, selection.Id);
        var paidAt = db.MonthlyBills.Single().PaidAt;
        var result = await service.ProcessTokenPaymentReturnAsync(Callback(online.OrderId));
        Assert.True(result.RequiresReview);
        Assert.Equal(PaymentTransactionStatus.Expired, db.PaymentTransactions.Single().Status);
        Assert.Equal(MonthlyBillStatus.Paid, db.MonthlyBills.Single().Status);
        Assert.Equal("Tiền mặt", db.MonthlyBills.Single().PaymentMethod);
        Assert.Equal(paidAt, db.MonthlyBills.Single().PaidAt);
        Assert.Equal(receipt.ReceiptNumber, db.CashPaymentReceipts.Single().ReceiptNumber);
        Assert.Single(db.CashPaymentReceipts);
    }
}
