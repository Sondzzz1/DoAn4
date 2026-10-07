-- Generated current EF schema. Select the target database before running.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL
BEGIN
BEGIN TRANSACTION;
CREATE TABLE [__UnusedRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(255) NULL,
    CONSTRAINT [PK___UnusedRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [DanhGiaPhong] (
    [Id] int NOT NULL IDENTITY,
    [ContractId] int NOT NULL,
    [PostId] int NOT NULL,
    [TenantAccountId] int NOT NULL,
    [SoSao] int NOT NULL,
    [NhanXet] nvarchar(2000) NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_DanhGiaPhong] PRIMARY KEY ([Id])
);

CREATE TABLE [DanhMucPhong] (
    [Id] int NOT NULL IDENTITY,
    [TenDanhMuc] nvarchar(100) NOT NULL,
    [MoTa] nvarchar(500) NULL,
    [AnhDaiDien] nvarchar(500) NULL,
    [TrangThai] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_DanhMucPhong] PRIMARY KEY ([Id])
);

CREATE TABLE [SuCo] (
    [Id] int NOT NULL IDENTITY,
    [ContractId] int NOT NULL,
    [ReporterAccountId] int NOT NULL,
    [TieuDe] nvarchar(200) NOT NULL,
    [MoTa] nvarchar(2000) NOT NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [HuongXuLy] nvarchar(2000) NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_SuCo] PRIMARY KEY ([Id])
);

CREATE TABLE [TaiKhoan] (
    [Id] int NOT NULL IDENTITY,
    [TenDangNhap] nvarchar(100) NOT NULL,
    [MatKhauHash] nvarchar(500) NOT NULL,
    [HoTen] nvarchar(150) NOT NULL,
    [SoDienThoai] nvarchar(20) NULL,
    [Email] nvarchar(150) NULL,
    [AnhDaiDien] nvarchar(500) NULL,
    [VaiTro] int NOT NULL DEFAULT 1,
    [TrangThai] bit NOT NULL DEFAULT CAST(1 AS bit),
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_TaiKhoan] PRIMARY KEY ([Id])
);

CREATE TABLE [TienNghi] (
    [Id] int NOT NULL IDENTITY,
    [TenTienNghi] nvarchar(100) NOT NULL,
    [Icon] nvarchar(200) NULL,
    [MoTa] nvarchar(300) NULL,
    [TrangThai] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_TienNghi] PRIMARY KEY ([Id])
);

CREATE TABLE [BaiViet] (
    [Id] int NOT NULL IDENTITY,
    [TieuDe] nvarchar(300) NOT NULL,
    [Slug] nvarchar(350) NOT NULL,
    [TomTat] nvarchar(1000) NULL,
    [NoiDung] nvarchar(max) NOT NULL,
    [AnhDaiDien] nvarchar(1000) NULL,
    [TacGiaId] int NOT NULL,
    [LuotXem] int NOT NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [NgayDang] datetime2 NULL,
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_BaiViet] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BaiViet_TaiKhoan_TacGiaId] FOREIGN KEY ([TacGiaId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ChuTro] (
    [Id] int NOT NULL IDENTITY,
    [TaiKhoanId] int NOT NULL,
    [GioiThieu] nvarchar(500) NULL,
    [DiaChi] nvarchar(300) NULL,
    [SoCanCuoc] nvarchar(20) NULL,
    [DaXacThuc] bit NOT NULL DEFAULT CAST(0 AS bit),
    [NgayXacThuc] datetime2 NULL,
    CONSTRAINT [PK_ChuTro] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChuTro_TaiKhoan_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [NguoiDung] (
    [Id] int NOT NULL IDENTITY,
    [TaiKhoanId] int NOT NULL,
    [NgaySinh] date NULL,
    [GioiTinh] nvarchar(20) NULL,
    [NgheNghiep] nvarchar(100) NULL,
    [DiaChiHienTai] nvarchar(300) NULL,
    [GioiThieu] nvarchar(500) NULL,
    CONSTRAINT [PK_NguoiDung] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NguoiDung_TaiKhoan_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ThongBao] (
    [Id] int NOT NULL IDENTITY,
    [TaiKhoanId] int NOT NULL,
    [TieuDe] nvarchar(200) NOT NULL,
    [NoiDung] nvarchar(1000) NOT NULL,
    [LoaiThongBao] int NULL,
    [LienKet] nvarchar(500) NULL,
    [DaDoc] bit NOT NULL DEFAULT CAST(0 AS bit),
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_ThongBao] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ThongBao_TaiKhoan_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BinhLuan] (
    [Id] int NOT NULL IDENTITY,
    [BaiVietId] int NOT NULL,
    [TaiKhoanId] int NOT NULL,
    [NoiDung] nvarchar(1000) NOT NULL,
    [TrangThai] bit NOT NULL DEFAULT CAST(1 AS bit),
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_BinhLuan] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BinhLuan_BaiViet_BaiVietId] FOREIGN KEY ([BaiVietId]) REFERENCES [BaiViet] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BinhLuan_TaiKhoan_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PhongTro] (
    [Id] int NOT NULL IDENTITY,
    [ChuTroId] int NOT NULL,
    [DanhMucId] int NOT NULL,
    [TenPhong] nvarchar(200) NOT NULL,
    [MoTa] nvarchar(max) NULL,
    [GiaThue] decimal(18,2) NOT NULL,
    [DienTich] decimal(10,2) NOT NULL,
    [SoNguoiToiDa] int NOT NULL,
    [SoNguoiHienTai] int NOT NULL,
    [SoPhongNgu] int NULL,
    [SoPhongTam] int NULL,
    [Tang] int NULL,
    [DiaChi] nvarchar(300) NOT NULL,
    [PhuongXa] nvarchar(100) NULL,
    [QuanHuyen] nvarchar(100) NULL,
    [TinhThanh] nvarchar(100) NULL,
    [ViDo] decimal(10,7) NULL,
    [KinhDo] decimal(10,7) NULL,
    [TienDien] decimal(18,2) NULL,
    [TienNuoc] decimal(18,2) NULL,
    [PhiDichVu] decimal(18,2) NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_PhongTro] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PhongTro_ChuTro_ChuTroId] FOREIGN KEY ([ChuTroId]) REFERENCES [ChuTro] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhongTro_DanhMucPhong_DanhMucId] FOREIGN KEY ([DanhMucId]) REFERENCES [DanhMucPhong] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [HinhAnhPhong] (
    [Id] int NOT NULL IDENTITY,
    [PhongTroId] int NOT NULL,
    [DuongDan] nvarchar(1000) NOT NULL,
    [LaAnhDaiDien] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ThuTu] int NOT NULL DEFAULT 0,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_HinhAnhPhong] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HinhAnhPhong_PhongTro_PhongTroId] FOREIGN KEY ([PhongTroId]) REFERENCES [PhongTro] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PhongTro_TienNghi] (
    [PhongTroId] int NOT NULL,
    [TienNghiId] int NOT NULL,
    CONSTRAINT [PK_PhongTro_TienNghi] PRIMARY KEY ([PhongTroId], [TienNghiId]),
    CONSTRAINT [FK_PhongTro_TienNghi_PhongTro_PhongTroId] FOREIGN KEY ([PhongTroId]) REFERENCES [PhongTro] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PhongTro_TienNghi_TienNghi_TienNghiId] FOREIGN KEY ([TienNghiId]) REFERENCES [TienNghi] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TinDang] (
    [Id] int NOT NULL IDENTITY,
    [PhongTroId] int NOT NULL,
    [ChuTroId] int NOT NULL,
    [TieuDe] nvarchar(300) NOT NULL,
    [NoiDung] nvarchar(max) NOT NULL,
    [GiaHienThi] decimal(18,2) NOT NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [LyDoTuChoi] nvarchar(500) NULL,
    [LuotXem] int NOT NULL,
    [NgayDang] datetime2 NULL,
    [NgayHetHan] datetime2 NULL,
    [NgayDuyet] datetime2 NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_TinDang] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TinDang_ChuTro_ChuTroId] FOREIGN KEY ([ChuTroId]) REFERENCES [ChuTro] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TinDang_PhongTro_PhongTroId] FOREIGN KEY ([PhongTroId]) REFERENCES [PhongTro] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BaoCaoTinDang] (
    [Id] int NOT NULL IDENTITY,
    [TinDangId] int NOT NULL,
    [NguoiBaoCaoId] int NOT NULL,
    [LyDo] nvarchar(500) NOT NULL,
    [MoTa] nvarchar(1000) NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [NgayBaoCao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayXuLy] datetime2 NULL,
    [NguoiXuLyId] int NULL,
    CONSTRAINT [PK_BaoCaoTinDang] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BaoCaoTinDang_TaiKhoan_NguoiBaoCaoId] FOREIGN KEY ([NguoiBaoCaoId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BaoCaoTinDang_TaiKhoan_NguoiXuLyId] FOREIGN KEY ([NguoiXuLyId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BaoCaoTinDang_TinDang_TinDangId] FOREIGN KEY ([TinDangId]) REFERENCES [TinDang] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LichHenXemPhong] (
    [Id] int NOT NULL IDENTITY,
    [NguoiDungId] int NOT NULL,
    [TinDangId] int NOT NULL,
    [ChuTroId] int NOT NULL,
    [ThoiGianHen] datetime2 NOT NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [NoiDung] nvarchar(500) NULL,
    [LyDoTuChoi] nvarchar(500) NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_LichHenXemPhong] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LichHenXemPhong_ChuTro_ChuTroId] FOREIGN KEY ([ChuTroId]) REFERENCES [ChuTro] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LichHenXemPhong_NguoiDung_NguoiDungId] FOREIGN KEY ([NguoiDungId]) REFERENCES [NguoiDung] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LichHenXemPhong_TinDang_TinDangId] FOREIGN KEY ([TinDangId]) REFERENCES [TinDang] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TinNhan] (
    [Id] int NOT NULL IDENTITY,
    [NguoiGuiId] int NOT NULL,
    [NguoiNhanId] int NOT NULL,
    [TinDangId] int NULL,
    [NoiDung] nvarchar(2000) NOT NULL,
    [DaDoc] bit NOT NULL DEFAULT CAST(0 AS bit),
    [NgayGui] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_TinNhan] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TinNhan_TaiKhoan_NguoiGuiId] FOREIGN KEY ([NguoiGuiId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TinNhan_TaiKhoan_NguoiNhanId] FOREIGN KEY ([NguoiNhanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TinNhan_TinDang_TinDangId] FOREIGN KEY ([TinDangId]) REFERENCES [TinDang] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [YeuCauThuePhong] (
    [Id] int NOT NULL IDENTITY,
    [PostId] int NOT NULL,
    [TenantAccountId] int NOT NULL,
    [LandlordAccountId] int NOT NULL,
    [GhiChu] nvarchar(1000) NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_YeuCauThuePhong] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_YeuCauThuePhong_TinDang_PostId] FOREIGN KEY ([PostId]) REFERENCES [TinDang] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [YeuThich] (
    [Id] int NOT NULL IDENTITY,
    [NguoiDungId] int NOT NULL,
    [TinDangId] int NOT NULL,
    [NgayLuu] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_YeuThich] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_YeuThich_NguoiDung_NguoiDungId] FOREIGN KEY ([NguoiDungId]) REFERENCES [NguoiDung] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_YeuThich_TinDang_TinDangId] FOREIGN KEY ([TinDangId]) REFERENCES [TinDang] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DatCoc] (
    [Id] int NOT NULL IDENTITY,
    [RentalRequestId] int NOT NULL,
    [TenantAccountId] int NOT NULL,
    [LandlordAccountId] int NOT NULL,
    [SoTien] decimal(18,2) NOT NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [HanThanhToan] datetime2 NULL,
    [NgayThanhToan] datetime2 NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_DatCoc] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DatCoc_YeuCauThuePhong_RentalRequestId] FOREIGN KEY ([RentalRequestId]) REFERENCES [YeuCauThuePhong] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [HopDongThue] (
    [Id] int NOT NULL IDENTITY,
    [RentalRequestId] int NOT NULL,
    [PostId] int NOT NULL,
    [TenantAccountId] int NOT NULL,
    [LandlordAccountId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [TienThueHangThang] decimal(18,2) NOT NULL,
    [TienDatCoc] decimal(18,2) NOT NULL,
    [GiaDien] decimal(18,2) NOT NULL,
    [GiaNuoc] decimal(18,2) NOT NULL,
    [PhiDichVu] decimal(18,2) NOT NULL,
    [DieuKhoan] nvarchar(4000) NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [NguoiThueDaXacNhan] bit NOT NULL,
    [ChuTroDaXacNhan] bit NOT NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_HopDongThue] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HopDongThue_TinDang_PostId] FOREIGN KEY ([PostId]) REFERENCES [TinDang] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HopDongThue_YeuCauThuePhong_RentalRequestId] FOREIGN KEY ([RentalRequestId]) REFERENCES [YeuCauThuePhong] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [HoaDonHangThang] (
    [Id] int NOT NULL IDENTITY,
    [HopDongId] int NOT NULL,
    [Thang] int NOT NULL,
    [Nam] int NOT NULL,
    [SoDienCu] decimal(18,2) NOT NULL,
    [SoDienMoi] decimal(18,2) NOT NULL,
    [GiaDien] decimal(18,2) NOT NULL,
    [SoNuocCu] decimal(18,2) NOT NULL,
    [SoNuocMoi] decimal(18,2) NOT NULL,
    [GiaNuoc] decimal(18,2) NOT NULL,
    [TienPhong] decimal(18,2) NOT NULL,
    [PhiDichVu] decimal(18,2) NOT NULL DEFAULT 0.0,
    [ChiPhiKhac] decimal(18,2) NOT NULL DEFAULT 0.0,
    [GhiChuChiPhiKhac] nvarchar(500) NULL,
    [TongTien] decimal(18,2) NOT NULL,
    [TrangThai] int NOT NULL DEFAULT 0,
    [HanThanhToan] datetime2 NULL,
    [NgayThanhToan] datetime2 NULL,
    [PhuongThucThanhToan] nvarchar(100) NULL,
    [GhiChu] nvarchar(1000) NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [NgayCapNhat] datetime2 NULL,
    CONSTRAINT [PK_HoaDonHangThang] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HoaDonHangThang_HopDongThue_HopDongId] FOREIGN KEY ([HopDongId]) REFERENCES [HopDongThue] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [GiaoDichThanhToan] (
    [Id] int NOT NULL IDENTITY,
    [DepositId] int NULL,
    [MonthlyBillId] int NULL,
    [TargetType] nvarchar(20) NOT NULL,
    [OrderId] nvarchar(100) NOT NULL,
    [TransactionCode] nvarchar(100) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL DEFAULT 0,
    [ResponseCode] nvarchar(20) NULL,
    [OrderInfo] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [ProcessedAt] datetime2 NULL,
    CONSTRAINT [PK_GiaoDichThanhToan] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_GiaoDichThanhToan_ExactlyOneTarget] CHECK (([DepositId] IS NOT NULL AND [MonthlyBillId] IS NULL AND [TargetType] = N'Deposit') OR ([DepositId] IS NULL AND [MonthlyBillId] IS NOT NULL AND [TargetType] = N'MonthlyBill')),
    CONSTRAINT [FK_GiaoDichThanhToan_DatCoc_DepositId] FOREIGN KEY ([DepositId]) REFERENCES [DatCoc] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GiaoDichThanhToan_HoaDonHangThang_MonthlyBillId] FOREIGN KEY ([MonthlyBillId]) REFERENCES [HoaDonHangThang] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX___UnusedRoles_Name] ON [__UnusedRoles] ([Name]);

CREATE UNIQUE INDEX [IX_BaiViet_Slug] ON [BaiViet] ([Slug]);

CREATE INDEX [IX_BaiViet_TacGiaId] ON [BaiViet] ([TacGiaId]);

CREATE UNIQUE INDEX [IX_BaoCaoTinDang_NguoiBaoCaoId_TinDangId] ON [BaoCaoTinDang] ([NguoiBaoCaoId], [TinDangId]) WHERE [TrangThai] = 0;

CREATE INDEX [IX_BaoCaoTinDang_NguoiXuLyId] ON [BaoCaoTinDang] ([NguoiXuLyId]);

CREATE INDEX [IX_BaoCaoTinDang_TinDangId] ON [BaoCaoTinDang] ([TinDangId]);

CREATE INDEX [IX_BinhLuan_BaiVietId] ON [BinhLuan] ([BaiVietId]);

CREATE INDEX [IX_BinhLuan_TaiKhoanId] ON [BinhLuan] ([TaiKhoanId]);

CREATE UNIQUE INDEX [IX_ChuTro_SoCanCuoc] ON [ChuTro] ([SoCanCuoc]) WHERE [SoCanCuoc] IS NOT NULL;

CREATE UNIQUE INDEX [IX_ChuTro_TaiKhoanId] ON [ChuTro] ([TaiKhoanId]);

CREATE UNIQUE INDEX [IX_DanhGiaPhong_ContractId_TenantAccountId] ON [DanhGiaPhong] ([ContractId], [TenantAccountId]);

CREATE INDEX [IX_DanhGiaPhong_PostId] ON [DanhGiaPhong] ([PostId]);

CREATE UNIQUE INDEX [IX_DatCoc_RentalRequestId] ON [DatCoc] ([RentalRequestId]);

CREATE INDEX [IX_DatCoc_TrangThai_HanThanhToan] ON [DatCoc] ([TrangThai], [HanThanhToan]);

CREATE INDEX [IX_GiaoDichThanhToan_DepositId] ON [GiaoDichThanhToan] ([DepositId]);

CREATE INDEX [IX_GiaoDichThanhToan_MonthlyBillId] ON [GiaoDichThanhToan] ([MonthlyBillId]);

CREATE UNIQUE INDEX [IX_GiaoDichThanhToan_OrderId] ON [GiaoDichThanhToan] ([OrderId]);

CREATE UNIQUE INDEX [IX_GiaoDichThanhToan_TransactionCode] ON [GiaoDichThanhToan] ([TransactionCode]) WHERE [TransactionCode] IS NOT NULL;

CREATE INDEX [IX_HinhAnhPhong_PhongTroId] ON [HinhAnhPhong] ([PhongTroId]);

CREATE INDEX [IX_HinhAnhPhong_ThuTu] ON [HinhAnhPhong] ([ThuTu]);

CREATE UNIQUE INDEX [IX_HoaDonHangThang_HopDongId_Thang_Nam] ON [HoaDonHangThang] ([HopDongId], [Thang], [Nam]);

CREATE INDEX [IX_HopDongThue_PostId] ON [HopDongThue] ([PostId]);

CREATE INDEX [IX_HopDongThue_RentalRequestId] ON [HopDongThue] ([RentalRequestId]);

CREATE INDEX [IX_HopDongThue_TenantAccountId_LandlordAccountId] ON [HopDongThue] ([TenantAccountId], [LandlordAccountId]);

CREATE INDEX [IX_LichHenXemPhong_ChuTroId] ON [LichHenXemPhong] ([ChuTroId]);

CREATE INDEX [IX_LichHenXemPhong_NguoiDungId] ON [LichHenXemPhong] ([NguoiDungId]);

CREATE INDEX [IX_LichHenXemPhong_ThoiGianHen] ON [LichHenXemPhong] ([ThoiGianHen]);

CREATE INDEX [IX_LichHenXemPhong_TinDangId] ON [LichHenXemPhong] ([TinDangId]);

CREATE INDEX [IX_LichHenXemPhong_TrangThai] ON [LichHenXemPhong] ([TrangThai]);

CREATE UNIQUE INDEX [IX_NguoiDung_TaiKhoanId] ON [NguoiDung] ([TaiKhoanId]);

CREATE INDEX [IX_PhongTro_ChuTroId] ON [PhongTro] ([ChuTroId]);

CREATE INDEX [IX_PhongTro_DanhMucId] ON [PhongTro] ([DanhMucId]);

CREATE INDEX [IX_PhongTro_QuanHuyen] ON [PhongTro] ([QuanHuyen]);

CREATE INDEX [IX_PhongTro_TinhThanh] ON [PhongTro] ([TinhThanh]);

CREATE INDEX [IX_PhongTro_TrangThai] ON [PhongTro] ([TrangThai]);

CREATE INDEX [IX_PhongTro_TienNghi_TienNghiId] ON [PhongTro_TienNghi] ([TienNghiId]);

CREATE INDEX [IX_SuCo_ContractId] ON [SuCo] ([ContractId]);

CREATE UNIQUE INDEX [IX_TaiKhoan_Email] ON [TaiKhoan] ([Email]) WHERE [Email] IS NOT NULL;

CREATE UNIQUE INDEX [IX_TaiKhoan_TenDangNhap] ON [TaiKhoan] ([TenDangNhap]);

CREATE INDEX [IX_ThongBao_TaiKhoanId] ON [ThongBao] ([TaiKhoanId]);

CREATE UNIQUE INDEX [IX_TienNghi_TenTienNghi] ON [TienNghi] ([TenTienNghi]);

CREATE INDEX [IX_TinDang_ChuTroId] ON [TinDang] ([ChuTroId]);

CREATE INDEX [IX_TinDang_TrangThai] ON [TinDang] ([TrangThai]);

CREATE UNIQUE INDEX [UX_TinDang_Room_Active] ON [TinDang] ([PhongTroId]) WHERE [TrangThai] IN (0, 1);

CREATE INDEX [IX_TinNhan_NgayGui] ON [TinNhan] ([NgayGui]);

CREATE INDEX [IX_TinNhan_NguoiGuiId_NguoiNhanId] ON [TinNhan] ([NguoiGuiId], [NguoiNhanId]);

CREATE INDEX [IX_TinNhan_NguoiNhanId] ON [TinNhan] ([NguoiNhanId]);

CREATE INDEX [IX_TinNhan_TinDangId] ON [TinNhan] ([TinDangId]);

CREATE INDEX [IX_YeuCauThuePhong_PostId] ON [YeuCauThuePhong] ([PostId]);

CREATE UNIQUE INDEX [IX_YeuCauThuePhong_TenantAccountId_PostId] ON [YeuCauThuePhong] ([TenantAccountId], [PostId]) WHERE [TrangThai] = 0;

CREATE UNIQUE INDEX [IX_YeuThich_NguoiDungId_TinDangId] ON [YeuThich] ([NguoiDungId], [TinDangId]);

CREATE INDEX [IX_YeuThich_TinDangId] ON [YeuThich] ([TinDangId]);

COMMIT;
END;
