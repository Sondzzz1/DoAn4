using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Post;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.DTO.Room;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL")))
            Skip = "Set ROOM_RENTAL_TEST_SQL to an isolated RoomRental_Verification_* database.";
    }
}

public class SqlServerWorkflowTests
{
    [SqlServerFact]
    public async Task SqlServer_concurrent_reservations_and_posts_have_one_winner_and_database_constraint()
    {
        var connection = Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL")!;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var export = Environment.GetEnvironmentVariable("ROOM_RENTAL_SCHEMA_EXPORT");
        if (!string.IsNullOrEmpty(export))
        {
            var script = db.Database.GenerateCreateScript();
            // Generated from the current EF model; bootstrap never alters an existing schema.
            script = System.Text.RegularExpressions.Regex.Replace(script, @"^GO\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            await File.WriteAllTextAsync(export,
                "-- Generated current EF schema. Select the target database before running.\n" +
                "SET XACT_ABORT ON;\nSET ANSI_NULLS ON;\nSET ANSI_PADDING ON;\nSET ANSI_WARNINGS ON;\nSET ARITHABORT ON;\nSET CONCAT_NULL_YIELDS_NULL ON;\nSET QUOTED_IDENTIFIER ON;\nSET NUMERIC_ROUNDABORT OFF;\nIF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL\nBEGIN\nBEGIN TRANSACTION;\n" +
                script + "\nCOMMIT;\nEND;\n");
        }
        var landlord = new LandlordProfile { Account = new User { UserName = "sql-landlord", FullName = "SQL landlord", PasswordHash = "test", Email = "sql-landlord@test", RoleId = 2 } };
        var room = new Room { RoomName = "SQL room", Price = 3000000, Area = 25, MaxOccupants = 2, Address = "Test", Landlord = landlord, Category = new RoomCategory { Name = "Test category" } };
        var post = new Post { Room = room, Landlord = landlord, Title = "SQL post", Content = "", Status = PostStatus.Approved, DisplayPrice = room.Price };
        var tenantA = new User { UserName = "sql-tenant-a", FullName = "SQL tenant A", Email = "sql-a@test", PasswordHash = "test", RoleId = 1 };
        var tenantB = new User { UserName = "sql-tenant-b", FullName = "SQL tenant B", Email = "sql-b@test", PasswordHash = "test", RoleId = 1 };
        db.AddRange(post, tenantA, tenantB); await db.SaveChangesAsync();
        var a = new RentalRequest { PostId = post.Id, TenantAccountId = tenantA.Id, LandlordAccountId = landlord.AccountId };
        var b = new RentalRequest { PostId = post.Id, TenantAccountId = tenantB.Id, LandlordAccountId = landlord.AccountId };
        db.AddRange(a, b); await db.SaveChangesAsync();
        async Task<int> Approve(int requestId)
        {
            await using var context = new ApplicationDbContext(options);
            try
            {
                await new RentalRequestBLL(context, null!, NullLogger<RentalRequestBLL>.Instance)
                    .CapNhatTrangThaiAsync(landlord.AccountId, requestId, RentalRequestStatus.Approved, soTienDatCoc: 1000000, hanThanhToanCoc: DateTime.Now.AddDays(1));
                return 200;
            }
            catch (BusinessRuleException e) { return e.StatusCode; }
        }
        Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(Approve(a.Id), Approve(b.Id))).Order().ToArray());
        db.ChangeTracker.Clear();
        Assert.Equal(RoomStatus.Reserved, (await db.Rooms.FindAsync(room.Id))!.Status);
        Assert.Single(await db.Deposits.ToListAsync());
        Assert.Equal(1, await db.RentalRequests.CountAsync(r => r.Status == RentalRequestStatus.Approved));

        var secondRoom = new Room { RoomName = "SQL new room", LandlordId = landlord.Id, CategoryId = room.CategoryId, Price = 3000000, Area = 25, Address = "Other" };
        db.Rooms.Add(secondRoom); await db.SaveChangesAsync();
        async Task<int> Publish()
        {
            await using var context = new ApplicationDbContext(options);
            try { await new PostBLL(context).CreatePostAsync(landlord.AccountId, new CreatePostDto { RoomId = secondRoom.Id, Title = "Concurrent post" }); return 200; }
            catch (BusinessRuleException e) { return e.StatusCode; }
        }
        Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(Publish(), Publish())).Order().ToArray());
        Assert.Equal(2, await db.Rooms.CountAsync());
        db.Posts.Add(new Post { RoomId = secondRoom.Id, LandlordId = landlord.Id, Title = "Bypass service", Content = "", DisplayPrice = 3000000, Status = PostStatus.Approved });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var request = await db.RentalRequests.SingleAsync(r => r.Status == RentalRequestStatus.Approved);
        var deposit = await db.Deposits.SingleAsync();
        deposit.Status = DepositStatus.Confirmed;
        await db.SaveChangesAsync();
        var contracts = new RentalContractBLL(db, null!, NullLogger<RentalContractBLL>.Instance);
        var contract = await contracts.TaoAsync(landlord.AccountId, new TaoHopDongDto
        {
            YeuCauThueId = request.Id, NgayBatDau = DateTime.Now.AddMinutes(-1),
            NgayKetThuc = DateTime.Now.AddMonths(3), TienThueHangThang = room.Price
        });
        await contracts.XacNhanAsync(landlord.AccountId, contract.Id);
        await contracts.XacNhanAsync(request.TenantAccountId, contract.Id);
        var bills = new MonthlyBillBLL(db, null!, NullLogger<MonthlyBillBLL>.Instance);
        var now = DateTime.Now;
        await bills.TaoAsync(landlord.AccountId, new TaoHoaDonDto { HopDongId = contract.Id, Thang = now.Month, Nam = now.Year, SoDienMoi = 10, SoNuocMoi = 20 });
        var next = now.AddMonths(1);
        var bill = await bills.TaoAsync(landlord.AccountId, new TaoHoaDonDto { HopDongId = contract.Id, Thang = next.Month, Nam = next.Year, SoDienCu = 999, SoDienMoi = 15, SoNuocCu = 999, SoNuocMoi = 25 });
        Assert.Equal(10, bill.SoDienCu);
        Assert.Equal(20, bill.SoNuocCu);
        var physical = await new RoomBLL(db).CreateRoomAsync(landlord.AccountId, new CreateRoomDto
        {
            RoomName = "SQL physical room", CategoryId = room.CategoryId, Price = 2000000,
            Area = 20, Address = "Test", ImageUrls = new() { "/uploads/test.jpg" }
        });
        Assert.Equal(new[] { "/uploads/test.jpg" }, physical.ImageUrls);
        Assert.Null(physical.ActivePostId);
    }
}
