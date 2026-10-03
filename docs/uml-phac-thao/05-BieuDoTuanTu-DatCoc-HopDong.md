# Phác thảo tuần tự - Đặt cọc và ký hợp đồng

Vì luồng này dài, nên vẽ **hai frame liên tiếp trên cùng một trang**: Frame A là `Tạo cọc và thanh toán VNPay`; Frame B là `Xác nhận cọc, tạo và ký hợp đồng`. Đây vẫn là một biểu đồ nghiệp vụ hoàn chỉnh nhưng dễ đưa vào báo cáo hơn.

## A. Các đối tượng tham gia, từ trái sang phải

### Frame A - Tạo cọc và thanh toán

1. **Actor: Người thuê**
2. **Boundary: `FormDatCocThanhToan`** - nhãn mô tả; UI thực tế là `TenantRentalsPage`.
3. **Control: `DepositController`**
4. **Control: `DepositBLL`**
5. **Control: `PaymentController`**
6. **Control: `PaymentBLL`**
7. **Entity: `ApplicationDbContext`**
8. **Database: SQL Server / `RoomRentalDB`**
9. **Actor: VNPay**
10. **Control: `NotificationBLL`** - chỉ có ở callback thành công.

### Frame B - Xác nhận cọc và ký hợp đồng

1. **Actor: Chủ trọ**
2. **Boundary: `FormQuanLyHopDong`** - nhãn mô tả; UI thực tế là `LandlordContractsPage`.
3. **Control: `DepositController`**
4. **Control: `DepositBLL`**
5. **Control: `ContractController`**
6. **Control: `RentalContractBLL`**
7. **Entity: `ApplicationDbContext`**
8. **Database: SQL Server / `RoomRentalDB`**
9. **Actor: Người thuê**
10. **Boundary: `FormXacNhanHopDong`** - nhãn mô tả; thao tác thực tế có ở `TenantRentalsPage` và `LandlordContractsPage`.

## B. Bảng các mũi tên tuần tự

### Frame A1 - Tạo khoản đặt cọc

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 1 | Người thuê | `FormDatCocThanhToan` | Nhập số tiền cọc cho RentalRequest đã được duyệt | Call |
| 1.1 | `FormDatCocThanhToan` | `DepositController` | `POST /api/dat-coc` - `Tao(TaoDatCocDto)` + JWT | Call |
| 1.2 | `DepositController` | `DepositBLL` | `TaoAsync(accountId, dto)` | Call |
| 1.3 | `DepositBLL` | `ApplicationDbContext` | Tìm RentalRequest cùng Post và Room | Call |
| 1.4 | `ApplicationDbContext` | SQL Server | `SELECT YeuCauThuePhong, TinDang, PhongTro, DatCoc` | Call |
| 1.5 | SQL Server | `ApplicationDbContext` | Trả Request, Room và Deposit hiện có | Return |
| 1.6 | `DepositBLL` | `ApplicationDbContext` | `Add(Deposit)` với `Status=Pending`; `SaveChangesAsync()` | Call |
| 1.7 | `ApplicationDbContext` | SQL Server | `INSERT DatCoc` | Call |
| 1.8 | SQL Server | `ApplicationDbContext` | Trả Deposit.Id | Return |
| 1.9 | `DepositBLL` | `DepositController` | Trả `DatCocDto(Status=Pending)` | Return |
| 1.10 | `DepositController` | `FormDatCocThanhToan` | `200 OK` | Return |

### Frame A2 - Tạo giao dịch VNPay và callback

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 2 | Người thuê | `FormDatCocThanhToan` | Chọn thanh toán VNPay cho Deposit | Call |
| 2.1 | `FormDatCocThanhToan` | `PaymentController` | `POST /api/thanh-toan/vnpay-tao-url` với `CreatePaymentRequestDto { DepositId }` | Call |
| 2.2 | `PaymentController` | `PaymentBLL` | `CreatePaymentUrlAsync(userId, dto, clientIp)` | Call |
| 2.3 | `PaymentBLL` | `ApplicationDbContext` | Bắt đầu transaction Serializable; kiểm tra Deposit thuộc Tenant, Pending và chưa có giao dịch active | Call |
| 2.4 | `ApplicationDbContext` | SQL Server | `SELECT DatCoc, GiaoDichThanhToan` | Call |
| 2.5 | SQL Server | `ApplicationDbContext` | Trả dữ liệu Deposit/giao dịch | Return |
| 2.6 | `PaymentBLL` | `ApplicationDbContext` | `Add(PaymentTransaction)` với `TargetType=Deposit`, `Status=Pending`; lưu và commit | Call |
| 2.7 | `ApplicationDbContext` | SQL Server | `INSERT GiaoDichThanhToan` | Call |
| 2.8 | `PaymentBLL` | `PaymentController` | Trả `PaymentResponseDto(PaymentUrl, OrderId, Amount)` | Return |
| 2.9 | `PaymentController` | `FormDatCocThanhToan` | `200 OK` | Return |
| 2.10 | `FormDatCocThanhToan` | VNPay | Điều hướng browser tới `PaymentUrl` | Call |
| 3 | VNPay | `PaymentController` | `GET /api/thanh-toan/vnpay-return` hoặc `GET /api/thanh-toan/vnpay-ipn` | Call |
| 3.1 | `PaymentController` | `PaymentBLL` | `ProcessPaymentReturnAsync(Request.Query)` | Call |
| 3.2 | `PaymentBLL` | `PaymentBLL` | Kiểm tra signature, `vnp_TxnRef`, amount, response code, transaction code | Call |
| 3.3 | `PaymentBLL` | `ApplicationDbContext` | Tìm `PaymentTransaction` theo `OrderId`, gồm Deposit | Call |
| 3.4 | `ApplicationDbContext` | SQL Server | `SELECT GiaoDichThanhToan, DatCoc` | Call |
| 3.5 | SQL Server | `ApplicationDbContext` | Trả transaction và Deposit | Return |
| 3.6 | `PaymentBLL` | `ApplicationDbContext` | Callback thành công: transaction Serializable, `PaymentTransaction=Succeeded`, `Deposit=Paid`, `PaidAt=Now` | Call |
| 3.7 | `ApplicationDbContext` | SQL Server | `UPDATE GiaoDichThanhToan`, `UPDATE DatCoc`, commit | Call |
| 3.8 | `PaymentBLL` | `NotificationBLL` | `CreateNotificationAsync` cho Tenant và Landlord | Call |
| 3.9 | `NotificationBLL` | SQL Server | `INSERT ThongBao`; phát SignalR nếu có kết nối | Call |
| 3.10 | `PaymentBLL` | `PaymentController` | Trả `PaymentResultDto` | Return |
| 3.11 | `PaymentController` | `FormDatCocThanhToan` | Với `vnpay-return`: redirect `/payment/result?...`; với IPN: trả `RspCode` | Return |

### Frame B1 - Chủ trọ xác nhận cọc

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 4 | Chủ trọ | `FormQuanLyHopDong` | Chọn xác nhận khoản cọc đã thanh toán | Call |
| 4.1 | `FormQuanLyHopDong` | `DepositController` | `PUT /api/dat-coc/{id}/trang-thai` với `TrangThai=Confirmed` | Call |
| 4.2 | `DepositController` | `DepositBLL` | `CapNhatTrangThaiAsync(accountId, id, Confirmed)` | Call |
| 4.3 | `DepositBLL` | `ApplicationDbContext` | Tìm Deposit, kiểm tra `LandlordAccountId` và `Status=Paid` | Call |
| 4.4 | `ApplicationDbContext` | SQL Server | `SELECT DatCoc`; `UPDATE TrangThai=Confirmed` | Call |
| 4.5 | `DepositBLL` | `DepositController` | Trả `DatCocDto(Status=Confirmed)` | Return |
| 4.6 | `DepositController` | `FormQuanLyHopDong` | `200 OK` | Return |

### Frame B2 - Tạo hợp đồng và hai bên ký

| STT | Từ đối tượng | Đến đối tượng | Nội dung mũi tên | Kiểu mũi tên |
|---|---|---|---|---|
| 5 | Chủ trọ | `FormQuanLyHopDong` | Nhập ngày bắt đầu, ngày kết thúc, tiền thuê, điều khoản | Call |
| 5.1 | `FormQuanLyHopDong` | `ContractController` | `POST /api/hop-dong` - `Tao(TaoHopDongDto)` | Call |
| 5.2 | `ContractController` | `RentalContractBLL` | `TaoAsync(accountId, dto)` | Call |
| 5.3 | `RentalContractBLL` | `ApplicationDbContext` | Bắt đầu transaction Serializable; lấy Request + Post + Room + Deposit + Contract liên quan | Call |
| 5.4 | `ApplicationDbContext` | SQL Server | `SELECT YeuCauThuePhong, TinDang, PhongTro, DatCoc, HopDongThue` | Call |
| 5.5 | SQL Server | `ApplicationDbContext` | Trả dữ liệu kiểm tra | Return |
| 5.6 | `RentalContractBLL` | `ApplicationDbContext` | `Add(RentalContract)` với `Status=PendingSignature`; `RentalRequest.Status=ConvertedToContract`; lưu/commit | Call |
| 5.7 | `ApplicationDbContext` | SQL Server | `INSERT HopDongThue`; `UPDATE YeuCauThuePhong` | Call |
| 5.8 | `RentalContractBLL` | `ContractController` | Trả `HopDongDto` | Return |
| 5.9 | `ContractController` | `FormQuanLyHopDong` | `200 OK` - chờ hai bên xác nhận | Return |
| 6 | Chủ trọ hoặc Người thuê | `FormXacNhanHopDong` | Chọn xác nhận hợp đồng | Call |
| 6.1 | `FormXacNhanHopDong` | `ContractController` | `PUT /api/hop-dong/{id}/xac-nhan` - `XacNhan` | Call |
| 6.2 | `ContractController` | `RentalContractBLL` | `XacNhanAsync(accountId, id)` | Call |
| 6.3 | `RentalContractBLL` | `ApplicationDbContext` | Bắt đầu transaction Serializable; kiểm tra participant và `PendingSignature` | Call |
| 6.4 | `ApplicationDbContext` | SQL Server | `SELECT HopDongThue` | Call |
| 6.5 | `RentalContractBLL` | `ApplicationDbContext` | Đặt `TenantConfirmed=true` hoặc `LandlordConfirmed=true` | Call |
| 6.6 | `ApplicationDbContext` | SQL Server | `UPDATE HopDongThue` | Call |
| 6.7 | `RentalContractBLL` | `ApplicationDbContext` | Khi cả hai đã true: kiểm tra overlap active và Room Available; đặt `Status=Active`, `Room.Status=Rented`; lưu/commit | Call |
| 6.8 | `ApplicationDbContext` | SQL Server | `SELECT` kiểm tra; `UPDATE HopDongThue, PhongTro` | Call |
| 6.9 | `RentalContractBLL` | `ContractController` | Trả `HopDongDto` PendingSignature hoặc Active | Return |
| 6.10 | `ContractController` | `FormXacNhanHopDong` | `200 OK` | Return |

## C. Các khung ALT cần vẽ

### ALT 1 - Tạo Deposit

**Vị trí:** sau `1.5`.

- **Thành công:** Request thuộc tenant, có `Status=Approved`, Room `Available`, `SoTien > 0`, chưa có Deposit; vẽ `1.6` đến `1.10`.
- **Thất bại:** request không tồn tại (`404`), không phải tenant (`403`), request chưa Approved/Room không trống/Deposit tồn tại (`409`), số tiền không dương (`400`).

### ALT 2 - Tạo link VNPay

**Vị trí:** sau `2.5`.

- **Thành công:** chỉ có `DepositId`, Deposit thuộc Tenant, đang Pending và chưa có PaymentTransaction Pending/Succeeded; vẽ `2.6` đến `2.10`.
- **Thất bại:** chọn đồng thời Deposit/Bill hoặc không chọn mục tiêu (`400`); không sở hữu (`403`); Deposit không Pending hoặc đã có giao dịch active (`409`).

### ALT 3 - Callback VNPay

**Vị trí:** sau `3.5`.

- **Nhánh signature/order/amount không hợp lệ:** `PaymentBLL` trả `PaymentResultDto(Success=false)`, không đánh dấu `Deposit=Paid`.
- **Nhánh callback lặp sau Success:** trả Success đã xử lý, không cập nhật lần hai.
- **Nhánh VNPay thất bại:** `PaymentTransaction.Status=Failed`; Deposit vẫn Pending.
- **Nhánh thành công lần đầu:** vẽ `3.6` đến `3.11`; Deposit đổi sang `Paid`, sau commit gọi `NotificationBLL`.

### ALT 4 - Chủ trọ xác nhận cọc

**Vị trí:** sau `4.3`.

- **Thành công:** chủ trọ sở hữu Deposit và Deposit `Paid`; cập nhật `Confirmed`.
- **Thất bại:** không sở hữu (`403`) hoặc không ở `Paid` (`409`).

### ALT 5 - Tạo hợp đồng

**Vị trí:** sau `5.5`.

- **Thành công:** Request Approved, Deposit Confirmed, Room Available, ngày hợp lệ, tiền thuê dương, chưa có contract và không overlap; vẽ `5.6` đến `5.9`.
- **Thất bại:** sai ownership, cọc chưa Confirmed, phòng không trống, thời gian trùng hoặc request đã có contract; trả `403`/`409`/`400`.

### ALT 6 - Chữ ký hợp đồng

**Vị trí:** sau `6.4`.

- **Nhánh chữ ký đầu tiên:** chỉ cập nhật một trong hai cờ xác nhận, contract vẫn `PendingSignature`.
- **Nhánh chữ ký thứ hai:** sau khi cả `TenantConfirmed` và `LandlordConfirmed` true, kiểm tra overlap/Room rồi cập nhật `Active` và `Rented`.
- **Nhánh lỗi:** user không là participant, ký lặp, contract không PendingSignature, overlap active hoặc Room không Available; trả `403`/`409`.

## D. Bố cục vẽ

- Frame A có **10 lifeline**, Frame B có **10 lifeline**. Không nên cố gộp toàn bộ 20 lifeline vào một hàng.
- Frame A: đặt Tenant, Form, DepositController, DepositBLL, PaymentController, PaymentBLL, DbContext, Database, VNPay, NotificationBLL theo thứ tự trái-phải.
- Frame B: đặt Landlord, Form quản lý, DepositController, DepositBLL, ContractController, RentalContractBLL, DbContext, Database, Tenant, Form xác nhận theo thứ tự trái-phải.
- Activation bar: `PaymentBLL` bao quanh `2.2` đến `2.8` và `3.1` đến `3.10`; `RentalContractBLL` bao quanh `5.2` đến `5.8` và `6.2` đến `6.9`.
- ALT 3 là khung lớn nhất; không cần tách riêng return/IPN thành hai sequence vì source dùng chung `ProcessPaymentReturnAsync`.
- Không vẽ mũi tên VNPay tự kích hoạt hợp đồng: callback chỉ đổi transaction và Deposit sang Paid; chủ trọ vẫn phải xác nhận cọc và tạo hợp đồng.

## E. Đối chiếu code

| Bước | Controller/Service | Method thực tế | File source |
|---|---|---|---|
| 1.1 | `DepositController` | `Tao(TaoDatCocDto)` | `backend/RoomRental.BackEnd/Controllers/RentalManagementControllers.cs` |
| 1.2 - 1.9 | `DepositBLL` | `TaoAsync` | `backend/RoomRental.BackEnd/BLL/RentalFeaturesBLL.cs` |
| 2.1 - 2.9 | `PaymentController` / `PaymentBLL` | `CreatePaymentUrl`, `CreatePaymentUrlAsync` | `Controllers/PaymentController.cs`, `BLL/PaymentBLL.cs` |
| 3 - 3.11 | `PaymentController` / `PaymentBLL` | `PaymentReturn`, `PaymentIpn`, `ProcessPaymentReturnAsync` | `Controllers/PaymentController.cs`, `BLL/PaymentBLL.cs` |
| 3.8 - 3.9 | `NotificationBLL` | `CreateNotificationAsync` | `backend/RoomRental.BackEnd/BLL/NotificationBLL.cs` |
| 4.1 - 4.5 | `DepositController` / `DepositBLL` | `TrangThai`, `CapNhatTrangThaiAsync` | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| 5.1 - 5.8 | `ContractController` / `RentalContractBLL` | `Tao`, `TaoAsync` | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| 6.1 - 6.9 | `ContractController` / `RentalContractBLL` | `XacNhan`, `XacNhanAsync` | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| Tenant frontend | `rentalService`, `paymentService` | `createDeposit`, `confirmContract`, `createVnPayUrl` | `frontend/src/services/rentalService.ts`, `frontend/src/services/paymentService.ts` |
| Landlord frontend | `landlordService`, `rentalService` | `confirmDeposit`, `createContract`, `confirmContract` | `frontend/src/services/landlordService.ts`, `frontend/src/services/rentalService.ts` |
