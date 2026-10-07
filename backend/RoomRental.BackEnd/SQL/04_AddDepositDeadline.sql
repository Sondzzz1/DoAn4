/*
Adds the deposit payment deadline required by the landlord-approved deposit flow.
Run this once only when 01_Deploy_WorkflowUpgrade.sql was executed before this feature existed.
*/
USE [RoomRentalDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.DatCoc', N'U') IS NULL
        THROW 51200, N'Khong tim thay bang DatCoc. Hay chay 01_Deploy_WorkflowUpgrade.sql truoc.', 1;

    IF COL_LENGTH(N'dbo.DatCoc', N'HanThanhToan') IS NULL
        ALTER TABLE dbo.DatCoc ADD HanThanhToan DATETIME2 NULL;

    /* Existing unpaid deposits receive a new 24-hour grace period from migration time. */
    UPDATE dbo.DatCoc
    SET HanThanhToan = DATEADD(HOUR, 24, SYSDATETIME())
    WHERE TrangThai = 0 AND HanThanhToan IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatCoc_TrangThai_HanThanhToan' AND object_id = OBJECT_ID(N'dbo.DatCoc'))
        CREATE INDEX IX_DatCoc_TrangThai_HanThanhToan ON dbo.DatCoc(TrangThai, HanThanhToan);

    COMMIT TRANSACTION;
    PRINT N'Deposit deadline upgrade completed successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
