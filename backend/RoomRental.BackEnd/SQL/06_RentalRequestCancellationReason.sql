SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.YeuCauThuePhong', N'U') IS NULL
        THROW 51002, 'Run the current schema or workflow upgrades first.', 1;
    IF COL_LENGTH(N'dbo.YeuCauThuePhong', N'LyDoHuy') IS NULL
        ALTER TABLE dbo.YeuCauThuePhong ADD LyDoHuy NVARCHAR(500) NULL;
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;
