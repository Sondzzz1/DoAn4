USE RoomRentalDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Keep the legacy contract table because the old invoice/payment tables still
-- reference it. The new workflow receives its own compatible table below.
IF OBJECT_ID(N'dbo.HopDongThue', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HopDongThue', N'RentalRequestId') IS NULL
BEGIN
    IF OBJECT_ID(N'dbo.HopDongThue_Legacy', N'U') IS NOT NULL
        THROW 51000, N'Không thể nâng cấp: HopDongThue và HopDongThue_Legacy cùng tồn tại.', 1;

    EXEC sys.sp_rename N'dbo.HopDongThue', N'HopDongThue_Legacy';
END;

IF OBJECT_ID(N'dbo.YeuCauThuePhong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.YeuCauThuePhong (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_YeuCauThuePhong PRIMARY KEY,
        PostId INT NOT NULL,
        TenantAccountId INT NOT NULL,
        LandlordAccountId INT NOT NULL,
        GhiChu NVARCHAR(1000) NULL,
        TrangThai INT NOT NULL CONSTRAINT DF_YeuCauThuePhong_TrangThai DEFAULT 0,
        NgayTao DATETIME2 NOT NULL CONSTRAINT DF_YeuCauThuePhong_NgayTao DEFAULT SYSDATETIME(),
        NgayCapNhat DATETIME2 NULL,
        CONSTRAINT FK_YeuCauThuePhong_TinDang_PostId
            FOREIGN KEY (PostId) REFERENCES dbo.TinDang(Id)
    );
END;

IF OBJECT_ID(N'dbo.DatCoc', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatCoc (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatCoc PRIMARY KEY,
        RentalRequestId INT NOT NULL,
        TenantAccountId INT NOT NULL,
        LandlordAccountId INT NOT NULL,
        SoTien DECIMAL(18,2) NOT NULL,
        TrangThai INT NOT NULL CONSTRAINT DF_DatCoc_TrangThai DEFAULT 0,
        NgayThanhToan DATETIME2 NULL,
        NgayTao DATETIME2 NOT NULL CONSTRAINT DF_DatCoc_NgayTao DEFAULT SYSDATETIME(),
        NgayCapNhat DATETIME2 NULL
    );
END;

IF OBJECT_ID(N'dbo.HopDongThue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HopDongThue (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HopDongThue_Workflow PRIMARY KEY,
        RentalRequestId INT NOT NULL,
        PostId INT NOT NULL,
        TenantAccountId INT NOT NULL,
        LandlordAccountId INT NOT NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        TienThueHangThang DECIMAL(18,2) NOT NULL,
        TienDatCoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_TienDatCoc DEFAULT 0,
        GiaDien DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_GiaDien DEFAULT 0,
        GiaNuoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_GiaNuoc DEFAULT 0,
        PhiDichVu DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_PhiDichVu DEFAULT 0,
        DieuKhoan NVARCHAR(4000) NULL,
        TrangThai INT NOT NULL CONSTRAINT DF_HopDongThue_Workflow_TrangThai DEFAULT 0,
        NguoiThueDaXacNhan BIT NOT NULL CONSTRAINT DF_HopDongThue_NguoiThueDaXacNhan DEFAULT 0,
        ChuTroDaXacNhan BIT NOT NULL CONSTRAINT DF_HopDongThue_ChuTroDaXacNhan DEFAULT 0,
        NgayTao DATETIME2 NOT NULL CONSTRAINT DF_HopDongThue_Workflow_NgayTao DEFAULT SYSDATETIME(),
        NgayCapNhat DATETIME2 NULL,
        CONSTRAINT FK_HopDongThue_Workflow_TinDang_PostId
            FOREIGN KEY (PostId) REFERENCES dbo.TinDang(Id)
    );
END;

IF OBJECT_ID(N'dbo.HoaDonHangThang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoaDonHangThang (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HoaDonHangThang PRIMARY KEY,
        HopDongId INT NOT NULL,
        Thang INT NOT NULL,
        Nam INT NOT NULL,
        SoDienCu DECIMAL(18,2) NOT NULL,
        SoDienMoi DECIMAL(18,2) NOT NULL,
        GiaDien DECIMAL(18,2) NOT NULL,
        SoNuocCu DECIMAL(18,2) NOT NULL,
        SoNuocMoi DECIMAL(18,2) NOT NULL,
        GiaNuoc DECIMAL(18,2) NOT NULL,
        TienPhong DECIMAL(18,2) NOT NULL,
        PhiDichVu DECIMAL(18,2) NOT NULL CONSTRAINT DF_HoaDonHangThang_PhiDichVu DEFAULT 0,
        ChiPhiKhac DECIMAL(18,2) NOT NULL CONSTRAINT DF_HoaDonHangThang_ChiPhiKhac DEFAULT 0,
        GhiChuChiPhiKhac NVARCHAR(500) NULL,
        TongTien DECIMAL(18,2) NOT NULL,
        TrangThai INT NOT NULL CONSTRAINT DF_HoaDonHangThang_TrangThai DEFAULT 0,
        HanThanhToan DATETIME2 NULL,
        NgayThanhToan DATETIME2 NULL,
        PhuongThucThanhToan NVARCHAR(100) NULL,
        GhiChu NVARCHAR(1000) NULL,
        NgayTao DATETIME2 NOT NULL CONSTRAINT DF_HoaDonHangThang_NgayTao DEFAULT SYSDATETIME(),
        NgayCapNhat DATETIME2 NULL,
        CONSTRAINT FK_HoaDonHangThang_HopDongThue_HopDongId
            FOREIGN KEY (HopDongId) REFERENCES dbo.HopDongThue(Id)
    );
END;

IF OBJECT_ID(N'dbo.SuCo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuCo (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuCo PRIMARY KEY,
        ContractId INT NOT NULL,
        ReporterAccountId INT NOT NULL,
        TieuDe NVARCHAR(200) NOT NULL,
        MoTa NVARCHAR(2000) NOT NULL,
        TrangThai INT NOT NULL CONSTRAINT DF_SuCo_TrangThai DEFAULT 0,
        HuongXuLy NVARCHAR(2000) NULL,
        NgayTao DATETIME2 NOT NULL CONSTRAINT DF_SuCo_NgayTao DEFAULT SYSDATETIME(),
        NgayCapNhat DATETIME2 NULL
    );
END;

IF OBJECT_ID(N'dbo.DanhGiaPhong', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DanhGiaPhong (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DanhGiaPhong PRIMARY KEY,
        ContractId INT NOT NULL,
        PostId INT NOT NULL,
        TenantAccountId INT NOT NULL,
        SoSao INT NOT NULL,
        NhanXet NVARCHAR(2000) NULL,
        NgayTao DATETIME2 NOT NULL CONSTRAINT DF_DanhGiaPhong_NgayTao DEFAULT SYSDATETIME()
    );
END;

IF EXISTS (SELECT 1 FROM dbo.DatCoc GROUP BY RentalRequestId HAVING COUNT(*) > 1)
    THROW 51001, N'Không thể tạo unique index: đang có yêu cầu thuê có nhiều khoản cọc.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DatCoc_RentalRequestId' AND object_id = OBJECT_ID(N'dbo.DatCoc'))
    CREATE UNIQUE INDEX UX_DatCoc_RentalRequestId ON dbo.DatCoc(RentalRequestId);

IF EXISTS (
    SELECT 1 FROM dbo.YeuCauThuePhong WHERE TrangThai = 0
    GROUP BY TenantAccountId, PostId HAVING COUNT(*) > 1
)
    THROW 51002, N'Không thể tạo filtered unique index: đang có yêu cầu thuê chờ duyệt bị trùng.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_YeuCauThuePhong_Pending' AND object_id = OBJECT_ID(N'dbo.YeuCauThuePhong'))
    CREATE UNIQUE INDEX UX_YeuCauThuePhong_Pending ON dbo.YeuCauThuePhong(TenantAccountId, PostId) WHERE TrangThai = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThuePhong_PostId' AND object_id = OBJECT_ID(N'dbo.YeuCauThuePhong'))
    CREATE INDEX IX_YeuCauThuePhong_PostId ON dbo.YeuCauThuePhong(PostId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HopDongThue_TenantAccountId_LandlordAccountId' AND object_id = OBJECT_ID(N'dbo.HopDongThue'))
    CREATE INDEX IX_HopDongThue_TenantAccountId_LandlordAccountId ON dbo.HopDongThue(TenantAccountId, LandlordAccountId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HopDongThue_PostId' AND object_id = OBJECT_ID(N'dbo.HopDongThue'))
    CREATE INDEX IX_HopDongThue_PostId ON dbo.HopDongThue(PostId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_HoaDonHangThang_HopDong_Thang_Nam' AND object_id = OBJECT_ID(N'dbo.HoaDonHangThang'))
    CREATE UNIQUE INDEX UX_HoaDonHangThang_HopDong_Thang_Nam ON dbo.HoaDonHangThang(HopDongId, Thang, Nam);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SuCo_ContractId' AND object_id = OBJECT_ID(N'dbo.SuCo'))
    CREATE INDEX IX_SuCo_ContractId ON dbo.SuCo(ContractId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DanhGiaPhong_Contract_Tenant' AND object_id = OBJECT_ID(N'dbo.DanhGiaPhong'))
    CREATE UNIQUE INDEX UX_DanhGiaPhong_Contract_Tenant ON dbo.DanhGiaPhong(ContractId, TenantAccountId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGiaPhong_PostId' AND object_id = OBJECT_ID(N'dbo.DanhGiaPhong'))
    CREATE INDEX IX_DanhGiaPhong_PostId ON dbo.DanhGiaPhong(PostId);

IF OBJECT_ID(N'dbo.GiaoDichThanhToan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GiaoDichThanhToan (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GiaoDichThanhToan PRIMARY KEY,
        DepositId INT NULL,
        MonthlyBillId INT NULL,
        TargetType NVARCHAR(20) NOT NULL CONSTRAINT DF_GiaoDichThanhToan_TargetType DEFAULT N'Deposit',
        OrderId NVARCHAR(100) NOT NULL,
        TransactionCode NVARCHAR(100) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_GiaoDichThanhToan_Status DEFAULT 0,
        ResponseCode NVARCHAR(20) NULL,
        OrderInfo NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GiaoDichThanhToan_CreatedAt DEFAULT SYSDATETIME(),
        ProcessedAt DATETIME2 NULL,
        CONSTRAINT FK_GiaoDichThanhToan_DatCoc_DepositId FOREIGN KEY (DepositId) REFERENCES dbo.DatCoc(Id),
        CONSTRAINT FK_GiaoDichThanhToan_HoaDonHangThang_MonthlyBillId FOREIGN KEY (MonthlyBillId) REFERENCES dbo.HoaDonHangThang(Id),
        CONSTRAINT CK_GiaoDichThanhToan_ExactlyOneTarget CHECK (
            (DepositId IS NOT NULL AND MonthlyBillId IS NULL AND TargetType = N'Deposit') OR
            (DepositId IS NULL AND MonthlyBillId IS NOT NULL AND TargetType = N'MonthlyBill')
        )
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_GiaoDichThanhToan_OrderId' AND object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan'))
    CREATE UNIQUE INDEX UX_GiaoDichThanhToan_OrderId ON dbo.GiaoDichThanhToan(OrderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_GiaoDichThanhToan_TransactionCode' AND object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan'))
    CREATE UNIQUE INDEX UX_GiaoDichThanhToan_TransactionCode ON dbo.GiaoDichThanhToan(TransactionCode) WHERE TransactionCode IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GiaoDichThanhToan_DepositId' AND object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan'))
    CREATE INDEX IX_GiaoDichThanhToan_DepositId ON dbo.GiaoDichThanhToan(DepositId);

-- Phase 3: price snapshots and monthly-bill payments. Every operation is
-- additive/idempotent so this same script upgrades an existing Phase 2 DB.
IF COL_LENGTH(N'dbo.HopDongThue', N'TienDatCoc') IS NULL
    ALTER TABLE dbo.HopDongThue ADD TienDatCoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_TienDatCoc DEFAULT 0;
IF COL_LENGTH(N'dbo.HopDongThue', N'GiaDien') IS NULL
    ALTER TABLE dbo.HopDongThue ADD GiaDien DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_GiaDien DEFAULT 0;
IF COL_LENGTH(N'dbo.HopDongThue', N'GiaNuoc') IS NULL
    ALTER TABLE dbo.HopDongThue ADD GiaNuoc DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_GiaNuoc DEFAULT 0;
IF COL_LENGTH(N'dbo.HopDongThue', N'PhiDichVu') IS NULL
    ALTER TABLE dbo.HopDongThue ADD PhiDichVu DECIMAL(18,2) NOT NULL CONSTRAINT DF_HopDongThue_PhiDichVu DEFAULT 0;
IF COL_LENGTH(N'dbo.HopDongThue', N'DieuKhoan') IS NULL
    ALTER TABLE dbo.HopDongThue ADD DieuKhoan NVARCHAR(4000) NULL;

GO

UPDATE hd
SET TienDatCoc = dc.SoTien,
    GiaDien = COALESCE(pt.TienDien, 0),
    GiaNuoc = COALESCE(pt.TienNuoc, 0),
    PhiDichVu = COALESCE(pt.PhiDichVu, 0)
FROM dbo.HopDongThue hd
JOIN dbo.YeuCauThuePhong yc ON yc.Id = hd.RentalRequestId
JOIN dbo.TinDang td ON td.Id = hd.PostId
JOIN dbo.PhongTro pt ON pt.Id = td.PhongTroId
LEFT JOIN dbo.DatCoc dc ON dc.RentalRequestId = yc.Id
WHERE hd.TienDatCoc = 0 AND hd.GiaDien = 0 AND hd.GiaNuoc = 0 AND hd.PhiDichVu = 0;

IF COL_LENGTH(N'dbo.HoaDonHangThang', N'PhiDichVu') IS NULL
    ALTER TABLE dbo.HoaDonHangThang ADD PhiDichVu DECIMAL(18,2) NOT NULL CONSTRAINT DF_HoaDonHangThang_PhiDichVu DEFAULT 0;

IF COL_LENGTH(N'dbo.DanhGiaPhong', N'NgayCapNhat') IS NULL
    ALTER TABLE dbo.DanhGiaPhong ADD NgayCapNhat DATETIME2 NULL;

IF COL_LENGTH(N'dbo.GiaoDichThanhToan', N'MonthlyBillId') IS NULL
    ALTER TABLE dbo.GiaoDichThanhToan ADD MonthlyBillId INT NULL;
IF COL_LENGTH(N'dbo.GiaoDichThanhToan', N'TargetType') IS NULL
    ALTER TABLE dbo.GiaoDichThanhToan ADD TargetType NVARCHAR(20) NOT NULL CONSTRAINT DF_GiaoDichThanhToan_TargetType DEFAULT N'Deposit';
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan') AND name = N'DepositId' AND is_nullable = 0
)
    ALTER TABLE dbo.GiaoDichThanhToan ALTER COLUMN DepositId INT NULL;

GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GiaoDichThanhToan_HoaDonHangThang_MonthlyBillId')
    ALTER TABLE dbo.GiaoDichThanhToan ADD CONSTRAINT FK_GiaoDichThanhToan_HoaDonHangThang_MonthlyBillId
        FOREIGN KEY (MonthlyBillId) REFERENCES dbo.HoaDonHangThang(Id);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_GiaoDichThanhToan_ExactlyOneTarget')
    ALTER TABLE dbo.GiaoDichThanhToan ADD CONSTRAINT CK_GiaoDichThanhToan_ExactlyOneTarget CHECK (
        (DepositId IS NOT NULL AND MonthlyBillId IS NULL AND TargetType = N'Deposit') OR
        (DepositId IS NULL AND MonthlyBillId IS NOT NULL AND TargetType = N'MonthlyBill')
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GiaoDichThanhToan_MonthlyBillId' AND object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan'))
    CREATE INDEX IX_GiaoDichThanhToan_MonthlyBillId ON dbo.GiaoDichThanhToan(MonthlyBillId);

IF EXISTS (
    SELECT 1 FROM dbo.BaoCaoTinDang WHERE TrangThai = 0
    GROUP BY NguoiBaoCaoId, TinDangId HAVING COUNT(*) > 1
)
    THROW 51003, N'Không thể tạo unique index: đang có báo cáo Pending trùng người báo cáo và tin đăng.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BaoCaoTinDang_Pending' AND object_id = OBJECT_ID(N'dbo.BaoCaoTinDang'))
    CREATE UNIQUE INDEX UX_BaoCaoTinDang_Pending ON dbo.BaoCaoTinDang(NguoiBaoCaoId, TinDangId) WHERE TrangThai = 0;

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_HoaDonHangThang_HopDongThue_HopDongId' AND delete_referential_action = 1
)
BEGIN
    ALTER TABLE dbo.HoaDonHangThang DROP CONSTRAINT FK_HoaDonHangThang_HopDongThue_HopDongId;
    ALTER TABLE dbo.HoaDonHangThang ADD CONSTRAINT FK_HoaDonHangThang_HopDongThue_HopDongId
        FOREIGN KEY (HopDongId) REFERENCES dbo.HopDongThue(Id);
END;

COMMIT TRANSACTION;
GO
