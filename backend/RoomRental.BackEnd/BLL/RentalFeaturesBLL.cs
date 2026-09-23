using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class RentalRequestBLL : IRentalRequestService
{
    private readonly ApplicationDbContext _db;
    public RentalRequestBLL(ApplicationDbContext db) => _db = db;

    public async Task<YeuCauThueDto> TaoAsync(int nguoiThueId, TaoYeuCauThueDto dto)
    {
        var post = await _db.Posts.Include(x => x.Landlord).FirstOrDefaultAsync(x => x.Id == dto.BaiDangId)
            ?? throw new Exception("Không tìm thấy bài đăng");
        if (post.Status != RoomRental.BackEnd.Models.Enums.PostStatus.Approved) throw new Exception("Bài đăng chưa được duyệt");
        if (await _db.RentalRequests.AnyAsync(x => x.PostId == dto.BaiDangId && x.TenantAccountId == nguoiThueId && x.Status == 0))
            throw new Exception("Bạn đã gửi yêu cầu cho bài đăng này");
        var item = new RentalRequest { PostId = post.Id, TenantAccountId = nguoiThueId, LandlordAccountId = post.Landlord.AccountId, Note = dto.GhiChu };
        _db.RentalRequests.Add(item); await _db.SaveChangesAsync(); return await Map(item);
    }

    public async Task<List<YeuCauThueDto>> LayCuaToiAsync(int taiKhoanId, bool chuTro)
    {
        var q = _db.RentalRequests.Include(x => x.Post).AsQueryable();
        q = chuTro ? q.Where(x => x.LandlordAccountId == taiKhoanId) : q.Where(x => x.TenantAccountId == taiKhoanId);
        return await q.OrderByDescending(x => x.CreatedAt).Select(x => new YeuCauThueDto { Id=x.Id, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, ChuTroId=x.LandlordAccountId, TieuDeBaiDang=x.Post.Title, TrangThai=x.Status, GhiChu=x.Note, NgayTao=x.CreatedAt }).ToListAsync();
    }

    public async Task<YeuCauThueDto> CapNhatTrangThaiAsync(int chuTroId, int id, int trangThai)
    {
        var item = await _db.RentalRequests.Include(x => x.Post).FirstOrDefaultAsync(x => x.Id == id && x.LandlordAccountId == chuTroId) ?? throw new Exception("Không tìm thấy yêu cầu thuê phòng");
        if (trangThai is < 1 or > 2) throw new Exception("Trạng thái phải là 1 (duyệt) hoặc 2 (từ chối)");
        item.Status = trangThai; await _db.SaveChangesAsync(); return await Map(item);
    }

    private async Task<YeuCauThueDto> Map(RentalRequest x) => await _db.RentalRequests.Where(y => y.Id == x.Id).Select(y => new YeuCauThueDto { Id=y.Id, BaiDangId=y.PostId, NguoiThueId=y.TenantAccountId, ChuTroId=y.LandlordAccountId, TieuDeBaiDang=y.Post.Title, TrangThai=y.Status, GhiChu=y.Note, NgayTao=y.CreatedAt }).FirstAsync();
}

public class DepositBLL : IDepositService
{
    private readonly ApplicationDbContext _db;
    public DepositBLL(ApplicationDbContext db) => _db = db;
    public async Task<DatCocDto> TaoAsync(int nguoiThueId, TaoDatCocDto dto)
    {
        var request = await _db.RentalRequests.FirstOrDefaultAsync(x => x.Id == dto.YeuCauThueId && x.TenantAccountId == nguoiThueId && x.Status == 1) ?? throw new Exception("Yêu cầu thuê chưa được chủ trọ duyệt");
        if (dto.SoTien <= 0) throw new Exception("Số tiền đặt cọc phải lớn hơn 0");
        var item = new Deposit { RentalRequestId=request.Id, TenantAccountId=nguoiThueId, LandlordAccountId=request.LandlordAccountId, Amount=dto.SoTien, Status=0 };
        _db.Deposits.Add(item); await _db.SaveChangesAsync(); return Map(item);
    }
    public async Task<List<DatCocDto>> LayCuaToiAsync(int taiKhoanId) => await _db.Deposits.Where(x => x.TenantAccountId == taiKhoanId || x.LandlordAccountId == taiKhoanId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<DatCocDto> CapNhatTrangThaiAsync(int taiKhoanId, int id, int trangThai)
    {
        var item = await _db.Deposits.FirstOrDefaultAsync(x => x.Id == id && x.LandlordAccountId == taiKhoanId) ?? throw new Exception("Không tìm thấy khoản đặt cọc");
        item.Status = trangThai; item.PaidAt = trangThai == 1 ? DateTime.Now : item.PaidAt; await _db.SaveChangesAsync(); return Map(item);
    }
    private static DatCocDto Map(Deposit x) => new() { Id=x.Id, YeuCauThueId=x.RentalRequestId, SoTien=x.Amount, TrangThai=x.Status, NgayThanhToan=x.PaidAt, NgayTao=x.CreatedAt };
    private static System.Linq.Expressions.Expression<Func<Deposit, DatCocDto>> MapExpression() => x => new DatCocDto { Id=x.Id, YeuCauThueId=x.RentalRequestId, SoTien=x.Amount, TrangThai=x.Status, NgayThanhToan=x.PaidAt, NgayTao=x.CreatedAt };
}

public class RentalContractBLL : IRentalContractService
{
    private readonly ApplicationDbContext _db;
    public RentalContractBLL(ApplicationDbContext db) => _db = db;
    public async Task<HopDongDto> TaoAsync(int chuTroId, TaoHopDongDto dto)
    {
        var request = await _db.RentalRequests.FirstOrDefaultAsync(x => x.Id == dto.YeuCauThueId && x.LandlordAccountId == chuTroId && x.Status == 1) ?? throw new Exception("Yêu cầu thuê chưa được duyệt");
        if (dto.NgayKetThuc <= dto.NgayBatDau || dto.TienThueHangThang <= 0) throw new Exception("Thông tin thời hạn hoặc tiền thuê không hợp lệ");
        var item = new RentalContract { RentalRequestId=request.Id, PostId=request.PostId, TenantAccountId=request.TenantAccountId, LandlordAccountId=chuTroId, StartDate=dto.NgayBatDau, EndDate=dto.NgayKetThuc, MonthlyRent=dto.TienThueHangThang };
        _db.RentalContracts.Add(item); await _db.SaveChangesAsync(); return Map(item);
    }
    public async Task<List<HopDongDto>> LayCuaToiAsync(int taiKhoanId) => await _db.RentalContracts.Where(x => x.TenantAccountId == taiKhoanId || x.LandlordAccountId == taiKhoanId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<HopDongDto> XacNhanAsync(int taiKhoanId, int id)
    {
        var item = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == id && (x.TenantAccountId == taiKhoanId || x.LandlordAccountId == taiKhoanId)) ?? throw new Exception("Không tìm thấy hợp đồng");
        if (item.TenantAccountId == taiKhoanId) item.TenantConfirmed = true; else item.LandlordConfirmed = true;
        if (item.TenantConfirmed && item.LandlordConfirmed) item.Status = 1;
        await _db.SaveChangesAsync(); return Map(item);
    }
    private static HopDongDto Map(RentalContract x) => new() { Id=x.Id, YeuCauThueId=x.RentalRequestId, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, ChuTroId=x.LandlordAccountId, NgayBatDau=x.StartDate, NgayKetThuc=x.EndDate, TienThueHangThang=x.MonthlyRent, TrangThai=x.Status, NguoiThueDaXacNhan=x.TenantConfirmed, ChuTroDaXacNhan=x.LandlordConfirmed };
    private static System.Linq.Expressions.Expression<Func<RentalContract, HopDongDto>> MapExpression() => x => new HopDongDto { Id=x.Id, YeuCauThueId=x.RentalRequestId, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, ChuTroId=x.LandlordAccountId, NgayBatDau=x.StartDate, NgayKetThuc=x.EndDate, TienThueHangThang=x.MonthlyRent, TrangThai=x.Status, NguoiThueDaXacNhan=x.TenantConfirmed, ChuTroDaXacNhan=x.LandlordConfirmed };
}

public class IncidentBLL : IIncidentService
{
    private readonly ApplicationDbContext _db;
    public IncidentBLL(ApplicationDbContext db) => _db = db;
    public async Task<SuCoDto> TaoAsync(int nguoiThueId, TaoSuCoDto dto)
    {
        var contract = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == dto.HopDongId && x.TenantAccountId == nguoiThueId) ?? throw new Exception("Không tìm thấy hợp đồng thuê");
        var item = new Incident { ContractId=contract.Id, ReporterAccountId=nguoiThueId, Title=dto.TieuDe, Description=dto.MoTa };
        _db.Incidents.Add(item); await _db.SaveChangesAsync(); return Map(item);
    }
    public async Task<List<SuCoDto>> LayCuaToiAsync(int taiKhoanId) => await _db.Incidents.Where(x => x.ReporterAccountId == taiKhoanId || _db.RentalContracts.Any(c => c.Id == x.ContractId && c.LandlordAccountId == taiKhoanId)).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<SuCoDto> XuLyAsync(int chuTroId, int id, XuLySuCoDto dto)
    {
        var item = await _db.Incidents.FirstOrDefaultAsync(x => x.Id == id && _db.RentalContracts.Any(c => c.Id == x.ContractId && c.LandlordAccountId == chuTroId)) ?? throw new Exception("Không tìm thấy sự cố");
        item.Status=dto.TrangThai; item.Resolution=dto.HuongXuLy; await _db.SaveChangesAsync(); return Map(item);
    }
    private static SuCoDto Map(Incident x) => new() { Id=x.Id, HopDongId=x.ContractId, NguoiBaoCaoId=x.ReporterAccountId, TieuDe=x.Title, MoTa=x.Description, TrangThai=x.Status, HuongXuLy=x.Resolution, NgayTao=x.CreatedAt };
    private static System.Linq.Expressions.Expression<Func<Incident, SuCoDto>> MapExpression() => x => new SuCoDto { Id=x.Id, HopDongId=x.ContractId, NguoiBaoCaoId=x.ReporterAccountId, TieuDe=x.Title, MoTa=x.Description, TrangThai=x.Status, HuongXuLy=x.Resolution, NgayTao=x.CreatedAt };
}

public class RoomReviewBLL : IRoomReviewService
{
    private readonly ApplicationDbContext _db;
    public RoomReviewBLL(ApplicationDbContext db) => _db = db;
    public async Task<DanhGiaDto> TaoAsync(int nguoiThueId, TaoDanhGiaDto dto)
    {
        var contract = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == dto.HopDongId && x.TenantAccountId == nguoiThueId) ?? throw new Exception("Không tìm thấy hợp đồng thuê");
        if (dto.SoSao < 1 || dto.SoSao > 5) throw new Exception("Số sao phải từ 1 đến 5");
        if (await _db.RoomReviews.AnyAsync(x => x.ContractId == dto.HopDongId && x.TenantAccountId == nguoiThueId)) throw new Exception("Hợp đồng này đã được đánh giá");
        var item = new RoomReview { ContractId=contract.Id, PostId=contract.PostId, TenantAccountId=nguoiThueId, Rating=dto.SoSao, Comment=dto.NhanXet };
        _db.RoomReviews.Add(item); await _db.SaveChangesAsync(); return Map(item);
    }
    public async Task<List<DanhGiaDto>> LayTheoBaiDangAsync(int baiDangId) => await _db.RoomReviews.Where(x => x.PostId == baiDangId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<List<DanhGiaDto>> LayCuaToiAsync(int nguoiThueId) => await _db.RoomReviews.Where(x => x.TenantAccountId == nguoiThueId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    private static DanhGiaDto Map(RoomReview x) => new() { Id=x.Id, HopDongId=x.ContractId, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, SoSao=x.Rating, NhanXet=x.Comment, NgayTao=x.CreatedAt };
    private static System.Linq.Expressions.Expression<Func<RoomReview, DanhGiaDto>> MapExpression() => x => new DanhGiaDto { Id=x.Id, HopDongId=x.ContractId, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, SoSao=x.Rating, NhanXet=x.Comment, NgayTao=x.CreatedAt };
}
