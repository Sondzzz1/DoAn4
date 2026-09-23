using RoomRental.BackEnd.DTO.Report;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IReportService
{
    Task<ReportDto> CreateReportAsync(int reporterAccountId, CreateReportDto createDto);
    Task<List<ReportDto>> GetReportsAsync(int? status = null);
    Task<ReportDto> GetReportByIdAsync(int id);
    Task<ReportDto> UpdateReportStatusAsync(int resolverAccountId, int reportId, UpdateReportStatusDto updateDto);
}
