using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;

namespace RoomRental.BackEnd.BLL;

public class PaymentBLL : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly INotificationService _notificationService;

    public PaymentBLL(
        ApplicationDbContext context,
        IConfiguration config,
        INotificationService notificationService)
    {
        _context = context;
        _config = config;
        _notificationService = notificationService;
    }

    public async Task<PaymentResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto dto, string clientIp)
    {
        var deposit = await _context.Deposits
            .FirstOrDefaultAsync(d => d.Id == dto.DepositId && d.TenantAccountId == userId)
            ?? throw new Exception("Không tìm thấy khoản đặt cọc hoặc bạn không có quyền thanh toán.");

        if (deposit.Status == 1)
        {
            throw new Exception("Khoản đặt cọc này đã được thanh toán trước đó.");
        }

        var tmnCode = _config["VnPay:TmnCode"] ?? "2QXUI4J4";
        var hashSecret = _config["VnPay:HashSecret"] ?? "RAOERGKBMVUXIAKSRBUEHNQAGWIDFUJK";
        var baseUrl = _config["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var returnUrl = _config["VnPay:ReturnUrl"] ?? "http://localhost:5000/api/thanh-toan/vnpay-return";

        var orderId = $"{deposit.Id}_{DateTime.Now.Ticks}";
        var amountInVnd = (long)(deposit.Amount * 100); // VNPay yêu cầu nhân 100

        var vnpay = new VnPayHelper();
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", tmnCode);
        vnpay.AddRequestData("vnp_Amount", amountInVnd.ToString());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(clientIp) || clientIp == "::1" ? "127.0.0.1" : clientIp);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", dto.OrderInfo ?? $"Thanh toan tien coc phong dat coc ID {deposit.Id}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", orderId);

        var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

        return new PaymentResponseDto
        {
            PaymentUrl = paymentUrl,
            OrderId = orderId,
            Amount = deposit.Amount
        };
    }

    public async Task<PaymentResultDto> ProcessPaymentReturnAsync(IQueryCollection query)
    {
        var vnpay = new VnPayHelper();
        foreach (var (key, value) in query)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value.ToString());
            }
        }

        var orderId = vnpay.GetResponseData("vnp_TxnRef");
        var transactionId = vnpay.GetResponseData("vnp_TransactionNo");
        var vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnpSecureHash = query["vnp_SecureHash"].ToString();
        var hashSecret = _config["VnPay:HashSecret"] ?? "RAOERGKBMVUXIAKSRBUEHNQAGWIDFUJK";

        var isValidSignature = vnpay.ValidateSignature(vnpSecureHash, hashSecret);
        if (!isValidSignature)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Chữ ký VNPay không hợp lệ.",
                OrderId = orderId,
                TransactionId = transactionId,
                ResponseCode = vnpResponseCode
            };
        }

        var parts = orderId.Split('_');
        var depositId = int.TryParse(parts[0], out var dId) ? dId : 0;
        var deposit = await _context.Deposits.FindAsync(depositId);

        if (deposit == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Không tìm thấy dữ liệu khoản đặt cọc tương ứng.",
                OrderId = orderId,
                TransactionId = transactionId,
                ResponseCode = vnpResponseCode
            };
        }

        if (vnpResponseCode == "00")
        {
            // Thanh toán thành công
            deposit.Status = 1;
            deposit.PaidAt = DateTime.Now;

            // Tìm hoặc tạo RentalContract tự động nếu chưa có
            var existingContract = await _context.RentalContracts
                .FirstOrDefaultAsync(c => c.RentalRequestId == deposit.RentalRequestId);

            if (existingContract == null)
            {
                var rentalReq = await _context.RentalRequests.FindAsync(deposit.RentalRequestId);
                if (rentalReq != null)
                {
                    var post = await _context.Posts.FindAsync(rentalReq.PostId);
                    var contract = new RentalContract
                    {
                        RentalRequestId = rentalReq.Id,
                        PostId = rentalReq.PostId,
                        TenantAccountId = rentalReq.TenantAccountId,
                        LandlordAccountId = rentalReq.LandlordAccountId,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(6),
                        MonthlyRent = post?.DisplayPrice ?? deposit.Amount,
                        Status = 1,
                        TenantConfirmed = true,
                        LandlordConfirmed = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.RentalContracts.Add(contract);
                }
            }
            else
            {
                existingContract.TenantConfirmed = true;
                existingContract.Status = 1;
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo real-time
            await _notificationService.CreateNotificationAsync(
                deposit.TenantAccountId,
                "Đặt cọc thành công",
                $"Bạn đã thanh toán thành công {deposit.Amount:N0} VNĐ tiền đặt cọc phòng.",
                1,
                "/tenant/rentals"
            );

            await _notificationService.CreateNotificationAsync(
                deposit.LandlordAccountId,
                "Nhận tiền đặt cọc phòng",
                $"Khách thuê đã thanh toán {deposit.Amount:N0} VNĐ tiền đặt cọc phòng qua VNPay.",
                1,
                "/landlord/contracts"
            );

            return new PaymentResultDto
            {
                Success = true,
                Message = "Giao dịch thanh toán đặt cọc thành công.",
                OrderId = orderId,
                TransactionId = transactionId,
                Amount = deposit.Amount,
                ResponseCode = vnpResponseCode,
                DepositId = deposit.Id
            };
        }
        else
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = $"Giao dịch không thành công hoặc đã bị hủy (Mã lỗi: {vnpResponseCode}).",
                OrderId = orderId,
                TransactionId = transactionId,
                Amount = deposit.Amount,
                ResponseCode = vnpResponseCode,
                DepositId = deposit.Id
            };
        }
    }
}
