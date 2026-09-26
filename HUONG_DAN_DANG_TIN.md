# Hướng Dẫn Sử Dụng Chức Năng Đăng Tin Phòng Trọ

## 📋 Tổng Quan

Chức năng đăng tin phòng trọ cho phép chủ trọ (Landlord) tạo, quản lý và chỉnh sửa các tin đăng cho thuê phòng trên hệ thống.

## 🎯 Các Tính Năng Đã Được Thêm

### 1. **Trang Quản Lý Tin Đăng** (`/landlord/posts`)
- Hiển thị danh sách tất cả tin đăng của chủ trọ
- Xem trạng thái tin đăng: Chờ duyệt, Đã duyệt, Bị từ chối, Đã ẩn, Hết hạn
- Xem trạng thái phòng: Còn trống, Đã cho thuê, Tạm ngưng
- Các thao tác:
  - Xem chi tiết tin đăng
  - Chỉnh sửa tin đăng
  - Xóa tin đăng

### 2. **Trang Tạo Tin Đăng Mới** (`/landlord/posts/create`)
Cho phép chủ trọ tạo tin đăng mới với các thông tin:

#### Thông tin cơ bản:
- **Tiêu đề tin đăng** (bắt buộc)
- **Mô tả chi tiết** (bắt buộc)
- **Giá thuê** (VNĐ/tháng, bắt buộc)
- **Diện tích** (m², bắt buộc)
- **Số người tối đa** (bắt buộc)

#### Địa chỉ:
- **Tỉnh/Thành phố** (bắt buộc)
- **Quận/Huyện**
- **Phường/Xã**
- **Địa chỉ cụ thể** (bắt buộc)

#### Tiện ích:
- Chọn các tiện ích có sẵn từ danh sách hệ thống
- Có thể chọn nhiều tiện ích

#### Hình ảnh:
- Thêm hình ảnh phòng trọ qua URL
- Có thể thêm nhiều ảnh
- Xóa ảnh không cần thiết

### 3. **Trang Chỉnh Sửa Tin Đăng** (`/landlord/posts/:id/edit`)
- Tương tự trang tạo mới nhưng với dữ liệu đã có
- Tự động tải thông tin tin đăng hiện tại
- Sau khi chỉnh sửa, tin sẽ được Admin duyệt lại

## 🚀 Cách Sử Dụng

### Bước 1: Đăng nhập với tài khoản Chủ trọ
Đảm bảo bạn đã đăng nhập với tài khoản có role **Landlord**.

### Bước 2: Truy cập trang quản lý tin đăng
- Click vào menu **"Tin đăng"** trong sidebar
- Hoặc truy cập trực tiếp: `http://localhost:5173/landlord/posts`

### Bước 3: Tạo tin đăng mới
1. Click nút **"Đăng tin mới"**
2. Điền đầy đủ thông tin vào form:
   - Nhập tiêu đề hấp dẫn (VD: "Phòng trọ cao cấp gần ĐH Bách Khoa")
   - Viết mô tả chi tiết về phòng
   - Nhập giá thuê, diện tích, số người
   - Điền địa chỉ đầy đủ
   - Chọn các tiện ích phòng có
   - Thêm hình ảnh (nhập URL ảnh)
3. Click **"Đăng tin"**
4. Tin đăng sẽ được chuyển sang trạng thái **"Chờ duyệt"**
5. Admin sẽ duyệt tin đăng trước khi hiển thị công khai

### Bước 4: Quản lý tin đăng
- Xem danh sách tin đăng với trạng thái
- Chỉnh sửa tin đăng khi cần
- Xóa tin đăng không cần thiết

## 📝 Lưu Ý Quan Trọng

### Trạng thái tin đăng:
- **Chờ duyệt (Pending)**: Tin mới đăng hoặc vừa chỉnh sửa, đang chờ Admin duyệt
- **Đã duyệt (Approved)**: Tin đã được Admin duyệt, hiển thị công khai
- **Bị từ chối (Rejected)**: Tin bị Admin từ chối, có lý do từ chối
- **Đã ẩn (Hidden)**: Tin đã bị ẩn, không hiển thị công khai
- **Hết hạn (Expired)**: Tin đã hết hạn đăng

### Quy trình phê duyệt:
1. Chủ trọ đăng tin → Trạng thái: **Chờ duyệt**
2. Admin xem xét tin đăng
3. Admin duyệt → Trạng thái: **Đã duyệt** (hiển thị công khai)
4. Hoặc Admin từ chối → Trạng thái: **Bị từ chối** (kèm lý do)
5. Chủ trọ chỉnh sửa tin đã duyệt → Trạng thái quay lại **Chờ duyệt**

### Validation:
- Các trường đánh dấu (*) là bắt buộc
- Giá thuê và diện tích phải lớn hơn 0
- Địa chỉ phải đầy đủ (ít nhất có Tỉnh/Thành phố và Địa chỉ cụ thể)

## 🔧 Các File Đã Tạo/Chỉnh Sửa

### Files mới tạo:
1. `frontend/src/pages/landlord/CreatePostPage.tsx` - Trang tạo/chỉnh sửa tin đăng
2. `frontend/src/pages/landlord/LandlordPostsPage.tsx` - Trang quản lý tin đăng

### Files đã chỉnh sửa:
1. `frontend/src/routes/AppRoutes.tsx` - Thêm routes cho các trang mới

### API đã sẵn có (không cần thay đổi):
- `POST /api/bai-dang` - Tạo tin đăng mới
- `GET /api/bai-dang/cua-toi` - Lấy danh sách tin đăng của chủ trọ
- `GET /api/bai-dang/:id` - Xem chi tiết tin đăng
- `PUT /api/bai-dang/:id` - Cập nhật tin đăng
- `DELETE /api/bai-dang/:id` - Xóa tin đăng
- `GET /api/tien-ich` - Lấy danh sách tiện ích

## 🎨 Giao Diện

Giao diện được thiết kế với:
- **Tailwind CSS** - Styling hiện đại
- **React Icons** (FiPlus, FiEdit2, FiTrash2, FiEye, FiMapPin, FiImage)
- **React Toastify** - Thông báo
- **Responsive Design** - Tương thích mọi thiết bị

## 🐛 Xử Lý Lỗi

Hệ thống sẽ hiển thị thông báo lỗi khi:
- Không điền đầy đủ thông tin bắt buộc
- Không thể kết nối API
- Không có quyền truy cập
- Lỗi từ server

## ✅ Kiểm Tra

Để kiểm tra chức năng hoạt động:

```bash
# 1. Chạy backend
cd backend/RoomRental.BackEnd
dotnet run

# 2. Chạy frontend
cd frontend
npm run dev

# 3. Truy cập http://localhost:5173
# 4. Đăng nhập với tài khoản Landlord
# 5. Vào menu "Tin đăng" và thử tạo tin mới
```

## 📞 Hỗ Trợ

Nếu có vấn đề, kiểm tra:
1. Backend API đang chạy (`https://localhost:7127`)
2. Token xác thực còn hợp lệ
3. Console browser để xem lỗi JavaScript
4. Network tab để xem lỗi API

---

**Chúc bạn sử dụng thành công! 🎉**
