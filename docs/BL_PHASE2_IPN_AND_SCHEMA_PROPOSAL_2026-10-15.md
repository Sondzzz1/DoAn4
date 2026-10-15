# Giai đoạn 2: IPN và đề xuất lưu vết thanh toán/hoàn cọc

> Đây là báo cáo trước phê duyệt. Sau khi người dùng đồng ý hai bảng và phương án, phần triển khai/kiểm thử mới được ghi ở [báo cáo hoàn thiện](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/BL_PHASE2_COMPLETION_2026-10-15.md). Các mục OPEN dưới đây phản ánh thời điểm lập đề xuất.

Ngày kiểm tra: 15/10/2026. Chỉ triển khai phần Giai đoạn 2 không cần thay đổi schema. Chưa sửa Giai đoạn 3/4. Không commit/push, không migration, không ghi dữ liệu thật, không gọi ngân hàng/hoàn tiền thật.

## 1. Kết quả

| Vấn đề | Kết quả | Bằng chứng |
|---|---|---|
| BL-004 phản hồi IPN | **FIXED phần giao thức và idempotency đã kiểm thử** | Phân loại 00/01/02/04/97/99; đúng tên JSON `RspCode`, `Message`; callback thành công/thất bại hợp lệ và lặp được xử lý riêng; không xác nhận giả cho callback mâu thuẫn/chưa xử lý |
| Kết quả trình duyệt hiển thị thất bại dù thanh toán thành công | **FIXED** | Backend từng gửi `True`/`False`, frontend chỉ nhận `true`; hai regression tests tái hiện thất bại trước sửa, sau sửa và HTTP/UI PASS |
| BL-002 hoàn/hủy cọc đã thanh toán | **OPEN, chờ duyệt schema/policy** | Deposit chỉ có số tiền/trạng thái/deadline/PaidAt, chưa lưu ai yêu cầu/duyệt, số tiền hoàn, chứng từ và xác nhận nhận tiền. Không nối tắt Paid/Confirmed → Cancelled/Available |
| BL-003 callback thành công muộn | **OPEN, chờ duyệt schema** | Attempt quá hạn hoặc target hết hiệu lực chưa có nơi lưu bền vững sự kiện nhận tiền/đối soát. Hiện trả 99 cho nhánh không áp dụng được, KHÔNG trả 00/02 giả; nhưng retry hữu hạn không thay thế được sổ sự kiện |
| VNPay real sandbox bank transaction / refund API | **NOT VERIFIED** | Tất cả callback test tự ký bằng secret QA local; không mở URL thanh toán của cổng, không xác thực thẻ/ngân hàng, không gọi refund/querydr thật |

BL-004 không đồng nghĩa toàn bộ module thanh toán đã an toàn với mọi callback muộn. BL-003 phải được hoàn thiện tiếp sau khi được phép bổ sung lưu vết. ReturnURL hiện vẫn dùng service xử lý chung như source ban đầu; chưa chuyển sang mô hình Return chỉ đọc/IPN là nguồn quyết định duy nhất trong bước sửa này.

## 2. Ảnh hưởng dữ liệu thật: đã kiểm kê CHỈ ĐỌC

Đã đọc aggregate và metadata từ SQL local `RoomRentalDB`, không đọc/xuất danh tính khách hàng, không sửa/xóa dữ liệu:

| Dữ liệu | Số lượng tại thời điểm kiểm tra |
|---|---:|
| Room Available / Reserved / TemporarilyUnavailable | 22 / 2 / 1 |
| Request Approved / Cancelled / ConvertedToContract | 2 / 1 / 1 |
| Deposit Pending / Confirmed | 1 / 1 |
| Payment Succeeded | 2 |
| Contract Terminated | 1 |
| Reserved không có Approved request hoặc effective contract | 0 |
| Attempt Expired | 0 |
| Deposit Pending đã quá deadline | 0 |

Đối chiếu kỹ nhóm cọc Confirmed không có effective contract: cọc này thuộc request ConvertedToContract và contract Terminated. Đây là lịch sử sau kết thúc hợp đồng, **không đủ bằng chứng coi nó là cọc đang làm phòng bị kẹt**. Không backfill Refunded, không đổi trạng thái Room hoặc Payment từ nhận định tĩnh.

Bằng chứng tổng hợp: [readonly-inventory.json](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/readonly-inventory.json), [readonly-cohorts.json](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/readonly-cohorts.json). Các con số là snapshot kiểm tra, không phải dữ liệu seed mới hay bảo đảm dữ liệu không bị người dùng khác thay đổi sau đó.

### Quy tắc bảo vệ dữ liệu khi sửa tiếp

- Giữ nguyên mã và lịch sử trạng thái Room/Request/Deposit/Payment/Contract hiện tại.
- Không tự coi cọc Confirmed trong contract đã kết thúc là đã hoàn tiền.
- Không tự mở lại quyền thuê khi nhận callback sau deadline/hủy request/phòng đã cấp Tenant khác.
- Không xóa attempt thất bại/hết hạn để "sửa" báo cáo. Callback nhận tiền thực tế phải có bản ghi đối soát riêng.
- Sự kiện tiền đã nhận nhưng chưa phân bổ không tự trở thành doanh thu thuê hoặc quyền giữ phòng.
- Không dùng database thật để test mutation. Các test trong lần này dùng InMemory hoặc `RoomRental_Verification_BLPhase2_20261015_01`, `_02` và các hậu tố QA.

## 3. File đã sửa và logic trước/sau

| File | Thay đổi | Giới hạn |
|---|---|---|
| [PaymentBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/PaymentBLL.cs:125) | Phân loại kết quả callback; kiểm tra status provider đầy đủ/nhất quán; callback trùng không ghi lại; không tái dùng mã transaction sang order khác; failure không có transaction/transaction `0` lưu null | Không thêm quy trình hoàn tiền/đối soát mới, không sửa CreatePayment amount/ownership |
| [PaymentController.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Controllers/PaymentController.cs:55) | IPN map kết quả xử lý, không suy ra từ `Success`; ReturnURL xuất boolean chữ thường | Giữ route/API auth hiện tại; không sửa frontend |
| [PaymentDto.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DTO/Payment/PaymentDto.cs:1) | Thêm metadata callback có JsonIgnore; DTO IPN giữ đúng tên JSON theo provider | Metadata không phải entity/cột DB; không đổi enum PaymentTransactionStatus |
| [PaymentIpnTests.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd.Tests/PaymentIpnTests.cs:1) | 28 InMemory cases + 1 SQL integration case | Không gọi provider; SQL chỉ chạy với tên DB QA có guard |
| Tài liệu này | Kết quả và schema đề xuất để xin duyệt | Không phải migration đã chạy |

Các thay đổi BL-001 có sẵn được giữ nguyên. Trong phiên có commit `3fc2511` từ bên ngoài ghi nhận Giai đoạn 1; trợ lý không thực hiện commit/push hay reset/revert commit đó.

### Phản hồi IPN sau sửa

| Tình huống | Trước | Sau |
|---|---|---|
| Chữ ký sai | 01 | 97 |
| Order không tồn tại | 01 | 01 |
| Sai/thiếu/không parse được amount | 01 | 04 |
| Callback thành công ghi DB lần đầu | 00 | 00 |
| Callback thất bại hợp lệ được ghi DB | 01 vì `Success=false` | 00: xác nhận đã ghi nhận kết quả thất bại, KHÔNG coi khách đã trả tiền |
| Callback trùng đã xử lý | Thành công vẫn 00, thất bại 01 | 02, không ghi lại ProcessedAt/paid state |
| Attempt hết hạn chưa ghi nhận tiền thành công | 01 | 99; chưa giải quyết BL-003, không báo đã xử lý thành công |
| Callback thiếu/sai status hoặc hai mã thành công mâu thuẫn | Có thể coi thành công khi thiếu TransactionStatus | 99, không áp dụng vào Deposit/Bill |
| Phản hồi khác với giao dịch đã ghi nhận | Có thể trả success từ trạng thái cũ mà không phân biệt | 99, giữ lịch sử cũ; cần lưu sự kiện đối soát ở bước sau |
| JSON với global camelCase | Có nguy cơ thành `rspCode`/`message` | Chính xác `RspCode`/`Message` |
| Return thành công | `success=True`, frontend không nhận | `success=true`, frontend hiện đúng |

Theo [tài liệu PAY chính thức VNPAY](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html), IPN 00/02 báo đã xử lý, còn các mã lỗi khác kích hoạt retry. Vì vậy ghi nhận một giao dịch thất bại hợp lệ vẫn cần ACK thành công; ACK không phải kết quả chuyển tiền. Đối chiếu tài liệu ngày 15/10/2026, không phải chứng nhận tích hợp ngân hàng.

Mã transaction `0`/rỗng của callback thất bại không được chiếm unique index như một transaction thực. Dữ liệu failed cũ lưu `0` vẫn đọc và ACK được mà không rewrite lịch sử. Thành công không có transaction hợp lệ bị từ chối.

## 4. Kiểm thử thực chạy

| Kiểm tra | Kết quả |
|---|---|
| Nhóm 14 IPN regression case ban đầu, trước sửa | 12 FAIL, 2 PASS: tái hiện lỗi |
| Nhóm IPN/Workflow sau sửa bước đầu | 25/25 PASS |
| Hai test Return boolean, trước sửa định dạng | 2 FAIL, tái hiện `True`/`False` |
| Full backend cuối cùng, có SQL QA riêng | **252 PASS, 0 FAIL, 0 SKIP** |
| SQL concurrent IPN | Một callback 00, một callback 02; một lần áp dụng payment, Room vẫn Reserved |
| SQL failure transaction `0` trên hai order | Cả hai lưu failure không xung đột unique index; retry 02; không đổi cọc đã Paid của order thành công trước |
| HTTP dùng API/DB QA thật và callback tự ký local | **15/15 PASS** |
| Playwright trang kết quả thanh toán hiện có | **6/6 PASS**, success/failure tại 1440px, 768px, 375px; ảnh đã mở kiểm tra |
| Tổng HTTP/UI | **21/21 PASS** |
| Backend Release build | **PASS**, 0 warning, 0 error; output temp riêng để không dừng server đang chạy |
| Frontend lint/build | **PASS**, không sửa file frontend |
| Git diff --check | **PASS**, chỉ cảnh báo CRLF của môi trường Windows |
| VNPay bank/refund thực tế, full E2E hoàn cọc | **NOT VERIFIED / chưa triển khai** |

HTTP đã kiểm tra: JSON exact case, signature/unknown order/amount/status; duplicate đồng thời; cọc Paid/Confirmed giữ Reserved; cấm cancel cọc đã trả theo policy hiện có; callback mâu thuẫn; Return true/false; hai failure mã `0`; hủy yêu cầu chưa trả chỉ giải phóng đúng reservation của nó.

Bằng chứng:
- [ipn-before.trx](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/ipn-before.trx), [return-before.trx](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/return-before.trx).
- [phase2-final.trx](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/phase2-final.trx), [http-ui-results.json](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/http-ui-results.json).
- [Success 375px](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/payment-success-375.png), [failure 768px](C:/Users/huanp/AppData/Local/Temp/bl-phase2-verification/payment-failure-768.png).

Chạy lại full tests cần `ROOM_RENTAL_TEST_SQL` trỏ database QA **mới** có prefix `RoomRental_Verification_`; không tái sử dụng fixture đã tồn tại, không trỏ vào RoomRentalDB.

## 5. Schema đề xuất: CHƯA TẠO, CHƯA MIGRATION

Đề xuất tối thiểu **hai bảng mới**, không thêm/đổi/xóa cột của bảng hiện có. Tên cột mới theo English như GiaoDichThanhToan; thời gian mới thống nhất UTC `datetime2(7)`. FK tài khoản trỏ `TaiKhoan.Id`, không cascade delete.

### A. SuKienThanhToan: lưu callback/đối soát bền vững

Lý do: không dùng `OrderInfo` hoặc log text để nhét dữ liệu đối soát; các callback muộn/mâu thuẫn phải tồn tại dù chưa thể áp dụng vào quyền thuê/hóa đơn. Không lưu JWT/hash secret/toàn bộ query chứa dữ liệu nhạy cảm.

| Cột | SQL type / null / default | Ý nghĩa |
|---|---|---|
| Id | bigint IDENTITY, PK | Sự kiện |
| PaymentTransactionId | int NULL, FK GiaoDichThanhToan.Id, RESTRICT | Nullable để giữ callback có chữ ký hợp lệ nhưng chưa tìm thấy attempt |
| ProviderOrderId | nvarchar(100) NOT NULL | TxnRef provider gửi |
| ProviderTransactionCode | nvarchar(100) NULL | Mã provider thật; `0`/rỗng là NULL |
| DeduplicationKey | char(64) NOT NULL, UNIQUE | SHA-256 từ các trường chuẩn hóa + nguồn; retry cùng nguồn không tạo bản ghi trùng |
| Source | tinyint NOT NULL | 0=IPN, 1=Return; mã Query/Manual chỉ mở rộng nếu được chốt riêng |
| ReportedAmount | decimal(18,2) NOT NULL | Tiền provider báo, tách số tiền kỳ vọng của attempt |
| ResponseCode | char(2) NOT NULL | Kết quả provider |
| ProviderStatus | char(2) NOT NULL | TransactionStatus, không suy từ ACK |
| ProviderPaidAtUtc | datetime2(7) NULL | PayDate được chuyển từ GMT+7 sang UTC; NULL nếu không có/hợp lệ |
| ReceivedAtUtc | datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME() | Thời điểm nhận |
| ProcessingState | tinyint NOT NULL DEFAULT 0 | 0=Recorded, 1=Applied, 2=RequiresReview, 3=Resolved |
| Reason | nvarchar(500) NULL | Vì sao không áp dụng/đưa đối soát |
| ResolvedByAccountId | int NULL, FK TaiKhoan.Id, RESTRICT | Người đối soát |
| ResolvedAtUtc | datetime2(7) NULL | Mốc xử lý |
| ResolutionNote | nvarchar(1000) NULL | Kết quả và tham chiếu xử lý thủ công |
| RowVersion | rowversion NOT NULL | Chống ghi đè khi đối soát đồng thời |

Index đề xuất: UNIQUE DeduplicationKey; index PaymentTransactionId; index (ProcessingState, ReceivedAtUtc). Check constraints: Source trong 0..1, ProcessingState trong 0..3, ReportedAmount >= 0. Không đặt unique toàn cục ProviderTransactionCode ở bảng sự kiện vì IPN và Return có thể cùng ghi nhận một giao dịch; uniqueness payment áp dụng vẫn do GiaoDichThanhToan + workflow lock kiểm soát.

Chỉ lưu sự kiện sau khi xác thực nguồn/chữ ký và validate định dạng/giới hạn các trường; dữ liệu báo số tiền khác kỳ vọng phải giữ nguyên số tiền báo để đối soát, không đổi Amount cũ cho khớp. Sự kiện xác nhận tiền đến muộn không tự mở lại request hoặc chuyển Room về Reserved/Rented. Cần DTO/UI cảnh báo đối soát thay vì báo chung là chưa thanh toán.

### B. YeuCauHoanCoc: lưu đề nghị và bằng chứng hoàn cọc

Phạm vi đề xuất cho Giai đoạn 2: hoàn **đủ tiền**, thủ công, trước khi có contract hiệu lực. Hoàn sau hợp đồng, khấu trừ, công nợ/bàn giao để Giai đoạn 3 chốt; không tự gọi API hoàn VNPay.

| Cột | SQL type / null / default | Ý nghĩa |
|---|---|---|
| Id | int IDENTITY, PK | Yêu cầu hoàn |
| DepositId | int NOT NULL, FK DatCoc.Id, RESTRICT | Cọc liên quan |
| Amount | decimal(18,2) NOT NULL | Tiền đề nghị hoàn; policy giai đoạn này = toàn bộ cọc hợp lệ |
| PreviousDepositStatus | int NOT NULL | Paid hoặc Confirmed trước khi chuyển RefundRequested, phục hồi đúng khi bị từ chối/rút |
| Status | tinyint NOT NULL DEFAULT 0 | 0=Requested, 1=Approved, 2=RefundReported, 3=Completed, 4=Rejected, 5=Cancelled |
| Reason | nvarchar(500) NOT NULL | Lý do đề nghị |
| RequestedByAccountId | int NOT NULL, FK TaiKhoan.Id, RESTRICT | Tenant của cọc |
| RequestedAtUtc | datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME() | Mốc yêu cầu |
| ReviewedByAccountId | int NULL, FK TaiKhoan.Id, RESTRICT | Landlord của cọc duyệt/từ chối |
| ReviewedAtUtc | datetime2(7) NULL | Mốc quyết định |
| DecisionNote | nvarchar(1000) NULL | Lý do từ chối/ghi chú xử lý |
| RefundReference | nvarchar(200) NULL | Chứng từ/mã chuyển tiền thủ công; không thay thế giao dịch refund thật |
| RefundReportedByAccountId | int NULL, FK TaiKhoan.Id, RESTRICT | Landlord báo đã hoàn |
| RefundReportedAtUtc | datetime2(7) NULL | Mốc báo hoàn |
| CompletedByAccountId | int NULL, FK TaiKhoan.Id, RESTRICT | Tenant xác nhận nhận lại; Admin chỉ xử lý tranh chấp có bằng chứng |
| CompletedAtUtc | datetime2(7) NULL | Mốc hoàn tất |
| RowVersion | rowversion NOT NULL | Chống xử lý trùng/ghi đè |

Index đề xuất: UNIQUE DepositId WHERE Status IN (0,1,2); index (RequestedByAccountId, Status); index (ReviewedByAccountId, Status). Check constraints: Amount > 0; PreviousDepositStatus IN (1,2); Status trong 0..5; RefundReported/Completed yêu cầu có RefundReference và mốc/người phù hợp. Một Completed phải chặn mở yêu cầu mới ở BLL vì Deposit đã Refunded.

Workflow đề xuất, CHƯA triển khai:
1. Tenant gửi đề nghị với lý do; đúng owner, cọc Paid/Confirmed, không có effective contract. Không cho dùng luồng này để bỏ qua bàn giao/nợ của contract đã kết thúc: cọc đã gắn contract phải theo giai đoạn 3.
2. Landlord duyệt hoặc từ chối; không sửa ghi chú Tenant hay số tiền payment gốc. Deposit chuyển RefundRequested khi lập yêu cầu; không được dùng để tạo contract trong lúc chờ hoàn.
3. Landlord hoàn tiền ngoài hệ thống và báo chứng từ; chưa đổi Deposit thành Refunded chỉ từ lời báo của Landlord.
4. Tenant xác nhận đã nhận (hoặc Admin đối soát có bằng chứng); transaction cập nhật yêu cầu Completed, Deposit Refunded, Request Cancelled. Chỉ mở Room nếu không còn reservation/effective contract/quyền khác; xử lý đồng thời với callback/worker bằng workflow lock.
5. Từ chối hoặc Tenant rút trước bước báo hoàn: khôi phục PreviousDepositStatus, giữ quyền giữ chỗ hiện hành; không đánh mất PaidAt/history.

### Tương thích và rủi ro migration cần duyệt

- Migration additive: chỉ tạo hai bảng, index và constraint mới; không DROP/ALTER cột cũ, không thay mã enum, không tự backfill trạng thái hay tạo refund lịch sử giả.
- Bảng mới bắt đầu rỗng. Cọc/payment cũ vẫn đọc từ model cũ; không có event không đồng nghĩa chưa từng nhận tiền. Những bất nhất lịch sử cần báo cáo/đối soát riêng.
- Phải có script kiểm tra tồn tại/đúng schema, transaction và backup trước khi người dùng chạy vào database thật; thử trên DB QA trước. Chưa viết/chạy DDL trong phiên này.
- Không có quy trình downgrade tự động xóa bảng chứa sự kiện tiền/hoàn cọc. Không được xóa dữ liệu tài chính để rollback ứng dụng.
- Cần cập nhật query/DTO và UI đúng phạm vi cọc/đối soát sau khi duyệt, không chỉ thêm bảng rồi để dữ liệu không được sử dụng.

## 6. Còn OPEN và điều kiện tiếp tục

| Việc | Điều kiện / test cần có |
|---|---|
| BL-002 | Duyệt schema + policy hoàn thủ công đủ tiền trước contract; tests owner/role, duplicate, approve/reject/withdraw, chứng từ, Tenant/Admin xác nhận, không giải phóng khi còn quyền khác |
| BL-003 | Duyệt bảng sự kiện; tests callback sau 30 phút/deadline/cancel, tiền đến sau retry khác, amount mismatch, không cấp lại quyền, event idempotent, manual review/audit |
| Contract refund/bàn giao/công nợ | Giai đoạn 3, không tự kéo vào bước này |
| Merchant sandbox/real bank transaction | Có cấu hình hợp lệ và test riêng thực sự; hiện NOT VERIFIED |

Không tự chạy migration hay tiếp tục sửa BL-002/003 khi chưa được xác nhận. Không commit/push. Cần khởi động lại backend sử dụng source mới để nhận bản sửa IPN/Return; các server đang chạy phiên bản cũ không tự nạp DLL mới.
