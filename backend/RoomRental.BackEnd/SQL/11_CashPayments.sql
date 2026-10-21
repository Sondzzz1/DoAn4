-- Additive cash receipt upgrade. Back up the selected database before running.
-- For RoomRentalDB, set @AllowMainDatabase to 1 after reviewing this file.
-- No existing deposit, invoice, contract or VNPay payment row is changed.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
DECLARE @AllowMainDatabase bit = 0;
IF DB_NAME() = N'RoomRentalDB' AND @AllowMainDatabase <> 1
    THROW 51200, 'Back up RoomRentalDB, then explicitly set @AllowMainDatabase = 1.', 1;
IF DB_NAME() <> N'RoomRentalDB' AND DB_NAME() NOT LIKE N'RoomRental[_]Verification[_]%'
    THROW 51201, 'Select RoomRentalDB or a RoomRental_Verification_* QA database.', 1;
IF @@TRANCOUNT <> 0
    THROW 51202, 'Run this upgrade outside an existing transaction.', 1;
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL OR OBJECT_ID(N'dbo.DatCoc', N'U') IS NULL
    OR OBJECT_ID(N'dbo.HoaDonHangThang', N'U') IS NULL OR OBJECT_ID(N'dbo.SuKienThanhToan', N'U') IS NULL
    THROW 51203, 'Missing prerequisite current schema or payment safety tables.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource=N'RoomRental.Schema.CashPayments',
        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
    IF @lock < 0 THROW 51204, 'Another cash schema upgrade is running. Retry later.', 1;

    IF OBJECT_ID(N'dbo.PhieuThuTienMat', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PhieuThuTienMat (
            Id int IDENTITY NOT NULL CONSTRAINT PK_PhieuThuTienMat PRIMARY KEY,
            DepositId int NULL,
            MonthlyBillId int NULL,
            Amount decimal(18,2) NOT NULL,
            TenantAccountId int NOT NULL,
            LandlordAccountId int NOT NULL,
            Status int NOT NULL CONSTRAINT DF_CashPayment_Status DEFAULT 0,
            SelectedAtUtc datetime2 NOT NULL CONSTRAINT DF_CashPayment_Selected DEFAULT SYSUTCDATETIME(),
            ConfirmedAtUtc datetime2 NULL,
            CancelledAtUtc datetime2 NULL,
            ConfirmedByAccountId int NULL,
            ReceiptNumber nvarchar(60) NULL,
            Note nvarchar(1000) NULL,
            RowVersion rowversion NOT NULL,
            CONSTRAINT CK_CashPayment_Target CHECK ((DepositId IS NOT NULL AND MonthlyBillId IS NULL) OR (DepositId IS NULL AND MonthlyBillId IS NOT NULL)),
            CONSTRAINT CK_CashPayment_Amount CHECK (Amount > 0),
            CONSTRAINT CK_CashPayment_Status CHECK (Status IN (0,1,2)),
            CONSTRAINT CK_CashPayment_Actors CHECK (TenantAccountId <> LandlordAccountId),
            CONSTRAINT CK_CashPayment_Received CHECK (Status <> 1 OR (ConfirmedAtUtc IS NOT NULL AND ConfirmedByAccountId IS NOT NULL AND ConfirmedByAccountId = LandlordAccountId AND ReceiptNumber IS NOT NULL AND LEN(LTRIM(RTRIM(ReceiptNumber))) > 0 AND CancelledAtUtc IS NULL)),
            CONSTRAINT CK_CashPayment_Cancelled CHECK (Status <> 2 OR CancelledAtUtc IS NOT NULL),
            CONSTRAINT CK_CashPayment_Unreceived CHECK (Status = 1 OR (ConfirmedAtUtc IS NULL AND ConfirmedByAccountId IS NULL AND ReceiptNumber IS NULL)),
            CONSTRAINT CK_CashPayment_Selected CHECK (Status <> 0 OR CancelledAtUtc IS NULL),
            CONSTRAINT FK_PhieuThuTienMat_DatCoc_DepositId FOREIGN KEY (DepositId) REFERENCES dbo.DatCoc(Id),
            CONSTRAINT FK_PhieuThuTienMat_HoaDonHangThang_MonthlyBillId FOREIGN KEY (MonthlyBillId) REFERENCES dbo.HoaDonHangThang(Id),
            CONSTRAINT FK_PhieuThuTienMat_TaiKhoan_TenantAccountId FOREIGN KEY (TenantAccountId) REFERENCES dbo.TaiKhoan(Id),
            CONSTRAINT FK_PhieuThuTienMat_TaiKhoan_LandlordAccountId FOREIGN KEY (LandlordAccountId) REFERENCES dbo.TaiKhoan(Id),
            CONSTRAINT FK_PhieuThuTienMat_TaiKhoan_ConfirmedByAccountId FOREIGN KEY (ConfirmedByAccountId) REFERENCES dbo.TaiKhoan(Id)
        );
        CREATE UNIQUE INDEX UX_CashPayment_Deposit_Active ON dbo.PhieuThuTienMat(DepositId) WHERE DepositId IS NOT NULL AND Status IN (0,1);
        CREATE UNIQUE INDEX UX_CashPayment_Bill_Active ON dbo.PhieuThuTienMat(MonthlyBillId) WHERE MonthlyBillId IS NOT NULL AND Status IN (0,1);
        CREATE UNIQUE INDEX UX_CashPayment_ReceiptNumber ON dbo.PhieuThuTienMat(ReceiptNumber) WHERE ReceiptNumber IS NOT NULL;
        CREATE INDEX IX_PhieuThuTienMat_TenantAccountId_Status ON dbo.PhieuThuTienMat(TenantAccountId,Status);
        CREATE INDEX IX_PhieuThuTienMat_LandlordAccountId_Status ON dbo.PhieuThuTienMat(LandlordAccountId,Status);
        CREATE INDEX IX_PhieuThuTienMat_ConfirmedByAccountId ON dbo.PhieuThuTienMat(ConfirmedByAccountId);
    END;

    DECLARE @expected TABLE (Name sysname, TypeName sysname, MaxLength int, Nullable bit);
    INSERT @expected VALUES
        ('Id','int',4,0),('DepositId','int',4,1),('MonthlyBillId','int',4,1),('Amount','decimal',9,0),
        ('TenantAccountId','int',4,0),('LandlordAccountId','int',4,0),('Status','int',4,0),
        ('SelectedAtUtc','datetime2',8,0),('ConfirmedAtUtc','datetime2',8,1),('CancelledAtUtc','datetime2',8,1),
        ('ConfirmedByAccountId','int',4,1),('ReceiptNumber','nvarchar',120,1),('Note','nvarchar',2000,1),('RowVersion','timestamp',8,0);
    IF EXISTS (SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND c.name=e.Name
        LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id
        WHERE c.column_id IS NULL OR t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable)
        OR (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.PhieuThuTienMat')) <> 14
        THROW 51205, 'Cash receipt columns differ from the expected schema. Inspect manually.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND name='Amount' AND (precision<>18 OR scale<>2))
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND name='Id' AND is_identity=1)
        THROW 51206, 'Cash receipt identity or money precision is incompatible.', 1;
    IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0) <> 5
        OR (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND is_disabled=0 AND is_not_trusted=0) <> 8
        THROW 51207, 'Cash receipt foreign keys/check constraints are missing or disabled.', 1;
    IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PhieuThuTienMat') AND is_unique=1 AND has_filter=1 AND is_disabled=0
        AND name IN ('UX_CashPayment_Deposit_Active','UX_CashPayment_Bill_Active','UX_CashPayment_ReceiptNumber')) <> 3
        THROW 51208, 'Cash receipt uniqueness indexes are missing or disabled.', 1;
    COMMIT;
    PRINT 'Cash receipt schema verified. Existing business rows unchanged.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
