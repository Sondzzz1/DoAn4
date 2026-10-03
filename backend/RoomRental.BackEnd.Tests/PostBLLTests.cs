using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public sealed class PostBLLTests
{
    [Fact]
    public async Task SearchPostsAsync_returns_only_approved_posts_with_available_rooms()
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var service = new PostBLL(context);

        var posts = await service.SearchPostsAsync(new PostQueryParameters());

        var post = Assert.Single(posts);
        Assert.Equal("Tin cong khai", post.Title);
        Assert.Equal(PostStatus.Approved, post.Status);
        Assert.Equal(RoomStatus.Available, post.RoomStatus);
    }

    [Fact]
    public async Task GetPublicPostByIdAsync_throws_not_found_for_non_public_post()
    {
        await using var context = CreateContext();
        await SeedPostsAsync(context);
        var service = new PostBLL(context);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPublicPostByIdAsync(2));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public void CalculateDistance_returns_zero_for_the_same_coordinates()
    {
        var distance = PostBLL.CalculateDistance(21.0285, 105.8542, 21.0285, 105.8542);

        Assert.Equal(0, distance, precision: 8);
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
