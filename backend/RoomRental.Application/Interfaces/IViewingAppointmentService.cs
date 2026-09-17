using RoomRental.Application.DTOs.ViewingAppointment;
using RoomRental.Domain.Enums;

namespace RoomRental.Application.Interfaces;

public interface IViewingAppointmentService
{
    // Tenant methods
    Task<AppointmentDto> CreateAppointmentAsync(int tenantAccountId, CreateAppointmentDto createDto);
    Task<List<AppointmentDto>> GetTenantAppointmentsAsync(int tenantAccountId);
    Task<AppointmentDto> CancelAppointmentAsync(int tenantAccountId, int appointmentId, string? reason);

    // Landlord methods
    Task<List<AppointmentDto>> GetLandlordAppointmentsAsync(int landlordAccountId, AppointmentStatus? status = null);
    Task<AppointmentDto> ConfirmAppointmentAsync(int landlordAccountId, int appointmentId);
    Task<AppointmentDto> RejectAppointmentAsync(int landlordAccountId, int appointmentId, string? reason);
    Task<AppointmentDto> CompleteAppointmentAsync(int landlordAccountId, int appointmentId);

    // Details
    Task<AppointmentDto> GetAppointmentByIdAsync(int accountId, int appointmentId);
}
