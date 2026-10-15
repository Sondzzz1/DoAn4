-- Generated additive upgrade for the approved Stage 3A schema. No old rows are updated.
-- Back up first. Only deliberately enable the main database after review.
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
DECLARE @AllowMainDatabase bit=0;
IF DB_NAME()=N'RoomRentalDB' AND @AllowMainDatabase<>1 THROW 51060,'Main database requires explicit opt-in and backup.',1;
IF DB_NAME()<>N'RoomRentalDB' AND DB_NAME() NOT LIKE N'RoomRental[_]Verification[_]%' THROW 51061,'Unexpected database.',1;
IF OBJECT_ID(N'dbo.NguoiOCungHopDong',N'U') IS NULL OR OBJECT_ID(N'dbo.TinTimNguoiOGhep',N'U') IS NULL OR OBJECT_ID(N'dbo.YeuCauOGhep',N'U') IS NULL OR OBJECT_ID(N'dbo.SuKienThanhToan',N'U') IS NULL OR OBJECT_ID(N'dbo.YeuCauHoanCoc',N'U') IS NULL
    THROW 51062,'Apply prerequisite SQL 07 and 08 after review first.',1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'RoomRental.Schema.ContractClosure',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
IF @lock<0 THROW 51063,'Schema upgrade busy.',1;
DECLARE @count int=(SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN (N'HoSoKetThucHopDong',N'YeuCauKetThucHopDong',N'PhuongAnTatToanHopDong',N'KhoanMucTatToanHopDong',N'LichSuKetThucHopDong'));
IF @count NOT IN (0,5) THROW 51064,'Partial closure schema. Inspect before retry.',1;
IF @count=0
BEGIN

CREATE TABLE [HoSoKetThucHopDong] (
    [Id] int NOT NULL IDENTITY,
    [ContractId] int NOT NULL,
    [RoomId] int NOT NULL,
    [EndKind] tinyint NULL,
    [EffectiveEndAtUtc] datetime2 NULL,
    [LastRentalDate] date NULL,
    [HandoverStatus] tinyint NOT NULL,
    [HandoverKind] tinyint NULL,
    [HandoverRevision] int NOT NULL,
    [ActualHandoverAtUtc] datetime2 NULL,
    [Condition] nvarchar(2000) NULL,
    [ClosingElectricity] decimal(18,2) NULL,
    [ClosingWater] decimal(18,2) NULL,
    [DamageNotes] nvarchar(2000) NULL,
    [EvidenceJson] nvarchar(max) NULL,
    [TenantHandoverConfirmedByAccountId] int NULL,
    [TenantHandoverConfirmedAtUtc] datetime2 NULL,
    [LandlordHandoverConfirmedByAccountId] int NULL,
    [LandlordHandoverConfirmedAtUtc] datetime2 NULL,
    [BillingRevision] int NOT NULL,
    [BillingFinalizedAtUtc] datetime2 NULL,
    [BillingFinalizedByAccountId] int NULL,
    [ReleasedAtUtc] datetime2 NULL,
    [ReleasedByAccountId] int NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_HoSoKetThucHopDong] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Closure_Confirmed] CHECK ([HandoverStatus]<>3 OR ([TenantHandoverConfirmedByAccountId] IS NOT NULL AND [TenantHandoverConfirmedAtUtc] IS NOT NULL AND [LandlordHandoverConfirmedByAccountId] IS NOT NULL AND [LandlordHandoverConfirmedAtUtc] IS NOT NULL AND [HandoverKind] IS NOT NULL AND ([HandoverKind]=1 OR ([ActualHandoverAtUtc] IS NOT NULL AND [ClosingElectricity] IS NOT NULL AND [ClosingWater] IS NOT NULL)))),
    CONSTRAINT [CK_Closure_Json] CHECK ([EvidenceJson] IS NULL OR ISJSON([EvidenceJson])=1),
    CONSTRAINT [CK_Closure_Meters] CHECK (([ClosingElectricity] IS NULL OR [ClosingElectricity]>=0) AND ([ClosingWater] IS NULL OR [ClosingWater]>=0)),
    CONSTRAINT [CK_Closure_Release] CHECK ([ReleasedAtUtc] IS NULL OR ([ReleasedByAccountId] IS NOT NULL AND [EffectiveEndAtUtc] IS NOT NULL AND [HandoverStatus]=3 AND [BillingFinalizedAtUtc] IS NOT NULL)),
    CONSTRAINT [CK_Closure_State] CHECK ([HandoverStatus] BETWEEN 0 AND 4 AND ([EndKind] IS NULL OR [EndKind] BETWEEN 0 AND 2) AND ([HandoverKind] IS NULL OR [HandoverKind] IN (0,1)) AND [HandoverRevision]>=0 AND [BillingRevision]>=0),
    CONSTRAINT [FK_HoSoKetThucHopDong_HopDongThue_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [HopDongThue] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoSoKetThucHopDong_PhongTro_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [PhongTro] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoSoKetThucHopDong_TaiKhoan_BillingFinalizedByAccountId] FOREIGN KEY ([BillingFinalizedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoSoKetThucHopDong_TaiKhoan_LandlordHandoverConfirmedByAccountId] FOREIGN KEY ([LandlordHandoverConfirmedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoSoKetThucHopDong_TaiKhoan_ReleasedByAccountId] FOREIGN KEY ([ReleasedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoSoKetThucHopDong_TaiKhoan_TenantHandoverConfirmedByAccountId] FOREIGN KEY ([TenantHandoverConfirmedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);


CREATE TABLE [PhuongAnTatToanHopDong] (
    [Id] int NOT NULL IDENTITY,
    [ClosureId] int NOT NULL,
    [DepositId] int NOT NULL,
    [Revision] int NOT NULL,
    [Status] tinyint NOT NULL,
    [BillingRevisionSnapshot] int NOT NULL,
    [DepositAmountSnapshot] decimal(18,2) NOT NULL,
    [TotalCharges] decimal(18,2) NOT NULL,
    [RefundDue] decimal(18,2) NOT NULL,
    [ExtraDue] decimal(18,2) NOT NULL,
    [ProposedByAccountId] int NOT NULL,
    [ProposedAtUtc] datetime2 NULL,
    [TenantAgreedByAccountId] int NULL,
    [TenantAgreedAtUtc] datetime2 NULL,
    [LandlordAgreedByAccountId] int NULL,
    [LandlordAgreedAtUtc] datetime2 NULL,
    [RefundReference] nvarchar(500) NULL,
    [RefundReportedByAccountId] int NULL,
    [RefundReportedAtUtc] datetime2 NULL,
    [RefundReceivedByAccountId] int NULL,
    [RefundReceivedAtUtc] datetime2 NULL,
    [ExtraReference] nvarchar(500) NULL,
    [ExtraReportedByAccountId] int NULL,
    [ExtraReportedAtUtc] datetime2 NULL,
    [ExtraReceivedByAccountId] int NULL,
    [ExtraReceivedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CompletedByAccountId] int NULL,
    [Note] nvarchar(2000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PhuongAnTatToanHopDong] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Settlement_Agreed] CHECK ([Status] NOT IN (2,3,4) OR ([TenantAgreedByAccountId] IS NOT NULL AND [TenantAgreedAtUtc] IS NOT NULL AND [LandlordAgreedByAccountId] IS NOT NULL AND [LandlordAgreedAtUtc] IS NOT NULL)),
    CONSTRAINT [CK_Settlement_Completed] CHECK ([Status]<>4 OR ([CompletedAtUtc] IS NOT NULL AND [CompletedByAccountId] IS NOT NULL AND ([RefundDue]=0 OR ([RefundReference] IS NOT NULL AND [RefundReportedAtUtc] IS NOT NULL AND [RefundReportedByAccountId] IS NOT NULL AND [RefundReceivedAtUtc] IS NOT NULL AND [RefundReceivedByAccountId] IS NOT NULL)) AND ([ExtraDue]=0 OR ([ExtraReference] IS NOT NULL AND [ExtraReportedAtUtc] IS NOT NULL AND [ExtraReportedByAccountId] IS NOT NULL AND [ExtraReceivedAtUtc] IS NOT NULL AND [ExtraReceivedByAccountId] IS NOT NULL)))),
    CONSTRAINT [CK_Settlement_Money] CHECK ([DepositAmountSnapshot]>=0 AND [TotalCharges]>=0 AND [RefundDue]>=0 AND [ExtraDue]>=0 AND ([RefundDue]=0 OR [ExtraDue]=0) AND [DepositAmountSnapshot]+[ExtraDue]=[TotalCharges]+[RefundDue]),
    CONSTRAINT [CK_Settlement_State] CHECK ([Status] BETWEEN 0 AND 6 AND [Revision]>0 AND [BillingRevisionSnapshot]>=0),
    CONSTRAINT [FK_PhuongAnTatToanHopDong_DatCoc_DepositId] FOREIGN KEY ([DepositId]) REFERENCES [DatCoc] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_HoSoKetThucHopDong_ClosureId] FOREIGN KEY ([ClosureId]) REFERENCES [HoSoKetThucHopDong] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_CompletedByAccountId] FOREIGN KEY ([CompletedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_ExtraReceivedByAccountId] FOREIGN KEY ([ExtraReceivedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_ExtraReportedByAccountId] FOREIGN KEY ([ExtraReportedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_LandlordAgreedByAccountId] FOREIGN KEY ([LandlordAgreedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_ProposedByAccountId] FOREIGN KEY ([ProposedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_RefundReceivedByAccountId] FOREIGN KEY ([RefundReceivedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_RefundReportedByAccountId] FOREIGN KEY ([RefundReportedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhuongAnTatToanHopDong_TaiKhoan_TenantAgreedByAccountId] FOREIGN KEY ([TenantAgreedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);


CREATE TABLE [YeuCauKetThucHopDong] (
    [Id] int NOT NULL IDENTITY,
    [ClosureId] int NOT NULL,
    [Status] tinyint NOT NULL,
    [RequestedByAccountId] int NOT NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    [ProposedEffectiveEndAtUtc] datetime2 NOT NULL,
    [RespondedByAccountId] int NULL,
    [RespondedAtUtc] datetime2 NULL,
    [ResponseNote] nvarchar(1000) NULL,
    [AgreedEffectiveEndAtUtc] datetime2 NULL,
    [EffectiveAtUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_YeuCauKetThucHopDong] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_EndRequest_Agreed] CHECK ([Status] NOT IN (1,5) OR ([RespondedByAccountId] IS NOT NULL AND [RespondedAtUtc] IS NOT NULL AND [AgreedEffectiveEndAtUtc] IS NOT NULL)),
    CONSTRAINT [CK_EndRequest_Effective] CHECK ([Status]<>5 OR [EffectiveAtUtc] IS NOT NULL),
    CONSTRAINT [CK_EndRequest_State] CHECK ([Status] BETWEEN 0 AND 5 AND LEN(LTRIM(RTRIM([Reason])))>=5),
    CONSTRAINT [FK_YeuCauKetThucHopDong_HoSoKetThucHopDong_ClosureId] FOREIGN KEY ([ClosureId]) REFERENCES [HoSoKetThucHopDong] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_YeuCauKetThucHopDong_TaiKhoan_RequestedByAccountId] FOREIGN KEY ([RequestedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_YeuCauKetThucHopDong_TaiKhoan_RespondedByAccountId] FOREIGN KEY ([RespondedByAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);


CREATE TABLE [KhoanMucTatToanHopDong] (
    [Id] int NOT NULL IDENTITY,
    [SettlementId] int NOT NULL,
    [Kind] tinyint NOT NULL,
    [MonthlyBillId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [DepositAppliedAmount] decimal(18,2) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [EvidenceJson] nvarchar(max) NULL,
    [BillStatusSnapshot] int NULL,
    [BillUpdatedAtSnapshot] datetime2 NULL,
    [BillTotalSnapshot] decimal(18,2) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_KhoanMucTatToanHopDong] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_SettlementLine_Amount] CHECK ([Amount]>0 AND [DepositAppliedAmount]>=0 AND [DepositAppliedAmount]<=[Amount]),
    CONSTRAINT [CK_SettlementLine_Json] CHECK ([EvidenceJson] IS NULL OR ISJSON([EvidenceJson])=1),
    CONSTRAINT [CK_SettlementLine_Kind] CHECK (([Kind]=0 AND [MonthlyBillId] IS NOT NULL AND [BillStatusSnapshot] IS NOT NULL AND [BillTotalSnapshot] IS NOT NULL) OR ([Kind]=1 AND [MonthlyBillId] IS NULL AND [EvidenceJson] IS NOT NULL)),
    CONSTRAINT [FK_KhoanMucTatToanHopDong_HoaDonHangThang_MonthlyBillId] FOREIGN KEY ([MonthlyBillId]) REFERENCES [HoaDonHangThang] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_KhoanMucTatToanHopDong_PhuongAnTatToanHopDong_SettlementId] FOREIGN KEY ([SettlementId]) REFERENCES [PhuongAnTatToanHopDong] ([Id]) ON DELETE NO ACTION
);


CREATE TABLE [LichSuKetThucHopDong] (
    [Id] bigint NOT NULL IDENTITY,
    [ClosureId] int NOT NULL,
    [EndRequestId] int NULL,
    [SettlementId] int NULL,
    [EventType] nvarchar(80) NOT NULL,
    [ActorAccountId] int NULL,
    [Source] tinyint NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [FromState] nvarchar(60) NULL,
    [ToState] nvarchar(60) NULL,
    [Reason] nvarchar(2000) NULL,
    [SnapshotJson] nvarchar(max) NULL,
    [IdempotencyKey] nvarchar(120) NOT NULL,
    CONSTRAINT [PK_LichSuKetThucHopDong] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ClosureEvent_Json] CHECK ([SnapshotJson] IS NULL OR ISJSON([SnapshotJson])=1),
    CONSTRAINT [CK_ClosureEvent_Source] CHECK ([Source] IN (0,1) AND ([Source]=1 OR [ActorAccountId] IS NOT NULL)),
    CONSTRAINT [FK_LichSuKetThucHopDong_HoSoKetThucHopDong_ClosureId] FOREIGN KEY ([ClosureId]) REFERENCES [HoSoKetThucHopDong] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LichSuKetThucHopDong_PhuongAnTatToanHopDong_SettlementId] FOREIGN KEY ([SettlementId]) REFERENCES [PhuongAnTatToanHopDong] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LichSuKetThucHopDong_TaiKhoan_ActorAccountId] FOREIGN KEY ([ActorAccountId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LichSuKetThucHopDong_YeuCauKetThucHopDong_EndRequestId] FOREIGN KEY ([EndRequestId]) REFERENCES [YeuCauKetThucHopDong] ([Id]) ON DELETE NO ACTION
);


CREATE INDEX [IX_HoSoKetThucHopDong_BillingFinalizedByAccountId] ON [HoSoKetThucHopDong] ([BillingFinalizedByAccountId]);


CREATE UNIQUE INDEX [IX_HoSoKetThucHopDong_ContractId] ON [HoSoKetThucHopDong] ([ContractId]);


CREATE INDEX [IX_HoSoKetThucHopDong_LandlordHandoverConfirmedByAccountId] ON [HoSoKetThucHopDong] ([LandlordHandoverConfirmedByAccountId]);


CREATE INDEX [IX_HoSoKetThucHopDong_ReleasedByAccountId] ON [HoSoKetThucHopDong] ([ReleasedByAccountId]);


CREATE INDEX [IX_HoSoKetThucHopDong_RoomId] ON [HoSoKetThucHopDong] ([RoomId]);


CREATE INDEX [IX_HoSoKetThucHopDong_TenantHandoverConfirmedByAccountId] ON [HoSoKetThucHopDong] ([TenantHandoverConfirmedByAccountId]);


CREATE INDEX [IX_KhoanMucTatToanHopDong_MonthlyBillId] ON [KhoanMucTatToanHopDong] ([MonthlyBillId]);


CREATE UNIQUE INDEX [IX_KhoanMucTatToanHopDong_SettlementId_MonthlyBillId] ON [KhoanMucTatToanHopDong] ([SettlementId], [MonthlyBillId]) WHERE [MonthlyBillId] IS NOT NULL;


CREATE INDEX [IX_LichSuKetThucHopDong_ActorAccountId] ON [LichSuKetThucHopDong] ([ActorAccountId]);


CREATE INDEX [IX_LichSuKetThucHopDong_ClosureId_OccurredAtUtc_Id] ON [LichSuKetThucHopDong] ([ClosureId], [OccurredAtUtc], [Id]);


CREATE INDEX [IX_LichSuKetThucHopDong_EndRequestId] ON [LichSuKetThucHopDong] ([EndRequestId]);


CREATE UNIQUE INDEX [IX_LichSuKetThucHopDong_IdempotencyKey] ON [LichSuKetThucHopDong] ([IdempotencyKey]);


CREATE INDEX [IX_LichSuKetThucHopDong_SettlementId] ON [LichSuKetThucHopDong] ([SettlementId]);


CREATE UNIQUE INDEX [IX_PhuongAnTatToanHopDong_ClosureId] ON [PhuongAnTatToanHopDong] ([ClosureId]) WHERE [Status]<>6;


CREATE UNIQUE INDEX [IX_PhuongAnTatToanHopDong_ClosureId_Revision] ON [PhuongAnTatToanHopDong] ([ClosureId], [Revision]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_CompletedByAccountId] ON [PhuongAnTatToanHopDong] ([CompletedByAccountId]);


CREATE UNIQUE INDEX [IX_PhuongAnTatToanHopDong_DepositId] ON [PhuongAnTatToanHopDong] ([DepositId]) WHERE [Status]<>6;


CREATE INDEX [IX_PhuongAnTatToanHopDong_ExtraReceivedByAccountId] ON [PhuongAnTatToanHopDong] ([ExtraReceivedByAccountId]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_ExtraReportedByAccountId] ON [PhuongAnTatToanHopDong] ([ExtraReportedByAccountId]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_LandlordAgreedByAccountId] ON [PhuongAnTatToanHopDong] ([LandlordAgreedByAccountId]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_ProposedByAccountId] ON [PhuongAnTatToanHopDong] ([ProposedByAccountId]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_RefundReceivedByAccountId] ON [PhuongAnTatToanHopDong] ([RefundReceivedByAccountId]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_RefundReportedByAccountId] ON [PhuongAnTatToanHopDong] ([RefundReportedByAccountId]);


CREATE INDEX [IX_PhuongAnTatToanHopDong_TenantAgreedByAccountId] ON [PhuongAnTatToanHopDong] ([TenantAgreedByAccountId]);


CREATE UNIQUE INDEX [IX_YeuCauKetThucHopDong_ClosureId] ON [YeuCauKetThucHopDong] ([ClosureId]) WHERE [Status] IN (0,1);


CREATE INDEX [IX_YeuCauKetThucHopDong_RequestedByAccountId] ON [YeuCauKetThucHopDong] ([RequestedByAccountId]);


CREATE INDEX [IX_YeuCauKetThucHopDong_RespondedByAccountId] ON [YeuCauKetThucHopDong] ([RespondedByAccountId]);

END;
DECLARE @columns TABLE(T sysname,C sysname,TypeName sysname,MaxLength int,Nullable bit);
INSERT @columns VALUES
(N'HoSoKetThucHopDong',N'Id',N'int',0,0),
(N'HoSoKetThucHopDong',N'ActualHandoverAtUtc',N'datetime2',0,1),
(N'HoSoKetThucHopDong',N'BillingFinalizedAtUtc',N'datetime2',0,1),
(N'HoSoKetThucHopDong',N'BillingFinalizedByAccountId',N'int',0,1),
(N'HoSoKetThucHopDong',N'BillingRevision',N'int',0,0),
(N'HoSoKetThucHopDong',N'ClosingElectricity',N'decimal',0,1),
(N'HoSoKetThucHopDong',N'ClosingWater',N'decimal',0,1),
(N'HoSoKetThucHopDong',N'Condition',N'nvarchar',4000,1),
(N'HoSoKetThucHopDong',N'ContractId',N'int',0,0),
(N'HoSoKetThucHopDong',N'CreatedAtUtc',N'datetime2',0,0),
(N'HoSoKetThucHopDong',N'DamageNotes',N'nvarchar',4000,1),
(N'HoSoKetThucHopDong',N'EffectiveEndAtUtc',N'datetime2',0,1),
(N'HoSoKetThucHopDong',N'EndKind',N'tinyint',0,1),
(N'HoSoKetThucHopDong',N'EvidenceJson',N'nvarchar',-1,1),
(N'HoSoKetThucHopDong',N'HandoverKind',N'tinyint',0,1),
(N'HoSoKetThucHopDong',N'HandoverRevision',N'int',0,0),
(N'HoSoKetThucHopDong',N'HandoverStatus',N'tinyint',0,0),
(N'HoSoKetThucHopDong',N'LandlordHandoverConfirmedAtUtc',N'datetime2',0,1),
(N'HoSoKetThucHopDong',N'LandlordHandoverConfirmedByAccountId',N'int',0,1),
(N'HoSoKetThucHopDong',N'LastRentalDate',N'date',0,1),
(N'HoSoKetThucHopDong',N'ReleasedAtUtc',N'datetime2',0,1),
(N'HoSoKetThucHopDong',N'ReleasedByAccountId',N'int',0,1),
(N'HoSoKetThucHopDong',N'RoomId',N'int',0,0),
(N'HoSoKetThucHopDong',N'RowVersion',N'timestamp',0,0),
(N'HoSoKetThucHopDong',N'TenantHandoverConfirmedAtUtc',N'datetime2',0,1),
(N'HoSoKetThucHopDong',N'TenantHandoverConfirmedByAccountId',N'int',0,1),
(N'HoSoKetThucHopDong',N'UpdatedAtUtc',N'datetime2',0,1),
(N'KhoanMucTatToanHopDong',N'Id',N'int',0,0),
(N'KhoanMucTatToanHopDong',N'Amount',N'decimal',0,0),
(N'KhoanMucTatToanHopDong',N'BillStatusSnapshot',N'int',0,1),
(N'KhoanMucTatToanHopDong',N'BillTotalSnapshot',N'decimal',0,1),
(N'KhoanMucTatToanHopDong',N'BillUpdatedAtSnapshot',N'datetime2',0,1),
(N'KhoanMucTatToanHopDong',N'DepositAppliedAmount',N'decimal',0,0),
(N'KhoanMucTatToanHopDong',N'Description',N'nvarchar',2000,0),
(N'KhoanMucTatToanHopDong',N'EvidenceJson',N'nvarchar',-1,1),
(N'KhoanMucTatToanHopDong',N'Kind',N'tinyint',0,0),
(N'KhoanMucTatToanHopDong',N'MonthlyBillId',N'int',0,1),
(N'KhoanMucTatToanHopDong',N'RowVersion',N'timestamp',0,0),
(N'KhoanMucTatToanHopDong',N'SettlementId',N'int',0,0),
(N'LichSuKetThucHopDong',N'Id',N'bigint',0,0),
(N'LichSuKetThucHopDong',N'ActorAccountId',N'int',0,1),
(N'LichSuKetThucHopDong',N'ClosureId',N'int',0,0),
(N'LichSuKetThucHopDong',N'EndRequestId',N'int',0,1),
(N'LichSuKetThucHopDong',N'EventType',N'nvarchar',160,0),
(N'LichSuKetThucHopDong',N'FromState',N'nvarchar',120,1),
(N'LichSuKetThucHopDong',N'IdempotencyKey',N'nvarchar',240,0),
(N'LichSuKetThucHopDong',N'OccurredAtUtc',N'datetime2',0,0),
(N'LichSuKetThucHopDong',N'Reason',N'nvarchar',4000,1),
(N'LichSuKetThucHopDong',N'SettlementId',N'int',0,1),
(N'LichSuKetThucHopDong',N'SnapshotJson',N'nvarchar',-1,1),
(N'LichSuKetThucHopDong',N'Source',N'tinyint',0,0),
(N'LichSuKetThucHopDong',N'ToState',N'nvarchar',120,1),
(N'PhuongAnTatToanHopDong',N'Id',N'int',0,0),
(N'PhuongAnTatToanHopDong',N'BillingRevisionSnapshot',N'int',0,0),
(N'PhuongAnTatToanHopDong',N'ClosureId',N'int',0,0),
(N'PhuongAnTatToanHopDong',N'CompletedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'CompletedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'CreatedAtUtc',N'datetime2',0,0),
(N'PhuongAnTatToanHopDong',N'DepositAmountSnapshot',N'decimal',0,0),
(N'PhuongAnTatToanHopDong',N'DepositId',N'int',0,0),
(N'PhuongAnTatToanHopDong',N'ExtraDue',N'decimal',0,0),
(N'PhuongAnTatToanHopDong',N'ExtraReceivedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'ExtraReceivedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'ExtraReference',N'nvarchar',1000,1),
(N'PhuongAnTatToanHopDong',N'ExtraReportedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'ExtraReportedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'LandlordAgreedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'LandlordAgreedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'Note',N'nvarchar',4000,1),
(N'PhuongAnTatToanHopDong',N'ProposedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'ProposedByAccountId',N'int',0,0),
(N'PhuongAnTatToanHopDong',N'RefundDue',N'decimal',0,0),
(N'PhuongAnTatToanHopDong',N'RefundReceivedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'RefundReceivedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'RefundReference',N'nvarchar',1000,1),
(N'PhuongAnTatToanHopDong',N'RefundReportedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'RefundReportedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'Revision',N'int',0,0),
(N'PhuongAnTatToanHopDong',N'RowVersion',N'timestamp',0,0),
(N'PhuongAnTatToanHopDong',N'Status',N'tinyint',0,0),
(N'PhuongAnTatToanHopDong',N'TenantAgreedAtUtc',N'datetime2',0,1),
(N'PhuongAnTatToanHopDong',N'TenantAgreedByAccountId',N'int',0,1),
(N'PhuongAnTatToanHopDong',N'TotalCharges',N'decimal',0,0),
(N'YeuCauKetThucHopDong',N'Id',N'int',0,0),
(N'YeuCauKetThucHopDong',N'AgreedEffectiveEndAtUtc',N'datetime2',0,1),
(N'YeuCauKetThucHopDong',N'ClosureId',N'int',0,0),
(N'YeuCauKetThucHopDong',N'EffectiveAtUtc',N'datetime2',0,1),
(N'YeuCauKetThucHopDong',N'ProposedEffectiveEndAtUtc',N'datetime2',0,0),
(N'YeuCauKetThucHopDong',N'Reason',N'nvarchar',2000,0),
(N'YeuCauKetThucHopDong',N'RequestedAtUtc',N'datetime2',0,0),
(N'YeuCauKetThucHopDong',N'RequestedByAccountId',N'int',0,0),
(N'YeuCauKetThucHopDong',N'RespondedAtUtc',N'datetime2',0,1),
(N'YeuCauKetThucHopDong',N'RespondedByAccountId',N'int',0,1),
(N'YeuCauKetThucHopDong',N'ResponseNote',N'nvarchar',2000,1),
(N'YeuCauKetThucHopDong',N'RowVersion',N'timestamp',0,0),
(N'YeuCauKetThucHopDong',N'Status',N'tinyint',0,0);
IF EXISTS(SELECT 1 FROM @columns e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.C
WHERE c.column_id IS NULL OR TYPE_NAME(c.system_type_id)<>e.TypeName OR c.is_nullable<>e.Nullable OR (e.MaxLength<>0 AND c.max_length<>e.MaxLength)
OR (e.TypeName='decimal' AND (c.precision<>18 OR c.scale<>2)) OR (e.TypeName='datetime2' AND c.scale<>7) OR (e.C='Id' AND c.is_identity<>1))
    THROW 51065,'Incompatible closure columns. No automatic repair.',1;
IF EXISTS(SELECT 1 FROM sys.columns c JOIN @columns e ON c.object_id=OBJECT_ID(N'dbo.'+e.T) GROUP BY c.object_id,c.name HAVING MAX(CASE WHEN c.name=e.C THEN 1 ELSE 0 END)=0)
    THROW 51066,'Unexpected closure columns.',1;
DECLARE @indexes TABLE(T sysname,N sysname,U bit,F nvarchar(max),Cols nvarchar(max));
INSERT @indexes VALUES
(N'HoSoKetThucHopDong',N'IX_HoSoKetThucHopDong_BillingFinalizedByAccountId',0,N'',N'BillingFinalizedByAccountId'),
(N'HoSoKetThucHopDong',N'IX_HoSoKetThucHopDong_ContractId',1,N'',N'ContractId'),
(N'HoSoKetThucHopDong',N'IX_HoSoKetThucHopDong_LandlordHandoverConfirmedByAccountId',0,N'',N'LandlordHandoverConfirmedByAccountId'),
(N'HoSoKetThucHopDong',N'IX_HoSoKetThucHopDong_ReleasedByAccountId',0,N'',N'ReleasedByAccountId'),
(N'HoSoKetThucHopDong',N'IX_HoSoKetThucHopDong_RoomId',0,N'',N'RoomId'),
(N'HoSoKetThucHopDong',N'IX_HoSoKetThucHopDong_TenantHandoverConfirmedByAccountId',0,N'',N'TenantHandoverConfirmedByAccountId'),
(N'KhoanMucTatToanHopDong',N'IX_KhoanMucTatToanHopDong_MonthlyBillId',0,N'',N'MonthlyBillId'),
(N'KhoanMucTatToanHopDong',N'IX_KhoanMucTatToanHopDong_SettlementId_MonthlyBillId',1,N'MonthlyBillIdISNOTNULL',N'SettlementId,MonthlyBillId'),
(N'LichSuKetThucHopDong',N'IX_LichSuKetThucHopDong_ActorAccountId',0,N'',N'ActorAccountId'),
(N'LichSuKetThucHopDong',N'IX_LichSuKetThucHopDong_ClosureId_OccurredAtUtc_Id',0,N'',N'ClosureId,OccurredAtUtc,Id'),
(N'LichSuKetThucHopDong',N'IX_LichSuKetThucHopDong_EndRequestId',0,N'',N'EndRequestId'),
(N'LichSuKetThucHopDong',N'IX_LichSuKetThucHopDong_IdempotencyKey',1,N'',N'IdempotencyKey'),
(N'LichSuKetThucHopDong',N'IX_LichSuKetThucHopDong_SettlementId',0,N'',N'SettlementId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_ClosureId',1,N'Status<>6',N'ClosureId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_ClosureId_Revision',1,N'',N'ClosureId,Revision'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_CompletedByAccountId',0,N'',N'CompletedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_DepositId',1,N'Status<>6',N'DepositId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_ExtraReceivedByAccountId',0,N'',N'ExtraReceivedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_ExtraReportedByAccountId',0,N'',N'ExtraReportedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_LandlordAgreedByAccountId',0,N'',N'LandlordAgreedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_ProposedByAccountId',0,N'',N'ProposedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_RefundReceivedByAccountId',0,N'',N'RefundReceivedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_RefundReportedByAccountId',0,N'',N'RefundReportedByAccountId'),
(N'PhuongAnTatToanHopDong',N'IX_PhuongAnTatToanHopDong_TenantAgreedByAccountId',0,N'',N'TenantAgreedByAccountId'),
(N'YeuCauKetThucHopDong',N'IX_YeuCauKetThucHopDong_ClosureId',1,N'StatusIN0,1',N'ClosureId'),
(N'YeuCauKetThucHopDong',N'IX_YeuCauKetThucHopDong_RequestedByAccountId',0,N'',N'RequestedByAccountId'),
(N'YeuCauKetThucHopDong',N'IX_YeuCauKetThucHopDong_RespondedByAccountId',0,N'',N'RespondedByAccountId');
IF EXISTS(SELECT 1 FROM @indexes e LEFT JOIN sys.indexes i ON i.object_id=OBJECT_ID(N'dbo.'+e.T) AND i.name=e.N
WHERE i.index_id IS NULL OR i.is_unique<>e.U OR i.is_disabled=1 OR REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(i.filter_definition,''),' ',''),'(',''),')',''),'[',''),']','')<>e.F
OR e.Cols<>(SELECT STRING_AGG(CONVERT(nvarchar(max),c.name),',') WITHIN GROUP(ORDER BY ic.key_ordinal) FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0))
    THROW 51067,'Missing or changed closure indexes.',1;
DECLARE @checks TABLE(T sysname,N sysname,D nvarchar(max));
INSERT @checks VALUES
(N'HoSoKetThucHopDong',N'CK_Closure_Confirmed',N'([HandoverStatus]<>(3) OR [TenantHandoverConfirmedByAccountId] IS NOT NULL AND [TenantHandoverConfirmedAtUtc] IS NOT NULL AND [LandlordHandoverConfirmedByAccountId] IS NOT NULL AND [LandlordHandoverConfirmedAtUtc] IS NOT NULL AND [HandoverKind] IS NOT NULL AND ([HandoverKind]=(1) OR [ActualHandoverAtUtc] IS NOT NULL AND [ClosingElectricity] IS NOT NULL AND [ClosingWater] IS NOT NULL))'),
(N'HoSoKetThucHopDong',N'CK_Closure_Json',N'([EvidenceJson] IS NULL OR isjson([EvidenceJson])=(1))'),
(N'HoSoKetThucHopDong',N'CK_Closure_Meters',N'(([ClosingElectricity] IS NULL OR [ClosingElectricity]>=(0)) AND ([ClosingWater] IS NULL OR [ClosingWater]>=(0)))'),
(N'HoSoKetThucHopDong',N'CK_Closure_Release',N'([ReleasedAtUtc] IS NULL OR [ReleasedByAccountId] IS NOT NULL AND [EffectiveEndAtUtc] IS NOT NULL AND [HandoverStatus]=(3) AND [BillingFinalizedAtUtc] IS NOT NULL)'),
(N'HoSoKetThucHopDong',N'CK_Closure_State',N'([HandoverStatus]>=(0) AND [HandoverStatus]<=(4) AND ([EndKind] IS NULL OR [EndKind]>=(0) AND [EndKind]<=(2)) AND ([HandoverKind] IS NULL OR ([HandoverKind]=(1) OR [HandoverKind]=(0))) AND [HandoverRevision]>=(0) AND [BillingRevision]>=(0))'),
(N'KhoanMucTatToanHopDong',N'CK_SettlementLine_Amount',N'([Amount]>(0) AND [DepositAppliedAmount]>=(0) AND [DepositAppliedAmount]<=[Amount])'),
(N'KhoanMucTatToanHopDong',N'CK_SettlementLine_Json',N'([EvidenceJson] IS NULL OR isjson([EvidenceJson])=(1))'),
(N'KhoanMucTatToanHopDong',N'CK_SettlementLine_Kind',N'([Kind]=(0) AND [MonthlyBillId] IS NOT NULL AND [BillStatusSnapshot] IS NOT NULL AND [BillTotalSnapshot] IS NOT NULL OR [Kind]=(1) AND [MonthlyBillId] IS NULL AND [EvidenceJson] IS NOT NULL)'),
(N'LichSuKetThucHopDong',N'CK_ClosureEvent_Json',N'([SnapshotJson] IS NULL OR isjson([SnapshotJson])=(1))'),
(N'LichSuKetThucHopDong',N'CK_ClosureEvent_Source',N'(([Source]=(1) OR [Source]=(0)) AND ([Source]=(1) OR [ActorAccountId] IS NOT NULL))'),
(N'PhuongAnTatToanHopDong',N'CK_Settlement_Agreed',N'(NOT ([Status]=(4) OR [Status]=(3) OR [Status]=(2)) OR [TenantAgreedByAccountId] IS NOT NULL AND [TenantAgreedAtUtc] IS NOT NULL AND [LandlordAgreedByAccountId] IS NOT NULL AND [LandlordAgreedAtUtc] IS NOT NULL)'),
(N'PhuongAnTatToanHopDong',N'CK_Settlement_Completed',N'([Status]<>(4) OR [CompletedAtUtc] IS NOT NULL AND [CompletedByAccountId] IS NOT NULL AND ([RefundDue]=(0) OR [RefundReference] IS NOT NULL AND [RefundReportedAtUtc] IS NOT NULL AND [RefundReportedByAccountId] IS NOT NULL AND [RefundReceivedAtUtc] IS NOT NULL AND [RefundReceivedByAccountId] IS NOT NULL) AND ([ExtraDue]=(0) OR [ExtraReference] IS NOT NULL AND [ExtraReportedAtUtc] IS NOT NULL AND [ExtraReportedByAccountId] IS NOT NULL AND [ExtraReceivedAtUtc] IS NOT NULL AND [ExtraReceivedByAccountId] IS NOT NULL))'),
(N'PhuongAnTatToanHopDong',N'CK_Settlement_Money',N'([DepositAmountSnapshot]>=(0) AND [TotalCharges]>=(0) AND [RefundDue]>=(0) AND [ExtraDue]>=(0) AND ([RefundDue]=(0) OR [ExtraDue]=(0)) AND ([DepositAmountSnapshot]+[ExtraDue])=([TotalCharges]+[RefundDue]))'),
(N'PhuongAnTatToanHopDong',N'CK_Settlement_State',N'([Status]>=(0) AND [Status]<=(6) AND [Revision]>(0) AND [BillingRevisionSnapshot]>=(0))'),
(N'YeuCauKetThucHopDong',N'CK_EndRequest_Agreed',N'(NOT ([Status]=(5) OR [Status]=(1)) OR [RespondedByAccountId] IS NOT NULL AND [RespondedAtUtc] IS NOT NULL AND [AgreedEffectiveEndAtUtc] IS NOT NULL)'),
(N'YeuCauKetThucHopDong',N'CK_EndRequest_Effective',N'([Status]<>(5) OR [EffectiveAtUtc] IS NOT NULL)'),
(N'YeuCauKetThucHopDong',N'CK_EndRequest_State',N'([Status]>=(0) AND [Status]<=(5) AND len(ltrim(rtrim([Reason])))>=(5))');
IF EXISTS(SELECT 1 FROM @checks e LEFT JOIN sys.check_constraints c ON c.parent_object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.N
WHERE c.object_id IS NULL OR c.is_disabled=1 OR c.is_not_trusted=1 OR REPLACE(c.definition,' ','')<>REPLACE(e.D,' ',''))
    THROW 51068,'Missing or changed closure check constraints.',1;
DECLARE @fks TABLE(T sysname,N sysname,P sysname,Cols nvarchar(max),PCols nvarchar(max));
INSERT @fks VALUES
(N'HoSoKetThucHopDong',N'FK_HoSoKetThucHopDong_HopDongThue_ContractId',N'HopDongThue',N'ContractId',N'Id'),
(N'HoSoKetThucHopDong',N'FK_HoSoKetThucHopDong_PhongTro_RoomId',N'PhongTro',N'RoomId',N'Id'),
(N'HoSoKetThucHopDong',N'FK_HoSoKetThucHopDong_TaiKhoan_BillingFinalizedByAccountId',N'TaiKhoan',N'BillingFinalizedByAccountId',N'Id'),
(N'HoSoKetThucHopDong',N'FK_HoSoKetThucHopDong_TaiKhoan_LandlordHandoverConfirmedByAccountId',N'TaiKhoan',N'LandlordHandoverConfirmedByAccountId',N'Id'),
(N'HoSoKetThucHopDong',N'FK_HoSoKetThucHopDong_TaiKhoan_ReleasedByAccountId',N'TaiKhoan',N'ReleasedByAccountId',N'Id'),
(N'HoSoKetThucHopDong',N'FK_HoSoKetThucHopDong_TaiKhoan_TenantHandoverConfirmedByAccountId',N'TaiKhoan',N'TenantHandoverConfirmedByAccountId',N'Id'),
(N'KhoanMucTatToanHopDong',N'FK_KhoanMucTatToanHopDong_HoaDonHangThang_MonthlyBillId',N'HoaDonHangThang',N'MonthlyBillId',N'Id'),
(N'KhoanMucTatToanHopDong',N'FK_KhoanMucTatToanHopDong_PhuongAnTatToanHopDong_SettlementId',N'PhuongAnTatToanHopDong',N'SettlementId',N'Id'),
(N'LichSuKetThucHopDong',N'FK_LichSuKetThucHopDong_HoSoKetThucHopDong_ClosureId',N'HoSoKetThucHopDong',N'ClosureId',N'Id'),
(N'LichSuKetThucHopDong',N'FK_LichSuKetThucHopDong_PhuongAnTatToanHopDong_SettlementId',N'PhuongAnTatToanHopDong',N'SettlementId',N'Id'),
(N'LichSuKetThucHopDong',N'FK_LichSuKetThucHopDong_TaiKhoan_ActorAccountId',N'TaiKhoan',N'ActorAccountId',N'Id'),
(N'LichSuKetThucHopDong',N'FK_LichSuKetThucHopDong_YeuCauKetThucHopDong_EndRequestId',N'YeuCauKetThucHopDong',N'EndRequestId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_DatCoc_DepositId',N'DatCoc',N'DepositId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_HoSoKetThucHopDong_ClosureId',N'HoSoKetThucHopDong',N'ClosureId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_CompletedByAccountId',N'TaiKhoan',N'CompletedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_ExtraReceivedByAccountId',N'TaiKhoan',N'ExtraReceivedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_ExtraReportedByAccountId',N'TaiKhoan',N'ExtraReportedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_LandlordAgreedByAccountId',N'TaiKhoan',N'LandlordAgreedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_ProposedByAccountId',N'TaiKhoan',N'ProposedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_RefundReceivedByAccountId',N'TaiKhoan',N'RefundReceivedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_RefundReportedByAccountId',N'TaiKhoan',N'RefundReportedByAccountId',N'Id'),
(N'PhuongAnTatToanHopDong',N'FK_PhuongAnTatToanHopDong_TaiKhoan_TenantAgreedByAccountId',N'TaiKhoan',N'TenantAgreedByAccountId',N'Id'),
(N'YeuCauKetThucHopDong',N'FK_YeuCauKetThucHopDong_HoSoKetThucHopDong_ClosureId',N'HoSoKetThucHopDong',N'ClosureId',N'Id'),
(N'YeuCauKetThucHopDong',N'FK_YeuCauKetThucHopDong_TaiKhoan_RequestedByAccountId',N'TaiKhoan',N'RequestedByAccountId',N'Id'),
(N'YeuCauKetThucHopDong',N'FK_YeuCauKetThucHopDong_TaiKhoan_RespondedByAccountId',N'TaiKhoan',N'RespondedByAccountId',N'Id');
IF EXISTS(SELECT 1 FROM @fks e LEFT JOIN sys.foreign_keys f ON f.parent_object_id=OBJECT_ID(N'dbo.'+e.T) AND f.name=e.N
WHERE f.object_id IS NULL OR f.is_disabled=1 OR f.is_not_trusted=1 OR f.delete_referential_action<>0 OR f.update_referential_action<>0 OR f.referenced_object_id<>OBJECT_ID(N'dbo.'+e.P)
OR e.Cols<>(SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(fc.parent_object_id,fc.parent_column_id)),',') WITHIN GROUP(ORDER BY fc.constraint_column_id) FROM sys.foreign_key_columns fc WHERE fc.constraint_object_id=f.object_id)
OR e.PCols<>(SELECT STRING_AGG(CONVERT(nvarchar(max),COL_NAME(fc.referenced_object_id,fc.referenced_column_id)),',') WITHIN GROUP(ORDER BY fc.constraint_column_id) FROM sys.foreign_key_columns fc WHERE fc.constraint_object_id=f.object_id))
    THROW 51069,'Missing or changed closure foreign keys.',1;
COMMIT;
PRINT 'Contract closure schema verified. Existing business rows unchanged.';
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;