using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
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

public class SavedPaymentMethodTests
{
    private static readonly IDataProtectionProvider Protection = new EphemeralDataProtectionProvider();
    internal static SavedPaymentMethodBLL Cards(ApplicationDbContext db, IConfiguration? config = null,
        IDataProtectionProvider? protection = null) => new(db, protection ?? Protection,
            new VnPayTokenProtocol(config ?? VnPayTokenProtocolTests.Config()), NullLogger<SavedPaymentMethodBLL>.Instance);

    internal static async Task<ApplicationDbContext> Fixture()
    {
        var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Users.AddRange(
            new User { Id = 30, UserName = "saved-admin", Email = "admin@saved.test", FullName = "Admin", PasswordHash = "test", RoleId = 0 },
            new User { Id = 40, UserName = "saved-other", Email = "other@saved.test", FullName = "Other tenant", PasswordHash = "test", RoleId = 1 });
        await db.SaveChangesAsync();
        return db;
    }

    internal static async Task<(int Id, IQueryCollection Callback)> Link(ApplicationDbContext db, int user = 10,
        string? token = null, string card = "970419xxxxxxxxx2198")
    {
        var response = await Cards(db).CreateLinkAsync(user, new() { CardType = "01" }, "127.0.0.1");
        var fields = VnPayTokenProtocolTests.LinkFields(response.Reference, user.ToString());
        fields["vnp_card_number"] = card;
        if (token != null) fields["vnp_token"] = token;
        var callback = VnPayTokenProtocolTests.Callback(fields);
        Assert.True(await Cards(db).CompleteLinkAsync(callback));
        var method = await db.SavedPaymentMethods.SingleAsync(x => x.UserId == user && x.IsActive);
        return (method.Id, callback);
    }

    [Fact]
    public async Task Creating_link_only_saves_a_bounded_intention_without_payment_or_method()
    {
        await using var db = await Fixture();
        var result = await Cards(db).CreateLinkAsync(10, new() { CardType = "01" }, "127.0.0.1");
        var intention = Assert.Single(db.VnPayCardLinks);
        Assert.Equal(result.Reference, intention.Reference);
        Assert.Equal(10, intention.UserId);
        Assert.Equal(VnPayTokenProtocolTests.Merchant, intention.MerchantCode);
        Assert.Equal(VnPayCardLinkStatus.Pending, intention.Status);
        Assert.InRange(intention.ExpiresAtUtc - intention.CreatedAtUtc, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
        Assert.Null(intention.CompletedAtUtc);
        Assert.Empty(db.SavedPaymentMethods);
        Assert.Empty(db.PaymentTransactions);
        Assert.Null(db.Deposits.Single().PaidAt);
        Assert.Contains("token_create", result.RedirectUrl);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(0)]
    [InlineData(999)]
    public async Task Only_active_tenants_can_list_or_create_link_intentions(int actor)
    {
        await using var db = await Fixture();
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).ListAsync(actor))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CreateLinkAsync(actor, new(), "127.0.0.1"))).StatusCode);
        Assert.Empty(db.VnPayCardLinks);
    }

    [Fact]
    public async Task Disabled_provider_capability_is_visible_and_cannot_create_fake_saved_cards()
    {
        await using var db = await Fixture();
        var config = VnPayTokenProtocolTests.Config(enabled: false);
        var list = await Cards(db, config).ListAsync(10);
        Assert.False(list.Enabled);
        Assert.False(string.IsNullOrWhiteSpace(list.UnavailableReason));
        Assert.Empty(list.Methods);
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db, config).CreateLinkAsync(10, new(), "127.0.0.1"))).StatusCode);
        Assert.Empty(db.SavedPaymentMethods);
        Assert.Empty(db.VnPayCardLinks);
    }

    [Fact]
    public async Task Successful_callback_encrypts_token_and_retains_only_card_last_four_digits()
    {
        await using var db = await Fixture();
        const string originalPan = "9704198526191432198";
        var linked = await Link(db, card: originalPan);
        var method = Assert.Single(db.SavedPaymentMethods);
        Assert.NotEqual(VnPayTokenProtocolTests.Token, method.TokenCiphertext);
        Assert.False(string.IsNullOrWhiteSpace(method.TokenCiphertext));
        Assert.Matches("^[A-Fa-f0-9]{64}$", method.TokenFingerprint);
        Assert.EndsWith("2198", method.MaskedCardNumber);
        Assert.DoesNotContain("970419", method.MaskedCardNumber);
        var decrypted = await Cards(db).GetTokenAsync(10, linked.Id);
        Assert.Equal(VnPayTokenProtocolTests.Token, decrypted.Token);
        var list = await Cards(db).ListAsync(10);
        var serialized = JsonSerializer.Serialize(list);
        Assert.DoesNotContain(originalPan, serialized);
        Assert.DoesNotContain(VnPayTokenProtocolTests.Token, serialized);
        Assert.DoesNotContain(method.TokenCiphertext!, serialized);
        Assert.DoesNotContain(method.TokenFingerprint, serialized);
        Assert.DoesNotContain("TokenCiphertext", serialized);
        Assert.Equal(linked.Id, Assert.Single(list.Methods).Id);
        Assert.Empty(db.PaymentTransactions);
    }

    [Fact]
    public async Task Exact_callback_replay_is_idempotent_and_cannot_resurrect_a_locally_unlinked_method()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        var ciphertext = db.SavedPaymentMethods.Single().TokenCiphertext;
        Assert.True(await Cards(db).CompleteLinkAsync(linked.Callback));
        Assert.Single(db.SavedPaymentMethods);
        Assert.Equal(ciphertext, db.SavedPaymentMethods.Single().TokenCiphertext);
        await Cards(db).UnlinkAsync(10, linked.Id);
        Assert.True(await Cards(db).CompleteLinkAsync(linked.Callback));
        Assert.False(db.SavedPaymentMethods.Single().IsActive);
        Assert.Null(db.SavedPaymentMethods.Single().TokenCiphertext);
        Assert.Empty((await Cards(db).ListAsync(10)).Methods);
    }

    [Fact]
    public async Task Signed_browser_and_ipn_optional_metadata_differences_keep_one_link_and_do_not_restore_unlinked_cards()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        var ciphertext = db.SavedPaymentMethods.Single().TokenCiphertext;
        var fields = linked.Callback.Where(p => p.Key != "vnp_secure_hash").ToDictionary(p => p.Key, p => p.Value.ToString());
        fields.Remove("vnp_pay_date");
        fields["vnp_txn_desc"] = "Browser return with optional metadata";
        fields["vnp_bank_tran_no"] = "different-optional-reference";
        Assert.True(await Cards(db).CompleteLinkAsync(VnPayTokenProtocolTests.Callback(fields)));
        Assert.Single(db.SavedPaymentMethods);
        Assert.Equal(ciphertext, db.SavedPaymentMethods.Single().TokenCiphertext);
        await Cards(db).UnlinkAsync(10, linked.Id);
        Assert.True(await Cards(db).CompleteLinkAsync(VnPayTokenProtocolTests.Callback(fields)));
        Assert.False(db.SavedPaymentMethods.Single().IsActive);
        Assert.Null(db.SavedPaymentMethods.Single().TokenCiphertext);
    }

    [Fact]
    public async Task Shared_token_ipn_routes_verified_link_notifications_without_creating_a_payment()
    {
        await using var db = await Fixture();
        var intent = await Cards(db).CreateLinkAsync(10, new() { CardType = "01" }, "127.0.0.1");
        var controller = new RoomRental.BackEnd.Controllers.VnPayTokenPaymentController(PaymentIpnTests.Service(db),
            VnPayTokenProtocolTests.Config(), NullLogger<RoomRental.BackEnd.Controllers.VnPayTokenPaymentController>.Instance, Cards(db))
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Query = VnPayTokenProtocolTests.Callback(VnPayTokenProtocolTests.LinkFields(intent.Reference));
        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(await controller.Ipn());
        Assert.Equal("00", Assert.IsType<VnPayIpnResponseDto>(result.Value).RspCode);
        Assert.Single(db.SavedPaymentMethods);
        Assert.Empty(db.PaymentTransactions);
    }

    [Fact]
    public async Task A_conflicting_signed_replay_cannot_replace_the_stored_card()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        var fields = linked.Callback.Where(x => x.Key != "vnp_secure_hash").ToDictionary(x => x.Key, x => x.Value.ToString());
        fields["vnp_token"] = new string('B', 64);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CompleteLinkAsync(VnPayTokenProtocolTests.Callback(fields)))).StatusCode);
        Assert.Equal(VnPayTokenProtocolTests.Token, (await Cards(db).GetTokenAsync(10, linked.Id)).Token);
    }

    [Theory]
    [InlineData("vnp_app_user_id", "40")]
    [InlineData("vnp_tmn_code", "OTHER123")]
    [InlineData("vnp_command", "token_pay")]
    [InlineData("vnp_card_type", "02")]
    [InlineData("vnp_txn_ref", "UNKNOWNREF")]
    [InlineData("vnp_token", "invalid")]
    [InlineData("vnp_bank_code", "NCB<script>")]
    [InlineData("vnp_card_number", "no-card-identification")]
    public async Task Signed_callback_still_must_match_the_server_intention_and_valid_metadata(string key, string value)
    {
        await using var db = await Fixture();
        var intention = await Cards(db).CreateLinkAsync(10, new(), "127.0.0.1");
        var fields = VnPayTokenProtocolTests.LinkFields(intention.Reference);
        fields[key] = value;
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CompleteLinkAsync(VnPayTokenProtocolTests.Callback(fields)));
        db.ChangeTracker.Clear();
        Assert.Empty(db.SavedPaymentMethods);
        Assert.Equal(VnPayCardLinkStatus.Pending, db.VnPayCardLinks.Single().Status);
        Assert.Null(db.VnPayCardLinks.Single().CompletedAtUtc);
    }

    [Theory]
    [InlineData("00", "24")]
    [InlineData("24", "00")]
    [InlineData("24", "24")]
    public async Task Both_provider_statuses_must_succeed_before_saving_a_card(string response, string status)
    {
        await using var db = await Fixture();
        var intention = await Cards(db).CreateLinkAsync(10, new(), "127.0.0.1");
        var fields = VnPayTokenProtocolTests.LinkFields(intention.Reference);
        fields["vnp_response_code"] = response;
        fields["vnp_transaction_status"] = status;
        Assert.False(await Cards(db).CompleteLinkAsync(VnPayTokenProtocolTests.Callback(fields)));
        Assert.Empty(db.SavedPaymentMethods);
        Assert.Equal(VnPayCardLinkStatus.Failed, db.VnPayCardLinks.Single().Status);
        Assert.Empty(db.PaymentTransactions);
    }

    [Fact]
    public async Task An_expired_link_intention_cannot_add_a_card_even_with_a_valid_provider_signature()
    {
        await using var db = await Fixture();
        var intention = await Cards(db).CreateLinkAsync(10, new(), "127.0.0.1");
        db.VnPayCardLinks.Single().ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CompleteLinkAsync(
            VnPayTokenProtocolTests.Callback(VnPayTokenProtocolTests.LinkFields(intention.Reference))))).StatusCode);
        Assert.Empty(db.SavedPaymentMethods);
    }

    [Fact]
    public async Task Disabling_a_tenant_after_redirect_prevents_callback_binding()
    {
        await using var db = await Fixture();
        var intention = await Cards(db).CreateLinkAsync(10, new(), "127.0.0.1");
        db.Users.Single(x => x.Id == 10).IsActive = false;
        await db.SaveChangesAsync();
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CompleteLinkAsync(
            VnPayTokenProtocolTests.Callback(VnPayTokenProtocolTests.LinkFields(intention.Reference))))).StatusCode);
        Assert.Empty(db.SavedPaymentMethods);
    }

    [Fact]
    public async Task Another_tenant_cannot_list_decrypt_unlink_or_claim_the_same_provider_token()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        Assert.Empty((await Cards(db).ListAsync(40)).Methods);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).GetTokenAsync(40, linked.Id))).StatusCode);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).UnlinkAsync(40, linked.Id))).StatusCode);
        var otherIntention = await Cards(db).CreateLinkAsync(40, new(), "127.0.0.1");
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CompleteLinkAsync(
            VnPayTokenProtocolTests.Callback(VnPayTokenProtocolTests.LinkFields(otherIntention.Reference, "40"))))).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(10, Assert.Single(db.SavedPaymentMethods).UserId);
    }

    [Fact]
    public async Task Lost_protection_keys_and_rotated_merchant_fail_closed_without_returning_token_material()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db, protection: new EphemeralDataProtectionProvider())
            .GetTokenAsync(10, linked.Id))).StatusCode);
        var rotated = VnPayTokenProtocolTests.Config(changes: new Dictionary<string, string?> { ["VnPay:Tokenization:TmnCode"] = "OTHER123" });
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db, rotated).GetTokenAsync(10, linked.Id))).StatusCode);
    }

    [Fact]
    public async Task Corrupt_token_storage_cannot_be_used_for_payment()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        db.SavedPaymentMethods.Single().TokenCiphertext = "corrupted-storage";
        await db.SaveChangesAsync();
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).GetTokenAsync(10, linked.Id))).StatusCode);
    }

    [Fact]
    public async Task Pending_payment_prevents_unlink_then_local_unlink_erases_token_and_preserves_historical_binding()
    {
        await using var db = await Fixture();
        var linked = await Link(db);
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            DepositId = 1, TargetType = "Deposit", OrderId = "saved-pending", Amount = 1_000_000,
            Status = PaymentTransactionStatus.Pending, SavedPaymentMethodId = linked.Id
        });
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).UnlinkAsync(10, linked.Id))).StatusCode);
        Assert.True(db.SavedPaymentMethods.Single().IsActive);
        db.PaymentTransactions.Single().Status = PaymentTransactionStatus.Expired;
        await db.SaveChangesAsync();
        await Cards(db, VnPayTokenProtocolTests.Config(enabled: false)).UnlinkAsync(10, linked.Id);
        await Cards(db, VnPayTokenProtocolTests.Config(enabled: false)).UnlinkAsync(10, linked.Id);
        Assert.False(db.SavedPaymentMethods.Single().IsActive);
        Assert.Null(db.SavedPaymentMethods.Single().TokenCiphertext);
        Assert.NotNull(db.SavedPaymentMethods.Single().UnlinkedAtUtc);
        Assert.Single(db.PaymentTransactions);
        Assert.True(await Cards(db).ValidateBindingAsync(10, linked.Id, VnPayTokenProtocolTests.Merchant, VnPayTokenProtocolTests.Token));
        Assert.False(await Cards(db).ValidateBindingAsync(40, linked.Id, VnPayTokenProtocolTests.Merchant, VnPayTokenProtocolTests.Token));
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).GetTokenAsync(10, linked.Id))).StatusCode);
    }

    [Fact]
    public async Task Repeated_pending_link_requests_are_bounded_and_invalid_card_types_create_no_rows()
    {
        await using var db = await Fixture();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CreateLinkAsync(10, new() { CardType = "03" }, "127.0.0.1"));
        Assert.Empty(db.VnPayCardLinks);
        for (var i = 0; i < 3; i++) await Cards(db).CreateLinkAsync(10, new(), "127.0.0.1");
        Assert.Equal(429, (await Assert.ThrowsAsync<BusinessRuleException>(() => Cards(db).CreateLinkAsync(10, new(), "127.0.0.1"))).StatusCode);
        Assert.Equal(3, db.VnPayCardLinks.Count());
    }
}
