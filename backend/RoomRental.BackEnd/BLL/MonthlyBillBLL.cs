using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class MonthlyBillBLL : IMonthlyBillService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MonthlyBillBLL> _logger;

    public MonthlyBillBLL(
        ApplicationDbContext db,
        INotificationService notificationService,
        ILogger<MonthlyBillBLL> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<HoaDonDto> TaoAsync(int chuTroId, TaoHoaDonDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_db);
        ValidatePeriod(dto.Thang, dto.Nam);
        if (dto.ChiPhiKhac < 0) throw new BusinessRuleException("Chi phí khác không được âm.");

        var contract = await _db.RentalContracts
            .Include(c => c.Post).ThenInclude(p => p!.Room)
            .FirstOrDefaultAsync(c => c.Id == dto.HopDongId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng thuê phòng.");

        if (contract.LandlordAccountId != chuTroId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền lập hóa đơn cho hợp đồng này.");
        if (contract.Status is not (RentalContractStatus.Active or RentalContractStatus.Expired or RentalContractStatus.Terminated))
            throw BusinessRuleException.Conflict("Chỉ có thể lập hóa đơn cho hợp đồng đã có hiệu lực.");
        if (await _db.MonthlyBills.AnyAsync(b => b.ContractId == dto.HopDongId && b.Month == dto.Thang && b.Year == dto.Nam))
            throw BusinessRuleException.Conflict($"Hóa đơn tháng {dto.Thang}/{dto.Nam} cho hợp đồng này đã tồn tại.");

        var periodStart = new DateTime(dto.Nam, dto.Thang, 1);
        var lastRentalDay = contract.EndDate.Date;
        if (contract.Status == RentalContractStatus.Terminated)
        {
            if (!contract.UpdatedAt.HasValue)
                throw BusinessRuleException.Conflict("Hợp đồng cũ chưa có mốc chấm dứt để xác định kỳ hóa đơn cuối.");
            // Terminal contracts cannot be edited; UpdatedAt is the existing UTC termination timestamp.
            var terminatedOn = RoomRental.BackEnd.Helpers.RentalCalendar.UtcDateInVietnam(contract.UpdatedAt.Value);
            if (terminatedOn < lastRentalDay) lastRentalDay = terminatedOn;
        }
        if (lastRentalDay < contract.StartDate.Date || periodStart > lastRentalDay || periodStart.AddMonths(1) <= contract.StartDate.Date)
            throw new BusinessRuleException("Kỳ hóa đơn phải thuộc thời gian thuê thực tế của hợp đồng.");
        if (await _db.MonthlyBills.AnyAsync(b => b.ContractId == contract.Id && b.Status != MonthlyBillStatus.Cancelled &&
            b.Year * 12 + b.Month > dto.Nam * 12 + dto.Thang))
            throw BusinessRuleException.Conflict("Cần lập hóa đơn theo thứ tự thời gian.");
        var previous = await PreviousBillAsync(contract.Id, dto.Nam, dto.Thang);
        var oldElectricity = previous?.NewElectricity ?? dto.SoDienCu;
        var oldWater = previous?.NewWater ?? dto.SoNuocCu;
        ValidateReadings(oldElectricity, dto.SoDienMoi, oldWater, dto.SoNuocMoi);

        var bill = new MonthlyBill
        {
            ContractId = contract.Id,
            Month = dto.Thang,
            Year = dto.Nam,
            OldElectricity = oldElectricity,
            NewElectricity = dto.SoDienMoi,
            ElectricityPrice = contract.ElectricityPrice,
            OldWater = oldWater,
            NewWater = dto.SoNuocMoi,
            WaterPrice = contract.WaterPrice,
            RoomPrice = contract.MonthlyRent,
            ServiceFee = contract.ServiceFee,
            OtherFees = dto.ChiPhiKhac,
            OtherFeesNote = dto.GhiChuChiPhiKhac?.Trim(),
            Status = (dto.HanThanhToan ?? DateTime.Now.AddDays(7)) < DateTime.Now ? MonthlyBillStatus.Overdue : MonthlyBillStatus.Unpaid,
            DueDate = dto.HanThanhToan ?? DateTime.Now.AddDays(7),
            Note = dto.GhiChu?.Trim(),
            CreatedAt = DateTime.Now
        };
        bill.TotalAmount = CalculateTotal(bill);

        _db.MonthlyBills.Add(bill);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            await _notificationService.CreateNotificationAsync(
                contract.TenantAccountId,
                $"Hóa đơn tiền phòng tháng {bill.Month}/{bill.Year}",
                $"Tổng tiền: {bill.TotalAmount:N0} VNĐ. Hạn thanh toán: {bill.DueDate:dd/MM/yyyy}.",
                1,
                "/tenant/rentals");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi thông báo cho hóa đơn {BillId}", bill.Id);
        }

        return await MapToDtoAsync(bill, contract);
    }

    public async Task<List<HoaDonDto>> LayTheoHopDongAsync(int taiKhoanId, int hopDongId)
    {
        await ReconcileOverdueAsync();
        var contract = await _db.RentalContracts.FirstOrDefaultAsync(c => c.Id == hopDongId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng.");
        EnsureContractParticipant(contract, taiKhoanId);

        var bills = await _db.MonthlyBills.Where(b => b.ContractId == hopDongId)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ToListAsync();
        var result = new List<HoaDonDto>();
        foreach (var bill in bills) result.Add(await MapToDtoAsync(bill, contract));
        return result;
    }

    public async Task<List<HoaDonDto>> LayDanhSachCuaToiAsync(int taiKhoanId, bool isLandlord)
    {
        await ReconcileOverdueAsync();
        var contractQuery = _db.RentalContracts.AsQueryable();
        contractQuery = isLandlord
            ? contractQuery.Where(c => c.LandlordAccountId == taiKhoanId)
            : contractQuery.Where(c => c.TenantAccountId == taiKhoanId);

        var contractIds = await contractQuery.Select(c => c.Id).ToListAsync();
        var bills = await _db.MonthlyBills.Include(b => b.Contract)
            .Where(b => contractIds.Contains(b.ContractId))
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ThenByDescending(b => b.CreatedAt)
            .ToListAsync();
        var result = new List<HoaDonDto>();
        foreach (var bill in bills) result.Add(await MapToDtoAsync(bill, bill.Contract));
        return result;
    }

    public async Task<HoaDonDto> LayChiTietAsync(int taiKhoanId, int id)
    {
        await ReconcileOverdueAsync();
        var bill = await _db.MonthlyBills.Include(b => b.Contract).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn.");
        EnsureContractParticipant(bill.Contract, taiKhoanId);
        return await MapToDtoAsync(bill, bill.Contract);
    }

    public async Task<HoaDonDto> CapNhatAsync(int chuTroId, int id, CapNhatHoaDonDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_db);
        ValidateReadings(dto.SoDienCu, dto.SoDienMoi, dto.SoNuocCu, dto.SoNuocMoi);
        if (dto.ChiPhiKhac < 0) throw new BusinessRuleException("Chi phí khác không được âm.");

        var bill = await _db.MonthlyBills.Include(b => b.Contract).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn.");
        if (bill.Contract.LandlordAccountId != chuTroId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền chỉnh sửa hóa đơn này.");
        if (bill.Status is MonthlyBillStatus.Paid or MonthlyBillStatus.PendingPayment or MonthlyBillStatus.Cancelled)
            throw BusinessRuleException.Conflict("Không thể chỉnh sửa hóa đơn đã thanh toán, đang thanh toán hoặc đã hủy.");

        var previous = await PreviousBillAsync(bill.ContractId, bill.Year, bill.Month);
        var oldElectricity = previous?.NewElectricity ?? bill.OldElectricity;
        var oldWater = previous?.NewWater ?? bill.OldWater;
        ValidateReadings(oldElectricity, dto.SoDienMoi, oldWater, dto.SoNuocMoi);
        if ((dto.SoDienMoi != bill.NewElectricity || dto.SoNuocMoi != bill.NewWater) &&
            await _db.MonthlyBills.AnyAsync(b => b.ContractId == bill.ContractId && b.Status != MonthlyBillStatus.Cancelled &&
                b.Year * 12 + b.Month > bill.Year * 12 + bill.Month))
            throw BusinessRuleException.Conflict("Không thể thay đổi chỉ số đã được dùng làm đầu kỳ của hóa đơn sau.");
        bill.OldElectricity = oldElectricity;
        bill.NewElectricity = dto.SoDienMoi;
        bill.OldWater = oldWater;
        bill.NewWater = dto.SoNuocMoi;
        bill.OtherFees = dto.ChiPhiKhac;
        bill.OtherFeesNote = dto.GhiChuChiPhiKhac?.Trim();
        if (dto.HanThanhToan.HasValue) bill.DueDate = dto.HanThanhToan;
        if (dto.GhiChu != null) bill.Note = dto.GhiChu.Trim();
        bill.Status = bill.DueDate < DateTime.Now ? MonthlyBillStatus.Overdue : MonthlyBillStatus.Unpaid;
        bill.TotalAmount = CalculateTotal(bill);
        bill.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await MapToDtoAsync(bill, bill.Contract);
    }

    public async Task<HoaDonDto> ThanhToanAsync(int taiKhoanId, int id, XacNhanThanhToanHoaDonDto? dto = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_db);
        var bill = await _db.MonthlyBills.Include(b => b.Contract).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn.");
        if (bill.Contract.LandlordAccountId != taiKhoanId)
            throw BusinessRuleException.Forbidden("Chỉ chủ trọ sở hữu hợp đồng mới được xác nhận thanh toán thủ công.");
        if (bill.Status == MonthlyBillStatus.Paid)
            throw BusinessRuleException.Conflict("Hóa đơn đã được thanh toán.");
        if (bill.Status == MonthlyBillStatus.Cancelled)
            throw BusinessRuleException.Conflict("Hóa đơn đã bị hủy.");
        if (bill.Status == MonthlyBillStatus.PendingPayment)
            throw BusinessRuleException.Conflict("Hóa đơn đang được thanh toán trực tuyến, không thể xác nhận thủ công.");

        bill.Status = MonthlyBillStatus.Paid;
        bill.PaidAt = DateTime.Now;
        bill.PaymentMethod = dto?.PhuongThucThanhToan?.Trim() ?? "Chủ trọ xác nhận";
        if (!string.IsNullOrWhiteSpace(dto?.GhiChu))
            bill.Note = string.IsNullOrWhiteSpace(bill.Note) ? dto.GhiChu.Trim() : $"{bill.Note} | {dto.GhiChu.Trim()}";
        bill.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            await _notificationService.CreateNotificationAsync(
                bill.Contract.TenantAccountId,
                $"Hóa đơn {bill.Month}/{bill.Year} đã được xác nhận",
                $"Chủ trọ đã xác nhận thanh toán {bill.TotalAmount:N0} VNĐ.",
                1,
                "/tenant/rentals");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi thông báo thanh toán hóa đơn {BillId}", bill.Id);
        }

        return await MapToDtoAsync(bill, bill.Contract);
    }

    public async Task<bool> XoaAsync(int chuTroId, int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_db);
        var bill = await _db.MonthlyBills.Include(b => b.Contract).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hóa đơn.");
        if (bill.Contract.LandlordAccountId != chuTroId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền hủy hóa đơn này.");
        if (bill.Status == MonthlyBillStatus.Paid)
            throw BusinessRuleException.Conflict("Không thể hủy hóa đơn đã thanh toán.");
        if (bill.Status == MonthlyBillStatus.PendingPayment)
            throw BusinessRuleException.Conflict("Không thể hủy hóa đơn đang có giao dịch thanh toán.");
        if (bill.Status == MonthlyBillStatus.Cancelled)
            throw BusinessRuleException.Conflict("Hóa đơn đã được hủy trước đó.");

        if (await _db.MonthlyBills.AnyAsync(b => b.ContractId == bill.ContractId && b.Status != MonthlyBillStatus.Cancelled &&
            b.Year * 12 + b.Month > bill.Year * 12 + bill.Month))
            throw BusinessRuleException.Conflict("Không thể hủy hóa đơn đã có kỳ sau sử dụng chỉ số điện nước.");
        bill.Status = MonthlyBillStatus.Cancelled;
        bill.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task ReconcileOverdueAsync()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowLock.AcquireAsync(_db);
        var overdue = await _db.MonthlyBills
            .Where(b => b.Status == MonthlyBillStatus.Unpaid && b.DueDate < DateTime.Now)
            .ToListAsync();
        if (overdue.Count == 0) return;
        foreach (var bill in overdue) bill.Status = MonthlyBillStatus.Overdue;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private Task<MonthlyBill?> PreviousBillAsync(int contractId, int year, int month) =>
        _db.MonthlyBills.Where(b => b.ContractId == contractId && b.Status != MonthlyBillStatus.Cancelled &&
            b.Year * 12 + b.Month < year * 12 + month)
        .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).FirstOrDefaultAsync();

    private static decimal CalculateTotal(MonthlyBill bill) =>
        bill.RoomPrice +
        (bill.NewElectricity - bill.OldElectricity) * bill.ElectricityPrice +
        (bill.NewWater - bill.OldWater) * bill.WaterPrice +
        bill.ServiceFee + bill.OtherFees;

    private static void ValidatePeriod(int month, int year)
    {
        if (month is < 1 or > 12) throw new BusinessRuleException("Tháng không hợp lệ (1-12).");
        if (year is < 2000 or > 2100) throw new BusinessRuleException("Năm không hợp lệ.");
    }

    private static void ValidateReadings(decimal oldElectricity, decimal newElectricity, decimal oldWater, decimal newWater)
    {
        if (oldElectricity < 0 || oldWater < 0) throw new BusinessRuleException("Chỉ số điện nước không được âm.");
        if (newElectricity < oldElectricity) throw new BusinessRuleException("Số điện mới không được nhỏ hơn số điện cũ.");
        if (newWater < oldWater) throw new BusinessRuleException("Số nước mới không được nhỏ hơn số nước cũ.");
    }

    private static void EnsureContractParticipant(RentalContract contract, int accountId)
    {
        if (contract.TenantAccountId != accountId && contract.LandlordAccountId != accountId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền xem hóa đơn này.");
    }

    private async Task<HoaDonDto> MapToDtoAsync(MonthlyBill bill, RentalContract contract)
    {
        var post = await _db.Posts.Include(p => p.Room).FirstOrDefaultAsync(p => p.Id == contract.PostId);
        var tenant = await _db.Users.FirstOrDefaultAsync(u => u.Id == contract.TenantAccountId);
        var landlord = await _db.Users.FirstOrDefaultAsync(u => u.Id == contract.LandlordAccountId);

        return new HoaDonDto
        {
            Id = bill.Id, HopDongId = bill.ContractId, Thang = bill.Month, Nam = bill.Year,
            SoDienCu = bill.OldElectricity, SoDienMoi = bill.NewElectricity, GiaDien = bill.ElectricityPrice,
            SoNuocCu = bill.OldWater, SoNuocMoi = bill.NewWater, GiaNuoc = bill.WaterPrice,
            TienPhong = bill.RoomPrice, PhiDichVu = bill.ServiceFee,
            ChiPhiKhac = bill.OtherFees, GhiChuChiPhiKhac = bill.OtherFeesNote,
            TongTien = bill.TotalAmount, TrangThai = bill.Status, HanThanhToan = bill.DueDate,
            NgayThanhToan = bill.PaidAt, PhuongThucThanhToan = bill.PaymentMethod,
            GhiChu = bill.Note, NgayTao = bill.CreatedAt,
            TenPhong = post?.Title ?? post?.Room?.RoomName ?? $"Phòng hợp đồng #{contract.Id}",
            DiaChiPhong = post?.Room != null ? $"{post.Room.Address}, {post.Room.Ward}, {post.Room.District}, {post.Room.Province}" : "",
            TenNguoiThue = tenant?.FullName ?? $"Khách thuê #{contract.TenantAccountId}",
            SdtNguoiThue = tenant?.Phone ?? "", TenChuTro = landlord?.FullName ?? $"Chủ trọ #{contract.LandlordAccountId}",
            SdtChuTro = landlord?.Phone ?? ""
        };
    }
}
