using System.Data;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Roommate;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

namespace RoomRental.BackEnd.BLL;

public class RoommateBLL : IRoommateService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<RoommateBLL> _logger;
    public RoommateBLL(ApplicationDbContext db, INotificationService notifications, ILogger<RoommateBLL> logger)
    { _db = db; _notifications = notifications; _logger = logger; }

    private IQueryable<RoommatePost> Posts() => _db.RoommatePosts.Include(p => p.Creator)
        .Include(p => p.Contract).ThenInclude(c => c.Post).ThenInclude(p => p!.Room).ThenInclude(r => r.Images)
        .Include(p => p.Contract).ThenInclude(c => c.Post).ThenInclude(p => p!.Room).ThenInclude(r => r.RoomAmenities).ThenInclude(a => a.Amenity);
    private async Task<RoommatePost> PostAsync(int id) => await Posts().FirstOrDefaultAsync(p => p.Id == id)
        ?? throw BusinessRuleException.NotFound("Không tìm thấy tin ở ghép.");
    private async Task<RentalContract> ContractEntityAsync(int id) => await _db.RentalContracts.Include(c => c.Post).ThenInclude(p => p!.Room)
        .FirstOrDefaultAsync(c => c.Id == id) ?? throw BusinessRuleException.NotFound("Không tìm thấy hợp đồng.");
    private async Task RoleAsync(int id, int role)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == id && u.RoleId == role && u.IsActive))
            throw BusinessRuleException.Forbidden("Tài khoản không có quyền thực hiện thao tác này.");
    }
    private static bool IsActive(RentalContract c)
    {
        var today = RentalCalendar.Today(TimeProvider.System);
        return c.Status == RentalContractStatus.Active && c.StartDate.Date <= today && c.EndDate.Date >= today && c.Post!.Room.Status == RoomStatus.Rented;
    }
    private static void EnsureActive(RentalContract c)
    { if (!IsActive(c)) throw BusinessRuleException.Conflict("Cần hợp đồng đang hiệu lực và phòng đang được thuê."); }
    private async Task<int> RemainingAsync(RoommatePost p) => Math.Max(0, p.RequestedSlots - await _db.RoommateRequests.CountAsync(r => r.RoommatePostId == p.Id && r.Status == RoommateRequestStatus.Approved));
    private async Task EnsureOpenAsync(RoommatePost p)
    {
        EnsureActive(p.Contract);
        if (p.Status != RoommatePostStatus.Open || await RemainingAsync(p) < 1 ||
            await ContractOccupancyPolicy.CountAsync(_db, p.Contract.Post!.RoomId) >= p.Contract.Post.Room.MaxOccupants)
            throw BusinessRuleException.Conflict("Tin không còn nhận yêu cầu hoặc phòng đã đủ người.");
    }
    private static void ValidatePost(CreateRoommatePostDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Title) || d.Title.Trim().Length > 200 || string.IsNullOrWhiteSpace(d.Description) || d.Description.Trim().Length > 4000)
            throw new BusinessRuleException("Tiêu đề cần 1–200 ký tự; mô tả cần 1–4000 ký tự.");
        if (d.RequestedSlots < 1 || d.EstimatedShare is < 0 or > 9999999999999999.99m || d.Requirements?.Length > 2000)
            throw new BusinessRuleException("Số người cần tìm, chi phí dự kiến hoặc yêu cầu không hợp lệ.");
    }
    private async Task SaveAsync()
    {
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
        { throw BusinessRuleException.Conflict("Dữ liệu ở ghép đã tồn tại hoặc vừa được xử lý. Vui lòng tải lại."); }
    }
    private async Task NotifyAsync(int id, string title, string content, bool landlord = false)
    {
        try { await _notifications.CreateNotificationAsync(id, title, content, 1, landlord ? "/landlord/roommates" : "/tenant/roommates"); }
        catch (Exception e) { _logger.LogWarning(e, "Không thể gửi thông báo ở ghép tới {AccountId}", id); }
    }

    public async Task<RoommateSearchResult> SearchAsync(string? keyword, int pageNumber, int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 1000000); pageSize = Math.Clamp(pageSize, 1, 50);
        var today = RentalCalendar.Today(TimeProvider.System);
        var q = Posts().AsNoTracking().Where(p => p.Status == RoommatePostStatus.Open &&
            p.Contract.Status == RentalContractStatus.Active && p.Contract.StartDate.Date <= today && p.Contract.EndDate.Date >= today &&
            p.Contract.Post!.Room.Status == RoomStatus.Rented &&
            _db.RentalContracts.Count(c => c.Post!.RoomId == p.Contract.Post.RoomId && c.Status == RentalContractStatus.Active) +
            _db.ContractOccupants.Count(o => o.Status == ContractOccupantStatus.Active && o.Contract.Status == RentalContractStatus.Active && o.Contract.Post!.RoomId == p.Contract.Post.RoomId) < p.Contract.Post.Room.MaxOccupants &&
            _db.RoommateRequests.Count(r => r.RoommatePostId == p.Id && r.Status == RoommateRequestStatus.Approved) < p.RequestedSlots);
        if (!string.IsNullOrWhiteSpace(keyword)) { keyword = keyword.Trim(); q = q.Where(p => p.Title.Contains(keyword) || p.Description.Contains(keyword) || p.Contract.Post!.Room.Address.Contains(keyword) || (p.Contract.Post.Room.Province ?? "").Contains(keyword)); }
        var result = new RoommateSearchResult { TotalCount = await q.CountAsync(), PageNumber = pageNumber, PageSize = pageSize };
        foreach (var p in await q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync())
            result.Items.Add(await MapAsync(p, false));
        return result;
    }
    public async Task<RoommatePostDto> PublicDetailAsync(int id)
    {
        var p = await PostAsync(id);
        await EnsureOpenAsync(p);
        return await MapAsync(p, false);
    }
    public async Task<List<RoommatePostDto>> MyPostsAsync(int accountId, bool landlord)
    {
        await RoleAsync(accountId, landlord ? 2 : 1);
        var result = new List<RoommatePostDto>();
        foreach (var p in await Posts().Where(p => landlord ? p.Contract.LandlordAccountId == accountId : p.CreatorTenantAccountId == accountId).OrderByDescending(p => p.CreatedAt).ToListAsync())
            result.Add(await MapAsync(p, true));
        return result;
    }
    public async Task<RoommatePostDto> CreateAsync(int accountId, CreateRoommatePostDto dto)
    {
        ValidatePost(dto); await RoleAsync(accountId, 1);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var c = await ContractEntityAsync(dto.ContractId);
        if (c.TenantAccountId != accountId) throw BusinessRuleException.Forbidden("Bạn không phải người thuê chính của hợp đồng.");
        EnsureActive(c);
        var count = await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, c.Post!.RoomId);
        if (dto.RequestedSlots > c.Post.Room.MaxOccupants - count) throw BusinessRuleException.Conflict("Số người cần tìm vượt số chỗ còn lại.");
        if (await _db.RoommatePosts.AnyAsync(p => p.ContractId == c.Id && (p.Status == RoommatePostStatus.PendingLandlordApproval || p.Status == RoommatePostStatus.Open)))
            throw BusinessRuleException.Conflict("Hợp đồng đã có tin đang mở hoặc chờ duyệt.");
        var post = new RoommatePost { ContractId = c.Id, CreatorTenantAccountId = accountId, Title = dto.Title.Trim(), Description = dto.Description.Trim(), RequestedSlots = dto.RequestedSlots, EstimatedShare = dto.EstimatedShare, Requirements = dto.Requirements?.Trim(), Status = RoommatePostStatus.PendingLandlordApproval };
        _db.RoommatePosts.Add(post); await SaveAsync(); await tx.CommitAsync();
        await NotifyAsync(c.LandlordAccountId, "Tin tìm người ở ghép chờ duyệt", post.Title, true);
        return await MapAsync(await PostAsync(post.Id), true);
    }
    public async Task<RoommatePostDto> UpdateAsync(int accountId, int id, CreateRoommatePostDto dto)
    {
        ValidatePost(dto); await RoleAsync(accountId, 1);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var p = await PostAsync(id);
        if (p.CreatorTenantAccountId != accountId) throw BusinessRuleException.Forbidden("Không có quyền sửa tin này.");
        EnsureActive(p.Contract);
        if (dto.ContractId != p.ContractId || p.Status is not (RoommatePostStatus.PendingLandlordApproval or RoommatePostStatus.Open or RoommatePostStatus.Rejected))
            throw BusinessRuleException.Conflict("Không thể đổi hợp đồng hoặc sửa tin đã kết thúc.");
        var approved = await _db.RoommateRequests.CountAsync(r => r.RoommatePostId == id && r.Status == RoommateRequestStatus.Approved);
        var count = await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, p.Contract.Post!.RoomId);
        if (dto.RequestedSlots <= approved || dto.RequestedSlots - approved > p.Contract.Post.Room.MaxOccupants - count)
            throw BusinessRuleException.Conflict("Số người cần tìm không phù hợp số chỗ còn lại.");
        if (await _db.RoommatePosts.AnyAsync(x => x.ContractId == p.ContractId && x.Id != id && (x.Status == RoommatePostStatus.Open || x.Status == RoommatePostStatus.PendingLandlordApproval)))
            throw BusinessRuleException.Conflict("Hợp đồng đã có tin khác đang hoạt động.");
        p.Title = dto.Title.Trim(); p.Description = dto.Description.Trim(); p.Requirements = dto.Requirements?.Trim();
        p.EstimatedShare = dto.EstimatedShare; p.RequestedSlots = dto.RequestedSlots;
        p.Status = RoommatePostStatus.PendingLandlordApproval; p.ApprovedAt = null; p.RejectionReason = null;
        await CancelPendingAsync(id); await SaveAsync(); await tx.CommitAsync();
        await NotifyAsync(p.Contract.LandlordAccountId, "Tin ở ghép cần duyệt lại", p.Title, true);
        return await MapAsync(p, true);
    }
    public async Task<RoommatePostDto> ModerateAsync(int accountId, int id, RoommateDecisionDto dto)
    {
        await RoleAsync(accountId, 2);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var p = await PostAsync(id);
        if (p.Contract.LandlordAccountId != accountId) throw BusinessRuleException.Forbidden("Tin không thuộc hợp đồng của bạn.");
        EnsureActive(p.Contract);
        if (p.Status != RoommatePostStatus.PendingLandlordApproval) throw BusinessRuleException.Conflict("Tin không đang chờ duyệt.");
        if (!dto.Approve && (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500)) throw new BusinessRuleException("Cần lý do từ chối không quá 500 ký tự.");
        var count = await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, p.Contract.Post!.RoomId);
        if (dto.Approve && await RemainingAsync(p) > p.Contract.Post.Room.MaxOccupants - count) throw BusinessRuleException.Conflict("Số chỗ đã thay đổi, cần người thuê cập nhật tin.");
        p.Status = dto.Approve ? RoommatePostStatus.Open : RoommatePostStatus.Rejected;
        p.ApprovedAt = dto.Approve ? DateTime.UtcNow : null; p.RejectionReason = dto.Approve ? null : dto.Reason!.Trim();
        await SaveAsync(); await tx.CommitAsync();
        await NotifyAsync(p.CreatorTenantAccountId, dto.Approve ? "Tin ở ghép đã được duyệt" : "Tin ở ghép bị từ chối", dto.Reason ?? p.Title);
        return await MapAsync(p, true);
    }
    public async Task<RoommatePostDto> CloseAsync(int accountId, int id)
    {
        await RoleAsync(accountId, 1);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var p = await PostAsync(id);
        if (p.CreatorTenantAccountId != accountId) throw BusinessRuleException.Forbidden("Không có quyền đóng tin.");
        if (p.Status is RoommatePostStatus.Closed or RoommatePostStatus.Expired) throw BusinessRuleException.Conflict("Tin đã kết thúc.");
        p.Status = RoommatePostStatus.Closed; p.ClosedAt = DateTime.UtcNow;
        await CancelPendingAsync(id); await SaveAsync(); await tx.CommitAsync(); return await MapAsync(p, true);
    }
    private async Task CancelPendingAsync(int postId)
    {
        foreach (var r in await _db.RoommateRequests.Where(r => r.RoommatePostId == postId && (r.Status == RoommateRequestStatus.Pending || r.Status == RoommateRequestStatus.AcceptedByPrimaryTenant)).ToListAsync())
            r.Status = RoommateRequestStatus.Cancelled;
    }
    public async Task<RoommateRequestDto> ApplyAsync(int accountId, int id, CreateRoommateRequestDto dto)
    {
        await RoleAsync(accountId, 1);
        if (dto.Message?.Length > 1000) throw new BusinessRuleException("Lời nhắn không quá 1000 ký tự.");
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var p = await PostAsync(id); await EnsureOpenAsync(p);
        if (p.CreatorTenantAccountId == accountId) throw BusinessRuleException.Forbidden("Không thể ứng tuyển tin của chính mình.");
        if (await _db.ContractOccupants.AnyAsync(o => o.ContractId == p.ContractId && o.TenantAccountId == accountId && o.Status == ContractOccupantStatus.Active))
            throw BusinessRuleException.Conflict("Bạn đã là người ở cùng của hợp đồng này.");
        if (await _db.RoommateRequests.AnyAsync(r => r.RoommatePostId == id && r.ApplicantTenantAccountId == accountId && (r.Status == RoommateRequestStatus.Pending || r.Status == RoommateRequestStatus.AcceptedByPrimaryTenant)))
            throw BusinessRuleException.Conflict("Bạn đã có yêu cầu đang chờ xử lý.");
        var request = new RoommateRequest { RoommatePostId = id, ApplicantTenantAccountId = accountId, Message = dto.Message?.Trim(), Status = RoommateRequestStatus.Pending };
        _db.RoommateRequests.Add(request); await SaveAsync(); await tx.CommitAsync();
        await NotifyAsync(p.CreatorTenantAccountId, "Có yêu cầu ở ghép mới", p.Title);
        return await RequestDtoAsync(request.Id);
    }
    public async Task<List<RoommateRequestDto>> RequestsAsync(int accountId, int postId)
    {
        var p = await PostAsync(postId);
        if (p.CreatorTenantAccountId != accountId && p.Contract.LandlordAccountId != accountId) throw BusinessRuleException.Forbidden("Không có quyền xem yêu cầu của tin này.");
        return await RequestDtos().Where(r => r.RoommatePostId == postId).OrderByDescending(r => r.CreatedAt).ToListAsync();
    }
    public async Task<List<RoommateRequestDto>> MyApplicationsAsync(int accountId)
    { await RoleAsync(accountId, 1); return await RequestDtos().Where(r => r.ApplicantTenantAccountId == accountId).OrderByDescending(r => r.CreatedAt).ToListAsync(); }
    public async Task<RoommateRequestDto> DecideAsync(int accountId, int requestId, bool landlord, RoommateDecisionDto dto)
    {
        await RoleAsync(accountId, landlord ? 2 : 1);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var r = await _db.RoommateRequests.FirstOrDefaultAsync(r => r.Id == requestId) ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu ở ghép.");
        var p = await PostAsync(r.RoommatePostId);
        if ((landlord ? p.Contract.LandlordAccountId : p.CreatorTenantAccountId) != accountId) throw BusinessRuleException.Forbidden("Không có quyền xử lý yêu cầu này.");
        if (r.Status != (landlord ? RoommateRequestStatus.AcceptedByPrimaryTenant : RoommateRequestStatus.Pending)) throw BusinessRuleException.Conflict("Yêu cầu không ở trạng thái phù hợp.");
        await EnsureOpenAsync(p);
        if (!dto.Approve) r.Status = RoommateRequestStatus.Rejected;
        else if (!landlord) r.Status = RoommateRequestStatus.AcceptedByPrimaryTenant;
        else
        {
            await RoleAsync(r.ApplicantTenantAccountId, 1);
            if (r.ApplicantTenantAccountId == p.CreatorTenantAccountId || await _db.ContractOccupants.AnyAsync(o => o.ContractId == p.ContractId && o.TenantAccountId == r.ApplicantTenantAccountId && o.Status == ContractOccupantStatus.Active))
                throw BusinessRuleException.Conflict("Người này đã cư trú trong phòng.");
            var count = await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, p.Contract.Post!.RoomId);
            if (count >= p.Contract.Post.Room.MaxOccupants) throw BusinessRuleException.Conflict("Phòng đã đủ người.");
            _db.ContractOccupants.Add(new ContractOccupant { ContractId = p.ContractId, TenantAccountId = r.ApplicantTenantAccountId });
            r.Status = RoommateRequestStatus.Approved;
            await SaveAsync();
            count = await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, p.Contract.Post.RoomId);
            if (count >= p.Contract.Post.Room.MaxOccupants || await RemainingAsync(p) == 0)
            {
                p.Status = RoommatePostStatus.Full; p.ClosedAt = DateTime.UtcNow;
                await CancelPendingAsync(p.Id);
            }
        }
        await SaveAsync(); await tx.CommitAsync();
        var message = dto.Approve ? landlord ? "Chủ trọ đã xác nhận bạn vào ở ghép." : "Người thuê chính đã chấp nhận; đang chờ chủ trọ xác nhận." : "Yêu cầu ở ghép đã bị từ chối.";
        await NotifyAsync(r.ApplicantTenantAccountId, "Cập nhật yêu cầu ở ghép", message);
        if (landlord) await NotifyAsync(p.CreatorTenantAccountId, "Chủ trọ đã xử lý yêu cầu ở ghép", message);
        else if (dto.Approve) await NotifyAsync(p.Contract.LandlordAccountId, "Yêu cầu ở ghép cần xác nhận cuối", p.Title, true);
        return await RequestDtoAsync(requestId);
    }
    public async Task<RoommateRequestDto> CancelAsync(int accountId, int requestId)
    {
        await RoleAsync(accountId, 1);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var r = await _db.RoommateRequests.FirstOrDefaultAsync(r => r.Id == requestId) ?? throw BusinessRuleException.NotFound("Không tìm thấy yêu cầu.");
        if (r.ApplicantTenantAccountId != accountId) throw BusinessRuleException.Forbidden("Không có quyền hủy yêu cầu.");
        if (r.Status is not (RoommateRequestStatus.Pending or RoommateRequestStatus.AcceptedByPrimaryTenant)) throw BusinessRuleException.Conflict("Không thể hủy yêu cầu đã kết thúc.");
        r.Status = RoommateRequestStatus.Cancelled; await SaveAsync(); await tx.CommitAsync(); return await RequestDtoAsync(requestId);
    }
    public async Task<List<RoommateContractDto>> MyContractsAsync(int accountId)
    {
        await RoleAsync(accountId, 1);
        var ids = await _db.RentalContracts.Where(c => c.TenantAccountId == accountId).OrderByDescending(c => c.CreatedAt).Select(c => c.Id).ToListAsync();
        var result = new List<RoommateContractDto>(); foreach (var id in ids) result.Add(await ContractAsync(accountId, id)); return result;
    }
    public async Task<RoommateContractDto> ContractAsync(int accountId, int contractId)
    {
        var c = await ContractEntityAsync(contractId);
        if (c.TenantAccountId != accountId && c.LandlordAccountId != accountId) throw BusinessRuleException.Forbidden("Không có quyền xem người cư trú của hợp đồng.");
        var count = await ContractOccupancyPolicy.CountAsync(_db, c.Post!.RoomId);
        return new RoommateContractDto { ContractId = c.Id, RoomName = c.Post.Room.RoomName, ContractStatus = c.Status, MaxOccupants = c.Post.Room.MaxOccupants, CurrentOccupants = count, AvailableSlots = Math.Max(0, c.Post.Room.MaxOccupants - count),
            CanCreatePost = IsActive(c) && count < c.Post.Room.MaxOccupants && !await _db.RoommatePosts.AnyAsync(p => p.ContractId == c.Id && (p.Status == RoommatePostStatus.Open || p.Status == RoommatePostStatus.PendingLandlordApproval)),
            Occupants = await _db.ContractOccupants.Where(o => o.ContractId == c.Id).OrderByDescending(o => o.JoinedAt).Select(o => new ContractOccupantDto { Id = o.Id, TenantAccountId = o.TenantAccountId, TenantName = o.Tenant.FullName, Status = o.Status, JoinedAt = o.JoinedAt, LeftAt = o.LeftAt }).ToListAsync() };
    }
    public async Task<RoommateContractDto> LeaveAsync(int accountId, int occupantId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); await WorkflowLock.AcquireAsync(_db);
        var o = await _db.ContractOccupants.FirstOrDefaultAsync(x => x.Id == occupantId) ?? throw BusinessRuleException.NotFound("Không tìm thấy người ở cùng.");
        var c = await ContractEntityAsync(o.ContractId);
        if (accountId != c.TenantAccountId && accountId != c.LandlordAccountId) throw BusinessRuleException.Forbidden("Chỉ người thuê chính hoặc chủ trọ được xác nhận rời phòng.");
        EnsureActive(c);
        if (o.Status != ContractOccupantStatus.Active) throw BusinessRuleException.Conflict("Người ở cùng đã rời phòng.");
        o.Status = ContractOccupantStatus.Left; o.LeftAt = DateTime.UtcNow;
        await SaveAsync(); await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, c.Post!.RoomId); await SaveAsync(); await tx.CommitAsync();
        await NotifyAsync(o.TenantAccountId, "Đã ghi nhận rời phòng", c.Post.Room.RoomName);
        return await ContractAsync(accountId, c.Id);
    }
    public async Task ReconcileLifecycleAsync(CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken); await WorkflowLock.AcquireAsync(_db, cancellationToken);
        var ended = await _db.RentalContracts.Where(c => c.Status == RentalContractStatus.Expired || c.Status == RentalContractStatus.Terminated || c.Status == RentalContractStatus.Cancelled).Select(c => new { c.Id, c.Status }).ToListAsync(cancellationToken);
        var departed = new List<int>();
        foreach (var c in ended)
        {
            departed.AddRange(await _db.ContractOccupants.Where(o => o.ContractId == c.Id && o.Status == ContractOccupantStatus.Active).Select(o => o.TenantAccountId).ToListAsync(cancellationToken));
            await ContractOccupancyPolicy.EndAsync(_db, c.Id, c.Status == RentalContractStatus.Expired);
        }
        await SaveAsync();
        foreach (var id in await _db.Rooms.Select(r => r.Id).ToListAsync(cancellationToken)) await ContractOccupancyPolicy.RecalculateCurrentOccupantsAsync(_db, id);
        await SaveAsync(); await tx.CommitAsync(cancellationToken);
        foreach (var id in departed.Distinct()) await NotifyAsync(id, "Hợp đồng phòng ở ghép đã kết thúc", "Vui lòng liên hệ người thuê chính hoặc chủ trọ.");
    }
    private IQueryable<RoommateRequestDto> RequestDtos() => _db.RoommateRequests.Select(r => new RoommateRequestDto { Id = r.Id, RoommatePostId = r.RoommatePostId, ApplicantTenantAccountId = r.ApplicantTenantAccountId, ApplicantName = r.Applicant.FullName, ApplicantAvatar = r.Applicant.AvatarUrl, Message = r.Message, Status = r.Status, PostTitle = r.Post.Title, CreatorTenantAccountId = r.Post.CreatorTenantAccountId, CreatorName = r.Post.Creator.FullName, CreatedAt = r.CreatedAt });
    private Task<RoommateRequestDto> RequestDtoAsync(int id) => RequestDtos().FirstAsync(r => r.Id == id);
    private async Task<RoommatePostDto> MapAsync(RoommatePost p, bool owner)
    {
        var room = p.Contract.Post!.Room;
        var count = await ContractOccupancyPolicy.CountAsync(_db, room.Id);
        return new RoommatePostDto { Id = p.Id, ContractId = owner ? p.ContractId : null, Title = p.Title, Description = p.Description, Requirements = p.Requirements, RejectionReason = owner ? p.RejectionReason : null, EstimatedShare = p.EstimatedShare, RequestedSlots = p.RequestedSlots, RemainingRequestedSlots = await RemainingAsync(p), Status = p.Status, CreatorName = p.Creator.FullName, CreatorAvatar = p.Creator.AvatarUrl, RoomName = room.RoomName, Address = room.Address, Province = room.Province ?? "", District = room.District ?? "", Area = room.Area, MaxOccupants = room.MaxOccupants, CurrentOccupants = count, AvailableSlots = Math.Max(0, room.MaxOccupants - count), ImageUrls = room.Images.OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(), Amenities = room.RoomAmenities.Select(a => a.Amenity.Name).ToList(), CreatedAt = p.CreatedAt };
    }
}
