-- Add five apartments, five shared-room listings and five published articles.
-- Select the application's database before running. Existing rows are untouched.
-- Sample addresses and photographs are illustrative.
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
DECLARE @BlogAuthorAccountId int = 1;
DECLARE @LandlordId int;
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @CreatedRooms int = 0, @CreatedPosts int = 0, @CreatedBlogs int = 0;

DECLARE @Photos TABLE (PhotoNo int PRIMARY KEY, Url nvarchar(1000));
INSERT INTO @Photos VALUES
(1,N'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1200&q=80'),
(2,N'https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1200&q=80'),
(3,N'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80'),
(4,N'https://images.unsplash.com/photo-1554995207-c18c203602cb?auto=format&fit=crop&w=1200&q=80'),
(5,N'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80');

DECLARE @Rooms TABLE (
    SampleNo int PRIMARY KEY, CategoryName nvarchar(100), RoomName nvarchar(200),
    Title nvarchar(300), Description nvarchar(max), Price decimal(18,2),
    Area decimal(10,2), MaxOccupants int, Province nvarchar(100), District nvarchar(100),
    Ward nvarchar(100), Address nvarchar(300), Latitude decimal(10,7), Longitude decimal(10,7), PhotoNo int
);
INSERT INTO @Rooms VALUES
(1,N'Căn hộ',N'[Mẫu CH01] Căn hộ 1 phòng ngủ Cầu Giấy',
 N'[Mẫu Căn hộ] 45m² Cầu Giấy, 1 phòng ngủ và ban công',
 N'Căn hộ minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Diện tích 45m² gồm phòng ngủ riêng, khu tiếp khách, bếp và ban công. Có điều hòa, giường, tủ, WiFi, máy giặt dùng chung, thang máy và chỗ để xe. Phù hợp tối đa 3 người. Giá thuê toàn căn 6.500.000đ/tháng; điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 6500000,45,3,N'Hà Nội',N'Cầu Giấy',N'Dịch Vọng',N'Khu vực đường Thành Thái',21.0283000,105.7919000,1),
(2,N'Căn hộ',N'[Mẫu CH02] Studio Mỹ Đình',
 N'[Mẫu Căn hộ] Studio 32m² Mỹ Đình, nội thất gọn gàng',
 N'Căn hộ minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Studio 32m² đón sáng tự nhiên, khu bếp tách khỏi giường ngủ, phù hợp 1–2 người. Có WiFi, điều hòa, giường, tủ, bình nóng lạnh, thang máy và chỗ để xe. Giá thuê toàn căn 4.800.000đ/tháng; điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 4800000,32,2,N'Hà Nội',N'Nam Từ Liêm',N'Mỹ Đình 1',N'Khu vực đường Đình Thôn',21.0155000,105.7713000,2),
(3,N'Căn hộ',N'[Mẫu CH03] Căn hộ mini Bình Thạnh',
 N'[Mẫu Căn hộ] 38m² Bình Thạnh, bếp riêng và ban công thoáng',
 N'Căn hộ minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Căn hộ mini 38m² có bếp riêng, góc làm việc và ban công. Trang bị điều hòa, giường, tủ, WiFi, máy giặt dùng chung, thang máy và nhà vệ sinh riêng. Phù hợp tối đa 3 người. Giá thuê toàn căn 5.800.000đ/tháng; điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 5800000,38,3,N'Hồ Chí Minh',N'Bình Thạnh',N'Phường 25',N'Khu vực đường D5',10.8046000,106.7161000,3),
(4,N'Căn hộ',N'[Mẫu CH04] Căn hộ gần biển Sơn Trà',
 N'[Mẫu Căn hộ] 42m² Sơn Trà, nhiều ánh sáng và đầy đủ nội thất',
 N'Căn hộ minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Căn hộ 42m² bố trí phòng ngủ, khu bếp và góc đọc sách. Có WiFi, điều hòa, bình nóng lạnh, giường, tủ, ban công, thang máy và chỗ để xe. Phù hợp tối đa 3 người. Giá thuê toàn căn 5.500.000đ/tháng; điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 5500000,42,3,N'Đà Nẵng',N'Sơn Trà',N'An Hải Bắc',N'Khu vực đường Ngô Quyền',16.0782000,108.2350000,4),
(5,N'Căn hộ',N'[Mẫu CH05] Căn hộ 1 phòng ngủ Văn Giang',
 N'[Mẫu Căn hộ] 50m² Văn Giang, không gian rộng cho 2–3 người',
 N'Căn hộ minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Căn hộ 50m² có phòng ngủ riêng, bếp và ban công; phù hợp nhóm 2–3 người. Có WiFi, điều hòa, giường, tủ, máy giặt dùng chung, bình nóng lạnh, thang máy và chỗ để xe. Giá thuê toàn căn 6.000.000đ/tháng; điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 6000000,50,3,N'Hưng Yên',N'Văn Giang',N'Xuân Quan',N'Khu vực đường Xuân Quan',20.9705000,105.9360000,5),
(6,N'Ở ghép',N'[Mẫu OG01] Phòng nhóm 2 người Cầu Giấy',
 N'[Mẫu Ở ghép] Phòng 28m² Cầu Giấy, phù hợp nhóm 2 người',
 N'Phòng minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Phòng trống 28m² dành cho nhóm 2 người thuê để ở ghép, có giường, tủ, WiFi, điều hòa, chỗ để xe và nhà vệ sinh riêng. Giá niêm yết 3.200.000đ/tháng cho toàn phòng; nhóm tự thỏa thuận cách chia chi phí. Điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 3200000,28,2,N'Hà Nội',N'Cầu Giấy',N'Dịch Vọng Hậu',N'Khu vực đường Duy Tân',21.0315000,105.7831000,2),
(7,N'Ở ghép',N'[Mẫu OG02] Phòng nhóm 3 người Thanh Xuân',
 N'[Mẫu Ở ghép] Phòng 35m² Thanh Xuân, có khu học tập chung',
 N'Phòng minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Phòng trống 35m² phù hợp nhóm tối đa 3 người, có khu học tập chung, WiFi, điều hòa, giường, tủ và nhà vệ sinh riêng. Giá niêm yết 3.900.000đ/tháng cho toàn phòng; nhóm tự thỏa thuận cách chia chi phí. Điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 3900000,35,3,N'Hà Nội',N'Thanh Xuân',N'Nhân Chính',N'Khu vực đường Nguyễn Tuân',20.9999000,105.8057000,1),
(8,N'Ở ghép',N'[Mẫu OG03] Phòng nhóm 2 người Thủ Đức',
 N'[Mẫu Ở ghép] Phòng 26m² Thủ Đức, WiFi và vệ sinh riêng',
 N'Phòng minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Phòng trống 26m² phù hợp nhóm 2 người, có WiFi, giường, tủ, điều hòa, chỗ để xe và nhà vệ sinh riêng. Giá niêm yết 2.800.000đ/tháng cho toàn phòng; nhóm tự thỏa thuận cách chia chi phí. Điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 2800000,26,2,N'Hồ Chí Minh',N'Thủ Đức',N'Linh Trung',N'Khu vực đường Hoàng Diệu 2',10.8541000,106.7622000,4),
(9,N'Ở ghép',N'[Mẫu OG04] Phòng nhóm 3 người Liên Chiểu',
 N'[Mẫu Ở ghép] Phòng 32m² Liên Chiểu, thoáng và có chỗ để xe',
 N'Phòng minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Phòng trống 32m² dành cho nhóm tối đa 3 người, có cửa sổ, giường, tủ, điều hòa, WiFi, chỗ để xe và nhà vệ sinh riêng. Giá niêm yết 2.700.000đ/tháng cho toàn phòng; nhóm tự thỏa thuận cách chia chi phí. Điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 2700000,32,3,N'Đà Nẵng',N'Liên Chiểu',N'Hòa Khánh Bắc',N'Khu vực đường Âu Cơ',16.0829000,108.1430000,3),
(10,N'Ở ghép',N'[Mẫu OG05] Phòng nhóm 2 người Văn Giang',
 N'[Mẫu Ở ghép] Phòng 24m² Văn Giang, nội thất cơ bản',
 N'Phòng minh họa cho đồ án, địa chỉ và ảnh là dữ liệu mẫu. Phòng trống 24m² phù hợp nhóm 2 người, có giường, tủ, điều hòa, WiFi, chỗ để xe và nhà vệ sinh riêng. Giá niêm yết 2.400.000đ/tháng cho toàn phòng; nhóm tự thỏa thuận cách chia chi phí. Điện 3.800đ/kWh, nước 22.000đ/m³, dịch vụ 150.000đ/tháng.',
 2400000,24,2,N'Hưng Yên',N'Văn Giang',N'Cửu Cao',N'Khu vực đường 179',20.9533000,105.9371000,5);

DECLARE @Amenities TABLE (AmenityName nvarchar(100), ApartmentOnly bit);
INSERT INTO @Amenities VALUES
(N'WiFi',0),(N'Điều hòa',0),(N'Giường',0),(N'Tủ quần áo',0),(N'Chỗ để xe',0),(N'Nhà vệ sinh riêng',0),
(N'Bình nóng lạnh',1),(N'Máy giặt',1),(N'Thang máy',1),(N'Ban công',1),(N'Bếp',1);

DECLARE @Blogs TABLE (
    SampleNo int PRIMARY KEY, Slug varchar(350), Title nvarchar(300), Summary nvarchar(1000),
    Content nvarchar(max), PhotoNo int
);
INSERT INTO @Blogs VALUES
(1,'mau-blog-01-quan-sat-khong-gian-khi-xem-phong',N'[Mẫu Blog] Quan sát không gian trong một buổi xem phòng',
 N'Từ ánh sáng, cửa sổ đến vị trí ổ điện: một danh sách nhỏ giúp buổi xem phòng có thêm thông tin để so sánh.',
 N'Bài viết mẫu phục vụ trình diễn đồ án RoomRentalSystem.

1. Nhìn phòng vào thời điểm bạn thường có mặt
Ánh sáng và tiếng ồn có thể khác nhau giữa buổi sáng và buổi chiều. Khi đặt lịch, hãy chọn thời gian thuận tiện và ghi lại cảm nhận về cửa sổ, không khí, đường đi vào phòng.

2. Hình dung cách sắp xếp đồ đạc
Đo khoảng đặt giường, bàn và tủ. Một phòng có diện tích phù hợp vẫn cần lối đi thoải mái. Quan sát vị trí ổ điện, cửa ra vào và khu vực bếp để tránh bố trí đồ chắn lối đi.

3. Ghi chú theo cùng một mẫu
Với mỗi phòng, lưu lại điểm bạn thích, điều cần hỏi chủ trọ và ảnh được phép chụp. Sau vài buổi xem, các ghi chú theo cùng cấu trúc sẽ giúp bạn so sánh dễ hơn.

Trong hệ thống, bạn có thể lưu tin yêu thích, xem bản đồ và đặt lịch để tổ chức việc tìm phòng.',1),
(2,'mau-blog-02-thong-nhat-nep-sinh-hoat-khi-o-ghep',N'[Mẫu Blog] Thống nhất nếp sinh hoạt trước khi ở ghép',
 N'Giờ ngủ, việc dọn dẹp, khu vực chung và cách trao đổi: những chủ đề nên nói rõ với bạn cùng phòng.',
 N'Bài viết mẫu phục vụ trình diễn đồ án RoomRentalSystem.

1. Trao đổi về nhịp sinh hoạt
Mỗi người có lịch học hoặc làm việc khác nhau. Hãy nói về giờ nghỉ, thời gian dùng bàn học, nhu cầu yên tĩnh và việc tiếp khách để nhóm cùng tìm cách sắp xếp phù hợp.

2. Phân chia việc chung
Một lịch dọn dẹp ngắn và dễ theo dõi thường hữu ích hơn nhiều quy định khó nhớ. Nhóm có thể luân phiên lau sàn, vệ sinh bếp, đổ rác và kiểm tra đồ dùng chung.

3. Có cách trao đổi rõ ràng
Khi có điều bất tiện, nên mô tả việc cụ thể và đưa ra đề nghị có thể thực hiện. Một cuộc trao đổi ngắn mỗi tuần giúp những vấn đề nhỏ được xử lý sớm.

4. Phân biệt giá phòng và phần chia của từng người
Các tin ở ghép mẫu trong đồ án hiển thị giá thuê toàn phòng. Nhóm cần trao đổi riêng về cách chia tiền thuê, điện, nước và đồ dùng chung; không mặc định giá niêm yết là giá mỗi giường.',2),
(3,'mau-blog-03-bo-tri-goc-hoc-tap-trong-phong-nho',N'[Mẫu Blog] Bố trí góc học tập trong phòng nhỏ',
 N'Một góc làm việc gọn gàng bắt đầu từ vị trí bàn, ánh sáng phù hợp và cách cất những đồ dùng thường xuyên.',
 N'Bài viết mẫu phục vụ trình diễn đồ án RoomRentalSystem.

1. Chọn vị trí bàn theo hoạt động hàng ngày
Đặt bàn ở nơi dễ đi lại và ít bị vướng khi mở cửa hoặc tủ. Nếu dùng ánh sáng từ cửa sổ, hãy thử vị trí màn hình để hạn chế phản chiếu gây khó nhìn.

2. Giữ mặt bàn đơn giản
Chỉ để các vật dụng cần cho buổi học hiện tại. Dùng hộp nhỏ hoặc ngăn kéo cho dây cáp, giấy tờ và đồ dùng ít sử dụng để mặt bàn có khoảng trống.

3. Sắp xếp theo tần suất sử dụng
Đồ dùng hàng ngày nên ở vị trí dễ lấy; đồ ít dùng có thể cất cao hơn hoặc trong hộp có nhãn. Một kệ gọn giúp tận dụng chiều cao của phòng mà vẫn giữ lối đi.

4. Thử bố cục trước khi mua thêm đồ
Có thể vẽ sơ đồ đơn giản với kích thước bàn, giường và tủ. Sau vài ngày sử dụng, điều chỉnh những điểm chưa thuận tiện rồi mới bổ sung đồ nội thất.',3),
(4,'mau-blog-04-so-sanh-studio-va-can-ho-mot-phong-ngu',N'[Mẫu Blog] So sánh studio và căn hộ một phòng ngủ',
 N'Tìm hiểu khác biệt về cách chia không gian để chọn kiểu căn hộ phù hợp với sinh hoạt của bạn.',
 N'Bài viết mẫu phục vụ trình diễn đồ án RoomRentalSystem.

Studio: sinh hoạt trong một không gian chính
Trong nhiều studio, giường, bàn và khu tiếp khách nằm cùng một không gian. Cách bố trí này thuận tiện cho người thích sự gọn gàng, nhưng cần chú ý vị trí bếp và cách cất đồ.

Căn hộ một phòng ngủ: có khu nghỉ tách riêng
Phòng ngủ riêng giúp chia rõ nơi nghỉ và nơi sinh hoạt. Khi xem căn hộ, hãy quan sát diện tích thực tế của từng khu vực, thay vì chỉ nhìn tổng diện tích trong tin đăng.

So sánh theo nhu cầu sử dụng
Bạn thường làm việc tại nhà, nấu ăn hay tiếp bạn bè? Cùng một diện tích, cách chia phòng có thể tạo ra trải nghiệm rất khác. Hãy ghi lại những hoạt động quan trọng và kiểm tra xem bố cục có đáp ứng được không.

Trên trang Căn hộ, bạn có thể kết hợp bộ lọc diện tích, giá, số người và tiện ích để tìm các lựa chọn phù hợp, rồi xem chi tiết hoặc đặt lịch.',4),
(5,'mau-blog-05-lap-danh-sach-phong-yeu-thich',N'[Mẫu Blog] Lập danh sách phòng yêu thích để dễ so sánh',
 N'Tổ chức các lựa chọn theo khu vực, diện tích và tiện ích giúp bạn quay lại đúng tin cần xem.',
 N'Bài viết mẫu phục vụ trình diễn đồ án RoomRentalSystem.

1. Bắt đầu với một khu vực cụ thể
Chọn khu vực phù hợp với việc đi học hoặc đi làm. Sau đó thử điều chỉnh diện tích, số người và các tiện ích thực sự cần thiết để danh sách không quá rộng.

2. Lưu một nhóm lựa chọn vừa đủ
Dùng nút lưu tin cho những phòng bạn muốn xem lại. Ghi nhớ một điểm nổi bật của mỗi phòng: nhiều ánh sáng, có ban công, bếp riêng hoặc vị trí thuận tiện.

3. So sánh cùng một nhóm thông tin
Kiểm tra diện tích, giá toàn phòng, số người tối đa, các khoản điện nước hiển thị và ảnh. Nếu một thông tin chưa rõ, bạn có thể nhắn tin với chủ trọ thay vì tự suy đoán.

4. Cập nhật sau khi xem trực tiếp
Sau buổi xem, giữ lại những lựa chọn còn phù hợp và bỏ lưu những tin không đáp ứng nhu cầu. Danh sách ngắn, có thông tin rõ ràng sẽ giúp lần xem tiếp theo hiệu quả hơn.',5);

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource=N'RoomRentalSystem.SeedApartmentSharedBlog',
        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
    IF @LockResult < 0 THROW 50010, 'Cannot obtain sample seed lock.', 1;

    SELECT @LandlordId=l.Id FROM dbo.ChuTro l JOIN dbo.TaiKhoan u ON u.Id=l.TaiKhoanId
    WHERE u.Id=@LandlordAccountId AND u.VaiTro=2 AND u.TrangThai=1;
    IF @LandlordId IS NULL THROW 50011, 'Select an active landlord account.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan WHERE Id=@BlogAuthorAccountId AND VaiTro=0 AND TrangThai=1)
        THROW 50012, 'Select an active admin account for blog author.', 1;
    IF EXISTS (SELECT 1 FROM @Rooms s WHERE (
        SELECT COUNT(*) FROM dbo.DanhMucPhong c WHERE c.TenDanhMuc=s.CategoryName AND c.TrangThai=1) <> 1)
        THROW 50013, 'Each sample category must resolve to exactly one active category.', 1;
    IF EXISTS (SELECT 1 FROM @Amenities a WHERE NOT EXISTS (
        SELECT 1 FROM dbo.TienNghi t WHERE t.TenTienNghi=a.AmenityName AND t.TrangThai=1))
        THROW 50014, 'A required active amenity is missing.', 1;

    DECLARE @SampleNo int=1, @RoomId int;
    WHILE @SampleNo <= 10
    BEGIN
        SET @RoomId=NULL;
        SELECT @RoomId=r.Id FROM dbo.PhongTro r JOIN @Rooms s ON s.RoomName=r.TenPhong
        WHERE s.SampleNo=@SampleNo AND r.ChuTroId=@LandlordId;
        IF @RoomId IS NULL
        BEGIN
            INSERT INTO dbo.PhongTro
                (ChuTroId,DanhMucId,TenPhong,MoTa,GiaThue,DienTich,SoNguoiToiDa,SoNguoiHienTai,
                 SoPhongNgu,SoPhongTam,Tang,DiaChi,PhuongXa,QuanHuyen,TinhThanh,ViDo,KinhDo,
                 TienDien,TienNuoc,PhiDichVu,TrangThai,NgayTao)
            SELECT @LandlordId,c.Id,s.RoomName,s.Description,s.Price,s.Area,s.MaxOccupants,0,
                   1,1,2,s.Address,s.Ward,s.District,s.Province,s.Latitude,s.Longitude,
                   3800,22000,150000,0,@Now
            FROM @Rooms s JOIN dbo.DanhMucPhong c ON c.TenDanhMuc=s.CategoryName AND c.TrangThai=1
            WHERE s.SampleNo=@SampleNo;
            SET @RoomId=CONVERT(int,SCOPE_IDENTITY());
            SET @CreatedRooms+=1;
            INSERT INTO dbo.HinhAnhPhong (PhongTroId,DuongDan,LaAnhDaiDien,ThuTu,NgayTao)
            SELECT @RoomId,p.Url,1,0,@Now FROM @Rooms s JOIN @Photos p ON p.PhotoNo=s.PhotoNo WHERE s.SampleNo=@SampleNo
            UNION ALL
            SELECT @RoomId,p.Url,0,1,@Now FROM @Rooms s JOIN @Photos p ON p.PhotoNo=(s.PhotoNo % 5)+1 WHERE s.SampleNo=@SampleNo;
            INSERT INTO dbo.PhongTro_TienNghi (PhongTroId,TienNghiId)
            SELECT DISTINCT @RoomId,t.Id FROM @Amenities a JOIN dbo.TienNghi t ON t.TenTienNghi=a.AmenityName AND t.TrangThai=1
            WHERE a.ApartmentOnly=0 OR @SampleNo<=5;
            INSERT INTO dbo.TinDang (PhongTroId,ChuTroId,TieuDe,NoiDung,GiaHienThi,TrangThai,LuotXem,NgayDang,NgayDuyet,NgayTao)
            SELECT @RoomId,@LandlordId,Title,Description,Price,1,0,@Now,@Now,@Now FROM @Rooms WHERE SampleNo=@SampleNo;
            SET @CreatedPosts+=1;
        END;
        SET @SampleNo+=1;
    END;

    INSERT INTO dbo.BaiViet (TieuDe,Slug,TomTat,NoiDung,AnhDaiDien,TacGiaId,LuotXem,TrangThai,NgayDang)
    SELECT b.Title,b.Slug,b.Summary,b.Content,p.Url,@BlogAuthorAccountId,0,1,@Now
    FROM @Blogs b JOIN @Photos p ON p.PhotoNo=b.PhotoNo
    WHERE NOT EXISTS (SELECT 1 FROM dbo.BaiViet existing WHERE existing.Slug=b.Slug);
    SET @CreatedBlogs=@@ROWCOUNT;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT @CreatedRooms AS CreatedRooms,@CreatedPosts AS CreatedPosts,@CreatedBlogs AS CreatedBlogs;
SELECT s.CategoryName,r.Id AS RoomId,p.Id AS PostId,p.TieuDe AS Title,r.GiaThue AS Price
FROM @Rooms s JOIN dbo.PhongTro r ON r.TenPhong=s.RoomName AND r.ChuTroId=@LandlordId
JOIN dbo.TinDang p ON p.PhongTroId=r.Id ORDER BY s.SampleNo,p.Id;
SELECT b.Id AS BlogId,b.TieuDe AS Title,b.Slug,b.TrangThai AS Status
FROM dbo.BaiViet b JOIN @Blogs s ON s.Slug=b.Slug ORDER BY s.SampleNo;
