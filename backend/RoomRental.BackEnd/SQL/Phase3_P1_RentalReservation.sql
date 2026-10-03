/*
Phase 3 P1: Rental reservation integrity.

Run this once only after backup and after reviewing the orphan checks below.
This script is intentionally not executed by the application or by this change.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.YeuCauThuePhong', N'U') IS NULL
    THROW 51010, N'Không tìm thấy bảng YeuCauThuePhong.', 1;
IF OBJECT_ID(N'dbo.DatCoc', N'U') IS NULL
    THROW 51011, N'Không tìm thấy bảng DatCoc.', 1;
IF OBJECT_ID(N'dbo.HopDongThue', N'U') IS NULL
    THROW 51012, N'Không tìm thấy bảng HopDongThue.', 1;
IF OBJECT_ID(N'dbo.TinDang', N'U') IS NULL
    THROW 51013, N'Không tìm thấy bảng TinDang.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.DatCoc d
    LEFT JOIN dbo.YeuCauThuePhong y ON y.Id = d.RentalRequestId
    WHERE y.Id IS NULL
)
    THROW 51014, N'Không thể tạo FK DatCoc: tồn tại khoản cọc không có yêu cầu thuê.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.HopDongThue h
    LEFT JOIN dbo.YeuCauThuePhong y ON y.Id = h.RentalRequestId
    LEFT JOIN dbo.TinDang t ON t.Id = h.PostId
    WHERE y.Id IS NULL OR t.Id IS NULL
)
    THROW 51015, N'Không thể tạo FK HopDongThue: tồn tại hợp đồng không có yêu cầu thuê hoặc tin đăng.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DatCoc_YeuCauThuePhong_RentalRequestId')
    ALTER TABLE dbo.DatCoc ADD CONSTRAINT FK_DatCoc_YeuCauThuePhong_RentalRequestId
        FOREIGN KEY (RentalRequestId) REFERENCES dbo.YeuCauThuePhong(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HopDongThue_YeuCauThuePhong_RentalRequestId')
    ALTER TABLE dbo.HopDongThue ADD CONSTRAINT FK_HopDongThue_YeuCauThuePhong_RentalRequestId
        FOREIGN KEY (RentalRequestId) REFERENCES dbo.YeuCauThuePhong(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HopDongThue_TinDang_PostId')
    ALTER TABLE dbo.HopDongThue ADD CONSTRAINT FK_HopDongThue_TinDang_PostId
        FOREIGN KEY (PostId) REFERENCES dbo.TinDang(Id);

COMMIT TRANSACTION;
