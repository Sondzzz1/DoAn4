# 📚 HƯỚNG DẪN SỬ DỤNG API

## 🚀 Khởi động API

```bash
cd backend/RoomRental.API
dotnet run
```

API sẽ chạy tại: **http://localhost:5168**  
Swagger UI: **http://localhost:5168/swagger**

---

## 👥 TÀI KHOẢN MẶC ĐỊNH

### Admin (Đã được seed tự động)
- **Email:** `admin@roomrental.com`
- **Password:** `Admin@123`
- **Role:** Admin

---

## 🔐 AUTHENTICATION APIs

### 1. Đăng ký tài khoản

**Endpoint:** `POST /api/auth/register`

**Request Body:**
```json
{
  "fullName": "Nguyễn Văn A",
  "email": "nguyenvana@gmail.com",
  "phone": "0987654321",
  "password": "Password@123",
  "confirmPassword": "Password@123",
  "roleName": "Tenant"
}
```

**RoleName có thể là:**
- `Tenant` - Người tìm trọ
- `Landlord` - Chủ nhà trọ
- ❌ `Admin` - Không được đăng ký trực tiếp

**Response thành công:**
```json
{
  "success": true,
  "message": "Đăng ký tài khoản thành công",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 1,
    "fullName": "Nguyễn Văn A",
    "email": "nguyenvana@gmail.com",
    "role": "Tenant",
    "avatarUrl": null
  },
  "errors": null
}
```

---

### 2. Đăng nhập

**Endpoint:** `POST /api/auth/login`

**Request Body:**
```json
{
  "email": "admin@roomrental.com",
  "password": "Admin@123"
}
```

**Response thành công:**
```json
{
  "success": true,
  "message": "Đăng nhập thành công",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 1,
    "fullName": "Administrator",
    "email": "admin@roomrental.com",
    "role": "Admin",
    "avatarUrl": null
  },
  "errors": null
}
```

---

## 🔑 SỬ DỤNG JWT TOKEN

Sau khi đăng nhập/đăng ký thành công, bạn nhận được **JWT Token**.

Sử dụng token này cho các API cần xác thực:

**Header:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Trong Swagger:**
1. Click nút **Authorize** (ổ khóa)
2. Nhập: `Bearer {your_token}`
3. Click **Authorize**

---

## 🗄️ DATABASE

**Server:** `localhost\SQLEXPRESS`  
**Database:** `RoomRentalDb_Dev`  
**Authentication:** Windows Authentication

### Kiểm tra database với SQL Query:

```sql
-- Xem tất cả bảng
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Xem các Role
SELECT * FROM Roles;

-- Xem User Admin
SELECT u.*, r.Name as RoleName 
FROM Users u 
INNER JOIN Roles r ON u.RoleId = r.Id
WHERE r.Name = 'Admin';

-- Xem Amenities
SELECT * FROM Amenities;
```

---

## 📦 TIỆN ÍCH ĐÃ SEED

10 tiện ích mặc định:
1. Wifi
2. Điều hòa
3. Nóng lạnh
4. Máy giặt
5. Tủ lạnh
6. Chỗ để xe
7. WC riêng
8. Ban công
9. Bếp
10. An ninh

---

## ⚠️ XỬ LÝ LỖI

### Lỗi Validation
```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "errors": [
    "Email là bắt buộc",
    "Mật khẩu phải từ 6 đến 100 ký tự"
  ]
}
```

### Lỗi Business Logic
```json
{
  "success": false,
  "message": "Email đã được sử dụng",
  "data": null,
  "errors": null
}
```

---

## 🔧 TROUBLESHOOTING

### API không khởi động được?
- Kiểm tra port 5168 có bị chiếm không
- Kiểm tra SQL Server đang chạy: `Get-Service | Where {$_.Name -like "*SQL*"}`

### Không kết nối được Database?
- Mở SQL Server Management Studio
- Kết nối với: `localhost\SQLEXPRESS`
- Kiểm tra database `RoomRentalDb_Dev` đã tồn tại

### Lỗi "Email đã được sử dụng"?
- Email phải unique, dùng email khác

---

## 📝 GHI CHÚ

- Token JWT có hiệu lực: **24 giờ** (1440 phút)
- Password được hash bằng **BCrypt**
- Tất cả API response đều theo format **ApiResponse<T>**
- API hỗ trợ **CORS** cho `http://localhost:5173` (Frontend)

---

## 🎯 TIẾP THEO

Các API sẽ được phát triển tiếp:
- [ ] User Management (Profile, Change Password)
- [ ] Post Management (CRUD Posts)
- [ ] Favorite Management
- [ ] Appointment Management
- [ ] Admin APIs (Approve/Reject Posts, Manage Users)
- [ ] Amenity Management

---

_Cập nhật lần cuối: 2026-09-10_
