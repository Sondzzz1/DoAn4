SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.TinDang', N'U') IS NULL
    THROW 51000, 'Run the base schema and workflow upgrades first.', 1;
-- Never change historical posts automatically to make the index succeed.
IF EXISTS (
    SELECT PhongTroId FROM dbo.TinDang WITH (UPDLOCK, HOLDLOCK)
    WHERE TrangThai IN (0, 1)
    GROUP BY PhongTroId HAVING COUNT(*) > 1
)
BEGIN
    SELECT Id, PhongTroId, TieuDe, TrangThai FROM dbo.TinDang
    WHERE TrangThai IN (0, 1) AND PhongTroId IN (
        SELECT PhongTroId FROM dbo.TinDang WHERE TrangThai IN (0, 1)
        GROUP BY PhongTroId HAVING COUNT(*) > 1
    );
    THROW 51001, 'Duplicate active posts found. Review and hide/reject duplicates before retrying.', 1;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TinDang') AND name = N'UX_TinDang_Room_Active')
    CREATE UNIQUE INDEX UX_TinDang_Room_Active ON dbo.TinDang(PhongTroId) WHERE TrangThai IN (0, 1);
COMMIT;
