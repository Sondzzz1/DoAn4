using RoomRental.BackEnd.DTO.Rental;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IMonthlyBillService
{
    Task<HoaDonDto> TaoAsync(int chuTroId, TaoHoaDonDto dto);
    Task<List<HoaDonDto>> LayTheoHopDongAsync(int taiKhoanId, int hopDongId);
    Task<List<HoaDonDto>> LayDanhSachCuaToiAsync(int taiKhoanId, bool isLandlord);
    Task<HoaDonDto> LayChiTietAsync(int taiKhoanId, int id);
    Task<HoaDonDto> CapNhatAsync(int chuTroId, int id, CapNhatHoaDonDto dto);
    Task<HoaDonDto> ThanhToanAsync(int taiKhoanId, int id, XacNhanThanhToanHoaDonDto? dto = null);
    Task<bool> XoaAsync(int chuTroId, int id);
}
