using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.BLL;

public class MonthlyBillBLL : IMonthlyBillService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public MonthlyBillBLL(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<HoaDonDto> TaoAsync(int chuTroId, TaoHoaDonDto dto)
    {
        if (dto.Thang < 1 || dto.Thang > 12)
            throw new Exception("Tháng không hợp lệ (1-12).");
        if (dto.Nam < 2000 || dto.Nam > 2100)
            throw new Exception("Năm không hợp lệ.");
        if (dto.SoDienMoi < dto.SoDienCu)
            throw new Exception("Số điện mới không được nhỏ hơn số điện cũ.");
        if (dto.SoNuocMoi < dto.SoNuocCu)
            throw new Exception("Số nước mới không được nhỏ hơn số nước cũ.");

        var contract = await _db.RentalContracts
            .Include(c => c.Post)
                .ThenInclude(p => p.Room)
            .FirstOrDefaultAsync(c => c.Id == dto.HopDongId && (c.LandlordAccountId == chuTroId || chuTroId == 0))
            ?? throw new Exception("Không tìm thấy hợp đồng thuê phòng hoặc bạn không có quyền.");

        var exists = await _db.MonthlyBills
            .AnyAsync(b => b.ContractId == dto.HopDongId && b.Month == dto.Thang && b.Year == dto.Nam);
        if (exists)
            throw new Exception($"Hóa đơn tháng {dto.Thang}/{dto.Nam} cho hợp đồng này đã được tạo trước đó.");

        var roomPrice = dto.TienPhong ?? contract.MonthlyRent;
        var elecDiff = Math.Max(0, dto.SoDienMoi - dto.SoDienCu);
        var waterDiff = Math.Max(0, dto.SoNuocMoi - dto.SoNuocCu);
        var elecAmount = elecDiff * dto.GiaDien;
        var waterAmount = waterDiff * dto.GiaNuoc;
        var totalAmount = roomPrice + elecAmount + waterAmount + dto.ChiPhiKhac;

        var bill = new MonthlyBill
        {
            ContractId = contract.Id,
            Month = dto.Thang,
            Year = dto.Nam,
            OldElectricity = dto.SoDienCu,
            NewElectricity = dto.SoDienMoi,
            ElectricityPrice = dto.GiaDien,
            OldWater = dto.SoNuocCu,
            NewWater = dto.SoNuocMoi,
            WaterPrice = dto.GiaNuoc,
            RoomPrice = roomPrice,
            OtherFees = dto.ChiPhiKhac,
            OtherFeesNote = dto.GhiChuChiPhiKhac,
            TotalAmount = totalAmount,
            Status = 0, // Chờ thanh toán
            DueDate = dto.HanThanhToan ?? DateTime.Now.AddDays(7),
            Note = dto.GhiChu,
            CreatedAt = DateTime.Now
        };

        _db.MonthlyBills.Add(bill);
        await _db.SaveChangesAsync();

        // Gửi thông báo đến người thuê phòng
        try
        {
            await _notificationService.CreateNotificationAsync(
                contract.TenantAccountId,
                $"Hóa đơn tiền phòng tháng {bill.Month}/{bill.Year}",
                $"Chủ trọ đã lập hóa đơn tháng {bill.Month}/{bill.Year}. Tổng tiền: {bill.TotalAmount:N0} VNĐ (Điện: {elecDiff} kWh, Nước: {waterDiff} m³).",
                1,
                "/tenant/rentals"
            );
        }
        catch { /* ignore notification errors */ }

        return await LayChiTietAsync(chuTroId, bill.Id);
    }

    public async Task<List<HoaDonDto>> LayTheoHopDongAsync(int taiKhoanId, int hopDongId)
    {
        var contract = await _db.RentalContracts
            .FirstOrDefaultAsync(c => c.Id == hopDongId && (c.TenantAccountId == taiKhoanId || c.LandlordAccountId == taiKhoanId || taiKhoanId == 0))
            ?? throw new Exception("Không tìm thấy hợp đồng hoặc bạn không có quyền xem.");

        var bills = await _db.MonthlyBills
            .Where(b => b.ContractId == hopDongId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync();

        var result = new List<HoaDonDto>();
        foreach (var b in bills)
        {
            result.Add(await MapToDtoAsync(b, contract));
        }
        return result;
    }

    public async Task<List<HoaDonDto>> LayDanhSachCuaToiAsync(int taiKhoanId, bool isLandlord)
    {
        var contractQuery = _db.RentalContracts.AsQueryable();
        if (taiKhoanId != 0)
        {
            contractQuery = isLandlord
                ? contractQuery.Where(c => c.LandlordAccountId == taiKhoanId)
                : contractQuery.Where(c => c.TenantAccountId == taiKhoanId);
        }

        var contractIds = await contractQuery.Select(c => c.Id).ToListAsync();

        var bills = await _db.MonthlyBills
            .Include(b => b.Contract)
            .Where(b => contractIds.Contains(b.ContractId))
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        var result = new List<HoaDonDto>();
        foreach (var b in bills)
        {
            result.Add(await MapToDtoAsync(b, b.Contract));
        }
        return result;
    }

    public async Task<HoaDonDto> LayChiTietAsync(int taiKhoanId, int id)
    {
        var bill = await _db.MonthlyBills
            .Include(b => b.Contract)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception("Không tìm thấy hóa đơn.");

        if (taiKhoanId != 0 && bill.Contract.TenantAccountId != taiKhoanId && bill.Contract.LandlordAccountId != taiKhoanId)
            throw new Exception("Bạn không có quyền xem hóa đơn này.");

        return await MapToDtoAsync(bill, bill.Contract);
    }

    public async Task<HoaDonDto> CapNhatAsync(int chuTroId, int id, CapNhatHoaDonDto dto)
    {
        if (dto.SoDienMoi < dto.SoDienCu)
            throw new Exception("Số điện mới không được nhỏ hơn số điện cũ.");
        if (dto.SoNuocMoi < dto.SoNuocCu)
            throw new Exception("Số nước mới không được nhỏ hơn số nước cũ.");

        var bill = await _db.MonthlyBills
            .Include(b => b.Contract)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception("Không tìm thấy hóa đơn.");

        if (chuTroId != 0 && bill.Contract.LandlordAccountId != chuTroId)
            throw new Exception("Bạn không có quyền chỉnh sửa hóa đơn này.");

        var elecDiff = Math.Max(0, dto.SoDienMoi - dto.SoDienCu);
        var waterDiff = Math.Max(0, dto.SoNuocMoi - dto.SoNuocCu);
        var totalAmount = dto.TienPhong + (elecDiff * dto.GiaDien) + (waterDiff * dto.GiaNuoc) + dto.ChiPhiKhac;

        bill.OldElectricity = dto.SoDienCu;
        bill.NewElectricity = dto.SoDienMoi;
        bill.ElectricityPrice = dto.GiaDien;
        bill.OldWater = dto.SoNuocCu;
        bill.NewWater = dto.SoNuocMoi;
        bill.WaterPrice = dto.GiaNuoc;
        bill.RoomPrice = dto.TienPhong;
        bill.OtherFees = dto.ChiPhiKhac;
        bill.OtherFeesNote = dto.GhiChuChiPhiKhac;
        bill.TotalAmount = totalAmount;
        if (dto.TrangThai.HasValue) bill.Status = dto.TrangThai.Value;
        if (dto.HanThanhToan.HasValue) bill.DueDate = dto.HanThanhToan.Value;
        if (dto.GhiChu != null) bill.Note = dto.GhiChu;
        bill.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return await MapToDtoAsync(bill, bill.Contract);
    }

    public async Task<HoaDonDto> ThanhToanAsync(int taiKhoanId, int id, XacNhanThanhToanHoaDonDto? dto = null)
    {
        var bill = await _db.MonthlyBills
            .Include(b => b.Contract)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception("Không tìm thấy hóa đơn.");

        if (taiKhoanId != 0 && bill.Contract.TenantAccountId != taiKhoanId && bill.Contract.LandlordAccountId != taiKhoanId)
            throw new Exception("Bạn không có quyền xác nhận thanh toán hóa đơn này.");

        bill.Status = 1; // Đã thanh toán
        bill.PaidAt = DateTime.Now;
        bill.PaymentMethod = dto?.PhuongThucThanhToan ?? "Xác nhận đã thanh toán";
        if (!string.IsNullOrEmpty(dto?.GhiChu))
        {
            bill.Note = string.IsNullOrEmpty(bill.Note) ? dto.GhiChu : $"{bill.Note} | {dto.GhiChu}";
        }
        bill.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        // Gửi thông báo
        try
        {
            var isTenantPaying = taiKhoanId == bill.Contract.TenantAccountId;
            var targetAccountId = isTenantPaying ? bill.Contract.LandlordAccountId : bill.Contract.TenantAccountId;
            var senderRole = isTenantPaying ? "Khách thuê" : "Chủ trọ";

            await _notificationService.CreateNotificationAsync(
                targetAccountId,
                $"Xác nhận thanh toán hóa đơn {bill.Month}/{bill.Year}",
                $"{senderRole} đã xác nhận thanh toán hóa đơn tháng {bill.Month}/{bill.Year} ({bill.TotalAmount:N0} VNĐ).",
                1,
                isTenantPaying ? "/landlord/contracts" : "/tenant/rentals"
            );
        }
        catch { /* ignore */ }

        return await MapToDtoAsync(bill, bill.Contract);
    }

    public async Task<bool> XoaAsync(int chuTroId, int id)
    {
        var bill = await _db.MonthlyBills
            .Include(b => b.Contract)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new Exception("Không tìm thấy hóa đơn.");

        if (chuTroId != 0 && bill.Contract.LandlordAccountId != chuTroId)
            throw new Exception("Bạn không có quyền xóa hóa đơn này.");

        _db.MonthlyBills.Remove(bill);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<HoaDonDto> MapToDtoAsync(MonthlyBill b, RentalContract contract)
    {
        var post = await _db.Posts
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Id == contract.PostId);

        var tenant = await _db.Users.FirstOrDefaultAsync(u => u.Id == contract.TenantAccountId);
        var landlord = await _db.Users.FirstOrDefaultAsync(u => u.Id == contract.LandlordAccountId);

        return new HoaDonDto
        {
            Id = b.Id,
            HopDongId = b.ContractId,
            Thang = b.Month,
            Nam = b.Year,
            SoDienCu = b.OldElectricity,
            SoDienMoi = b.NewElectricity,
            GiaDien = b.ElectricityPrice,
            SoNuocCu = b.OldWater,
            SoNuocMoi = b.NewWater,
            GiaNuoc = b.WaterPrice,
            TienPhong = b.RoomPrice,
            ChiPhiKhac = b.OtherFees,
            GhiChuChiPhiKhac = b.OtherFeesNote,
            TongTien = b.TotalAmount,
            TrangThai = b.Status,
            HanThanhToan = b.DueDate,
            NgayThanhToan = b.PaidAt,
            PhuongThucThanhToan = b.PaymentMethod,
            GhiChu = b.Note,
            NgayTao = b.CreatedAt,
            TenPhong = post?.Title ?? post?.Room?.RoomName ?? $"Phòng hợp đồng #{contract.Id}",
            DiaChiPhong = post?.Room != null ? $"{post.Room.Address}, {post.Room.Ward}, {post.Room.District}, {post.Room.Province}" : "",
            TenNguoiThue = tenant?.FullName ?? $"Khách thuê #{contract.TenantAccountId}",
            SdtNguoiThue = tenant?.Phone ?? "",
            TenChuTro = landlord?.FullName ?? $"Chủ trọ #{contract.LandlordAccountId}",
            SdtChuTro = landlord?.Phone ?? ""
        };
    }
}
