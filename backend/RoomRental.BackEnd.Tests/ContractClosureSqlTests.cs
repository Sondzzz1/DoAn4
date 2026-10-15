using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.DTO.Rental;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

[Collection("SqlServerWorkflow")]
public class ContractClosureSqlTests
{
    internal static readonly string[] Tables = ["HoSoKetThucHopDong", "YeuCauKetThucHopDong", "PhuongAnTatToanHopDong", "KhoanMucTatToanHopDong", "LichSuKetThucHopDong"];
    private static DbContextOptions<ApplicationDbContext> Options(string suffix)
    {
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", b.InitialCatalog); b.InitialCatalog += suffix;
        return new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(b.ConnectionString).Options;
    }
    internal static bool IsClosureBatch(string batch)
    {
        var m = Regex.Match(batch, @"(?:CREATE TABLE|\bON) \[(?<table>[^\]]+)\]", RegexOptions.IgnoreCase);
        return m.Success && Tables.Contains(m.Groups["table"].Value);
    }
    // Export is mechanical from EF metadata so the manual upgrade cannot drift from EnsureCreated QA.
    internal static string UpgradeScript(ApplicationDbContext db)
    {
        static string Canonical(string sql)
        {
            return Regex.Replace(sql, @"[\s\[\]()]", "");
        }
        var batches = Regex.Split(db.Database.GenerateCreateScript(), @"^GO\s*$", RegexOptions.Multiline).Where(IsClosureBatch).ToArray();
        var model = db.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var tables = model.Tables.Where(t => Tables.Contains(t.Name)).ToList();
        var columns = tables.SelectMany(t => t.Columns.Select(c =>
        {
            var type = Regex.Match(c.StoreType, @"^\w+").Value;
            if (type == "rowversion") type = "timestamp";
            var length = Regex.Match(c.StoreType, @"\((max|\d+)\)");
            var maxLength = length.Success && type is "nvarchar" or "varchar" or "char" ? length.Groups[1].Value == "max" ? -1 : int.Parse(length.Groups[1].Value) * (type == "nvarchar" ? 2 : 1) : 0;
            return $"(N'{t.Name}',N'{c.Name}',N'{type}',{maxLength},{(c.IsNullable ? 1 : 0)})";
        }));
        var indexes = tables.SelectMany(t => t.Indexes.Select(i => $"(N'{t.Name}',N'{i.Name}',{(i.IsUnique ? 1 : 0)},N'{Canonical(i.Filter ?? "").Replace("'", "''")}',N'{string.Join(",", i.Columns.Select(c => c.Name))}')"));
        // Let SQL Server canonicalize CHECK expressions; IN/BETWEEN are rewritten by the engine.
        using var metadataConnection = new Microsoft.Data.SqlClient.SqlConnection(db.Database.GetConnectionString());
        Assert.StartsWith("RoomRental_Verification_", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(metadataConnection.ConnectionString).InitialCatalog);
        metadataConnection.Open();
        var constraints = tables.SelectMany(t => t.CheckConstraints.Select(c =>
        {
            using var command = metadataConnection.CreateCommand();
            var shape = string.Join(",", t.Columns.Where(x => x.Name != "RowVersion").Select(x => $"[{x.Name}] {x.StoreType} NULL"));
            command.CommandText = $"CREATE TABLE #ClosureCheckShape ({shape},CHECK({c.Sql})); SELECT definition FROM tempdb.sys.check_constraints WHERE parent_object_id=OBJECT_ID('tempdb..#ClosureCheckShape'); DROP TABLE #ClosureCheckShape;";
            var definition = (string)command.ExecuteScalar()!;
            return $"(N'{t.Name}',N'{c.Name}',N'{definition.Replace("'", "''")}')";
        })).ToArray();
        var fks = tables.SelectMany(t => t.ForeignKeyConstraints.Select(f => $"(N'{t.Name}',N'{f.Name}',N'{f.PrincipalTable.Name}',N'{string.Join(",", f.Columns.Select(c => c.Name))}',N'{string.Join(",", f.PrincipalColumns.Select(c => c.Name))}')"));
        var names = string.Join(",", Tables.Select(t => $"N'{t}'"));
        return """
-- Generated additive upgrade for the approved Stage 3A schema. No old rows are updated.
-- Back up first. Only deliberately enable the main database after review.
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
DECLARE @AllowMainDatabase bit=0;
IF DB_NAME()=N'RoomRentalDB' AND @AllowMainDatabase<>1 THROW 51060,'Main database requires explicit opt-in and backup.',1;
IF DB_NAME()<>N'RoomRentalDB' AND DB_NAME() NOT LIKE N'RoomRental[_]Verification[_]%' THROW 51061,'Unexpected database.',1;
IF OBJECT_ID(N'dbo.NguoiOCungHopDong',N'U') IS NULL OR OBJECT_ID(N'dbo.TinTimNguoiOGhep',N'U') IS NULL OR OBJECT_ID(N'dbo.YeuCauOGhep',N'U') IS NULL OR OBJECT_ID(N'dbo.SuKienThanhToan',N'U') IS NULL OR OBJECT_ID(N'dbo.YeuCauHoanCoc',N'U') IS NULL
    THROW 51062,'Apply prerequisite SQL 07 and 08 after review first.',1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'RoomRental.Schema.ContractClosure',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
IF @lock<0 THROW 51063,'Schema upgrade busy.',1;
""" + $"\nDECLARE @count int=(SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN ({names}));\nIF @count NOT IN (0,5) THROW 51064,'Partial closure schema. Inspect before retry.',1;\nIF @count=0\nBEGIN\n" +
            string.Join("\n", batches) + "\nEND;\n" +
            "DECLARE @columns TABLE(T sysname,C sysname,TypeName sysname,MaxLength int,Nullable bit);\nINSERT @columns VALUES\n" + string.Join(",\n", columns) + ";\n" +
            """
IF EXISTS(SELECT 1 FROM @columns e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.C
WHERE c.column_id IS NULL OR TYPE_NAME(c.system_type_id)<>e.TypeName OR c.is_nullable<>e.Nullable OR (e.MaxLength<>0 AND c.max_length<>e.MaxLength)
OR (e.TypeName='decimal' AND (c.precision<>18 OR c.scale<>2)) OR (e.TypeName='datetime2' AND c.scale<>7) OR (e.C='Id' AND c.is_identity<>1))
    THROW 51065,'Incompatible closure columns. No automatic repair.',1;
IF EXISTS(SELECT 1 FROM sys.columns c JOIN @columns e ON c.object_id=OBJECT_ID(N'dbo.'+e.T) GROUP BY c.object_id,c.name HAVING MAX(CASE WHEN c.name=e.C THEN 1 ELSE 0 END)=0)
    THROW 51066,'Unexpected closure columns.',1;
""" + "\nDECLARE @indexes TABLE(T sysname,N sysname,U bit,F nvarchar(max),Cols nvarchar(max));\nINSERT @indexes VALUES\n" + string.Join(",\n", indexes) + ";\n" +
            """
IF EXISTS(SELECT 1 FROM @indexes e LEFT JOIN sys.indexes i ON i.object_id=OBJECT_ID(N'dbo.'+e.T) AND i.name=e.N
WHERE i.index_id IS NULL OR i.is_unique<>e.U OR i.is_disabled=1 OR REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(i.filter_definition,''),' ',''),'(',''),')',''),'[',''),']','')<>e.F
OR e.Cols<>(SELECT STRING_AGG(CONVERT(nvarchar(max),c.name),',') WITHIN GROUP(ORDER BY ic.key_ordinal) FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0))
    THROW 51067,'Missing or changed closure indexes.',1;
""" + "\nDECLARE @checks TABLE(T sysname,N sysname,D nvarchar(max));\nINSERT @checks VALUES\n" + string.Join(",\n", constraints) + ";\n" +
            """
IF EXISTS(SELECT 1 FROM @checks e LEFT JOIN sys.check_constraints c ON c.parent_object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.N
WHERE c.object_id IS NULL OR c.is_disabled=1 OR c.is_not_trusted=1 OR REPLACE(c.definition,' ','')<>REPLACE(e.D,' ',''))
    THROW 51068,'Missing or changed closure check constraints.',1;
""" + "\nDECLARE @fks TABLE(T sysname,N sysname,P sysname,Cols nvarchar(max),PCols nvarchar(max));\nINSERT @fks VALUES\n" + string.Join(",\n", fks) + ";\n" +
            """
IF EXISTS(SELECT 1 FROM @fks e LEFT JOIN sys.foreign_keys f ON f.parent_object_id=OBJECT_ID(N'dbo.'+e.T) AND f.name=e.N
WHERE f.object_id IS NULL OR f.is_disabled=1 OR f.is_not_trusted=1 OR f.delete_referential_action<>0 OR f.update_referential_action<>0 OR f.referenced_object_id<>OBJECT_ID(N'dbo.'+e.P)
OR e.Cols<>(SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(fc.parent_object_id,fc.parent_column_id)),',') WITHIN GROUP(ORDER BY fc.constraint_column_id) FROM sys.foreign_key_columns fc WHERE fc.constraint_object_id=f.object_id)
OR e.PCols<>(SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(fc.referenced_object_id,fc.referenced_column_id)),',') WITHIN GROUP(ORDER BY fc.constraint_column_id) FROM sys.foreign_key_columns fc WHERE fc.constraint_object_id=f.object_id))
    THROW 51069,'Missing or changed closure foreign keys.',1;
COMMIT;
PRINT 'Contract closure schema verified. Existing business rows unchanged.';
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;
""";
    }

    [SqlServerFact]
    public async Task Sql_concurrent_proposals_and_completion_are_atomic_and_preserve_financial_history()
    {
        var options = Options("_Closure"); await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync(); var clock = new ContractClosureTests.Clock(); await ContractClosureTests.Seed(db, clock);
        var export = Environment.GetEnvironmentVariable("ROOM_RENTAL_CLOSURE_SQL_EXPORT");
        if (!string.IsNullOrWhiteSpace(export)) await File.WriteAllTextAsync(export, UpgradeScript(db));
        var dto = new ClosureCommandDto { IdempotencyKey = Guid.NewGuid().ToString(), Note = "Đồng ý kết thúc QA", EffectiveEndAt = clock.Now };
        async Task<int> Propose(ClosureCommandDto command)
        {
            await using var context = new ApplicationDbContext(options);
            try { await ContractClosureTests.Service(context, clock).ExecuteAsync(2, 1, "request-end", command); return 200; }
            catch (BusinessRuleException e) { return e.StatusCode; }
        }
        var other = new ClosureCommandDto { IdempotencyKey = Guid.NewGuid().ToString(), Note = dto.Note, EffectiveEndAt = dto.EffectiveEndAt };
        var outcomes = await Task.WhenAll(Propose(dto), Propose(other));
        Assert.Equal(new[] { 200, 409 }, outcomes.Order().ToArray());
        Assert.Equal(200, await Propose(outcomes[0] == 200 ? dto : other));
        db.ChangeTracker.Clear();
        var pendingView = await ContractClosureTests.Service(db, clock).GetAsync(1, 1);
        async Task<int> Agree()
        {
            await using var context = new ApplicationDbContext(options);
            try { await ContractClosureTests.Service(context, clock).ExecuteAsync(1, 1, "agree-end", new() { IdempotencyKey = Guid.NewGuid().ToString(), RowVersion = Convert.ToBase64String(pendingView.Closure!.RowVersion), TargetId = pendingView.EndRequests[0].Id }); return 200; }
            catch (BusinessRuleException e) { return e.StatusCode; }
        }
        Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(Agree(), Agree())).Order().ToArray());
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.ContractOccupants.SingleAsync()).Status); Assert.Equal(RoomStatus.Rented, (await db.Rooms.SingleAsync()).Status);
        await ContractClosureTests.Handover(db, clock); await ContractClosureTests.Plan(db, clock);
        await ContractClosureTests.Run(db, clock, 1, "report-refund", x => x.Reference = "QA-SQL-REFUND-ONLY");
        await ContractClosureTests.Run(db, clock, 2, "confirm-refund");
        var view = await ContractClosureTests.Service(db, clock).GetAsync(2, 1);
        var complete = new ClosureCommandDto { IdempotencyKey = Guid.NewGuid().ToString(), RowVersion = Convert.ToBase64String(view.Closure!.RowVersion), TargetId = view.Settlements[0].Id };
        async Task<int> Complete()
        {
            await using var context = new ApplicationDbContext(options);
            await ContractClosureTests.Service(context, clock).ExecuteAsync(2, 1, "complete-settlement", complete); return 200;
        }
        Assert.All(await Task.WhenAll(Complete(), Complete()), x => Assert.Equal(200, x));
        db.ChangeTracker.Clear(); Assert.Single(await db.ContractClosureEvents.Where(e => e.EventType == "complete-settlement").ToListAsync());
        await ContractClosureTests.Run(db, clock, 1, "release");
        Assert.Equal(RoomStatus.Available, (await db.Rooms.SingleAsync()).Status); Assert.Equal(2, (await db.Deposits.SingleAsync()).Status);
        Assert.Equal("closure-original", (await db.PaymentTransactions.SingleAsync()).TransactionCode);
        Assert.Equal(8, (await db.ContractClosures.SingleAsync()).RowVersion.Length);
    }

    [SqlServerFact]
    public async Task Sql09_upgrade_from_prior_schema_is_additive_idempotent_and_detects_changed_index()
    {
        var options = Options("_ClosureUpgrade"); await using var db = new ApplicationDbContext(options);
        // Create only the previous schema, without dropping anything from any existing database.
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString());
        await using (var master = new Microsoft.Data.SqlClient.SqlConnection(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(b.ConnectionString) { InitialCatalog = "master" }.ConnectionString))
        {
            await master.OpenAsync(); await using var cmd = master.CreateCommand();
            cmd.CommandText = $"IF DB_ID(N'{b.InitialCatalog}') IS NOT NULL THROW 51090,'QA database must be new.',1; CREATE DATABASE [{b.InitialCatalog}];";
            await cmd.ExecuteNonQueryAsync();
        }
        foreach (var batch in Regex.Split(db.Database.GenerateCreateScript(), @"^GO\s*$", RegexOptions.Multiline).Where(s => !string.IsNullOrWhiteSpace(s) && !IsClosureBatch(s)))
            await db.Database.ExecuteSqlRawAsync(batch);
        var clock = new ContractClosureTests.Clock(); await ContractClosureTests.Seed(db, clock);
        Assert.Equal(503, (await Assert.ThrowsAsync<BusinessRuleException>(() => ContractClosureTests.Service(db, clock).GetAsync(1, 1))).StatusCode);
        var before = await ContractReadSafetyTests.SnapshotAsync(db); var script = UpgradeScript(db);
        await db.Database.ExecuteSqlRawAsync(script); await db.Database.ExecuteSqlRawAsync(script);
        Assert.Equal(before, await ContractReadSafetyTests.SnapshotAsync(db));
        Assert.Equal(5, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('HoSoKetThucHopDong','YeuCauKetThucHopDong','PhuongAnTatToanHopDong','KhoanMucTatToanHopDong','LichSuKetThucHopDong')").SingleAsync());
        // QA-only schema tampering proves validation inspects columns/filter, not only index names.
        await db.Database.ExecuteSqlRawAsync("DROP INDEX IX_YeuCauKetThucHopDong_ClosureId ON dbo.YeuCauKetThucHopDong; CREATE UNIQUE INDEX IX_YeuCauKetThucHopDong_ClosureId ON dbo.YeuCauKetThucHopDong(ClosureId) WHERE Status=0;");
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
        Assert.Equal(before, await ContractReadSafetyTests.SnapshotAsync(db));
    }
}
