namespace RoomRental.BackEnd.DTO.Rental;

public class TaoYeuCauThueDto
{
    public int BaiDangId { get; set; }
    public string? GhiChu { get; set; }
}

public class CapNhatTrangThaiDto
{
    public int TrangThai { get; set; }
    public string? GhiChu { get; set; }
}

public class TaoDatCocDto
{
    public int YeuCauThueId { get; set; }
    public decimal SoTien { get; set; }
}

public class TaoHopDongDto
{
    public int YeuCauThueId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public decimal TienThueHangThang { get; set; }
}

public class TaoSuCoDto
{
    public int HopDongId { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
}

public class XuLySuCoDto
{
    public int TrangThai { get; set; }
    public string? HuongXuLy { get; set; }
}

public class TaoDanhGiaDto
{
    public int HopDongId { get; set; }
    public int SoSao { get; set; }
    public string? NhanXet { get; set; }
}

public class YeuCauThueDto
{
    public int Id { get; set; }
    public int BaiDangId { get; set; }
    public int NguoiThueId { get; set; }
    public int ChuTroId { get; set; }
    public string? TieuDeBaiDang { get; set; }
    public int TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
}

public class DatCocDto
{
    public int Id { get; set; }
    public int YeuCauThueId { get; set; }
    public decimal SoTien { get; set; }
    public int TrangThai { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public DateTime NgayTao { get; set; }
}

public class HopDongDto
{
    public int Id { get; set; }
    public int YeuCauThueId { get; set; }
    public int BaiDangId { get; set; }
    public int NguoiThueId { get; set; }
    public int ChuTroId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public decimal TienThueHangThang { get; set; }
    public int TrangThai { get; set; }
    public bool NguoiThueDaXacNhan { get; set; }
    public bool ChuTroDaXacNhan { get; set; }
}

public class SuCoDto
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int NguoiBaoCaoId { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public int TrangThai { get; set; }
    public string? HuongXuLy { get; set; }
    public DateTime NgayTao { get; set; }
}

public class DanhGiaDto
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int BaiDangId { get; set; }
    public int NguoiThueId { get; set; }
    public int SoSao { get; set; }
    public string? NhanXet { get; set; }
    public DateTime NgayTao { get; set; }
}
