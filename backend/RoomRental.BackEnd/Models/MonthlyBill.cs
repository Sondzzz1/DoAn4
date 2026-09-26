namespace RoomRental.BackEnd.Models;

/// <summary>
/// Entity HoaDonHangThang - Quản lý chỉ số điện, nước & hóa đơn hàng tháng cho phòng trọ (PMS).
/// </summary>
public class MonthlyBill
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Chỉ số điện & đơn giá
    public decimal OldElectricity { get; set; }
    public decimal NewElectricity { get; set; }
    public decimal ElectricityPrice { get; set; }

    // Chỉ số nước & đơn giá
    public decimal OldWater { get; set; }
    public decimal NewWater { get; set; }
    public decimal WaterPrice { get; set; }

    // Tiền phòng và phụ phí
    public decimal RoomPrice { get; set; }
    public decimal OtherFees { get; set; }
    public string? OtherFeesNote { get; set; }

    // Tổng tiền thanh toán = (NewElectricity - OldElectricity) * ElectricityPrice + (NewWater - OldWater) * WaterPrice + RoomPrice + OtherFees
    public decimal TotalAmount { get; set; }

    // Trạng thái hóa đơn: 0 = Chờ thanh toán, 1 = Đã thanh toán, 2 = Đã hủy
    public int Status { get; set; } = 0;

    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public virtual RentalContract Contract { get; set; } = null!;
}
