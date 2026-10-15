using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.Controllers;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class PaymentIpnTests
{
    internal static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["VnPay:TmnCode"] = "TESTONLY", ["VnPay:HashSecret"] = "ipn-test-secret-not-for-production",
        ["VnPay:BaseUrl"] = "https://example.test/pay", ["VnPay:ReturnUrl"] = "https://example.test/return"
    }).Build();

    internal static PaymentBLL Service(ApplicationDbContext db) => new(db, Config(), null!, NullLogger<PaymentBLL>.Instance);

    internal static IQueryCollection Callback(string order = "ipn-order", string amount = "100000000", string response = "00",
        string? status = "00", string? transaction = "ipn-transaction")
    {
        var signer = new VnPayHelper();
        signer.AddRequestData("vnp_TmnCode", "TESTONLY");
        signer.AddRequestData("vnp_TxnRef", order);
        signer.AddRequestData("vnp_Amount", amount);
        signer.AddRequestData("vnp_ResponseCode", response);
        if (status != null) signer.AddRequestData("vnp_TransactionStatus", status);
        if (transaction != null) signer.AddRequestData("vnp_TransactionNo", transaction);
        return new QueryCollection(QueryHelpers.ParseQuery(new Uri(signer.CreateRequestUrl("https://example.test/callback", Config()["VnPay:HashSecret"]!)).Query));
    }

    internal static async Task<JsonElement> Ipn(ApplicationDbContext db, IQueryCollection query, IConfiguration? config = null)
    {
        config ??= Config();
        var service = new PaymentBLL(db, config, null!, NullLogger<PaymentBLL>.Instance);
        var controller = new PaymentController(service, config, NullLogger<PaymentController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Query = query;
        var ok = Assert.IsType<OkObjectResult>(await controller.PaymentIpn());
        return JsonSerializer.SerializeToElement(ok.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    internal static string Code(JsonElement json) => json.TryGetProperty("RspCode", out var code)
        ? code.GetString()! : json.GetProperty("rspCode").GetString()!;

    private static async Task<ApplicationDbContext> Fixture(bool bill = false)
    {
        var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Deposits.Single().Status = DepositStatus.Pending;
        var attempt = new PaymentTransaction { OrderId = "ipn-order", Amount = 1000000, Status = PaymentTransactionStatus.Pending, DepositId = 1 };
        if (bill)
        {
            var contract = new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1, LandlordAccountId = 1, TenantAccountId = 10, Status = RentalContractStatus.Active };
            db.RentalContracts.Add(contract);
            db.MonthlyBills.Add(new MonthlyBill { Id = 1, Contract = contract, TotalAmount = 1000000, Status = MonthlyBillStatus.PendingPayment, DueDate = DateTime.Now.AddDays(1) });
            attempt.DepositId = null; attempt.MonthlyBillId = 1; attempt.TargetType = "MonthlyBill";
        }
        db.PaymentTransactions.Add(attempt);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Invalid_signature_returns_97_and_does_not_touch_data()
    {
        await using var db = await Fixture();
        var query = new QueryCollection(QueryHelpers.ParseQuery("?vnp_TxnRef=ipn-order&vnp_SecureHash=invalid"));
        Assert.Equal("97", Code(await Ipn(db, query)));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Fact]
    public async Task Unknown_order_returns_01() { await using var db = await Fixture(); Assert.Equal("01", Code(await Ipn(db, Callback(order: "not-found")))); }

    [Theory]
    [InlineData("99999900")]
    [InlineData("invalid")]
    [InlineData("")]
    public async Task Amount_mismatch_returns_04_without_accepting_payment(string amount)
    {
        await using var db = await Fixture();
        Assert.Equal("04", Code(await Ipn(db, Callback(amount: amount))));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
    }

    [Fact]
    public async Task Wire_json_preserves_exact_VnPay_property_names_and_hides_internal_metadata()
    {
        await using var db = await Fixture();
        var wire = await Ipn(db, Callback(order: "not-found"));
        Assert.True(wire.TryGetProperty("RspCode", out _));
        Assert.True(wire.TryGetProperty("Message", out _));
        Assert.False(wire.TryGetProperty("rspCode", out _));
        Assert.Equal(2, wire.EnumerateObject().Count());
        var result = JsonSerializer.SerializeToElement(await Service(db).ProcessPaymentReturnAsync(Callback(order: "not-found")), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.False(result.TryGetProperty("callbackOutcome", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Successful_callback_returns_00_then_02_without_reapplying(bool bill)
    {
        await using var db = await Fixture(bill);
        var query = Callback();
        Assert.Equal("00", Code(await Ipn(db, query)));
        var processedAt = db.PaymentTransactions.Single().ProcessedAt;
        Assert.Equal("02", Code(await Ipn(db, query)));
        Assert.Equal(processedAt, db.PaymentTransactions.Single().ProcessedAt);
        Assert.Equal(PaymentTransactionStatus.Succeeded, db.PaymentTransactions.Single().Status);
        Assert.Equal(bill ? MonthlyBillStatus.Paid : DepositStatus.Paid, bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
        Assert.Equal(RoomStatus.Reserved, db.Rooms.Single(r => r.Id == 1).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Valid_failure_is_acknowledged_but_does_not_report_payment_success(bool bill)
    {
        await using var db = await Fixture(bill);
        var query = Callback(response: "24", status: "02");
        Assert.Equal("00", Code(await Ipn(db, query)));
        Assert.Equal(PaymentTransactionStatus.Failed, db.PaymentTransactions.Single().Status);
        var processed = db.PaymentTransactions.Single().ProcessedAt;
        Assert.Equal("02", Code(await Ipn(db, query)));
        Assert.Equal(processed, db.PaymentTransactions.Single().ProcessedAt);
        Assert.False((await Service(db).ProcessPaymentReturnAsync(query)).Success);
        Assert.Equal(bill ? MonthlyBillStatus.Unpaid : DepositStatus.Pending, bill ? db.MonthlyBills.Single().Status : db.Deposits.Single().Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bad")]
    public async Task Missing_or_malformed_provider_status_is_not_accepted_as_success(string? status)
    {
        await using var db = await Fixture();
        Assert.Equal("99", Code(await Ipn(db, Callback(status: status))));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Fact]
    public async Task Expired_success_is_acknowledged_only_after_persisting_a_review_event()
    {
        await using var db = await Fixture();
        db.PaymentTransactions.Single().CreatedAt = DateTime.Now.AddMinutes(-31);
        await db.SaveChangesAsync();
        Assert.Equal("00", Code(await Ipn(db, Callback())));
        Assert.Equal(PaymentEventState.RequiresReview, db.PaymentCallbackEvents.Single().ProcessingState);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
        Assert.NotEqual(PaymentTransactionStatus.Succeeded, db.PaymentTransactions.Single().Status);
    }

    [Fact]
    public async Task Expired_deposit_is_recorded_for_review_but_system_errors_return_99()
    {
        await using var db = await Fixture();
        db.Deposits.Single().DueAt = DateTime.Now.AddMinutes(-1);
        await db.SaveChangesAsync();
        Assert.Equal("00", Code(await Ipn(db, Callback())));
        Assert.Equal(PaymentEventState.RequiresReview, db.PaymentCallbackEvents.Single().ProcessingState);
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
        Assert.Equal("99", Code(await Ipn(db, Callback(), new ConfigurationBuilder().Build())));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    public async Task Failed_callback_without_a_provider_transaction_does_not_claim_a_unique_transaction_code(string? transaction)
    {
        await using var db = await Fixture();
        var query = Callback(response: "24", status: "02", transaction: transaction);
        Assert.Equal("00", Code(await Ipn(db, query)));
        Assert.Null(db.PaymentTransactions.Single().TransactionCode);
        Assert.Equal("02", Code(await Ipn(db, query)));
    }

    [Fact]
    public async Task Legacy_failed_zero_transaction_can_be_acknowledged_without_rewriting_history()
    {
        await using var db = await Fixture();
        var payment = db.PaymentTransactions.Single();
        payment.Status = PaymentTransactionStatus.Failed; payment.ResponseCode = "24";
        payment.TransactionCode = "0"; payment.ProcessedAt = DateTime.Now.AddMinutes(-1);
        await db.SaveChangesAsync();
        var processed = payment.ProcessedAt;
        Assert.Equal("02", Code(await Ipn(db, Callback(response: "24", status: "02", transaction: "0"))));
        Assert.Equal("0", payment.TransactionCode);
        Assert.Equal(processed, payment.ProcessedAt);
    }

    [Theory]
    [InlineData("00", "02")]
    [InlineData("24", "00")]
    [InlineData("bad", "02")]
    public async Task Inconsistent_or_malformed_status_does_not_become_a_recorded_failure(string response, string status)
    {
        await using var db = await Fixture();
        Assert.Equal("99", Code(await Ipn(db, Callback(response: response, status: status))));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    public async Task Success_without_a_real_provider_transaction_is_not_confirmed(string? transaction)
    {
        await using var db = await Fixture();
        Assert.Equal("99", Code(await Ipn(db, Callback(transaction: transaction))));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
    }

    [Fact]
    public async Task Contradictory_callback_does_not_overwrite_a_recorded_payment()
    {
        await using var db = await Fixture();
        Assert.Equal("00", Code(await Ipn(db, Callback())));
        var processed = db.PaymentTransactions.Single().ProcessedAt;
        Assert.Equal("00", Code(await Ipn(db, Callback(response: "24", status: "02"))));
        Assert.Equal("00", Code(await Ipn(db, Callback(transaction: "another-transaction"))));
        Assert.Equal(PaymentTransactionStatus.Succeeded, db.PaymentTransactions.Single().Status);
        Assert.Equal(processed, db.PaymentTransactions.Single().ProcessedAt);
        Assert.Equal("ipn-transaction", db.PaymentTransactions.Single().TransactionCode);
    }

    [Fact]
    public async Task Provider_transaction_cannot_be_reused_for_another_order()
    {
        await using var db = await Fixture();
        db.PaymentTransactions.Add(new PaymentTransaction { OrderId = "other-order", DepositId = 1, Amount = 1000000,
            Status = PaymentTransactionStatus.Succeeded, ResponseCode = "00", TransactionCode = "ipn-transaction" });
        await db.SaveChangesAsync();
        Assert.Equal("00", Code(await Ipn(db, Callback())));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single(p => p.OrderId == "ipn-order").Status);
        Assert.Equal(DepositStatus.Pending, db.Deposits.Single().Status);
    }

    [Fact]
    public async Task Empty_callback_does_not_access_payment_configuration_or_report_success()
    {
        await using var db = await Fixture();
        Assert.Equal("99", Code(await Ipn(db, new QueryCollection())));
        Assert.Equal(PaymentTransactionStatus.Pending, db.PaymentTransactions.Single().Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Browser_return_uses_lowercase_boolean_expected_by_the_existing_frontend(bool success)
    {
        await using var db = await Fixture();
        var controller = new PaymentController(Service(db), Config(), NullLogger<PaymentController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Query = success ? Callback() : Callback(response: "24", status: "02");
        var redirect = Assert.IsType<RedirectResult>(await controller.PaymentReturn());
        var query = QueryHelpers.ParseQuery(new Uri(redirect.Url).Query);
        Assert.Equal(success ? "true" : "false", query["success"].ToString());
        Assert.Equal(success ? "00" : "24", query["responseCode"].ToString());
    }
}

[Collection("SqlServerWorkflow")]
public class PaymentIpnSqlTests
{
    [SqlServerFact]
    public async Task Concurrent_ipn_callbacks_have_one_application_and_one_already_processed_on_sql()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        builder.InitialCatalog += "_PaymentIpn";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var landlord = new LandlordProfile { Account = new User { UserName = "ipn-owner", FullName = "IPN owner", Email = "ipn-owner@test", PasswordHash = "test", RoleId = 2 } };
        var tenant = new User { UserName = "ipn-tenant", FullName = "IPN tenant", Email = "ipn-tenant@test", PasswordHash = "test", RoleId = 1 };
        var room = new Room { Landlord = landlord, Category = new RoomCategory { Name = "IPN QA" }, RoomName = "IPN QA", Address = "QA", Price = 3000000, Area = 25, MaxOccupants = 2, Status = RoomStatus.Reserved };
        var post = new Post { Landlord = landlord, Room = room, Title = "IPN QA", Content = "QA", Status = PostStatus.Approved };
        db.AddRange(post, tenant); await db.SaveChangesAsync();
        var request = new RentalRequest { PostId = post.Id, TenantAccountId = tenant.Id, LandlordAccountId = landlord.AccountId, Status = RentalRequestStatus.Approved };
        db.Add(request); await db.SaveChangesAsync();
        var deposit = new Deposit { RentalRequestId = request.Id, TenantAccountId = tenant.Id, LandlordAccountId = landlord.AccountId, Amount = 1000000, DueAt = DateTime.Now.AddDays(1) };
        db.Add(deposit); await db.SaveChangesAsync();
        db.Add(new PaymentTransaction { DepositId = deposit.Id, OrderId = "sql-ipn", Amount = 1000000 }); await db.SaveChangesAsync();
        async Task<string> Send()
        {
            await using var context = new ApplicationDbContext(options);
            return PaymentIpnTests.Code(await PaymentIpnTests.Ipn(context, PaymentIpnTests.Callback(order: "sql-ipn")));
        }
        Assert.Equal(new[] { "00", "02" }, (await Task.WhenAll(Send(), Send())).Order().ToArray());
        db.ChangeTracker.Clear();
        Assert.Equal(DepositStatus.Paid, (await db.Deposits.SingleAsync()).Status);
        Assert.Equal(PaymentTransactionStatus.Succeeded, (await db.PaymentTransactions.SingleAsync()).Status);
        Assert.Equal(RoomStatus.Reserved, (await db.Rooms.SingleAsync()).Status);

        foreach (var order in new[] { "sql-failure-a", "sql-failure-b" })
        {
            db.Add(new PaymentTransaction { DepositId = deposit.Id, OrderId = order, Amount = 1000000 });
            await db.SaveChangesAsync();
            await using var context = new ApplicationDbContext(options);
            var query = PaymentIpnTests.Callback(order: order, response: "24", status: "02", transaction: "0");
            Assert.Equal("00", PaymentIpnTests.Code(await PaymentIpnTests.Ipn(context, query)));
            Assert.Equal("02", PaymentIpnTests.Code(await PaymentIpnTests.Ipn(context, query)));
        }
        Assert.Equal(2, await db.PaymentTransactions.CountAsync(p => p.Status == PaymentTransactionStatus.Failed && p.TransactionCode == null));
        Assert.Equal(DepositStatus.Paid, (await db.Deposits.AsNoTracking().SingleAsync()).Status);
    }
}
