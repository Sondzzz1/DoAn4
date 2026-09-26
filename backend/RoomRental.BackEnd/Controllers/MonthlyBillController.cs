using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Rental;

namespace RoomRental.BackEnd.Controllers;

[ApiController]
[Route("api/hoa-don")]
[Authorize]
public class MonthlyBillController : ControllerBase
{
    private readonly IMonthlyBillService _service;

    public MonthlyBillController(IMonthlyBillService service)
    {
        _service = service;
    }

    /// <summary>
    /// Chủ trọ nhập số điện, nước và tạo hóa đơn hàng tháng
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Landlord,Admin")]
    public async Task<IActionResult> Tao([FromBody] TaoHoaDonDto dto)
    {
        try
        {
            var result = await _service.TaoAsync(GetUserId(), dto);
            return Ok(ApiResponse<HoaDonDto>.SuccessResponse(result, "Tạo hóa đơn điện nước thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<HoaDonDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy danh sách hóa đơn theo hợp đồng thuê
    /// </summary>
    [HttpGet("hop-dong/{hopDongId:int}")]
    public async Task<IActionResult> LayTheoHopDong(int hopDongId)
    {
        try
        {
            var result = await _service.LayTheoHopDongAsync(GetUserId(), hopDongId);
            return Ok(ApiResponse<List<HoaDonDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<HoaDonDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy toàn bộ danh sách hóa đơn của tôi (Người thuê hoặc Chủ trọ)
    /// </summary>
    [HttpGet("cua-toi")]
    public async Task<IActionResult> LayCuaToi([FromQuery] bool? isLandlord = null)
    {
        try
        {
            var isUserLandlord = isLandlord ?? User.IsInRole("Landlord");
            var result = await _service.LayDanhSachCuaToiAsync(GetUserId(), isUserLandlord);
            return Ok(ApiResponse<List<HoaDonDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<HoaDonDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Lấy chi tiết một hóa đơn
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> LayChiTiet(int id)
    {
        try
        {
            var result = await _service.LayChiTietAsync(GetUserId(), id);
            return Ok(ApiResponse<HoaDonDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<HoaDonDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Chủ trọ cập nhật chỉ số điện nước hoặc điều chỉnh hóa đơn
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Landlord,Admin")]
    public async Task<IActionResult> CapNhat(int id, [FromBody] CapNhatHoaDonDto dto)
    {
        try
        {
            var result = await _service.CapNhatAsync(GetUserId(), id, dto);
            return Ok(ApiResponse<HoaDonDto>.SuccessResponse(result, "Cập nhật hóa đơn thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<HoaDonDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xác nhận thanh toán hóa đơn (Người thuê xác nhận hoặc Chủ trọ xác nhận nhận tiền)
    /// </summary>
    [HttpPut("{id:int}/thanh-toan")]
    public async Task<IActionResult> ThanhToan(int id, [FromBody] XacNhanThanhToanHoaDonDto? dto = null)
    {
        try
        {
            var result = await _service.ThanhToanAsync(GetUserId(), id, dto);
            return Ok(ApiResponse<HoaDonDto>.SuccessResponse(result, "Xác nhận thanh toán hóa đơn thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<HoaDonDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Xóa hóa đơn
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Landlord,Admin")]
    public async Task<IActionResult> Xoa(int id)
    {
        try
        {
            await _service.XoaAsync(GetUserId(), id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa hóa đơn thành công"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
