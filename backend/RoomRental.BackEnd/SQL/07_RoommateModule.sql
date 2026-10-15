-- Version 07: additive Roommate module. QA only until deployment is reviewed.
-- Requires the existing current schema. No existing rows or financial columns change.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() NOT LIKE N'RoomRental[_]Verification[_]%'
    THROW 51030, 'This review script is restricted to a RoomRental_Verification_* QA database.', 1;
IF OBJECT_ID(N'dbo.HopDongThue',N'U') IS NULL OR OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NULL
    THROW 51031, 'Apply the existing base schema before version 07.', 1;
IF OBJECT_ID(N'dbo.TinTimNguoiOGhep',N'U') IS NOT NULL OR OBJECT_ID(N'dbo.YeuCauOGhep',N'U') IS NOT NULL OR OBJECT_ID(N'dbo.NguoiOCungHopDong',N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.TinTimNguoiOGhep',N'U') IS NULL OR OBJECT_ID(N'dbo.YeuCauOGhep',N'U') IS NULL OR OBJECT_ID(N'dbo.NguoiOCungHopDong',N'U') IS NULL
        THROW 51032, 'Partial Roommate schema detected; inspect before proceeding.', 1;
    PRINT 'Roommate tables already exist; no changes performed.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;
    CREATE TABLE dbo.TinTimNguoiOGhep (
        Id int IDENTITY NOT NULL CONSTRAINT PK_TinTimNguoiOGhep PRIMARY KEY,
        ContractId int NOT NULL,
        CreatorTenantAccountId int NOT NULL,
        TieuDe nvarchar(200) NOT NULL,
        MoTa nvarchar(4000) NOT NULL,
        SoNguoiCanTim int NOT NULL,
        ChiPhiDuKien decimal(18,2) NULL,
        YeuCau nvarchar(2000) NULL,
        LyDoTuChoi nvarchar(500) NULL,
        TrangThai int NOT NULL,
        NgayTao datetime2 NOT NULL,
        NgayCapNhat datetime2 NULL,
        NgayDuyet datetime2 NULL,
        NgayDong datetime2 NULL,
        CONSTRAINT CK_RoommatePost_Slots CHECK(SoNguoiCanTim >= 1),
        CONSTRAINT CK_RoommatePost_Share CHECK(ChiPhiDuKien IS NULL OR ChiPhiDuKien >= 0),
        CONSTRAINT CK_RoommatePost_Status CHECK(TrangThai BETWEEN 0 AND 5),
        CONSTRAINT FK_TinTimNguoiOGhep_HopDongThue_ContractId FOREIGN KEY(ContractId) REFERENCES dbo.HopDongThue(Id),
        CONSTRAINT FK_TinTimNguoiOGhep_TaiKhoan_CreatorTenantAccountId FOREIGN KEY(CreatorTenantAccountId) REFERENCES dbo.TaiKhoan(Id)
    );
    CREATE UNIQUE INDEX UX_RoommatePost_ActiveContract ON dbo.TinTimNguoiOGhep(ContractId) WHERE TrangThai IN(0,1);
    CREATE INDEX IX_TinTimNguoiOGhep_CreatorTenantAccountId ON dbo.TinTimNguoiOGhep(CreatorTenantAccountId);
    CREATE INDEX IX_TinTimNguoiOGhep_TrangThai_NgayTao ON dbo.TinTimNguoiOGhep(TrangThai,NgayTao);
    CREATE TABLE dbo.YeuCauOGhep (
        Id int IDENTITY NOT NULL CONSTRAINT PK_YeuCauOGhep PRIMARY KEY,
        RoommatePostId int NOT NULL,
        ApplicantTenantAccountId int NOT NULL,
        LoiNhan nvarchar(1000) NULL,
        TrangThai int NOT NULL,
        NgayTao datetime2 NOT NULL,
        NgayCapNhat datetime2 NULL,
        CONSTRAINT CK_RoommateRequest_Status CHECK(TrangThai BETWEEN 0 AND 4),
        CONSTRAINT FK_YeuCauOGhep_TinTimNguoiOGhep_RoommatePostId FOREIGN KEY(RoommatePostId) REFERENCES dbo.TinTimNguoiOGhep(Id),
        CONSTRAINT FK_YeuCauOGhep_TaiKhoan_ApplicantTenantAccountId FOREIGN KEY(ApplicantTenantAccountId) REFERENCES dbo.TaiKhoan(Id)
    );
    CREATE UNIQUE INDEX UX_RoommateRequest_ActiveApplicant ON dbo.YeuCauOGhep(RoommatePostId,ApplicantTenantAccountId) WHERE TrangThai IN(0,1);
    CREATE INDEX IX_YeuCauOGhep_ApplicantTenantAccountId ON dbo.YeuCauOGhep(ApplicantTenantAccountId);
    CREATE TABLE dbo.NguoiOCungHopDong (
        Id int IDENTITY NOT NULL CONSTRAINT PK_NguoiOCungHopDong PRIMARY KEY,
        ContractId int NOT NULL,
        TenantAccountId int NOT NULL,
        TrangThai int NOT NULL,
        NgayVao datetime2 NOT NULL,
        NgayRoi datetime2 NULL,
        CONSTRAINT CK_ContractOccupant_Status CHECK(TrangThai IN(0,1)),
        CONSTRAINT CK_ContractOccupant_Dates CHECK((TrangThai=0 AND NgayRoi IS NULL) OR (TrangThai=1 AND NgayRoi IS NOT NULL AND NgayRoi>=NgayVao)),
        CONSTRAINT FK_NguoiOCungHopDong_HopDongThue_ContractId FOREIGN KEY(ContractId) REFERENCES dbo.HopDongThue(Id),
        CONSTRAINT FK_NguoiOCungHopDong_TaiKhoan_TenantAccountId FOREIGN KEY(TenantAccountId) REFERENCES dbo.TaiKhoan(Id)
    );
    CREATE UNIQUE INDEX UX_ContractOccupant_ActiveTenant ON dbo.NguoiOCungHopDong(ContractId,TenantAccountId) WHERE TrangThai=0;
    CREATE INDEX IX_NguoiOCungHopDong_TenantAccountId ON dbo.NguoiOCungHopDong(TenantAccountId);
    COMMIT TRANSACTION;
END;
