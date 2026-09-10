# HỆ THỐNG TÌM KIẾM VÀ CHO THUÊ PHÒNG TRỌ TRỰC TUYẾN

## 📋 Giới thiệu

Dự án xây dựng hệ thống tìm kiếm và cho thuê phòng trọ trực tuyến với 3 Actor:
- **Tenant (Người tìm trọ)**: Tìm kiếm, xem phòng, đặt lịch xem phòng
- **Landlord (Chủ nhà trọ)**: Đăng tin cho thuê, quản lý tin đăng, xác nhận lịch hẹn
- **Admin (Quản trị viên)**: Duyệt tin đăng, quản lý người dùng, quản lý hệ thống

## 🛠️ Công nghệ sử dụng

### Backend
- ASP.NET Core Web API 8.0
- Entity Framework Core 8.0
- SQL Server
- JWT Authentication
- BCrypt (Password hashing)
- Swagger/OpenAPI

### Frontend
- React 19
- TypeScript
- Vite
- Tailwind CSS
- React Router DOM
- Axios
- React Hook Form
- React Icons
- React Toastify
- Dayjs

## 📁 Cấu trúc dự án

```
RoomRentalSystem/
│
├── backend/
│   ├── RoomRental.API/              # Web API Layer
│   ├── RoomRental.Application/      # Business Logic Layer
│   ├── RoomRental.Domain/           # Domain Entities & Enums
│   ├── RoomRental.Infrastructure/   # Data Access Layer
│   └── RoomRental.sln
│
└── frontend/
    ├── src/
    │   ├── components/              # Reusable components
    │   ├── pages/                   # Page components
    │   ├── layouts/                 # Layout components
    │   ├── routes/                  # Route configuration
    │   ├── services/                # API services
    │   ├── contexts/                # React Context
    │   ├── hooks/                   # Custom hooks
    │   ├── types/                   # TypeScript types
    │   └── utils/                   # Utility functions
    └── package.json
```

## 🚀 Cách chạy Backend

### Yêu cầu
- .NET SDK 8.0 trở lên
- SQL Server
- Visual Studio 2022 hoặc VS Code

### Các bước chạy Backend

1. **Mở terminal tại thư mục backend:**
   ```bash
   cd backend
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build solution:**
   ```bash
   dotnet build
   ```

4. **Chạy API (sau khi đã tạo database):**
   ```bash
   cd RoomRental.API
   dotnet run
   ```

5. **Backend sẽ chạy tại:**
   - API: `http://localhost:5000`
   - Swagger UI: `http://localhost:5000/swagger`

## 🎨 Cách chạy Frontend

### Yêu cầu
- Node.js 18 trở lên
- npm hoặc yarn

### Các bước chạy Frontend

1. **Mở terminal tại thư mục frontend:**
   ```bash
   cd frontend
   ```

2. **Cài đặt dependencies (nếu chưa cài):**
   ```bash
   npm install
   ```

3. **Chạy development server:**
   ```bash
   npm run dev
   ```

4. **Frontend sẽ chạy tại:**
   - `http://localhost:5173`

5. **Build production:**
   ```bash
   npm run build
   ```

## 📦 Packages đã cài đặt

### Backend Packages

#### RoomRental.API
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.11)
- Swashbuckle.AspNetCore (10.2.3)
- Microsoft.EntityFrameworkCore.Design (8.0.11)

#### RoomRental.Application
- BCrypt.Net-Next (4.0.3)

#### RoomRental.Infrastructure
- Microsoft.EntityFrameworkCore.SqlServer (8.0.11)
- Microsoft.EntityFrameworkCore.Tools (8.0.11)

### Frontend Packages

#### Dependencies
- react (19.2.8)
- react-dom (19.2.8)
- react-router-dom (7.18.3)
- axios (1.20.0)
- react-hook-form (7.87.0)
- react-icons (5.7.0)
- react-toastify (11.1.0)
- dayjs (1.11.23)
- clsx (2.1.1)

#### Dev Dependencies
- typescript (6.0.2)
- vite (8.2.2)
- tailwindcss (4.3.3)
- postcss (8.5.28)
- autoprefixer (10.5.5)
- @vitejs/plugin-react (6.1.0)
- eslint (10.9.0)

## 🔗 Project References

```
RoomRental.API
├── → RoomRental.Application
└── → RoomRental.Infrastructure

RoomRental.Application
└── → RoomRental.Domain

RoomRental.Infrastructure
└── → RoomRental.Domain
```

## ✅ Kiểm tra Build

### Backend
```bash
cd backend
dotnet build
```

**Kết quả:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Frontend
```bash
cd frontend
npm run build
```

## 📝 Ghi chú quan trọng

### Backend
- ✅ KHÔNG sử dụng ASP.NET Core Identity
- ✅ Tự xây dựng User/Role Entity
- ✅ Sử dụng BCrypt để hash password
- ✅ DbContext trực tiếp trong Service (KHÔNG Repository Pattern)
- ✅ JWT Authentication tự build
- ✅ Kiến trúc đơn giản: Controller → Service → DbContext → SQL Server

### Frontend
- ✅ Sử dụng Tailwind CSS cho styling
- ✅ React Router DOM cho routing
- ✅ Axios cho API calls
- ✅ React Hook Form cho form validation
- ✅ React Context cho state management
- ✅ Giao diện hoàn toàn bằng Tiếng Việt

## 🎯 Trạng thái hiện tại

### ✅ Đã hoàn thành
- [x] Tạo cấu trúc Backend (4 projects)
- [x] Thiết lập Project References
- [x] Cài đặt NuGet Packages
- [x] Tạo Frontend với Vite + React + TypeScript
- [x] Cài đặt Tailwind CSS
- [x] Cài đặt tất cả npm packages
- [x] Tạo cấu trúc thư mục Frontend
- [x] Build Backend thành công
- [x] Cấu hình environment variables

### ⏳ Chưa thực hiện
- [ ] Tạo Entity (User, Role, Post, Room, etc.)
- [ ] Tạo DbContext
- [ ] Cấu hình SQL Server Connection String
- [ ] Tạo Migration
- [ ] Tạo Authentication & JWT
- [ ] Tạo API Controllers
- [ ] Tạo Services
- [ ] Tạo Frontend UI Components
- [ ] Kết nối Frontend với Backend

## 📞 Liên hệ

Dự án đồ án tốt nghiệp - Phát triển bởi Sinh viên

---

**Lưu ý:** Đây là phiên bản MVP đầu tiên. Các tính năng nâng cao (Chat, Payment, Maps) sẽ được bổ sung sau.
