using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.DTO.Blog;
using RoomRental.BackEnd.DTO.ViewingAppointment;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models.Enums;
using RoomRental.BackEnd.Models;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoomRental.BackEnd.Controllers;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Notification;

namespace RoomRental.BackEnd.Tests;

[Collection("SqlServerWorkflow")]
public class AuditRegressionTests
{
    [Fact]
    public async Task Issue13_reservation_cancellation_notifies_appointment_tenant()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Available;
        db.RentalRequests.Single().Status = RentalRequestStatus.Cancelled;
        db.RentalRequests.Add(new RentalRequest { Id = 2, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1 });
        db.ViewingAppointments.Add(new ViewingAppointment { Id = 1, PostId = 1, TenantId = 10, LandlordId = 1,
            ScheduledAt = DateTime.Now.AddDays(1), Status = AppointmentStatus.Pending });
        await db.SaveChangesAsync();
        var notifications = new TestNotifications();
        await new RentalRequestBLL(db, notifications, NullLogger<RentalRequestBLL>.Instance)
            .CapNhatTrangThaiAsync(1, 2, RentalRequestStatus.Approved, soTienDatCoc: 1000000, hanThanhToanCoc: DateTime.Now.AddDays(1));
        Assert.Equal(AppointmentStatus.Cancelled, db.ViewingAppointments.Single().Status);
        Assert.Equal(new[] { 10, 10 }, notifications.Recipients);
    }

    [Theory]
    [InlineData(RentalContractStatus.Expired)]
    [InlineData(RentalContractStatus.Terminated)]
    public async Task Issue03_final_bill_requires_actual_period_owner_unique_and_preserves_paid_history(int status)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        var contract = new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 10,
            LandlordAccountId = 1, Status = status, StartDate = new DateTime(2026, 9, 15),
            EndDate = status == RentalContractStatus.Expired ? new DateTime(2026, 10, 31) : new DateTime(2026, 12, 31),
            UpdatedAt = new DateTime(2026, 10, 31, 16, 59, 59, DateTimeKind.Utc), MonthlyRent = 2400000 };
        db.RentalContracts.Add(contract);
        await db.SaveChangesAsync();
        var service = new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance);
        var dto = new TaoHoaDonDto { HopDongId = 1, Thang = 10, Nam = 2026, SoDienMoi = 10, SoNuocMoi = 1 };
        Assert.Equal(403, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(10, dto))).StatusCode);
        var bill = await service.TaoAsync(1, dto);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, dto))).StatusCode);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, new TaoHoaDonDto { HopDongId = 1, Thang = 11, Nam = 2026 }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, new TaoHoaDonDto { HopDongId = 1, Thang = 8, Nam = 2026 }));
        var endedAt = contract.UpdatedAt;
        await service.ThanhToanAsync(1, bill.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.XoaAsync(1, bill.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CapNhatAsync(1, bill.Id, new CapNhatHoaDonDto()));
        Assert.Equal(endedAt, contract.UpdatedAt);
    }

    [Fact]
    public async Task Issue03_termination_before_start_or_missing_timestamp_cannot_bill()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        var contract = new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 10,
            LandlordAccountId = 1, Status = RentalContractStatus.Terminated,
            StartDate = DateTime.Today.AddMonths(1), EndDate = DateTime.Today.AddMonths(3) };
        db.RentalContracts.Add(contract); await db.SaveChangesAsync();
        var service = new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance);
        var dto = new TaoHoaDonDto { HopDongId = 1, Thang = contract.StartDate.Month, Nam = contract.StartDate.Year };
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, dto));
        contract.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(1, dto));
        Assert.Empty(db.MonthlyBills);
    }

    [Theory]
    [InlineData("2026-10-30T17:00:00Z", RentalContractStatus.Active)]
    [InlineData("2026-10-31T16:59:59Z", RentalContractStatus.Active)]
    [InlineData("2026-10-31T17:00:00Z", RentalContractStatus.Expired)]
    [InlineData("2026-10-31T17:00:01Z", RentalContractStatus.Expired)]
    public async Task Issue11_end_date_inclusive_vietnam_midnight(string utcNow, int expected)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.RentalRequests.Single().Status = RentalRequestStatus.ConvertedToContract;
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Rented;
        db.RentalContracts.Add(new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1,
            TenantAccountId = 10, LandlordAccountId = 1, Status = RentalContractStatus.Active,
            StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 31) });
        await db.SaveChangesAsync();
        var service = new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance, new FixedClock(DateTimeOffset.Parse(utcNow)));
        await service.ReconcileContractLifecycleAsync(72);
        Assert.Equal(expected, db.RentalContracts.Single().Status);
        Assert.Equal(expected == RentalContractStatus.Active ? RoomStatus.Rented : RoomStatus.Available, db.Rooms.Single(r => r.Id == 1).Status);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Issue09_replacement_post_keeps_room_reviews_without_other_rooms()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Posts.Single(p => p.Id == 1).Status = PostStatus.Hidden;
        db.Posts.Add(new Post { Id = 4, RoomId = 1, LandlordId = 1, Title = "Replacement", Content = "QA", Status = PostStatus.Approved });
        db.RoomReviews.AddRange(new RoomReview { Id = 1, PostId = 1, ContractId = 1, TenantAccountId = 10, Rating = 5 },
            new RoomReview { Id = 2, PostId = 2, ContractId = 2, TenantAccountId = 10, Rating = 1 });
        await db.SaveChangesAsync();
        var reviews = await new RoomReviewBLL(db).LayTheoBaiDangAsync(4);
        Assert.Equal(1, Assert.Single(reviews).Id);
        Assert.Equal(2, db.RoomReviews.Count());
    }

    [Fact]
    public async Task Issue13_notifications_follow_successful_transitions_and_failure_does_not_undo_save()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = RoomStatus.Available;
        await db.SaveChangesAsync();
        var notifications = new TestNotifications();
        var service = new ViewingAppointmentBLL(db, notifications);
        async Task<int> Create() => (await service.CreateAppointmentAsync(10, new CreateAppointmentDto { PostId = 1, ScheduledAt = DateTime.Now.AddDays(1) })).Id;
        var id = await Create();
        Assert.Equal(1, notifications.Recipients.Last());
        await service.ConfirmAppointmentAsync(1, id);
        Assert.Equal(10, notifications.Recipients.Last());
        var count = notifications.Recipients.Count;
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConfirmAppointmentAsync(1, id));
        Assert.Equal(count, notifications.Recipients.Count);
        await service.CancelAppointmentAsync(10, id, "QA");
        Assert.Equal(1, notifications.Recipients.Last());
        id = await Create();
        await service.RejectAppointmentAsync(1, id, "QA");
        Assert.Equal(10, notifications.Recipients.Last());
        id = await Create();
        await service.ConfirmAppointmentAsync(1, id);
        db.ViewingAppointments.Single(a => a.Id == id).ScheduledAt = DateTime.Now.AddMinutes(-1);
        await db.SaveChangesAsync();
        await service.CompleteAppointmentAsync(1, id);
        Assert.Equal(10, notifications.Recipients.Last());
        notifications.Fail = true;
        id = await Create();
        Assert.Equal(AppointmentStatus.Pending, db.ViewingAppointments.Single(a => a.Id == id).Status);
    }

    private sealed class TestNotifications : INotificationService
    {
        public List<int> Recipients { get; } = new();
        public bool Fail { get; set; }
        public Task<NotificationDto> CreateNotificationAsync(int userId, string title, string content, int? type = 0, string? link = null)
        {
            if (Fail) throw new IOException("QA notification unavailable");
            Recipients.Add(userId);
            return Task.FromResult(new NotificationDto { AccountId = userId, Title = title });
        }
        public Task<List<NotificationDto>> GetMyNotificationsAsync(int userId) => throw new NotSupportedException();
        public Task<bool> MarkAsReadAsync(int userId, int notificationId) => throw new NotSupportedException();
        public Task<bool> MarkAllAsReadAsync(int userId) => throw new NotSupportedException();
        public Task<int> GetUnreadCountAsync(int userId) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("2026-10-31", null)]
    [InlineData(null, "09:00")]
    [InlineData("2026-02-31", "09:00")]
    [InlineData("2026-10-31", "25:00")]
    public void Issue10_missing_or_invalid_schedule_is_rejected(string? date, string? time)
    {
        Assert.Equal(400, Assert.Throws<BusinessRuleException>(() => new CreateAppointmentDto
            { NgayXem = date, GioXem = time }.GetScheduledDateTime()).StatusCode);
    }

    [Fact]
    public void Issue10_explicit_schedule_and_date_time_aliases_work()
    {
        var expected = new DateTime(2026, 10, 31, 9, 30, 0);
        Assert.Equal(expected, new CreateAppointmentDto { ScheduledAt = expected }.GetScheduledDateTime());
        Assert.Equal(expected, new CreateAppointmentDto { NgayXem = "2026-10-31", GioXem = "09:30" }.GetScheduledDateTime());
    }

    [Fact]
    public void Issue08_unexpected_errors_are_sanitized_and_log_inner_exception()
    {
        var logger = new CaptureLogger();
        using var services = new ServiceCollection().AddLogging(b => b.AddProvider(logger)).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var controller = new TestController { ControllerContext = new ControllerContext { HttpContext = http } };
        var ex = new DbUpdateException("private SQL path", new Exception("private database details"));
        var result = Assert.IsType<ObjectResult>(controller.BusinessError<object>(ex));
        Assert.Equal(500, result.StatusCode);
        Assert.Equal(BusinessErrorHandling.UnexpectedMessage, Assert.IsType<ApiResponse<object>>(result.Value).Message);
        Assert.Contains(ex, logger.Exceptions);
        var context = new ExceptionContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>()) { Exception = ex };
        new BusinessExceptionFilter().OnException(context);
        Assert.True(context.ExceptionHandled);
        Assert.Equal(500, Assert.IsType<ObjectResult>(context.Result).StatusCode);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    public void Issue08_business_status_is_preserved(int status)
    {
        var controller = new TestController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var result = Assert.IsType<ObjectResult>(controller.BusinessError<object>(new BusinessRuleException("validation", status)));
        Assert.Equal(status, result.StatusCode);
    }

    private sealed class TestController : ControllerBase { }
    private sealed class CaptureLogger : ILogger, ILoggerProvider
    {
        public List<Exception?> Exceptions { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Exceptions.Add(exception);
        public void Dispose() { }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task Issue07_comments_require_published_blog(int status, bool accepted)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.BlogPosts.Add(new BlogPost { Id = 1, Title = "QA", Slug = "qa", Content = "QA", AuthorAccountId = 1, Status = status });
        await db.SaveChangesAsync();
        var service = new BlogBLL(db);
        var dto = new CreateBlogCommentDto { Content = "QA comment" };
        if (accepted) Assert.Equal(1, (await service.AddCommentAsync(1, 1, dto)).BlogPostId);
        else
        {
            Assert.Equal(404, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.AddCommentAsync(1, 1, dto))).StatusCode);
            Assert.Empty(db.BlogComments);
        }
    }

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public async Task Issue06_review_length_boundary(int length, bool accepted)
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.RentalContracts.Add(new RentalContract { Id = 1, RentalRequestId = 1, PostId = 1,
            TenantAccountId = 10, LandlordAccountId = 1, Status = RentalContractStatus.Active });
        await db.SaveChangesAsync();
        var service = new RoomReviewBLL(db);
        var dto = new TaoDanhGiaDto { HopDongId = 1, SoSao = 5, NhanXet = new string('x', length) };
        if (accepted) Assert.Equal(length, (await service.TaoAsync(10, dto)).NhanXet!.Length);
        else
        {
            Assert.Equal(400, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.TaoAsync(10, dto))).StatusCode);
            Assert.Empty(db.RoomReviews);
        }
    }

    [Fact]
    public async Task Issue04_pdf_renders_with_bundled_font()
    {
        await using var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.RentalContracts.Add(new RentalContract
        {
            Id = 1, RentalRequestId = 1, PostId = 1, TenantAccountId = 10, LandlordAccountId = 1,
            StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 31),
            MonthlyRent = 2400000, DepositAmount = 1000000, ElectricityPrice = 3500, WaterPrice = 20000
        });
        await db.SaveChangesAsync();
        var room = await db.Rooms.FindAsync(1);
        room!.ElectricityPrice = 9876;
        room.WaterPrice = 54321;
        room.ServiceFee = 99999;
        await db.SaveChangesAsync();
        var bytes = await new ExportBLL(db).XuatHopDongPdfAsync(1, 1);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 8));
        Assert.True(bytes.Length > 1000);
        var evidence = Environment.GetEnvironmentVariable("ROOM_RENTAL_PDF_EVIDENCE");
        if (!string.IsNullOrWhiteSpace(evidence)) await File.WriteAllBytesAsync(evidence, bytes);
    }

    [Theory]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//example.com/a.png")]
    [InlineData("/uploads/../secret.png")]
    [InlineData("/uploads/%2e%2e/secret.png")]
    [InlineData("blob:invalid")]
    public async Task Issue02_create_and_update_reject_invalid_image_without_saving(string url)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        var service = new RoomBLL(db);
        var count = db.Rooms.Count();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateRoomAsync(1,
            new CreateRoomDto { RoomName = "QA", CategoryId = 1, Price = 1000000, Area = 20, Address = "QA", ImageUrls = new() { url } }));
        Assert.Equal(count, db.Rooms.Count());
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateRoomAsync(1, 1,
            new UpdateRoomDto { ImageUrls = new() { url } }));
        Assert.DoesNotContain(db.PostImages, i => i.ImageUrl == url);
    }

    [Fact]
    public async Task Issue02_urls_are_trimmed_deduplicated_and_preserved()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        var room = await new RoomBLL(db).CreateRoomAsync(1, new CreateRoomDto
        {
            RoomName = "QA", CategoryId = 1, Price = 1000000, Area = 20, Address = "QA",
            ImageUrls = new() { " https://example.com/a.webp?q=AbC ", "https://example.com/a.webp?q=AbC", "http://example.com/b.jpg", "/uploads/c.png" }
        });
        Assert.Equal(new[] { "https://example.com/a.webp?q=AbC", "http://example.com/b.jpg", "/uploads/c.png" }, room.ImageUrls);
    }

    [SqlServerFact]
    public async Task Issue01_first_admin_seed_is_admin_without_profiles_and_is_idempotent()
    {
        var connection = Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL")!;
        Assert.StartsWith("RoomRental_Verification_", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAdminAsync(db);
        db.ChangeTracker.Clear();
        var admin = await db.Users.SingleAsync(u => u.Email == "admin@roomrental.com");
        Assert.Equal(0, admin.RoleId);
        Assert.False(await db.TenantProfiles.AnyAsync(p => p.AccountId == admin.Id));
        Assert.False(await db.LandlordProfiles.AnyAsync(p => p.AccountId == admin.Id));
        await DbInitializer.SeedAdminAsync(db);
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == admin.Email));
    }
}
