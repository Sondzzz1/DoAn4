using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.BLL;

public class ExportBLL : IExportService
{
    private readonly ApplicationDbContext _db;

    static ExportBLL()
    {
        // Thiết lập bản quyền Community của QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ExportBLL(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> XuatHopDongPdfAsync(int taiKhoanId, int hopDongId)
    {
        var contract = await _db.RentalContracts
            .Include(c => c.Post)
                .ThenInclude(p => p.Room)
            .FirstOrDefaultAsync(c => c.Id == hopDongId && (c.TenantAccountId == taiKhoanId || c.LandlordAccountId == taiKhoanId || taiKhoanId == 0))
            ?? throw new Exception("Không tìm thấy hợp đồng hoặc bạn không có quyền xem.");

        var landlord = await _db.Users.FirstOrDefaultAsync(u => u.Id == contract.LandlordAccountId);
        var tenant = await _db.Users.FirstOrDefaultAsync(u => u.Id == contract.TenantAccountId);
        var post = contract.Post ?? await _db.Posts.Include(p => p.Room).FirstOrDefaultAsync(p => p.Id == contract.PostId);
        var room = post?.Room;

        var deposit = await _db.Deposits.FirstOrDefaultAsync(d => d.RentalRequestId == contract.RentalRequestId && d.Status == 1);
        var depositAmount = deposit?.Amount ?? contract.MonthlyRent;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(12);
                    col.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(11);
                    col.Item().AlignCenter().Text("---------------------------------").FontSize(9);
                    col.Item().PaddingTop(10).AlignCenter().Text("HỢP ĐỒNG THUÊ PHÒNG TRỌ").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text($"Số: HD-{contract.Id:D4}/{contract.CreatedAt.Year} | Hệ thống PMS RoomRental").Italic().FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text("Hôm nay, ngày " + DateTime.Now.ToString("dd/MM/yyyy") + ", tại hệ thống Quản lý phòng trọ RoomRentalSystem, hai bên gồm có:").Italic();

                    // Bên A
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                    {
                        c.Item().Text("BÊN CHO THUÊ (BÊN A - CHỦ TRỌ):").Bold().FontColor(Colors.Blue.Darken3);
                        c.Item().Text($"• Họ và tên: {landlord?.FullName ?? "........................................"}");
                        c.Item().Text($"• Số điện thoại: {landlord?.Phone ?? "........................................"}");
                        c.Item().Text($"• Email: {landlord?.Email ?? "........................................"}");
                    });

                    // Bên B
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                    {
                        c.Item().Text("BÊN THUÊ (BÊN B - KHÁCH THUÊ):").Bold().FontColor(Colors.Green.Darken3);
                        c.Item().Text($"• Họ và tên: {tenant?.FullName ?? "........................................"}");
                        c.Item().Text($"• Số điện thoại: {tenant?.Phone ?? "........................................"}");
                        c.Item().Text($"• Email: {tenant?.Email ?? "........................................"}");
                    });

                    col.Item().PaddingTop(5).Text("Hai bên thống nhất ký kết Hợp đồng thuê phòng trọ với các điều khoản sau:").Bold();

                    // Điều 1
                    col.Item().Column(c =>
                    {
                        c.Item().Text("ĐIỀU 1: ĐỐI TƯỢNG HỢP ĐỒNG").Bold();
                        c.Item().PaddingLeft(10).Text($"• Phòng trọ: {post?.Title ?? room?.RoomName ?? "Phòng cho thuê"}");
                        c.Item().PaddingLeft(10).Text($"• Địa chỉ: {room?.Address ?? ""}, {room?.Ward ?? ""}, {room?.District ?? ""}, {room?.Province ?? ""}");
                        c.Item().PaddingLeft(10).Text($"• Diện tích: {room?.Area ?? 20} m² | Số người tối đa: {room?.MaxOccupants ?? 2} người.");
                    });

                    // Điều 2
                    col.Item().Column(c =>
                    {
                        c.Item().Text("ĐIỀU 2: THỜI HẠN THUÊ").Bold();
                        c.Item().PaddingLeft(10).Text($"• Thời hạn thuê: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {contract.EndDate:dd/MM/yyyy}.");
                    });

                    // Điều 3
                    col.Item().Column(c =>
                    {
                        c.Item().Text("ĐIỀU 3: GIÁ THUÊ VÀ CHI PHÍ ĐIỆN NƯỚC").Bold();
                        c.Item().PaddingLeft(10).Text($"• Tiền thuê phòng: {contract.MonthlyRent:N0} VNĐ / tháng (Bằng chữ: thanh toán theo thỏa thuận).");
                        c.Item().PaddingLeft(10).Text($"• Tiền đặt cọc đã ghi nhận: {depositAmount:N0} VNĐ.");
                        c.Item().PaddingLeft(10).Text($"• Đơn giá điện: {room?.ElectricityPrice ?? 3500:N0} VNĐ/kWh (tính theo chỉ số đồng hồ thực tế hàng tháng).");
                        c.Item().PaddingLeft(10).Text($"• Đơn giá nước: {room?.WaterPrice ?? 20000:N0} VNĐ/m³ (tính theo chỉ số đồng hồ thực tế hàng tháng).");
                        c.Item().PaddingLeft(10).Text($"• Phí dịch vụ khác (nếu có): {room?.ServiceFee ?? 0:N0} VNĐ/tháng.");
                    });

                    // Điều 4 & 5
                    col.Item().Column(c =>
                    {
                        c.Item().Text("ĐIỀU 4: NGHĨA VỤ VÀ QUYỀN HẠN CỦA HAI BÊN").Bold();
                        c.Item().PaddingLeft(10).Text("• Bên A bàn giao phòng đúng chất lượng, bảo đảm an ninh và hỗ trợ khắc phục các sự cố kỹ thuật.");
                        c.Item().PaddingLeft(10).Text("• Bên B có trách nhiệm thanh toán tiền thuê phòng và tiền điện nước đúng kỳ hạn hàng tháng qua hệ thống hoặc tiền mặt.");
                        c.Item().PaddingLeft(10).Text("• Bên B sử dụng phòng đúng mục đích, không gây mất an ninh trật tự, chấp hành quy định tạm trú tạm vắng.");
                    });

                    // Chữ ký
                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Column(c =>
                        {
                            c.Item().AlignCenter().Text("ĐẠI DIỆN BÊN CHO THUÊ (BÊN A)").Bold();
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9);
                            c.Item().PaddingTop(30).AlignCenter().Text(landlord?.FullName ?? "").Bold();
                            c.Item().AlignCenter().Text(contract.LandlordConfirmed ? "[ĐÃ XÁC NHẬN ĐIỆN TỬ]" : "[CHƯA XÁC NHẬN]").FontSize(8).FontColor(contract.LandlordConfirmed ? Colors.Green.Medium : Colors.Orange.Medium);
                        });

                        table.Cell().Column(c =>
                        {
                            c.Item().AlignCenter().Text("ĐẠI DIỆN BÊN THUÊ (BÊN B)").Bold();
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9);
                            c.Item().PaddingTop(30).AlignCenter().Text(tenant?.FullName ?? "").Bold();
                            c.Item().AlignCenter().Text(contract.TenantConfirmed ? "[ĐÃ XÁC NHẬN ĐIỆN TỬ]" : "[CHƯA XÁC NHẬN]").FontSize(8).FontColor(contract.TenantConfirmed ? Colors.Green.Medium : Colors.Orange.Medium);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                    x.Span(" - Hợp đồng tạo tự động từ Timnhatro.vn");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> XuatBaoCaoDoanhThuExcelAsync(int taiKhoanId, bool isAdmin, int? nam = null, int? thang = null)
    {
        var targetYear = nam ?? DateTime.Now.Year;

        var contractQuery = _db.RentalContracts.AsQueryable();
        if (!isAdmin && taiKhoanId != 0)
        {
            contractQuery = contractQuery.Where(c => c.LandlordAccountId == taiKhoanId);
        }

        var contractIds = await contractQuery.Select(c => c.Id).ToListAsync();

        var billQuery = _db.MonthlyBills
            .Include(b => b.Contract)
                .ThenInclude(c => c.Post)
                    .ThenInclude(p => p.Room)
            .Where(b => contractIds.Contains(b.ContractId) && b.Year == targetYear);

        if (thang.HasValue && thang.Value > 0)
        {
            billQuery = billQuery.Where(b => b.Month == thang.Value);
        }

        var bills = await billQuery
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.ContractId)
            .ToListAsync();

        var users = await _db.Users.ToListAsync();
        var userDict = users.ToDictionary(u => u.Id, u => u.FullName);
        var userPhoneDict = users.ToDictionary(u => u.Id, u => u.Phone ?? "");

        using var workbook = new XLWorkbook();

        // Sheet 1: Báo cáo hóa đơn & doanh thu
        var ws = workbook.Worksheets.Add("Báo Cáo Doanh Thu");

        // Header Title
        ws.Cell("A1").Value = "HỆ THỐNG QUẢN LÝ PHÒNG TRỌ TIMNHATRO.VN - PMS";
        ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.DarkBlue);

        ws.Cell("A2").Value = $"BÁO CÁO DOANH THU & CHỈ SỐ ĐIỆN NƯỚC - NĂM {targetYear}" + (thang.HasValue ? $" (THÁNG {thang.Value})" : "");
        ws.Cell("A2").Style.Font.SetBold(true).Font.SetFontSize(16).Font.SetFontColor(XLColor.Black);

        ws.Cell("A3").Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm} | Phạm vi: {(isAdmin ? "Toàn hệ thống (Admin)" : "Chủ trọ")}";
        ws.Cell("A3").Style.Font.SetItalic(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.Gray);

        // Table Headers
        var headers = new string[]
        {
            "STT", "Mã HĐ", "Tháng/Năm", "Phòng Trọ", "Khách Thuê", "SĐT",
            "Tiền Phòng", "Số Điện Cũ", "Số Điện Mới", "Tiêu Thụ (kWh)", "Tiền Điện",
            "Số Nước Cũ", "Số Nước Mới", "Tiêu Thụ (m³)", "Tiền Nước",
            "Phụ Phí", "Tổng Tiền", "Trạng Thái", "Ngày Thanh Toán"
        };

        int headerRow = 5;
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0084FF"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        int currentRow = 6;
        int stt = 1;

        decimal totalRevenuePaid = 0;
        decimal totalRevenuePending = 0;
        decimal totalElectricityAmount = 0;
        decimal totalWaterAmount = 0;

        foreach (var b in bills)
        {
            var roomTitle = b.Contract?.Post?.Title ?? b.Contract?.Post?.Room?.RoomName ?? $"HĐ #{b.ContractId}";
            var tenantName = b.Contract != null && userDict.ContainsKey(b.Contract.TenantAccountId) ? userDict[b.Contract.TenantAccountId] : $"Khách #{b.Contract?.TenantAccountId}";
            var tenantPhone = b.Contract != null && userPhoneDict.ContainsKey(b.Contract.TenantAccountId) ? userPhoneDict[b.Contract.TenantAccountId] : "";

            var elecUsed = Math.Max(0, b.NewElectricity - b.OldElectricity);
            var elecAmount = elecUsed * b.ElectricityPrice;
            var waterUsed = Math.Max(0, b.NewWater - b.OldWater);
            var waterAmount = waterUsed * b.WaterPrice;

            ws.Cell(currentRow, 1).Value = stt++;
            ws.Cell(currentRow, 2).Value = $"HD-{b.ContractId:D4}";
            ws.Cell(currentRow, 3).Value = $"{b.Month}/{b.Year}";
            ws.Cell(currentRow, 4).Value = roomTitle;
            ws.Cell(currentRow, 5).Value = tenantName;
            ws.Cell(currentRow, 6).Value = tenantPhone;

            ws.Cell(currentRow, 7).Value = b.RoomPrice;
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";

            ws.Cell(currentRow, 8).Value = b.OldElectricity;
            ws.Cell(currentRow, 9).Value = b.NewElectricity;
            ws.Cell(currentRow, 10).Value = elecUsed;
            ws.Cell(currentRow, 11).Value = elecAmount;
            ws.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";

            ws.Cell(currentRow, 12).Value = b.OldWater;
            ws.Cell(currentRow, 13).Value = b.NewWater;
            ws.Cell(currentRow, 14).Value = waterUsed;
            ws.Cell(currentRow, 15).Value = waterAmount;
            ws.Cell(currentRow, 15).Style.NumberFormat.Format = "#,##0";

            ws.Cell(currentRow, 16).Value = b.OtherFees;
            ws.Cell(currentRow, 16).Style.NumberFormat.Format = "#,##0";

            ws.Cell(currentRow, 17).Value = b.TotalAmount;
            ws.Cell(currentRow, 17).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 17).Style.Font.SetBold(true);

            ws.Cell(currentRow, 18).Value = b.Status == 1 ? "Đã thanh toán" : (b.Status == 2 ? "Đã hủy" : "Chờ thanh toán");
            if (b.Status == 1)
            {
                ws.Cell(currentRow, 18).Style.Font.SetFontColor(XLColor.DarkGreen).Font.SetBold(true);
                totalRevenuePaid += b.TotalAmount;
            }
            else
            {
                ws.Cell(currentRow, 18).Style.Font.SetFontColor(XLColor.Orange).Font.SetBold(true);
                totalRevenuePending += b.TotalAmount;
            }

            ws.Cell(currentRow, 19).Value = b.PaidAt.HasValue ? b.PaidAt.Value.ToString("dd/MM/yyyy HH:mm") : "-";

            totalElectricityAmount += elecAmount;
            totalWaterAmount += waterAmount;

            currentRow++;
        }

        // Total Summary Row
        if (bills.Count > 0)
        {
            ws.Cell(currentRow, 1).Value = "TỔNG CỘNG";
            ws.Range(currentRow, 1, currentRow, 6).Merge();
            ws.Cell(currentRow, 1).Style.Font.SetBold(true);
            ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E8F0FE"));

            ws.Cell(currentRow, 11).Value = totalElectricityAmount;
            ws.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 11).Style.Font.SetBold(true);

            ws.Cell(currentRow, 15).Value = totalWaterAmount;
            ws.Cell(currentRow, 15).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 15).Style.Font.SetBold(true);

            ws.Cell(currentRow, 17).Value = totalRevenuePaid + totalRevenuePending;
            ws.Cell(currentRow, 17).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 17).Style.Font.SetBold(true);
        }

        ws.Columns().AdjustToContents();

        // Sheet 2: Danh sách hợp đồng
        var wsContracts = workbook.Worksheets.Add("Danh Sách Hợp Đồng");
        var contractHeaders = new string[] { "Mã HĐ", "Phòng Trọ", "Khách Thuê", "SĐT", "Chủ Trọ", "Ngày Bắt Đầu", "Ngày Kết Thúc", "Tiền Thuê/Tháng", "Trạng Thái" };
        for (int i = 0; i < contractHeaders.Length; i++)
        {
            var cell = wsContracts.Cell(1, i + 1);
            cell.Value = contractHeaders[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#10B981"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        var contracts = await contractQuery
            .Include(c => c.Post)
                .ThenInclude(p => p.Room)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        int cRow = 2;
        foreach (var c in contracts)
        {
            var roomTitle = c.Post?.Title ?? c.Post?.Room?.RoomName ?? $"Phòng #{c.PostId}";
            var tenantName = userDict.ContainsKey(c.TenantAccountId) ? userDict[c.TenantAccountId] : $"Khách #{c.TenantAccountId}";
            var tenantPhone = userPhoneDict.ContainsKey(c.TenantAccountId) ? userPhoneDict[c.TenantAccountId] : "";
            var landlordName = userDict.ContainsKey(c.LandlordAccountId) ? userDict[c.LandlordAccountId] : $"Chủ #{c.LandlordAccountId}";

            wsContracts.Cell(cRow, 1).Value = $"HD-{c.Id:D4}";
            wsContracts.Cell(cRow, 2).Value = roomTitle;
            wsContracts.Cell(cRow, 3).Value = tenantName;
            wsContracts.Cell(cRow, 4).Value = tenantPhone;
            wsContracts.Cell(cRow, 5).Value = landlordName;
            wsContracts.Cell(cRow, 6).Value = c.StartDate.ToString("dd/MM/yyyy");
            wsContracts.Cell(cRow, 7).Value = c.EndDate.ToString("dd/MM/yyyy");
            wsContracts.Cell(cRow, 8).Value = c.MonthlyRent;
            wsContracts.Cell(cRow, 8).Style.NumberFormat.Format = "#,##0";
            wsContracts.Cell(cRow, 9).Value = c.Status == 1 ? "Đang hiệu lực" : (c.Status == 2 ? "Đã kết thúc" : "Chờ xác nhận");
            cRow++;
        }
        wsContracts.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
