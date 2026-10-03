# Phác thảo tuần tự - Lập và thanh toán hóa đơn hàng tháng

Nên vẽ hai frame liên tiếp: Frame A cho việc lập hóa đơn của chủ trọ; Frame B cho Tenant xem và thanh toán VNPay. Đây là cùng một luồng nghiệp vụ tài chính.

## A. Các đối tượng tham gia, từ trái sang phải

### Frame A - Chủ trọ lập hóa đơn

1. **Actor: Chủ trọ**
2. **Boundary: `FormLapHoaDon`** - nhãn mô tả giao diện; thao tác thực tế trong `LandlordContractsPage`.
3. **Control: `MonthlyBillController`**
4. **Control: `MonthlyBillBLL`**
5. **Entity: `ApplicationDbContext`**
6. **Database: SQL Server / `RoomRentalDB`**
7. **Control: `NotificationBLL`** - được gọi best-effort sau khi bill đã lưu.

### Frame B - Tenant thanh toán

1. **Actor: Người thuê**
2. **Boundary: `FormHoaDonCuaToi`** - nhãn mô tả; UI thực tế là `TenantRentalsPage`.
3. **Control: `MonthlyBillController`**
4. **Control: `MonthlyBillBLL`**
5. **Control: `PaymentController`**
6. **Control: `PaymentBLL`**
7. **Entity: `ApplicationDbContext`**
8. **Database: SQL Server / `RoomRentalDB`**
9. **Actor: VNPay**
10. **Control: `NotificationBLL`**

## B. Bảng các mũi tên tuần tự

### Frame A - Chủ trọ lập hóa đơn

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 1 | Chủ trọ | `FormLapHoaDon` | Chọn contract và nhập tháng/năm, điện, nước, phụ phí, hạn thanh toán | Call |
| 1.1 | `FormLapHoaDon` | `MonthlyBillController` | `POST /api/hoa-don` - `Tao(TaoHoaDonDto)` + JWT Landlord | Call |
| 1.2 | `MonthlyBillController` | `MonthlyBillBLL` | `TaoAsync(accountId, dto)` | Call |
| 1.3 | `MonthlyBillBLL` | `MonthlyBillBLL` | `ValidatePeriod`; `ValidateReadings`; kiểm tra `ChiPhiKhac >= 0` | Call nội bộ |
| 1.4 | `MonthlyBillBLL` | `ApplicationDbContext` | Lấy `RentalContract` + Post + Room, kiểm tra bill cùng Contract/Month/Year | Call |
| 1.5 | `ApplicationDbContext` | SQL Server | `SELECT HopDongThue, TinDang, PhongTro, HoaDonHangThang` | Call |
| 1.6 | SQL Server | `ApplicationDbContext` | Trả contract và các hóa đơn hiện có | Return |
| 1.7 | `MonthlyBillBLL` | `MonthlyBillBLL` | Dùng snapshot từ contract và `CalculateTotal(bill)` | Call nội bộ |
| 1.8 | `MonthlyBillBLL` | `ApplicationDbContext` | `Add(MonthlyBill)` với `Status=Unpaid`; `SaveChangesAsync()` | Call |
| 1.9 | `ApplicationDbContext` | SQL Server | `INSERT HoaDonHangThang` | Call |
| 1.10 | SQL Server | `ApplicationDbContext` | Trả Bill.Id | Return |
| 1.11 | `MonthlyBillBLL` | `NotificationBLL` | `CreateNotificationAsync(tenantId, ...)` | Call |
| 1.12 | `NotificationBLL` | SQL Server | `INSERT ThongBao`; có thể phát `ReceiveNotification` qua SignalR | Call |
| 1.13 | `MonthlyBillBLL` | `MonthlyBillController` | Trả `HoaDonDto` | Return |
| 1.14 | `MonthlyBillController` | `FormLapHoaDon` | `200 OK` | Return |
| 1.15 | `FormLapHoaDon` | Chủ trọ | Hiển thị hóa đơn vừa lập | Return |

### Frame B1 - Tenant xem hóa đơn

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 2 | Người thuê | `FormHoaDonCuaToi` | Mở danh sách hóa đơn | Call |
| 2.1 | `FormHoaDonCuaToi` | `MonthlyBillController` | `GET /api/hoa-don/cua-toi` - `LayCuaToi` | Call |
| 2.2 | `MonthlyBillController` | `MonthlyBillBLL` | `LayDanhSachCuaToiAsync(accountId, false)` | Call |
| 2.3 | `MonthlyBillBLL` | `ApplicationDbContext` | `ReconcileOverdueAsync()` và query contract/bill của tenant | Call |
| 2.4 | `ApplicationDbContext` | SQL Server | `UPDATE` bill Unpaid quá hạn nếu có; `SELECT HopDongThue, HoaDonHangThang` | Call |
| 2.5 | SQL Server | `ApplicationDbContext` | Trả danh sách bill | Return |
| 2.6 | `MonthlyBillBLL` | `MonthlyBillController` | Trả `List<HoaDonDto>` | Return |
| 2.7 | `MonthlyBillController` | `FormHoaDonCuaToi` | `200 OK` | Return |

### Frame B2 - Tạo giao dịch và nhận callback VNPay

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 3 | Người thuê | `FormHoaDonCuaToi` | Chọn thanh toán một MonthlyBill | Call |
| 3.1 | `FormHoaDonCuaToi` | `PaymentController` | `POST /api/thanh-toan/vnpay-tao-url` với `{ MonthlyBillId }` | Call |
| 3.2 | `PaymentController` | `PaymentBLL` | `CreatePaymentUrlAsync(userId, dto, clientIp)` | Call |
| 3.3 | `PaymentBLL` | `ApplicationDbContext` | Bắt đầu transaction Serializable; lấy Bill + Contract + PaymentTransaction | Call |
| 3.4 | `ApplicationDbContext` | SQL Server | `SELECT HoaDonHangThang, HopDongThue, GiaoDichThanhToan` | Call |
| 3.5 | SQL Server | `ApplicationDbContext` | Trả dữ liệu bill và giao dịch | Return |
| 3.6 | `PaymentBLL` | `ApplicationDbContext` | `Bill.Status=PendingPayment`; thêm `PaymentTransaction(Status=Pending)`; lưu/commit | Call |
| 3.7 | `ApplicationDbContext` | SQL Server | `UPDATE HoaDonHangThang`; `INSERT GiaoDichThanhToan` | Call |
| 3.8 | `PaymentBLL` | `PaymentController` | Trả `PaymentResponseDto(PaymentUrl, OrderId, Amount)` | Return |
| 3.9 | `PaymentController` | `FormHoaDonCuaToi` | `200 OK` | Return |
| 3.10 | `FormHoaDonCuaToi` | VNPay | Điều hướng tới `PaymentUrl` | Call |
| 4 | VNPay | `PaymentController` | `GET /api/thanh-toan/vnpay-return` hoặc `GET /api/thanh-toan/vnpay-ipn` | Call |
| 4.1 | `PaymentController` | `PaymentBLL` | `ProcessPaymentReturnAsync(Request.Query)` | Call |
| 4.2 | `PaymentBLL` | `PaymentBLL` | Validate signature, order, amount, response code, transaction code | Call nội bộ |
| 4.3 | `PaymentBLL` | `ApplicationDbContext` | Lấy PaymentTransaction gồm MonthlyBill và RentalContract | Call |
| 4.4 | `ApplicationDbContext` | SQL Server | `SELECT GiaoDichThanhToan, HoaDonHangThang, HopDongThue` | Call |
| 4.5 | SQL Server | `ApplicationDbContext` | Trả transaction/bill/contract | Return |
| 4.6 | `PaymentBLL` | `ApplicationDbContext` | Callback success: transaction Serializable, `PaymentTransaction=Succeeded`, `MonthlyBill=Paid`, `PaidAt=Now`, `PaymentMethod=VNPay`; lưu/commit | Call |
| 4.7 | `ApplicationDbContext` | SQL Server | `UPDATE GiaoDichThanhToan`, `UPDATE HoaDonHangThang` | Call |
| 4.8 | `PaymentBLL` | `NotificationBLL` | `CreateNotificationAsync` cho Tenant và Landlord | Call |
| 4.9 | `NotificationBLL` | SQL Server | `INSERT ThongBao`; SignalR nếu có kết nối | Call |
| 4.10 | `PaymentBLL` | `PaymentController` | Trả `PaymentResultDto` | Return |
| 4.11 | `PaymentController` | `FormHoaDonCuaToi` | `vnpay-return`: redirect `/payment/result?...`; `vnpay-ipn`: trả `RspCode` | Return |

## C. Các khung ALT cần vẽ

### ALT 1 - Kiểm tra khi lập MonthlyBill

**Vị trí:** sau `1.6`, trước `1.7`.

- **Nhánh thành công:** tháng 1-12, năm 2000-2100, chỉ số mới không nhỏ hơn cũ, phụ phí không âm; contract thuộc Landlord, `Status=Active`, chưa có bill cho Contract/Month/Year. Vẽ `1.7` đến `1.15`.
- **Nhánh lỗi dữ liệu:** period/readings/phụ phí sai, trả `400`.
- **Nhánh lỗi quyền/trạng thái:** không sở hữu contract trả `403`; contract không Active hoặc bill trùng trả `409`.

**Ghi chú đặt trong nhánh thành công:** `ElectricityPrice`, `WaterPrice`, `RoomPrice`, `ServiceFee` lấy từ `RentalContract`, không lấy từ giá client gửi lên trong DTO.

### ALT 2 - Gửi notification sau khi lập bill

**Vị trí:** bao quanh `1.11` - `1.12`, dùng `opt` hoặc ALT nhỏ.

- **Thành công:** Notification được lưu và có thể phát SignalR.
- **Thất bại:** `MonthlyBillBLL` chỉ ghi warning; hóa đơn đã tạo vẫn trả thành công. Không vẽ như một lỗi làm rollback bill.

### ALT 3 - Tạo link thanh toán Bill

**Vị trí:** sau `3.5`.

- **Thành công:** Bill thuộc Tenant, `Status` là `Unpaid` hoặc `Overdue`, không có transaction Pending/Succeeded; vẽ `3.6` đến `3.10`.
- **Thất bại:** Bill không thuộc Tenant (`403`); Bill `Paid`, `Cancelled`, `PendingPayment` hoặc có giao dịch active (`409`); request chọn sai số lượng mục tiêu thanh toán (`400`).

### ALT 4 - Callback VNPay cho MonthlyBill

**Vị trí:** sau `4.5`.

- **Nhánh signature/order/amount sai:** trả `PaymentResultDto(Success=false)`, không trả Bill sang Paid.
- **Nhánh callback trùng sau success:** trả kết quả đã xử lý, không update lần hai.
- **Nhánh VNPay thất bại:** `PaymentTransaction=Failed`; nếu Bill đang `PendingPayment`, khôi phục `Unpaid` hoặc `Overdue` theo `DueDate`.
- **Nhánh success lần đầu:** vẽ `4.6` đến `4.11`; Bill thành `Paid`, `PaymentMethod=VNPay`, thông báo cho hai bên.

## D. Bố cục vẽ

- Frame A có **7 lifeline**; Frame B có **10 lifeline**. Vẽ các frame dọc liên tiếp trên cùng trang.
- Frame A: Chủ trọ, Form, MonthlyBillController, MonthlyBillBLL, DbContext, Database, NotificationBLL.
- Frame B: Tenant, Form, MonthlyBillController, MonthlyBillBLL, PaymentController, PaymentBLL, DbContext, Database, VNPay, NotificationBLL.
- Activation bar: `MonthlyBillBLL` từ `1.2` đến `1.13`; `PaymentBLL` từ `3.2` đến `3.8` và `4.1` đến `4.10`.
- ALT 1 bắt đầu sau khi DB trả contract/bill ở `1.6`. ALT 3 bắt đầu sau `3.5`. ALT 4 là khung lớn nhất và bao quanh toàn bộ nhánh callback.
- Không vẽ Tenant gọi `PUT /api/hoa-don/{id}/thanh-toan`: method `MonthlyBillController.ThanhToan` đang có `[Authorize(Roles = "Landlord")]`, nên không phải luồng thanh toán Tenant hiện tại.

## E. Đối chiếu code

| Bước | Controller/Service | Method thực tế | File source |
|---|---|---|---|
| 1.1 | `MonthlyBillController` | `Tao(TaoHoaDonDto)` | `backend/RoomRental.BackEnd/Controllers/MonthlyBillController.cs` |
| 1.2 - 1.13 | `MonthlyBillBLL` | `TaoAsync` | `backend/RoomRental.BackEnd/BLL/MonthlyBillBLL.cs` |
| 1 frontend | `monthlyBillService` | `createBill` | `frontend/src/services/monthlyBillService.ts` |
| 1 frontend UI | `LandlordContractsPage` | Gọi `monthlyBillService.createBill` | `frontend/src/pages/landlord/LandlordContractsPage.tsx` |
| 2.1 - 2.6 | `MonthlyBillController` / `MonthlyBillBLL` | `LayCuaToi`, `LayDanhSachCuaToiAsync` | `Controllers/MonthlyBillController.cs`, `BLL/MonthlyBillBLL.cs` |
| 2 frontend | `monthlyBillService` | `getMyBills` | `frontend/src/services/monthlyBillService.ts` |
| 3.1 - 3.9 | `PaymentController` / `PaymentBLL` | `CreatePaymentUrl`, `CreatePaymentUrlAsync` | `Controllers/PaymentController.cs`, `BLL/PaymentBLL.cs` |
| 4 - 4.11 | `PaymentController` / `PaymentBLL` | `PaymentReturn`, `PaymentIpn`, `ProcessPaymentReturnAsync` | `Controllers/PaymentController.cs`, `BLL/PaymentBLL.cs` |
| 4.8 - 4.9 | `NotificationBLL` | `CreateNotificationAsync` | `backend/RoomRental.BackEnd/BLL/NotificationBLL.cs` |
| 3 frontend | `paymentService` | `createVnPayUrl` | `frontend/src/services/paymentService.ts` |
| 2/3 frontend UI | `TenantRentalsPage` | Gọi `getMyBills` và `createVnPayUrl` | `frontend/src/pages/tenant/TenantRentalsPage.tsx` |
