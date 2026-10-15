using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Payment;

namespace RoomRental.BackEnd.Controllers;

[ApiController, Route("api/hoan-coc"), Authorize(Roles = "Tenant,Landlord,Admin")]
public class DepositRefundController(DepositRefundBLL service) : ControllerBase
{
    private int Id() => int.TryParse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id > 0
        ? id : throw new BusinessRuleException("Không thể xác thực người dùng.", 401);
    [HttpGet] public async Task<IActionResult> List() => Ok(ApiResponse<List<DepositRefundDto>>.SuccessResponse(await service.ListAsync(Id())));
    [HttpPost, Authorize(Roles = "Tenant")] public async Task<IActionResult> Create(RefundCreateDto dto) => Ok(ApiResponse<DepositRefundDto>.SuccessResponse(await service.CreateAsync(Id(), dto)));
    [HttpPut("{id:int}/{operation}")] public async Task<IActionResult> Act(int id, string operation, RefundActionDto dto) => Ok(ApiResponse<DepositRefundDto>.SuccessResponse(await service.ActAsync(Id(), id, operation, dto)));
}

[ApiController, Route("api/thanh-toan/su-kien"), Authorize(Roles = "Tenant,Landlord,Admin")]
public class PaymentEventController(PaymentEventBLL service) : ControllerBase
{
    private int Id() => int.TryParse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id > 0
        ? id : throw new BusinessRuleException("Không thể xác thực người dùng.", 401);
    [HttpGet] public async Task<IActionResult> List([FromQuery] int page = 1) => Ok(ApiResponse<List<PaymentEventDto>>.SuccessResponse(await service.ListAsync(Id(), page)));
    [HttpPut("{id:long}/doi-soat"), Authorize(Roles = "Admin")] public async Task<IActionResult> Resolve(long id, RefundActionDto dto) => Ok(ApiResponse<PaymentEventDto>.SuccessResponse(await service.ResolveAsync(Id(), id, dto)));
}
