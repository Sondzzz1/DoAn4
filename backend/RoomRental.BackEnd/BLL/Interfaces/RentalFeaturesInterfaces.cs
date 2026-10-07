using RoomRental.BackEnd.DTO.Rental;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IRentalRequestService
{
    Task<YeuCauThueDto> TaoAsync(int nguoiThueId, TaoYeuCauThueDto dto);
    Task<List<YeuCauThueDto>> LayCuaToiAsync(int taiKhoanId, bool chuTro);
    Task<YeuCauThueDto> CapNhatTrangThaiAsync(int chuTroId, int id, int trangThai, string? ghiChu = null, decimal? soTienDatCoc = null, DateTime? hanThanhToanCoc = null);
    Task<DatCocDto> ThietLapDatCocAsync(int chuTroId, int id, ThietLapDatCocDto dto);
    Task<YeuCauThueDto> HuyAsync(int nguoiThueId, int id);
}

public interface IDepositService
{
    Task<List<DatCocDto>> LayCuaToiAsync(int taiKhoanId);
    Task<DatCocDto> CapNhatTrangThaiAsync(int taiKhoanId, int id, int trangThai);
    Task ReconcileExpiredDepositsAsync(CancellationToken cancellationToken = default);
}

public interface IRentalContractService
{
    Task<HopDongDto> TaoAsync(int chuTroId, TaoHopDongDto dto);
    Task<List<HopDongDto>> LayCuaToiAsync(int taiKhoanId);
    Task<HopDongDto> LayChiTietAsync(int taiKhoanId, int id);
    Task<HopDongDto> XacNhanAsync(int taiKhoanId, int id);
    Task<HopDongDto> ChamDutAsync(int taiKhoanId, int id, string? lyDo = null);
    Task ReconcileContractLifecycleAsync(int pendingSignatureExpiryHours, CancellationToken cancellationToken = default);
}

public interface IIncidentService
{
    Task<SuCoDto> TaoAsync(int nguoiThueId, TaoSuCoDto dto);
    Task<List<SuCoDto>> LayCuaToiAsync(int taiKhoanId);
    Task<SuCoDto> XuLyAsync(int nguoiXuLyId, int id, XuLySuCoDto dto, bool isAdmin);
}

public interface IRoomReviewService
{
    Task<DanhGiaDto> TaoAsync(int nguoiThueId, TaoDanhGiaDto dto);
    Task<List<DanhGiaDto>> LayTheoBaiDangAsync(int baiDangId);
    Task<List<DanhGiaDto>> LayCuaToiAsync(int nguoiThueId);
}
