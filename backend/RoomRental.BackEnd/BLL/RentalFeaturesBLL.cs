using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models.Enums;
using System.Data;

namespace RoomRental.BackEnd.BLL;

public class RentalRequestBLL : IRentalRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RentalRequestBLL> _logger;

    public RentalRequestBLL(
        ApplicationDbContext db,
        INotificationService notificationService,
        ILogger<RentalRequestBLL> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<YeuCauThueDto> TaoAsync(int nguoiThueId, TaoYeuCauThueDto dto)
    {
        var tenant = await _db.Users.FirstOrDefaultAsync(x => x.Id == nguoiThueId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy người thuê.");
        if (tenant.RoleId != 1) throw BusinessRuleException.Forbidden("Chỉ người thuê mới có thể gửi yêu cầu thuê phòng.");

        var post = await _db.Posts.Include(x => x.Landlord).Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == dto.BaiDangId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy bài đăng");
        if (post.Status != PostStatus.Approved) throw BusinessRuleException.Conflict("Bài đăng chưa được duyệt hoặc không còn khả dụng.");
        if (post.Room.Status != RoomStatus.Available) throw BusinessRuleException.Conflict("Phòng hiện không còn trống.");
        if (post.Landlord.AccountId == nguoiThueId) throw BusinessRuleException.Forbidden("Chủ trọ không thể gửi yêu cầu thuê phòng của mình.");
        if (await _db.RentalRequests.AnyAsync(x => x.PostId == dto.BaiDangId && x.TenantAccountId == nguoiThueId && x.Status == RentalRequestStatus.Pending))
            throw BusinessRuleException.Conflict("Bạn đã gửi yêu cầu thuê phòng này.");
        var item = new RentalRequest { PostId = post.Id, TenantAccountId = nguoiThueId, LandlordAccountId = post.Landlord.AccountId, Note = dto.GhiChu, Status = RentalRequestStatus.Pending };
        _db.RentalRequests.Add(item);
        await _db.SaveChangesAsync();
        await SendNotificationAsync(
            post.Landlord.AccountId,
            "Yêu cầu thuê phòng mới",
            $"Có một yêu cầu thuê mới cho tin '{post.Title}'.",
            "/landlord/posts");
        return await Map(item);
    }

    public async Task<List<YeuCauThueDto>> LayCuaToiAsync(int taiKhoanId, bool chuTro)
    {
        var q = _db.RentalRequests.Include(x => x.Post).AsQueryable();
        q = chuTro ? q.Where(x => x.LandlordAccountId == taiKhoanId) : q.Where(x => x.TenantAccountId == taiKhoanId);
        return await q.OrderByDescending(x => x.CreatedAt).Select(x => new YeuCauThueDto { Id=x.Id, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, ChuTroId=x.LandlordAccountId, TieuDeBaiDang=x.Post.Title, AnhPhong=x.Post.Room.Images.OrderByDescending(i => i.IsThumbnail).Select(i => i.ImageUrl).FirstOrDefault(), GiaThue=x.Post.DisplayPrice, DiaChi=x.Post.Room.Address, TenNguoiThue=_db.Users.Where(u => u.Id == x.TenantAccountId).Select(u => u.FullName).FirstOrDefault(), SdtNguoiThue=_db.Users.Where(u => u.Id == x.TenantAccountId).Select(u => u.Phone).FirstOrDefault(), TrangThai=x.Status, GhiChu=x.Note, NgayTao=x.CreatedAt }).ToListAsync();
    }

    public async Task<YeuCauThueDto> CapNhatTrangThaiAsync(int chuTroId, int id, int trangThai, string? ghiChu = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.RentalRequests
            .Include(x => x.Post).ThenInclude(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu thuê phòng");
        if (chuTroId != 0 && item.LandlordAccountId != chuTroId) throw BusinessRuleException.Forbidden("Bạn không có quyền xử lý yêu cầu thuê phòng này.");
        if (trangThai != RentalRequestStatus.Approved && trangThai != RentalRequestStatus.Rejected)
            throw new BusinessRuleException("Chỉ có thể chấp nhận hoặc từ chối yêu cầu thuê phòng.");
        if (item.Status != RentalRequestStatus.Pending) throw BusinessRuleException.Conflict("Yêu cầu thuê phòng này không còn ở trạng thái chờ xử lý.");

        if (trangThai == RentalRequestStatus.Approved)
        {
            var room = item.Post.Room;
            if (room.Status != RoomStatus.Available)
                throw BusinessRuleException.Conflict("Phòng đã được giữ chỗ hoặc không còn trống.");

            var hasOtherApprovedRequest = await _db.RentalRequests
                .Include(x => x.Post)
                .AnyAsync(x => x.Id != item.Id &&
                    x.Status == RentalRequestStatus.Approved &&
                    x.Post.RoomId == room.Id);
            if (hasOtherApprovedRequest)
                throw BusinessRuleException.Conflict("Phòng đã có yêu cầu thuê được duyệt.");

            room.Status = RoomStatus.Reserved;
            room.UpdatedAt = DateTime.Now;
        }

        item.Status = trangThai;
        if (trangThai == RentalRequestStatus.Rejected && !string.IsNullOrWhiteSpace(ghiChu)) item.Note = ghiChu.Trim();
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await SendNotificationAsync(
            item.TenantAccountId,
            trangThai == RentalRequestStatus.Approved ? "Yêu cầu thuê đã được duyệt" : "Yêu cầu thuê bị từ chối",
            trangThai == RentalRequestStatus.Approved
                ? $"Yêu cầu thuê cho tin '{item.Post.Title}' đã được duyệt. Bạn có thể tiếp tục đặt cọc."
                : $"Yêu cầu thuê cho tin '{item.Post.Title}' đã bị từ chối.",
            "/tenant/rentals");
        return await Map(item);
    }

    public async Task<YeuCauThueDto> HuyAsync(int nguoiThueId, int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.RentalRequests
            .Include(x => x.Post).ThenInclude(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu thuê phòng");
        if (item.TenantAccountId != nguoiThueId) throw BusinessRuleException.Forbidden("Bạn không có quyền hủy yêu cầu này.");
        if (item.Status is not (RentalRequestStatus.Pending or RentalRequestStatus.Approved))
            throw BusinessRuleException.Conflict("Chỉ có thể hủy yêu cầu đang chờ xử lý hoặc đã được duyệt.");

        if (item.Status == RentalRequestStatus.Approved)
        {
            if (await _db.Deposits.AnyAsync(x => x.RentalRequestId == item.Id))
                throw BusinessRuleException.Conflict("Không thể hủy yêu cầu đã có khoản cọc. Hãy xử lý hoàn cọc trước.");
            if (await _db.RentalContracts.AnyAsync(x => x.RentalRequestId == item.Id))
                throw BusinessRuleException.Conflict("Không thể hủy yêu cầu đã được chuyển thành hợp đồng.");

            var room = item.Post.Room;
            var hasOtherApprovedRequest = await _db.RentalRequests
                .Include(x => x.Post)
                .AnyAsync(x => x.Id != item.Id &&
                    x.Status == RentalRequestStatus.Approved &&
                    x.Post.RoomId == room.Id);
            if (!hasOtherApprovedRequest && room.Status == RoomStatus.Reserved)
            {
                room.Status = RoomStatus.Available;
                room.UpdatedAt = DateTime.Now;
            }
        }

        item.Status = RentalRequestStatus.Cancelled;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await SendNotificationAsync(
            item.LandlordAccountId,
            "Yêu cầu thuê đã bị hủy",
            $"Khách thuê đã hủy yêu cầu cho tin '{item.Post.Title}'.",
            "/landlord/posts");
        return await Map(item);
    }

    private async Task SendNotificationAsync(int accountId, string title, string content, string link)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(accountId, title, content, 1, link);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi thông báo cho yêu cầu thuê");
        }
    }

    private async Task<YeuCauThueDto> Map(RentalRequest x) => await _db.RentalRequests.Where(y => y.Id == x.Id).Select(y => new YeuCauThueDto { Id=y.Id, BaiDangId=y.PostId, NguoiThueId=y.TenantAccountId, ChuTroId=y.LandlordAccountId, TieuDeBaiDang=y.Post.Title, AnhPhong=y.Post.Room.Images.OrderByDescending(i => i.IsThumbnail).Select(i => i.ImageUrl).FirstOrDefault(), GiaThue=y.Post.DisplayPrice, DiaChi=y.Post.Room.Address, TenNguoiThue=_db.Users.Where(u => u.Id == y.TenantAccountId).Select(u => u.FullName).FirstOrDefault(), SdtNguoiThue=_db.Users.Where(u => u.Id == y.TenantAccountId).Select(u => u.Phone).FirstOrDefault(), TrangThai=y.Status, GhiChu=y.Note, NgayTao=y.CreatedAt }).FirstAsync();
}

public class DepositBLL : IDepositService
{
    private readonly ApplicationDbContext _db;
    public DepositBLL(ApplicationDbContext db) => _db = db;
    public async Task<DatCocDto> TaoAsync(int nguoiThueId, TaoDatCocDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await _db.RentalRequests.Include(x => x.Post).ThenInclude(x => x.Room).FirstOrDefaultAsync(x => x.Id == dto.YeuCauThueId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu thuê phòng.");
        if (request.TenantAccountId != nguoiThueId) throw BusinessRuleException.Forbidden("Bạn không có quyền tạo khoản cọc cho yêu cầu này.");
        if (request.Status != RentalRequestStatus.Approved) throw BusinessRuleException.Conflict("Yêu cầu thuê chưa được chủ trọ duyệt");
        var room = request.Post.Room;
        if (room.Status is not (RoomStatus.Available or RoomStatus.Reserved))
            throw BusinessRuleException.Conflict("Phòng hiện không còn trong trạng thái giữ chỗ.");
        var hasOtherApprovedRequest = await _db.RentalRequests
            .Include(x => x.Post)
            .AnyAsync(x => x.Id != request.Id &&
                x.Status == RentalRequestStatus.Approved &&
                x.Post.RoomId == room.Id);
        if (hasOtherApprovedRequest)
            throw BusinessRuleException.Conflict("Phòng đang được giữ bởi yêu cầu thuê khác.");
        if (dto.SoTien <= 0) throw new BusinessRuleException("Số tiền đặt cọc phải lớn hơn 0");
        if (await _db.Deposits.AnyAsync(x => x.RentalRequestId == request.Id)) throw BusinessRuleException.Conflict("Khoản cọc cho yêu cầu thuê này đã tồn tại.");
        if (room.Status == RoomStatus.Available)
        {
            room.Status = RoomStatus.Reserved;
            room.UpdatedAt = DateTime.Now;
        }
        var item = new Deposit { RentalRequestId=request.Id, TenantAccountId=nguoiThueId, LandlordAccountId=request.LandlordAccountId, Amount=dto.SoTien, Status=DepositStatus.Pending };
        _db.Deposits.Add(item);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Map(item);
    }
    public async Task<List<DatCocDto>> LayCuaToiAsync(int taiKhoanId) => await _db.Deposits.Where(x => x.TenantAccountId == taiKhoanId || x.LandlordAccountId == taiKhoanId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<DatCocDto> CapNhatTrangThaiAsync(int taiKhoanId, int id, int trangThai)
    {
        var item = await _db.Deposits.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy khoản đặt cọc");
        if (taiKhoanId != 0 && item.LandlordAccountId != taiKhoanId) throw BusinessRuleException.Forbidden("Bạn không có quyền xác nhận khoản cọc này.");
        if (trangThai != DepositStatus.Confirmed) throw new BusinessRuleException("Chủ trọ chỉ có thể xác nhận khoản cọc đã thanh toán.");
        if (item.Status != DepositStatus.Paid) throw BusinessRuleException.Conflict("Chỉ khoản cọc đã thanh toán mới có thể được xác nhận.");
        item.Status = DepositStatus.Confirmed; await _db.SaveChangesAsync(); return Map(item);
    }
    private static DatCocDto Map(Deposit x) => new() { Id=x.Id, YeuCauThueId=x.RentalRequestId, SoTien=x.Amount, TrangThai=x.Status, NgayThanhToan=x.PaidAt, NgayTao=x.CreatedAt };
    private static System.Linq.Expressions.Expression<Func<Deposit, DatCocDto>> MapExpression() => x => new DatCocDto { Id=x.Id, YeuCauThueId=x.RentalRequestId, SoTien=x.Amount, TrangThai=x.Status, NgayThanhToan=x.PaidAt, NgayTao=x.CreatedAt };
}

public class RentalContractBLL : IRentalContractService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RentalContractBLL> _logger;

    public RentalContractBLL(
        ApplicationDbContext db,
        INotificationService notificationService,
        ILogger<RentalContractBLL> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<HopDongDto> TaoAsync(int chuTroId, TaoHopDongDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await _db.RentalRequests
            .Include(x => x.Post).ThenInclude(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == dto.YeuCauThueId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu thuê phòng.");

        if (chuTroId != 0 && request.LandlordAccountId != chuTroId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền tạo hợp đồng cho yêu cầu này.");
        if (request.Status != RentalRequestStatus.Approved)
            throw BusinessRuleException.Conflict("Yêu cầu thuê chưa được duyệt hoặc đã được chuyển thành hợp đồng.");
        var room = request.Post.Room;
        if (room.Status is not (RoomStatus.Available or RoomStatus.Reserved))
            throw BusinessRuleException.Conflict("Phòng hiện không còn trong trạng thái giữ chỗ.");
        var hasOtherApprovedRequest = await _db.RentalRequests
            .Include(x => x.Post)
            .AnyAsync(x => x.Id != request.Id &&
                x.Status == RentalRequestStatus.Approved &&
                x.Post.RoomId == room.Id);
        if (hasOtherApprovedRequest)
            throw BusinessRuleException.Conflict("Phòng đang được giữ bởi yêu cầu thuê khác.");
        if (request.TenantAccountId == request.LandlordAccountId)
            throw BusinessRuleException.Conflict("Người thuê và chủ trọ không thể là cùng một tài khoản.");

        var confirmedDeposit = await _db.Deposits.FirstOrDefaultAsync(d =>
            d.RentalRequestId == request.Id && d.Status == DepositStatus.Confirmed);
        if (confirmedDeposit == null)
            throw BusinessRuleException.Conflict("Khoản cọc phải được chủ trọ xác nhận trước khi tạo hợp đồng.");

        if (dto.NgayKetThuc <= dto.NgayBatDau)
            throw new BusinessRuleException("Ngày kết thúc hợp đồng phải sau ngày bắt đầu.");
        if (dto.TienThueHangThang <= 0)
            throw new BusinessRuleException("Tiền thuê hàng tháng phải lớn hơn 0.");

        if (await _db.RentalContracts.AnyAsync(c => c.RentalRequestId == request.Id &&
            RentalContractStatus.EffectiveStatuses.Contains(c.Status)))
            throw BusinessRuleException.Conflict("Yêu cầu thuê này đã có hợp đồng.");

        var roomId = request.Post.RoomId;
        var hasOverlappingContract = await _db.RentalContracts
            .Include(c => c.Post)
            .AnyAsync(c => c.Post!.RoomId == roomId &&
                RentalContractStatus.EffectiveStatuses.Contains(c.Status) &&
                c.StartDate <= dto.NgayKetThuc && c.EndDate >= dto.NgayBatDau);
        if (hasOverlappingContract)
            throw BusinessRuleException.Conflict("Phòng đã có hợp đồng trùng thời gian.");

        var item = new RentalContract
        {
            RentalRequestId = request.Id,
            PostId = request.PostId,
            TenantAccountId = request.TenantAccountId,
            LandlordAccountId = request.LandlordAccountId,
            StartDate = dto.NgayBatDau,
            EndDate = dto.NgayKetThuc,
            MonthlyRent = dto.TienThueHangThang,
            DepositAmount = confirmedDeposit.Amount,
            ElectricityPrice = request.Post.Room.ElectricityPrice ?? 0,
            WaterPrice = request.Post.Room.WaterPrice ?? 0,
            ServiceFee = request.Post.Room.ServiceFee ?? 0,
            Terms = string.IsNullOrWhiteSpace(dto.DieuKhoan) ? null : dto.DieuKhoan.Trim(),
            Status = RentalContractStatus.PendingSignature,
            CreatedAt = DateTime.Now
        };

        _db.RentalContracts.Add(item);
        request.Status = RentalRequestStatus.ConvertedToContract;
        if (room.Status == RoomStatus.Available)
        {
            room.Status = RoomStatus.Reserved;
            room.UpdatedAt = DateTime.Now;
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await SendNotificationAsync(
            item.TenantAccountId,
            "Có hợp đồng thuê chờ xác nhận",
            $"Chủ trọ đã tạo hợp đồng cho tin '{request.Post.Title}'. Vui lòng kiểm tra và xác nhận.",
            "/tenant/rentals");

        return await MapToDtoAsync(item);
    }

    public async Task<List<HopDongDto>> LayCuaToiAsync(int taiKhoanId)
    {
        await ExpireEndedContractsAsync();
        var query = _db.RentalContracts.AsQueryable();
        if (taiKhoanId != 0)
        {
            query = query.Where(x => x.TenantAccountId == taiKhoanId || x.LandlordAccountId == taiKhoanId);
        }

        var contracts = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var result = new List<HopDongDto>();
        foreach (var c in contracts)
        {
            result.Add(await MapToDtoAsync(c));
        }
        return result;
    }

    public async Task<HopDongDto> LayChiTietAsync(int taiKhoanId, int id)
    {
        await ExpireEndedContractsAsync();
        var contract = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng.");

        if (taiKhoanId != 0 && contract.TenantAccountId != taiKhoanId && contract.LandlordAccountId != taiKhoanId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền xem hợp đồng này.");

        return await MapToDtoAsync(contract);
    }

    public async Task<HopDongDto> XacNhanAsync(int taiKhoanId, int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng.");
        if (taiKhoanId != 0 && item.TenantAccountId != taiKhoanId && item.LandlordAccountId != taiKhoanId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền xác nhận hợp đồng này.");
        if (item.Status != RentalContractStatus.PendingSignature)
            throw BusinessRuleException.Conflict("Hợp đồng này không còn ở trạng thái chờ ký.");

        if (taiKhoanId == item.TenantAccountId)
        {
            if (item.TenantConfirmed) throw BusinessRuleException.Conflict("Bạn đã xác nhận hợp đồng này.");
            item.TenantConfirmed = true;
        }
        if (taiKhoanId == item.LandlordAccountId || taiKhoanId == 0)
        {
            if (item.LandlordConfirmed) throw BusinessRuleException.Conflict("Chủ trọ đã xác nhận hợp đồng này.");
            item.LandlordConfirmed = true;
        }

        if (item.TenantConfirmed && item.LandlordConfirmed)
        {
            var post = await _db.Posts.Include(p => p.Room).FirstAsync(p => p.Id == item.PostId);
            var hasOtherActiveContract = await _db.RentalContracts
                .Include(c => c.Post)
                .AnyAsync(c => c.Id != item.Id && c.Post!.RoomId == post.RoomId &&
                    c.Status == RentalContractStatus.Active &&
                    c.StartDate <= item.EndDate && c.EndDate >= item.StartDate);
            if (hasOtherActiveContract)
                throw BusinessRuleException.Conflict("Phòng đã có hợp đồng đang hoạt động trùng thời gian.");
            if (post.Room.Status is not (RoomStatus.Available or RoomStatus.Reserved))
                throw BusinessRuleException.Conflict("Phòng không còn ở trạng thái sẵn sàng cho thuê.");

            item.Status = RentalContractStatus.Active;
            item.UpdatedAt = DateTime.Now;
            post.Room.Status = RoomStatus.Rented;
            post.Room.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        var recipientId = taiKhoanId == item.TenantAccountId ? item.LandlordAccountId : item.TenantAccountId;
        var content = item.Status == RentalContractStatus.Active
            ? "Hai bên đã xác nhận. Hợp đồng đã có hiệu lực."
            : "Bên còn lại đã xác nhận hợp đồng. Vui lòng kiểm tra và xác nhận.";
        var link = recipientId == item.LandlordAccountId ? "/landlord/contracts" : "/tenant/rentals";
        await SendNotificationAsync(recipientId, "Cập nhật xác nhận hợp đồng", content, link);
        return await MapToDtoAsync(item);
    }

    public async Task<HopDongDto> ChamDutAsync(int taiKhoanId, int id, string? lyDo = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng.");
        if (taiKhoanId != 0 && item.LandlordAccountId != taiKhoanId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền chấm dứt hợp đồng này.");
        if (item.Status != RentalContractStatus.Active)
            throw BusinessRuleException.Conflict("Chỉ hợp đồng đang hiệu lực mới có thể chấm dứt.");

        item.Status = RentalContractStatus.Terminated;
        item.UpdatedAt = DateTime.Now;

        var post = await _db.Posts.Include(p => p.Room).FirstOrDefaultAsync(p => p.Id == item.PostId);
        if (post?.Room != null)
        {
            var hasOtherActiveContract = await _db.RentalContracts
                .Include(c => c.Post)
                .AnyAsync(c => c.Id != item.Id && c.Post!.RoomId == post.RoomId && c.Status == RentalContractStatus.Active);
            if (!hasOtherActiveContract)
            {
                post.Room.Status = RoomStatus.Available;
                post.Room.UpdatedAt = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await SendNotificationAsync(
            item.TenantAccountId,
            "Hợp đồng đã chấm dứt",
            string.IsNullOrWhiteSpace(lyDo) ? "Chủ trọ đã chấm dứt hợp đồng thuê." : $"Chủ trọ đã chấm dứt hợp đồng: {lyDo.Trim()}",
            "/tenant/rentals");
        return await MapToDtoAsync(item);
    }

    private async Task SendNotificationAsync(int accountId, string title, string content, string link)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(accountId, title, content, 1, link);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi thông báo cho hợp đồng thuê");
        }
    }

    private async Task ExpireEndedContractsAsync()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var expired = await _db.RentalContracts
            .Include(c => c.Post).ThenInclude(p => p!.Room)
            .Where(c => c.Status == RentalContractStatus.Active && c.EndDate < DateTime.Now)
            .ToListAsync();
        if (expired.Count == 0)
        {
            await transaction.CommitAsync();
            return;
        }

        foreach (var contract in expired)
        {
            contract.Status = RentalContractStatus.Expired;
            contract.UpdatedAt = DateTime.Now;
        }
        await _db.SaveChangesAsync();
        foreach (var room in expired.Select(c => c.Post!.Room).DistinctBy(r => r.Id))
        {
            var hasActive = await _db.RentalContracts.Include(c => c.Post)
                .AnyAsync(c => c.Status == RentalContractStatus.Active && c.Post!.RoomId == room.Id);
            if (!hasActive) room.Status = RoomStatus.Available;
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ReconcileContractLifecycleAsync(int pendingSignatureExpiryHours, CancellationToken cancellationToken = default)
    {
        await ExpireEndedContractsAsync();

        var expiryHours = Math.Clamp(pendingSignatureExpiryHours, 1, 24 * 30);
        var expiryCutoff = DateTime.Now.AddHours(-expiryHours);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var staleContracts = await _db.RentalContracts
            .Where(contract => contract.Status == RentalContractStatus.PendingSignature && contract.CreatedAt <= expiryCutoff)
            .ToListAsync(cancellationToken);

        if (staleContracts.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var requestIds = staleContracts.Select(contract => contract.RentalRequestId).Distinct().ToList();
        var staleContractIds = staleContracts.Select(contract => contract.Id).ToList();
        var requestIdsWithOtherEffectiveContracts = await _db.RentalContracts
            .Where(contract => requestIds.Contains(contract.RentalRequestId) &&
                !staleContractIds.Contains(contract.Id) &&
                RentalContractStatus.EffectiveStatuses.Contains(contract.Status))
            .Select(contract => contract.RentalRequestId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var requests = await _db.RentalRequests
            .Where(request => requestIds.Contains(request.Id) && request.Status == RentalRequestStatus.ConvertedToContract)
            .ToDictionaryAsync(request => request.Id, cancellationToken);

        foreach (var contract in staleContracts)
        {
            contract.Status = RentalContractStatus.Cancelled;
            contract.UpdatedAt = DateTime.Now;
            if (!requestIdsWithOtherEffectiveContracts.Contains(contract.RentalRequestId) &&
                requests.TryGetValue(contract.RentalRequestId, out var request))
                request.Status = RentalRequestStatus.Approved;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var contract in staleContracts)
        {
            await SendNotificationAsync(
                contract.TenantAccountId,
                "Hợp đồng chờ ký đã hết hạn",
                "Hợp đồng chưa được hai bên xác nhận đúng hạn. Vui lòng liên hệ chủ trọ để tạo lại hoặc xử lý khoản cọc.",
                "/tenant/rentals");
            await SendNotificationAsync(
                contract.LandlordAccountId,
                "Hợp đồng chờ ký đã hết hạn",
                "Hợp đồng chưa đủ chữ ký đã được hủy. Yêu cầu thuê đã quay lại trạng thái được duyệt để bạn xử lý tiếp.",
                "/landlord/contracts");
        }
    }

    private async Task<HopDongDto> MapToDtoAsync(RentalContract x)
    {
        var post = await _db.Posts.Include(p => p.Room).FirstOrDefaultAsync(p => p.Id == x.PostId);
        var tenant = await _db.Users.FirstOrDefaultAsync(u => u.Id == x.TenantAccountId);
        var landlord = await _db.Users.FirstOrDefaultAsync(u => u.Id == x.LandlordAccountId);
        return new HopDongDto
        {
            Id = x.Id,
            YeuCauThueId = x.RentalRequestId,
            BaiDangId = x.PostId,
            NguoiThueId = x.TenantAccountId,
            ChuTroId = x.LandlordAccountId,
            NgayBatDau = x.StartDate,
            NgayKetThuc = x.EndDate,
            TienThueHangThang = x.MonthlyRent,
            TienDatCoc = x.DepositAmount,
            GiaDien = x.ElectricityPrice,
            GiaNuoc = x.WaterPrice,
            PhiDichVu = x.ServiceFee,
            DieuKhoan = x.Terms,
            TrangThai = x.Status,
            NguoiThueDaXacNhan = x.TenantConfirmed,
            ChuTroDaXacNhan = x.LandlordConfirmed,
            NgayTao = x.CreatedAt,
            TenPhong = post?.Title ?? post?.Room?.RoomName ?? $"Hợp đồng #{x.Id}",
            DiaChiPhong = post?.Room != null ? $"{post.Room.Address}, {post.Room.Ward}, {post.Room.District}, {post.Room.Province}" : "",
            TenNguoiThue = tenant?.FullName ?? $"Khách thuê #{x.TenantAccountId}",
            SdtNguoiThue = tenant?.Phone ?? "",
            TenChuTro = landlord?.FullName ?? $"Chủ trọ #{x.LandlordAccountId}",
            SdtChuTro = landlord?.Phone ?? ""
        };
    }
}

public class IncidentBLL : IIncidentService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<IncidentBLL> _logger;

    public IncidentBLL(
        ApplicationDbContext db,
        INotificationService notificationService,
        ILogger<IncidentBLL> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }
    public async Task<SuCoDto> TaoAsync(int nguoiThueId, TaoSuCoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TieuDe) || string.IsNullOrWhiteSpace(dto.MoTa))
            throw new BusinessRuleException("Tiêu đề và mô tả sự cố là bắt buộc.");
        if (dto.TieuDe.Trim().Length > 200 || dto.MoTa.Trim().Length > 2000)
            throw new BusinessRuleException("Tiêu đề hoặc mô tả sự cố vượt quá độ dài cho phép.");

        var contract = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == dto.HopDongId && x.TenantAccountId == nguoiThueId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng thuê.");
        if (contract.Status != RentalContractStatus.Active)
            throw BusinessRuleException.Conflict("Chỉ có thể báo sự cố cho hợp đồng đang hiệu lực.");

        var item = new Incident
        {
            ContractId = contract.Id,
            ReporterAccountId = nguoiThueId,
            Title = dto.TieuDe.Trim(),
            Description = dto.MoTa.Trim(),
            Status = IncidentStatus.Pending
        };
        _db.Incidents.Add(item);
        await _db.SaveChangesAsync();
        await SendNotificationAsync(
            contract.LandlordAccountId,
            "Có sự cố mới cần xử lý",
            $"Khách thuê đã báo sự cố: '{item.Title}'.",
            "/landlord/contracts");
        return Map(item);
    }
    public async Task<List<SuCoDto>> LayCuaToiAsync(int taiKhoanId) => await _db.Incidents.Where(x => x.ReporterAccountId == taiKhoanId || _db.RentalContracts.Any(c => c.Id == x.ContractId && c.LandlordAccountId == taiKhoanId)).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<SuCoDto> XuLyAsync(int nguoiXuLyId, int id, XuLySuCoDto dto, bool isAdmin)
    {
        if (dto.TrangThai is not (IncidentStatus.InProgress or IncidentStatus.Resolved or IncidentStatus.Rejected))
            throw new BusinessRuleException("Trạng thái xử lý sự cố không hợp lệ.");
        if (dto.TrangThai is IncidentStatus.Resolved or IncidentStatus.Rejected && string.IsNullOrWhiteSpace(dto.HuongXuLy))
            throw new BusinessRuleException("Cần nhập hướng xử lý khi đóng sự cố.");
        if (dto.HuongXuLy?.Trim().Length > 2000)
            throw new BusinessRuleException("Hướng xử lý vượt quá độ dài cho phép.");

        var item = await _db.Incidents.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy sự cố.");
        var isLandlord = await _db.RentalContracts.AnyAsync(c => c.Id == item.ContractId && c.LandlordAccountId == nguoiXuLyId);
        if (!isAdmin && !isLandlord)
            throw BusinessRuleException.Forbidden("Bạn không có quyền xử lý sự cố này.");

        var transitionAllowed = item.Status switch
        {
            IncidentStatus.Pending => dto.TrangThai is IncidentStatus.InProgress or IncidentStatus.Resolved or IncidentStatus.Rejected,
            IncidentStatus.InProgress => dto.TrangThai is IncidentStatus.Resolved or IncidentStatus.Rejected,
            _ => false
        };
        if (!transitionAllowed)
            throw BusinessRuleException.Conflict("Không thể chuyển sự cố từ trạng thái hiện tại.");

        item.Status = dto.TrangThai;
        item.Resolution = string.IsNullOrWhiteSpace(dto.HuongXuLy) ? null : dto.HuongXuLy.Trim();
        item.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await SendNotificationAsync(
            item.ReporterAccountId,
            dto.TrangThai == IncidentStatus.InProgress ? "Sự cố đang được xử lý" : "Sự cố đã được cập nhật",
            dto.TrangThai == IncidentStatus.InProgress
                ? $"Sự cố '{item.Title}' đang được xử lý."
                : $"Sự cố '{item.Title}' đã được xử lý: {item.Resolution}",
            "/tenant/rentals");
        return Map(item);
    }

    private async Task SendNotificationAsync(int accountId, string title, string content, string link)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(accountId, title, content, 1, link);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi thông báo cho sự cố {Title}", title);
        }
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
        var tenant = await _db.Users.FirstOrDefaultAsync(x => x.Id == nguoiThueId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy người dùng.");
        if (tenant.RoleId != 1)
            throw BusinessRuleException.Forbidden("Chỉ người thuê mới có thể đánh giá phòng.");

        var contract = await _db.RentalContracts.FirstOrDefaultAsync(x => x.Id == dto.HopDongId)
            ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng thuê.");
        if (contract.TenantAccountId != nguoiThueId)
            throw BusinessRuleException.Forbidden("Bạn không có quyền đánh giá hợp đồng này.");
        if (contract.Status is not (RentalContractStatus.Active or RentalContractStatus.Expired or RentalContractStatus.Terminated))
            throw BusinessRuleException.Conflict("Hợp đồng chưa đủ điều kiện để đánh giá.");
        if (dto.SoSao < 1 || dto.SoSao > 5) throw new BusinessRuleException("Số sao phải từ 1 đến 5.");
        if (await _db.RoomReviews.AnyAsync(x => x.ContractId == dto.HopDongId && x.TenantAccountId == nguoiThueId))
            throw BusinessRuleException.Conflict("Hợp đồng này đã được đánh giá.");
        var item = new RoomReview { ContractId=contract.Id, PostId=contract.PostId, TenantAccountId=nguoiThueId, Rating=dto.SoSao, Comment=dto.NhanXet };
        _db.RoomReviews.Add(item); await _db.SaveChangesAsync(); return Map(item);
    }
    public async Task<List<DanhGiaDto>> LayTheoBaiDangAsync(int baiDangId) => await _db.RoomReviews.Where(x => x.PostId == baiDangId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    public async Task<List<DanhGiaDto>> LayCuaToiAsync(int nguoiThueId) => await _db.RoomReviews.Where(x => x.TenantAccountId == nguoiThueId).OrderByDescending(x => x.CreatedAt).Select(MapExpression()).ToListAsync();
    private static DanhGiaDto Map(RoomReview x) => new() { Id=x.Id, HopDongId=x.ContractId, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, SoSao=x.Rating, NhanXet=x.Comment, NgayTao=x.CreatedAt };
    private static System.Linq.Expressions.Expression<Func<RoomReview, DanhGiaDto>> MapExpression() => x => new DanhGiaDto { Id=x.Id, HopDongId=x.ContractId, BaiDangId=x.PostId, NguoiThueId=x.TenantAccountId, SoSao=x.Rating, NhanXet=x.Comment, NgayTao=x.CreatedAt };
}
