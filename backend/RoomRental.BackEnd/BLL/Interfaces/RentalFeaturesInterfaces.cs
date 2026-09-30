using RoomRental.BackEnd.DTO.Rental;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IRentalRequestService
{
    Task<YeuCauThueDto> TaoAsync(int nguoiThueId, TaoYeuCauThueDto dto);
    Task<List<YeuCauThueDto>> LayCuaToiAsync(int taiKhoanId, bool chuTro);
    Task<YeuCauThueDto> CapNhatTrangThaiAsync(int chuTroId, int id, int trangThai, string? ghiChu = null);
    Task<YeuCauThueDto> HuyAsync(int nguoiThueId, int id);
}

public interface IDepositService
{
    Task<DatCocDto> TaoAsync(int nguoiThueId, TaoDatCocDto dto);
    Task<List<DatCocDto>> LayCuaToiAsync(int taiKhoanId);
    Task<DatCocDto> CapNhatTrangThaiAsync(int taiKhoanId, int id, int trangThai);
}

public interface IRentalContractService
{
    Task<HopDongDto> TaoAsync(int chuTroId, TaoHopDongDto dto);
    Task<List<HopDongDto>> LayCuaToiAsync(int taiKhoanId);
    Task<HopDongDto> LayChiTietAsync(int taiKhoanId, int id);
    Task<HopDongDto> XacNhanAsync(int taiKhoanId, int id);
    Task<HopDongDto> ChamDutAsync(int taiKhoanId, int id, string? lyDo = null);
}

public interface IIncidentService
{
    Task<SuCoDto> TaoAsync(int nguoiThueId, TaoSuCoDto dto);
    Task<List<SuCoDto>> LayCuaToiAsync(int taiKhoanId);
    Task<SuCoDto> XuLyAsync(int chuTroId, int id, XuLySuCoDto dto);
}

public interface IRoomReviewService
{
    Task<DanhGiaDto> TaoAsync(int nguoiThueId, TaoDanhGiaDto dto);
    Task<List<DanhGiaDto>> LayTheoBaiDangAsync(int baiDangId);
    Task<List<DanhGiaDto>> LayCuaToiAsync(int nguoiThueId);
}
