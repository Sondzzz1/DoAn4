using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Payment;

namespace RoomRental.BackEnd.Controllers;

[ApiController, Route("api/thanh-toan")]
public class VnPayTokenPaymentController(IPaymentService payments, IConfiguration configuration,
    ILogger<VnPayTokenPaymentController> logger, SavedPaymentMethodBLL? savedCards = null) : ControllerBase
{
    [HttpGet("vnpay-token-return"), AllowAnonymous]
    public async Task<IActionResult> Return()
    {
        try
        {
            var result = await payments.ProcessTokenPaymentReturnAsync(Request.Query, 1);
            return Redirect(QueryHelpers.AddQueryString(FrontendUrl(), new Dictionary<string, string?>
            {
                ["success"] = result.Success ? "true" : "false", ["method"] = "token", ["message"] = result.Message,
                ["orderId"] = result.OrderId, ["transactionId"] = result.TransactionId, ["responseCode"] = result.ResponseCode,
                ["amount"] = result.Amount.ToString(CultureInfo.InvariantCulture), ["depositId"] = result.DepositId?.ToString(),
                ["monthlyBillId"] = result.MonthlyBillId?.ToString(), ["targetType"] = result.TargetType,
                ["processedAt"] = result.ProcessedAt?.ToString("O"), ["requiresReview"] = result.RequiresReview ? "true" : "false",
                ["paymentEventId"] = result.PaymentEventId?.ToString(), ["eventState"] = result.EventState?.ToString()
            }));
        }
        catch (Exception)
        {
            logger.LogError("Unable to process VNPay saved-card payment return.");
            return Redirect(QueryHelpers.AddQueryString(FrontendUrl(), new Dictionary<string, string?>
                { ["success"] = "false", ["method"] = "token", ["message"] = "Chưa thể xác nhận giao dịch. Vui lòng kiểm tra trạng thái hoặc liên hệ hỗ trợ." }));
        }
    }

    [HttpGet("vnpay-token-cancel"), AllowAnonymous]
    public IActionResult Cancel() => Redirect(QueryHelpers.AddQueryString(FrontendUrl(), new Dictionary<string, string?>
        { ["success"] = "false", ["method"] = "token", ["message"] = "Bạn đã rời trang thanh toán. Khoản thu chỉ hoàn tất khi VNPay xác nhận giao dịch." }));

    [HttpGet("vnpay-token-ipn"), AllowAnonymous]
    public async Task<IActionResult> Ipn()
    {
        try
        {
            // VNPay may register one IPN URL for the whole Token merchant. Each service verifies the signature.
            if (Request.Query["vnp_command"].Count == 1 && Request.Query["vnp_command"] == "token_create")
            {
                if (savedCards == null) throw new BusinessRuleException("Liên kết thẻ chưa sẵn sàng.", 503);
                await savedCards.CompleteLinkAsync(Request.Query);
                return Ok(new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" });
            }
            var result = await payments.ProcessTokenPaymentReturnAsync(Request.Query);
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
        catch (Exception)
        {
            logger.LogError("Unable to process VNPay saved-card payment notification.");
            return Ok(new VnPayIpnResponseDto { RspCode = "99", Message = "Unable to process callback" });
        }
    }

    private string FrontendUrl() => configuration["VnPay:FrontendReturnUrl"] ?? "http://localhost:5174/payment/result";
}
