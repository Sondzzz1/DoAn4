-- Five illustrative Room/Post pairs for the existing public search interface.
-- Run in the application's database. Re-running does not overwrite existing rows.
-- Change this account ID to assign new sample rooms to another active landlord.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @LandlordAccountId int = 20;
DECLARE @LandlordId int;
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @CreatedRooms int = 0;
DECLARE @CreatedPosts int = 0;

DECLARE @Samples TABLE (
    SampleNo int PRIMARY KEY,
    RoomName nvarchar(200) NOT NULL,
    Title nvarchar(300) NOT NULL,
    Description nvarchar(max) NOT NULL,
    CategoryName nvarchar(100) NOT NULL,
    Price decimal(18,2) NOT NULL,
    Area decimal(10,2) NOT NULL,
    MaxOccupants int NOT NULL,
    Address nvarchar(300) NOT NULL,
    Ward nvarchar(100) NOT NULL,
    District nvarchar(100) NOT NULL,
    Province nvarchar(100) NOT NULL,
    Latitude decimal(10,7) NOT NULL,
    Longitude decimal(10,7) NOT NULL,
    FloorNumber int NOT NULL,
    ElectricityPrice decimal(18,2) NOT NULL,
    WaterPrice decimal(18,2) NOT NULL,
    ServiceFee decimal(18,2) NOT NULL,
    CoverUrl nvarchar(1000) NOT NULL,
    SecondUrl nvarchar(1000) NOT NULL
);

INSERT INTO @Samples VALUES
(1, N'[Mẫu 01] Phòng sáng gần khu đại học Văn Giang',
 N'[Mẫu] Phòng trọ 25m² tại Văn Giang, có điều hòa và WiFi',
 N'Dữ liệu mẫu phục vụ trình diễn đồ án, địa chỉ và ảnh mang tính minh họa. Phòng riêng 25m², cửa sổ đón sáng, phù hợp 1–2 người. Có điều hòa, bình nóng lạnh, WiFi, chỗ để xe và nhà vệ sinh riêng. Giờ giấc tự do; điện 3.500đ/kWh, nước 20.000đ/m³, phí dịch vụ 100.000đ/tháng. Có thể đặt lịch xem hoặc gửi yêu cầu thuê để thử quy trình.',
 N'Phòng trọ', 2300000, 25, 2, N'Đường 179, khu vực Xuân Quan', N'Xuân Quan', N'Văn Giang', N'Hưng Yên',
 20.9670000, 105.9340000, 2, 3500, 20000, 100000,
 N'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80',
 N'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80'),
(2, N'[Mẫu 02] Phòng có ban công tại Cầu Giấy',
 N'[Mẫu] Phòng 30m² Cầu Giấy, ban công và nội thất cơ bản',
 N'Dữ liệu mẫu phục vụ trình diễn đồ án, địa chỉ và ảnh mang tính minh họa. Phòng 30m² phù hợp 2 người, có ban công thoáng, giường, tủ quần áo và nhà vệ sinh riêng. Khu vực để xe có camera, WiFi sẵn có. Điện 3.800đ/kWh, nước 22.000đ/m³ và phí dịch vụ 150.000đ/tháng. Chủ trọ hỗ trợ đặt lịch xem phòng.',
 N'Phòng trọ', 3200000, 30, 2, N'Khu vực đường Trần Thái Tông', N'Dịch Vọng Hậu', N'Cầu Giấy', N'Hà Nội',
 21.0302000, 105.7890000, 3, 3800, 22000, 150000,
 N'https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80',
 N'https://images.unsplash.com/photo-1554995207-c18c203602cb?auto=format&fit=crop&w=1200&q=80'),
(3, N'[Mẫu 03] Studio đầy đủ nội thất tại Bình Thạnh',
 N'[Mẫu] Studio 35m² Bình Thạnh, bếp riêng và đầy đủ nội thất',
 N'Dữ liệu mẫu phục vụ trình diễn đồ án, địa chỉ và ảnh mang tính minh họa. Studio 35m² có khu bếp riêng, điều hòa, giường, tủ, bình nóng lạnh, máy giặt dùng chung và thang máy. Phù hợp tối đa 3 người. Điện 4.000đ/kWh, nước 25.000đ/m³, phí dịch vụ 200.000đ/tháng. WiFi và chỗ để xe sẵn có.',
 N'Căn hộ', 4500000, 35, 3, N'Khu vực đường Nguyễn Gia Trí', N'Phường 25', N'Bình Thạnh', N'Hồ Chí Minh',
 10.8023000, 106.7141000, 4, 4000, 25000, 200000,
 N'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80',
 N'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80'),
(4, N'[Mẫu 04] Phòng giá hợp lý tại Liên Chiểu',
 N'[Mẫu] Phòng trọ 22m² Liên Chiểu, vệ sinh riêng, có chỗ để xe',
 N'Dữ liệu mẫu phục vụ trình diễn đồ án, địa chỉ và ảnh mang tính minh họa. Phòng 22m² thoáng sáng, phù hợp sinh viên hoặc người đi làm ở 1–2 người. Có WiFi, chỗ để xe, camera và nhà vệ sinh riêng. Điện 3.500đ/kWh, nước 18.000đ/m³, phí dịch vụ 80.000đ/tháng. Có thể liên hệ chủ trọ qua chức năng nhắn tin.',
 N'Phòng trọ', 1800000, 22, 2, N'Khu vực đường Nguyễn Lương Bằng', N'Hòa Khánh Bắc', N'Liên Chiểu', N'Đà Nẵng',
 16.0734000, 108.1505000, 1, 3500, 18000, 80000,
 N'https://images.unsplash.com/photo-1554995207-c18c203602cb?auto=format&fit=crop&w=1200&q=80',
 N'https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80'),
(5, N'[Mẫu 05] Căn hộ mini có ban công tại Nam Từ Liêm',
 N'[Mẫu] Căn hộ mini 40m² Nam Từ Liêm, thoáng sáng và bếp riêng',
 N'Dữ liệu mẫu phục vụ trình diễn đồ án, địa chỉ và ảnh mang tính minh họa. Căn hộ mini 40m² phù hợp 2–3 người, có ban công, khu bếp, điều hòa, giường, tủ quần áo, máy giặt dùng chung và thang máy. WiFi, chỗ để xe và camera sẵn có. Điện 3.800đ/kWh, nước 22.000đ/m³, phí dịch vụ 200.000đ/tháng.',
 N'Căn hộ', 5200000, 40, 3, N'Khu vực đường Lê Đức Thọ', N'Mỹ Đình 2', N'Nam Từ Liêm', N'Hà Nội',
 21.0289000, 105.7658000, 5, 3800, 22000, 200000,
 N'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80',
 N'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80');

DECLARE @SampleAmenities TABLE (SampleNo int, AmenityName nvarchar(100));
INSERT INTO @SampleAmenities VALUES
(1,N'WiFi'),(1,N'Điều hòa'),(1,N'Bình nóng lạnh'),(1,N'Chỗ để xe'),(1,N'Nhà vệ sinh riêng'),
(2,N'WiFi'),(2,N'Giường'),(2,N'Tủ quần áo'),(2,N'Chỗ để xe'),(2,N'Camera'),(2,N'Ban công'),(2,N'Nhà vệ sinh riêng'),
(3,N'WiFi'),(3,N'Điều hòa'),(3,N'Giường'),(3,N'Tủ quần áo'),(3,N'Bình nóng lạnh'),(3,N'Máy giặt'),(3,N'Thang máy'),(3,N'Bếp'),(3,N'Chỗ để xe'),(3,N'Nhà vệ sinh riêng'),
(4,N'WiFi'),(4,N'Chỗ để xe'),(4,N'Camera'),(4,N'Nhà vệ sinh riêng'),
(5,N'WiFi'),(5,N'Điều hòa'),(5,N'Giường'),(5,N'Tủ quần áo'),(5,N'Máy giặt'),(5,N'Thang máy'),(5,N'Ban công'),(5,N'Bếp'),(5,N'Chỗ để xe'),(5,N'Camera'),(5,N'Nhà vệ sinh riêng');

BEGIN TRY
    BEGIN TRANSACTION;

    -- Serialize this seed only; existing application rows are not updated.
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'RoomRentalSystem.Seed5PublicRooms',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0 THROW 50001, 'Cannot obtain sample seed lock.', 1;

    SELECT @LandlordId = l.Id
    FROM dbo.ChuTro l JOIN dbo.TaiKhoan u ON u.Id = l.TaiKhoanId
    WHERE u.Id = @LandlordAccountId AND u.VaiTro = 2 AND u.TrangThai = 1;
    IF @LandlordId IS NULL THROW 50002, 'Select an existing active landlord account.', 1;

    IF EXISTS (SELECT 1 FROM @Samples s WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DanhMucPhong c WHERE c.TenDanhMuc = s.CategoryName AND c.TrangThai = 1))
        THROW 50003, 'A required active category is missing.', 1;
    IF EXISTS (SELECT 1 FROM @SampleAmenities sa WHERE NOT EXISTS (
        SELECT 1 FROM dbo.TienNghi a WHERE a.TenTienNghi = sa.AmenityName AND a.TrangThai = 1))
        THROW 50004, 'A required active amenity is missing.', 1;

    DECLARE @SampleNo int = 1;
    DECLARE @RoomId int;
    WHILE @SampleNo <= 5
    BEGIN
        SET @RoomId = NULL;
        SELECT @RoomId = r.Id FROM dbo.PhongTro r JOIN @Samples s ON s.RoomName = r.TenPhong
        WHERE s.SampleNo = @SampleNo AND r.ChuTroId = @LandlordId;

        -- Leave an existing sample room and its later rental/moderation history untouched.
        IF @RoomId IS NULL
        BEGIN
            INSERT INTO dbo.PhongTro
                (ChuTroId, DanhMucId, TenPhong, MoTa, GiaThue, DienTich, SoNguoiToiDa,
                 SoNguoiHienTai, SoPhongNgu, SoPhongTam, Tang, DiaChi, PhuongXa, QuanHuyen,
                 TinhThanh, ViDo, KinhDo, TienDien, TienNuoc, PhiDichVu, TrangThai, NgayTao)
            SELECT @LandlordId, c.Id, s.RoomName, s.Description, s.Price, s.Area, s.MaxOccupants,
                   0, 1, 1, s.FloorNumber, s.Address, s.Ward, s.District, s.Province,
                   s.Latitude, s.Longitude, s.ElectricityPrice, s.WaterPrice, s.ServiceFee, 0, @Now
            FROM @Samples s JOIN dbo.DanhMucPhong c ON c.TenDanhMuc = s.CategoryName AND c.TrangThai = 1
            WHERE s.SampleNo = @SampleNo;
            IF @@ROWCOUNT <> 1 THROW 50005, 'Category must resolve to exactly one row.', 1;
            SET @RoomId = CONVERT(int, SCOPE_IDENTITY());
            SET @CreatedRooms += 1;

            INSERT INTO dbo.HinhAnhPhong (PhongTroId, DuongDan, LaAnhDaiDien, ThuTu, NgayTao)
            SELECT @RoomId, CoverUrl, 1, 0, @Now FROM @Samples WHERE SampleNo = @SampleNo
            UNION ALL
            SELECT @RoomId, SecondUrl, 0, 1, @Now FROM @Samples WHERE SampleNo = @SampleNo;

            INSERT INTO dbo.PhongTro_TienNghi (PhongTroId, TienNghiId)
            SELECT DISTINCT @RoomId, a.Id FROM @SampleAmenities sa
            JOIN dbo.TienNghi a ON a.TenTienNghi = sa.AmenityName AND a.TrangThai = 1
            WHERE sa.SampleNo = @SampleNo;

            INSERT INTO dbo.TinDang
                (PhongTroId, ChuTroId, TieuDe, NoiDung, GiaHienThi, TrangThai, LuotXem,
                 NgayDang, NgayDuyet, NgayTao)
            SELECT @RoomId, @LandlordId, Title, Description, Price, 1, 0, @Now, @Now, @Now
            FROM @Samples WHERE SampleNo = @SampleNo;
            SET @CreatedPosts += 1;
        END;
        SET @SampleNo += 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT @CreatedRooms AS CreatedRooms, @CreatedPosts AS CreatedPosts;
SELECT r.Id AS RoomId, p.Id AS PostId, p.TieuDe AS Title,
       r.GiaThue AS Price, r.DienTich AS Area, r.TinhThanh AS Province,
       r.TrangThai AS RoomStatus, p.TrangThai AS PostStatus,
       u.HoTen AS LandlordName
FROM @Samples s JOIN dbo.PhongTro r ON r.TenPhong = s.RoomName AND r.ChuTroId = @LandlordId
JOIN dbo.TinDang p ON p.PhongTroId = r.Id
JOIN dbo.ChuTro l ON l.Id = r.ChuTroId JOIN dbo.TaiKhoan u ON u.Id = l.TaiKhoanId
ORDER BY s.SampleNo, p.Id;
