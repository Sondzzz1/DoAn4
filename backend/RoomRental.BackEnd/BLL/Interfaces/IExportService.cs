namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IExportService
{
    Task<byte[]> XuatHopDongPdfAsync(int taiKhoanId, int hopDongId);
    Task<byte[]> XuatBaoCaoDoanhThuExcelAsync(int taiKhoanId, bool isAdmin, int? nam = null, int? thang = null);
}
