using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Report;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class ReportBLL : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportDto> CreateReportAsync(int reporterAccountId, CreateReportDto createDto)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == createDto.PostId);
        if (post == null)
        {
            throw new Exception("Tin đăng không tồn tại");
        }

        var report = new Report
        {
            PostId = createDto.PostId,
            ReporterAccountId = reporterAccountId,
            Reason = createDto.GetReason(),
            Description = createDto.GetDescription(),
            Status = 0, // Pending
            CreatedAt = DateTime.Now
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return await GetReportByIdAsync(report.Id);
    }

    public async Task<List<ReportDto>> GetReportsAsync(int? status = null)
    {
        var query = _context.Reports
            .Include(r => r.Post)
            .Include(r => r.Reporter)
            .Include(r => r.ResolvedBy)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportDto
            {
                Id = r.Id,
                PostId = r.PostId,
                PostTitle = r.Post.Title,
                ReporterAccountId = r.ReporterAccountId,
                ReporterName = r.Reporter.FullName,
                ReporterEmail = r.Reporter.Email,
                Reason = r.Reason,
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt,
                ResolvedByAccountId = r.ResolvedByAccountId,
                ResolvedByName = r.ResolvedBy != null ? r.ResolvedBy.FullName : null
            })
            .ToListAsync();
    }

    public async Task<ReportDto> GetReportByIdAsync(int id)
    {
        var r = await _context.Reports
            .Include(r => r.Post)
            .Include(r => r.Reporter)
            .Include(r => r.ResolvedBy)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null)
        {
            throw new Exception("Không tìm thấy báo cáo vi phạm");
        }

        return new ReportDto
        {
            Id = r.Id,
            PostId = r.PostId,
            PostTitle = r.Post.Title,
            ReporterAccountId = r.ReporterAccountId,
            ReporterName = r.Reporter.FullName,
            ReporterEmail = r.Reporter.Email,
            Reason = r.Reason,
            Description = r.Description,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt,
            ResolvedByAccountId = r.ResolvedByAccountId,
            ResolvedByName = r.ResolvedBy != null ? r.ResolvedBy.FullName : null
        };
    }

    public async Task<ReportDto> UpdateReportStatusAsync(int resolverAccountId, int reportId, UpdateReportStatusDto updateDto)
    {
        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null)
        {
            throw new Exception("Không tìm thấy báo cáo vi phạm");
        }

        report.Status = updateDto.GetResolvedStatus();
        report.ResolvedAt = DateTime.Now;
        report.ResolvedByAccountId = resolverAccountId;

        await _context.SaveChangesAsync();

        return await GetReportByIdAsync(reportId);
    }
}
