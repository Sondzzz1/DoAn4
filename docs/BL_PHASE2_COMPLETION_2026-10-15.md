# Giai đoạn 2: Hoàn cọc trước hợp đồng và lưu vết thanh toán

Ngày 15/10/2026. Triển khai theo xác nhận cho phép bổ sung `SuKienThanhToan`, `YeuCauHoanCoc` và phương án hoàn đủ tiền thủ công trước khi cọc gắn hợp đồng. Không triển khai Giai đoạn 3/4, không commit/push.

## 1. Kết quả

| Mã / Hạng mục | Trạng thái trong phạm vi được duyệt |
|---|---|
| BL-002 | **FIXED cho cọc chưa gắn bất kỳ hợp đồng nào**: đề nghị, duyệt/từ chối, rút trước khi báo hoàn, chứng từ, Tenant xác nhận nhận đủ hoặc Admin xác minh bằng chứng; hoàn tất mới hủy request và mở phòng nếu không còn quyền khác |
| BL-003 | **FIXED phần lưu vết và bảo vệ quyền thuê đã kiểm thử**: callback muộn, request đã hủy, phòng đã cấp người khác, amount khác kỳ vọng, callback mâu thuẫn đều có event đối soát; không tự cấp lại quyền hoặc sửa lịch sử payment thành công |
| BL-004 | Giữ bản sửa IPN; ACK 00 cho sự kiện hợp lệ đã lưu bền vững, 02 cho retry đã ghi nhận; 01/04/97/99 vẫn phân biệt đúng trường hợp |
| Kết quả frontend | Có cảnh báo màu vàng cho giao dịch cần đối soát, không hiển thị nó như đã thanh toán/cấp quyền thuê thành công |
| Dữ liệu thật | **Chưa chạy SQL nâng cấp vào RoomRentalDB**, chỉ kiểm tra metadata đọc; số bảng mới ở DB thật = 0 |
| VNPay real sandbox bank transaction / refund thật | **NOT VERIFIED**; không gọi ngân hàng, không gọi API hoàn tiền thật |

Các chữ FIXED trên không bao gồm hoàn tiền tự động qua VNPay, hoàn cọc/khấu trừ sau hợp đồng, tất toán công nợ/bàn giao hay chứng nhận tích hợp ngân hàng. Việc Admin ghi kết quả đối soát chỉ lưu bằng chứng xử lý ngoài hệ thống, không tự chuyển tiền hoặc tự cấp lại quyền thuê.

## 2. Nghiệp vụ đã triển khai

### Hoàn cọc

1. Tenant của cọc Paid/Confirmed gửi lý do. Số tiền do server lấy từ Deposit, không nhận số tiền hoàn tùy ý từ client.
2. Chặn nếu cọc đã gắn bất kỳ contract nào, kể cả contract Cancelled/Expired/Terminated; những cọc đó phải theo quy trình Giai đoạn 3.
3. Chỉ có tối đa một yêu cầu Requested/Approved/RefundReported cho một Deposit; transaction Serializable + workflow lock và filtered unique index bảo vệ đồng thời.
4. Deposit chuyển RefundRequested, không thể thanh toán thêm, tạo contract hoặc hủy request để đi vòng giải phóng phòng.
5. Đúng Landlord được duyệt/từ chối. Từ chối hoặc Tenant rút trước bước báo hoàn sẽ khôi phục chính xác Paid/Confirmed trước đó, không đổi PaidAt hay payment history.
6. Landlord báo đã hoàn với chứng từ/mã chuyển tiền. Đây là lời báo có lưu vết, chưa đồng nghĩa Tenant đã nhận tiền.
7. Tenant xác nhận nhận đủ; Admin có thể xác minh thay khi có nội dung bằng chứng bắt buộc. Landlord/khác Tenant không được tự xác nhận đã nhận tiền.
8. Hoàn tất cập nhật RefundCompleted, Deposit Refunded, Request Cancelled trong transaction; chỉ đổi Reserved → Available nếu không còn request Approved, effective contract hoặc cọc/quyền khác được bảo vệ.
9. Lặp xác nhận hoàn tất không ghi lại thời điểm hoàn, không giải phóng phòng lần nữa. Phát sinh event cần đối soát trong lúc hoàn sẽ chặn bước duyệt/báo hoàn/xác nhận cho đến khi xử lý đối soát.

### Callback và đối soát

- Kiểm tra chữ ký, merchant, giới hạn trường, định dạng amount/status trước khi lưu. Chữ ký/merchant sai không tạo event.
- Event lưu số tiền provider báo, order/transaction, source IPN/Return, status provider, mốc nhận UTC, PayDate chuyển GMT+7 sang UTC, lý do và kết quả xử lý. Không lưu secret/JWT/chữ ký/toàn bộ query.
- DeduplicationKey SHA-256 gồm nguồn và trường phản hồi chuẩn hóa; retry cùng payload/nguồn không tạo thêm event. IPN và Return có thể có hai event của cùng một giao dịch; **không cộng các event thành tổng tiền/doanh thu**.
- Chỉ callback hợp lệ với attempt còn Pending và đúng quyền giữ chỗ/target hiện hành mới áp dụng vào Deposit/Bill. Cọc yêu cầu request Approved, Room Reserved, không có quyền thuê xung đột.
- Attempt quá hạn, target/request thay đổi, amount không khớp hoặc transaction đã thuộc order khác được lưu RequiresReview, không ghi đè payment lịch sử, không nhận lại phòng cho Tenant cũ.
- Event hợp lệ đã được lưu để đối soát có thể ACK 00; ACK này không phải `Success=true` cho Tenant. Retry 02, vẫn giữ RequiresReview và không áp dụng tiền vào quyền thuê.
- Tenant/Landlord chỉ đọc event thuộc target của mình; event không xác định order chỉ Admin thấy. Danh sách event có phân trang 30 bản ghi.
- Admin ghi kết quả/bằng chứng đối soát, có RowVersion và kiểm tra trạng thái; các event IPN/Return cùng giao dịch được cập nhật kết quả đồng thời. Callback từ nguồn khác đến sau khi đã đối soát kế thừa kết quả, không tự áp dụng lại.
- Chặn tạo lượt thanh toán mới khi target còn event RequiresReview. Không chuyển khoản chưa phân bổ thành doanh thu thuê hoặc quyền thuê.
- Có thông báo cập nhật hoàn cọc và thông báo Tenant/Admin khi event cần đối soát; lỗi gửi thông báo không rollback kết quả tài chính đã commit.

## 3. File thay đổi

### Backend và SQL

| File | Mục đích |
|---|---|
| [PaymentSafety.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Models/PaymentSafety.cs) | Hai entity và mã trạng thái mới; không đổi enum tài chính cũ |
| [PaymentSafetyConfiguration.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DAL/Configurations/PaymentSafetyConfiguration.cs) | Đúng 2 bảng, FK RESTRICT, check/index, RowVersion |
| [ApplicationDbContext.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DAL/ApplicationDbContext.cs) | Đăng ký 2 DbSet |
| [DepositRefundBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/DepositRefundBLL.cs) | Hoàn cọc, ownership, chứng từ, transaction, bảo vệ quyền khác |
| [PaymentEventBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/PaymentEventBLL.cs) | Danh sách theo ownership, Admin đối soát |
| [PaymentSafetySchema.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/PaymentSafetySchema.cs) | Báo 503 rõ ràng cho chức năng cần SQL 08 khi chưa nâng cấp, không âm thầm bỏ qua lưu vết |
| [PaymentBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/PaymentBLL.cs) và [IPaymentService.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/Interfaces/IPaymentService.cs) | Ledger callback, phân biệt nguồn, giữ entitlement, chặn retry chưa đối soát |
| [PaymentController.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Controllers/PaymentController.cs) | Return có requiresReview/eventState; giữ IPN contract |
| [PaymentSafetyController.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Controllers/PaymentSafetyController.cs) | API hoàn cọc và event/đối soát, role + account claim hợp lệ |
| [PaymentSafetyDto.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DTO/Payment/PaymentSafetyDto.cs) và [PaymentDto.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DTO/Payment/PaymentDto.cs) | Payload/validation/RowVersion, trạng thái kết quả |
| [RoomBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/RoomBLL.cs) | Bảo vệ thêm RefundRequested khi đổi trạng thái thủ công |
| [Program.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Program.cs) | Đăng ký 2 BLL vào DI |
| [08_PaymentSafety.sql](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/SQL/08_PaymentSafety.sql) | Nâng cấp additive, idempotent, kiểm tra schema, explicit opt-in cho database thật |

`RentalRequestBLL.HuyAsync` hiện đã chặn mọi Deposit không còn Pending, nên RefundRequested được bảo vệ sẵn; không sửa lại nhánh đã đúng đó. Không thay đổi contract/bill schema hoặc quy trình kết thúc hợp đồng.

### Frontend

- [paymentSafetyService.ts](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/services/paymentSafetyService.ts): API thật, không mock.
- [PaymentSafetyPanel.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/payment/PaymentSafetyPanel.tsx), [PaymentSafety.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/payment/PaymentSafety.css): panel dùng chung theo vai trò, modal giữ input khi lỗi, phân trang event.
- [TenantRentalsPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/tenant/TenantRentalsPage.tsx): panel trong tab Yêu cầu & Cọc; chỉ đề nghị cọc chưa có contract.
- [LandlordContractsPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/landlord/LandlordContractsPage.tsx): duyệt/từ chối/báo hoàn trong phần cọc, hiển thị rõ RefundRequested/Refunded.
- [PaymentResultPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/payment/PaymentResultPage.tsx): phân biệt cần đối soát, đã đối soát và thực sự áp dụng thành công.
- [AppRoutes.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/routes/AppRoutes.tsx), [AdminDashboardLayout.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/layouts/AdminDashboardLayout.tsx): mục `/admin/payments`, không sửa module Admin khác.

### Tests

- [PaymentSafetyTests.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd.Tests/PaymentSafetyTests.cs): refund, ledger, quyền, ngày UTC, merchant, SQL concurrency/unique/immutable.
- [PaymentIpnTests.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd.Tests/PaymentIpnTests.cs): mở rộng expected behavior: callback muộn được ACK sau khi đã ghi event, không phải chấp nhận thanh toán/quyền thuê.
- [WorkflowRegressionTests.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd.Tests/WorkflowRegressionTests.cs): fixture callback có TmnCode theo validation thực tế; không thay đổi mục tiêu regression.

## 4. Kiểm thử đã chạy

| Kiểm tra | Kết quả |
|---|---|
| Backend full suite với SQL QA riêng | **278 PASS, 0 FAIL, 0 SKIP** |
| Backend Release build | **PASS**, 0 warning / 0 error |
| Frontend lint / production build | **PASS** |
| HTTP/UI workflow thật trên database QA | **25/25 PASS** |
| Responsive | Tenant/Landlord/Admin và trang cần đối soát ở **375px, 768px, 1440px** PASS; đã mở ảnh chụp kiểm tra |
| SQL 08 trên database QA cũ chưa có 2 bảng | Tạo đúng 2 bảng PASS |
| Chạy lại SQL 08 | PASS, idempotent; count/checksum dữ liệu DatCoc/GiaoDichThanhToan cũ không đổi |
| SQL đồng thời | Tạo refund chỉ một người thắng; xác nhận hoàn hai lần chỉ hoàn tất một lần; callback đồng thời 00/02, một receipt |
| Financial history | Succeeded payment/transaction/PaidAt gốc giữ nguyên sau refund; không cấp lại quyền khi callback muộn |
| Git diff --check | PASS, chỉ cảnh báo CRLF Windows |
| DB thật | Metadata đọc xác nhận **0/2 bảng mới**; chưa áp dụng nâng cấp |
| Giao dịch VNPay/ngân hàng/refund thật | **NOT VERIFIED**, không thực hiện |

E2E kiểm tra Tenant tạo đề nghị → Landlord duyệt → lỗi RowVersion giữ input chứng từ → báo hoàn → Tenant nhận đủ → Available; từ chối/rút trả đúng Confirmed; hủy request chưa trả rồi cấp phòng cho Tenant khác → callback cũ được lưu RequiresReview → Admin ghi bằng chứng mà không đổi quyền của Tenant mới. Không gọi đây là full bank E2E hoặc full contract handover/settlement.

Trong E2E lần đầu phát hiện route parameter `action` của API mới trùng tên dành riêng MVC, làm HTTP 404 dù unit BLL PASS. Đã đổi thành `operation`, thêm regression guard route và chạy lại đầy đủ 25/25 trên bản build sửa. Lỗi lint effect ở component mới cũng đã sửa theo pattern hiện có rồi chạy lint/build lại. Không còn lỗi test/build/lint được ghi nhận trong bộ kiểm thử cuối.

Bằng chứng: [final-all.trx](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/final-all.trx), [HTTP/UI JSON](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/http-ui-results.json), [SQL upgrade JSON](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/sql-upgrade-results.json), [metadata DB thật](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/main-schema-check.json).

Ảnh: [modal lỗi 375px giữ chứng từ](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/refund-error-375.png), [Admin 375px](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/panel-admin-375.png), [cảnh báo đối soát 375px](C:/Users/huanp/AppData/Local/Temp/payment-safety-verification/review-result-375.png).

## 5. Áp dụng vào database đang dùng

Script: [08_PaymentSafety.sql](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/SQL/08_PaymentSafety.sql).

1. Backup database và chọn đúng `RoomRentalDB` trong SQL Server Management Studio.
2. Đọc script; đổi duy nhất `DECLARE @AllowMainDatabase bit = 0;` thành `... = 1;` để chủ động cho phép chạy trên DB thật.
3. Chạy toàn bộ script trong một lần. Script không có UPDATE/DELETE/DROP dữ liệu cũ; nếu schema không khớp hoặc có một bảng được tạo dở sẽ dừng, không tự sửa phá dữ liệu.
4. Khởi động lại backend sau khi SQL thành công, rồi tải lại frontend.
5. Tenant: tab **Yêu cầu & Cọc**; Landlord: **Quản lý cọc**; Admin: **Đối soát thanh toán**.

Khi chưa chạy SQL 08, chức năng mới và tạo URL thanh toán sẽ báo cần nâng cấp SQL thay vì cho thanh toán mà không lưu được event. Các bảng mới khởi đầu rỗng; không tạo refund giả cho cọc cũ, không coi thiếu event là chưa từng thanh toán. Script 08 độc lập với SQL 07 của module Roommate, không tự thực hiện SQL 07 hoặc thay đổi module đó.

## 6. Preview QA và giới hạn còn lại

Bản QA tách riêng: [frontend](http://127.0.0.1:3000), [Swagger QA](http://127.0.0.1:5095/swagger). Đây không phải RoomRentalDB; dữ liệu có nhãn QA. Tài khoản test của database này: `tenant@safety.test`, `owner@safety.test`, `admin@safety.test`; mật khẩu test `SafetyOnly-2026!`. Không sử dụng các tài khoản/mật khẩu test cho production.

- Chưa chạy SQL 08 trên database thật; đây là bước triển khai người dùng cần thực hiện sau backup/kiểm tra script.
- Hoàn sau contract, khấu trừ và bàn giao/công nợ vẫn thuộc Giai đoạn 3. Các contract cũ không bị thay đổi.
- Manual reconciliation chỉ ghi nhận kết quả xử lý ngoài hệ thống, không gọi ngân hàng/refund API. Không được đánh dấu đã đối soát khi chưa có bằng chứng thực tế.
- Thời gian event mới là UTC; không backfill/chuyển đổi các deadline DateTime lịch sử. Cần đối chiếu các nguồn deadline cũ nếu triển khai sang host khác timezone; bản sửa này chọn lưu review khi không đủ điều kiện, không tự khôi phục quyền thuê từ thời gian callback.
- Return và IPN còn dùng chung service xử lý có xác thực chữ ký nhưng khác Source; chưa chuyển Return thành read-only hoặc thêm querydr tự động.
- Không chứng nhận độ sẵn sàng production của gateway, thanh toán ngân hàng hay refund thật. **VNPay real sandbox bank transaction vẫn NOT VERIFIED.**
