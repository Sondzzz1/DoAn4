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
public class SavedPaymentMethodSchemaSqlTests
{
    [SqlServerFact]
    public async Task Saved_method_upgrade_matches_model_repeats_and_preserves_financial_history()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ROOM_RENTAL_TEST_SQL"));
        Assert.StartsWith("RoomRental_Verification_", builder.InitialCatalog);
        var databaseName = builder.InitialCatalog[..Math.Min(85, builder.InitialCatalog.Length)] + "_SavedSchema_" + Guid.NewGuid().ToString("N")[..12];
        builder.InitialCatalog = databaseName;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options);
        var created = await db.Database.EnsureCreatedAsync();
        Assert.True(created, "Only a freshly created, uniquely named QA fixture may be changed or deleted.");
        try
        {
            await RoommateTests.SeedAsync(db);
            var deposit = new Deposit { RentalRequestId = 1, TenantAccountId = 2, LandlordAccountId = 1, Amount = 1_000_000,
                Status = DepositStatus.Paid, PaidAt = DateTime.Now, DueAt = DateTime.Now.AddDays(1) };
            var bill = new MonthlyBill { ContractId = 1, Month = DateTime.Today.Month, Year = DateTime.Today.Year, TotalAmount = 3_000_000,
                RoomPrice = 3_000_000, DueDate = DateTime.Now.AddDays(7), Status = MonthlyBillStatus.Unpaid };
            db.AddRange(deposit, bill);
            await db.SaveChangesAsync();
            db.PaymentTransactions.Add(new() { DepositId = deposit.Id, TargetType = "Deposit", OrderId = "PRIOR-SAVED-SCHEMA",
                Amount = deposit.Amount, Status = PaymentTransactionStatus.Succeeded, TransactionCode = "PRIOR-SAVED-PAYMENT", ProcessedAt = deposit.PaidAt });
            await db.SaveChangesAsync();

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            var beforeRows = await BusinessRowsAsync(connection);
            var expectedMetadata = await MetadataAsync(connection);
            await ExecuteAsync(connection, """
                ALTER TABLE dbo.GiaoDichThanhToan DROP CONSTRAINT FK_GiaoDichThanhToan_PhuongThucThanhToanDaLuu_SavedPaymentMethodId;
                DROP INDEX IX_GiaoDichThanhToan_SavedPaymentMethodId ON dbo.GiaoDichThanhToan;
                ALTER TABLE dbo.GiaoDichThanhToan DROP CONSTRAINT CK_Payment_Provider;
                DECLARE @default sysname = (SELECT d.name FROM sys.default_constraints d JOIN sys.columns c
                    ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
                    WHERE d.parent_object_id=OBJECT_ID(N'dbo.GiaoDichThanhToan') AND c.name=N'Provider');
                EXEC(N'ALTER TABLE dbo.GiaoDichThanhToan DROP CONSTRAINT '+QUOTENAME(@default));
                ALTER TABLE dbo.GiaoDichThanhToan DROP COLUMN SavedPaymentMethodId,Provider;
                DROP TABLE dbo.LienKetTheVNPay;
                DROP TABLE dbo.PhuongThucThanhToanDaLuu;
                """);
            var script = File.ReadAllText(FindUpgrade());
            await ExecuteAsync(connection, script);
            Assert.Equal(expectedMetadata, await MetadataAsync(connection));
            Assert.Equal(beforeRows, await BusinessRowsAsync(connection));
            await ExecuteAsync(connection, script);
            Assert.Equal(expectedMetadata, await MetadataAsync(connection));
            Assert.Equal(beforeRows, await BusinessRowsAsync(connection));
            Assert.Equal(1, await ScalarIntAsync(connection, "SELECT COUNT(*) FROM dbo.GiaoDichThanhToan WHERE Provider=N'VNPay' AND SavedPaymentMethodId IS NULL;"));

            await CheckFailureAsync(connection, "UPDATE dbo.GiaoDichThanhToan SET Provider=N'VNPayToken';");
            await CheckFailureAsync(connection, "UPDATE dbo.GiaoDichThanhToan SET Provider=N'Other';");
            await CheckFailureAsync(connection, "UPDATE dbo.GiaoDichThanhToan SET Provider=N'VNPayToken',SavedPaymentMethodId=2147483000;");
            await CheckFailureAsync(connection, MethodInsert("B", "NULL"));
            await CheckFailureAsync(connection, MethodInsert("C", "N'ciphertext-test'", "03"));
            await ExecuteAsync(connection, MethodInsert("A", "N'ciphertext-test'"));
            await UniqueFailureAsync(connection, MethodInsert("A", "N'different-ciphertext-test'"));
            var methodId = await ScalarIntAsync(connection, "SELECT Id FROM dbo.PhuongThucThanhToanDaLuu;");
            await CheckFailureAsync(connection, $"UPDATE dbo.GiaoDichThanhToan SET SavedPaymentMethodId={methodId};");
            await ExecuteAsync(connection, $"UPDATE dbo.GiaoDichThanhToan SET Provider=N'VNPayToken',SavedPaymentMethodId={methodId};");
            await CheckFailureAsync(connection, "DELETE dbo.PhuongThucThanhToanDaLuu;");
            await CheckFailureAsync(connection, """
                INSERT dbo.LienKetTheVNPay(UserId,Reference,MerchantCode,CardType,Status,CreatedAtUtc,ExpiresAtUtc,CompletedAtUtc,CallbackFingerprint)
                VALUES(2,N'INVALIDCOMPLETED',N'TOKEN001','01',1,SYSUTCDATETIME(),DATEADD(minute,15,SYSUTCDATETIME()),SYSUTCDATETIME(),REPLICATE('A',64));
                """);
            const string pendingLink = """
                INSERT dbo.LienKetTheVNPay(UserId,Reference,MerchantCode,CardType,Status,CreatedAtUtc,ExpiresAtUtc)
                VALUES(2,N'UNIQUELINKREFERENCE',N'TOKEN001','01',0,SYSUTCDATETIME(),DATEADD(minute,15,SYSUTCDATETIME()));
                """;
            await ExecuteAsync(connection, pendingLink);
            await UniqueFailureAsync(connection, pendingLink);
            Assert.Equal(beforeRows, await BusinessRowsAsync(connection));
        }
        finally
        {
            Assert.True(created);
            Assert.StartsWith("RoomRental_Verification_", databaseName);
            Assert.Contains("_SavedSchema_", databaseName);
            Assert.Equal(databaseName, new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog);
            await db.Database.CloseConnectionAsync();
            SqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static string MethodInsert(string fingerprint, string ciphertext, string type = "01") => $"""
        INSERT dbo.PhuongThucThanhToanDaLuu(UserId,MerchantCode,TokenCiphertext,TokenFingerprint,BankCode,MaskedCardNumber,CardType)
        VALUES(2,N'TOKEN001',{ciphertext},REPLICATE('{fingerprint}',64),N'NCB',N'•••• 1234','{type}');
        """;

    private static async Task CheckFailureAsync(SqlConnection c, string sql) => Assert.Equal(547, (await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(c, sql))).Number);
    private static async Task UniqueFailureAsync(SqlConnection c, string sql) => Assert.Contains((await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(c, sql))).Number, new[] { 2601, 2627 });
    private static async Task ExecuteAsync(SqlConnection c, string sql)
    {
        await using var command = c.CreateCommand(); command.CommandText = sql; command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<int> ScalarIntAsync(SqlConnection c, string sql)
    {
        await using var command = c.CreateCommand(); command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
    private static string Canonical(string sql) => Regex.Replace(sql, @"[\s\[\]()]", "").ToUpperInvariant();
    private static async Task<string> QueryJsonAsync(SqlConnection c, string sql)
    {
        await using var command = c.CreateCommand(); command.CommandText = sql; command.CommandTimeout = 60;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<object?[]>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount]; reader.GetValues(values);
            rows.Add(values.Select(v => v is DBNull ? null : v is string s ? Canonical(s) : v).ToArray());
        }
        return JsonSerializer.Serialize(rows);
    }
    private static async Task<string> MetadataAsync(SqlConnection c)
    {
        const string tables = "OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu'),OBJECT_ID(N'dbo.LienKetTheVNPay'),OBJECT_ID(N'dbo.GiaoDichThanhToan')";
        return string.Join("\n", new[]
        {
            await QueryJsonAsync(c, $"""
                SELECT OBJECT_NAME(c.object_id),c.name,TYPE_NAME(c.system_type_id),c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,ISNULL(d.definition,'')
                FROM sys.columns c LEFT JOIN sys.default_constraints d ON d.object_id=c.default_object_id
                WHERE c.object_id IN({tables}) ORDER BY OBJECT_NAME(c.object_id),c.name;
                """),
            await QueryJsonAsync(c, $"""
                SELECT OBJECT_NAME(i.object_id),i.name,i.type,i.is_unique,i.is_disabled,ISNULL(i.filter_definition,''),
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),c.name),',') WITHIN GROUP(ORDER BY ic.key_ordinal)
                     FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                     WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0)
                FROM sys.indexes i WHERE i.object_id IN({tables}) AND i.index_id>0 ORDER BY OBJECT_NAME(i.object_id),i.name;
                """),
            await QueryJsonAsync(c, $"SELECT OBJECT_NAME(parent_object_id),name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id IN({tables}) ORDER BY OBJECT_NAME(parent_object_id),name;"),
            await QueryJsonAsync(c, $"""
                SELECT OBJECT_NAME(f.parent_object_id),f.name,OBJECT_NAME(f.referenced_object_id),f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted,
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(x.parent_object_id,x.parent_column_id)),',') WITHIN GROUP(ORDER BY x.constraint_column_id) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=f.object_id),
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(x.referenced_object_id,x.referenced_column_id)),',') WITHIN GROUP(ORDER BY x.constraint_column_id) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=f.object_id)
                FROM sys.foreign_keys f WHERE f.parent_object_id IN({tables}) ORDER BY OBJECT_NAME(f.parent_object_id),f.name;
                """)
        });
    }
    private static async Task<string> BusinessRowsAsync(SqlConnection c)
    {
        var tables = new List<(string Schema, string Table, string Keys, string Columns)>();
        await using (var command = c.CreateCommand())
        {
            command.CommandText = """
                SELECT s.name,t.name,
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),',') WITHIN GROUP(ORDER BY ic.key_ordinal)
                     FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
                     JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=t.object_id AND i.is_primary_key=1),
                    (SELECT STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),',') WITHIN GROUP(ORDER BY c.column_id)
                     FROM sys.columns c WHERE c.object_id=t.object_id AND NOT(t.name=N'GiaoDichThanhToan' AND c.name IN(N'Provider',N'SavedPaymentMethodId')))
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                WHERE t.name NOT IN(N'PhuongThucThanhToanDaLuu',N'LienKetTheVNPay') AND t.is_ms_shipped=0 ORDER BY s.name,t.name;
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }
        var snapshots = new SortedDictionary<string, string>();
        foreach (var table in tables)
        {
            static string Quote(string s) => "[" + s.Replace("]", "]]") + "]";
            await using var command = c.CreateCommand();
            command.CommandText = $"SELECT (SELECT {table.Columns} FROM {Quote(table.Schema)}.{Quote(table.Table)} ORDER BY {table.Keys} FOR JSON PATH,INCLUDE_NULL_VALUES);";
            var value = await command.ExecuteScalarAsync();
            snapshots[table.Schema + "." + table.Table] = value is null or DBNull ? "[]" : (string)value;
        }
        return JsonSerializer.Serialize(snapshots);
    }
    private static string FindUpgrade()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
                foreach (var relative in new[] { "RoomRental.BackEnd/SQL/12_SavedPaymentMethods.sql", "backend/RoomRental.BackEnd/SQL/12_SavedPaymentMethods.sql", "SQL/12_SavedPaymentMethods.sql" })
                {
                    var path = Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(path)) return path;
                }
        throw new FileNotFoundException("Cannot find source SQL/12_SavedPaymentMethods.sql.");
    }
}
