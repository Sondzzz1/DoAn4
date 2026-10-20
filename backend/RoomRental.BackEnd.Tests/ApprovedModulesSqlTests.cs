using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RoomRental.BackEnd.DAL;
using Xunit;

namespace RoomRental.BackEnd.Tests;

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL")))
            Skip = "Set ROOM_RENTAL_TEST_SQL to an isolated RoomRental_Verification_* database.";
    }
}

[Collection("SqlServerWorkflow")]
public class ApprovedModulesSqlTests
{
    private static readonly string[] RoommateTables = ["TinTimNguoiOGhep", "YeuCauOGhep", "NguoiOCungHopDong"];
    private static readonly string[] PaymentTables = ["SuKienThanhToan", "YeuCauHoanCoc"];
    private static readonly string[] NewTables = [.. RoommateTables, .. PaymentTables, .. ContractClosureSqlTests.Tables];
    private static string SqlDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "RoomRental.BackEnd", "SQL");
                if (Directory.Exists(path)) return path;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Cannot locate the source SQL directory.");
        }
    }
    private static string Read(string name) => File.ReadAllText(Path.Combine(SqlDirectory, name));

    private static async Task<ApplicationDbContext> CreatePreviousSchema(string suffix, int existingModules = 0)
    {
        var b = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", b.InitialCatalog);
        b.InitialCatalog += "_Bundle_" + suffix;
        await using (var master = new SqlConnection(new SqlConnectionStringBuilder(b.ConnectionString) { InitialCatalog = "master" }.ConnectionString))
        {
            await master.OpenAsync(); await using var command = master.CreateCommand();
            command.CommandText = $"IF DB_ID(N'{b.InitialCatalog.Replace("'", "''")}') IS NOT NULL THROW 51190,'QA database must be new.',1; CREATE DATABASE [{b.InitialCatalog.Replace("]", "]]")}];";
            await command.ExecuteNonQueryAsync();
        }
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(b.ConnectionString).Options);
        var excluded = NewTables.Skip(existingModules).ToArray();
        foreach (var batch in Regex.Split(db.Database.GenerateCreateScript(), @"^GO\s*$", RegexOptions.Multiline))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            var match = Regex.Match(batch, @"(?:CREATE TABLE|\bON) \[(?<table>[^\]]+)\]", RegexOptions.IgnoreCase);
            if (match.Success && excluded.Contains(match.Groups["table"].Value)) continue;
            await db.Database.ExecuteSqlRawAsync(batch);
        }
        await ContractClosureTests.Seed(db, new(), occupied: false);
        db.ChangeTracker.Clear();
        return db;
    }

    // Snapshot every existing table, including all financial columns, not only row counts.
    private static async Task<string> Snapshot(ApplicationDbContext db, string[] excluded)
    {
        var model = db.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var snapshots = new SortedDictionary<string, string>();
        foreach (var table in model.Tables.Where(t => !excluded.Contains(t.Name)))
        {
            var keys = string.Join(",", table.PrimaryKey!.Columns.Select(c => $"[{c.Name}]"));
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT * FROM dbo.[{table.Name}] ORDER BY {keys} FOR JSON PATH, INCLUDE_NULL_VALUES;";
            await using var reader = await command.ExecuteReaderAsync();
            var json = new System.Text.StringBuilder();
            while (await reader.ReadAsync()) json.Append(reader.GetString(0));
            snapshots.Add(table.Name, json.ToString());
        }
        return JsonSerializer.Serialize(snapshots);
    }

    private static async Task AssertModelColumns(ApplicationDbContext db)
    {
        var model = db.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        foreach (var table in model.Tables.Where(t => NewTables.Contains(t.Name)))
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT c.name, TYPE_NAME(c.system_type_id), c.is_nullable, c.max_length, c.precision, c.scale, c.is_identity FROM sys.columns c WHERE c.object_id=OBJECT_ID(@table);";
            command.Parameters.Add(new SqlParameter("@table", "dbo." + table.Name));
            await using var reader = await command.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                var column = Assert.Single(table.Columns, c => c.Name == reader.GetString(0));
                var type = Regex.Match(column.StoreType, @"^\w+").Value;
                Assert.Equal(type == "rowversion" ? "timestamp" : type, reader.GetString(1));
                Assert.Equal(column.IsNullable, reader.GetBoolean(2));
                var length = Regex.Match(column.StoreType, @"\((max|\d+)\)");
                if (length.Success && type is "nvarchar" or "varchar" or "char")
                    Assert.Equal(length.Groups[1].Value == "max" ? -1 : int.Parse(length.Groups[1].Value) * (type == "nvarchar" ? 2 : 1), (int)reader.GetInt16(3));
                if (type == "decimal") { Assert.Equal(18, reader.GetByte(4)); Assert.Equal(2, reader.GetByte(5)); }
                if (type == "datetime2") Assert.Equal(7, reader.GetByte(5));
                if (column.Name == "Id") Assert.True(reader.GetBoolean(6));
                count++;
            }
            Assert.Equal(table.Columns.Count(), count);
        }
    }

    [SqlServerTheory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Bundle_adds_missing_modules_matches_model_and_preserves_all_existing_rows(int existingModules)
    {
        await using var db = await CreatePreviousSchema("Prior" + existingModules, existingModules);
        // Seed opens and retains this QA connection; commands below share its transaction state.
        var before = await Snapshot(db, NewTables.Skip(existingModules).ToArray());
        var script = Read("10_Deploy_ApprovedModules.sql");
        await db.Database.ExecuteSqlRawAsync(script);
        await AssertModelColumns(db);
        Assert.Equal(before, await Snapshot(db, NewTables.Skip(existingModules).ToArray()));
        var deployed = await Snapshot(db, []);
        await db.Database.ExecuteSqlRawAsync(script);
        Assert.Equal(deployed, await Snapshot(db, []));
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT @@TRANCOUNT AS [Value]").SingleAsync());
    }

    [SqlServerFact]
    public async Task Late_failure_rolls_back_new_roommate_and_payment_tables_without_touching_history()
    {
        await using var db = await CreatePreviousSchema("Rollback");
        var before = await Snapshot(db, NewTables);
        // Simulate an interrupted earlier deployment. Never repair or remove its table automatically.
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE dbo.HoSoKetThucHopDong(Id int NOT NULL PRIMARY KEY);");
        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(Read("10_Deploy_ApprovedModules.sql")));
        Assert.Equal(51064, error.Number);
        Assert.Equal(before, await Snapshot(db, NewTables));
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('TinTimNguoiOGhep','SuKienThanhToan','HoSoKetThucHopDong')").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT @@TRANCOUNT AS [Value]").SingleAsync());
    }

    [SqlServerFact]
    public async Task Main_opt_in_and_wrong_database_guards_fail_before_any_schema_changes()
    {
        await using var db = await CreatePreviousSchema("Guards");
        var before = await Snapshot(db, NewTables);
        var script = Read("10_Deploy_ApprovedModules.sql");
        // Exercise the actual main guard against this disposable QA name, never RoomRentalDB.
        var qaName = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog;
        var simulatedMain = script.Replace("N'RoomRentalDB'", $"N'{qaName}'");
        Assert.Equal(51100, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(simulatedMain))).Number);
        var wrongDatabase = script.Replace("N'RoomRental[_]Verification[_]%'", "N'NotThisDatabase%'");
        Assert.Equal(51101, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(wrongDatabase))).Number);
        Assert.Equal(before, await Snapshot(db, NewTables));
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name='TinTimNguoiOGhep'").SingleAsync());
    }
}
