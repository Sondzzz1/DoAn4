# Thanh toán tiền mặt và phiếu thu

Người thuê có thể chọn VNPay hoặc tiền mặt cho khoản cọc và hóa đơn tháng. Chọn tiền mặt chỉ ghi nhận dự định trả trực tiếp; chưa ghi nhận đã thanh toán và không thay đổi hạn cọc.

## Sử dụng

1. Người thuê mở khoản cọc hoặc hóa đơn, bấm **Thanh toán**, chọn **Tiền mặt trực tiếp**.
2. Người thuê giao đủ số tiền cho đúng chủ trọ trước hạn. Màn hình tiếp tục hiển thị chưa thanh toán cho tới khi được xác nhận.
3. Chủ trọ mở **Tiền mặt và phiếu thu**, bấm **Xác nhận đã nhận tiền mặt**, kiểm tra số tiền và xác nhận đã thực nhận đủ tiền. Có thể thêm ghi chú.
4. Hệ thống phát hành mã phiếu thu, lưu người xác nhận và thời gian, thông báo cho người thuê. Hóa đơn chuyển sang đã thanh toán; cọc chuyển sang đã xác nhận và có thể tiếp tục lập hợp đồng theo điều kiện hiện có.

Chủ trọ cũng có thể bấm **Thu tiền mặt** ngay trên hóa đơn chưa thanh toán/quá hạn. Thao tác này tạo phiếu thu theo cùng quy tắc dù người thuê chưa chọn phương thức trước đó.

Người thuê có thể hủy lựa chọn tiền mặt chưa được xác nhận để chọn VNPay. Phiếu thu đã xác nhận được giữ trong lịch sử, không hủy bằng thao tác đổi phương thức. Các khoản hoàn cọc và tất toán tiếp tục dùng quy trình nghiệp vụ tương ứng.

## Quy tắc

- Chỉ chủ trọ sở hữu khoản thu được xác nhận nhận tiền; quản trị viên không thay chủ trọ thu tiền mặt qua API này.
- Số tiền và hai bên được kiểm tra lại lúc xác nhận; khoản thu thay đổi phải chọn lại phương thức.
- Thao tác thu trực tiếp trên hóa đơn gửi kèm `expectedAmount` đúng số tiền chủ trọ đã kiểm tra. Nếu hóa đơn đổi số tiền khi cửa sổ xác nhận đang mở, yêu cầu bị từ chối và cần tải lại.
- Cọc phải còn hạn, yêu cầu còn được duyệt, phòng còn được giữ cho đúng yêu cầu và chưa có quyền thuê khác.
- Không thu tiền mặt khi VNPay đang xử lý, đã thành công hoặc có sự kiện cần đối soát. Không tạo lượt VNPay mới khi lựa chọn tiền mặt còn hiệu lực.
- Cùng một khoản thu chỉ có một lựa chọn tiền mặt còn hiệu lực hoặc một phiếu thu đã nhận. Thao tác xác nhận gửi lại trả về phiếu thu cũ.
- Xử lý tiền mặt và VNPay dùng cùng khóa giao dịch. Callback VNPay đến muộn được đưa vào đối soát và không ghi nhận thanh toán hai lần.
- Phiếu thu tiền mặt tách khỏi giao dịch của cổng VNPay; không tạo mã giao dịch ngân hàng giả cho tiền mặt.

## Cập nhật database

Database hiện tại: sao lưu và kiểm tra bản sao lưu, chạy `backend/RoomRental.BackEnd/SQL/11_CashPayments.sql` trên đúng database. Với `RoomRentalDB`, đặt `@AllowMainDatabase = 1` sau khi rà soát. Script chỉ thêm `PhieuThuTienMat`, không sửa các dòng nghiệp vụ hiện có và có thể chạy lại để kiểm tra schema.

Database mới: tạo schema nền bằng `00_CreateCurrentSchema.sql`, áp dụng các nâng cấp hiện có tới `10_Deploy_ApprovedModules.sql`, rồi chạy `11_CashPayments.sql`.

Khởi động lại backend sau khi cập nhật code/schema. Nếu thiếu bảng phiếu thu, chức năng trả lỗi 503 chỉ rõ script cần áp dụng.

## API

| Phương thức | Đường dẫn | Quyền | Nội dung |
| --- | --- | --- | --- |
| GET | `/api/thanh-toan/tien-mat` | Người tham gia; Admin xem toàn bộ | Danh sách lựa chọn và phiếu thu |
| POST | `/api/thanh-toan/tien-mat/chon` | Tenant sở hữu khoản thu | `{ "depositId": 1 }` hoặc `{ "monthlyBillId": 1 }` |
| POST | `/api/thanh-toan/tien-mat/{id}/xac-nhan` | Landlord sở hữu khoản thu | `{ "note": "Đã nhận đủ tiền" }` |
| POST | `/api/thanh-toan/tien-mat/{id}/huy` | Tenant sở hữu lựa chọn | Hủy lựa chọn chưa nhận tiền |
| PUT | `/api/hoa-don/{id}/thanh-toan` | Landlord sở hữu hóa đơn | `{ "expectedAmount": 3000000, "ghiChu": "Đã nhận đủ tiền" }`; tạo cùng loại phiếu thu |

Trạng thái lựa chọn/phiếu thu: `0` đã chọn nhưng chưa trả tiền; `1` đã nhận và có phiếu thu; `2` đã hủy lựa chọn. Trạng thái này không thay thế trạng thái cọc hoặc hóa đơn.

## Kiểm tra

`dotnet test backend/RoomRental.BackEnd.Tests` chạy kiểm thử nghiệp vụ. Các kiểm thử SQL thực chỉ chạy khi `ROOM_RENTAL_TEST_SQL` chỉ tới database tên bắt đầu bằng `RoomRental_Verification_`; mỗi kiểm thử tạo database QA riêng.

`npm run build` trong `frontend` kiểm tra TypeScript và bản build giao diện. Luồng lưu/liên kết thẻ chưa được bổ sung trong thay đổi này.
