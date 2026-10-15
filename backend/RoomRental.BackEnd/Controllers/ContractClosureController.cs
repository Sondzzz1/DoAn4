using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Rental;

namespace RoomRental.BackEnd.Controllers;

[ApiController, Authorize, Route("api/hop-dong/{contractId:int}/ket-thuc")]
public class ContractClosureController(ContractClosureBLL service) : ControllerBase
{
    private int AccountId() => int.TryParse(User.FindFirst("UserId")?.Value, out var id) && id > 0 ? id : throw BusinessRuleException.Forbidden("Thiếu tài khoản hợp lệ.");
    [HttpGet]
    public async Task<IActionResult> Get(int contractId) => Ok(ApiResponse<ContractClosureView>.SuccessResponse(await service.GetAsync(AccountId(), contractId)));
    [HttpPost("{operation}")]
    public async Task<IActionResult> Execute(int contractId, string operation, [FromBody] ClosureCommandDto dto) =>
        Ok(ApiResponse<ContractClosureView>.SuccessResponse(await service.ExecuteAsync(AccountId(), contractId, operation, dto), "Đã cập nhật hồ sơ kết thúc hợp đồng."));
}
