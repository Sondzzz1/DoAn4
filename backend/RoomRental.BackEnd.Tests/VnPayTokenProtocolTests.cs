using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using RoomRental.BackEnd.BLL;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class VnPayTokenProtocolTests
{
    internal const string Merchant = "TOKENTST";
    internal const string Secret = "unit-test-token-secret-not-for-real-payments-2026";
    internal const string Token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    internal static IConfiguration Config(string encoding = "Raw", bool enabled = true,
        IReadOnlyDictionary<string, string?>? changes = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["VnPay:Tokenization:Enabled"] = enabled.ToString(),
            ["VnPay:Tokenization:TmnCode"] = Merchant,
            ["VnPay:Tokenization:HashSecret"] = Secret,
            ["VnPay:Tokenization:SignatureEncoding"] = encoding
        };
        if (changes != null) foreach (var entry in changes) values[entry.Key] = entry.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    internal static Dictionary<string, string> LinkFields(string reference = "LINKTEST001", string user = "10") => new()
    {
        ["vnp_app_user_id"] = user, ["vnp_tmn_code"] = Merchant,
        ["vnp_txn_ref"] = reference, ["vnp_command"] = "token_create",
        ["vnp_response_code"] = "00", ["vnp_transaction_status"] = "00",
        ["vnp_token"] = Token, ["vnp_card_number"] = "970419xxxxxxxxx2198",
        ["vnp_bank_code"] = "NCB", ["vnp_card_type"] = "01",
        ["vnp_transaction_no"] = "13713726", ["vnp_txn_desc"] = "Tao moi token",
        ["vnp_pay_date"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
    };

    internal static string Sign(IEnumerable<KeyValuePair<string, string>> fields, string encoding = "Raw", string secret = Secret)
    {
        static string Encode(string value, string mode) => mode == "UrlEncoded" ? WebUtility.UrlEncode(value) : value;
        var data = string.Join("&", fields.Where(x => x.Key != "vnp_secure_hash" && !string.IsNullOrEmpty(x.Value))
            .OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Encode(x.Key, encoding) + "=" + Encode(x.Value, encoding)));
        return Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }

    internal static IQueryCollection Callback(Dictionary<string, string>? fields = null, string encoding = "Raw")
    {
        fields ??= LinkFields();
        var values = fields.ToDictionary(x => x.Key, x => new StringValues(x.Value), StringComparer.Ordinal);
        values["vnp_secure_hash"] = Sign(fields, encoding);
        return new QueryCollection(values);
    }

    [Theory]
    [InlineData("Raw")]
    [InlineData("UrlEncoded")]
    public void Link_request_uses_documented_lowercase_protocol_and_independent_sha512_signature(string encoding)
    {
        var protocol = new VnPayTokenProtocol(Config(encoding));
        Assert.True(protocol.IsEnabled);
        var uri = new Uri(protocol.CreateLinkUrl("LINKTEST001", 10, "01", "127.0.0.1"));
        Assert.Equal("https", uri.Scheme);
        Assert.Equal("sandbox.vnpayment.vn", uri.Host);
        Assert.Equal("/token_ui/create-token.html", uri.AbsolutePath);
        var fields = QueryHelpers.ParseQuery(uri.Query).ToDictionary(x => x.Key, x => x.Value.ToString());
        Assert.All(fields.Keys, key => Assert.Equal(key.ToLowerInvariant(), key));
        Assert.Equal("2.1.0", fields["vnp_version"]);
        Assert.Equal("token_create", fields["vnp_command"]);
        Assert.Equal(Merchant, fields["vnp_tmn_code"]);
        Assert.Equal("10", fields["vnp_app_user_id"]);
        Assert.Equal("01", fields["vnp_card_type"]);
        Assert.Equal("LINKTEST001", fields["vnp_txn_ref"]);
        Assert.False(fields.ContainsKey("vnp_token"));
        Assert.False(fields.ContainsKey("vnp_amount"));
        Assert.Matches("^[a-f0-9]{128}$", fields["vnp_secure_hash"]);
        Assert.Equal(Sign(fields, encoding), fields["vnp_secure_hash"]);
    }

    [Theory]
    [InlineData("Raw")]
    [InlineData("UrlEncoded")]
    public void Saved_card_payment_scales_amount_and_uses_the_saved_token(string encoding)
    {
        var protocol = new VnPayTokenProtocol(Config(encoding));
        var uri = new Uri(protocol.CreatePaymentUrl("PAYTEST001", 10, Token, 1_000_000m, "127.0.0.1"));
        Assert.Equal("/token_ui/payment-token.html", uri.AbsolutePath);
        var fields = QueryHelpers.ParseQuery(uri.Query).ToDictionary(x => x.Key, x => x.Value.ToString());
        Assert.Equal("token_pay", fields["vnp_command"]);
        Assert.Equal("100000000", fields["vnp_amount"]);
        Assert.Equal("VND", fields["vnp_curr_code"]);
        Assert.Equal(Token, fields["vnp_token"]);
        Assert.Equal(Sign(fields, encoding), fields["vnp_secure_hash"]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.001")]
    [InlineData("10000000000")]
    public void Invalid_or_unrepresentable_payment_amount_is_rejected_without_truncation(string amount)
    {
        var protocol = new VnPayTokenProtocol(Config());
        Assert.Throws<BusinessRuleException>(() => protocol.CreatePaymentUrl("PAYTEST001", 10, Token,
            decimal.Parse(amount, CultureInfo.InvariantCulture), "127.0.0.1"));
    }

    [Theory]
    [InlineData("Raw")]
    [InlineData("UrlEncoded")]
    public void Callback_accepts_only_its_configured_encoding_and_detects_tampering(string encoding)
    {
        var protocol = new VnPayTokenProtocol(Config(encoding));
        var fields = LinkFields();
        Assert.Equal(Token, protocol.ValidateCallback(Callback(fields, encoding))["vnp_token"]);
        var signed = Callback(fields, encoding).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        signed["vnp_app_user_id"] = "40";
        Assert.Throws<BusinessRuleException>(() => protocol.ValidateCallback(new QueryCollection(signed)));
        var opposite = encoding == "Raw" ? "UrlEncoded" : "Raw";
        Assert.Throws<BusinessRuleException>(() => protocol.ValidateCallback(Callback(fields, opposite)));
    }

    [Theory]
    [InlineData("vnp_app_user_id")]
    [InlineData("vnp_token")]
    [InlineData("vnp_secure_hash")]
    public void Duplicate_query_values_are_never_collapsed_into_one_signed_value(string key)
    {
        var protocol = new VnPayTokenProtocol(Config());
        var fields = Callback().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        fields[key] = new StringValues([fields[key].ToString(), fields[key].ToString()]);
        Assert.Throws<BusinessRuleException>(() => protocol.ValidateCallback(new QueryCollection(fields)));
    }

    [Theory]
    [InlineData("vnp_TmnCode")]
    [InlineData("vnp_ResponseCode")]
    [InlineData("vnp_Command")]
    public void Mixed_case_regular_gateway_fields_cannot_impersonate_a_token_callback(string key)
    {
        var fields = LinkFields();
        fields[key] = "00";
        Assert.Throws<BusinessRuleException>(() => new VnPayTokenProtocol(Config()).ValidateCallback(Callback(fields)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("not-a-sha512-signature")]
    public void Missing_malformed_or_legacy_hashes_are_rejected(string hash)
    {
        var fields = Callback().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        fields["vnp_secure_hash"] = hash;
        Assert.Throws<BusinessRuleException>(() => new VnPayTokenProtocol(Config()).ValidateCallback(new QueryCollection(fields)));
    }

    [Fact]
    public void Existing_signed_callbacks_are_verifiable_when_new_linking_is_disabled()
    {
        var protocol = new VnPayTokenProtocol(Config(enabled: false));
        Assert.False(protocol.IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(protocol.UnavailableReason));
        Assert.Equal(503, Assert.Throws<BusinessRuleException>(() => protocol.EnsureEnabled()).StatusCode);
        Assert.Equal("LINKTEST001", protocol.ValidateCallback(Callback())["vnp_txn_ref"]);
    }

    [Theory]
    [InlineData("TmnCode", null)]
    [InlineData("TmnCode", "short")]
    [InlineData("HashSecret", null)]
    [InlineData("SignatureEncoding", null)]
    [InlineData("SignatureEncoding", "MD5")]
    public void Incomplete_or_unsafe_configuration_cannot_enable_linking(string key, string? value)
    {
        var config = Config(changes: new Dictionary<string, string?> { ["VnPay:Tokenization:" + key] = value });
        var protocol = new VnPayTokenProtocol(config);
        Assert.False(protocol.IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(protocol.UnavailableReason));
        Assert.Equal(503, Assert.Throws<BusinessRuleException>(() => protocol.EnsureEnabled()).StatusCode);
    }

    [Theory]
    [InlineData("CreateUrl")]
    [InlineData("PayUrl")]
    public void Provider_urls_cannot_downgrade_transport_to_plain_http(string key)
    {
        var protocol = new VnPayTokenProtocol(Config(changes: new Dictionary<string, string?>
        {
            ["VnPay:Tokenization:" + key] = "http://sandbox.vnpayment.vn/token_ui/unsafe.html"
        }));
        var error = key == "CreateUrl"
            ? Assert.Throws<BusinessRuleException>(() => protocol.CreateLinkUrl("LINKTEST001", 10, "01", "127.0.0.1"))
            : Assert.Throws<BusinessRuleException>(() => protocol.CreatePaymentUrl("PAYTEST001", 10, Token, 1_000_000m, "127.0.0.1"));
        Assert.Equal(503, error.StatusCode);
    }

    [Fact]
    public void Existing_regular_payment_credentials_do_not_implicitly_enable_saved_cards()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VnPay:TmnCode"] = Merchant, ["VnPay:HashSecret"] = Secret
        }).Build();
        Assert.False(new VnPayTokenProtocol(config).IsEnabled);
    }
}
