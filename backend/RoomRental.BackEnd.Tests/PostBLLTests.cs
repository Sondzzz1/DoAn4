using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.DTO.ViewingAppointment;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public sealed class PostBLLTests
{
    [Fact]
    public async Task SearchPostsAsync_returns_approved_posts_including_reserved_rooms()
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var service = new PostBLL(context);

        var posts = await service.SearchPostsAsync(new PostQueryParameters());

        Assert.Equal(2, posts.Count);
        Assert.All(posts, post => Assert.Equal(PostStatus.Approved, post.Status));
        Assert.Contains(posts, post => post.Id == 1 && post.RoomStatus == RoomStatus.Available);
        Assert.Contains(posts, post => post.Id == 2 && post.RoomStatus == RoomStatus.Reserved);
    }

    [Fact]
    public async Task GetPublicPostByIdAsync_throws_not_found_for_non_public_post()
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var service = new PostBLL(context);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPublicPostByIdAsync(3));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public void CalculateDistance_returns_zero_for_the_same_coordinates()
    {
        var distance = PostBLL.CalculateDistance(21.0285, 105.8542, 21.0285, 105.8542);

        Assert.Equal(0, distance, precision: 8);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task Updating_address_discards_stale_coordinates_but_keeps_explicit_new_pin(bool throughPost, bool withPin)
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var room = await context.Rooms.SingleAsync(r => r.Id == 1);
        room.Latitude = 21;
        room.Longitude = 105;
        await context.SaveChangesAsync();
        if (throughPost)
            await new PostBLL(context).UpdatePostAsync(1, 1, new UpdatePostDto { Address = "New address", Latitude = withPin ? 22 : null, Longitude = withPin ? 106 : null });
        else
            await new RoomBLL(context).UpdateRoomAsync(1, 1, new UpdateRoomDto { Address = "New address", Latitude = withPin ? 22 : null, Longitude = withPin ? 106 : null });
        Assert.Equal(withPin ? 22m : (decimal?)null, room.Latitude);
        Assert.Equal(withPin ? 106m : (decimal?)null, room.Longitude);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Updating_non_location_fields_preserves_saved_pin(bool throughPost)
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var room = await context.Rooms.SingleAsync(r => r.Id == 1);
        room.Latitude = 21;
        room.Longitude = 105;
        await context.SaveChangesAsync();
        if (throughPost) await new PostBLL(context).UpdatePostAsync(1, 1, new UpdatePostDto { Title = "New title" });
        else await new RoomBLL(context).UpdateRoomAsync(1, 1, new UpdateRoomDto { RoomName = "New name" });
        Assert.Equal(21m, room.Latitude);
        Assert.Equal(105m, room.Longitude);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Explicit_empty_optional_address_parts_clear_old_values(bool throughPost)
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var room = await context.Rooms.SingleAsync(r => r.Id == 1);
        room.Ward = "Old ward";
        room.District = "Old district";
        await context.SaveChangesAsync();
        if (throughPost) await new PostBLL(context).UpdatePostAsync(1, 1, new UpdatePostDto { Ward = "", District = "" });
        else await new RoomBLL(context).UpdateRoomAsync(1, 1, new UpdateRoomDto { Phuong = "", Quan = "" });
        Assert.Equal("", room.Ward);
        Assert.Equal("", room.District);
    }

    public static IEnumerable<object[]> VisibilityCases =>
        from postStatus in Enum.GetValues<PostStatus>()
        from roomStatus in Enum.GetValues<RoomStatus>()
        select new object[] { postStatus, roomStatus };

    [Theory]
    [MemberData(nameof(VisibilityCases))]
    public async Task Public_list_and_detail_have_consistent_visibility(PostStatus postStatus, RoomStatus roomStatus)
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var post = await context.Posts.Include(p => p.Room).SingleAsync(p => p.Id == 1);
        post.Status = postStatus;
        post.Room.Status = roomStatus;
        await context.SaveChangesAsync();
        var service = new PostBLL(context);
        var visible = postStatus == PostStatus.Approved && roomStatus != RoomStatus.TemporarilyUnavailable;
        var posts = await service.SearchPostsAsync(new PostQueryParameters());
        Assert.Equal(visible, posts.Any(p => p.Id == 1));
        if (visible)
        {
            Assert.Equal(roomStatus, (await service.GetPublicPostByIdAsync(1)).RoomStatus);
            Assert.Equal(roomStatus, posts.Single(p => p.Id == 1).RoomStatus);
        }
        else
        {
            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPublicPostByIdAsync(1));
            Assert.Equal(404, error.StatusCode);
            Assert.Equal(0, post.ViewCount);
        }
    }

    [Theory]
    [InlineData(RoomStatus.Rented)]
    [InlineData(RoomStatus.Reserved)]
    public async Task Visible_unavailable_rooms_still_reject_rental_requests_and_viewings(RoomStatus status)
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var room = await context.Rooms.SingleAsync(r => r.Id == 1);
        room.Status = status;
        context.Users.Add(new User { Id = 10, UserName = "tenant", PasswordHash = "test", FullName = "Tenant", Email = "tenant@example.test", RoleId = 1 });
        await context.SaveChangesAsync();
        // Rejection must happen before notification dispatch or persistence.
        var rentals = new RentalRequestBLL(context, null!, NullLogger<RentalRequestBLL>.Instance);
        var rentalError = await Assert.ThrowsAsync<BusinessRuleException>(() => rentals.TaoAsync(10, new TaoYeuCauThueDto { BaiDangId = 1 }));
        var appointmentError = await Assert.ThrowsAsync<BusinessRuleException>(() => new ViewingAppointmentBLL(context).CreateAppointmentAsync(10,
            new CreateAppointmentDto { PostId = 1, ScheduledAt = DateTime.Now.AddDays(1) }));
        Assert.Equal(409, rentalError.StatusCode);
        Assert.Equal(409, appointmentError.StatusCode);
        Assert.Empty(await context.RentalRequests.ToListAsync());
        Assert.Empty(await context.ViewingAppointments.ToListAsync());
    }

    [Fact]
    public async Task Renting_and_releasing_room_updates_availability_without_hiding_post()
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var room = await context.Rooms.SingleAsync(r => r.Id == 1);
        var service = new PostBLL(context);
        foreach (var status in new[] { RoomStatus.Available, RoomStatus.Reserved, RoomStatus.Rented, RoomStatus.Available })
        {
            room.Status = status;
            await context.SaveChangesAsync();
            var list = await service.SearchPostsAsync(new PostQueryParameters { Keyword = "Tin cong khai", MinPrice = 2_000_000, MaxPrice = 4_000_000 });
            Assert.Equal(status, Assert.Single(list).RoomStatus);
            Assert.Equal(status, (await service.GetPublicPostByIdAsync(1)).RoomStatus);
        }
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedPostsAsync(ApplicationDbContext context)
    {
        var account = new User
        {
            Id = 1,
            UserName = "landlord",
            PasswordHash = "test",
            FullName = "Chu tro",
            Email = "landlord@example.test",
            RoleId = 2
        };
        var landlord = new LandlordProfile { Id = 1, AccountId = account.Id, Account = account };
        var category = new RoomCategory { Id = 1, Name = "Phong tro" };

        var publicRoom = CreateRoom(1, landlord, category, RoomStatus.Available);
        var reservedRoom = CreateRoom(2, landlord, category, RoomStatus.Reserved);
        var pendingRoom = CreateRoom(3, landlord, category, RoomStatus.Available);

        context.AddRange(
            new Post
            {
                Id = 1,
                RoomId = publicRoom.Id,
                Room = publicRoom,
                LandlordId = landlord.Id,
                Landlord = landlord,
                Title = "Tin cong khai",
                Content = "Noi dung phong cong khai",
                DisplayPrice = 3_000_000,
                Status = PostStatus.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new Post
            {
                Id = 2,
                RoomId = reservedRoom.Id,
                Room = reservedRoom,
                LandlordId = landlord.Id,
                Landlord = landlord,
                Title = "Tin phong da giu",
                Content = "Noi dung phong da giu",
                DisplayPrice = 3_000_000,
                Status = PostStatus.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new Post
            {
                Id = 3,
                RoomId = pendingRoom.Id,
                Room = pendingRoom,
                LandlordId = landlord.Id,
                Landlord = landlord,
                Title = "Tin cho duyet",
                Content = "Noi dung tin cho duyet",
                DisplayPrice = 3_000_000,
                Status = PostStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }

    private static Room CreateRoom(int id, LandlordProfile landlord, RoomCategory category, RoomStatus status) => new()
    {
        Id = id,
        LandlordId = landlord.Id,
        Landlord = landlord,
        CategoryId = category.Id,
        Category = category,
        RoomName = $"Phong {id}",
        Price = 3_000_000,
        Area = 25,
        MaxOccupants = 2,
        Address = "Dia chi test",
        Status = status,
        CreatedAt = DateTime.UtcNow
    };
}
