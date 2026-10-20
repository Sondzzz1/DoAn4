# SQL bổ sung các module đã duyệt

Ngày chuẩn bị: 17/10/2026. File chạy: `10_Deploy_ApprovedModules.sql` trong cùng thư mục.

## Phạm vi

File tổng hợp chạy lần lượt nội dung SQL 07, 08 và 09 trong một transaction bao ngoài. Không cần chạy lại ba file riêng khi đã dùng file tổng hợp này.

| Nhóm | Bảng mới |
|---|---|
| Ở ghép | `TinTimNguoiOGhep`, `YeuCauOGhep`, `NguoiOCungHopDong` |
| Đối soát thanh toán và hoàn cọc trước hợp đồng | `SuKienThanhToan`, `YeuCauHoanCoc` |
| Kết thúc hợp đồng, bàn giao và tất toán | `HoSoKetThucHopDong`, `YeuCauKetThucHopDong`, `PhuongAnTatToanHopDong`, `KhoanMucTatToanHopDong`, `LichSuKetThucHopDong` |

Các trường, khóa ngoại, CHECK, index và RowVersion nằm trong định nghĩa các bảng mới. **Không thêm/sửa/xóa cột của bảng cũ; không UPDATE/DELETE dữ liệu nghiệp vụ; không tạo dữ liệu mẫu; không tự lập hồ sơ/tất toán giả cho hợp đồng cũ.** Không bổ sung cột chỉ số điện/nước ban đầu ngoài thiết kế đã được duyệt.

Đây là bản nâng cấp cho database đang có schema nền tảng của project, không phải script cài đặt database trống. Nếu thiếu schema nền tảng hoặc gặp schema không tương thích, dừng lại để kiểm tra lỗi; không xóa bảng hoặc đổi tên bảng để chạy cho qua.

## Cách chạy trên SSMS

1. Kết nối đúng SQL Server đang sử dụng. Sao lưu đầy đủ `RoomRentalDB` trước khi chạy; xác nhận bản sao lưu thành công và có thể phục hồi.
2. Tạm dừng backend API/worker để không phát sinh thao tác trong lúc nâng cấp.
3. Mở `10_Deploy_ApprovedModules.sql` bằng SSMS. Chọn **RoomRentalDB** ở danh sách database của cửa sổ query. Không để `master`.
4. Chỉ đổi dòng xác nhận **ở đầu file** từ `DECLARE @AllowMainDatabase bit = 0;` thành `DECLARE @AllowMainDatabase bit = 1;`. Không chỉnh nội dung các batch bên trong.
5. Nhấn F5 để chạy **toàn bộ file**, không chỉ bôi đen một đoạn. Không cần bật SQLCMD Mode. Không chạy bên trong transaction đang mở.
6. Chỉ coi là hoàn thành khi Messages có `SUCCESS: approved modules 07/08/09 committed. Existing business rows unchanged.` và Results hiển thị đủ 10 bảng. Các thông báo thành công của từng module trước đó chưa có nghĩa transaction chung đã commit.
7. Chạy lại backend và kiểm tra các chức năng ở ghép, hoàn cọc, hồ sơ kết thúc hợp đồng với tài khoản phù hợp. Không chỉnh dữ liệu tài chính cũ để vượt qua các bước xác minh của ứng dụng.

Yêu cầu SQL Server 2017 trở lên, compatibility level tối thiểu 130, tài khoản có quyền tạo bảng/index/constraint và default schema là `dbo`. Script chỉ kiểm tra các điều kiện này, không tự thay đổi cấu hình database/quyền.

## Nếu có lỗi

- `51100`: chưa bật xác nhận cho database chính sau khi sao lưu.
- `51101`: đang chọn sai database.
- `51102`: còn transaction khác trên cửa sổ query. Xác minh và xử lý transaction đó trước; không tự COMMIT một transaction chưa rõ nguồn gốc.
- `51103` đến `51106`: chưa đủ điều kiện về tài khoản, phiên bản hoặc schema nền tảng.
- Lỗi partial schema hoặc incompatible schema: giữ nguyên hiện trạng, gửi đầy đủ mã/thông báo lỗi để kiểm tra. Script không tự sửa schema khác thiết kế.
- Lỗi trong một module: transaction chung rollback cả các bảng mới vừa tạo ở module trước. Dữ liệu có trước lần chạy được giữ lại. Không dùng `00_CreateCurrentSchema.sql` hoặc script full schema/data để xử lý lỗi trên database đã có dữ liệu.

Các module đã đầy đủ được giữ lại; file có thể chạy lại với schema tương thích. Đây không phải công cụ tự sửa mọi schema tùy biến. Nên thử trên bản sao database thực trước khi triển khai chính thức.

## Kiểm tra đã thực hiện

6/6 test SQL Server PASS trên database QA riêng `RoomRental_Verification_SqlBundle_20261017_01_*`, không bỏ qua test:

- Nâng cấp từ database thiếu cả 10 bảng, đã có 3 bảng, đã có 5 bảng, hoặc đã đủ 10 bảng.
- Đối chiếu toàn bộ cột mới với EF model: tên, kiểu, độ dài chuỗi, nullable, decimal precision/scale, datetime2 scale và identity.
- Snapshot mọi cột/mọi dòng của các bảng có trước lần nâng cấp; so sánh trước/sau và sau khi chạy lại.
- Giả lập lỗi partial schema ở module cuối, kiểm tra rollback cả bảng ở ghép/thanh toán vừa tạo, giữ nguyên bảng có trước và không để transaction mở.
- Kiểm tra chặn database sai/chưa xác nhận triển khai chính. Nhánh xác nhận được kiểm tra bằng tên database QA thay thế, không chạy DDL trên database chính.

Đây là kiểm tra file SQL nâng cấp, không phải xác nhận lại toàn bộ E2E/ngân hàng. VNPay real sandbox bank transaction/refund vẫn **NOT VERIFIED**.

**Chưa chạy SQL lên RoomRentalDB. Không commit/push.**

## Tái tạo file khi SQL nguồn thay đổi

Từ thư mục project:

```powershell
powershell -NoProfile -File tools/New-ModuleUpgradeSql.ps1
powershell -NoProfile -File tools/New-ModuleUpgradeSql.ps1 -Check
```

Generator chỉ đọc/ghi file, không kết nối database. Các script 07/08/09 gốc được giữ nguyên; việc cho phép database chính trong batch con chỉ nằm trong bản tổng hợp đã có chốt xác nhận bên ngoài.
