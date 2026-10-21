using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Payment;

namespace RoomRental.BackEnd.Controllers;

[ApiController, Route("api/thanh-toan/tien-mat"), Authorize(Roles = "Tenant,Landlord,Admin")]
public class CashPaymentController(CashPaymentBLL service) : ControllerBase
{
    private int ActorId() => int.TryParse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id > 0
        ? id : throw new BusinessRuleException("Không thể xác thực người dùng.", StatusCodes.Status401Unauthorized);

    [HttpGet]
    public async Task<IActionResult> List() => Ok(ApiResponse<List<CashPaymentDto>>.SuccessResponse(await service.ListAsync(ActorId())));

    [HttpPost("chon"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Select(CashPaymentSelectDto dto) => Ok(ApiResponse<CashPaymentDto>.SuccessResponse(
        await service.SelectAsync(ActorId(), dto), "Đã chọn trả tiền mặt. Khoản thu chỉ hoàn tất sau khi chủ trọ xác nhận thực nhận tiền."));

    [HttpPost("{id:int}/xac-nhan"), Authorize(Roles = "Landlord")]
    public async Task<IActionResult> Confirm(int id, [FromBody] CashPaymentConfirmDto? dto = null) => Ok(ApiResponse<CashPaymentDto>.SuccessResponse(
        await service.ConfirmAsync(ActorId(), id, dto), "Đã ghi nhận phiếu thu tiền mặt."));

    [HttpPost("{id:int}/huy"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Cancel(int id) => Ok(ApiResponse<CashPaymentDto>.SuccessResponse(
        await service.CancelAsync(ActorId(), id), "Đã hủy lựa chọn trả tiền mặt."));
}
