# 🎨 FRONTEND GUIDE - Room Rental System

## ✅ ĐÃ HOÀN THÀNH

### 📦 Setup & Configuration
- [x] Types & Interfaces (auth, user, post, common)
- [x] Constants & Helpers
- [x] API Service với Axios interceptors
- [x] AuthContext & useAuth hook
- [x] Tailwind CSS v4 configuration
- [x] React Router DOM setup
- [x] React Toastify cho notifications

### 🎨 Components
- [x] **Common Components:**
  - Button (primary, secondary, danger, outline)
  - Input (với label, error, helper text)
  - Card
  - Spinner (fullScreen option)
  - Modal
  - Header (responsive, với menu)
  - Footer

- [x] **Room Components:**
  - RoomCard (hiển thị phòng trong list)

### 📄 Pages Đã Tạo
- [x] **Public Pages:**
  - HomePage (landing page với hero, features, CTA)
  - RoomListPage (tìm kiếm, filter phòng)
  - RoomDetailPage (chi tiết phòng, slider ảnh, contact)

- [x] **Auth Pages:**
  - LoginPage (form đăng nhập)
  - RegisterPage (form đăng ký với role selection)

- [x] **Protected Routes:**
  - ProtectedRoute component với role-based access
  - Placeholder pages cho Tenant, Landlord, Admin

### 🔧 Services
- [x] authService (register, login)
- [x] userService (getProfile, updateProfile, changePassword)
- [x] postService (CRUD posts, search)

### 🎯 Routing
- [x] PublicLayout với Header & Footer
- [x] AppRoutes với protected routes
- [x] Role-based redirects

---

## 🚀 CÁCH CHẠY FRONTEND

### 1. Cài đặt dependencies (nếu chưa)
```bash
cd frontend
npm install
```

### 2. Chạy development server
```bash
npm run dev
```

Frontend sẽ chạy tại: `http://localhost:5174`

### 3. Build production
```bash
npm run build
```

---

## 📁 CẤU TRÚC FRONTEND

```
frontend/src/
├── components/
│   ├── common/
│   │   ├── Button.tsx           ✅ Done
│   │   ├── Input.tsx            ✅ Done
│   │   ├── Card.tsx             ✅ Done
│   │   ├── Spinner.tsx          ✅ Done
│   │   ├── Modal.tsx            ✅ Done
│   │   ├── Header.tsx           ✅ Done
│   │   └── Footer.tsx           ✅ Done
│   └── room/
│       └── RoomCard.tsx         ✅ Done
│
├── pages/
│   ├── public/
│   │   ├── HomePage.tsx         ✅ Done
│   │   ├── RoomListPage.tsx    ✅ Done
│   │   └── RoomDetailPage.tsx  ✅ Done
│   ├── auth/
│   │   ├── LoginPage.tsx        ✅ Done
│   │   └── RegisterPage.tsx     ✅ Done
│   ├── tenant/                  🔲 Placeholder
│   ├── landlord/                🔲 Placeholder
│   └── admin/                   🔲 Placeholder
│
├── services/
│   ├── api.ts                   ✅ Done
│   ├── authService.ts           ✅ Done
│   ├── userService.ts           ✅ Done
│   └── postService.ts           ✅ Done
│
├── contexts/
│   └── AuthContext.tsx          ✅ Done
│
├── hooks/
│   └── useAuth.ts               ✅ Done
│
├── routes/
│   ├── AppRoutes.tsx            ✅ Done
│   └── ProtectedRoute.tsx       ✅ Done
│
├── layouts/
│   └── PublicLayout.tsx         ✅ Done
│
├── types/
│   ├── common.types.ts          ✅ Done
│   ├── auth.types.ts            ✅ Done
│   ├── user.types.ts            ✅ Done
│   └── post.types.ts            ✅ Done
│
├── utils/
│   ├── constants.ts             ✅ Done
│   └── helpers.ts               ✅ Done
│
├── App.tsx                      ✅ Done
├── main.tsx                     ✅ Done
└── index.css                    ✅ Done
```

---

## 🎨 PAGES ĐÃ CÓ

### 1. HomePage (`/`)
- Hero section với search bar
- Features section
- How it works (cho Tenant & Landlord)
- CTA buttons

### 2. RoomListPage (`/rooms`)
- Hiển thị danh sách phòng (grid)
- Filter theo province, district
- RoomCard components
- Empty state

### 3. RoomDetailPage (`/rooms/:id`)
- Image slider với thumbnails
- Thông tin chi tiết phòng
- Amenities list
- Landlord contact info
- CTA buttons (gọi điện, đặt lịch, yêu thích)

### 4. LoginPage (`/login`)
- Form đăng nhập
- Remember me checkbox
- Forgot password link
- Social login buttons
- Redirect based on role

### 5. RegisterPage (`/register`)
- Role selection (Tenant/Landlord)
- Full registration form
- Password confirmation
- Terms checkbox
- Auto login after register

---

## 🔐 AUTHENTICATION FLOW

### 1. Register
```typescript
const { register } = useAuth();
await register({
  fullName: 'Nguyễn Văn A',
  email: 'test@example.com',
  phone: '0123456789',
  password: 'password',
  roleName: 'Tenant' // or 'Landlord'
});
// Auto redirect based on role
```

### 2. Login
```typescript
const { login } = useAuth();
await login({
  email: 'test@example.com',
  password: 'password'
});
// Auto redirect based on role
```

### 3. Logout
```typescript
const { logout } = useAuth();
logout();
// Clear token & user from localStorage
```

### 4. Get Current User
```typescript
const { user, isAuthenticated, isLandlord, isTenant, isAdmin } = useAuth();
```

---

## 🎯 PROTECTED ROUTES

```typescript
// Chỉ Tenant
<ProtectedRoute allowedRoles={['Tenant']}>
  <TenantPage />
</ProtectedRoute>

// Chỉ Landlord
<ProtectedRoute allowedRoles={['Landlord']}>
  <LandlordPage />
</ProtectedRoute>

// Chỉ Admin
<ProtectedRoute allowedRoles={['Admin']}>
  <AdminPage />
</ProtectedRoute>

// Bất kỳ user đã login
<ProtectedRoute>
  <ProfilePage />
</ProtectedRoute>
```

---

## 📡 API CALLS

### Example: Tìm kiếm phòng
```typescript
import { postService } from '../services/postService';

const fetchRooms = async () => {
  try {
    const response = await postService.searchPosts({
      province: 'Hồ Chí Minh',
      district: 'Quận 7'
    });
    setPosts(response.data);
  } catch (error) {
    toast.error('Không thể tải danh sách phòng');
  }
};
```

### Example: Get user profile
```typescript
import { userService } from '../services/userService';

const fetchProfile = async () => {
  const response = await userService.getProfile();
  setProfile(response.data);
};
```

---

## 🎨 TAILWIND CSS v4

### Đã cấu hình:
- `@tailwindcss/postcss` plugin
- Custom scrollbar styles
- Line clamp utilities
- Responsive classes

### Sử dụng:
```tsx
<Button variant="primary" size="lg" fullWidth>
  Click me
</Button>

<Input
  label="Email"
  type="email"
  error="Email không hợp lệ"
  required
/>

<Card hoverable>
  <RoomCard post={post} />
</Card>
```

---

## 🔄 TOAST NOTIFICATIONS

```typescript
import { toast } from 'react-toastify';

// Success
toast.success('Đăng nhập thành công!');

// Error
toast.error('Email hoặc mật khẩu không đúng');

// Info
toast.info('Vui lòng kiểm tra email');

// Warning
toast.warning('Bạn chưa điền đủ thông tin');
```

---

## 📱 RESPONSIVE DESIGN

Tất cả components và pages đã responsive:
- Mobile first approach
- Tailwind breakpoints: `sm:`, `md:`, `lg:`, `xl:`
- Mobile menu trong Header
- Grid layouts tự động adjust

---

## 🔲 CÒN THIẾU (TODO)

### Tenant Pages
- [ ] ProfilePage (xem/sửa profile)
- [ ] FavoritesPage (danh sách yêu thích)
- [ ] AppointmentsPage (lịch hẹn của tenant)

### Landlord Pages
- [ ] DashboardPage (thống kê)
- [ ] MyPostsPage (quản lý tin đăng)
- [ ] CreatePostPage (form đăng tin)
- [ ] EditPostPage (form sửa tin)
- [ ] AppointmentManagePage (xác nhận lịch hẹn)

### Admin Pages
- [ ] DashboardPage (thống kê tổng quan)
- [ ] UserManagementPage (quản lý users)
- [ ] PostApprovalPage (duyệt tin)
- [ ] PostManagementPage (quản lý tất cả tin)
- [ ] AmenityManagementPage (CRUD amenities)

### Components
- [ ] RoomFilter (advanced filters)
- [ ] RoomSearch (search bar component)
- [ ] AppointmentForm
- [ ] AppointmentList
- [ ] Pagination

### Features
- [ ] Favorite functionality
- [ ] Appointment booking
- [ ] Image upload
- [ ] Real-time notifications
- [ ] Chat (optional)

---

## 🧪 TESTING

### Test Account (từ Backend):
- **Admin:** admin@roomrental.com / Admin@123
- **Landlord:** landlord1@example.com / Landlord@123
- **Tenant:** tenant1@example.com / Tenant@123

### Test Flow:
1. Đăng ký tài khoản mới
2. Login
3. Xem danh sách phòng
4. Xem chi tiết phòng
5. Test protected routes

---

## 🐛 TROUBLESHOOTING

### Lỗi CORS:
- Backend đã config CORS cho `http://localhost:5173`
- Nếu frontend chạy port khác, cần update backend

### Lỗi 401 Unauthorized:
- Token đã hết hạn hoặc không hợp lệ
- Logout và login lại

### Tailwind classes không hoạt động:
- Đã fix bằng cách dùng `@tailwindcss/postcss`
- Restart dev server nếu cần

---

## 📞 NEXT STEPS

1. **Hoàn thiện Tenant Pages** (Profile, Favorites, Appointments)
2. **Hoàn thiện Landlord Pages** (Create/Edit Post, Manage Appointments)
3. **Hoàn thiện Admin Pages** (Approve Posts, Manage Users)
4. **Thêm Favorite APIs** (Backend + Frontend)
5. **Thêm Appointment APIs** (Backend + Frontend)
6. **Image Upload** (Cloudinary or local storage)
7. **Testing & Bug Fixes**
8. **Deployment**

---

**Last Updated:** 2026-09-14  
**Current Status:** ✅ Core Frontend đã hoàn thành (~40%)
