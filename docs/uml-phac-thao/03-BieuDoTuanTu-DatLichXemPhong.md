# Phác thảo tuần tự - Đặt lịch xem phòng

## A. Các đối tượng tham gia, từ trái sang phải

1. **Actor: Người thuê**
2. **Boundary: `FormDatLichXemPhong`** - nhãn mô tả giao diện; component thực tế là `BookingModal`.
3. **Control: `ViewingAppointmentController`**
4. **Control: `ViewingAppointmentBLL`**
5. **Entity: `ApplicationDbContext`**
6. **Database: SQL Server / `RoomRentalDB`**
7. **Actor: Chủ trọ**
8. **Boundary: `FormQuanLyLichHen`** - nhãn mô tả giao diện; component thực tế là `LandlordAppointmentsPage`.

`ViewingAppointmentBLL` không gọi `INotificationService`, nên không đặt `NotificationBLL` hay `NotificationHub` vào biểu đồ hiện trạng.

## B. Bảng các mũi tên tuần tự

### Phần 1 - Tenant đặt lịch

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 1 | Người thuê | `FormDatLichXemPhong` | Chọn ngày, giờ và ghi chú đặt lịch | Call |
| 1.1 | `FormDatLichXemPhong` | `ViewingAppointmentController` | `POST /api/lich-hen-xem-phong` - `CreateAppointment(CreateAppointmentDto)` + JWT | Call |
| 1.2 | `ViewingAppointmentController` | `ViewingAppointmentBLL` | `CreateAppointmentAsync(accountId, createDto)` | Call |
| 1.3 | `ViewingAppointmentBLL` | `ApplicationDbContext` | `GetOrCreateTenantProfileAsync(accountId)` | Call |
| 1.4 | `ApplicationDbContext` | SQL Server | `SELECT TaiKhoan, NguoiDung`; có thể `INSERT NguoiDung` | Call |
| 1.5 | SQL Server | `ApplicationDbContext` | Trả TenantProfile | Return |
| 1.6 | `ViewingAppointmentBLL` | `ApplicationDbContext` | Lấy Post + Room + Landlord và lịch hẹn hiện có | Call |
| 1.7 | `ApplicationDbContext` | SQL Server | `SELECT TinDang, PhongTro, ChuTro, LichHenXemPhong` | Call |
| 1.8 | SQL Server | `ApplicationDbContext` | Trả dữ liệu kiểm tra | Return |
| 1.9 | `ViewingAppointmentBLL` | `ApplicationDbContext` | `Add(ViewingAppointment)` với `Status=Pending`; `SaveChangesAsync()` | Call |
| 1.10 | `ApplicationDbContext` | SQL Server | `INSERT LichHenXemPhong` | Call |
| 1.11 | SQL Server | `ApplicationDbContext` | Trả `Appointment.Id` | Return |
| 1.12 | `ViewingAppointmentBLL` | `ViewingAppointmentController` | Trả `AppointmentDto(Status=Pending)` | Return |
| 1.13 | `ViewingAppointmentController` | `FormDatLichXemPhong` | `200 OK` - chờ chủ trọ xác nhận | Return |
| 1.14 | `FormDatLichXemPhong` | Người thuê | Hiển thị kết quả | Return |

### Phần 2 - Chủ trọ xác nhận hoặc từ chối

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 2 | Chủ trọ | `FormQuanLyLichHen` | Mở danh sách lịch hẹn | Call |
| 2.1 | `FormQuanLyLichHen` | `ViewingAppointmentController` | `GET /api/lich-hen-xem-phong/chu-tro` - `GetLandlordAppointments` | Call |
| 2.2 | `ViewingAppointmentController` | `ViewingAppointmentBLL` | `GetLandlordAppointmentsAsync(accountId, status)` | Call |
| 2.3 | `ViewingAppointmentBLL` | `ApplicationDbContext` | Lấy `LandlordProfile` và lịch theo `LandlordId` | Call |
| 2.4 | `ApplicationDbContext` | SQL Server | `SELECT ChuTro, LichHenXemPhong` | Call |
| 2.5 | SQL Server | `ApplicationDbContext` | Trả danh sách lịch | Return |
| 2.6 | `ViewingAppointmentBLL` | `ViewingAppointmentController` | Trả `List<AppointmentDto>` | Return |
| 2.7 | `ViewingAppointmentController` | `FormQuanLyLichHen` | `200 OK` | Return |
| 3 | Chủ trọ | `FormQuanLyLichHen` | Chọn xác nhận hoặc từ chối lịch | Call |
| 3.1A | `FormQuanLyLichHen` | `ViewingAppointmentController` | Xác nhận: `PUT /api/lich-hen-xem-phong/{id}/xac-nhan` | Call |
| 3.2A | `ViewingAppointmentController` | `ViewingAppointmentBLL` | `ConfirmAppointmentAsync(accountId, id)` | Call |
| 3.3A | `ViewingAppointmentBLL` | `ApplicationDbContext` | Tìm lịch, kiểm tra `LandlordId` và `Status=Pending` | Call |
| 3.4A | `ApplicationDbContext` | SQL Server | `SELECT`; sau đó `UPDATE Status=Confirmed` | Call |
| 3.5A | `ViewingAppointmentBLL` | `ViewingAppointmentController` | Trả `AppointmentDto` | Return |
| 3.6A | `ViewingAppointmentController` | `FormQuanLyLichHen` | `200 OK` | Return |
| 3.1B | `FormQuanLyLichHen` | `ViewingAppointmentController` | Từ chối: `PUT /api/lich-hen-xem-phong/{id}/tu-choi` kèm lý do | Call |
| 3.2B | `ViewingAppointmentController` | `ViewingAppointmentBLL` | `RejectAppointmentAsync(accountId, id, reason)` | Call |
| 3.3B | `ViewingAppointmentBLL` | `ApplicationDbContext` | Kiểm tra ownership/Pending; cập nhật `Status=Rejected`, `LandlordResponse` | Call |
| 3.4B | `ApplicationDbContext` | SQL Server | `SELECT` và `UPDATE LichHenXemPhong` | Call |
| 3.5B | `ViewingAppointmentBLL` | `ViewingAppointmentController` | Trả `AppointmentDto` | Return |
| 3.6B | `ViewingAppointmentController` | `FormQuanLyLichHen` | `200 OK` | Return |

## C. Các khung ALT cần vẽ

### ALT 1 - Điều kiện tạo lịch

**Vị trí:** sau `1.8`, trước `1.9`.

- **Nhánh thành công:** user là Tenant, Post `Approved`, Room `Available`, không tự đặt lịch phòng của mình, thời gian ở tương lai, landlordId trong request (nếu có) khớp và không conflict; vẽ `1.9` đến `1.14`.
- **Nhánh thất bại 1:** user không phải Tenant hoặc bị khóa; trả lỗi `403`/`400`.
- **Nhánh thất bại 2:** Post không `Approved` hoặc Room không `Available`; trả `409`.
- **Nhánh thất bại 3:** tự đặt lịch, thời gian không tương lai, landlordId không khớp; trả `403`/`400`.
- **Nhánh thất bại 4:** đã có lịch Pending/Confirmed cùng Post hoặc trùng đúng `ScheduledAt` của Tenant/Landlord; trả `409`.

### ALT 2 - Chủ trọ phản hồi lịch

**Vị trí:** sau mũi tên `3`.

- **Nhánh xác nhận:** `3.1A` đến `3.6A`; chỉ được cập nhật `Pending -> Confirmed`.
- **Nhánh từ chối:** `3.1B` đến `3.6B`; chỉ được cập nhật `Pending -> Rejected` và gán `LandlordResponse`.
- **Nhánh lỗi bên trong:** nếu lịch không thuộc chủ trọ hoặc không còn `Pending`, BLL trả `Forbidden`/`Conflict`, Controller trả `403`/`409`.

## D. Bố cục vẽ

- Có **8 lifeline**. Đặt 6 lifeline Tenant đến Database ở nửa trái; Landlord và Form quản lý ở nửa phải.
- Nếu giao diện quá rộng, dùng hai frame ngang: `== Tenant đặt lịch ==` chứa bước 1, `== Chủ trọ phản hồi ==` chứa bước 2 và 3.
- Activation bar: Controller từ `1.1` đến `1.13`; BLL từ `1.2` đến `1.12`; phần landlord tương tự từ `2.1` đến `2.7` và `3.1` đến phản hồi.
- ALT 1 bao quanh các quyết định sau khi dữ liệu đã trả từ DB. ALT 2 bao quanh hai nhánh `3.1A` và `3.1B`.
- Không vẽ Notification trong biểu đồ vì chưa xác nhận được lời gọi notification trong `ViewingAppointmentBLL`.

## E. Đối chiếu code

| Bước | Controller/Service | Method thực tế | File source |
|---|---|---|---|
| 1.1 frontend | `appointmentService` | `createAppointment` | `frontend/src/services/appointmentService.ts` |
| 1 frontend UI | `BookingModal` | Submit gọi `appointmentService.createAppointment` | `frontend/src/components/room/BookingModal.tsx` |
| 1.1 | `ViewingAppointmentController` | `CreateAppointment(CreateAppointmentDto)` | `backend/RoomRental.BackEnd/Controllers/ViewingAppointmentController.cs` |
| 1.2 - 1.12 | `ViewingAppointmentBLL` | `CreateAppointmentAsync` | `backend/RoomRental.BackEnd/BLL/ViewingAppointmentBLL.cs` |
| 2.1 | `ViewingAppointmentController` | `GetLandlordAppointments` | `backend/RoomRental.BackEnd/Controllers/ViewingAppointmentController.cs` |
| 3.1A - 3.6A | `ViewingAppointmentController` / `ViewingAppointmentBLL` | `ConfirmAppointment`, `ConfirmAppointmentAsync` | `Controllers/ViewingAppointmentController.cs`, `BLL/ViewingAppointmentBLL.cs` |
| 3.1B - 3.6B | `ViewingAppointmentController` / `ViewingAppointmentBLL` | `RejectAppointment`, `RejectAppointmentAsync` | `Controllers/ViewingAppointmentController.cs`, `BLL/ViewingAppointmentBLL.cs` |
| 2/3 frontend | `landlordService` | `getAppointments`, `confirmAppointment`, `rejectAppointment` | `frontend/src/services/landlordService.ts` |
