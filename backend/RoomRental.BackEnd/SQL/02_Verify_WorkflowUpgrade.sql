/* Read-only verification after 01_Deploy_WorkflowUpgrade.sql. */
USE [RoomRentalDB];
GO

SET NOCOUNT ON;

SELECT name AS TableName
FROM sys.tables
WHERE name IN (N'YeuCauThuePhong', N'DatCoc', N'HopDongThue', N'HoaDonHangThang', N'SuCo', N'DanhGiaPhong', N'GiaoDichThanhToan')
ORDER BY name;

SELECT name AS ForeignKeyName
FROM sys.foreign_keys
WHERE name IN (
    N'FK_DatCoc_YeuCauThuePhong_RentalRequestId',
    N'FK_HopDongThue_YeuCauThuePhong_RentalRequestId',
    N'FK_HopDongThue_TinDang_PostId',
    N'FK_GiaoDichThanhToan_HoaDonHangThang_MonthlyBillId'
)
ORDER BY name;

SELECT name AS IndexName
FROM sys.indexes
WHERE name IN (
    N'UX_DatCoc_RentalRequestId', N'UX_YeuCauThuePhong_Pending',
    N'UX_HoaDonHangThang_HopDong_Thang_Nam', N'UX_GiaoDichThanhToan_OrderId',
    N'UX_GiaoDichThanhToan_TransactionCode'
)
ORDER BY name;

SELECT N'Orphan deposits' AS CheckName, COUNT(*) AS ProblemCount
FROM dbo.DatCoc deposit LEFT JOIN dbo.YeuCauThuePhong request ON request.Id = deposit.RentalRequestId
WHERE request.Id IS NULL
UNION ALL
SELECT N'Orphan contracts', COUNT(*)
FROM dbo.HopDongThue contract
LEFT JOIN dbo.YeuCauThuePhong request ON request.Id = contract.RentalRequestId
LEFT JOIN dbo.TinDang post ON post.Id = contract.PostId
WHERE request.Id IS NULL OR post.Id IS NULL;
