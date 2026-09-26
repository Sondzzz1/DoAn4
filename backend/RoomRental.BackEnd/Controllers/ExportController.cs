using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL.Interfaces;

namespace RoomRental.BackEnd.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// Xuất file PDF Hợp đồng thuê phòng trọ theo mẫu chuẩn pháp lý Việt Nam
    /// </summary>
    [HttpGet("hop-dong/{id:int}/xuat-pdf")]
    [HttpGet("xuat-du-lieu/hop-dong/{id:int}/pdf")]
    public async Task<IActionResult> XuatHopDongPdf(int id)
    {
        try
        {
            var pdfBytes = await _exportService.XuatHopDongPdfAsync(GetUserId(), id);
            return File(pdfBytes, "application/pdf", $"HopDongThuePhong_HD{id:D4}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Xuất báo cáo doanh thu & hóa đơn điện nước ra Excel (.xlsx) cho Chủ trọ hoặc Admin
    /// </summary>
    [HttpGet("bao-cao/doanh-thu/xuat-excel")]
    [HttpGet("xuat-du-lieu/doanh-thu/excel")]
    [Authorize(Roles = "Landlord,Admin")]
    public async Task<IActionResult> XuatBaoCaoDoanhThuExcel([FromQuery] int? nam = null, [FromQuery] int? thang = null)
    {
        try
        {
            var isAdmin = User.IsInRole("Admin");
            var excelBytes = await _exportService.XuatBaoCaoDoanhThuExcelAsync(GetUserId(), isAdmin, nam, thang);
            var fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
