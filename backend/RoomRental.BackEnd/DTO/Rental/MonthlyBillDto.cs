namespace RoomRental.BackEnd.DTO.Rental;

public class TaoHoaDonDto
{
    public int HopDongId { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }

    public decimal SoDienCu { get; set; }
    public decimal SoDienMoi { get; set; }
    public decimal GiaDien { get; set; }

    public decimal SoNuocCu { get; set; }
    public decimal SoNuocMoi { get; set; }
    public decimal GiaNuoc { get; set; }

    public decimal? TienPhong { get; set; }
    public decimal ChiPhiKhac { get; set; } = 0;
    public string? GhiChuChiPhiKhac { get; set; }

    public DateTime? HanThanhToan { get; set; }
    public string? GhiChu { get; set; }
}

public class CapNhatHoaDonDto
{
    public decimal SoDienCu { get; set; }
    public decimal SoDienMoi { get; set; }
    public decimal GiaDien { get; set; }

    public decimal SoNuocCu { get; set; }
    public decimal SoNuocMoi { get; set; }
    public decimal GiaNuoc { get; set; }

    public decimal TienPhong { get; set; }
    public decimal ChiPhiKhac { get; set; }
    public string? GhiChuChiPhiKhac { get; set; }

    public int? TrangThai { get; set; }
    public DateTime? HanThanhToan { get; set; }
    public string? GhiChu { get; set; }
}

public class XacNhanThanhToanHoaDonDto
{
    public string? PhuongThucThanhToan { get; set; } = "Tiền mặt";
    public string? GhiChu { get; set; }
}

public class HoaDonDto
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }

    public decimal SoDienCu { get; set; }
    public decimal SoDienMoi { get; set; }
    public decimal SoDienTieuThu => Math.Max(0, SoDienMoi - SoDienCu);
    public decimal GiaDien { get; set; }
    public decimal TienDien => Math.Max(0, SoDienMoi - SoDienCu) * GiaDien;

    public decimal SoNuocCu { get; set; }
    public decimal SoNuocMoi { get; set; }
    public decimal SoNuocTieuThu => Math.Max(0, SoNuocMoi - SoNuocCu);
    public decimal GiaNuoc { get; set; }
    public decimal TienNuoc => Math.Max(0, SoNuocMoi - SoNuocCu) * GiaNuoc;

    public decimal TienPhong { get; set; }
    public decimal ChiPhiKhac { get; set; }
    public string? GhiChuChiPhiKhac { get; set; }

    public decimal TongTien { get; set; }
    public int TrangThai { get; set; } // 0: Chờ thanh toán, 1: Đã thanh toán, 2: Đã hủy
    public DateTime? HanThanhToan { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    // Thông tin thêm
    public string? TenPhong { get; set; }
    public string? DiaChiPhong { get; set; }
    public string? TenNguoiThue { get; set; }
    public string? SdtNguoiThue { get; set; }
    public string? TenChuTro { get; set; }
    public string? SdtChuTro { get; set; }
}
