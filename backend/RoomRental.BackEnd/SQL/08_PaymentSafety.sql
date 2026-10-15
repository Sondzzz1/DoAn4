-- Additive upgrade only. Back up the selected database before deployment.
-- On RoomRentalDB, deliberately change @AllowMainDatabase to 1 after review.
-- This script never modifies existing Room/Request/Deposit/Payment/Contract rows.
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
    THROW 51040, 'Main database requires explicit opt-in and backup. Review @AllowMainDatabase.', 1;
IF DB_NAME() <> N'RoomRentalDB' AND DB_NAME() NOT LIKE N'RoomRental[_]Verification[_]%'
    THROW 51041, 'Unexpected database. Select RoomRentalDB or a RoomRental_Verification_* QA database.', 1;
IF OBJECT_ID(N'dbo.DatCoc',N'U') IS NULL OR OBJECT_ID(N'dbo.GiaoDichThanhToan',N'U') IS NULL OR OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NULL
    THROW 51042, 'Missing prerequisite base tables.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock=sys.sp_getapplock @Resource=N'RoomRental.Schema.PaymentSafety',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
    IF @lock < 0 THROW 51043, 'Schema upgrade is busy. Retry later.', 1;
    IF (OBJECT_ID(N'dbo.SuKienThanhToan',N'U') IS NULL AND OBJECT_ID(N'dbo.YeuCauHoanCoc',N'U') IS NOT NULL)
        OR (OBJECT_ID(N'dbo.SuKienThanhToan',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.YeuCauHoanCoc',N'U') IS NULL)
        THROW 51044, 'Partial payment safety schema; inspect before retry.', 1;
    IF OBJECT_ID(N'dbo.SuKienThanhToan',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SuKienThanhToan (
            Id bigint IDENTITY NOT NULL CONSTRAINT PK_SuKienThanhToan PRIMARY KEY,
            PaymentTransactionId int NULL,
            ProviderOrderId nvarchar(100) NOT NULL,
            ProviderTransactionCode nvarchar(100) NULL,
            DeduplicationKey char(64) NOT NULL,
            Source tinyint NOT NULL,
            ReportedAmount decimal(18,2) NOT NULL,
            ResponseCode char(2) NOT NULL,
            ProviderStatus char(2) NOT NULL,
            ProviderPaidAtUtc datetime2(7) NULL,
            ReceivedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_PaymentEvent_Received DEFAULT SYSUTCDATETIME(),
            ProcessingState tinyint NOT NULL CONSTRAINT DF_PaymentEvent_State DEFAULT 0,
            Reason nvarchar(500) NULL,
            ResolvedByAccountId int NULL,
            ResolvedAtUtc datetime2(7) NULL,
            ResolutionNote nvarchar(1000) NULL,
            RowVersion rowversion NOT NULL,
            CONSTRAINT CK_PaymentEvent_Source CHECK(Source IN(0,1)),
            CONSTRAINT CK_PaymentEvent_State CHECK(ProcessingState BETWEEN 0 AND 3),
            CONSTRAINT CK_PaymentEvent_Amount CHECK(ReportedAmount >= 0),
            CONSTRAINT FK_SuKienThanhToan_GiaoDichThanhToan_PaymentTransactionId FOREIGN KEY(PaymentTransactionId) REFERENCES dbo.GiaoDichThanhToan(Id),
            CONSTRAINT FK_SuKienThanhToan_TaiKhoan_ResolvedByAccountId FOREIGN KEY(ResolvedByAccountId) REFERENCES dbo.TaiKhoan(Id)
        );
        CREATE UNIQUE INDEX IX_SuKienThanhToan_DeduplicationKey ON dbo.SuKienThanhToan(DeduplicationKey);
        CREATE INDEX IX_SuKienThanhToan_PaymentTransactionId ON dbo.SuKienThanhToan(PaymentTransactionId);
        CREATE INDEX IX_SuKienThanhToan_ResolvedByAccountId ON dbo.SuKienThanhToan(ResolvedByAccountId);
        CREATE INDEX IX_SuKienThanhToan_ProcessingState_ReceivedAtUtc ON dbo.SuKienThanhToan(ProcessingState,ReceivedAtUtc);
        CREATE TABLE dbo.YeuCauHoanCoc (
            Id int IDENTITY NOT NULL CONSTRAINT PK_YeuCauHoanCoc PRIMARY KEY,
            DepositId int NOT NULL,
            Amount decimal(18,2) NOT NULL,
            PreviousDepositStatus int NOT NULL,
            Status tinyint NOT NULL CONSTRAINT DF_DepositRefund_Status DEFAULT 0,
            Reason nvarchar(500) NOT NULL,
            RequestedByAccountId int NOT NULL,
            RequestedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_DepositRefund_Requested DEFAULT SYSUTCDATETIME(),
            ReviewedByAccountId int NULL,
            ReviewedAtUtc datetime2(7) NULL,
            DecisionNote nvarchar(1000) NULL,
            RefundReference nvarchar(200) NULL,
            RefundReportedByAccountId int NULL,
            RefundReportedAtUtc datetime2(7) NULL,
            CompletedByAccountId int NULL,
            CompletedAtUtc datetime2(7) NULL,
            RowVersion rowversion NOT NULL,
            CONSTRAINT CK_DepositRefund_Amount CHECK(Amount > 0),
            CONSTRAINT CK_DepositRefund_PreviousStatus CHECK(PreviousDepositStatus IN(1,2)),
            CONSTRAINT CK_DepositRefund_Status CHECK(Status BETWEEN 0 AND 5),
            CONSTRAINT CK_DepositRefund_Evidence CHECK(Status NOT IN(2,3) OR (RefundReference IS NOT NULL AND LEN(LTRIM(RTRIM(RefundReference)))>0 AND RefundReportedByAccountId IS NOT NULL AND RefundReportedAtUtc IS NOT NULL)),
            CONSTRAINT CK_DepositRefund_Completed CHECK(Status<>3 OR (CompletedByAccountId IS NOT NULL AND CompletedAtUtc IS NOT NULL)),
            CONSTRAINT FK_YeuCauHoanCoc_DatCoc_DepositId FOREIGN KEY(DepositId) REFERENCES dbo.DatCoc(Id),
            CONSTRAINT FK_YeuCauHoanCoc_TaiKhoan_RequestedByAccountId FOREIGN KEY(RequestedByAccountId) REFERENCES dbo.TaiKhoan(Id),
            CONSTRAINT FK_YeuCauHoanCoc_TaiKhoan_ReviewedByAccountId FOREIGN KEY(ReviewedByAccountId) REFERENCES dbo.TaiKhoan(Id),
            CONSTRAINT FK_YeuCauHoanCoc_TaiKhoan_RefundReportedByAccountId FOREIGN KEY(RefundReportedByAccountId) REFERENCES dbo.TaiKhoan(Id),
            CONSTRAINT FK_YeuCauHoanCoc_TaiKhoan_CompletedByAccountId FOREIGN KEY(CompletedByAccountId) REFERENCES dbo.TaiKhoan(Id)
        );
        CREATE UNIQUE INDEX IX_YeuCauHoanCoc_DepositId ON dbo.YeuCauHoanCoc(DepositId) WHERE Status IN(0,1,2);
        CREATE INDEX IX_YeuCauHoanCoc_RequestedByAccountId_Status ON dbo.YeuCauHoanCoc(RequestedByAccountId,Status);
        CREATE INDEX IX_YeuCauHoanCoc_ReviewedByAccountId_Status ON dbo.YeuCauHoanCoc(ReviewedByAccountId,Status);
        CREATE INDEX IX_YeuCauHoanCoc_RefundReportedByAccountId ON dbo.YeuCauHoanCoc(RefundReportedByAccountId);
        CREATE INDEX IX_YeuCauHoanCoc_CompletedByAccountId ON dbo.YeuCauHoanCoc(CompletedByAccountId);
    END;
    -- Fail closed on incompatible existing tables rather than silently skipping them.
    DECLARE @expected TABLE(TableName sysname, ColumnName sysname, TypeName sysname, MaxLength smallint, Nullable bit);
    INSERT @expected VALUES
    ('SuKienThanhToan','Id','bigint',8,0),('SuKienThanhToan','PaymentTransactionId','int',4,1),
    ('SuKienThanhToan','ProviderOrderId','nvarchar',200,0),('SuKienThanhToan','ProviderTransactionCode','nvarchar',200,1),
    ('SuKienThanhToan','DeduplicationKey','char',64,0),('SuKienThanhToan','Source','tinyint',1,0),
    ('SuKienThanhToan','ReportedAmount','decimal',9,0),('SuKienThanhToan','ResponseCode','char',2,0),('SuKienThanhToan','ProviderStatus','char',2,0),
    ('SuKienThanhToan','ProviderPaidAtUtc','datetime2',8,1),('SuKienThanhToan','ReceivedAtUtc','datetime2',8,0),
    ('SuKienThanhToan','ProcessingState','tinyint',1,0),('SuKienThanhToan','Reason','nvarchar',1000,1),
    ('SuKienThanhToan','ResolvedByAccountId','int',4,1),('SuKienThanhToan','ResolvedAtUtc','datetime2',8,1),
    ('SuKienThanhToan','ResolutionNote','nvarchar',2000,1),('SuKienThanhToan','RowVersion','timestamp',8,0),
    ('YeuCauHoanCoc','Id','int',4,0),('YeuCauHoanCoc','DepositId','int',4,0),('YeuCauHoanCoc','Amount','decimal',9,0),
    ('YeuCauHoanCoc','PreviousDepositStatus','int',4,0),('YeuCauHoanCoc','Status','tinyint',1,0),('YeuCauHoanCoc','Reason','nvarchar',1000,0),
    ('YeuCauHoanCoc','RequestedByAccountId','int',4,0),('YeuCauHoanCoc','RequestedAtUtc','datetime2',8,0),
    ('YeuCauHoanCoc','ReviewedByAccountId','int',4,1),('YeuCauHoanCoc','ReviewedAtUtc','datetime2',8,1),('YeuCauHoanCoc','DecisionNote','nvarchar',2000,1),
    ('YeuCauHoanCoc','RefundReference','nvarchar',400,1),('YeuCauHoanCoc','RefundReportedByAccountId','int',4,1),
    ('YeuCauHoanCoc','RefundReportedAtUtc','datetime2',8,1),('YeuCauHoanCoc','CompletedByAccountId','int',4,1),
    ('YeuCauHoanCoc','CompletedAtUtc','datetime2',8,1),('YeuCauHoanCoc','RowVersion','timestamp',8,0);
    IF EXISTS(SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName
        LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.column_id IS NULL OR t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable)
        THROW 51045, 'Existing columns do not match the approved schema. No automatic repair performed.', 1;
    IF EXISTS(SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
        WHERE t.name IN('SuKienThanhToan','YeuCauHoanCoc') AND NOT EXISTS(SELECT 1 FROM @expected e WHERE e.TableName=t.name AND e.ColumnName=c.name))
        THROW 51046, 'Unexpected additional columns detected. Inspect schema manually.', 1;
    IF EXISTS(SELECT 1 FROM sys.columns c WHERE c.object_id IN(OBJECT_ID(N'dbo.SuKienThanhToan'),OBJECT_ID(N'dbo.YeuCauHoanCoc'))
        AND c.name IN('Amount','ReportedAmount') AND (c.precision<>18 OR c.scale<>2))
        THROW 51047, 'Incompatible money precision.', 1;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SuKienThanhToan') AND name='IX_SuKienThanhToan_DeduplicationKey' AND is_unique=1)
        OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.YeuCauHoanCoc') AND name='IX_YeuCauHoanCoc_DepositId' AND is_unique=1 AND has_filter=1)
        THROW 51048, 'Required financial uniqueness indexes are missing.', 1;
    IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN(OBJECT_ID(N'dbo.SuKienThanhToan'),OBJECT_ID(N'dbo.YeuCauHoanCoc')) AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>7
        OR (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN(OBJECT_ID(N'dbo.SuKienThanhToan'),OBJECT_ID(N'dbo.YeuCauHoanCoc')) AND is_disabled=0 AND is_not_trusted=0)<>8
        THROW 51049, 'Required foreign keys/check constraints are missing or disabled.', 1;
    IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id IN(OBJECT_ID(N'dbo.SuKienThanhToan'),OBJECT_ID(N'dbo.YeuCauHoanCoc')) AND name='Id' AND is_identity=0)
        THROW 51050, 'Expected identity primary keys.', 1;
    COMMIT;
    PRINT 'Payment safety schema verified. Existing business data unchanged.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
