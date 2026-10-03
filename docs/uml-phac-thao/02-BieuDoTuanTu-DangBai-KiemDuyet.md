# Phác thảo tuần tự - Đăng bài và kiểm duyệt bài đăng

## A. Các đối tượng tham gia, từ trái sang phải

1. **Actor: Chủ trọ**
2. **Boundary: `FormDangBai`** - nhãn giao diện mô tả; class React thực tế là `CreatePostPage`.
3. **Control: `PostController`**
4. **Control: `PostBLL`**
5. **Entity: `ApplicationDbContext`**
6. **Database: SQL Server / `RoomRentalDB`**
7. **Actor: Quản trị viên**
8. **Boundary: `FormQuanLyBaiDang`** - nhãn giao diện mô tả; màn quản trị thực tế là `AdminManagementPage`/`AdminManagementContent`.
9. **Control: `AdminController`**
10. **Control: `AdminBLL`**

Khi vẽ, chỉ vẽ một lifeline `ApplicationDbContext` và một lifeline `SQL Server`; hai phần tạo bài và kiểm duyệt dùng chung hai lifeline này. `FormDangBai` và `FormQuanLyBaiDang` là nhãn UI, không phải tên class backend.

## B. Bảng các mũi tên tuần tự

### Phần 1 - Chủ trọ tạo bài đăng

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 1 | Chủ trọ | `FormDangBai` | Nhập dữ liệu phòng, ảnh, tiện nghi và gửi đăng bài | Call |
| 1.1 | `FormDangBai` | `PostController` | `POST /api/bai-dang` - `CreatePost(CreatePostDto)` kèm JWT | Call |
| 1.2 | `PostController` | `PostBLL` | `CreatePostAsync(accountId, createDto)` | Call |
| 1.3 | `PostBLL` | `ApplicationDbContext` | Lấy/tạo `LandlordProfile`; kiểm tra tiêu đề và giá | Call |
| 1.4 | `ApplicationDbContext` | SQL Server | `SELECT TaiKhoan, ChuTro`; có thể `INSERT ChuTro` | Call |
| 1.5 | SQL Server | `ApplicationDbContext` | Trả hồ sơ chủ trọ | Return |
| 1.6 | `PostBLL` | `ApplicationDbContext` | `Add(Room)` với `Status = Available`; `SaveChangesAsync()` | Call |
| 1.7 | `ApplicationDbContext` | SQL Server | `INSERT PhongTro` | Call |
| 1.8 | SQL Server | `ApplicationDbContext` | Trả `Room.Id` | Return |
| 1.9 | `PostBLL` | `ApplicationDbContext` | Thêm `PostAmenity`, `PostImage` nếu có; thêm `Post(Status = Pending)` | Call |
| 1.10 | `ApplicationDbContext` | SQL Server | `INSERT PhongTro_TienNghi`, `HinhAnhPhong`, `TinDang` | Call |
| 1.11 | SQL Server | `ApplicationDbContext` | Trả `Post.Id` | Return |
| 1.12 | `PostBLL` | `PostController` | Trả `PostDto` có `Status = Pending` | Return |
| 1.13 | `PostController` | `FormDangBai` | `200 OK`, `ApiResponse<PostDto>`: tin đang chờ duyệt | Return |
| 1.14 | `FormDangBai` | Chủ trọ | Hiển thị kết quả đăng tin | Return |

### Phần 2 - Admin kiểm duyệt bài đăng

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 2 | Quản trị viên | `FormQuanLyBaiDang` | Mở danh sách bài đăng chờ duyệt | Call |
| 2.1 | `FormQuanLyBaiDang` | `AdminController` | `GET /api/quan-tri/bai-dang?status=Pending` - `GetPosts` | Call |
| 2.2 | `AdminController` | `AdminBLL` | `GetAllPostsAsync(PostStatus.Pending, keyword)` | Call |
| 2.3 | `AdminBLL` | `ApplicationDbContext` | Truy vấn danh sách Post | Call |
| 2.4 | `ApplicationDbContext` | SQL Server | `SELECT TinDang` và dữ liệu Room/Landlord | Call |
| 2.5 | SQL Server | `ApplicationDbContext` | Trả danh sách Post Pending | Return |
| 2.6 | `AdminBLL` | `AdminController` | Trả `List<PostListDto>` | Return |
| 2.7 | `AdminController` | `FormQuanLyBaiDang` | `200 OK` | Return |
| 3 | Quản trị viên | `FormQuanLyBaiDang` | Chọn duyệt hoặc từ chối một bài đăng | Call |
| 3.1A | `FormQuanLyBaiDang` | `AdminController` | Duyệt: `PUT /api/quan-tri/bai-dang/{id}/duyet` - `ApprovePost` | Call |
| 3.2A | `AdminController` | `AdminBLL` | `ApprovePostAsync(id)` | Call |
| 3.3A | `AdminBLL` | `ApplicationDbContext` | Tìm Post theo `id`, kiểm tra `Status == Pending` | Call |
| 3.4A | `ApplicationDbContext` | SQL Server | `SELECT TinDang` | Call |
| 3.5A | `AdminBLL` | `ApplicationDbContext` | `Status=Approved`, `ApprovedAt=Now`, `PostedAt=Now`; lưu | Call |
| 3.6A | `ApplicationDbContext` | SQL Server | `UPDATE TinDang` | Call |
| 3.7A | `AdminBLL` | `AdminController` | Trả `PostDto` đã duyệt | Return |
| 3.8A | `AdminController` | `FormQuanLyBaiDang` | `200 OK` | Return |
| 3.1B | `FormQuanLyBaiDang` | `AdminController` | Từ chối: `PUT /api/quan-tri/bai-dang/{id}/tu-choi` - `RejectPost` với `RejectPostDto` | Call |
| 3.2B | `AdminController` | `AdminBLL` | `RejectPostAsync(id, dto.GetReason())` | Call |
| 3.3B | `AdminBLL` | `ApplicationDbContext` | Tìm Post, kiểm tra Pending và lý do | Call |
| 3.4B | `AdminBLL` | `ApplicationDbContext` | `Status=Rejected`, `RejectionReason=reason`; lưu | Call |
| 3.5B | `ApplicationDbContext` | SQL Server | `UPDATE TinDang` | Call |
| 3.6B | `AdminBLL` | `AdminController` | Trả `PostDto` bị từ chối | Return |
| 3.7B | `AdminController` | `FormQuanLyBaiDang` | `200 OK` | Return |

## C. Các khung ALT cần vẽ

### ALT 1 - Authorization khi tạo bài

**Vị trí:** bắt đầu ngay sau mũi tên `1.1`, trước `1.2`.

- **Nhánh thành công:** JWT hợp lệ và role là `Landlord` hoặc `Admin`; đi tiếp mũi tên `1.2` đến `1.14`.
- **Nhánh thất bại:** JWT thiếu/sai hoặc role không hợp lệ; middleware authorization trả `401`/`403` về `FormDangBai`. Không gọi `PostBLL`.

### ALT 2 - Dữ liệu tạo bài không hợp lệ

**Vị trí:** nằm trong nhánh thành công của ALT 1, sau `1.5`.

- **Nhánh thành công:** `title` không rỗng và `price > 0`; vẽ `1.6` đến `1.14`.
- **Nhánh thất bại:** `PostBLL.CreatePostAsync` ném lỗi khi title rỗng hoặc giá không dương; `PostController` trả `400 BadRequest` với `ApiResponse` lỗi.

### ALT 3 - Admin duyệt hoặc từ chối

**Vị trí:** bắt đầu sau mũi tên `3`.

- **Nhánh duyệt:** mũi tên `3.1A` đến `3.8A`.
- **Nhánh từ chối:** mũi tên `3.1B` đến `3.7B`.

### ALT 4 - Trạng thái kiểm duyệt không hợp lệ

**Vị trí:** đặt bên trong từng nhánh của ALT 3, sau `3.4A` hoặc sau bước đọc Post ở nhánh B.

- **Nhánh thành công:** Post có `Status = Pending`; tiếp tục `UPDATE`.
- **Nhánh thất bại:** `AdminBLL` ném `BusinessRuleException.Conflict`; Controller trả `409`.
- Với nhánh từ chối, thêm điều kiện lỗi khi lý do rỗng; `RejectPostAsync` trả lỗi `400`.

## D. Bố cục vẽ

- Vẽ **10 lifeline** theo đúng thứ tự phần A. Nếu khổ giấy hẹp, đặt `Quản trị viên`, `FormQuanLyBaiDang`, `AdminController`, `AdminBLL` ở nửa phải và dùng nhãn `== Kiểm duyệt ==` để chia hai giai đoạn.
- Activation bar: `PostController` từ `1.1` đến `1.13`; `PostBLL` từ `1.2` đến `1.12`; `AdminController` từ `2.1` đến `2.7` và từ `3.1A/3.1B` đến phản hồi; `AdminBLL` trong các call tương ứng.
- Mũi tên `Call` là mũi tên liền; mũi tên `Return` là mũi tên nét đứt quay về đối tượng gọi.
- Mũi tên `3.1A` và `3.1B` nằm trong cùng ALT, không vẽ nối tiếp cả hai nhánh.
- Không vẽ bước kiểm tra "ownership Room có sẵn" vì `PostBLL.CreatePostAsync` tạo `Room` mới, không nhận `RoomId` từ `CreatePostDto` để kiểm tra quyền sở hữu phòng cũ.

## E. Đối chiếu code

| Bước | Controller/Service | Method thực tế | File source |
|---|---|---|---|
| 1.1 | `PostController` | `CreatePost(CreatePostDto)` | `backend/RoomRental.BackEnd/Controllers/PostController.cs` |
| 1.2 - 1.12 | `PostBLL` | `CreatePostAsync(int accountId, CreatePostDto createDto)` | `backend/RoomRental.BackEnd/BLL/PostBLL.cs` |
| 1.1 frontend | `postService` | `createPost` | `frontend/src/services/postService.ts` |
| 1 frontend UI | `CreatePostPage` | Hàm submit dùng `postService.createPost` | `frontend/src/pages/landlord/CreatePostPage.tsx` |
| 2.1 | `AdminController` | `GetPosts(PostStatus? status, string? keyword)` | `backend/RoomRental.BackEnd/Controllers/AdminController.cs` |
| 2.2 | `AdminBLL` | `GetAllPostsAsync(PostStatus? status, string? keyword)` | `backend/RoomRental.BackEnd/BLL/AdminBLL.cs` |
| 3.1A - 3.8A | `AdminController` / `AdminBLL` | `ApprovePost`, `ApprovePostAsync(int postId)` | `Controllers/AdminController.cs`, `BLL/AdminBLL.cs` |
| 3.1B - 3.7B | `AdminController` / `AdminBLL` | `RejectPost`, `RejectPostAsync(int postId, string reason)` | `Controllers/AdminController.cs`, `BLL/AdminBLL.cs` |
| 3 frontend | `adminService` | `getPosts`, `approvePost`, `rejectPost` | `frontend/src/services/adminService.ts` |
