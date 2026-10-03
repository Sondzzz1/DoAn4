using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class PaymentBLL : IPaymentService
{
    private const string DepositTarget = "Deposit";
    private const string MonthlyBillTarget = "MonthlyBill";
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentBLL> _logger;

    public PaymentBLL(
        ApplicationDbContext context,
        IConfiguration config,
        INotificationService notificationService,
        ILogger<PaymentBLL> logger)
    {
        _context = context;
        _config = config;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto dto, string clientIp)
    {
        var vnpaySettings = GetVnPaySettings();
        var hasDeposit = dto.DepositId is > 0;
        var hasBill = dto.MonthlyBillId is > 0;
        if (hasDeposit == hasBill)
            throw new BusinessRuleException("Phải chọn đúng một đối tượng thanh toán: khoản cọc hoặc hóa đơn tháng.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Deposit? deposit = null;
        MonthlyBill? bill = null;
        decimal amount;
        string targetType;
        string orderInfo;

        if (hasDeposit)
        {
            deposit = await _context.Deposits.FirstOrDefaultAsync(d => d.Id == dto.DepositId)
                ?? throw BusinessRuleException.NotFound("Không tìm thấy khoản đặt cọc.");
            if (deposit.TenantAccountId != userId)
                throw BusinessRuleException.Forbidden("Bạn không có quyền thanh toán khoản đặt cọc này.");
            if (deposit.Status != DepositStatus.Pending)
                throw BusinessRuleException.Conflict("Chỉ khoản cọc đang chờ thanh toán mới có thể thanh toán.");
            if (await _context.PaymentTransactions.AnyAsync(p => p.DepositId == deposit.Id &&
                (p.Status == PaymentTransactionStatus.Pending || p.Status == PaymentTransactionStatus.Succeeded)))
                throw BusinessRuleException.Conflict("Khoản cọc đã có giao dịch đang xử lý hoặc đã thành công.");

            amount = deposit.Amount;
            targetType = DepositTarget;
            orderInfo = dto.OrderInfo ?? $"Thanh toan tien coc ID {deposit.Id}";
        }
        else
        {
            bill = await _context.MonthlyBills.Include(b => b.Contract)
                .FirstOrDefaultAsync(b => b.Id == dto.MonthlyBillId)
                ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn tháng.");
            if (bill.Contract.TenantAccountId != userId)
                throw BusinessRuleException.Forbidden("Bạn không có quyền thanh toán hóa đơn này.");
            if (bill.Status is not (MonthlyBillStatus.Unpaid or MonthlyBillStatus.Overdue))
                throw BusinessRuleException.Conflict("Hóa đơn không ở trạng thái có thể thanh toán.");
            if (await _context.PaymentTransactions.AnyAsync(p => p.MonthlyBillId == bill.Id &&
                (p.Status == PaymentTransactionStatus.Pending || p.Status == PaymentTransactionStatus.Succeeded)))
                throw BusinessRuleException.Conflict("Hóa đơn đã có giao dịch đang xử lý hoặc đã thanh toán.");

            amount = bill.TotalAmount;
            targetType = MonthlyBillTarget;
            orderInfo = dto.OrderInfo ?? $"Thanh toan hoa don thang {bill.Month}/{bill.Year} ID {bill.Id}";
            bill.Status = MonthlyBillStatus.PendingPayment;
            bill.UpdatedAt = DateTime.Now;
        }

        var orderId = $"{(targetType == DepositTarget ? "D" : "B")}_{(deposit?.Id ?? bill!.Id)}_{DateTime.UtcNow.Ticks}";
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            DepositId = deposit?.Id,
            MonthlyBillId = bill?.Id,
            TargetType = targetType,
            OrderId = orderId,
            Amount = amount,
            Status = PaymentTransactionStatus.Pending,
            OrderInfo = orderInfo
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var vnpay = new VnPayHelper();
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnpaySettings.TmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString(CultureInfo.InvariantCulture));
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(clientIp) || clientIp == "::1" ? "127.0.0.1" : clientIp);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", vnpaySettings.ReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", orderId);

        var paymentUrl = vnpay.CreateRequestUrl(
            vnpaySettings.BaseUrl,
            vnpaySettings.HashSecret);
        return new PaymentResponseDto { PaymentUrl = paymentUrl, OrderId = orderId, Amount = amount };
    }

    public async Task<PaymentResultDto> ProcessPaymentReturnAsync(IQueryCollection query)
    {
        var vnpay = new VnPayHelper();
        foreach (var (key, value) in query)
            if (key.StartsWith("vnp_", StringComparison.Ordinal)) vnpay.AddResponseData(key, value.ToString());

        var orderId = vnpay.GetResponseData("vnp_TxnRef");
        var transactionCode = vnpay.GetResponseData("vnp_TransactionNo");
        var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var hashSecret = GetVnPaySettings().HashSecret;
        if (!vnpay.ValidateSignature(query["vnp_SecureHash"].ToString(), hashSecret))
            return Failure("Chữ ký VNPay không hợp lệ.", orderId, transactionCode, responseCode);

        var payment = await PaymentQuery().FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (payment == null)
            return Failure("Không tìm thấy giao dịch thanh toán tương ứng.", orderId, transactionCode, responseCode);
        if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), out var rawAmount) || rawAmount != (long)(payment.Amount * 100))
            return Failure("Số tiền VNPay trả về không khớp với giao dịch.", orderId, transactionCode, responseCode, payment);
        if (payment.Status == PaymentTransactionStatus.Succeeded)
            return Success(payment, "Giao dịch đã được xử lý thành công trước đó.");

        if (responseCode != "00" || (!string.IsNullOrWhiteSpace(transactionStatus) && transactionStatus != "00"))
        {
            payment.Status = PaymentTransactionStatus.Failed;
            payment.ResponseCode = responseCode;
            payment.TransactionCode = string.IsNullOrWhiteSpace(transactionCode) ? null : transactionCode;
            payment.ProcessedAt = DateTime.Now;
            RestoreBillAfterFailedPayment(payment);
            await _context.SaveChangesAsync();
            return Failure($"Giao dịch chưa thành công (mã: {responseCode}).", orderId, transactionCode, responseCode, payment);
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        payment = await PaymentQuery().FirstAsync(p => p.OrderId == orderId);
        if (payment.Status == PaymentTransactionStatus.Succeeded)
        {
            await dbTransaction.CommitAsync();
            return Success(payment, "Giao dịch đã được xử lý thành công trước đó.");
        }
        if (string.IsNullOrWhiteSpace(transactionCode))
            return Failure("VNPay không trả về mã giao dịch hợp lệ.", orderId, transactionCode, responseCode, payment);
        if (await _context.PaymentTransactions.AnyAsync(p => p.Id != payment.Id && p.TransactionCode == transactionCode))
            return Failure("Mã giao dịch VNPay đã được xử lý.", orderId, transactionCode, responseCode, payment);

        ValidateTargetForSuccess(payment);
        payment.Status = PaymentTransactionStatus.Succeeded;
        payment.TransactionCode = transactionCode;
        payment.ResponseCode = responseCode;
        payment.ProcessedAt = DateTime.Now;

        if (payment.Deposit != null)
        {
            payment.Deposit.Status = DepositStatus.Paid;
            payment.Deposit.PaidAt = DateTime.Now;
        }
        else
        {
            payment.MonthlyBill!.Status = MonthlyBillStatus.Paid;
            payment.MonthlyBill.PaidAt = DateTime.Now;
            payment.MonthlyBill.PaymentMethod = "VNPay";
            payment.MonthlyBill.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        await SendSuccessNotificationsAsync(payment);
        return Success(payment, payment.TargetType == DepositTarget
            ? "Giao dịch thanh toán đặt cọc thành công."
            : "Giao dịch thanh toán hóa đơn thành công.");
    }

    private IQueryable<PaymentTransaction> PaymentQuery() => _context.PaymentTransactions
        .Include(p => p.Deposit)
        .Include(p => p.MonthlyBill).ThenInclude(b => b!.Contract);

    private VnPaySettings GetVnPaySettings() => new(
        GetRequiredVnPaySetting("TmnCode"),
        GetRequiredVnPaySetting("HashSecret"),
        GetRequiredVnPaySetting("BaseUrl"),
        GetRequiredVnPaySetting("ReturnUrl"));

    private string GetRequiredVnPaySetting(string key)
    {
        var value = _config[$"VnPay:{key}"];
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException("Dịch vụ thanh toán chưa sẵn sàng.", StatusCodes.Status503ServiceUnavailable);

        return value;
    }

    private sealed record VnPaySettings(string TmnCode, string HashSecret, string BaseUrl, string ReturnUrl);

    private static void ValidateTargetForSuccess(PaymentTransaction payment)
    {
        if (payment.TargetType == DepositTarget && payment.Deposit != null)
        {
            if (payment.Deposit.Amount != payment.Amount)
                throw BusinessRuleException.Conflict("Số tiền khoản cọc đã thay đổi, giao dịch bị từ chối.");
            if (payment.Deposit.Status != DepositStatus.Pending)
                throw BusinessRuleException.Conflict("Khoản cọc không còn ở trạng thái chờ thanh toán.");
            return;
        }

        if (payment.TargetType == MonthlyBillTarget && payment.MonthlyBill != null)
        {
            if (payment.MonthlyBill.TotalAmount != payment.Amount)
                throw BusinessRuleException.Conflict("Số tiền hóa đơn đã thay đổi, giao dịch bị từ chối.");
            if (payment.MonthlyBill.Status != MonthlyBillStatus.PendingPayment)
                throw BusinessRuleException.Conflict("Hóa đơn không còn ở trạng thái chờ thanh toán.");
            return;
        }

        throw BusinessRuleException.Conflict("Đối tượng thanh toán không hợp lệ.");
    }

    private static void RestoreBillAfterFailedPayment(PaymentTransaction payment)
    {
        if (payment.MonthlyBill?.Status == MonthlyBillStatus.PendingPayment)
            payment.MonthlyBill.Status = payment.MonthlyBill.DueDate < DateTime.Now
                ? MonthlyBillStatus.Overdue
                : MonthlyBillStatus.Unpaid;
    }

    private async Task SendSuccessNotificationsAsync(PaymentTransaction payment)
    {
        try
        {
            if (payment.Deposit != null)
            {
                await _notificationService.CreateNotificationAsync(payment.Deposit.TenantAccountId, "Đặt cọc thành công",
                    $"Bạn đã thanh toán {payment.Amount:N0} VNĐ tiền đặt cọc.", 1, "/tenant/rentals");
                await _notificationService.CreateNotificationAsync(payment.Deposit.LandlordAccountId, "Có khoản cọc mới",
                    $"Khách thuê đã thanh toán {payment.Amount:N0} VNĐ. Vui lòng xác nhận khoản cọc.", 1, "/landlord/contracts");
            }
            else if (payment.MonthlyBill != null)
            {
                await _notificationService.CreateNotificationAsync(payment.MonthlyBill.Contract.TenantAccountId,
                    "Thanh toán hóa đơn thành công", $"Bạn đã thanh toán {payment.Amount:N0} VNĐ qua VNPay.", 1, "/tenant/rentals");
                await _notificationService.CreateNotificationAsync(payment.MonthlyBill.Contract.LandlordAccountId,
                    "Hóa đơn đã được thanh toán", $"Hóa đơn {payment.MonthlyBill.Month}/{payment.MonthlyBill.Year} đã được thanh toán.", 1, "/landlord/contracts");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thanh toán {OrderId} thành công nhưng không gửi được thông báo", payment.OrderId);
        }
    }

    private static PaymentResultDto Success(PaymentTransaction payment, string message) => new()
    {
        Success = true, Message = message, OrderId = payment.OrderId,
        TransactionId = payment.TransactionCode ?? string.Empty, Amount = payment.Amount,
        ResponseCode = payment.ResponseCode ?? "00", DepositId = payment.DepositId,
        MonthlyBillId = payment.MonthlyBillId, ProcessedAt = payment.ProcessedAt,
        TargetType = payment.TargetType
    };

    private static PaymentResultDto Failure(
        string message, string orderId, string transactionCode, string responseCode,
        PaymentTransaction? payment = null) => new()
    {
        Success = false, Message = message, OrderId = orderId, TransactionId = transactionCode,
        Amount = payment?.Amount ?? 0, ResponseCode = responseCode,
        DepositId = payment?.DepositId, MonthlyBillId = payment?.MonthlyBillId,
        ProcessedAt = payment?.ProcessedAt, TargetType = payment?.TargetType ?? string.Empty
    };
}
