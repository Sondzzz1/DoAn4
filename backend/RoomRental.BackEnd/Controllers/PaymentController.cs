using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Payment;
using System.Security.Claims;
using System.Globalization;

namespace RoomRental.BackEnd.Controllers;

[Route("api/thanh-toan")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IConfiguration config,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay Sandbox cho khoản đặt cọc hoặc hóa đơn tháng
    /// </summary>
    [HttpPost("vnpay-tao-url")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _paymentService.CreatePaymentUrlAsync(userId, dto, clientIp);
            return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "Tạo liên kết thanh toán thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo URL thanh toán VNPay");
            return this.BusinessError<PaymentResponseDto>(ex);
        }
    }

    /// <summary>
    /// Endpoint nhận kết quả trả về từ VNPay sau khi khách hàng hoàn tất giao dịch
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentReturn()
    {
        try
        {
            var result = await _paymentService.ProcessPaymentReturnAsync(Request.Query, 1);
            var frontendReturnUrl = _config["VnPay:FrontendReturnUrl"] ?? "http://localhost:5174/payment/result";
            var success = result.Success ? "true" : "false";
            var redirectUrl = $"{frontendReturnUrl}?success={success}&orderId={Uri.EscapeDataString(result.OrderId)}&transactionId={Uri.EscapeDataString(result.TransactionId)}&amount={result.Amount.ToString(CultureInfo.InvariantCulture)}&responseCode={Uri.EscapeDataString(result.ResponseCode)}&message={Uri.EscapeDataString(result.Message)}&depositId={result.DepositId}&monthlyBillId={result.MonthlyBillId}&processedAt={Uri.EscapeDataString(result.ProcessedAt?.ToString("O") ?? string.Empty)}&targetType={Uri.EscapeDataString(result.TargetType)}";

            return Redirect(redirectUrl + $"&requiresReview={(result.RequiresReview ? "true" : "false")}&paymentEventId={result.PaymentEventId}&eventState={result.EventState}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý phản hồi từ VNPay");
            var frontendReturnUrl = _config["VnPay:FrontendReturnUrl"] ?? "http://localhost:5174/payment/result";
            var (_, message) = BusinessErrorHandling.Describe(ex, HttpContext);
            return Redirect($"{frontendReturnUrl}?success=false&message={Uri.EscapeDataString(message)}");
        }
    }

    /// <summary>
    /// IPN Webhook callback từ server VNPay
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentIpn()
    {
        try
        {
            var result = await _paymentService.ProcessPaymentReturnAsync(Request.Query);
            var (code, message) = result.CallbackOutcome switch
            {
                PaymentCallbackOutcome.Recorded => ("00", "Confirm Success"),
                PaymentCallbackOutcome.AlreadyRecorded => ("02", "Order already confirmed"),
                PaymentCallbackOutcome.OrderNotFound => ("01", "Order not found"),
                PaymentCallbackOutcome.InvalidAmount => ("04", "Invalid amount"),
                PaymentCallbackOutcome.InvalidSignature => ("97", "Invalid signature"),
                _ => ("99", "Unable to process callback")
            };
            return Ok(new VnPayIpnResponseDto { RspCode = code, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý VNPay IPN");
            return Ok(new VnPayIpnResponseDto());
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new Exception("Không thể xác thực người dùng");
        }

        return userId;
    }
}
