# Hướng dẫn tự kiểm thử RoomRentalSystem

Đối chiếu source hiện tại ngày 17/10/2026. Đây là **các bước cần thực hiện và kết quả mong đợi**, không phải báo cáo các bước đã PASS trên database của bạn.

## 1. Chuẩn bị an toàn

- Nên dùng bản sao database và tài khoản/dữ liệu thử riêng. Các thao tác kết thúc hợp đồng, hoàn cọc và xác nhận nhận tiền có thể thay đổi dữ liệu thật nếu chạy trên RoomRentalDB.
- Không chạy UPDATE trực tiếp để đổi trạng thái cọc, thanh toán, hợp đồng hoặc phòng. Không đổi đồng hồ Windows/SQL Server để ép hết hạn.
- Chuẩn bị Admin, Chủ trọ A, Chủ trọ B, Người thuê A, Người thuê B và Người thuê C. B là chủ trọ không liên quan, dùng để kiểm tra ownership; B/C là người ứng tuyển ở ghép.
- Dùng các browser profile hoặc các trình duyệt riêng cho từng tài khoản. Hai tab cùng trình duyệt thông thường vẫn dùng chung phiên đăng nhập.
- Backend: từ `backend/RoomRental.BackEnd`, chạy `dotnet run --launch-profile http`; mặc định Swagger tại `http://localhost:5000/swagger`. Nếu port bị chiếm, kiểm tra terminal và API URL frontend, không khởi động hai backend cùng database khi đang test thời gian.
- Frontend: từ `frontend`, chạy `npm run dev`; mở đúng URL Vite in ra. Cấu hình hiện tại ưu tiên port 5174, có thể tự chọn port khác nếu đang bận.
- Mỗi case ghi PASS, FAIL hoặc BLOCKED. Thiếu sandbox/cọc đã thanh toán hợp lệ thì ghi BLOCKED, không coi là PASS và không dùng SQL tạo thanh toán thành công giả.

## 2. Đường dẫn thao tác hiện có

Các đường dẫn dưới đây nối sau địa chỉ frontend đang chạy.

| Vai trò | Đường dẫn | Mục đích |
|---|---|---|
| Công khai | `/rooms` | Tìm phòng |
| Công khai | `/roommates` | Tìm bạn ở ghép, khác với loại phòng “ở ghép” |
| Công khai | `/blog` | Blog |
| Người thuê | `/tenant/appointments` | Lịch xem phòng |
| Người thuê | `/tenant/rentals` | Các tab yêu cầu/cọc, hợp đồng, hóa đơn, sự cố, đánh giá |
| Người thuê | `/tenant/roommates` | Tin tìm người, ứng tuyển, người ở cùng |
| Chủ trọ | `/landlord/rooms` | Phòng vật lý |
| Chủ trọ | `/landlord/posts` | Tin cho thuê |
| Chủ trọ | `/landlord/appointments` | Xử lý lịch xem |
| Chủ trọ | `/landlord/contracts?tab=requests` | Yêu cầu thuê, cọc, hoàn cọc/đối soát |
| Chủ trọ | `/landlord/contracts?tab=contracts` | Hợp đồng; nút “Kết thúc & trả phòng” |
| Chủ trọ | `/landlord/contracts?tab=bills` | Hóa đơn |
| Chủ trọ | `/landlord/roommates` | Duyệt tin và xác nhận người ở ghép |
| Admin | `/admin/posts/approval` | Duyệt bài đăng cho thuê |
| Admin | `/admin/payments` | Đối soát, hoàn cọc và tra cứu hồ sơ kết thúc theo mã hợp đồng |

Không phải chức năng nào cũng có route riêng: yêu cầu thuê/cọc/hợp đồng/hóa đơn dùng các tab. Ở trang người thuê, chọn tab phù hợp sau khi mở `/tenant/rentals`.

## 3. Bộ dữ liệu thử và thứ tự

Tạo dữ liệu qua form, đặt tên có tiền tố `TEST-`, ghi lại ID từ danh sách hoặc F12 → Network. Không lấy hợp đồng đang sử dụng thật để thử chấm dứt.

| Dữ liệu | Giá trị thử |
|---|---|
| Phòng chính | `TEST-P101`, sức chứa 3 người, giá 3.000.000/tháng |
| Giá điện/nước/dịch vụ | 3.500/kWh; 20.000/m³; dịch vụ 150.000/tháng |
| Cọc luồng chính | 5.000.000, hạn thanh toán trong tương lai |
| Hợp đồng chính | Bắt đầu hôm nay, ngày thuê cuối sau hôm nay; hai bên cùng ký |
| Phòng hoàn cọc | `TEST-P102`, dành riêng nhánh chưa có bất kỳ hợp đồng nào |
| Phòng kiểm duyệt | `TEST-P103`, dùng thử sửa giá và duyệt lại |

Thứ tự: phòng/tin → lịch xem/yêu cầu → cọc → hợp đồng → ở ghép → hóa đơn → kết thúc/bàn giao/tất toán. Luồng hoàn cọc trước hợp đồng dùng P102 riêng. Không chạy một nhánh phá hủy điều kiện của nhánh đang kiểm tra.

## 4. Phòng, tin đăng và kiểm duyệt

| Case | Thao tác | Mong đợi |
|---|---|---|
| ROOM-01 | Chủ trọ A tạo TEST-P101, nhập đủ dữ liệu, dán URL ảnh trực tiếp hợp lệ rồi thêm ảnh | Preview hiện; lưu/reload vẫn thấy ảnh; phòng xuất hiện trong danh sách của A |
| ROOM-02 | Thử URL ảnh không tồn tại; URL trùng; `data:image/...`; chuỗi có khoảng trắng | Ảnh lỗi có cảnh báo; URL được trim; không nhận duplicate/Base64; không báo lưu thành công giả |
| ROOM-03 | Tạo tin và chọn chính TEST-P101 | Tin gắn phòng đã có; số phòng không tăng; tin chờ duyệt, chưa có trên tìm phòng |
| ROOM-04 | Tạo tiếp tin thứ hai cho cùng phòng khi tin đầu Pending hoặc Approved | Bị chặn; không có hai tin hoạt động/chờ duyệt cho cùng phòng |
| ROOM-05 | Admin duyệt; người thuê tìm theo giá/khu vực/loại phòng và mở chi tiết | Tin Approved + phòng Available được tìm thấy; thông tin phòng nhất quán |
| ROOM-06 | Với TEST-P103 đã Approved: chủ trọ sửa giá rồi tìm lại | Tin quay về Pending, biến mất khỏi public search; Admin duyệt lại mới xuất hiện với giá mới |
| ROOM-07 | Chủ trọ B thử xem/sửa TEST-P101 qua API bằng ID của A | Bị từ chối 403/404; không đọc/sửa được dữ liệu của A |
| ROOM-08 | Dùng phòng đang Reserved/Rented để thử tạo tin/đặt lịch/gửi yêu cầu mới | Không cấp quyền thuê mới; public core search không hiển thị như phòng còn thuê được; URL/lịch sử hợp lệ có thể hiện trạng thái nhưng không cho đặt lịch/yêu cầu |

Test ảnh nên dùng URL HTTPS trực tiếp trả về ảnh, không phải URL trang kết quả tìm kiếm Bing. Cần có mạng để kiểm tra ảnh/bản đồ; lỗi dịch vụ ngoài phải ghi riêng.

## 5. Lịch xem, yêu cầu thuê và đặt cọc

| Case | Thao tác | Mong đợi |
|---|---|---|
| RENT-01 | Người thuê A đặt lịch TEST-P101 vào ngày/giờ tương lai | A thấy lịch; chủ trọ A thấy cùng lịch và xử lý được; bên B không thấy dữ liệu riêng |
| RENT-02 | Bỏ trống ngày/giờ hoặc chọn thời điểm không hợp lệ | Validation chặn, không tạo lịch; kiểm tra thông báo khi tạo/xử lý lịch hợp lệ |
| RENT-03 | A gửi yêu cầu thuê | A thấy yêu cầu chờ duyệt; chủ trọ A thấy đúng người, đúng phòng |
| RENT-04 | Chủ trọ bấm “Duyệt Yêu Cầu”, nhập cọc 5.000.000 và hạn tương lai, bấm “Duyệt và tạo cọc” | Yêu cầu được duyệt, tạo một khoản cọc, phòng Reserved; A thấy tiền cọc/hạn thanh toán |
| RENT-05 | Người thuê B đồng thời thử thuê/đặt lịch phòng đang giữ cho A | Không được cấp lại phòng; không tạo quyền giữ phòng thứ hai |
| RENT-06 | A bấm “Thanh toán VNPay” bằng cấu hình sandbox hợp lệ | Kiểm tra đúng số tiền/mã đơn. Chỉ khi backend nhận và xác thực kết quả thành công mới có trạng thái Paid |
| RENT-07 | Sau Paid, chủ trọ bấm “Xác nhận đã nhận cọc” | Cọc Confirmed; chưa tự thành hợp đồng Active; có thể tạo hợp đồng theo điều kiện hiện có |
| RENT-08 | Hủy/thất bại thanh toán; reload trang kết quả | Không được đánh Paid chỉ vì mở URL kết quả; không tạo khoản thu/thành công trùng |

**VNPay real sandbox bank transaction: NOT VERIFIED cho tới khi tự thực hiện và đối chiếu được giao dịch thật trong sandbox.** Mở được trang thanh toán hoặc thấy trang kết quả chưa đủ chứng minh. Nếu chưa cấu hình được, ghi RENT-06/07 và các bước phụ thuộc là BLOCKED. Có thể test những bước sau bằng hợp đồng/cọc QA hợp lệ đã có, ghi rõ đây không phải kết quả E2E từ đầu.

## 6. Hợp đồng và hóa đơn

| Case | Thao tác | Mong đợi |
|---|---|---|
| CONTRACT-01 | Tạo hợp đồng từ yêu cầu/cọc TEST-P101 đã đủ điều kiện | Chỉ một hợp đồng hợp lệ; đúng hai bên, phòng, giá/cọc/đơn giá; PendingSignature |
| CONTRACT-02 | Chỉ một bên ký | Chưa được coi là hai bên hoàn tất; không tự Active chỉ vì một bên ký |
| CONTRACT-03 | Hai bên ký, ngày bắt đầu hôm nay | Active/Rented theo điều kiện thời gian; lịch sử ký có người và thời điểm |
| CONTRACT-04 | Một hợp đồng QA khác có ngày bắt đầu tương lai, hai bên ký | PendingStart/Reserved, chưa được tuyển người như hợp đồng Active |
| CONTRACT-05 | Tải PDF, kiểm tra tên/địa chỉ tiếng Việt và số tiền | Không mất dấu; dữ liệu khớp snapshot hợp đồng, không đổi theo việc sửa phòng sau này |
| BILL-01 | Lập kỳ hóa đơn đầu thuộc thời gian thuê: điện 0→10, nước 0→2, phụ phí khác 0 | Với đơn giá ở mục 3: 3.000.000 + 35.000 + 40.000 + 150.000 = **3.225.000**; server dùng đơn giá hợp đồng |
| BILL-02 | Tạo lại kỳ đang có; nhập NewMeter thấp hơn OldMeter; tạo kỳ ngoài thời gian thuê | Bị chặn; không sinh bill trùng hoặc tiêu thụ âm/kỳ sau thời gian thuê |
| BILL-03 | Nếu có kỳ tiếp theo hợp lệ, lập sau kỳ đầu | OldMeter lấy từ NewMeter của kỳ trước; không tùy ý đặt lại về 0 |
| BILL-04 | Thanh toán/xác nhận bill bằng phương thức hiện có; thử sửa/hủy bill Paid | Người thuê/chủ trọ thấy thống nhất; bill Paid và lịch sử không bị sửa/xóa |

Để test hoàn cọc sau hợp đồng theo ví dụ mục 9, giữ bill BILL-01 chưa thanh toán. BILL-04 nên dùng hợp đồng/kỳ thử khác. Chỉ số đầu kỳ được nhập theo chức năng hiện có; việc thêm bảng mới không đồng nghĩa đã thêm cột chỉ số ban đầu vào hợp đồng.

## 7. Tìm người ở ghép, kiểm tra trước khi kết thúc hợp đồng

Điều kiện: hợp đồng chính TEST-P101 Active, phòng còn chỗ. Chọn loại phòng “Ở ghép” không tự tạo tin tìm người ở ghép.

| Case | Thao tác | Mong đợi |
|---|---|---|
| RM-01 | Người thuê A mở `/tenant/roommates`, bấm “Tạo tin ở ghép”, chọn hợp đồng chính, tìm 2 người | Tin chờ chủ trọ duyệt; chưa xuất hiện ở `/roommates` |
| RM-02 | Chủ trọ A mở `/landlord/roommates` → “Chi tiết / Yêu cầu” → “Duyệt tin” | Tin “Đang tìm người”, xuất hiện ở trang công khai |
| RM-03 | Người thuê B mở tin công khai và gửi yêu cầu | Trạng thái “Chờ người thuê chính”; chưa có người ở cùng mới, chưa tăng số cư trú |
| RM-04 | Người thuê A bấm “Chấp nhận” | “Chờ chủ trọ xác nhận”; vẫn chưa tự ghi B đã vào ở |
| RM-05 | Chủ trọ A bấm “Xác nhận vào ở” | B “Đã được duyệt”; có người ở cùng “Đang ở”; số cư trú tăng một lần; hợp đồng thuê chính không bị tạo lại |
| RM-06 | Lặp với C đến đủ 3/3; thử ứng tuyển thêm hoặc gửi trùng | Tin đủ người/ngừng tuyển; không vượt sức chứa; không tạo trùng người đang ở |
| RM-07 | Khi hợp đồng còn Active, xác nhận một người ở ghép rời | Ghi “Đã rời”, có mốc rời, giảm số người một lần; không mở cả phòng Available; tin đã Full không nhất thiết tự mở lại |
| RM-08 | Tài khoản không thuộc hợp đồng/chủ trọ khác thử duyệt/xác nhận/rời thay | Bị chặn; không thay đổi số cư trú/cọc/hợp đồng chính |

Muốn kiểm tra việc giữ cư trú khi hết hiệu lực, để lại ít nhất một người ở ghép đang ở trước mục 9.

## 8. Hoàn cọc trước hợp đồng, dùng TEST-P102 riêng

Điều kiện: cọc của P102 đã Paid hoặc Confirmed hợp lệ, **chưa từng có hợp đồng gắn với yêu cầu này**, kể cả hợp đồng cũ đã hủy/kết thúc.

| Bước | Người thực hiện và thao tác | Mong đợi |
|---|---|---|
| 1 | Người thuê → `/tenant/rentals` → tab yêu cầu/cọc → “Hoàn cọc và đối soát” → “Yêu cầu hoàn cọc” | Có form lý do; lý do quá ngắn bị chặn; gửi hợp lệ thành “Chờ chủ trọ duyệt” |
| 2 | Gửi trùng đề nghị hoặc thử tạo hợp đồng trong lúc đang xử lý hoàn | Bị chặn; không đồng thời hoàn và cấp hợp đồng |
| 3 | Chủ trọ → yêu cầu/cọc → “Duyệt hoàn” | “Đã duyệt hoàn”; phòng vẫn giữ, chưa Available chỉ vì đã duyệt |
| 4 | Chủ trọ → “Báo đã hoàn tiền”, nhập chứng từ | “Chờ xác nhận nhận tiền”; chưa tự coi là người thuê đã nhận |
| 5 | Đúng người thuê → “Đã nhận đủ tiền” | Hoàn thành yêu cầu, cọc Refunded; quyền giữ chỗ của lượt này kết thúc; phòng chỉ Available nếu không còn blocker khác |
| 6 | Reload và thử xác nhận lại | Không hoàn lần hai, không xóa khoản thanh toán gốc |

Thử riêng nhánh chủ trọ từ chối có lý do, hoặc người thuê rút yêu cầu trước báo hoàn: không được đánh Refunded. Cọc đã gắn hợp đồng phải xử lý mục 9, không dùng luồng này.

Đây là **ghi nhận hoàn tiền thủ công**, không phải gọi API hoàn VNPay. Trên QA có thể dùng chứng từ giả lập có nhãn `QA-ONLY-REFUND-001` để kiểm tra giao diện/trạng thái, nhưng phải ghi kết quả là mô phỏng, không phải xác minh đã chuyển tiền. Trên dữ liệu thật, chỉ báo/xác nhận sau khi thực sự chuyển/nhận tiền và có chứng từ thật.

## 9. Kết thúc hợp đồng, bàn giao và tất toán TEST-P101

Cả hai bên vào tab Hợp đồng của mình → tìm đúng ID → nút **“Kết thúc & trả phòng”**. Sau mỗi thao tác thành công modal có thể đóng: mở lại hồ sơ để xem trạng thái/nút kế tiếp. Tải lại hồ sơ ở tài khoản bên kia để tránh dùng phiên bản cũ.

| Bước | Thao tác | Mong đợi |
|---|---|---|
| END-01 | Người thuê A → “Đề nghị kết thúc / xác minh ngày kết thúc”, nhập lý do và thời điểm hiện tại thuộc thời gian hợp đồng | Đề nghị chờ phản hồi; chưa chấm dứt đơn phương; phòng chưa Available |
| END-02 | Chủ trọ A → “Đồng ý kết thúc” | Khi đã tới thời điểm hiệu lực: contract Terminated; tin/yêu cầu tuyển ở ghép được đóng; không tự hoàn cọc/Available/ghi người ở ghép Left |
| END-03 | Thử mở lại phòng ngay, khi chưa bàn giao/tất toán | Bị chặn hoặc không có nút; hồ sơ hiển thị lý do chưa đủ điều kiện |
| END-04 | “Lập / sửa biên bản bàn giao” → “Đã nhận phòng, trả thực tế”; nhập tình trạng, thời điểm trả hợp lệ, điện 10, nước 2 | Biên bản chờ hai xác nhận; số cuối không thấp hơn số bill đang có; chưa tự tất toán |
| END-05 | Chỉ một bên “Xác nhận bàn giao” | Chưa coi hai bên đã xác nhận; chưa mở phòng |
| END-06 | Bên còn lại “Xác nhận bàn giao” | Bàn giao được xác nhận; người ở cùng ghi rời theo thời điểm trả thực tế; vẫn còn chốt nợ/tất toán |
| END-07 | Trước chốt nợ, lập bill cuối nếu chưa có: đúng kỳ thuê, đúng chỉ số bàn giao; rồi chủ trọ “Chốt công nợ cuối” | Chấp nhận bill cuối của kỳ hợp lệ sau Terminated/Expired; không tạo kỳ sau thời gian thuê; không tạo lại bill BILL-01 đã có |
| END-08 | Chủ trọ “Lập phương án tất toán”, không thêm thiệt hại nếu không có | Bill còn nợ lấy từ server. Với cọc 5.000.000 và đúng một bill chưa trả 3.225.000: hoàn **1.775.000**, trả thêm 0 |
| END-09 | “Gửi phương án”; hai bên lần lượt “Đồng ý phương án” | Cả hai xác nhận cùng phương án; chỉ một bên đồng ý chưa đủ; không tự khấu trừ chỉ vì chủ trọ gửi |
| END-10 | Chủ trọ “Báo đã chuyển tiền hoàn”; người thuê “Xác nhận đã nhận tiền hoàn” | Có chứng từ, người và thời gian; không hoàn thành chỉ nhờ báo đã chuyển. Quy định QA/chứng từ thật giống mục 8 |
| END-11 | “Hoàn tất quyết toán” | Phương án Completed; bill còn mở trong phương án được tất toán, không tạo Succeeded VNPay giả; chưa nhất thiết tự mở phòng |
| END-12 | Chủ trọ “Xác nhận sẵn sàng cho thuê” | Chỉ thành công khi hết blocker; phòng Available và tin Approved có thể trở lại search. Phòng đang bảo trì phải giữ tạm ngưng |
| END-13 | Reload cả hai tài khoản và kiểm tra lịch sử | Vẫn đủ đề nghị/đồng thuận/bàn giao/chốt nợ/chứng từ/tất toán; không ghi lặp hoàn tiền khi bấm lại |

**Lưu ý:** Cọc gốc đã gắn hợp đồng có thể vẫn hiện Confirmed để giữ lịch sử nhận cọc. Xem trạng thái **phương án tất toán**, số tiền hoàn/trả thêm và lịch sử, không kết luận lỗi chỉ vì cọc gốc không đổi thành Refunded. Đây khác với hoàn trước hợp đồng ở mục 8.

## 10. Các nhánh quan trọng phải thử riêng

| Case | Cách thử | Mong đợi |
|---|---|---|
| EDGE-01 | Chủ trọ từ chối đề nghị kết thúc, hoặc người gửi rút trước khi đồng ý | Contract không bị chấm dứt bởi đề nghị bị từ chối/rút; lưu lý do/lịch sử |
| EDGE-02 | Hai bên đồng ý kết thúc ở thời điểm tương lai gần | Chưa tới mốc vẫn còn hiệu lực theo trạng thái hiện tại; tới mốc worker xử lý; GET hồ sơ không được tự thúc kết thúc |
| EDGE-03 | Ngày thuê cuối là D, kiểm tra trước/sau 00:00 ngày D+1 theo giờ Việt Nam | D là ngày thuê cuối inclusive; chỉ hết hạn từ D+1. Worker mặc định chạy chu kỳ 5 phút, có thể có độ trễ; không coi GET là worker |
| EDGE-04 | Hợp đồng tự Expired nhưng chưa bàn giao/hoàn tiền | Phòng và người ở ghép không tự Available/Left; phải đi qua hồ sơ kết thúc |
| EDGE-05 | Một bên “Chưa thống nhất bàn giao/tất toán” | Không có xác nhận hai bên giả; không hoàn tất hoặc mở phòng qua tranh chấp |
| EDGE-06 | Đã báo chuyển tiền rồi có tranh chấp; hai bên đồng ý lại | Giữ chứng từ cũ/cùng phương án; không bắt báo chuyển lần hai; không tạo thêm khoản thu/hoàn |
| EDGE-07 | Sửa bill trước khi phương án có báo tiền | Phương án/chốt nợ cũ bị vô hiệu hóa theo policy; không giữ đồng thuận cho số nợ đã thay đổi |
| EDGE-08 | Đã báo chuyển tiền hoặc Completed, thử sửa bill/phương án | Bị chặn; không sửa số tiền đã đồng thuận và có chứng từ |
| EDGE-09 | Hợp đồng QA khác: tổng nợ lớn hơn cọc | Chỉ có khoản trả thêm = nợ − cọc; người thuê báo trả, chủ trọ xác nhận nhận; không đồng thời hoàn và trả thêm |
| EDGE-10 | Tổng nợ bằng cọc | Hoàn 0, trả thêm 0; không yêu cầu chứng từ chuyển tiền giả cho số tiền 0 |
| EDGE-11 | Hợp đồng chưa nhận phòng, đã hủy chờ ký hợp lệ | Nhánh “Chưa từng nhận phòng” cần chứng cứ/hai xác nhận; không nhập ngày nhận/trả hay chỉ số giả; dữ liệu bill/cư trú mâu thuẫn phải bị chặn |
| EDGE-12 | Mở hợp đồng lịch sử thiếu mốc kết thúc | Báo cần xác minh, không tự gán ngày kết thúc hôm nay/đã tất toán; thực hiện xác minh và hai bên đồng thuận theo các thao tác được phép |
| EDGE-13 | Hai phiên mở cùng hồ sơ, phiên 1 sửa trước, phiên 2 gửi dữ liệu cũ | Phiên cũ bị chặn/xin tải lại; không ghi đè âm thầm. Bấm đúp/retry không tạo hai lần nghiệp vụ |
| EDGE-14 | Admin tra mã hợp đồng tại `/admin/payments` | Xem được hồ sơ, ghi hỗ trợ/đối soát; không có quyền ký/bàn giao/đồng ý tất toán thay hai bên trong luồng 3A |

Các case thời gian cần chờ mốc thật hoặc dùng bộ test tự động có đồng hồ giả lập trên QA. Không thể kết luận boundary PASS chỉ bằng đổi ngày trong SQL hoặc chờ vài phút với một hợp đồng chưa tới hạn.

## 11. Kiểm tra nhanh các màn hình còn lại

| Nhóm | Thao tác và mong đợi |
|---|---|
| Tài khoản/quyền | Đăng nhập/đăng xuất; nhập sai mật khẩu; mở route của vai trò khác phải bị chặn. Gọi API bằng ID của người khác cũng phải bị chặn, không chỉ ẩn nút |
| Tìm kiếm/phân trang | Kết hợp giá/khu vực/loại; chuyển trang không mất filter; không có dữ liệu thì có trạng thái rỗng; phòng Reserved/Rented không lẫn vào kết quả còn thuê được |
| Yêu thích | Thêm/xóa/reload; phòng sau khi thuê có thể vẫn xem qua mục đã lưu, nhưng không gửi yêu cầu/đặt lịch mới |
| Địa chỉ/bản đồ | Nhập địa chỉ rồi chọn đúng gợi ý để cập nhật pin; click map kiểm tra địa chỉ tương ứng; lưu/reload giữ tọa độ; ghi riêng lỗi mạng/geocoding |
| Blog | Xem bài công khai, bình luận khi được phép; bài riêng tư/ẩn không nhận bình luận công khai bằng cách đoán ID |
| Đánh giá/sự cố | Chỉ đúng người đủ điều kiện mới gửi; quá độ dài phải báo lỗi; đánh giá hiển thị theo phòng, không mất do thay tin; chủ trọ chỉ xử lý sự cố của mình |
| Chat/thông báo | Hai phiên trao đổi, nhận thông báo đúng người; reload không mất; người thứ ba không đọc được hội thoại riêng |
| Admin | Duyệt/từ chối bài đúng trạng thái; danh sách/chi tiết không làm mất dữ liệu; đối soát chỉ có kết quả/chứng cứ hợp lệ, không tự tạo chuyển khoản ngân hàng |

## 12. Responsive, lỗi mạng và bằng chứng

Mở F12 → Device Toolbar. Kiểm tra desktop, **768px** và **375px** với dữ liệu thật vừa tạo:

- Sidebar mở/đóng được, tên phòng/người dài không đè nút.
- Modal có nút đóng/Hủy, cuộn được tới nút xác nhận; bảng cuộn trong vùng của nó, không kéo vỡ toàn trang.
- Bỏ trường bắt buộc rồi gửi: thấy lỗi, form không mất dữ liệu.
- Trên thao tác QA chưa thực hiện, dùng Network → Offline rồi submit: không có toast thành công giả; online lại và retry phải có kết quả nhất quán, không nhân đôi nghiệp vụ.
- Sau mỗi mốc chính F5, đối chiếu cả hai vai trò. Đọc Network response, đừng chỉ dựa vào toast.

Mã HTTP: 400/403/404/409 có thể là kết quả đúng của case bị chặn. Nếu đang thao tác hợp lệ mà gặp 500 hoặc lỗi validation không có thông báo hữu ích, ghi FAIL và lấy log backend. Lỗi trả về do chưa tới bước/thiếu sandbox là BLOCKED hoặc cần bổ sung tiền điều kiện, không đổi dữ liệu để né lỗi.

Ghi kết quả theo mẫu:

```text
Case: END-08
Kết quả: PASS / FAIL / BLOCKED / CHƯA TEST
Môi trường: URL frontend, URL backend, tên database (không kèm mật khẩu)
Vai trò/tài khoản thử:
RoomId / PostId / RequestId / DepositId / ContractId / BillId:
Bước đã làm:
Mong đợi:
Thực tế và số tiền/trạng thái trước-sau:
Ảnh màn hình, URL + method + status + response liên quan:
Thời điểm xảy ra và log backend:
```

Không gửi JWT/access_token, cookie, mật khẩu, chữ ký callback hoặc thông tin thẻ/ngân hàng. Trong URL SignalR cần che tham số `access_token`.

Một luồng chỉ PASS end-to-end khi các bước trước đều thực sự thực hiện thành công trên môi trường thử; không lấy kết quả schema 10 bảng hay một bộ test tự động cũ để thay cho kết quả kiểm thử thủ công này.
