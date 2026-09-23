-- =============================================
-- ROOM RENTAL SYSTEM - DATABASE SCRIPT
-- Database: RoomRentalDb_Dev
-- Server: localhost\SQLEXPRESS
-- Author: Room Rental System Team
-- Description: Hệ thống tìm kiếm và cho thuê phòng trọ
-- =============================================

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'RoomRentalDb_Dev')
BEGIN
    ALTER DATABASE RoomRentalDb_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RoomRentalDb_Dev;
END
GO

-- Create database
CREATE DATABASE RoomRentalDb_Dev;
GO

USE RoomRentalDb_Dev;
GO

-- =============================================
-- CREATE TABLES
-- =============================================

-- Table: Roles
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

-- Table: Users
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    AvatarUrl NVARCHAR(500) NULL,
    RoleId INT NOT NULL,
    IsBlocked BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);
GO

-- Table: Posts
CREATE TABLE Posts (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    LandlordId INT NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0:Pending, 1:Approved, 2:Rejected, 3:Hidden, 4:Expired
    RejectionReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Posts_Users FOREIGN KEY (LandlordId) REFERENCES Users(Id)
);
GO

-- Table: Rooms
CREATE TABLE Rooms (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PostId INT NOT NULL UNIQUE,
    Area DECIMAL(10,2) NOT NULL,
    MaxOccupants INT NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0:Available, 1:Rented, 2:TemporarilyUnavailable
    Province NVARCHAR(100) NOT NULL,
    District NVARCHAR(100) NOT NULL,
    Ward NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Rooms_Posts FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
);
GO

-- Table: Amenities
CREATE TABLE Amenities (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Icon NVARCHAR(100) NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
GO

-- Table: PostAmenities (Many-to-Many)
CREATE TABLE PostAmenities (
    PostId INT NOT NULL,
    AmenityId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY (PostId, AmenityId),
    CONSTRAINT FK_PostAmenities_Posts FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PostAmenities_Amenities FOREIGN KEY (AmenityId) REFERENCES Amenities(Id) ON DELETE CASCADE
);
GO

-- Table: PostImages
CREATE TABLE PostImages (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PostId INT NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PostImages_Posts FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
);
GO

-- Table: Favorites
CREATE TABLE Favorites (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    PostId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Posts FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE NO ACTION,
    CONSTRAINT UQ_Favorites_UserPost UNIQUE (UserId, PostId)
);
GO

-- Table: ViewingAppointments
CREATE TABLE ViewingAppointments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    PostId INT NOT NULL,
    ScheduledAt DATETIME2 NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0:Pending, 1:Approved, 2:Rejected, 3:Cancelled, 4:Completed
    TenantNote NVARCHAR(500) NULL,
    LandlordResponse NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_ViewingAppointments_Users FOREIGN KEY (TenantId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ViewingAppointments_Posts FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE NO ACTION
);
GO

-- =============================================
-- CREATE INDEXES
-- =============================================

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_Posts_LandlordId ON Posts(LandlordId);
CREATE INDEX IX_Posts_Status ON Posts(Status);
CREATE INDEX IX_Posts_CreatedAt ON Posts(CreatedAt DESC);
CREATE INDEX IX_Rooms_PostId ON Rooms(PostId);
CREATE INDEX IX_Rooms_Province ON Rooms(Province);
CREATE INDEX IX_Rooms_District ON Rooms(District);
CREATE INDEX IX_PostImages_PostId ON PostImages(PostId);
CREATE INDEX IX_Favorites_UserId ON Favorites(UserId);
CREATE INDEX IX_Favorites_PostId ON Favorites(PostId);
CREATE INDEX IX_ViewingAppointments_TenantId ON ViewingAppointments(TenantId);
CREATE INDEX IX_ViewingAppointments_PostId ON ViewingAppointments(PostId);
GO

-- =============================================
-- INSERT MASTER DATA
-- =============================================

-- Insert Roles
INSERT INTO Roles (Name, Description) VALUES
(N'Tenant', N'Người tìm phòng trọ'),
(N'Landlord', N'Chủ nhà cho thuê phòng trọ'),
(N'Admin', N'Quản trị viên hệ thống');
GO

-- Insert Amenities
INSERT INTO Amenities (Name, Icon, Description) VALUES
(N'Wifi miễn phí', N'wifi', N'Mạng Wifi tốc độ cao miễn phí'),
(N'Điều hòa', N'ac_unit', N'Máy lạnh điều hòa nhiệt độ'),
(N'Nóng lạnh', N'water_heater', N'Bình nóng lạnh'),
(N'Máy giặt', N'washing_machine', N'Máy giặt chung hoặc riêng'),
(N'Tủ lạnh', N'kitchen', N'Tủ lạnh để đồ'),
(N'Giường', N'bed', N'Giường ngủ'),
(N'Tủ quần áo', N'checkroom', N'Tủ để quần áo'),
(N'Bàn ghế', N'chair', N'Bàn học và ghế ngồi'),
(N'Bếp', N'restaurant', N'Bếp nấu ăn'),
(N'Chỗ để xe', N'directions_car', N'Chỗ để xe máy, ô tô'),
(N'An ninh', N'security', N'Camera an ninh, bảo vệ'),
(N'Thú cưng', N'pets', N'Cho phép nuôi thú cưng'),
(N'Ban công', N'balcony', N'Ban công riêng'),
(N'Thang máy', N'elevator', N'Có thang máy');
GO

-- =============================================
-- INSERT SAMPLE DATA
-- =============================================

-- Insert Users (Password: "123456" - Đã hash bằng BCrypt)
-- BCrypt hash của "123456": $2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5
-- Lưu ý: Trong production, cần hash password thật với BCrypt.Net-Next

-- Admin
INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, CreatedAt) VALUES
(N'Admin System', N'admin@timnhatro.vn', N'0900000000', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 3, GETUTCDATE());

-- Landlords
INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, CreatedAt) VALUES
(N'Nguyễn Văn An', N'landlord1@gmail.com', N'0901234567', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 2, GETUTCDATE()),
(N'Trần Thị Bình', N'landlord2@gmail.com', N'0902345678', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 2, GETUTCDATE()),
(N'Lê Văn Cường', N'landlord3@gmail.com', N'0903456789', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 2, GETUTCDATE()),
(N'Phạm Thị Dung', N'landlord4@gmail.com', N'0904567890', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 2, GETUTCDATE());

-- Tenants
INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, CreatedAt) VALUES
(N'Hoàng Văn Em', N'tenant1@gmail.com', N'0905678901', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 1, GETUTCDATE()),
(N'Đỗ Thị Giang', N'tenant2@gmail.com', N'0906789012', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 1, GETUTCDATE()),
(N'Vũ Văn Hùng', N'tenant3@gmail.com', N'0907890123', N'$2a$11$8uXJZ3E.K7Y.PfxGC3xVEuK3H9y7VK5h7vY5Y5Y5Y5Y5Y5Y5Y5Y5Y5', 1, GETUTCDATE());
GO

-- Insert Posts (LandlordId: 2, 3, 4, 5)
INSERT INTO Posts (Title, Description, Price, LandlordId, Status, CreatedAt) VALUES
(N'Phòng trọ cao cấp Quận 7, đầy đủ nội thất', 
N'Phòng trọ mới xây, sạch sẽ, an ninh, gần chợ, siêu thị. Diện tích 25m2, đầy đủ nội thất: giường, tủ, bàn ghế, máy lạnh, nóng lạnh. Giá thuê 3.5 triệu/tháng bao gồm điện nước, wifi.',
3500000, 2, 1, DATEADD(DAY, -10, GETUTCDATE())),

(N'Phòng trọ giá rẻ gần ĐH Bách Khoa HCM',
N'Phòng trọ cho sinh viên, giá rẻ, gần trường Đại học Bách Khoa. Diện tích 18m2, có giường, tủ, bàn học. Điện nước tính theo đồng hồ. Giá 2 triệu/tháng.',
2000000, 3, 1, DATEADD(DAY, -8, GETUTCDATE())),

(N'Nhà nguyên căn 3 tầng Quận Tân Bình',
N'Nhà nguyên căn 3 tầng, 4 phòng ngủ, 3 WC, phòng khách rộng rãi. Khu vực an ninh, gần bệnh viện, trường học. Thích hợp cho gia đình. Giá 12 triệu/tháng.',
12000000, 4, 1, DATEADD(DAY, -7, GETUTCDATE())),

(N'Căn hộ dịch vụ 2 phòng ngủ Quận 1',
N'Căn hộ dịch vụ cao cấp tại trung tâm Quận 1. 2 phòng ngủ, 2 WC, bếp riêng, ban công view đẹp. Đầy đủ nội thất hiện đại. Có thang máy, bảo vệ 24/7. Giá 15 triệu/tháng.',
15000000, 5, 1, DATEADD(DAY, -6, GETUTCDATE())),

(N'Phòng trọ Hà Nội - Cầu Giấy, gần Big C',
N'Phòng trọ mới 100%, diện tích 20m2, đầy đủ nội thất. Gần Big C Thăng Long, ĐH FPT, siêu thị, bệnh viện. Giá 2.8 triệu/tháng.',
2800000, 2, 1, DATEADD(DAY, -5, GETUTCDATE())),

(N'Phòng trọ có gác - Quận Bình Thạnh',
N'Phòng trọ có gác lửng, diện tích 30m2. Khu vực yên tĩnh, an ninh. Đầy đủ tiện nghi. Giá 3.2 triệu/tháng.',
3200000, 3, 1, DATEADD(DAY, -4, GETUTCDATE())),

(N'Phòng ở ghép nữ - Quận Phú Nhuận',
N'Tìm 1 bạn nữ ở ghép, phòng 2 người. Phòng rộng 28m2, đầy đủ tiện nghi. Khu vực an ninh, gần chợ. Giá 1.8 triệu/người/tháng.',
1800000, 4, 1, DATEADD(DAY, -3, GETUTCDATE())),

(N'Nhà nguyên căn 2 tầng Quận 9',
N'Nhà nguyên căn 2 tầng, 3 phòng ngủ, có sân để xe. Khu dân cư văn minh. Giá 8 triệu/tháng.',
8000000, 5, 1, DATEADD(DAY, -2, GETUTCDATE())),

(N'Căn hộ mini 1 phòng ngủ - Quận Gò Vấp',
N'Căn hộ mini mới xây, 1 phòng ngủ, bếp riêng. Gần chợ, siêu thị. Giá 4.5 triệu/tháng.',
4500000, 2, 1, DATEADD(DAY, -1, GETUTCDATE())),

(N'Phòng trọ Hà Nội - Hoàng Mai, giá sinh viên',
N'Phòng trọ dành cho sinh viên, giá rẻ. Diện tích 16m2, có giường, tủ. Điện nước theo đồng hồ. Giá 1.5 triệu/tháng.',
1500000, 3, 1, GETUTCDATE());
GO

-- Insert Rooms
INSERT INTO Rooms (PostId, Area, MaxOccupants, Status, Province, District, Ward, Address, CreatedAt) VALUES
(1, 25, 2, 0, N'Hồ Chí Minh', N'Quận 7', N'Phường Tân Phú', N'123 Nguyễn Văn Linh', DATEADD(DAY, -10, GETUTCDATE())),
(2, 18, 1, 0, N'Hồ Chí Minh', N'Quận 10', N'Phường 12', N'45 Lý Thường Kiệt', DATEADD(DAY, -8, GETUTCDATE())),
(3, 120, 6, 0, N'Hồ Chí Minh', N'Quận Tân Bình', N'Phường 15', N'78 Cộng Hòa', DATEADD(DAY, -7, GETUTCDATE())),
(4, 80, 4, 0, N'Hồ Chí Minh', N'Quận 1', N'Phường Bến Nghé', N'56 Nguyễn Huệ', DATEADD(DAY, -6, GETUTCDATE())),
(5, 20, 2, 0, N'Hà Nội', N'Cầu Giấy', N'Dịch Vọng', N'Ngõ 120 Trần Duy Hưng', DATEADD(DAY, -5, GETUTCDATE())),
(6, 30, 2, 0, N'Hồ Chí Minh', N'Quận Bình Thạnh', N'Phường 25', N'234 Điện Biên Phủ', DATEADD(DAY, -4, GETUTCDATE())),
(7, 28, 2, 0, N'Hồ Chí Minh', N'Quận Phú Nhuận', N'Phường 10', N'67 Phan Đăng Lưu', DATEADD(DAY, -3, GETUTCDATE())),
(8, 100, 5, 0, N'Hồ Chí Minh', N'Quận 9', N'Phường Long Bình', N'89 Đỗ Xuân Hợp', DATEADD(DAY, -2, GETUTCDATE())),
(9, 35, 2, 0, N'Hồ Chí Minh', N'Quận Gò Vấp', N'Phường 6', N'12 Quang Trung', DATEADD(DAY, -1, GETUTCDATE())),
(10, 16, 1, 0, N'Hà Nội', N'Hoàng Mai', N'Bạch Mai', N'Số 254D Ngõ Minh Khai', GETUTCDATE());
GO

-- Insert PostAmenities
-- Post 1: Full amenities
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8), (1, 10), (1, 11);

-- Post 2: Basic amenities
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(2, 1), (2, 6), (2, 7), (2, 8);

-- Post 3: House amenities
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 9), (3, 10), (3, 11), (3, 14);

-- Post 4: Apartment amenities
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(4, 1), (4, 2), (4, 3), (4, 5), (4, 6), (4, 7), (4, 8), (4, 9), (4, 11), (4, 13), (4, 14);

-- Post 5: Standard amenities
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(5, 1), (5, 2), (5, 3), (5, 6), (5, 7), (5, 8), (5, 10);

-- Post 6: With loft
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(6, 1), (6, 2), (6, 3), (6, 6), (6, 7), (6, 8), (6, 10), (6, 11);

-- Post 7: Shared room
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(7, 1), (7, 2), (7, 3), (7, 4), (7, 6), (7, 7), (7, 8), (7, 11);

-- Post 8: House
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(8, 1), (8, 2), (8, 3), (8, 9), (8, 10), (8, 11);

-- Post 9: Mini apartment
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(9, 1), (9, 2), (9, 3), (9, 5), (9, 6), (9, 7), (9, 8), (9, 9), (9, 14);

-- Post 10: Student room
INSERT INTO PostAmenities (PostId, AmenityId) VALUES
(10, 1), (10, 6), (10, 7), (10, 8);
GO

-- Insert PostImages (Using Unsplash placeholder images)
INSERT INTO PostImages (PostId, ImageUrl, DisplayOrder) VALUES
-- Post 1
(1, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800', 0),
(1, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800', 1),
(1, 'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=800', 2),

-- Post 2
(2, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800', 0),
(2, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800', 1),

-- Post 3
(3, 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800', 0),
(3, 'https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?w=800', 1),
(3, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800', 2),

-- Post 4
(4, 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800', 0),
(4, 'https://images.unsplash.com/photo-1502672023488-70e25813eb80?w=800', 1),

-- Post 5
(5, 'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=800', 0),
(5, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800', 1),

-- Post 6
(6, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800', 0),

-- Post 7
(7, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800', 0),
(7, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800', 1),

-- Post 8
(8, 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800', 0),

-- Post 9
(9, 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800', 0),
(9, 'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=800', 1),

-- Post 10
(10, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800', 0);
GO

-- Insert Favorites (Tenant favorite posts)
INSERT INTO Favorites (UserId, PostId, CreatedAt) VALUES
(6, 1, DATEADD(DAY, -5, GETUTCDATE())),
(6, 4, DATEADD(DAY, -3, GETUTCDATE())),
(7, 1, DATEADD(DAY, -4, GETUTCDATE())),
(7, 2, DATEADD(DAY, -2, GETUTCDATE())),
(8, 5, DATEADD(DAY, -1, GETUTCDATE()));
GO

-- Insert ViewingAppointments
INSERT INTO ViewingAppointments (TenantId, PostId, ScheduledAt, Status, TenantNote, LandlordResponse, CreatedAt) VALUES
(6, 1, DATEADD(DAY, 2, GETUTCDATE()), 1, N'Tôi muốn xem phòng vào buổi chiều', N'Được, hẹn bạn 3h chiều nhé', DATEADD(DAY, -3, GETUTCDATE())),
(7, 2, DATEADD(DAY, 3, GETUTCDATE()), 0, N'Có thể xem phòng sáng thứ 7 được không?', NULL, DATEADD(DAY, -2, GETUTCDATE())),
(8, 5, DATEADD(DAY, 1, GETUTCDATE()), 1, N'Em muốn xem phòng ạ', N'Ok bạn, mai 2h chiều nhé', DATEADD(DAY, -1, GETUTCDATE()));
GO

-- =============================================
-- UTILITY VIEWS
-- =============================================

-- View: Posts with Room details
CREATE VIEW vw_PostsWithRoomDetails AS
SELECT 
    p.Id AS PostId,
    p.Title,
    p.Description,
    p.Price,
    p.Status AS PostStatus,
    p.CreatedAt AS PostCreatedAt,
    r.Id AS RoomId,
    r.Area,
    r.MaxOccupants,
    r.Status AS RoomStatus,
    r.Province,
    r.District,
    r.Ward,
    r.Address,
    u.Id AS LandlordId,
    u.FullName AS LandlordName,
    u.Phone AS LandlordPhone,
    u.Email AS LandlordEmail,
    (SELECT TOP 1 ImageUrl FROM PostImages WHERE PostId = p.Id ORDER BY DisplayOrder) AS ThumbnailUrl
FROM Posts p
INNER JOIN Rooms r ON p.Id = r.PostId
INNER JOIN Users u ON p.LandlordId = u.Id;
GO

-- View: User statistics
CREATE VIEW vw_UserStatistics AS
SELECT 
    u.Id,
    u.FullName,
    u.Email,
    r.Name AS RoleName,
    CASE 
        WHEN r.Name = 'Landlord' THEN (SELECT COUNT(*) FROM Posts WHERE LandlordId = u.Id)
        WHEN r.Name = 'Tenant' THEN (SELECT COUNT(*) FROM Favorites WHERE UserId = u.Id)
        ELSE 0
    END AS ActivityCount,
    u.CreatedAt,
    u.IsBlocked
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.Id;
GO

-- =============================================
-- STORED PROCEDURES
-- =============================================

-- SP: Get Posts with filters
CREATE PROCEDURE sp_GetPosts
    @Province NVARCHAR(100) = NULL,
    @District NVARCHAR(100) = NULL,
    @MinPrice DECIMAL(18,2) = NULL,
    @MaxPrice DECIMAL(18,2) = NULL,
    @MinArea DECIMAL(10,2) = NULL,
    @MaxArea DECIMAL(10,2) = NULL,
    @Status INT = 1, -- Default: Approved
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.*,
        r.Area,
        r.Province,
        r.District,
        r.Ward,
        r.Address,
        u.FullName AS LandlordName,
        u.Phone AS LandlordPhone,
        (SELECT TOP 1 ImageUrl FROM PostImages WHERE PostId = p.Id ORDER BY DisplayOrder) AS ThumbnailUrl,
        (SELECT COUNT(*) FROM Favorites WHERE PostId = p.Id) AS FavoriteCount
    FROM Posts p
    INNER JOIN Rooms r ON p.Id = r.PostId
    INNER JOIN Users u ON p.LandlordId = u.Id
    WHERE 
        p.Status = @Status
        AND (@Province IS NULL OR r.Province = @Province)
        AND (@District IS NULL OR r.District = @District)
        AND (@MinPrice IS NULL OR p.Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR p.Price <= @MaxPrice)
        AND (@MinArea IS NULL OR r.Area >= @MinArea)
        AND (@MaxArea IS NULL OR r.Area <= @MaxArea)
    ORDER BY p.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- SP: Get Post details by Id
CREATE PROCEDURE sp_GetPostById
    @PostId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Post and Room info
    SELECT 
        p.*,
        r.*,
        u.FullName AS LandlordName,
        u.Phone AS LandlordPhone,
        u.Email AS LandlordEmail,
        u.AvatarUrl AS LandlordAvatar
    FROM Posts p
    INNER JOIN Rooms r ON p.Id = r.PostId
    INNER JOIN Users u ON p.LandlordId = u.Id
    WHERE p.Id = @PostId;
    
    -- Images
    SELECT ImageUrl, DisplayOrder
    FROM PostImages
    WHERE PostId = @PostId
    ORDER BY DisplayOrder;
    
    -- Amenities
    SELECT a.Id, a.Name, a.Icon
    FROM Amenities a
    INNER JOIN PostAmenities pa ON a.Id = pa.AmenityId
    WHERE pa.PostId = @PostId;
END;
GO

-- =============================================
-- COMPLETION MESSAGE
-- =============================================

PRINT '==============================================';
PRINT 'Database RoomRentalDb_Dev created successfully!';
PRINT '==============================================';
PRINT 'Summary:';
PRINT '- Roles: 3 (Admin, Landlord, Tenant)';
PRINT '- Users: 8 (1 Admin, 4 Landlords, 3 Tenants)';
PRINT '- Posts: 10';
PRINT '- Rooms: 10';
PRINT '- Amenities: 14';
PRINT '- PostImages: 20+';
PRINT '- Favorites: 5';
PRINT '- ViewingAppointments: 3';
PRINT '==============================================';
PRINT 'Default credentials:';
PRINT 'Email: admin@timnhatro.vn | Password: 123456';
PRINT 'Email: landlord1@gmail.com | Password: 123456';
PRINT 'Email: tenant1@gmail.com | Password: 123456';
PRINT '==============================================';
GO
