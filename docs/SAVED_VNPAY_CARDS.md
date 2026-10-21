# Thẻ VNPay đã liên kết

Người thuê có thể liên kết thẻ qua trang VNPay, chọn lại thẻ khi thanh toán cọc/hóa đơn và gỡ thẻ khỏi ứng dụng. Hệ thống lưu mã liên kết được mã hóa và bốn số cuối để hiển thị. Ứng dụng không thu thập số thẻ đầy đủ, CVV, mật khẩu ngân hàng hoặc OTP.

## Trạng thái hiện tại

Tính năng mặc định **chưa được kích hoạt** (`VnPay:Tokenization:Enabled = false`) vì merchant chưa xác nhận được cấp dịch vụ Tokenization. Màn hình hiển thị trạng thái này; VNPay thông thường và tiền mặt tiếp tục sử dụng được. Các kiểm thử liên kết/thanh toán dùng phản hồi ký giả lập, chưa thực hiện liên kết thẻ hoặc giao dịch ngân hàng thật.

## Trước khi kích hoạt

1. Yêu cầu VNPay cấp/bật dịch vụ Tokenization cho merchant và cung cấp terminal code, hash secret, môi trường, địa chỉ dịch vụ và cấu hình ký tương ứng. Thông tin của dịch vụ thanh toán thông thường không tự xác nhận quyền Tokenization.
2. Xác nhận phiên bản Token **2.1.0**, dùng **HMACSHA512**. Tài liệu Token có ví dụ ký cũ không nhất quán với hướng dẫn chuyển đổi thuật toán. Xác nhận với VNPay cách chuẩn hóa chuỗi ký rồi đặt `SignatureEncoding` thành chính xác `Raw` hoặc `UrlEncoded`; không tự bật theo phỏng đoán. Không có cơ chế hạ cấp sang MD5.
3. Đăng ký IPN Token tại `/api/thanh-toan/vnpay-token-ipn`. Endpoint này xử lý cả thông báo liên kết (`token_create`) và thanh toán (`token_pay`), xác minh chữ ký trước khi cập nhật. IPN phải dùng địa chỉ HTTPS mà máy chủ VNPay truy cập được; localhost chỉ dùng cho phát triển/đường trình duyệt trả về.
4. Cấu hình các URL trả về đúng domain/port frontend và backend, kiểm thử trên môi trường sandbox được VNPay cấp, rồi mới bật dịch vụ.

Thêm cấu hình riêng vào `backend/RoomRental.BackEnd/appsettings.Local.json` theo khối `VnPay.Tokenization` trong file ví dụ. Giữ nguyên cấu hình kết nối database, JWT và VNPay thường đã có. Không đưa hash secret hoặc khóa Data Protection vào Git.

| Khóa trong `VnPay:Tokenization` | Ý nghĩa |
| --- | --- |
| `Enabled` | Chỉ bật sau khi được cấp dịch vụ và xác nhận cấu hình |
| `TmnCode`, `HashSecret` | Thông tin kết nối của dịch vụ Token |
| `SignatureEncoding` | `Raw` hoặc `UrlEncoded` theo cấu hình đã xác nhận với VNPay |
| `CreateUrl`, `PayUrl` | Địa chỉ liên kết và thanh toán; mặc định các endpoint sandbox chính thức |
| `ReturnUrl` | Backend `/api/thanh-toan/the-da-luu/return` |
| `PaymentReturnUrl` | Backend `/api/thanh-toan/vnpay-token-return` |
| `FrontendReturnUrl` | Trang `/tenant/rentals` nhận kết quả liên kết |

Kết quả thanh toán tiếp tục dùng `VnPay:FrontendReturnUrl` hiện có, trỏ tới trang `/payment/result`.

## Sử dụng sau khi được kích hoạt

Trong **Thuê phòng → Hóa đơn**, chọn **Liên kết thẻ VNPay**, chọn thẻ nội địa/quốc tế và tiếp tục trên VNPay. Kiểm tra thông tin xác thực mà VNPay hiển thị trước khi đồng ý. Khi liên kết hoàn tất, danh sách chỉ hiển thị ngân hàng và bốn số cuối.

Khi thanh toán cọc/hóa đơn, chọn thẻ đã liên kết trong cửa sổ phương thức thanh toán. Xác thực giao dịch trên VNPay theo yêu cầu của ngân hàng. Việc liên kết không tự trả tiền cho khoản cọc/hóa đơn và không tự gia hạn chúng.

**Gỡ thẻ khỏi hệ thống** xóa mã đã mã hóa khỏi ứng dụng và giữ lại dấu vết cần thiết cho lịch sử giao dịch. Thao tác này không đóng thẻ hoặc xóa tài khoản tại ngân hàng/VNPay. Không gỡ khi thẻ còn giao dịch đang chờ xử lý.

Theo [tài liệu Token của VNPay](https://sandbox.vnpayment.vn/apis/files/VNPAY%20Payment%20Token_Techspec%202.1.0-VN.pdf), liên kết có thể phát sinh khoản xác thực tạm thu rồi hoàn; thanh toán bằng token vẫn có bước xác thực OTP. Tham khảo thêm [đặc tả các API Token](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-token/token.html) và [hướng dẫn chuyển đổi chữ ký](https://sandbox.vnpayment.vn/apis/docs/chuyen-doi-thuat-toan/changeTypeHash.html).

## Database và khóa mã hóa

Sao lưu database, áp dụng `backend/RoomRental.BackEnd/SQL/12_SavedPaymentMethods.sql` trên đúng database rồi khởi động lại backend. Script thêm hai bảng thẻ/liên kết và thông tin provider trên giao dịch; các cột tiền, trạng thái, mã đơn và mã giao dịch cũ được giữ nguyên. Script có opt-in riêng khi chạy trên `RoomRentalDB` và có thể chạy lại.

Khóa mã hóa nằm tại `backend/RoomRental.BackEnd/App_Data/DataProtection-Keys` (đã được Git bỏ qua). Trên Windows, khóa còn được bảo vệ bằng DPAPI của tài khoản chạy backend. Bảo quản/sao lưu khóa cùng database và giữ đúng danh tính chạy ứng dụng khi khôi phục. Mất khóa hoặc đổi tài khoản Windows có thể khiến thẻ cũ cần được gỡ và liên kết lại.

## Kiểm thử

- Kiểm thử backend: `SavedPaymentMethodTests`, `VnPayTokenProtocolTests`, `TokenPaymentTests`.
- Kiểm thử SQL opt-in: `SavedPaymentMethodSchemaSqlTests`, chỉ tạo database QA mới có tiền tố `RoomRental_Verification_`.
- Kiểm thử giao diện giả lập: `frontend/scripts/saved-payment-method-ui-test.mjs`.

Các tình huống gồm chữ ký sai, phản hồi trùng/khác metadata tùy chọn, sai chủ thẻ, hết hạn liên kết, token mã hóa, mất khóa, gỡ thẻ đang thanh toán, thanh toán trùng với tiền mặt và callback đến muộn.
