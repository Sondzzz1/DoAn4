# Phác thảo tuần tự - Gửi và xử lý yêu cầu thuê phòng

## A. Các đối tượng tham gia, từ trái sang phải

1. **Actor: Người thuê**
2. **Boundary: `FormYeuCauThue`** - nhãn mô tả giao diện; component thực tế là `RentalRequestModal`.
3. **Control: `RentalRequestController`**
4. **Control: `RentalRequestBLL`**
5. **Entity: `ApplicationDbContext`**
6. **Database: SQL Server / `RoomRentalDB`**
7. **Actor: Chủ trọ**
8. **Boundary: `FormQuanLyYeuCauThue`** - nhãn mô tả giao diện; các thao tác được gọi trong `LandlordContractsPage` qua `landlordService`.

## B. Bảng các mũi tên tuần tự

### Phần 1 - Tenant gửi yêu cầu thuê

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 1 | Người thuê | `FormYeuCauThue` | Nhập ghi chú và chọn gửi yêu cầu thuê | Call |
| 1.1 | `FormYeuCauThue` | `RentalRequestController` | `POST /api/yeu-cau-thue` - `Tao(TaoYeuCauThueDto)` + JWT | Call |
| 1.2 | `RentalRequestController` | `RentalRequestBLL` | `TaoAsync(accountId, dto)` | Call |
| 1.3 | `RentalRequestBLL` | `ApplicationDbContext` | Tìm User theo `nguoiThueId` | Call |
| 1.4 | `ApplicationDbContext` | SQL Server | `SELECT TaiKhoan` | Call |
| 1.5 | SQL Server | `ApplicationDbContext` | Trả User và RoleId | Return |
| 1.6 | `RentalRequestBLL` | `ApplicationDbContext` | Tìm Post, Landlord, Room và RentalRequest Pending trùng | Call |
| 1.7 | `ApplicationDbContext` | SQL Server | `SELECT TinDang, ChuTro, PhongTro, YeuCauThuePhong` | Call |
| 1.8 | SQL Server | `ApplicationDbContext` | Trả dữ liệu kiểm tra | Return |
| 1.9 | `RentalRequestBLL` | `ApplicationDbContext` | `Add(RentalRequest)` với `Status=Pending`; `SaveChangesAsync()` | Call |
| 1.10 | `ApplicationDbContext` | SQL Server | `INSERT YeuCauThuePhong` | Call |
| 1.11 | SQL Server | `ApplicationDbContext` | Trả Request.Id | Return |
| 1.12 | `RentalRequestBLL` | `RentalRequestController` | Trả `YeuCauThueDto` | Return |
| 1.13 | `RentalRequestController` | `FormYeuCauThue` | `200 OK` - yêu cầu Pending | Return |
| 1.14 | `FormYeuCauThue` | Người thuê | Hiển thị gửi thành công | Return |

### Phần 2 - Chủ trọ xử lý yêu cầu

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 2 | Chủ trọ | `FormQuanLyYeuCauThue` | Mở danh sách yêu cầu thuê | Call |
| 2.1 | `FormQuanLyYeuCauThue` | `RentalRequestController` | `GET /api/yeu-cau-thue/chu-tro` - `CuaChuTro` | Call |
| 2.2 | `RentalRequestController` | `RentalRequestBLL` | `LayCuaToiAsync(accountId, true)` | Call |
| 2.3 | `RentalRequestBLL` | `ApplicationDbContext` | Query theo `LandlordAccountId` | Call |
| 2.4 | `ApplicationDbContext` | SQL Server | `SELECT YeuCauThuePhong` cùng Post/Room | Call |
| 2.5 | SQL Server | `ApplicationDbContext` | Trả danh sách | Return |
| 2.6 | `RentalRequestBLL` | `RentalRequestController` | Trả `List<YeuCauThueDto>` | Return |
| 2.7 | `RentalRequestController` | `FormQuanLyYeuCauThue` | `200 OK` | Return |
| 3 | Chủ trọ | `FormQuanLyYeuCauThue` | Chọn chấp nhận hoặc từ chối | Call |
| 3.1A | `FormQuanLyYeuCauThue` | `RentalRequestController` | Chấp nhận: `PUT /api/yeu-cau-thue/{id}/trang-thai` với `TrangThai=Approved` | Call |
| 3.2A | `RentalRequestController` | `RentalRequestBLL` | `CapNhatTrangThaiAsync(accountId, id, Approved, ghiChu)` | Call |
| 3.3A | `RentalRequestBLL` | `ApplicationDbContext` | Tìm request, kiểm tra owner và `Status=Pending` | Call |
| 3.4A | `ApplicationDbContext` | SQL Server | `SELECT`, sau đó `UPDATE TrangThai=Approved` | Call |
| 3.5A | `RentalRequestBLL` | `RentalRequestController` | Trả `YeuCauThueDto` | Return |
| 3.6A | `RentalRequestController` | `FormQuanLyYeuCauThue` | `200 OK` | Return |
| 3.1B | `FormQuanLyYeuCauThue` | `RentalRequestController` | Từ chối: cùng route, `TrangThai=Rejected`, có thể kèm `GhiChu` | Call |
| 3.2B | `RentalRequestController` | `RentalRequestBLL` | `CapNhatTrangThaiAsync(accountId, id, Rejected, ghiChu)` | Call |
| 3.3B | `RentalRequestBLL` | `ApplicationDbContext` | Kiểm tra owner/Pending; cập nhật Rejected và Note nếu có | Call |
| 3.4B | `ApplicationDbContext` | SQL Server | `SELECT` và `UPDATE YeuCauThuePhong` | Call |
| 3.5B | `RentalRequestBLL` | `RentalRequestController` | Trả DTO | Return |
| 3.6B | `RentalRequestController` | `FormQuanLyYeuCauThue` | `200 OK` | Return |

### Phần 3 - Tenant hủy yêu cầu (tùy chọn trong cùng sơ đồ)

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 4 | Người thuê | `FormYeuCauThue` | Chọn hủy yêu cầu Pending của mình | Call |
| 4.1 | `FormYeuCauThue` | `RentalRequestController` | `PUT /api/yeu-cau-thue/{id}/huy` - `Huy` | Call |
| 4.2 | `RentalRequestController` | `RentalRequestBLL` | `HuyAsync(accountId, id)` | Call |
| 4.3 | `RentalRequestBLL` | `ApplicationDbContext` | Kiểm tra `TenantAccountId` và `Status=Pending` | Call |
| 4.4 | `ApplicationDbContext` | SQL Server | `SELECT`; `UPDATE TrangThai=Cancelled` | Call |
| 4.5 | `RentalRequestBLL` | `RentalRequestController` | Trả DTO | Return |
| 4.6 | `RentalRequestController` | `FormYeuCauThue` | `200 OK` hoặc lỗi `403/409` | Return |

## C. Các khung ALT cần vẽ

### ALT 1 - Điều kiện gửi RentalRequest

**Vị trí:** sau `1.8`, trước `1.9`.

- **Thành công:** User có `RoleId == 1`, Post `Approved`, Room `Available`, tenant khác tài khoản Landlord và chưa có request Pending trùng; vẽ `1.9` đến `1.14`.
- **Thất bại:** user không phải Tenant trả `403`; Post/Room không khả dụng hoặc request Pending trùng trả `409`; tenant là chính Landlord của post trả `403`.

### ALT 2 - Chủ trọ chấp nhận hoặc từ chối

**Vị trí:** sau `3`.

- **Nhánh A:** chấp nhận bằng `3.1A` đến `3.6A`.
- **Nhánh B:** từ chối bằng `3.1B` đến `3.6B`.
- **ALT lồng:** sau khi đọc request ở `3.3A/3.3B`, nếu tài khoản không trùng `LandlordAccountId` trả `403`; nếu status không Pending trả `409`.

### ALT 3 - Tenant hủy yêu cầu

**Vị trí:** bao quanh bước `4` đến `4.6`; có thể đặt dạng `opt` nếu muốn sơ đồ ngắn.

- **Thành công:** request thuộc Tenant và đang Pending, đổi thành `Cancelled`.
- **Thất bại:** không sở hữu trả `403`; không còn Pending trả `409`.

## D. Bố cục vẽ

- Có **8 lifeline**, theo thứ tự phần A; dùng ba frame ngang: `Gửi yêu cầu`, `Chủ trọ xử lý`, `Tenant hủy`.
- Activation bar: `RentalRequestController` trong từng API call; `RentalRequestBLL` từ sau khi Controller gọi đến khi trả DTO; `ApplicationDbContext` chỉ trong từng truy vấn/lưu.
- ALT 1 bắt đầu sau mũi tên DB trả ở `1.8`. ALT 2 bắt đầu sau mũi tên `3`. ALT 3 có thể là `opt` dưới cùng.
- Không đặt notification lifeline: `RentalRequestBLL` hiện không gọi `INotificationService`.

## E. Đối chiếu code

| Bước | Controller/Service | Method thực tế | File source |
|---|---|---|---|
| 1.1 frontend | `rentalService` | `createRentalRequest` | `frontend/src/services/rentalService.ts` |
| 1 frontend UI | `RentalRequestModal` | Submit gọi `rentalService.createRentalRequest` | `frontend/src/components/room/RentalRequestModal.tsx` |
| 1.1 | `RentalRequestController` | `Tao(TaoYeuCauThueDto)` | `backend/RoomRental.BackEnd/Controllers/RentalManagementControllers.cs` |
| 1.2 - 1.12 | `RentalRequestBLL` | `TaoAsync` | `backend/RoomRental.BackEnd/BLL/RentalFeaturesBLL.cs` |
| 2.1 - 2.6 | `RentalRequestController` / `RentalRequestBLL` | `CuaChuTro`, `LayCuaToiAsync` | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| 3.1 - 3.6 | `RentalRequestController` / `RentalRequestBLL` | `TrangThai`, `CapNhatTrangThaiAsync` | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| 4.1 - 4.6 | `RentalRequestController` / `RentalRequestBLL` | `Huy`, `HuyAsync` | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| 2/3 frontend | `landlordService` | `getRentalRequests`, `approveRentalRequest`, `rejectRentalRequest` | `frontend/src/services/landlordService.ts` |
