using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.Application.DTOs.Common;
using RoomRental.Application.DTOs.Rental;
using RoomRental.Application.Interfaces;

namespace RoomRental.API.Controllers;

[ApiController]
[Route("api/yeu-cau-thue")]
[Authorize]
public class RentalRequestController : ControllerBase
{
    private readonly IRentalRequestService _service;
    public RentalRequestController(IRentalRequestService service) => _service = service;
    [HttpPost] public async Task<IActionResult> Tao([FromBody] TaoYeuCauThueDto dto) => Ok(ApiResponse<YeuCauThueDto>.SuccessResponse(await _service.TaoAsync(Id(), dto), "Gửi yêu cầu thuê phòng thành công"));
    [HttpGet("cua-toi")] public async Task<IActionResult> CuaToi() => Ok(ApiResponse<List<YeuCauThueDto>>.SuccessResponse(await _service.LayCuaToiAsync(Id(), false)));
    [HttpGet("chu-tro")][Authorize(Roles="Landlord,Admin")] public async Task<IActionResult> CuaChuTro() => Ok(ApiResponse<List<YeuCauThueDto>>.SuccessResponse(await _service.LayCuaToiAsync(Id(), true)));
    [HttpPut("{id:int}/trang-thai")][Authorize(Roles="Landlord,Admin")] public async Task<IActionResult> TrangThai(int id, [FromBody] CapNhatTrangThaiDto dto) => Ok(ApiResponse<YeuCauThueDto>.SuccessResponse(await _service.CapNhatTrangThaiAsync(Id(), id, dto.TrangThai)));
    private int Id() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

[ApiController]
[Route("api/dat-coc")]
[Authorize]
public class DepositController : ControllerBase
{
    private readonly IDepositService _service;
    public DepositController(IDepositService service) => _service = service;
    [HttpPost] public async Task<IActionResult> Tao([FromBody] TaoDatCocDto dto) => Ok(ApiResponse<DatCocDto>.SuccessResponse(await _service.TaoAsync(Id(), dto), "Tạo thông tin đặt cọc thành công"));
    [HttpGet("cua-toi")] public async Task<IActionResult> CuaToi() => Ok(ApiResponse<List<DatCocDto>>.SuccessResponse(await _service.LayCuaToiAsync(Id())));
    [HttpPut("{id:int}/trang-thai")][Authorize(Roles="Landlord,Admin")] public async Task<IActionResult> TrangThai(int id, [FromBody] CapNhatTrangThaiDto dto) => Ok(ApiResponse<DatCocDto>.SuccessResponse(await _service.CapNhatTrangThaiAsync(Id(), id, dto.TrangThai)));
    private int Id() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

[ApiController]
[Route("api/hop-dong")]
[Authorize]
public class ContractController : ControllerBase
{
    private readonly IRentalContractService _service;
    public ContractController(IRentalContractService service) => _service = service;
    [HttpPost][Authorize(Roles="Landlord,Admin")] public async Task<IActionResult> Tao([FromBody] TaoHopDongDto dto) => Ok(ApiResponse<HopDongDto>.SuccessResponse(await _service.TaoAsync(Id(), dto), "Tạo hợp đồng thành công"));
    [HttpGet("cua-toi")] public async Task<IActionResult> CuaToi() => Ok(ApiResponse<List<HopDongDto>>.SuccessResponse(await _service.LayCuaToiAsync(Id())));
    [HttpPut("{id:int}/xac-nhan")] public async Task<IActionResult> XacNhan(int id) => Ok(ApiResponse<HopDongDto>.SuccessResponse(await _service.XacNhanAsync(Id(), id), "Xác nhận hợp đồng thành công"));
    private int Id() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

[ApiController]
[Route("api/su-co")]
[Authorize]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _service;
    public IncidentController(IIncidentService service) => _service = service;
    [HttpPost] public async Task<IActionResult> Tao([FromBody] TaoSuCoDto dto) => Ok(ApiResponse<SuCoDto>.SuccessResponse(await _service.TaoAsync(Id(), dto), "Báo cáo sự cố thành công"));
    [HttpGet("cua-toi")] public async Task<IActionResult> CuaToi() => Ok(ApiResponse<List<SuCoDto>>.SuccessResponse(await _service.LayCuaToiAsync(Id())));
    [HttpPut("{id:int}/xu-ly")][Authorize(Roles="Landlord,Admin")] public async Task<IActionResult> XuLy(int id, [FromBody] XuLySuCoDto dto) => Ok(ApiResponse<SuCoDto>.SuccessResponse(await _service.XuLyAsync(Id(), id, dto), "Xử lý sự cố thành công"));
    private int Id() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

[ApiController]
[Route("api/danh-gia")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IRoomReviewService _service;
    public ReviewController(IRoomReviewService service) => _service = service;
    [HttpPost] public async Task<IActionResult> Tao([FromBody] TaoDanhGiaDto dto) => Ok(ApiResponse<DanhGiaDto>.SuccessResponse(await _service.TaoAsync(Id(), dto), "Đánh giá phòng thành công"));
    [HttpGet("bai-dang/{baiDangId:int}")][AllowAnonymous] public async Task<IActionResult> TheoBaiDang(int baiDangId) => Ok(ApiResponse<List<DanhGiaDto>>.SuccessResponse(await _service.LayTheoBaiDangAsync(baiDangId)));
    [HttpGet("cua-toi")] public async Task<IActionResult> CuaToi() => Ok(ApiResponse<List<DanhGiaDto>>.SuccessResponse(await _service.LayCuaToiAsync(Id())));
    private int Id() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
