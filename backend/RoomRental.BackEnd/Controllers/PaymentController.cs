using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Payment;
using System.Security.Claims;

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
    /// Tạo URL thanh toán VNPay Sandbox cho khoản đặt cọc
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
            return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
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
            var result = await _paymentService.ProcessPaymentReturnAsync(Request.Query);
            var frontendReturnUrl = _config["VnPay:FrontendReturnUrl"] ?? "http://localhost:5174/payment/result";

            var redirectUrl = $"{frontendReturnUrl}?success={result.Success}&orderId={result.OrderId}&transactionId={result.TransactionId}&amount={result.Amount}&responseCode={result.ResponseCode}&message={Uri.EscapeDataString(result.Message)}&depositId={result.DepositId}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý phản hồi từ VNPay");
            var frontendReturnUrl = _config["VnPay:FrontendReturnUrl"] ?? "http://localhost:5174/payment/result";
            return Redirect($"{frontendReturnUrl}?success=false&message={Uri.EscapeDataString(ex.Message)}");
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
            if (result.Success)
            {
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            return Ok(new { RspCode = "01", Message = "Order not found or invalid signature" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý VNPay IPN");
            return Ok(new { RspCode = "99", Message = "Unknown error" });
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
