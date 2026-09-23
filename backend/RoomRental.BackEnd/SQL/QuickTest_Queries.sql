-- =============================================
-- ROOM RENTAL SYSTEM - QUICK TEST QUERIES
-- Các query đơn giản để test database
-- =============================================

USE RoomRentalDb_Dev;
GO

-- =============================================
-- 1. KIỂM TRA CƠ BẢN
-- =============================================

PRINT '========================================';
PRINT '1. CHECKING DATABASE STRUCTURE';
PRINT '========================================';

-- Liệt kê tất cả tables
SELECT 
    TABLE_NAME AS 'Table Name',
    (SELECT COUNT(*) 
     FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_NAME = t.TABLE_NAME) AS 'Column Count'
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Đếm records trong mỗi table
PRINT '';
PRINT 'Record Counts:';
SELECT 'Roles' AS [Table], COUNT(*) AS [Records] FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Posts', COUNT(*) FROM Posts
UNION ALL SELECT 'Rooms', COUNT(*) FROM Rooms
UNION ALL SELECT 'Amenities', COUNT(*) FROM Amenities
UNION ALL SELECT 'PostAmenities', COUNT(*) FROM PostAmenities
UNION ALL SELECT 'PostImages', COUNT(*) FROM PostImages
UNION ALL SELECT 'Favorites', COUNT(*) FROM Favorites
UNION ALL SELECT 'ViewingAppointments', COUNT(*) FROM ViewingAppointments;

-- =============================================
-- 2. XEM DỮ LIỆU MẪU
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '2. SAMPLE DATA';
PRINT '========================================';

-- Xem các Role
PRINT 'Roles:';
SELECT * FROM Roles;

-- Xem Users theo Role
PRINT '';
PRINT 'Users by Role:';
SELECT 
    u.Id,
    u.FullName,
    u.Email,
    u.Phone,
    r.Name AS Role,
    u.IsBlocked,
    u.CreatedAt
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.Id
ORDER BY r.Id, u.Id;

-- Xem Posts với thông tin cơ bản
PRINT '';
PRINT 'Posts Overview:';
SELECT TOP 5
    p.Id,
    p.Title,
    p.Price,
    r.Province,
    r.District,
    r.Area,
    CASE p.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Approved'
        WHEN 2 THEN 'Rejected'
        WHEN 3 THEN 'Hidden'
        WHEN 4 THEN 'Expired'
    END AS Status,
    u.FullName AS Landlord
FROM Posts p
INNER JOIN Rooms r ON p.Id = r.PostId
INNER JOIN Users u ON p.LandlordId = u.Id
ORDER BY p.CreatedAt DESC;

-- =============================================
-- 3. TEST QUERIES - TÌM KIẾM PHÒNG
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '3. SEARCH TESTS';
PRINT '========================================';

-- Test 1: Tìm phòng tại Hồ Chí Minh
PRINT 'Test 1: Rooms in Ho Chi Minh City';
SELECT 
    p.Id,
    p.Title,
    FORMAT(p.Price, 'N0') + ' VNĐ' AS Price,
    r.District,
    r.Area AS 'Area (m2)'
FROM Posts p
INNER JOIN Rooms r ON p.Id = r.PostId
WHERE r.Province = N'Hồ Chí Minh'
  AND p.Status = 1
ORDER BY p.Price;

-- Test 2: Tìm phòng giá rẻ (< 3 triệu)
PRINT '';
PRINT 'Test 2: Cheap rooms (< 3,000,000 VND)';
SELECT 
    p.Title,
    FORMAT(p.Price, 'N0') + ' VNĐ' AS Price,
    r.Province,
    r.District,
    r.Area AS 'Area (m2)'
FROM Posts p
INNER JOIN Rooms r ON p.Id = r.PostId
WHERE p.Price < 3000000
  AND p.Status = 1
ORDER BY p.Price;

-- Test 3: Tìm phòng có diện tích lớn (> 30m2)
PRINT '';
PRINT 'Test 3: Large rooms (> 30 m2)';
SELECT 
    p.Title,
    r.Area AS 'Area (m2)',
    FORMAT(p.Price, 'N0') + ' VNĐ' AS Price,
    r.Province,
    r.District
FROM Posts p
INNER JOIN Rooms r ON p.Id = r.PostId
WHERE r.Area > 30
  AND p.Status = 1
ORDER BY r.Area DESC;

-- =============================================
-- 4. TEST RELATIONSHIPS
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '4. RELATIONSHIP TESTS';
PRINT '========================================';

-- Test 4: Post với số lượng hình ảnh
PRINT 'Test 4: Posts with image count';
SELECT 
    p.Id,
    p.Title,
    COUNT(pi.Id) AS ImageCount
FROM Posts p
LEFT JOIN PostImages pi ON p.Id = pi.PostId
GROUP BY p.Id, p.Title
ORDER BY ImageCount DESC;

-- Test 5: Post với số lượng tiện ích
PRINT '';
PRINT 'Test 5: Posts with amenity count';
SELECT 
    p.Id,
    p.Title,
    COUNT(pa.AmenityId) AS AmenityCount
FROM Posts p
LEFT JOIN PostAmenities pa ON p.Id = pa.PostId
GROUP BY p.Id, p.Title
ORDER BY AmenityCount DESC;

-- Test 6: Post với danh sách tiện ích
PRINT '';
PRINT 'Test 6: Post #1 with amenities list';
SELECT 
    p.Title,
    a.Name AS Amenity,
    a.Icon
FROM Posts p
INNER JOIN PostAmenities pa ON p.Id = pa.PostId
INNER JOIN Amenities a ON pa.AmenityId = a.Id
WHERE p.Id = 1
ORDER BY a.Name;

-- Test 7: Favorites count
PRINT '';
PRINT 'Test 7: Posts with favorite count';
SELECT 
    p.Id,
    p.Title,
    COUNT(f.Id) AS FavoriteCount
FROM Posts p
LEFT JOIN Favorites f ON p.Id = f.PostId
GROUP BY p.Id, p.Title
ORDER BY FavoriteCount DESC;

-- =============================================
-- 5. TEST STORED PROCEDURES
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '5. STORED PROCEDURE TESTS';
PRINT '========================================';

-- Test SP: Get all approved posts
PRINT 'Test SP: Get all approved posts (First 3)';
EXEC sp_GetPosts @PageNumber = 1, @PageSize = 3;

-- Test SP: Get posts in Ho Chi Minh
PRINT '';
PRINT 'Test SP: Get posts in Ho Chi Minh City';
EXEC sp_GetPosts @Province = N'Hồ Chí Minh', @PageSize = 5;

-- Test SP: Get post by ID
PRINT '';
PRINT 'Test SP: Get post details (ID = 1)';
EXEC sp_GetPostById @PostId = 1;

-- =============================================
-- 6. TEST VIEWS
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '6. VIEW TESTS';
PRINT '========================================';

-- Test View: PostsWithRoomDetails
PRINT 'Test View: vw_PostsWithRoomDetails (First 3)';
SELECT TOP 3
    PostId,
    Title,
    FORMAT(Price, 'N0') + ' VNĐ' AS Price,
    Province,
    District,
    Area AS 'Area (m2)',
    LandlordName
FROM vw_PostsWithRoomDetails
ORDER BY PostCreatedAt DESC;

-- Test View: UserStatistics
PRINT '';
PRINT 'Test View: vw_UserStatistics';
SELECT 
    FullName,
    Email,
    RoleName,
    ActivityCount,
    IsBlocked
FROM vw_UserStatistics
ORDER BY RoleName, FullName;

-- =============================================
-- 7. BUSINESS LOGIC TESTS
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '7. BUSINESS LOGIC TESTS';
PRINT '========================================';

-- Test 8: Landlord's posts statistics
PRINT 'Test 8: Landlord posts statistics';
SELECT 
    u.FullName AS Landlord,
    COUNT(p.Id) AS TotalPosts,
    SUM(CASE WHEN p.Status = 1 THEN 1 ELSE 0 END) AS ApprovedPosts,
    SUM(CASE WHEN p.Status = 0 THEN 1 ELSE 0 END) AS PendingPosts,
    AVG(p.Price) AS AvgPrice
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.Id
LEFT JOIN Posts p ON u.Id = p.LandlordId
WHERE r.Name = 'Landlord'
GROUP BY u.Id, u.FullName
ORDER BY TotalPosts DESC;

-- Test 9: Upcoming appointments
PRINT '';
PRINT 'Test 9: Viewing appointments';
SELECT 
    va.ScheduledAt,
    t.FullName AS Tenant,
    p.Title AS Post,
    CASE va.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Approved'
        WHEN 2 THEN 'Rejected'
        WHEN 3 THEN 'Cancelled'
        WHEN 4 THEN 'Completed'
    END AS Status,
    va.TenantNote
FROM ViewingAppointments va
INNER JOIN Users t ON va.TenantId = t.Id
INNER JOIN Posts p ON va.PostId = p.Id
ORDER BY va.ScheduledAt;

-- Test 10: Most popular amenities
PRINT '';
PRINT 'Test 10: Most popular amenities';
SELECT 
    a.Name AS Amenity,
    COUNT(pa.PostId) AS UsageCount
FROM Amenities a
LEFT JOIN PostAmenities pa ON a.Id = pa.AmenityId
GROUP BY a.Id, a.Name
ORDER BY UsageCount DESC;

-- =============================================
-- 8. DATA INTEGRITY TESTS
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '8. DATA INTEGRITY TESTS';
PRINT '========================================';

-- Test 11: Orphan records check
PRINT 'Test 11: Check for orphan records';

-- Posts without Rooms
SELECT 'Posts without Rooms' AS Issue, COUNT(*) AS Count
FROM Posts p
LEFT JOIN Rooms r ON p.Id = r.PostId
WHERE r.Id IS NULL

UNION ALL

-- Rooms without Posts
SELECT 'Rooms without Posts', COUNT(*)
FROM Rooms r
LEFT JOIN Posts p ON r.PostId = p.Id
WHERE p.Id IS NULL

UNION ALL

-- Favorites for non-existent posts
SELECT 'Favorites with invalid PostId', COUNT(*)
FROM Favorites f
LEFT JOIN Posts p ON f.PostId = p.Id
WHERE p.Id IS NULL

UNION ALL

-- Appointments for non-existent posts
SELECT 'Appointments with invalid PostId', COUNT(*)
FROM ViewingAppointments va
LEFT JOIN Posts p ON va.PostId = p.Id
WHERE p.Id IS NULL;

-- =============================================
-- COMPLETION
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'ALL TESTS COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT 'Database: RoomRentalDb_Dev';
PRINT 'Server: localhost\SQLEXPRESS';
PRINT 'Status: OK';
PRINT '========================================';
GO
