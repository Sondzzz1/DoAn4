using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;
using Xunit;

namespace RoomRental.BackEnd.Tests;

[Collection("SqlServerWorkflow")]
public class CashPaymentSchemaSqlTests
{
    [SqlServerFact]
    public async Task Cash_upgrade_matches_EF_schema_is_repeatable_and_preserves_all_existing_rows()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        var databaseName = builder.InitialCatalog[..Math.Min(85, builder.InitialCatalog.Length)] + "_CashSchema_" + Guid.NewGuid().ToString("N")[..12];
        builder.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        var createdByThisTest = await db.Database.EnsureCreatedAsync();
        Assert.True(createdByThisTest, "A schema test must use a freshly created, uniquely named QA database.");
        try
        {
            await RoommateTests.SeedAsync(db);
            var deposit = new Deposit
            {
                RentalRequestId = 1, TenantAccountId = 2, LandlordAccountId = 1,
                Amount = 1_000_000, Status = DepositStatus.Confirmed, DueAt = DateTime.Now.AddDays(1), PaidAt = DateTime.Now
            };
            var bill = new MonthlyBill
            {
                ContractId = 1, Month = DateTime.Today.Month, Year = DateTime.Today.Year,
                TotalAmount = 1_000_000, RoomPrice = 1_000_000, DueDate = DateTime.Now.AddDays(3)
            };
            db.AddRange(deposit, bill);
            await db.SaveChangesAsync();
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            var beforeRows = await ExistingRowsAsync(connection);
            var expectedMetadata = await CashMetadataAsync(connection);
            Assert.Empty(await db.CashPaymentReceipts.ToListAsync());
            // Only the empty cash table in this freshly-created fixture is removed to exercise an upgrade.
            await ExecuteAsync(connection, "DROP TABLE dbo.PhieuThuTienMat;");
            var script = await File.ReadAllTextAsync(FindUpgrade());
            await ExecuteAsync(connection, script);
            Assert.Equal(expectedMetadata, await CashMetadataAsync(connection));
            Assert.Equal(beforeRows, await ExistingRowsAsync(connection));
            await ExecuteAsync(connection, script);
            Assert.Equal(expectedMetadata, await CashMetadataAsync(connection));
            Assert.Equal(beforeRows, await ExistingRowsAsync(connection));

            await AssertCheckFailureAsync(connection, Insert(deposit.Id, null, "1", "NULL", "NULL", "NULL", "NULL"));
            await AssertCheckFailureAsync(connection, Insert(deposit.Id, null, "1", "SYSUTCDATETIME()", "NULL", "N'MISSING-ACTOR'", "NULL"));
            await AssertCheckFailureAsync(connection, Insert(deposit.Id, null, "1", "SYSUTCDATETIME()", "2", "N'WRONG-ACTOR'", "NULL"));
            await AssertCheckFailureAsync(connection, Insert(deposit.Id, bill.Id));
            await AssertCheckFailureAsync(connection, Insert(deposit.Id, null, amount: "0"));
            await AssertCheckFailureAsync(connection, Insert(deposit.Id, null, receipt: "N'NOT-YET-RECEIVED'"));
            await AssertCheckFailureAsync(connection, Insert(deposit.Id, null, status: "2"));

            await ExecuteAsync(connection, Insert(deposit.Id, null));
            await AssertUniqueFailureAsync(connection, Insert(deposit.Id, null));
            await ExecuteAsync(connection, "UPDATE dbo.PhieuThuTienMat SET Status=2,CancelledAtUtc=SYSUTCDATETIME() WHERE Status=0;");
            await ExecuteAsync(connection, Insert(deposit.Id, null, "1", "SYSUTCDATETIME()", "1", "N'TM-SCHEMA-VALID'", "NULL"));
            await AssertUniqueFailureAsync(connection, Insert(deposit.Id, null));
            await AssertUniqueFailureAsync(connection, Insert(null, bill.Id, "1", "SYSUTCDATETIME()", "1", "N'TM-SCHEMA-VALID'", "NULL"));
            await ExecuteAsync(connection, Insert(null, bill.Id));
            await AssertUniqueFailureAsync(connection, Insert(null, bill.Id));
            Assert.Equal(beforeRows, await ExistingRowsAsync(connection));
        }
        finally
        {
            // No connection supplied by the user is ever deleted: only this exact newly-created GUID fixture.
            Assert.True(createdByThisTest);
            Assert.StartsWith("RoomRental_Verification_", databaseName);
            Assert.Contains("_CashSchema_", databaseName);
            Assert.Equal(databaseName, new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog);
            await db.Database.CloseConnectionAsync();
            SqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static string Insert(int? deposit, int? bill, string status = "0", string confirmed = "NULL", string actor = "NULL",
        string receipt = "NULL", string cancelled = "NULL", string amount = "1000000") => $"""
        INSERT dbo.PhieuThuTienMat(DepositId,MonthlyBillId,Amount,TenantAccountId,LandlordAccountId,Status,ConfirmedAtUtc,ConfirmedByAccountId,ReceiptNumber,CancelledAtUtc)
        VALUES ({deposit?.ToString() ?? "NULL"},{bill?.ToString() ?? "NULL"},{amount},2,1,{status},{confirmed},{actor},{receipt},{cancelled});
        """;

    private static async Task AssertCheckFailureAsync(SqlConnection connection, string sql)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection, sql));
        Assert.Equal(547, error.Number);
    }

    private static async Task AssertUniqueFailureAsync(SqlConnection connection, string sql)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection, sql));
        Assert.Contains(error.Number, new[] { 2601, 2627 });
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> QueryJsonAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<object?[]>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values.Select(v => v is DBNull ? null : v is string s ? Canonical(s) : v).ToArray());
        }
        return JsonSerializer.Serialize(rows);
    }

    private static string Canonical(string sql) => Regex.Replace(sql, @"[\s\[\]()]", "").ToUpperInvariant();

    private static async Task<string> CashMetadataAsync(SqlConnection connection)
    {
        var parts = new List<string>
        {
            await QueryJsonAsync(connection, """
                SELECT c.name,TYPE_NAME(c.system_type_id),c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,ISNULL(d.definition,'')
                FROM sys.columns c LEFT JOIN sys.default_constraints d ON d.object_id=c.default_object_id
                WHERE c.object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') ORDER BY c.name;
                """),
            await QueryJsonAsync(connection, """
                SELECT i.name,i.type,i.is_unique,i.is_disabled,ISNULL(i.filter_definition,''),
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),c.name),',') WITHIN GROUP(ORDER BY ic.key_ordinal)
                     FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                     WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0)
                FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND i.index_id>0 ORDER BY i.name;
                """),
            await QueryJsonAsync(connection, """
                SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
                WHERE parent_object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') ORDER BY name;
                """),
            await QueryJsonAsync(connection, """
                SELECT f.name,OBJECT_NAME(f.referenced_object_id),f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted,
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(c.parent_object_id,c.parent_column_id)),',') WITHIN GROUP(ORDER BY c.constraint_column_id) FROM sys.foreign_key_columns c WHERE c.constraint_object_id=f.object_id),
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(c.referenced_object_id,c.referenced_column_id)),',') WITHIN GROUP(ORDER BY c.constraint_column_id) FROM sys.foreign_key_columns c WHERE c.constraint_object_id=f.object_id)
                FROM sys.foreign_keys f WHERE f.parent_object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') ORDER BY f.name;
                """)
        };
        return string.Join("\n", parts);
    }

    private static async Task<string> ExistingRowsAsync(SqlConnection connection)
    {
        var tables = new List<(string Schema, string Table, string Keys)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT s.name,t.name,STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),',') WITHIN GROUP(ORDER BY ic.key_ordinal)
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1
                JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
                JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                WHERE t.name<>N'PhieuThuTienMat' AND t.is_ms_shipped=0
                GROUP BY s.name,t.name ORDER BY s.name,t.name;
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }
        var snapshots = new SortedDictionary<string, string>();
        foreach (var table in tables)
        {
            static string Quote(string name) => "[" + name.Replace("]", "]]") + "]";
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT (SELECT * FROM {Quote(table.Schema)}.{Quote(table.Table)} ORDER BY {table.Keys} FOR JSON PATH,INCLUDE_NULL_VALUES);";
            // The outer scalar keeps long FOR JSON output in one nvarchar(max) value;
            // SQL Server returns NULL for an empty nested JSON result.
            var json = await command.ExecuteScalarAsync();
            snapshots[table.Schema + "." + table.Table] = json is null or DBNull ? "[]" : (string)json;
        }
        return JsonSerializer.Serialize(snapshots);
    }

    private static string FindUpgrade()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var folder = new DirectoryInfo(start); folder != null; folder = folder.Parent)
                foreach (var relative in new[] { "RoomRental.BackEnd/SQL/11_CashPayments.sql", "backend/RoomRental.BackEnd/SQL/11_CashPayments.sql", "SQL/11_CashPayments.sql" })
                {
                    var candidate = Path.Combine(folder.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(candidate)) return candidate;
                }
        throw new FileNotFoundException("Cannot find source SQL/11_CashPayments.sql for the upgrade test.");
    }
}
