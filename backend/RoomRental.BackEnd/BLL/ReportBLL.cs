using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.Report;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models.Enums;

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
        var postId = createDto.GetPostId();
        var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == reporterAccountId)
            ?? throw BusinessRuleException.NotFound("Người báo cáo không tồn tại.");
        if (!reporter.IsActive)
            throw BusinessRuleException.Forbidden("Tài khoản không thể gửi báo cáo.");
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
        {
            throw BusinessRuleException.NotFound("Tin đăng không tồn tại.");
        }

        var reason = createDto.GetReason().Trim();
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessRuleException("Lý do báo cáo là bắt buộc.");
        if (await _context.Reports.AnyAsync(r => r.ReporterAccountId == reporterAccountId &&
            r.PostId == postId && r.Status == ReportStatus.Pending))
            throw BusinessRuleException.Conflict("Bạn đã có báo cáo đang chờ xử lý cho tin đăng này.");

        var report = new Report
        {
            PostId = postId,
            ReporterAccountId = reporterAccountId,
            Reason = reason,
            Description = createDto.GetDescription(),
            Status = ReportStatus.Pending,
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
            throw BusinessRuleException.NotFound("Không tìm thấy báo cáo vi phạm.");
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
        var resolver = await _context.Users.FirstOrDefaultAsync(u => u.Id == resolverAccountId)
            ?? throw BusinessRuleException.NotFound("Người xử lý không tồn tại.");
        if (resolver.RoleId != 0)
            throw BusinessRuleException.Forbidden("Chỉ Admin mới được xử lý báo cáo.");

        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy báo cáo vi phạm.");
        }

        var nextStatus = updateDto.GetResolvedStatus();
        var valid = report.Status switch
        {
            ReportStatus.Pending => nextStatus is ReportStatus.Processing or ReportStatus.Resolved or ReportStatus.Rejected,
            ReportStatus.Processing => nextStatus is ReportStatus.Resolved or ReportStatus.Rejected,
            _ => false
        };
        if (!valid) throw BusinessRuleException.Conflict("Trạng thái xử lý báo cáo không hợp lệ.");

        report.Status = nextStatus;
        report.ResolvedAt = nextStatus is ReportStatus.Resolved or ReportStatus.Rejected ? DateTime.Now : null;
        report.ResolvedByAccountId = resolverAccountId;

        await _context.SaveChangesAsync();

        return await GetReportByIdAsync(reportId);
    }
}
