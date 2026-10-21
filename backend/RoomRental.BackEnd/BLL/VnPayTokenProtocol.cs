using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace RoomRental.BackEnd.BLL;

/// <summary>VNPay Token 2.1.0 uses lowercase field names and a separately provisioned merchant profile.</summary>
public sealed class VnPayTokenProtocol(IConfiguration configuration)
{
    private const string Prefix = "VnPay:Tokenization:";
    private const string DisabledMessage = "Tính năng liên kết thẻ chưa được bật cho tài khoản VNPay của hệ thống.";

    public bool IsEnabled => bool.TryParse(configuration[Prefix + "Enabled"], out var enabled) && enabled && TrySettings(out _);
    public string? UnavailableReason => IsEnabled ? null : DisabledMessage;
    public string MerchantCode => Settings().MerchantCode;
    public string FrontendReturnUrl => OptionalUrl("FrontendReturnUrl", "http://localhost:5174/tenant/rentals", false);

    public void EnsureEnabled()
    {
        if (!IsEnabled) throw new BusinessRuleException(DisabledMessage, StatusCodes.Status503ServiceUnavailable);
    }

    public string CreateLinkUrl(string reference, int userId, string cardType, string clientIp)
    {
        EnsureEnabled();
        if (cardType is not ("01" or "02")) throw new BusinessRuleException("Vui lòng chọn loại thẻ nội địa hoặc quốc tế.");
        var fields = CommonFields("token_create", reference, userId, clientIp);
        fields["vnp_card_type"] = cardType;
        fields["vnp_txn_desc"] = "Lien ket the thanh toan";
        fields["vnp_return_url"] = OptionalUrl("ReturnUrl", "http://localhost:5000/api/thanh-toan/the-da-luu/return", false);
        fields["vnp_cancel_url"] = OptionalUrl("CancelUrl", "http://localhost:5000/api/thanh-toan/the-da-luu/cancel", false);
        return SignedUrl(OptionalUrl("CreateUrl", "https://sandbox.vnpayment.vn/token_ui/create-token.html", true), fields);
    }

    public string CreatePaymentUrl(string reference, int userId, string token, decimal amount, string clientIp)
    {
        EnsureEnabled();
        if (!Regex.IsMatch(token, "^[A-Za-z0-9]{64}$", RegexOptions.CultureInvariant))
            throw new BusinessRuleException("Liên kết thẻ không hợp lệ. Vui lòng liên kết lại.");
        var minorAmount = amount * 100m;
        if (amount <= 0 || decimal.Truncate(minorAmount) != minorAmount || minorAmount > 999999999999m)
            throw new BusinessRuleException("Số tiền vượt giới hạn thanh toán bằng thẻ đã liên kết.");
        var fields = CommonFields("token_pay", reference, userId, clientIp);
        fields["vnp_token"] = token;
        fields["vnp_amount"] = minorAmount.ToString("0", CultureInfo.InvariantCulture);
        fields["vnp_curr_code"] = "VND";
        fields["vnp_txn_desc"] = "Thanh toan khoan thue phong";
        fields["vnp_return_url"] = OptionalUrl("PaymentReturnUrl", "http://localhost:5000/api/thanh-toan/vnpay-token-return", false);
        fields["vnp_cancel_url"] = OptionalUrl("PaymentCancelUrl", "http://localhost:5000/api/thanh-toan/vnpay-token-cancel", false);
        return SignedUrl(OptionalUrl("PayUrl", "https://sandbox.vnpayment.vn/token_ui/payment-token.html", true), fields);
    }

    // A feature may be disabled for new requests while previously issued, authenticated callbacks still arrive.
    public IReadOnlyDictionary<string, string> ValidateCallback(IQueryCollection query)
    {
        var settings = Settings();
        if (query.Count > 40) throw InvalidCallback();
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query)
        {
            if (!pair.Key.StartsWith("vnp_", StringComparison.Ordinal)) continue;
            if (pair.Key != pair.Key.ToLowerInvariant() || pair.Value.Count != 1 || pair.Value[0]?.Length > 4096)
                throw InvalidCallback();
            fields.Add(pair.Key, pair.Value[0] ?? "");
        }
        if (!fields.Remove("vnp_secure_hash", out var supplied) || !Regex.IsMatch(supplied, "^[A-Fa-f0-9]{128}$", RegexOptions.CultureInvariant))
            throw InvalidCallback();
        fields.Remove("vnp_secure_hash_type");
        var expected = Sign(fields, settings);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(supplied), Convert.FromHexString(expected)))
            throw InvalidCallback();
        if (!fields.TryGetValue("vnp_tmn_code", out var merchant) || merchant != settings.MerchantCode)
            throw InvalidCallback();
        return fields;
    }

    private SortedDictionary<string, string> CommonFields(string command, string reference, int userId, string ip)
    {
        if (userId <= 0 || !Regex.IsMatch(reference, "^[A-Za-z0-9_-]{1,100}$", RegexOptions.CultureInvariant))
            throw new BusinessRuleException("Yêu cầu thanh toán không hợp lệ.");
        if (!IPAddress.TryParse(ip, out var address)) throw new BusinessRuleException("Địa chỉ truy cập không hợp lệ.");
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        return new(StringComparer.Ordinal)
        {
            ["vnp_version"] = "2.1.0", ["vnp_command"] = command, ["vnp_tmn_code"] = MerchantCode,
            ["vnp_app_user_id"] = userId.ToString(CultureInfo.InvariantCulture), ["vnp_txn_ref"] = reference,
            ["vnp_create_date"] = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_ip_addr"] = address.ToString(), ["vnp_locale"] = "vn"
        };
    }

    private string SignedUrl(string url, SortedDictionary<string, string> fields)
    {
        var query = string.Join("&", fields.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => WebUtility.UrlEncode(p.Key) + "=" + WebUtility.UrlEncode(p.Value)));
        return url + "?" + query + "&vnp_secure_hash=" + Sign(fields, Settings());
    }

    private static string Sign(IEnumerable<KeyValuePair<string, string>> fields, TokenSettings settings)
    {
        string Encode(string value) => settings.UrlEncoded ? WebUtility.UrlEncode(value) : value;
        var canonical = string.Join("&", fields.Where(p => !string.IsNullOrEmpty(p.Value)).OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => Encode(p.Key) + "=" + Encode(p.Value)));
        return Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(settings.Secret), Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private bool TrySettings(out TokenSettings? settings)
    {
        settings = null;
        var merchant = configuration[Prefix + "TmnCode"] ?? "";
        var secret = configuration[Prefix + "HashSecret"] ?? "";
        var encoding = configuration[Prefix + "SignatureEncoding"];
        if (!Regex.IsMatch(merchant, "^[A-Za-z0-9]{8}$", RegexOptions.CultureInvariant) || secret.Length is < 16 or > 512 ||
            Regex.IsMatch(secret, "replace-with|YOUR_|CHANGE_ME", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
            encoding is not ("Raw" or "UrlEncoded")) return false;
        settings = new(merchant, secret, encoding == "UrlEncoded");
        return true;
    }

    private TokenSettings Settings() => TrySettings(out var settings) ? settings! :
        throw new BusinessRuleException(DisabledMessage, StatusCodes.Status503ServiceUnavailable);

    private string OptionalUrl(string name, string fallback, bool provider)
    {
        var value = configuration[Prefix + name] ?? fallback;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) || (uri.Scheme != "https" && (provider || uri.Scheme != "http" || !uri.IsLoopback)) ||
            (provider && !string.IsNullOrEmpty(uri.Query)))
            throw new BusinessRuleException(DisabledMessage, StatusCodes.Status503ServiceUnavailable);
        return uri.AbsoluteUri;
    }

    private static BusinessRuleException InvalidCallback() => new("Không thể xác minh phản hồi liên kết thẻ VNPay.");
    private sealed record TokenSettings(string MerchantCode, string Secret, bool UrlEncoded);
}
