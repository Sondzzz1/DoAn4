using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

[Collection("SqlServerWorkflow")]
public class SavedPaymentFlowSqlTests
{
    [SqlServerFact]
    public async Task Concurrent_verified_link_callbacks_save_once_then_saved_card_payment_and_local_unlink_preserve_history()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        builder.InitialCatalog = builder.InitialCatalog[..Math.Min(85, builder.InitialCatalog.Length)] + "_SavedFlow_" + Guid.NewGuid().ToString("N")[..12];
        var databaseName = builder.InitialCatalog;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        var config = new ConfigurationBuilder().AddConfiguration(PaymentIpnTests.Config()).AddConfiguration(VnPayTokenProtocolTests.Config()).Build();
        var protection = new EphemeralDataProtectionProvider();
        SavedPaymentMethodBLL Cards(ApplicationDbContext context) => SavedPaymentMethodTests.Cards(context, config, protection);
        PaymentBLL Payments(ApplicationDbContext context) => new(context, config, null!, NullLogger<PaymentBLL>.Instance, Cards(context), new(config));
        await using var db = new ApplicationDbContext(options);
        var created = await db.Database.EnsureCreatedAsync();
        Assert.True(created);
        try
        {
            await RoommateTests.SeedAsync(db);
            var intent = await Cards(db).CreateLinkAsync(2, new() { CardType = "01" }, "127.0.0.1");
            var linkCallback = VnPayTokenProtocolTests.Callback(VnPayTokenProtocolTests.LinkFields(intent.Reference, "2"));
            async Task<bool> CompleteLink()
            {
                await using var context = new ApplicationDbContext(options);
                return await Cards(context).CompleteLinkAsync(linkCallback);
            }
            Assert.All(await Task.WhenAll(CompleteLink(), CompleteLink()), outcome => Assert.True(outcome));
            db.ChangeTracker.Clear();
            var method = await db.SavedPaymentMethods.SingleAsync();
            Assert.NotEqual(VnPayTokenProtocolTests.Token, method.TokenCiphertext);
            Assert.Equal("•••• 2198", method.MaskedCardNumber);
            Assert.Equal(method.Id, (await db.VnPayCardLinks.SingleAsync()).SavedPaymentMethodId);
            var bill = new MonthlyBill { ContractId = 1, Month = DateTime.Today.Month, Year = DateTime.Today.Year,
                TotalAmount = 1_000_000, RoomPrice = 1_000_000, DueDate = DateTime.Now.AddDays(2), Status = MonthlyBillStatus.Unpaid };
            db.MonthlyBills.Add(bill);
            await db.SaveChangesAsync();
            var payment = await Payments(db).CreatePaymentUrlAsync(2, new() { MonthlyBillId = bill.Id, SavedPaymentMethodId = method.Id }, "127.0.0.1");
            Assert.Equal("VNPayToken", (await db.PaymentTransactions.SingleAsync()).Provider);
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).UnlinkAsync(2, method.Id))).StatusCode);
            var fields = VnPayTokenProtocolTests.LinkFields(payment.OrderId, "2");
            fields["vnp_command"] = "token_pay";
            fields["vnp_amount"] = "100000000";
            fields["vnp_curr_code"] = "VND";
            var callback = VnPayTokenProtocolTests.Callback(fields);
            Assert.True((await Payments(db).ProcessTokenPaymentReturnAsync(callback)).Success);
            await Cards(db).UnlinkAsync(2, method.Id);
            Assert.True((await Payments(db).ProcessTokenPaymentReturnAsync(callback, 1)).Success);
            db.ChangeTracker.Clear();
            Assert.Equal(MonthlyBillStatus.Paid, (await db.MonthlyBills.SingleAsync()).Status);
            var storedPayment = await db.PaymentTransactions.SingleAsync();
            Assert.Equal(1_000_000m, storedPayment.Amount);
            Assert.Equal(PaymentTransactionStatus.Succeeded, storedPayment.Status);
            Assert.Equal(method.Id, storedPayment.SavedPaymentMethodId);
            Assert.False((await db.SavedPaymentMethods.SingleAsync()).IsActive);
            Assert.Null((await db.SavedPaymentMethods.SingleAsync()).TokenCiphertext);
        }
        finally
        {
            Assert.True(created);
            Assert.StartsWith("RoomRental_Verification_", databaseName);
            Assert.Equal(databaseName, new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog);
            await db.Database.CloseConnectionAsync();
            SqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
