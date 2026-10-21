-- Additive VNPay saved-method upgrade. Back up the selected database first.
-- Set @AllowMainDatabase = 1 only after reviewing this script and the backup.
-- Existing money, status, order and transaction-code columns are not changed.
-- Existing gateway rows receive only the new Provider='VNPay' metadata default.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
DECLARE @AllowMainDatabase bit = 0;
IF DB_NAME() = N'RoomRentalDB' AND @AllowMainDatabase <> 1
    THROW 51300, 'Back up RoomRentalDB, then explicitly set @AllowMainDatabase = 1.', 1;
IF DB_NAME() <> N'RoomRentalDB' AND DB_NAME() NOT LIKE N'RoomRental[_]Verification[_]%'
    THROW 51301, 'Select RoomRentalDB or a RoomRental_Verification_* QA database.', 1;
IF @@TRANCOUNT <> 0 THROW 51302, 'Run this upgrade outside an existing transaction.', 1;
IF OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NULL OR OBJECT_ID(N'dbo.GiaoDichThanhToan',N'U') IS NULL
    THROW 51303, 'The current account/payment base tables are required.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource=N'RoomRental.Schema.SavedPaymentMethods',
        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
    IF @lock < 0 THROW 51304, 'Another saved-method schema upgrade is running. Retry later.', 1;
    IF (OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu',N'U') IS NULL AND OBJECT_ID(N'dbo.LienKetTheVNPay',N'U') IS NOT NULL)
        OR (OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.LienKetTheVNPay',N'U') IS NULL)
        THROW 51305, 'Partial saved-method schema detected. Inspect it before upgrading.', 1;
    IF (COL_LENGTH(N'dbo.GiaoDichThanhToan',N'Provider') IS NULL AND COL_LENGTH(N'dbo.GiaoDichThanhToan',N'SavedPaymentMethodId') IS NOT NULL)
        OR (COL_LENGTH(N'dbo.GiaoDichThanhToan',N'Provider') IS NOT NULL AND COL_LENGTH(N'dbo.GiaoDichThanhToan',N'SavedPaymentMethodId') IS NULL)
        THROW 51306, 'Partial payment-provider metadata detected. Inspect it before upgrading.', 1;

    IF OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PhuongThucThanhToanDaLuu (
            Id int IDENTITY NOT NULL CONSTRAINT PK_PhuongThucThanhToanDaLuu PRIMARY KEY,
            UserId int NOT NULL,
            MerchantCode nvarchar(8) NOT NULL,
            TokenCiphertext nvarchar(2048) NULL,
            TokenFingerprint char(64) NOT NULL,
            BankCode nvarchar(20) NOT NULL,
            MaskedCardNumber nvarchar(12) NOT NULL,
            CardType char(2) NOT NULL,
            IsActive bit NOT NULL CONSTRAINT DF_SavedMethod_Active DEFAULT CONVERT(bit,1),
            CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_SavedMethod_Created DEFAULT SYSUTCDATETIME(),
            UnlinkedAtUtc datetime2 NULL,
            RowVersion rowversion NOT NULL,
            CONSTRAINT CK_SavedPaymentMethod_CardType CHECK (CardType IN ('01','02')),
            CONSTRAINT CK_SavedPaymentMethod_ActiveToken CHECK ((IsActive=1 AND TokenCiphertext IS NOT NULL AND LEN(TokenCiphertext)>0 AND UnlinkedAtUtc IS NULL) OR (IsActive=0 AND TokenCiphertext IS NULL AND UnlinkedAtUtc IS NOT NULL)),
            CONSTRAINT FK_PhuongThucThanhToanDaLuu_TaiKhoan_UserId FOREIGN KEY(UserId) REFERENCES dbo.TaiKhoan(Id)
        );
        CREATE UNIQUE INDEX IX_PhuongThucThanhToanDaLuu_MerchantCode_TokenFingerprint ON dbo.PhuongThucThanhToanDaLuu(MerchantCode,TokenFingerprint);
        CREATE INDEX IX_PhuongThucThanhToanDaLuu_UserId_IsActive ON dbo.PhuongThucThanhToanDaLuu(UserId,IsActive);

        CREATE TABLE dbo.LienKetTheVNPay (
            Id int IDENTITY NOT NULL CONSTRAINT PK_LienKetTheVNPay PRIMARY KEY,
            UserId int NOT NULL,
            Reference nvarchar(100) NOT NULL,
            MerchantCode nvarchar(8) NOT NULL,
            CardType char(2) NOT NULL,
            Status int NOT NULL,
            CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_CardLink_Created DEFAULT SYSUTCDATETIME(),
            ExpiresAtUtc datetime2 NOT NULL,
            CompletedAtUtc datetime2 NULL,
            SavedPaymentMethodId int NULL,
            CallbackFingerprint char(64) NULL,
            RowVersion rowversion NOT NULL,
            CONSTRAINT CK_VnPayCardLink_Status CHECK (Status IN (0,1,2)),
            CONSTRAINT CK_VnPayCardLink_CardType CHECK (CardType IN ('01','02')),
            CONSTRAINT CK_VnPayCardLink_Expiry CHECK (ExpiresAtUtc > CreatedAtUtc),
            CONSTRAINT CK_VnPayCardLink_Result CHECK ((Status=0 AND CompletedAtUtc IS NULL AND SavedPaymentMethodId IS NULL AND CallbackFingerprint IS NULL) OR (Status=1 AND CompletedAtUtc IS NOT NULL AND SavedPaymentMethodId IS NOT NULL AND CallbackFingerprint IS NOT NULL) OR (Status=2 AND CompletedAtUtc IS NOT NULL AND SavedPaymentMethodId IS NULL AND CallbackFingerprint IS NOT NULL)),
            CONSTRAINT FK_LienKetTheVNPay_TaiKhoan_UserId FOREIGN KEY(UserId) REFERENCES dbo.TaiKhoan(Id),
            CONSTRAINT FK_LienKetTheVNPay_PhuongThucThanhToanDaLuu_SavedPaymentMethodId FOREIGN KEY(SavedPaymentMethodId) REFERENCES dbo.PhuongThucThanhToanDaLuu(Id)
        );
        CREATE UNIQUE INDEX IX_LienKetTheVNPay_Reference ON dbo.LienKetTheVNPay(Reference);
        CREATE INDEX IX_LienKetTheVNPay_UserId_Status_ExpiresAtUtc ON dbo.LienKetTheVNPay(UserId,Status,ExpiresAtUtc);
        CREATE INDEX IX_LienKetTheVNPay_SavedPaymentMethodId ON dbo.LienKetTheVNPay(SavedPaymentMethodId);
    END;

    IF COL_LENGTH(N'dbo.GiaoDichThanhToan',N'Provider') IS NULL
    BEGIN
        ALTER TABLE dbo.GiaoDichThanhToan ADD Provider nvarchar(20) NOT NULL CONSTRAINT DF_Payment_Provider DEFAULT N'VNPay' WITH VALUES;
        ALTER TABLE dbo.GiaoDichThanhToan ADD SavedPaymentMethodId int NULL;
        -- A fresh batch is needed because these columns did not exist when this batch was compiled.
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.GiaoDichThanhToan WITH CHECK ADD CONSTRAINT CK_Payment_Provider CHECK (([Provider] = N''VNPay'' AND [SavedPaymentMethodId] IS NULL) OR ([Provider] = N''VNPayToken'' AND [SavedPaymentMethodId] IS NOT NULL));
            ALTER TABLE dbo.GiaoDichThanhToan WITH CHECK ADD CONSTRAINT FK_GiaoDichThanhToan_PhuongThucThanhToanDaLuu_SavedPaymentMethodId FOREIGN KEY(SavedPaymentMethodId) REFERENCES dbo.PhuongThucThanhToanDaLuu(Id);
            CREATE INDEX IX_GiaoDichThanhToan_SavedPaymentMethodId ON dbo.GiaoDichThanhToan(SavedPaymentMethodId);';
    END;

    DECLARE @expected TABLE(TableName sysname,ColumnName sysname,TypeName sysname,MaxLength int,Nullable bit);
    INSERT @expected VALUES
        ('PhuongThucThanhToanDaLuu','Id','int',4,0),('PhuongThucThanhToanDaLuu','UserId','int',4,0),
        ('PhuongThucThanhToanDaLuu','MerchantCode','nvarchar',16,0),('PhuongThucThanhToanDaLuu','TokenCiphertext','nvarchar',4096,1),
        ('PhuongThucThanhToanDaLuu','TokenFingerprint','char',64,0),('PhuongThucThanhToanDaLuu','BankCode','nvarchar',40,0),
        ('PhuongThucThanhToanDaLuu','MaskedCardNumber','nvarchar',24,0),('PhuongThucThanhToanDaLuu','CardType','char',2,0),
        ('PhuongThucThanhToanDaLuu','IsActive','bit',1,0),('PhuongThucThanhToanDaLuu','CreatedAtUtc','datetime2',8,0),
        ('PhuongThucThanhToanDaLuu','UnlinkedAtUtc','datetime2',8,1),('PhuongThucThanhToanDaLuu','RowVersion','timestamp',8,0),
        ('LienKetTheVNPay','Id','int',4,0),('LienKetTheVNPay','UserId','int',4,0),('LienKetTheVNPay','Reference','nvarchar',200,0),
        ('LienKetTheVNPay','MerchantCode','nvarchar',16,0),('LienKetTheVNPay','CardType','char',2,0),('LienKetTheVNPay','Status','int',4,0),
        ('LienKetTheVNPay','CreatedAtUtc','datetime2',8,0),('LienKetTheVNPay','ExpiresAtUtc','datetime2',8,0),
        ('LienKetTheVNPay','CompletedAtUtc','datetime2',8,1),('LienKetTheVNPay','SavedPaymentMethodId','int',4,1),
        ('LienKetTheVNPay','CallbackFingerprint','char',64,1),('LienKetTheVNPay','RowVersion','timestamp',8,0),
        ('GiaoDichThanhToan','Provider','nvarchar',40,0),('GiaoDichThanhToan','SavedPaymentMethodId','int',4,1);
    IF EXISTS (SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName
        LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id
        WHERE c.column_id IS NULL OR t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable)
        THROW 51307, 'Existing saved-method/provider columns do not match the required schema.', 1;
    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id IN(OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu'),OBJECT_ID(N'dbo.LienKetTheVNPay')))<>24
        THROW 51308, 'Unexpected saved-method columns detected. Inspect manually.', 1;
    IF (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN(OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu'),OBJECT_ID(N'dbo.LienKetTheVNPay')) AND is_disabled=0 AND is_not_trusted=0)<>6
        OR NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GiaoDichThanhToan') AND name=N'CK_Payment_Provider' AND is_disabled=0 AND is_not_trusted=0)
        THROW 51309, 'Required saved-method/provider checks are missing or disabled.', 1;
    IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN(OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu'),OBJECT_ID(N'dbo.LienKetTheVNPay')) AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>3
        OR NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.GiaoDichThanhToan') AND name=N'FK_GiaoDichThanhToan_PhuongThucThanhToanDaLuu_SavedPaymentMethodId' AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)
        THROW 51310, 'Required saved-method/provider foreign keys are missing or disabled.', 1;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PhuongThucThanhToanDaLuu') AND name=N'IX_PhuongThucThanhToanDaLuu_MerchantCode_TokenFingerprint' AND is_unique=1 AND is_disabled=0)
        OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LienKetTheVNPay') AND name=N'IX_LienKetTheVNPay_Reference' AND is_unique=1 AND is_disabled=0)
        OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.GiaoDichThanhToan') AND name=N'IX_GiaoDichThanhToan_SavedPaymentMethodId' AND is_disabled=0)
        THROW 51311, 'Required saved-method/provider indexes are missing or disabled.', 1;
    COMMIT;
    PRINT 'Saved-method schema verified. Existing financial values unchanged.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
