using RoomRental.Application.DTOs.Report;

namespace RoomRental.Application.Interfaces;

public interface IReportService
{
    Task<ReportDto> CreateReportAsync(int reporterAccountId, CreateReportDto createDto);
    Task<List<ReportDto>> GetReportsAsync(int? status = null);
    Task<ReportDto> GetReportByIdAsync(int id);
    Task<ReportDto> UpdateReportStatusAsync(int resolverAccountId, int reportId, UpdateReportStatusDto updateDto);
}
