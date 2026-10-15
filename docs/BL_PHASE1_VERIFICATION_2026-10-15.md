# Kiểm tra nghiệp vụ: Giai đoạn 1, ngày 15/10/2026

## Phạm vi và mức độ xác minh

Chỉ sửa BL-001. BL-002 đến BL-025 chỉ đọc code, đối chiếu kiểm thử sẵn có, tái hiện một số tình huống trên QA và lập kế hoạch. Không triển khai giai đoạn 2/3/4, không gọi thanh toán/hoàn tiền thật, không thực hiện commit/push.

Tài liệu yêu cầu cung cấp chủ đề của 24 mã; không có nội dung BL-011 và không đính kèm toàn bộ báo cáo Claude. Vì vậy đây là kết quả xác minh các chủ đề được cung cấp, không khẳng định đã đối chiếu từng nhận định chi tiết trong báo cáo Claude gốc.

Ký hiệu bằng chứng:
- **Runtime**: đã gọi HTTP hoặc chạy test trong lần này.
- **Code**: đã xác minh nhánh xử lý trong source, chưa tái hiện riêng mọi tình huống.
- **Chưa đủ bằng chứng**: không coi là lỗi đã xác nhận.
- **Thiếu nghiệp vụ**: phần chưa có để đáp ứng quy trình thực tế, không đồng nghĩa mọi dữ liệu hiện tại đều sai.

## 1. Kết quả xác minh

| Mã | Có tồn tại? / Kết quả | Bằng chứng chính | Mức độ |
|---|---|---|---|
| BL-001 | **Có, đã sửa** | Runtime: 19/51 test mới thất bại trước sửa. DTO nhầm `2` thành tạm ngưng, `3`/rỗng/sai thành Available. Trạng thái Rented không có contract và trạng thái Room lệch với reservation cũng chưa được bảo vệ đầy đủ. Sau sửa kiểm thử PASS. | Cao |
| BL-002 | Thiếu quy trình hoàn/hủy cọc đã trả | Code: `DepositBLL.CapNhatTrangThaiAsync` chỉ Paid → Confirmed; `RentalRequestBLL.HuyAsync` chặn Paid/Confirmed; worker chỉ hết hạn Pending. Các hằng RefundRequested/Refunded chưa tạo thành workflow hoàn cọc. Chưa chạy hoàn tiền. | Cao, OPEN |
| BL-003 | Có rủi ro mất ghi nhận callback muộn | Code: `PaymentBLL.ProcessPaymentReturnAsync` gọi `ExpireAttemptsAsync` trước kiểm tra trạng thái; attempt sau 30 phút bị Expired và thoát trước lưu mã giao dịch thành công. `ValidateTargetForSuccess` từ chối cọc hết hạn nhưng chưa có nhánh đối soát riêng. Test callback thành công đúng hạn/lặp đã PASS, không chứng minh callback muộn an toàn. | Cao, OPEN |
| BL-004 | **Có**, phản hồi IPN chưa phân loại đúng | Runtime: chữ ký sai trả `RspCode=01`, tài liệu PAY quy định `97`; controller gộp các thất bại thành `01`, thành công lặp vẫn `00` thay vì phân biệt `02`. Không sửa trong lần này. | Cao, OPEN |
| BL-005 | Thiếu tất toán/bàn giao khi kết thúc | Code: `RentalContractBLL.ChamDutAsync` đặt Terminated, gửi lý do qua thông báo nhưng không lưu lý do riêng; mở phòng khi không có quyền thuê khác, chưa yêu cầu bàn giao/nợ/hoàn cọc. Không khẳng định thay đổi này là sửa nhỏ. | Cao, OPEN |
| BL-006 | Không xác nhận là lỗi giá hợp đồng | Code + tests: `TaoAsync` lưu giá thương lượng từ DTO vào MonthlyRent; hai bên xác nhận; Bill/PDF dùng snapshot tài chính của contract. Không tự cấm giá khác Room.Price. | Không coi là bug |
| BL-007 | Đã bảo vệ một phần; còn phụ thuộc snapshot | Runtime tests: sửa điều kiện công khai hủy request Pending, chặn request Approved; contract đã chuyển đổi giữ nguyên giá/phí. Code: sau ConvertedToContract vẫn có thể sửa nội dung Room; PDF còn đọc địa chỉ/tên/diện tích hiện tại. Cần xử lý cùng BL-015, không cấm mọi sửa Room. | Trung bình/cao, PARTIAL |
| BL-008 | Không còn đúng nếu cho rằng số người luôn không được quản lý | Code + Roommate tests: `ContractOccupancyPolicy.CountAsync` tính người thuê chính của Active contract + occupant Active thuộc Active contract; cache được đồng bộ trong lifecycle; chặn giảm MaxOccupants dưới số ở thực tế. SQL tranh chấp chỗ cuối đã PASS. Không có thuê từng giường. | Đã có bảo vệ; giới hạn mô hình |
| BL-009 | Có lý do từ chối; thiếu thông báo kiểm duyệt | Code: `AdminBLL.RejectPostAsync` bắt buộc/lưu RejectionReason; Approve/Reject/Hide chưa gọi notification service. Hide là ngừng quảng cáo, không tự hủy hợp đồng/cọc. Không kết luận thiếu lý do ở mọi thao tác là lỗi đã xác nhận. | Trung bình, PARTIAL |
| BL-010 | **Có** | Runtime QA: khóa Landlord, public detail vẫn trả tin, Tenant vẫn gửi được request Pending. JWT của tài khoản bị khóa đã bị chặn đúng, nhưng query public/Tenant không kiểm tra trạng thái chủ tin. | Cao, OPEN |
| BL-011 | Không có nội dung trong đầu vào | Không tự đặt tên hay suy đoán mã lỗi còn thiếu. | Chưa đánh giá |
| BL-012 | Có chống trùng cơ bản, thiếu xử lý lịch quá hạn | Code: cấm lịch quá khứ khi tạo, cấm duplicate Pending/Confirmed theo Tenant/Post, kiểm tra trùng đúng timestamp của Tenant/Landlord; chỉ hoàn thành sau giờ hẹn. Chưa có thời lượng buổi xem, chưa tự đóng lịch Pending quá hạn; nhánh tạo chưa có khóa chống race như core rental. Các nhánh sẵn có đã có tests PASS, race lịch xem chưa chạy. | Trung bình, PARTIAL |
| BL-013 | **Có** | Runtime: sau từ chối, `GhiChu` đổi từ `TENANT ORIGINAL NOTE` thành `LANDLORD REJECTION REASON`. `RentalRequestBLL.CapNhatTrangThaiAsync` ghi đè `item.Note`. | Trung bình, OPEN |
| BL-014 | **Có**, GET có tác dụng ghi | Code: `RentalContractBLL.LayCuaToiAsync` và `LayChiTietAsync` gọi `ExpireEndedContractsAsync`, có SaveChanges và cập nhật contract/Room/Roommate. Worker đã tồn tại nên có thể tách trách nhiệm sau, nhưng phải bảo vệ đọc dữ liệu lifecycle trễ. | Trung bình, OPEN |
| BL-015 | Snapshot tài chính đúng; PDF chưa bất biến đầy đủ | Code: `ExportBLL.XuatHopDongPdfAsync` dùng MonthlyRent/DepositAmount/phí snapshot, font Lato; nhưng đọc User/Room/Post hiện tại, dùng ngày xuất DateTime.Now ở phần đầu, không đưa contract.Terms vào PDF, chỉ có cờ ký không có signed timestamp. Không render/đối chiếu PDF mới trong lần này. | Cao, PARTIAL |
| BL-016 | Kỳ cuối đã có bảo vệ; hóa đơn hủy chưa tái tạo được | Code + tests: đúng owner, kỳ giao thời gian thuê thực tế, không trùng tháng, Paid bất biến. AnyAsync trùng kỳ tính cả Cancelled. Đây là policy cần chốt cách thay thế bill, không xóa lịch sử để tạo lại. UTC UpdatedAt được DbContext ghi đè về UtcNow; không kết luận nhầm vì BLL gán DateTime.Now. | Trung bình, PARTIAL |
| BL-017 | **Có** nếu yêu cầu bài public phải đủ ảnh/mô tả | Runtime QA: Room không ảnh/mô tả vẫn tạo; Post nội dung rỗng vẫn được Admin duyệt. Giá/diện tích/số người/địa chỉ có validation sẵn. Cho lưu phòng nháp thiếu ảnh có thể hợp lý; nên chặn ở gửi duyệt/duyệt sau khi chốt checklist. | Trung bình, thiếu policy publication |
| BL-018 | **Có**, dữ liệu mặc định ngầm | Runtime: không gửi CategoryId/Area/MaxOccupants vẫn nhận `1/20/2`. `CreateRoomDto.GetCategoryId/GetMaxOccupants` và `RoomBLL.CreateRoomAsync` tự bù. Chưa sửa vì ngoài BL-001. | Trung bình, OPEN |
| BL-019 | Search/paging cơ bản PASS; còn hạn chế | Tests PASS: radius bỏ tọa độ thiếu, lọc trước count/paging, trang rỗng giữ total. Code: area/views sort thiếu tie-breaker Id; ViewCount tăng trên từng GET bằng read-modify-write, chưa chống refresh/race. Radius lấy ứng viên vào memory. Chưa load test, không khẳng định toàn bộ search sai. | Thấp/trung bình, PARTIAL |
| BL-020 | Có mẫu fallback đáng cải thiện, chưa tái hiện bypass | Code: nhiều controller parse claim rồi fallback `0`, trong khi một số BLL xem `0` là nội bộ/Admin. Tuy nhiên `AccountTokenValidation` bắt buộc UserId hợp lệ, account tồn tại/active, role khớp DB trước khi vào controller. Không có bằng chứng Tenant giả Admin hay truy cập chéo; ownership tests PASS. | Cần hardening; chưa xác nhận exploit |
| BL-021 | Validation có sẵn; thiếu giới hạn đăng nhập | Code: BCrypt, JWT kiểm chữ ký/expiry/account/role, role đăng ký chỉ Tenant/Landlord, DTO EmailAddress/Phone/StringLength. Program/Auth chưa cấu hình rate limiting/brute-force lockout. Không thực hiện tấn công hay tải lớn để đo. | Trung bình, hardening OPEN |
| BL-022 | Xóa phòng thực tế là tạm ngưng, không xóa lịch sử | Code + tests: DeleteRoom giữ dữ liệu, chặn reservation/effective contract; cùng kiểm tra manual status BL-001 để tránh bypass. HidePost không hủy quyền thuê hiện có là hành vi cần giữ. Chính sách xử lý request Pending/lịch xem khi ẩn hoặc tạm ngưng vẫn cần chốt; không triển khai BL-022 riêng. | PARTIAL, không destructive |
| BL-023 | Không xác nhận lỗi tách Room/Post như nhận định chung | Code + tests: Post chỉ tham chiếu Room tồn tại/đúng owner; Room là nguồn dữ liệu công khai; detail Approved có thể Reserved/Rented còn search chỉ Approved+Available là chủ đích. Giá contract là snapshot, giá Room là niêm yết; cần phân biệt nhãn khi mở rộng báo cáo, không ép mọi giá bằng nhau. | Bảo vệ cốt lõi PASS |
| BL-024 | Các nút trạng thái Room cốt lõi đúng | Runtime Playwright: nút tạm ngưng/đổi trạng thái disabled ở Rented/Reserved; payload thực gửi `3`/`0`; modal giữ khi API 409. Detail chặn lịch/request nếu không Available. Chưa duyệt mọi nút trên toàn bộ ứng dụng; thiếu UI hoàn cọc là phụ thuộc BL-002. | Đã kiểm tra phạm vi Room; phần còn lại PARTIAL |
| BL-025 | Có thiếu validation OrderInfo | Code: `CreatePaymentRequestDto.OrderInfo` không ràng buộc; BLL nhận `dto.OrderInfo ?? default`, chưa trim/chặn rỗng/độ dài/ký tự theo PAY. Amount/target lấy DB và ownership đã được bảo vệ, không phải client tự sửa số tiền. | Trung bình, OPEN |

### Điểm đọc code để đối chiếu

Các đường dẫn sau là source thực tế, không phải file mới được sửa toàn bộ:
- [UpdateRoomStatusDto.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DTO/Room/UpdateRoomStatusDto.cs:12): `GetResolvedStatus`.
- [RoomBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/RoomBLL.cs:269): `UpdateRoomStatusAsync`, `DeleteRoomAsync`, `EnsureManualStatusChangeAllowedAsync`; Create/Update/ValidateRoom trong cùng file.
- [RentalFeaturesBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/RentalFeaturesBLL.cs:106): Note bị ghi đè; trong cùng file có `HuyAsync`, DepositBLL, `TaoAsync`, GET contract, `ChamDutAsync`, reconcile lifecycle.
- [PaymentBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/PaymentBLL.cs:125): callback và expiry; [PaymentController.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Controllers/PaymentController.cs:81): IPN.
- [ExportBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/ExportBLL.cs:27): PDF; [MonthlyBillBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/MonthlyBillBLL.cs:27): kỳ/hóa đơn trùng/snapshot.
- [RoomPublicationPolicy.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/RoomPublicationPolicy.cs:36), [ContractOccupancyPolicy.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/ContractOccupancyPolicy.cs:24): quyền thuê và số người.
- [PostBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/PostBLL.cs:31): search, detail, create, hide; [AdminBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/AdminBLL.cs:201): khóa tài khoản, kiểm duyệt.
- [ViewingAppointmentBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/ViewingAppointmentBLL.cs:27): tạo/xác nhận/từ chối/hủy lịch.
- [AccountTokenValidation.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Services/AccountTokenValidation.cs:10), [AuthBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/AuthBLL.cs:120), [RentalManagementControllers.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/Controllers/RentalManagementControllers.cs:31): authentication/claims.

Đối chiếu BL-004 và BL-025 với [tài liệu PAY chính thức VNPAY](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html), đọc ngày 15/10/2026. PAY phân biệt mã IPN đã xác nhận, sai số tiền, sai chữ ký; OrderInfo có giới hạn và yêu cầu định dạng. Đây là đối chiếu tài liệu, không phải bằng chứng ngân hàng sandbox đã chạy thành công.

## 2. Những file chỉnh sửa trong lần này

| File | Thay đổi | Lý do |
|---|---|---|
| [UpdateRoomStatusDto.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/DTO/Room/UpdateRoomStatusDto.cs) | Parse chính xác 0/1/2/3; giữ tên/alias cũ hợp lệ; kiểm tra DesiredStatus; từ chối thiếu/sai/mâu thuẫn các alias | Không cho đầu vào sai âm thầm mở phòng |
| [RoomBLL.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd/BLL/RoomBLL.cs) | Chỉ manual Available ↔ TemporarilyUnavailable; guard dùng chung cho đổi trạng thái và Delete-as-suspend; chặn effective contract, Approved request, cọc Paid/Confirmed chưa chuyển sang contract | Bảo vệ quyền giữ chỗ/khoản tiền khi RoomStatus cũ bị lệch; không cho đi vòng qua DELETE |
| [RoomStatusTests.cs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/backend/RoomRental.BackEnd.Tests/RoomStatusTests.cs) | 51 test case DTO/BLL + 1 test tích hợp SQL Server | Tái hiện trước sửa và regression; kiểm chứng SQL thực sự lưu enum đúng |
| Tài liệu này | Bằng chứng, giới hạn kiểm thử và kế hoạch | Dừng đúng giai đoạn để người dùng kiểm tra |

Không sửa frontend vì enum, label, filter, payload và disable button đang đúng; lỗi nằm ở backend đọc payload. Không sửa Payment/Deposit/Contract/MonthlyBill ngoài việc đọc điều kiện để bảo vệ thao tác trạng thái phòng.

Đã ghi nhận git status trước khi sửa: có thay đổi module Roommate/backend/frontend và seed/docs từ công việc trước. Không revert chúng. Trong khi thực hiện xuất hiện commit `f8f3256` từ bên ngoài, bao gồm một phần test mới; không reset HEAD hoặc ghi đè commit đó. Trợ lý không chạy commit/push. Vì HEAD thay đổi giữa phiên, diff cuối không thể hiện toàn bộ các dòng test được thêm từ đầu phiên.

### Database

Không đổi enum/giá trị lịch sử; không migration, không ALTER/DROP/UPDATE database `RoomRentalDB`.

EF tạo schema hiện hành trong các database QA mới, có guard tên `RoomRental_Verification_`:
- `RoomRental_Verification_BL001_20261015_01`.
- Cùng tiền tố, hậu tố `_Roommates`, `_RoommatesLifecycle`, `_RoomStatus`.

HTTP/UI chỉ ghi dữ liệu kiểm thử ở database hậu tố `_RoomStatus`. Tài khoản QA bị khóa trong ca BL-010 đã được mở lại trong `finally`; Post thiếu ảnh/mô tả của ca audit đã được ẩn. Không tạo hoặc gọi giao dịch ngân hàng thật.

## 3. Logic trước và sau sửa BL-001

Luồng thực tế: `LandlordRoomManagementPage.handleStatusToggle` → `roomService.updateRoomStatus` gửi `{ trangThai: "3" }` → `RoomController.UpdateRoomStatus` → DTO resolve → `RoomBLL.UpdateRoomStatusAsync` → `PhongTro.TrangThai` (EF conversion int).

Frontend, enum backend và cột SQL thống nhất: Available=0; Rented=1; Reserved=2; TemporarilyUnavailable=3. Chỉ DTO bị lệch mapping.

| Đầu vào/trường hợp | Trước sửa | Sau sửa |
|---|---|---|
| Chủ trọ chọn tạm ngưng, gửi `"3"` | DTO fallback Available; thao tác báo thành công nhưng phòng vẫn mở | Lưu chính xác `3`, search không trả tin của phòng đó |
| Gửi `"2"` | Bị hiểu thành tạm ngưng | Resolve Reserved; BLL trả 400 vì không được đặt thủ công |
| Gửi `{}`, rỗng, `"unknown"` | Fallback Available, có thể mở phòng ngoài ý muốn | 400, không lưu thay đổi |
| `desiredStatus=99` hoặc alias mâu thuẫn | Không kiểm tra đầy đủ ở DTO | 400 |
| Rented nhưng không tìm thấy effective contract | Có thể đổi thủ công sang Available/tạm ngưng | 409, cần kiểm tra/sửa dữ liệu qua quy trình riêng |
| RoomStatus lệch nhưng còn Approved request hoặc cọc Paid/Confirmed chưa chuyển contract | Chỉ nhìn trạng thái Room/effective contract nên có thể bỏ sót | 409 cho cả đổi trạng thái lẫn tạm ngưng qua DELETE |
| Contract đã kết thúc, request ConvertedToContract, cọc giữ làm lịch sử | Không nên làm phòng bị khóa vĩnh viễn bởi cọc lịch sử | Vẫn cho đổi 0/3 nếu không còn quyền thuê/giữ chỗ khác; test PASS. Không đồng nghĩa đã có tất toán/bàn giao theo BL-005 |

Ma trận thủ công khi không có quyền thuê/cọc được bảo vệ: 0→0, 0→3, 3→0, 3→3 được phép; mọi đích 1/2 bị từ chối; nguồn 1/2 hoặc ngoài enum không được tự sửa. PendingSignature/PendingStart/Active contract đều chặn thao tác. Ownership được kiểm tra ở backend, không chỉ dựa role hoặc disabled UI.

Không tự sửa các dòng SQL cũ bị trạng thái lệch, không giải phóng cọc Paid/Confirmed. Cần xử lý chúng trong giai đoạn đối soát sau, có xác minh dữ liệu.

## 4. Kết quả kiểm thử thực chạy

| Kiểm thử | Mong đợi | Kết quả | Trạng thái |
|---|---|---|---|
| 51 regression case mới, chạy trước sửa | Phát hiện mapping/fallback/manual transition sai | 19 FAIL, 32 PASS; có TRX | Tái hiện lỗi |
| Nhóm RoomStatus + RoomPublication + RequestTerms sau sửa | Không regression Room/Post/quyền giữ chỗ | 102/102 PASS | PASS |
| Toàn bộ backend với SQL QA riêng | Tất cả case chạy, không skip SQL | 223 PASS, 0 FAIL, 0 SKIP | PASS |
| SQL RoomStatus integration | Lưu 3/0 chính xác; Approved request/cọc đã trả chặn tạm ngưng dù status cũ Available | PASS, thuộc 223 case | PASS |
| DTO string/enum/alias + 16 cặp chuyển trạng thái | Mapping đúng, sai không persist | PASS, thuộc suite trên | PASS |
| HTTP tạo/sửa Room, tạo Post, Admin duyệt, search | RoomStatus không sai, Post dùng Room tồn tại | PASS | PASS |
| HTTP tạm ngưng rồi mở lại | Search 1→0→1, Room lưu 0→3→0 | PASS | PASS |
| HTTP thiếu/sai/mâu thuẫn/trạng thái giữ chỗ hoặc đã thuê | 400, giữ nguyên Room đang tạm ngưng | PASS | PASS |
| HTTP Tenant/khác Landlord | 403 | PASS | PASS |
| HTTP reservation dữ liệu lệch, PUT và DELETE | 409, không mở/tạm ngưng sai | PASS | PASS |
| Playwright 1440px, 768px, 375px | Render không tràn trang/modal; gửi đúng 3/0; disabled nút 1/2; lỗi 409 giữ modal | 9/9 ca UI PASS; ảnh chụp đã mở kiểm tra | PASS |
| Tổng HTTP + UI ở trên | Không giả lập response/API | 28/28 PASS | PASS |
| Backend Release build | Build thành công | 0 warning, 0 error, output riêng QA | PASS |
| Frontend lint | Không lỗi ESLint | Exit 0 | PASS |
| Frontend production build | TypeScript/Vite thành công | Exit 0 | PASS |
| Git diff --check | Không whitespace error | Exit 0, có cảnh báo CRLF của môi trường Windows | PASS |
| 5 quan sát audit BL-004/010/013/017/018 | Xác định hành vi thực tế, không sửa ngoài phạm vi | Tái hiện đủ 5 khoảng thiếu như bảng 1 | CONFIRMED GAP, không phải nghiệp vụ đã PASS |
| VNPay real sandbox bank transaction / refund thật | Cần kiểm chứng riêng có merchant hợp lệ và giao dịch sandbox thực | Không thực hiện | **NOT VERIFIED** |
| Full manual E2E gồm ngân hàng, bàn giao/tất toán thực tế | Cần policy và nghiệp vụ các giai đoạn sau | Không tuyên bố hoàn tất; chỉ core automated regression PASS | Chưa kiểm chứng đầy đủ |

Build Release vào bin cũ ban đầu bị file-lock do server QA từ phiên trước đang chạy. Không dừng server đó; build lại với `-o .../bl001-verification/bin` PASS. Lần test trong sandbox bị kẹt giao tiếp testhost đã được hủy; các số liệu PASS/FAIL ở bảng lấy từ lần chạy ngoài sandbox kết thúc đầy đủ. Script HTTP ban đầu dùng route `/auth/login` và `/admin/...` không đúng source; đã sửa script sang `/xac-thuc/dang-nhap` và `/quan-tri/...`, chạy lại đầy đủ 28/28. Không đổi route ứng dụng để phù hợp script.

### Bằng chứng và chạy lại

Thư mục kết quả: `C:/Users/huanp/AppData/Local/Temp/bl001-verification`.
- [TRX trước sửa](C:/Users/huanp/AppData/Local/Temp/bl001-verification/bl001-before.trx).
- [TRX module sau sửa](C:/Users/huanp/AppData/Local/Temp/bl001-verification/bl001-after.trx).
- [TRX toàn bộ sau sửa](C:/Users/huanp/AppData/Local/Temp/bl001-verification/bl001-all.trx).
- [Kết quả HTTP/UI](C:/Users/huanp/AppData/Local/Temp/bl001-verification/http-ui-results.json), [quan sát audit](C:/Users/huanp/AppData/Local/Temp/bl001-verification/audit-observations.json).
- [Modal 375px](C:/Users/huanp/AppData/Local/Temp/bl001-verification/room-status-modal-3-375.png), [danh sách 768px](C:/Users/huanp/AppData/Local/Temp/bl001-verification/room-status-list-768.png), [lỗi 409 giữ modal 375px](C:/Users/huanp/AppData/Local/Temp/bl001-verification/room-status-error-375.png).

Chạy nhóm thuần InMemory: `dotnet test backend/RoomRental.BackEnd.Tests --filter FullyQualifiedName~RoomStatusTests`.

Full suite: cấu hình `ROOM_RENTAL_TEST_SQL` trỏ tới database QA **mới** có tiền tố `RoomRental_Verification_`, rồi `dotnet test backend/RoomRental.BackEnd.Tests`. Không tái sử dụng database có fixture từ lần trước; không trỏ biến này vào RoomRentalDB. Nếu không cấu hình biến, SQL facts bị skip và không được báo là đã kiểm tra SQL.

## 5. Ảnh hưởng đến nghiệp vụ

| Nghiệp vụ | Phạm vi xác nhận lần này |
|---|---|
| Thêm phòng | HTTP + regression PASS, mặc định Available; không tự tạo Post |
| Sửa phòng | HTTP + regression PASS; không sửa policy field công khai, ảnh/giá/số người; policy điều kiện thuê vẫn chạy như cũ |
| Tạm ngưng | Đã sửa; PUT gửi 3 lưu 3, DELETE cùng guard; không xóa dữ liệu |
| Mở lại | Chỉ từ Available/TemporarilyUnavailable và không có quyền được bảo vệ; Approved Post xuất hiện lại trong search là hành vi hiện có |
| Đăng bài | Create với Room thuộc owner + Admin approve + search đã chạy HTTP; uniqueness/ownership test cũ PASS |
| Yêu cầu thuê | Test cũ + SQL tranh chấp reservation PASS; audit khóa Landlord/ghi đè Note vẫn OPEN |
| Đặt cọc | Không sửa workflow tài chính; guard đọc Paid/Confirmed ngăn đổi trạng thái sai. Callback đúng hạn/idempotency test cũ PASS không xác nhận callback muộn/hoàn cọc |
| Hợp đồng | Test lifecycle/ký/snapshot/EndDate inclusive/Roommate cũ PASS; không sửa termination, PDF, bàn giao/tất toán |

Mô hình vẫn thuê nguyên phòng/nguyên căn. Module tìm người ở ghép hiện tại quản lý người ở cùng một contract chính và chi phí dự kiến, không tạo hợp đồng giường/chỗ độc lập; không cần triển khai Bed trong giai đoạn này. Cần giữ phân biệt giá toàn phòng với chi phí dự kiến/người khi rà soát nhãn danh mục sau.

## 6. Vấn đề còn tồn tại và ưu tiên

1. **Ưu tiên cao**: BL-002/003/004. Không mở khóa cọc Paid chỉ bằng đổi enum; cần phân biệt tiền thực nhận và quyền thuê, chống callback muộn/lặp/race, lưu bằng chứng đối soát.
2. **Ưu tiên cao**: BL-005/015 và phần còn lại BL-007. Cần policy bàn giao/tất toán và snapshot trước khi thay đổi contract hiện hành. Không coi việc test hiện tại PASS là workflow kết thúc hợp đồng đã đủ ngoài đời thực.
3. **Ưu tiên cao/trung bình**: BL-010, BL-013, BL-017/018, BL-025. Có bằng chứng tái hiện/code nhưng không nằm trong phạm vi triển khai lần này.
4. **Ưu tiên trung bình**: BL-009/012/014/016/021; cần chốt policy trước. BL-019/020 là hardening/giới hạn một phần, chưa chứng minh toàn bộ module sai.
5. BL-006/008/023 không tự sửa vì các bảo vệ chính hiện có đúng hoặc phạm vi đã được mở rộng bởi module Roommate. BL-024 chỉ xác nhận phần đã thử, không tuyên bố mọi nút mọi trang đã kiểm tra.
6. BL-011 cần nội dung gốc để kiểm tra. Không suy ra còn đúng 24 lỗi chưa sửa chỉ từ số mã được liệt kê.

## 7. Kế hoạch giai đoạn tiếp theo, chưa triển khai

### Giai đoạn 2: BL-002/003/004

- Kiểm kê **chỉ đọc** dữ liệu thật trước sửa: Room Reserved, request Approved/Expired/Cancelled, Deposit Paid/Confirmed, payment Succeeded/Expired/Failed, số contract liên quan. Chưa chạy kiểm kê production trong lần này.
- Lập ma trận cọc đã thu/chưa thu, còn/hết quyền giữ chỗ, có/chưa có contract; chốt ai yêu cầu hoàn, ai duyệt, số tiền/khấu trừ, bằng chứng và điều kiện trả phòng về Available. Không hoàn tiền tự động khi chưa cho phép.
- Callback muộn phải lưu sự kiện giao dịch thực tế/đối soát nhưng không tự cấp lại quyền thuê đã hết hạn hoặc đã cấp người khác. Giữ immutable lịch sử payment thành công, xử lý duplicate/reordered IPN và Return an toàn.
- IPN cần kết quả xử lý có phân loại để trả đúng mã PAY; tách nghĩa xác thực callback, trạng thái chuyển tiền và trạng thái quyền thuê. Không đưa SecretKey/hash vào log.
- **Ảnh hưởng dữ liệu**: không đổi hàng loạt Paid/Confirmed/Expired cũ, không xóa attempt; cần phương án xử lý bản ghi không nhất quán riêng. Có thể cần entity/field ghi nhận sự kiện đối soát/hoàn cọc, nhưng chưa chốt schema. Phải trình bày cột, nullable/default, index, dữ liệu cũ và xin xác nhận trước mọi migration.
- **Test cần chạy**: tiền đến trước/sau deadline; callback đến sau 30 phút; request đã hủy; phòng đã cấp Tenant khác; nhiều attempt; callback trùng/sai tiền/sai chữ ký; pending bill hồi trạng thái; transaction rollback; race Approve/Cancel/IPN/worker; kiểm tra không giải phóng phòng còn contract hoặc cọc chưa tất toán. Giao dịch ngân hàng sandbox vẫn NOT VERIFIED cho đến khi chạy riêng thực sự.

### Giai đoạn 3: Contract và vòng đời Room

- BL-005: chốt mốc kết thúc thực tế, lý do, nghĩa vụ còn nợ, handover và refund; tránh auto Available khi chưa bàn giao. Cần thảo luận tương thích contract cũ và Roommate occupants, không tự backfill lý do giả.
- BL-006/007/015: giữ giá thương lượng, hai bên xác nhận; snapshot thông tin đối tượng/đối tác/điều khoản và ngày ký nếu cần; báo schema trước, không lấy giá niêm yết mới ghi đè contract cũ. PDF phải hiển thị đúng Terms và phân biệt ngày xuất với ngày ký.
- BL-014: bỏ mutation khỏi GET khi worker/command có bảo vệ đủ dữ liệu lifecycle trễ; test GET không ghi DB, restart/worker retry/idempotency, boundary midnight Việt Nam.
- BL-016: chốt bill hủy có thay thế không, dùng revision/link lịch sử nếu cần; Paid immutable, không nhân đôi nghĩa vụ thanh toán, đúng kỳ cuối và meter continuity. Giữ cách hiểu EndDate inclusive đã PASS.
- Chạy regression toàn bộ Room/Post/Request/Deposit/Payment/Contract/Bill và Roommate sau từng thay đổi; không dồn nhiều policy tài chính vào một sửa lớn.

### Giai đoạn 4

Sau khi được cho phép mới xử lý các vấn đề còn lại theo mức ưu tiên. Chốt policy khóa chủ tin, note/rejection tách biệt, publication checklist, dữ liệu Room bắt buộc, lịch xem quá hạn, kiểm duyệt/thông báo và OrderInfo. Hardening claims/rate limit phải giữ tương thích auth và không mở rộng scope sang viết lại hệ thống.

**Kết thúc giai đoạn 1. Dừng tại đây để người dùng kiểm tra, không tự triển khai giai đoạn 2.**
