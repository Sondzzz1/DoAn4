using Microsoft.AspNetCore.Http;
using RoomRental.BackEnd.DTO.Payment;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentUrlAsync(int userId, CreatePaymentRequestDto dto, string clientIp);
    Task<PaymentResultDto> ProcessPaymentReturnAsync(IQueryCollection query);
}
