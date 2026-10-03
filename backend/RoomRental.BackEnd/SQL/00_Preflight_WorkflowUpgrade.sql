/*
Read-only preflight for RoomRentalDB workflow upgrade.
Run this first in SSMS. It does not alter data or schema.
*/
USE [RoomRentalDB];
GO

SET NOCOUNT ON;

SELECT DB_NAME() AS CurrentDatabase, @@SERVERNAME AS ServerName, SUSER_SNAME() AS LoginName;

;WITH RequiredTables AS (
    SELECT N'TinDang' AS TableName UNION ALL
    SELECT N'PhongTro' UNION ALL
    SELECT N'YeuCauThuePhong' UNION ALL
    SELECT N'DatCoc' UNION ALL
    SELECT N'HopDongThue' UNION ALL
    SELECT N'HoaDonHangThang' UNION ALL
    SELECT N'GiaoDichThanhToan'
)
SELECT
    required.TableName,
    CASE WHEN OBJECT_ID(N'dbo.' + required.TableName, N'U') IS NULL THEN N'MISSING' ELSE N'EXISTS' END AS Status
FROM RequiredTables required
ORDER BY required.TableName;

SELECT
    CASE
        WHEN OBJECT_ID(N'dbo.HopDongThue', N'U') IS NULL THEN N'No contract table yet'
        WHEN COL_LENGTH(N'dbo.HopDongThue', N'RentalRequestId') IS NULL THEN N'Legacy contract table: deployment will rename it to HopDongThue_Legacy'
        ELSE N'Workflow contract table already exists'
    END AS ContractTableStatus;

DECLARE @Checks TABLE (CheckName NVARCHAR(100) NOT NULL, ProblemCount INT NULL, Note NVARCHAR(200) NULL);

IF OBJECT_ID(N'dbo.DatCoc', N'U') IS NOT NULL
BEGIN
    INSERT @Checks (CheckName, ProblemCount)
    SELECT N'Duplicate deposits', COUNT(*)
    FROM (
        SELECT RentalRequestId
        FROM dbo.DatCoc
        GROUP BY RentalRequestId
        HAVING COUNT(*) > 1
    ) duplicates;
END
ELSE INSERT @Checks VALUES (N'Duplicate deposits', NULL, N'DatCoc does not exist yet.');

IF OBJECT_ID(N'dbo.YeuCauThuePhong', N'U') IS NOT NULL
BEGIN
    INSERT @Checks (CheckName, ProblemCount)
    SELECT N'Duplicate pending rental requests', COUNT(*)
    FROM (
        SELECT TenantAccountId, PostId
        FROM dbo.YeuCauThuePhong
        WHERE TrangThai = 0
        GROUP BY TenantAccountId, PostId
        HAVING COUNT(*) > 1
    ) duplicates;
END
ELSE INSERT @Checks VALUES (N'Duplicate pending rental requests', NULL, N'YeuCauThuePhong does not exist yet.');

IF OBJECT_ID(N'dbo.DatCoc', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.YeuCauThuePhong', N'U') IS NOT NULL
BEGIN
    INSERT @Checks (CheckName, ProblemCount)
    SELECT N'Orphan deposits', COUNT(*)
    FROM dbo.DatCoc deposit
    LEFT JOIN dbo.YeuCauThuePhong request ON request.Id = deposit.RentalRequestId
    WHERE request.Id IS NULL;
END
ELSE INSERT @Checks VALUES (N'Orphan deposits', NULL, N'Related workflow tables do not both exist yet.');

IF OBJECT_ID(N'dbo.HopDongThue', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HopDongThue', N'RentalRequestId') IS NOT NULL
   AND OBJECT_ID(N'dbo.YeuCauThuePhong', N'U') IS NOT NULL
BEGIN
    INSERT @Checks (CheckName, ProblemCount)
    SELECT N'Orphan contracts', COUNT(*)
    FROM dbo.HopDongThue contract
    LEFT JOIN dbo.YeuCauThuePhong request ON request.Id = contract.RentalRequestId
    LEFT JOIN dbo.TinDang post ON post.Id = contract.PostId
    WHERE request.Id IS NULL OR post.Id IS NULL;
END
ELSE INSERT @Checks VALUES (N'Orphan contracts', NULL, N'Workflow contract table does not exist yet.');

SELECT CheckName, ProblemCount, Note FROM @Checks ORDER BY CheckName;

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable
FROM sys.foreign_keys fk
WHERE fk.name IN (
    N'FK_DatCoc_YeuCauThuePhong_RentalRequestId',
    N'FK_HopDongThue_YeuCauThuePhong_RentalRequestId',
    N'FK_HopDongThue_TinDang_PostId'
)
ORDER BY fk.name;
