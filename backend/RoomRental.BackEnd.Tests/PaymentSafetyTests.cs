using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Payment;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public class PaymentSafetyTests
{
    [Fact]
    public void Refund_command_route_does_not_use_reserved_MVC_action_parameter()
    {
        var method=typeof(RoomRental.BackEnd.Controllers.DepositRefundController).GetMethod("Act")!;
        var route=Assert.Single(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPutAttribute),false).Cast<Microsoft.AspNetCore.Mvc.HttpPutAttribute>());
        Assert.Equal("{id:int}/{operation}",route.Template);
    }
    internal static async Task<ApplicationDbContext> Fixture()
    {
        var db = PostBLLTests.CreateContext();
        await WorkflowRegressionTests.SeedWorkflowAsync(db);
        db.Users.AddRange(new User { Id=30, RoleId=0, UserName="admin", Email="admin@safety.test", FullName="Admin", PasswordHash="test" },
            new User { Id=40, RoleId=1, UserName="other", Email="other@safety.test", FullName="Other", PasswordHash="test" });
        await db.SaveChangesAsync(); return db;
    }
    internal static DepositRefundBLL Refunds(ApplicationDbContext db) => new(db, null!, NullLogger<DepositRefundBLL>.Instance);
    private static RefundCreateDto Create => new() { DepositId=1, Reason="Tôi thay đổi kế hoạch thuê phòng" };

    [Theory]
    [InlineData(DepositStatus.Paid)]
    [InlineData(DepositStatus.Confirmed)]
    public async Task Refund_happy_path_keeps_room_until_tenant_confirms_and_preserves_payment_history(int original)
    {
        await using var db = await Fixture(); db.Deposits.Single().Status=original;
        db.PaymentTransactions.Add(new PaymentTransaction { DepositId=1, Amount=1000000, Status=PaymentTransactionStatus.Succeeded, OrderId="original", TransactionCode="original-payment" });
        await db.SaveChangesAsync();
        var service=Refunds(db);
        var r=await service.CreateAsync(10, Create);
        Assert.Equal(DepositStatus.RefundRequested, db.Deposits.Single().Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => new RentalRequestBLL(db,null!,NullLogger<RentalRequestBLL>.Instance).HuyAsync(10,1));
        await Assert.ThrowsAsync<BusinessRuleException>(() => new RoomBLL(db).UpdateRoomStatusAsync(1,1,RoomStatus.Available));
        await Assert.ThrowsAsync<BusinessRuleException>(() => new RentalContractBLL(db,null!,NullLogger<RentalContractBLL>.Instance).TaoAsync(1,new TaoHopDongDto { YeuCauThueId=1, NgayBatDau=DateTime.Today, NgayKetThuc=DateTime.Today.AddMonths(1), TienThueHangThang=3000000 }));
        await service.ActAsync(1,r.Id,"approve",new());
        await service.ActAsync(1,r.Id,"report",new() { Reference="BANK-REFUND-QA-001" });
        Assert.Equal(RoomStatus.Reserved, db.Rooms.Single(x=>x.Id==1).Status);
        Assert.Equal(DepositStatus.RefundRequested, db.Deposits.Single().Status);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>service.ActAsync(10,r.Id,"withdraw",new()));
        var done=await service.ActAsync(10,r.Id,"confirm",new());
        Assert.Equal(DepositRefundStatus.Completed,done.Status);
        var completed=done.CompletedAtUtc;
        Assert.Equal(completed,(await service.ActAsync(10,r.Id,"confirm",new())).CompletedAtUtc);
        Assert.Equal(RoomStatus.Available,db.Rooms.Single(x=>x.Id==1).Status);
        Assert.Equal(DepositStatus.Refunded,db.Deposits.Single().Status);
        Assert.Equal(RentalRequestStatus.Cancelled,db.RentalRequests.Single().Status);
        Assert.Equal(PaymentTransactionStatus.Succeeded,db.PaymentTransactions.Single().Status);
        Assert.Equal("original-payment",db.PaymentTransactions.Single().TransactionCode);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>service.CreateAsync(10,Create));
    }

    [Theory]
    [InlineData("reject", DepositStatus.Paid)]
    [InlineData("reject", DepositStatus.Confirmed)]
    [InlineData("withdraw", DepositStatus.Paid)]
    [InlineData("withdraw", DepositStatus.Confirmed)]
    public async Task Rejection_or_withdrawal_restores_exact_previous_status(string action,int original)
    {
        await using var db=await Fixture(); db.Deposits.Single().Status=original; await db.SaveChangesAsync();
        var s=Refunds(db); var r=await s.CreateAsync(10,Create);
        await s.ActAsync(action=="reject"?1:10,r.Id,action,new() { Note="Lý do kiểm thử hợp lệ" });
        Assert.Equal(original,db.Deposits.Single().Status);
        Assert.Equal(RoomStatus.Reserved,db.Rooms.Single(x=>x.Id==1).Status);
        Assert.Equal(RentalRequestStatus.Approved,db.RentalRequests.Single().Status);
        Assert.NotNull(await s.CreateAsync(10,Create));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task Any_existing_contract_blocks_pre_contract_refund(int status)
    {
        await using var db=await Fixture();
        db.RentalContracts.Add(new RentalContract { RentalRequestId=1,PostId=1,TenantAccountId=10,LandlordAccountId=1,Status=status }); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(()=>Refunds(db).CreateAsync(10,Create)); Assert.Empty(db.DepositRefundRequests);
    }

    [Fact]
    public async Task Ownership_validation_duplicate_state_and_admin_evidence_are_enforced()
    {
        await using var db=await Fixture(); var s=Refunds(db);
        foreach(var id in new[]{1,30,40}) Assert.Equal(403,(await Assert.ThrowsAsync<BusinessRuleException>(()=>s.CreateAsync(id,Create))).StatusCode);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.CreateAsync(10,new(){DepositId=1,Reason=" "}));
        var r=await s.CreateAsync(10,Create);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.CreateAsync(10,Create));
        Assert.Empty(await s.ListAsync(40));
        foreach(var id in new[]{10,30,40}) await Assert.ThrowsAsync<BusinessRuleException>(()=>s.ActAsync(id,r.Id,"approve",new()));
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.ActAsync(1,r.Id,"report",new(){Reference="proof"}));
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.ActAsync(1,r.Id,"approve",new(){RowVersion="stale"}));
        await s.ActAsync(1,r.Id,"approve",new());
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.ActAsync(1,r.Id,"report",new()));
        await s.ActAsync(1,r.Id,"report",new(){Reference="PROOF-01"});
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.ActAsync(1,r.Id,"confirm",new()));
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.ActAsync(30,r.Id,"confirm",new()));
        await s.ActAsync(30,r.Id,"confirm",new(){Note="Đã kiểm tra chứng từ và xác minh người nhận"});
        Assert.Equal(30,db.DepositRefundRequests.Single().CompletedByAccountId);
    }

    [Fact]
    public async Task Completing_refund_does_not_release_another_tenants_valid_reservation()
    {
        await using var db=await Fixture(); var s=Refunds(db); var r=await s.CreateAsync(10,Create);
        db.RentalRequests.Add(new RentalRequest {Id=2,PostId=1,TenantAccountId=40,LandlordAccountId=1,Status=RentalRequestStatus.Approved}); await db.SaveChangesAsync();
        await s.ActAsync(1,r.Id,"approve",new()); await s.ActAsync(1,r.Id,"report",new(){Reference="PROOF-02"}); await s.ActAsync(10,r.Id,"confirm",new());
        Assert.Equal(RoomStatus.Reserved,db.Rooms.Single(x=>x.Id==1).Status);
        Assert.Equal(RentalRequestStatus.Approved,db.RentalRequests.Single(x=>x.Id==2).Status);
    }

    [Theory]
    [InlineData("attempt")] [InlineData("request")] [InlineData("deadline")] [InlineData("other-tenant")] [InlineData("target-amount")]
    public async Task Late_or_invalid_entitlement_is_recorded_without_regranting_rights(string reason)
    {
        await using var db=await Fixture(); var d=db.Deposits.Single(); d.Status=DepositStatus.Pending;
        var p=new PaymentTransaction {DepositId=1,OrderId="ipn-order",Amount=1000000}; db.PaymentTransactions.Add(p);
        if(reason=="attempt") p.CreatedAt=DateTime.Now.AddMinutes(-31);
        if(reason=="deadline") d.DueAt=DateTime.Now.AddMinutes(-1);
        if(reason=="request") db.RentalRequests.Single().Status=RentalRequestStatus.Cancelled;
        if(reason=="target-amount") d.Amount=2000000;
        if(reason=="other-tenant") { db.RentalRequests.Single().Status=RentalRequestStatus.Expired; db.Rooms.Single(x=>x.Id==1).Status=RoomStatus.Rented; }
        await db.SaveChangesAsync();
        var roomBefore=db.Rooms.Single(x=>x.Id==1).Status; var requestBefore=db.RentalRequests.Single().Status;
        var s=PaymentIpnTests.Service(db); var query=PaymentIpnTests.Callback();
        var result=await s.ProcessPaymentReturnAsync(query);
        Assert.False(result.Success); Assert.True(result.RequiresReview); Assert.Equal(1000000,result.Amount);
        Assert.Equal(PaymentCallbackOutcome.Recorded,result.CallbackOutcome);
        Assert.Equal(PaymentEventState.RequiresReview,db.PaymentCallbackEvents.Single().ProcessingState);
        Assert.Equal(PaymentCallbackOutcome.AlreadyRecorded,(await s.ProcessPaymentReturnAsync(query)).CallbackOutcome);
        Assert.Single(db.PaymentCallbackEvents);
        Assert.Equal(roomBefore,db.Rooms.Single(x=>x.Id==1).Status); Assert.Equal(requestBefore,db.RentalRequests.Single().Status);
        Assert.Equal(DepositStatus.Pending,d.Status); Assert.NotEqual(PaymentTransactionStatus.Succeeded,p.Status);
    }

    [Fact]
    public async Task Ledger_separates_sources_blocks_retry_and_admin_review_never_changes_money_or_rights()
    {
        await using var db=await Fixture(); db.Deposits.Single().Status=DepositStatus.Pending;
        db.PaymentTransactions.Add(new PaymentTransaction{DepositId=1,OrderId="ipn-order",Amount=1000000,CreatedAt=DateTime.Now.AddMinutes(-31)}); await db.SaveChangesAsync();
        var s=PaymentIpnTests.Service(db); var query=PaymentIpnTests.Callback();
        var first=await s.ProcessPaymentReturnAsync(query); await s.ProcessPaymentReturnAsync(query,PaymentEventSource.Return);
        Assert.Equal(2,db.PaymentCallbackEvents.Count());
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.CreatePaymentUrlAsync(10,new(){DepositId=1},"127.0.0.1"));
        var audit=new PaymentEventBLL(db); Assert.Empty(await audit.ListAsync(40)); Assert.Equal(2,(await audit.ListAsync(10)).Count);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>audit.ResolveAsync(10,first.PaymentEventId!.Value,new(){Note="Tenant cannot resolve"}));
        await Assert.ThrowsAsync<BusinessRuleException>(()=>audit.ResolveAsync(30,first.PaymentEventId!.Value,new(){Note=" "}));
        await audit.ResolveAsync(30,first.PaymentEventId!.Value,new(){Note="Đã đối soát và hoàn tiền bên ngoài theo chứng từ QA-123"});
        Assert.All(db.PaymentCallbackEvents,e=>Assert.Equal(PaymentEventState.Resolved,e.ProcessingState));
        Assert.Equal(DepositStatus.Pending,db.Deposits.Single().Status);
        Assert.Equal(RoomStatus.Reserved,db.Rooms.Single(x=>x.Id==1).Status);
    }

    [Fact]
    public async Task Unknown_order_is_admin_only_and_amount_mismatch_preserves_reported_amount()
    {
        await using var db=await Fixture(); var s=PaymentIpnTests.Service(db);
        var unknown=await s.ProcessPaymentReturnAsync(PaymentIpnTests.Callback(order:"unknown"));
        Assert.Equal(PaymentCallbackOutcome.OrderNotFound,unknown.CallbackOutcome);
        Assert.Null(db.PaymentCallbackEvents.Single().PaymentTransactionId);
        Assert.Empty(await new PaymentEventBLL(db).ListAsync(10));
        db.Deposits.Single().Status=DepositStatus.Pending; db.PaymentTransactions.Add(new PaymentTransaction{DepositId=1,OrderId="ipn-order",Amount=1000000}); await db.SaveChangesAsync();
        var mismatch=await s.ProcessPaymentReturnAsync(PaymentIpnTests.Callback(amount:"150000000"));
        Assert.Equal(PaymentCallbackOutcome.InvalidAmount,mismatch.CallbackOutcome);
        Assert.Equal(1500000,mismatch.Amount); Assert.Equal(1000000,db.PaymentTransactions.Single().Amount);
        Assert.Equal(DepositStatus.Pending,db.Deposits.Single().Status);
    }

    [Fact]
    public async Task Incoming_review_during_refund_blocks_completion_until_admin_reconciles()
    {
        await using var db=await Fixture(); var refunds=Refunds(db); var r=await refunds.CreateAsync(10,Create);
        await refunds.ActAsync(1,r.Id,"approve",new());
        db.PaymentTransactions.Add(new PaymentTransaction{DepositId=1,OrderId="ipn-order",Amount=1000000}); await db.SaveChangesAsync();
        var result=await PaymentIpnTests.Service(db).ProcessPaymentReturnAsync(PaymentIpnTests.Callback());
        Assert.True(result.RequiresReview);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>refunds.ActAsync(1,r.Id,"report",new(){Reference="proof-123"}));
        await new PaymentEventBLL(db).ResolveAsync(30,result.PaymentEventId!.Value,new(){Note="Đã đối soát giao dịch phát sinh bên ngoài bằng chứng QA-123"});
        await refunds.ActAsync(1,r.Id,"report",new(){Reference="proof-123"}); await refunds.ActAsync(10,r.Id,"confirm",new());
        Assert.Equal(DepositStatus.Refunded,db.Deposits.Single().Status);
        await PaymentIpnTests.Service(db).ProcessPaymentReturnAsync(PaymentIpnTests.Callback(),PaymentEventSource.Return);
        Assert.Equal(DepositStatus.Refunded,db.Deposits.Single().Status);
        Assert.All(db.PaymentCallbackEvents,e=>Assert.Equal(PaymentEventState.Resolved,e.ProcessingState));
    }

    [Fact]
    public async Task Provider_pay_date_is_converted_from_Vietnam_to_UTC_and_signature_is_not_stored()
    {
        await using var db=await Fixture();
        var signer=new RoomRental.BackEnd.Helpers.VnPayHelper();
        foreach(var pair in PaymentIpnTests.Callback().Where(p=>p.Key!="vnp_SecureHash")) signer.AddRequestData(pair.Key,pair.Value.ToString());
        signer.AddRequestData("vnp_PayDate","20261031235959");
        var query=new Microsoft.AspNetCore.Http.QueryCollection(Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(signer.CreateRequestUrl("https://example.test/callback",PaymentIpnTests.Config()["VnPay:HashSecret"]!)).Query));
        await PaymentIpnTests.Service(db).ProcessPaymentReturnAsync(query);
        var receipt=db.PaymentCallbackEvents.Single();
        Assert.Equal(new DateTime(2026,10,31,16,59,59,DateTimeKind.Utc),receipt.ProviderPaidAtUtc);
        Assert.Equal(64,receipt.DeduplicationKey.Length);
        Assert.NotEqual(query["vnp_SecureHash"].ToString(),receipt.DeduplicationKey);
    }

    [Fact]
    public async Task Correctly_signed_callback_for_wrong_merchant_does_not_create_a_receipt()
    {
        await using var db=await Fixture(); var signer=new RoomRental.BackEnd.Helpers.VnPayHelper();
        foreach(var pair in PaymentIpnTests.Callback().Where(p=>p.Key!="vnp_SecureHash")) signer.AddRequestData(pair.Key,pair.Key=="vnp_TmnCode"?"OTHER":pair.Value.ToString());
        var query=new Microsoft.AspNetCore.Http.QueryCollection(Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(signer.CreateRequestUrl("https://example.test/callback",PaymentIpnTests.Config()["VnPay:HashSecret"]!)).Query));
        Assert.Equal(PaymentCallbackOutcome.InvalidSignature,(await PaymentIpnTests.Service(db).ProcessPaymentReturnAsync(query)).CallbackOutcome);
        Assert.Empty(db.PaymentCallbackEvents);
    }
}

[Collection("SqlServerWorkflow")]
public class PaymentSafetySqlTests
{
    [SqlServerFact]
    public async Task Refund_concurrency_completion_and_late_receipt_are_atomic_on_sql()
    {
        var b=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_",b.InitialCatalog); b.InitialCatalog+="_PaymentSafety";
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(b.ConnectionString).Options;
        await using var db=new ApplicationDbContext(options); await db.Database.EnsureCreatedAsync();
        var password=BCrypt.Net.BCrypt.HashPassword("SafetyOnly-2026!");
        User Account(string name,int role)=>new(){UserName=name+"@safety.test",Email=name+"@safety.test",FullName="QA "+name,RoleId=role,PasswordHash=password};
        var owner=new LandlordProfile{Account=Account("owner",2)}; var tenant=Account("tenant",1); var admin=Account("admin",0);
        var room=new Room{Landlord=owner,Category=new RoomCategory{Name="Safety QA"},RoomName="Safety QA",Address="QA",Price=3000000,Area=25,MaxOccupants=2,Status=RoomStatus.Reserved};
        var post=new Post{Room=room,Landlord=owner,Title="Safety QA",Content="QA",Status=PostStatus.Approved};
        db.AddRange(post,tenant,admin); await db.SaveChangesAsync();
        var request=new RentalRequest{PostId=post.Id,TenantAccountId=tenant.Id,LandlordAccountId=owner.AccountId,Status=RentalRequestStatus.Approved}; db.Add(request); await db.SaveChangesAsync();
        var deposit=new Deposit{RentalRequestId=request.Id,TenantAccountId=tenant.Id,LandlordAccountId=owner.AccountId,Amount=1000000,Status=DepositStatus.Confirmed,PaidAt=DateTime.Now.AddDays(-1)};
        db.Add(deposit); await db.SaveChangesAsync();
        var paidAt=deposit.PaidAt;
        db.Add(new PaymentTransaction{DepositId=deposit.Id,OrderId="original-paid",Amount=1000000,Status=PaymentTransactionStatus.Succeeded,TransactionCode="original-paid-transaction"}); await db.SaveChangesAsync();
        async Task<int> Create()
        {
            await using var context=new ApplicationDbContext(options);
            try {await PaymentSafetyTests.Refunds(context).CreateAsync(tenant.Id,new(){DepositId=deposit.Id,Reason="Đổi kế hoạch thuê QA"});return 200;}
            catch(BusinessRuleException e){return e.StatusCode;}
        }
        Assert.Equal(new[]{200,409},(await Task.WhenAll(Create(),Create())).Order().ToArray());
        db.ChangeTracker.Clear(); var refund=await db.DepositRefundRequests.SingleAsync(); var service=PaymentSafetyTests.Refunds(db);
        await service.ActAsync(owner.AccountId,refund.Id,"approve",new()); await service.ActAsync(owner.AccountId,refund.Id,"report",new(){Reference="QA-PROOF-PAID"});
        async Task<DepositRefundDto> Complete()
        {
            await using var context=new ApplicationDbContext(options);
            return await PaymentSafetyTests.Refunds(context).ActAsync(tenant.Id,refund.Id,"confirm",new());
        }
        var results=await Task.WhenAll(Complete(),Complete());
        Assert.All(results,x=>Assert.Equal(DepositRefundStatus.Completed,x.Status)); Assert.Equal(results[0].CompletedAtUtc,results[1].CompletedAtUtc);
        db.ChangeTracker.Clear();
        Assert.Equal(RoomStatus.Available,(await db.Rooms.SingleAsync()).Status);
        Assert.Equal(DepositStatus.Refunded,(await db.Deposits.SingleAsync()).Status);
        Assert.Equal(paidAt!.Value.ToString("s"),(await db.Deposits.SingleAsync()).PaidAt!.Value.ToString("s"));
        Assert.Equal(PaymentTransactionStatus.Succeeded,(await db.PaymentTransactions.SingleAsync()).Status);
        db.Add(new PaymentTransaction{DepositId=deposit.Id,OrderId="ipn-order",Amount=1000000,CreatedAt=DateTime.Now.AddMinutes(-31)}); await db.SaveChangesAsync();
        async Task<string> Callback()
        {
            await using var context=new ApplicationDbContext(options);
            return PaymentIpnTests.Code(await PaymentIpnTests.Ipn(context,PaymentIpnTests.Callback()));
        }
        Assert.Equal(new[]{"00","02"},(await Task.WhenAll(Callback(),Callback())).Order().ToArray());
        db.ChangeTracker.Clear();
        Assert.Equal(PaymentEventState.RequiresReview,(await db.PaymentCallbackEvents.SingleAsync()).ProcessingState);
        Assert.Equal(DepositStatus.Refunded,(await db.Deposits.SingleAsync()).Status); Assert.Equal(RoomStatus.Available,(await db.Rooms.SingleAsync()).Status);
        Assert.Equal(8,(await db.PaymentCallbackEvents.SingleAsync()).RowVersion.Length);
        var original=await db.PaymentCallbackEvents.SingleAsync();
        db.Add(new PaymentCallbackEvent{DeduplicationKey=original.DeduplicationKey,ProviderOrderId="duplicate",ReportedAmount=1,ResponseCode="00",ProviderStatus="00"});
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync()); db.ChangeTracker.Clear();
    }
}
