using Microsoft.EntityFrameworkCore;
using RoomRental.Application.DTOs.ViewingAppointment;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Entities;
using RoomRental.Domain.Enums;
using RoomRental.Infrastructure.Data;

namespace RoomRental.Application.Services;

public class ViewingAppointmentService : IViewingAppointmentService
{
    private readonly ApplicationDbContext _context;

    public ViewingAppointmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tenant đặt lịch xem phòng (Mục 7)
    /// </summary>
    public async Task<AppointmentDto> CreateAppointmentAsync(int tenantAccountId, CreateAppointmentDto createDto)
    {
        var tenant = await GetOrCreateTenantProfileAsync(tenantAccountId);

        var post = await _context.Posts
            .Include(p => p.Landlord)
            .FirstOrDefaultAsync(p => p.Id == createDto.PostId);

        if (post == null)
        {
            throw new Exception("Không tìm thấy tin đăng phòng trọ");
        }

        var scheduledAt = createDto.GetScheduledDateTime();

        if (scheduledAt <= DateTime.Now)
        {
            throw new Exception("Thời gian hẹn xem phòng phải ở tương lai");
        }

        var landlordId = createDto.LandlordId ?? post.LandlordId;

        var appointment = new ViewingAppointment
        {
            TenantId = tenant.Id,
            PostId = post.Id,
            LandlordId = landlordId,
            ScheduledAt = scheduledAt,
            Status = AppointmentStatus.Pending,
            TenantNote = createDto.GetNote(),
            CreatedAt = DateTime.Now
        };

        _context.ViewingAppointments.Add(appointment);
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(tenantAccountId, appointment.Id);
    }

    /// <summary>
    /// Tenant xem danh sách lịch hẹn của mình
    /// </summary>
    public async Task<List<AppointmentDto>> GetTenantAppointmentsAsync(int tenantAccountId)
    {
        var tenant = await GetOrCreateTenantProfileAsync(tenantAccountId);

        return await _context.ViewingAppointments
            .Include(a => a.Post)
                .ThenInclude(p => p.Room)
            .Include(a => a.Tenant)
                .ThenInclude(t => t.Account)
            .Include(a => a.Landlord)
                .ThenInclude(l => l.Account)
            .Where(a => a.TenantId == tenant.Id)
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    /// <summary>
    /// Tenant hủy lịch xem phòng (Mục 8)
    /// </summary>
    public async Task<AppointmentDto> CancelAppointmentAsync(int tenantAccountId, int appointmentId, string? reason)
    {
        var tenant = await GetOrCreateTenantProfileAsync(tenantAccountId);

        var appointment = await _context.ViewingAppointments
            .Include(a => a.Post)
                .ThenInclude(p => p.Room)
            .Include(a => a.Tenant)
                .ThenInclude(t => t.Account)
            .Include(a => a.Landlord)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
        {
            throw new Exception("Không tìm thấy lịch hẹn");
        }

        if (appointment.TenantId != tenant.Id)
        {
            throw new Exception("Bạn không có quyền hủy lịch hẹn này");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new Exception("Lịch hẹn này đã bị hủy trước đó");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            throw new Exception("Không thể hủy lịch hẹn đã hoàn thành");
        }

        if (appointment.ScheduledAt <= DateTime.Now)
        {
            throw new Exception("Chỉ được hủy khi lịch hẹn chưa diễn ra");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            appointment.TenantNote = string.IsNullOrWhiteSpace(appointment.TenantNote) 
                ? $"[Đã hủy]: {reason}" 
                : $"{appointment.TenantNote} | [Đã hủy]: {reason}";
        }
        appointment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(appointment);
    }

    /// <summary>
    /// Landlord xem danh sách lịch xem phòng (Mục 17)
    /// </summary>
    public async Task<List<AppointmentDto>> GetLandlordAppointmentsAsync(int landlordAccountId, AppointmentStatus? status = null)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var query = _context.ViewingAppointments
            .Include(a => a.Post)
                .ThenInclude(p => p.Room)
            .Include(a => a.Tenant)
                .ThenInclude(t => t.Account)
            .Include(a => a.Landlord)
                .ThenInclude(l => l.Account)
            .Where(a => a.LandlordId == landlord.Id);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    /// <summary>
    /// Landlord xác nhận lịch hẹn
    /// </summary>
    public async Task<AppointmentDto> ConfirmAppointmentAsync(int landlordAccountId, int appointmentId)
    {
        var appointment = await GetLandlordAppointmentEntityAsync(landlordAccountId, appointmentId);

        if (appointment.Status != AppointmentStatus.Pending)
        {
            throw new Exception("Chỉ có thể xác nhận các lịch hẹn đang ở trạng thái Chờ xác nhận (Pending)");
        }

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(appointment);
    }

    /// <summary>
    /// Landlord từ chối lịch hẹn
    /// </summary>
    public async Task<AppointmentDto> RejectAppointmentAsync(int landlordAccountId, int appointmentId, string? reason)
    {
        var appointment = await GetLandlordAppointmentEntityAsync(landlordAccountId, appointmentId);

        if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new Exception("Không thể từ chối lịch hẹn đã hoàn tất hoặc đã hủy");
        }

        appointment.Status = AppointmentStatus.Rejected;
        appointment.LandlordResponse = reason ?? "Chủ trọ bận không thể tiếp vào thời gian này.";
        appointment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(appointment);
    }

    /// <summary>
    /// Landlord xác nhận buổi xem phòng đã hoàn thành
    /// </summary>
    public async Task<AppointmentDto> CompleteAppointmentAsync(int landlordAccountId, int appointmentId)
    {
        var appointment = await GetLandlordAppointmentEntityAsync(landlordAccountId, appointmentId);

        appointment.Status = AppointmentStatus.Completed;
        appointment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(appointment);
    }

    /// <summary>
    /// Chi tiết 1 lịch hẹn
    /// </summary>
    public async Task<AppointmentDto> GetAppointmentByIdAsync(int accountId, int appointmentId)
    {
        var appointment = await _context.ViewingAppointments
            .Include(a => a.Post)
                .ThenInclude(p => p.Room)
            .Include(a => a.Tenant)
                .ThenInclude(t => t.Account)
            .Include(a => a.Landlord)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
        {
            throw new Exception("Không tìm thấy lịch hẹn");
        }

        // Kiểm tra quyền xem
        if (appointment.Tenant.AccountId != accountId && appointment.Landlord.AccountId != accountId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == accountId);
            if (user == null || user.RoleId != 0) // Không phải Admin
            {
                throw new Exception("Bạn không có quyền truy cập thông tin lịch hẹn này");
            }
        }

        return MapToDto(appointment);
    }

    private async Task<ViewingAppointment> GetLandlordAppointmentEntityAsync(int landlordAccountId, int appointmentId)
    {
        var landlord = await GetOrCreateLandlordProfileAsync(landlordAccountId);

        var appointment = await _context.ViewingAppointments
            .Include(a => a.Post)
                .ThenInclude(p => p.Room)
            .Include(a => a.Tenant)
                .ThenInclude(t => t.Account)
            .Include(a => a.Landlord)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
        {
            throw new Exception("Không tìm thấy lịch hẹn");
        }

        if (appointment.LandlordId != landlord.Id)
        {
            throw new Exception("Bạn không có quyền xử lý lịch hẹn này");
        }

        return appointment;
    }

    private static AppointmentDto MapToDto(ViewingAppointment a)
    {
        return new AppointmentDto
        {
            Id = a.Id,
            PostId = a.PostId,
            PostTitle = a.Post?.Title ?? string.Empty,
            PostAddress = a.Post?.Room?.Address ?? string.Empty,
            PostPrice = a.Post?.DisplayPrice ?? 0,
            RoomName = a.Post?.Room?.RoomName,
            TenantId = a.TenantId,
            TenantAccountId = a.Tenant?.AccountId ?? 0,
            TenantName = a.Tenant?.Account?.FullName ?? string.Empty,
            TenantPhone = a.Tenant?.Account?.Phone ?? string.Empty,
            TenantAvatar = a.Tenant?.Account?.AvatarUrl,
            LandlordId = a.LandlordId,
            LandlordAccountId = a.Landlord?.AccountId ?? 0,
            LandlordName = a.Landlord?.Account?.FullName ?? string.Empty,
            LandlordPhone = a.Landlord?.Account?.Phone ?? string.Empty,
            ScheduledAt = a.ScheduledAt,
            Status = a.Status,
            TenantNote = a.TenantNote,
            LandlordResponse = a.LandlordResponse,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    private async Task<TenantProfile> GetOrCreateTenantProfileAsync(int accountId)
    {
        var user = await _context.Users
            .Include(u => u.TenantProfile)
            .FirstOrDefaultAsync(u => u.Id == accountId);

        if (user == null)
        {
            throw new Exception("Người dùng không tồn tại");
        }

        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa");
        }

        if (user.TenantProfile != null)
        {
            return user.TenantProfile;
        }

        var profile = new TenantProfile { AccountId = user.Id };
        _context.TenantProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile;
    }

    private async Task<LandlordProfile> GetOrCreateLandlordProfileAsync(int accountId)
    {
        var user = await _context.Users
            .Include(u => u.LandlordProfile)
            .FirstOrDefaultAsync(u => u.Id == accountId);

        if (user == null)
        {
            throw new Exception("Người dùng không tồn tại");
        }

        if (user.RoleId != 2 && user.RoleId != 0)
        {
            throw new Exception("Chỉ chủ trọ mới có quyền thực hiện");
        }

        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa");
        }

        if (user.LandlordProfile != null)
        {
            return user.LandlordProfile;
        }

        var profile = new LandlordProfile { AccountId = user.Id };
        _context.LandlordProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile;
    }
}
