using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DTO.ViewingAppointment;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using RoomRental.BackEnd.DAL;

namespace RoomRental.BackEnd.BLL;

public class ViewingAppointmentBLL : IViewingAppointmentService
{
    private readonly ApplicationDbContext _context;

    public ViewingAppointmentBLL(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tenant đặt lịch xem phòng (Mục 7)
    /// </summary>
    public async Task<AppointmentDto> CreateAppointmentAsync(int tenantAccountId, CreateAppointmentDto createDto)
    {
        var tenant = await GetOrCreateTenantProfileAsync(tenantAccountId);

        var postId = createDto.GetPostId();
        var post = await _context.Posts
            .Include(p => p.Landlord)
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw BusinessRuleException.NotFound("Không tìm thấy tin đăng phòng trọ");
        }

        if (post.Status != PostStatus.Approved)
            throw BusinessRuleException.Conflict("Tin đăng chưa được duyệt hoặc không còn khả dụng.");

        if (post.Room.Status != RoomStatus.Available)
            throw BusinessRuleException.Conflict("Phòng hiện không còn trống để đặt lịch xem.");

        if (tenant.AccountId == post.Landlord.AccountId)
            throw BusinessRuleException.Forbidden("Chủ trọ không thể tự đặt lịch xem phòng của mình.");

        var scheduledAt = createDto.GetScheduledDateTime();

        if (scheduledAt <= DateTime.Now)
        {
            throw new BusinessRuleException("Thời gian hẹn xem phòng phải ở tương lai");
        }

        var requestedLandlordId = createDto.GetLandlordId();
        if (requestedLandlordId.HasValue && requestedLandlordId.Value != post.LandlordId)
            throw new BusinessRuleException("Chủ trọ không khớp với tin đăng.");

        var hasDuplicate = await _context.ViewingAppointments.AnyAsync(a =>
            a.TenantId == tenant.Id &&
            a.PostId == post.Id &&
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));
        if (hasDuplicate)
            throw BusinessRuleException.Conflict("Bạn đã có một lịch xem đang chờ xử lý cho phòng này.");

        var hasScheduleConflict = await _context.ViewingAppointments.AnyAsync(a =>
            a.ScheduledAt == scheduledAt &&
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
            (a.TenantId == tenant.Id || a.LandlordId == post.LandlordId));
        if (hasScheduleConflict)
            throw BusinessRuleException.Conflict("Khung giờ này đã có lịch xem phòng. Vui lòng chọn thời gian khác.");

        var appointment = new ViewingAppointment
        {
            TenantId = tenant.Id,
            PostId = post.Id,
            LandlordId = post.LandlordId,
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
            throw BusinessRuleException.NotFound("Không tìm thấy lịch hẹn");
        }

        if (appointment.TenantId != tenant.Id)
        {
            throw BusinessRuleException.Forbidden("Bạn không có quyền hủy lịch hẹn này");
        }

        if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Confirmed)
            throw BusinessRuleException.Conflict("Chỉ có thể hủy lịch hẹn đang chờ hoặc đã được xác nhận.");

        if (appointment.ScheduledAt <= DateTime.Now)
        {
            throw BusinessRuleException.Conflict("Chỉ được hủy khi lịch hẹn chưa diễn ra");
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
            throw BusinessRuleException.Conflict("Chỉ có thể xác nhận lịch hẹn đang chờ xử lý.");
        }

        if (appointment.Post.Status != PostStatus.Approved || appointment.Post.Room.Status != RoomStatus.Available ||
            appointment.ScheduledAt <= DateTime.Now)
            throw BusinessRuleException.Conflict("Tin phải được duyệt, phòng còn trống và lịch hẹn chưa diễn ra.");
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

        if (appointment.Status != AppointmentStatus.Pending)
            throw BusinessRuleException.Conflict("Chỉ có thể từ chối lịch hẹn đang chờ xử lý.");

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

        if (appointment.Status != AppointmentStatus.Confirmed)
            throw BusinessRuleException.Conflict("Chỉ lịch hẹn đã được xác nhận mới có thể hoàn thành.");

        if (appointment.ScheduledAt > DateTime.Now)
            throw BusinessRuleException.Conflict("Chưa đến thời gian xem phòng, không thể hoàn thành lịch hẹn.");
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
            throw BusinessRuleException.NotFound("Không tìm thấy lịch hẹn");
        }

        // Kiểm tra quyền xem
        if (appointment.Tenant.AccountId != accountId && appointment.Landlord.AccountId != accountId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == accountId);
            if (user == null || user.RoleId != 0) // Không phải Admin
            {
                throw BusinessRuleException.Forbidden("Bạn không có quyền truy cập thông tin lịch hẹn này");
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
            throw BusinessRuleException.NotFound("Không tìm thấy lịch hẹn");
        }

        if (appointment.LandlordId != landlord.Id)
        {
            throw BusinessRuleException.Forbidden("Bạn không có quyền xử lý lịch hẹn này");
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
            PostPrice = a.Post?.Room?.Price ?? 0,
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
            throw BusinessRuleException.NotFound("Người dùng không tồn tại");
        }

        if (user.RoleId != 1)
            throw BusinessRuleException.Forbidden("Chỉ người thuê mới có thể đặt và quản lý lịch xem phòng.");

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

        if (user.RoleId != 2)
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
