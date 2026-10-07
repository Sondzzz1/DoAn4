using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class RoomPublicationTests
{
    [Fact]
    public async Task Create_reuses_existing_room_and_room_price_without_changing_old_data()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Posts.Single(p => p.Id == 1).Status = PostStatus.Hidden;
        await db.SaveChangesAsync();
        var count = await db.Rooms.CountAsync();
        var created = await new PostBLL(db).CreatePostAsync(1, new CreatePostDto { RoomId = 1, Title = "New advertisement", Description = "New content" });
        Assert.Equal(count, await db.Rooms.CountAsync());
        Assert.Equal(1, created.RoomId);
        Assert.Equal(3_000_000m, created.Price);
        Assert.Equal("Phong 1", (await db.Rooms.FindAsync(1))!.RoomName);
        Assert.Equal(PostStatus.Hidden, (await db.Posts.FindAsync(1))!.Status);
        Assert.Equal("Tin cong khai", (await new PostBLL(db).GetMyPostByIdAsync(1, 1)).Title);
    }

    [Theory]
    [InlineData(PostStatus.Pending)]
    [InlineData(PostStatus.Approved)]
    public async Task Active_slot_blocks_creation_and_resubmission(PostStatus status)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Posts.Single(p => p.Id == 1).Status = status;
        db.Posts.Add(new Post { Id = 10, RoomId = 1, LandlordId = 1, Title = "History", Content = "", Status = PostStatus.Hidden });
        await db.SaveChangesAsync();
        var service = new PostBLL(db);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreatePostAsync(1, new CreatePostDto { RoomId = 1, Title = "Duplicate" }))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdatePostStatusAsync(1, 10, PostStatus.Pending))).StatusCode);
    }

    [Fact]
    public async Task Other_owner_cannot_create_read_update_delete_or_change_status()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Users.Add(new User { Id = 20, RoleId = 2, UserName = "other", Email = "other@test", FullName = "Other", PasswordHash = "test" });
        db.LandlordProfiles.Add(new LandlordProfile { Id = 20, AccountId = 20 });
        await db.SaveChangesAsync();
        var rooms = new RoomBLL(db);
        var errors = new[]
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => new PostBLL(db).CreatePostAsync(20, new CreatePostDto { RoomId = 1, Title = "Other" })),
            await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.GetAccessibleRoomAsync(20, 1)),
            await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomAsync(20, 1, new UpdateRoomDto { Gia = 1 })),
            await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.DeleteRoomAsync(20, 1)),
            await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomStatusAsync(20, 1, RoomStatus.TemporarilyUnavailable))
        };
        Assert.All(errors, e => Assert.Equal(403, e.StatusCode));
    }

    [Theory]
    [InlineData(RoomStatus.Reserved)]
    [InlineData(RoomStatus.Rented)]
    [InlineData(RoomStatus.TemporarilyUnavailable)]
    public async Task Unavailable_room_cannot_be_advertised(RoomStatus status)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Rooms.Single(r => r.Id == 1).Status = status;
        db.Posts.Single(p => p.Id == 1).Status = PostStatus.Hidden;
        await db.SaveChangesAsync();
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => new PostBLL(db).CreatePostAsync(1, new CreatePostDto { RoomId = 1, Title = "Not available" }))).StatusCode);
    }

    [Fact]
    public async Task Public_edit_requires_reapproval_then_admin_can_approve()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        await new RoomBLL(db).UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 4_000_000, Area = 30 });
        Assert.Equal(PostStatus.Pending, db.Posts.Single(p => p.Id == 1).Status);
        Assert.Empty(await new PostBLL(db).SearchPostsAsync(new PostQueryParameters()));
        var result = await new AdminBLL(db, new PostBLL(db)).ApprovePostAsync(1);
        Assert.Equal(PostStatus.Approved, result.Status);
        Assert.Equal(4_000_000m, result.Price);
        Assert.Single(await new PostBLL(db).SearchPostsAsync(new PostQueryParameters()));
    }

    [Fact]
    public async Task No_op_edit_and_technical_status_do_not_reset_moderation()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        await new RoomBLL(db).UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 3_000_000, ImageUrls = new(), AmenityIds = new() });
        Assert.Equal(PostStatus.Approved, db.Posts.Single(p => p.Id == 1).Status);
        await new RoomBLL(db).UpdateRoomStatusAsync(1, 1, RoomStatus.TemporarilyUnavailable);
        Assert.Equal(PostStatus.Approved, db.Posts.Single(p => p.Id == 1).Status);
    }

    [Fact]
    public async Task Admin_reads_rooms_without_becoming_landlord_and_cannot_create_posts()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Users.Add(new User { Id = 30, RoleId = 0, UserName = "admin", Email = "admin@test", FullName = "Admin", PasswordHash = "test" });
        await db.SaveChangesAsync();
        Assert.Equal(1, (await new RoomBLL(db).GetAccessibleRoomAsync(30, 1)).Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => new PostBLL(db).CreatePostAsync(30, new CreatePostDto { RoomId = 1, Title = "Admin" }));
        Assert.False(await db.LandlordProfiles.AnyAsync(l => l.AccountId == 30));
    }

    [Fact]
    public async Task Create_room_saves_images_and_amenities_without_creating_a_post()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Amenities.Add(new Amenity { Id = 10, Name = "Wifi" });
        await db.SaveChangesAsync();
        var room = await new RoomBLL(db).CreateRoomAsync(1, new CreateRoomDto
        {
            RoomName = "Physical room", Price = 2_000_000, Area = 20, Address = "Test address",
            AmenityIds = new() { 10, 10 }, ImageUrls = new() { "/uploads/test.jpg" }
        });
        Assert.Equal(4, await db.Rooms.CountAsync());
        Assert.Equal(3, await db.Posts.CountAsync());
        Assert.Equal(new[] { 10 }, room.AmenityIds);
        Assert.Equal(new[] { "/uploads/test.jpg" }, room.ImageUrls);
    }

    [Fact]
    public async Task Omitted_room_collections_are_preserved_and_explicit_empty_aliases_clear_them()
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.Amenities.Add(new Amenity { Id = 10, Name = "Wifi" });
        db.PostAmenities.Add(new PostAmenity { RoomId = 1, AmenityId = 10 });
        db.PostImages.Add(new PostImage { RoomId = 1, ImageUrl = "/uploads/old.jpg", IsThumbnail = true });
        await db.SaveChangesAsync();
        var rooms = new RoomBLL(db);
        var unchanged = await rooms.UpdateRoomAsync(1, 1, new UpdateRoomDto { Price = 3_000_000 });
        Assert.Equal(new[] { 10 }, unchanged.AmenityIds);
        Assert.Equal(new[] { "/uploads/old.jpg" }, unchanged.ImageUrls);
        Assert.Equal(PostStatus.Approved, db.Posts.Single(p => p.Id == 1).Status);
        var cleared = await rooms.UpdateRoomAsync(1, 1, new UpdateRoomDto { TienIchIds = new(), DanhSachAnh = new() });
        Assert.Empty(cleared.AmenityIds);
        Assert.Empty(cleared.ImageUrls);
        Assert.Equal(PostStatus.Pending, db.Posts.Single(p => p.Id == 1).Status);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("price")]
    [InlineData("area")]
    [InlineData("category")]
    [InlineData("occupants")]
    [InlineData("bedrooms")]
    [InlineData("bathrooms")]
    [InlineData("floor")]
    [InlineData("address")]
    [InlineData("province")]
    [InlineData("district")]
    [InlineData("ward")]
    [InlineData("coordinates")]
    [InlineData("electricity")]
    [InlineData("water")]
    [InlineData("service")]
    [InlineData("amenities")]
    [InlineData("images")]
    public async Task Every_public_room_field_requires_reapproval(string field)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.RoomCategories.Add(new RoomCategory { Id = 10, Name = "Apartment" });
        db.Amenities.Add(new Amenity { Id = 10, Name = "Wifi" });
        await db.SaveChangesAsync();
        var dto = new UpdateRoomDto();
        switch (field)
        {
            case "name": dto.RoomName = "New name"; break;
            case "description": dto.Description = "New description"; break;
            case "price": dto.Price = 4_000_000; break;
            case "area": dto.Area = 30; break;
            case "category": dto.CategoryId = 10; break;
            case "occupants": dto.MaxOccupants = 3; break;
            case "bedrooms": dto.Bedrooms = 1; break;
            case "bathrooms": dto.Bathrooms = 1; break;
            case "floor": dto.Floor = 2; break;
            case "address": dto.Address = "New address"; break;
            case "province": dto.Province = "New province"; break;
            case "district": dto.District = "New district"; break;
            case "ward": dto.Ward = "New ward"; break;
            case "coordinates": dto.Latitude = 21; dto.Longitude = 105; break;
            case "electricity": dto.ElectricityPrice = 4000; break;
            case "water": dto.WaterPrice = 10000; break;
            case "service": dto.ServiceFee = 50000; break;
            case "amenities": dto.AmenityIds = new() { 10 }; break;
            case "images": dto.ImageUrls = new() { "/uploads/new.jpg" }; break;
        }
        await new RoomBLL(db).UpdateRoomAsync(1, 1, dto);
        Assert.Equal(PostStatus.Pending, db.Posts.Single(p => p.Id == 1).Status);
        Assert.Equal(PostStatus.Approved, (await new AdminBLL(db, new PostBLL(db)).ApprovePostAsync(1)).Status);
    }

    [Theory]
    [InlineData("price")]
    [InlineData("area")]
    [InlineData("occupants")]
    [InlineData("address")]
    [InlineData("coordinatePair")]
    [InlineData("coordinateRange")]
    [InlineData("fee")]
    [InlineData("category")]
    [InlineData("amenity")]
    [InlineData("image")]
    public async Task Invalid_room_does_not_leave_a_partially_created_room(string invalidField)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        var dto = new CreateRoomDto { RoomName = "Invalid", Price = 1_000_000, Area = 20, Address = "Test" };
        switch (invalidField)
        {
            case "price": dto.Price = 0; break;
            case "area": dto.Area = -1; break;
            case "occupants": dto.MaxOccupants = 0; break;
            case "address": dto.Address = ""; break;
            case "coordinatePair": dto.Latitude = 21; break;
            case "coordinateRange": dto.Latitude = 100; dto.Longitude = 105; break;
            case "fee": dto.ServiceFee = -1; break;
            case "category": dto.CategoryId = 999; break;
            case "amenity": dto.AmenityIds = new() { 999 }; break;
            case "image": dto.ImageUrls = new() { "" }; break;
        }
        await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).CreateRoomAsync(1, dto));
        Assert.Equal(3, await db.Rooms.CountAsync());
        Assert.Empty(await db.PostImages.ToListAsync());
    }

    [Theory]
    [InlineData(RentalContractStatus.PendingSignature)]
    [InlineData(RentalContractStatus.PendingStart)]
    [InlineData(RentalContractStatus.Active)]
    public async Task Effective_contract_prevents_manual_room_status_override(int status)
    {
        await using var db = PostBLLTests.CreateContext();
        await PostBLLTests.SeedPostsAsync(db);
        db.RentalContracts.Add(new RentalContract { PostId = 1, Status = status });
        await db.SaveChangesAsync();
        var rooms = new RoomBLL(db);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.UpdateRoomStatusAsync(1, 1, RoomStatus.TemporarilyUnavailable))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessRuleException>(() => rooms.DeleteRoomAsync(1, 1))).StatusCode);
        Assert.Equal(RoomStatus.Available, (await db.Rooms.FindAsync(1))!.Status);
    }
}
