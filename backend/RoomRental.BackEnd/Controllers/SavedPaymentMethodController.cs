using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Payment;

namespace RoomRental.BackEnd.Controllers;

[ApiController, Route("api/thanh-toan/the-da-luu"), Authorize(Roles = "Tenant")]
public class SavedPaymentMethodController(SavedPaymentMethodBLL service, VnPayTokenProtocol protocol,
    ILogger<SavedPaymentMethodController> logger) : ControllerBase
{
    private int ActorId() => int.TryParse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id > 0
        ? id : throw new BusinessRuleException("Không thể xác thực người dùng.", StatusCodes.Status401Unauthorized);

    [HttpGet]
    public async Task<IActionResult> List() => Ok(ApiResponse<SavedPaymentMethodsDto>.SuccessResponse(await service.ListAsync(ActorId())));

    [HttpPost("lien-ket")]
    public async Task<IActionResult> Link(VnPayCardLinkRequestDto dto) => Ok(ApiResponse<VnPayCardLinkResponseDto>.SuccessResponse(
        await service.CreateLinkAsync(ActorId(), dto, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"),
        "Tiếp tục liên kết thẻ trên cổng VNPay."));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Unlink(int id)
    {
        await service.UnlinkAsync(ActorId(), id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Đã gỡ thẻ khỏi ứng dụng."));
    }

    [HttpGet("return"), AllowAnonymous]
    public async Task<IActionResult> Return()
    {
        var result = "failed";
        try { result = await service.CompleteLinkAsync(Request.Query) ? "success" : "failed"; }
        catch (BusinessRuleException) { logger.LogWarning("Rejected VNPay card-link browser callback."); }
        catch (Exception) { logger.LogError("Unable to process VNPay card-link browser callback."); }
        return Redirect(FrontendResult(result));
    }

    [HttpGet("cancel"), AllowAnonymous]
    public IActionResult Cancel() => Redirect(FrontendResult("cancelled"));

    [HttpGet("ipn"), AllowAnonymous]
    public async Task<IActionResult> Ipn()
    {
        try
        {
            await service.CompleteLinkAsync(Request.Query);
            return Ok(new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" });
        }
        catch (BusinessRuleException e)
        {
            logger.LogWarning("Rejected VNPay card-link notification with HTTP status {Status}", e.StatusCode);
            return Ok(new VnPayIpnResponseDto { RspCode = "99", Message = "Unable to confirm card link" });
        }
        catch (Exception)
        {
            logger.LogError("Unable to process VNPay card-link notification.");
            return Ok(new VnPayIpnResponseDto { RspCode = "99", Message = "Unable to confirm card link" });
        }
    }

    private string FrontendResult(string result) => protocol.FrontendReturnUrl +
        (protocol.FrontendReturnUrl.Contains('?') ? "&" : "?") + "cardLink=" + result;
}
