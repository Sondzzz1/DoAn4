# Room Rental System - Database Documentation

## 📊 Tổng quan Database

Database **RoomRentalDb_Dev** được thiết kế cho hệ thống tìm kiếm và cho thuê phòng trọ với 3 actors chính:
- **Tenant** (Người tìm trọ)
- **Landlord** (Chủ nhà)
- **Admin** (Quản trị viên)

## 🗂️ Cấu trúc Database

### Tables (9 bảng chính)

1. **Roles** - Vai trò người dùng
2. **Users** - Người dùng hệ thống
3. **Posts** - Tin đăng cho thuê
4. **Rooms** - Thông tin phòng trọ (1-1 với Posts)
5. **Amenities** - Tiện ích (Wifi, điều hòa, v.v.)
6. **PostAmenities** - Bảng trung gian Posts ↔ Amenities
7. **PostImages** - Hình ảnh tin đăng
8. **Favorites** - Tin yêu thích của Tenant
9. **ViewingAppointments** - Lịch hẹn xem phòng

### Enums

**PostStatus** (Trạng thái tin đăng):
- 0: Pending (Chờ duyệt)
- 1: Approved (Đã duyệt)
- 2: Rejected (Bị từ chối)
- 3: Hidden (Đã ẩn)
- 4: Expired (Hết hạn)

**RoomStatus** (Trạng thái phòng):
- 0: Available (Còn trống)
- 1: Rented (Đã cho thuê)
- 2: TemporarilyUnavailable (Tạm thời không khả dụng)

**AppointmentStatus** (Trạng thái lịch hẹn):
- 0: Pending (Chờ xác nhận)
- 1: Approved (Đã chấp nhận)
- 2: Rejected (Bị từ chối)
- 3: Cancelled (Đã hủy)
- 4: Completed (Đã hoàn thành)

## 🚀 Cách sử dụng

### Bước 1: Mở SQL Server Management Studio (SSMS)

1. Kết nối đến server: `localhost\SQLEXPRESS`
2. Sử dụng Windows Authentication

### Bước 2: Chạy SQL Script

1. Mở file `RoomRentalDb_Schema_And_Data.sql`
2. Click **Execute** (F5) hoặc click nút ▶️
3. Chờ script chạy xong (khoảng 5-10 giây)
4. Kiểm tra Messages để xác nhận thành công

### Bước 3: Kiểm tra Database

```sql
-- Xem tất cả tables
USE RoomRentalDb_Dev;
GO

SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Đếm số records
SELECT 'Users' AS TableName, COUNT(*) AS RecordCount FROM Users
UNION ALL
SELECT 'Posts', COUNT(*) FROM Posts
UNION ALL
SELECT 'Rooms', COUNT(*) FROM Rooms
UNION ALL
SELECT 'Amenities', COUNT(*) FROM Amenities;
```

## 👤 Tài khoản mẫu

### Admin
- **Email**: `admin@timnhatro.vn`
- **Password**: `123456`
- **Quyền**: Quản trị toàn bộ hệ thống

### Landlords (Chủ nhà)
- **Email**: `landlord1@gmail.com` | **Password**: `123456`
- **Email**: `landlord2@gmail.com` | **Password**: `123456`
- **Email**: `landlord3@gmail.com` | **Password**: `123456`
- **Email**: `landlord4@gmail.com` | **Password**: `123456`

### Tenants (Người tìm trọ)
- **Email**: `tenant1@gmail.com` | **Password**: `123456`
- **Email**: `tenant2@gmail.com` | **Password**: `123456`
- **Email**: `tenant3@gmail.com` | **Password**: `123456`

**⚠️ Lưu ý**: Trong production, password cần được hash thật bằng BCrypt.Net-Next. Script hiện tại sử dụng hash giả để demo.

## 📝 Dữ liệu mẫu

### Posts (10 tin đăng)
- 6 tin **Phòng trọ** (Hà Nội + HCM)
- 2 tin **Nhà nguyên căn**
- 1 tin **Căn hộ dịch vụ**
- 1 tin **Ở ghép**

### Amenities (14 tiện ích)
- Wifi miễn phí
- Điều hòa
- Nóng lạnh
- Máy giặt
- Tủ lạnh
- Giường
- Tủ quần áo
- Bàn ghế
- Bếp
- Chỗ để xe
- An ninh
- Thú cưng
- Ban công
- Thang máy

### Locations
- **Hồ Chí Minh**: Quận 1, 7, 9, 10, Tân Bình, Bình Thạnh, Phú Nhuận, Gò Vấp
- **Hà Nội**: Cầu Giấy, Hoàng Mai

## 🔍 Views và Stored Procedures

### Views

#### 1. vw_PostsWithRoomDetails
Hiển thị tin đăng kèm thông tin phòng và chủ nhà

```sql
SELECT * FROM vw_PostsWithRoomDetails;
```

#### 2. vw_UserStatistics
Thống kê hoạt động người dùng

```sql
SELECT * FROM vw_UserStatistics;
```

### Stored Procedures

#### 1. sp_GetPosts
Lấy danh sách tin đăng với filter và phân trang

```sql
-- Lấy tất cả tin đăng đã duyệt
EXEC sp_GetPosts;

-- Lấy tin đăng tại Hồ Chí Minh
EXEC sp_GetPosts @Province = N'Hồ Chí Minh';

-- Lấy tin đăng giá từ 2-4 triệu
EXEC sp_GetPosts @MinPrice = 2000000, @MaxPrice = 4000000;

-- Phân trang (trang 2, mỗi trang 5 items)
EXEC sp_GetPosts @PageNumber = 2, @PageSize = 5;
```

#### 2. sp_GetPostById
Lấy chi tiết tin đăng theo ID

```sql
-- Xem chi tiết post ID = 1
EXEC sp_GetPostById @PostId = 1;
```

## 📊 Query Examples

### 1. Tìm phòng trọ giá rẻ < 3 triệu tại HCM

```sql
SELECT 
    p.Title,
    p.Price,
    r.District,
    r.Ward,
    r.Address
FROM Posts p
INNER JOIN Rooms r ON p.Id = r.PostId
WHERE 
    p.Status = 1 -- Approved
    AND p.Price < 3000000
    AND r.Province = N'Hồ Chí Minh'
ORDER BY p.Price;
```

### 2. Xem tin đăng có nhiều yêu thích nhất

```sql
SELECT TOP 5
    p.Id,
    p.Title,
    COUNT(f.Id) AS FavoriteCount
FROM Posts p
LEFT JOIN Favorites f ON p.Id = f.PostId
WHERE p.Status = 1
GROUP BY p.Id, p.Title
ORDER BY FavoriteCount DESC;
```

### 3. Thống kê số tin đăng theo Landlord

```sql
SELECT 
    u.FullName AS LandlordName,
    COUNT(p.Id) AS TotalPosts,
    SUM(CASE WHEN p.Status = 1 THEN 1 ELSE 0 END) AS ApprovedPosts
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.Id
LEFT JOIN Posts p ON u.Id = p.LandlordId
WHERE r.Name = 'Landlord'
GROUP BY u.FullName
ORDER BY TotalPosts DESC;
```

### 4. Xem lịch hẹn sắp tới

```sql
SELECT 
    va.ScheduledAt,
    t.FullName AS TenantName,
    p.Title AS PostTitle,
    va.Status,
    va.TenantNote
FROM ViewingAppointments va
INNER JOIN Users t ON va.TenantId = t.Id
INNER JOIN Posts p ON va.PostId = p.Id
WHERE va.ScheduledAt > GETUTCDATE()
ORDER BY va.ScheduledAt;
```

### 5. Tìm phòng có tiện ích đầy đủ

```sql
SELECT 
    p.Title,
    COUNT(pa.AmenityId) AS AmenityCount,
    STRING_AGG(a.Name, ', ') AS Amenities
FROM Posts p
INNER JOIN PostAmenities pa ON p.Id = pa.PostId
INNER JOIN Amenities a ON pa.AmenityId = a.Id
WHERE p.Status = 1
GROUP BY p.Id, p.Title
HAVING COUNT(pa.AmenityId) >= 5
ORDER BY AmenityCount DESC;
```

## 🔐 Security Notes

1. **Password Hashing**: 
   - Production cần sử dụng BCrypt.Net-Next
   - Không lưu plaintext password
   - Script hiện tại dùng hash giả cho demo

2. **SQL Injection Prevention**:
   - Sử dụng parameterized queries
   - Không concat string trong SQL

3. **Connection String**:
   ```
   Server=localhost\\SQLEXPRESS;Database=RoomRentalDb_Dev;Trusted_Connection=true
   ```

## 🛠️ Maintenance

### Backup Database

```sql
BACKUP DATABASE RoomRentalDb_Dev
TO DISK = 'C:\Backup\RoomRentalDb_Dev.bak'
WITH FORMAT, NAME = 'Full Backup';
```

### Restore Database

```sql
RESTORE DATABASE RoomRentalDb_Dev
FROM DISK = 'C:\Backup\RoomRentalDb_Dev.bak'
WITH REPLACE;
```

### Drop Database (Cẩn thận!)

```sql
USE master;
GO

ALTER DATABASE RoomRentalDb_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE RoomRentalDb_Dev;
GO
```

## 📞 Support

Nếu gặp vấn đề:
1. Kiểm tra SQL Server đã chạy chưa
2. Kiểm tra quyền của user
3. Xem Messages trong SSMS để biết lỗi chi tiết
4. Đảm bảo server name đúng: `localhost\SQLEXPRESS`

---

**Version**: 1.0  
**Last Updated**: 2026-09-16  
**Database**: RoomRentalDb_Dev
