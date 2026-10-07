# RoomRentalSystem: báo cáo hoàn thiện luồng nghiệp vụ

Ngày kiểm tra: 07/10/2026. Phạm vi: source hiện tại, theo các quyết định Room/Post đã được người dùng duyệt. Không đổi framework/kiến trúc, không xóa dữ liệu cũ, không drop bảng legacy.

## 1. Kết quả audit và sửa lỗi

| Nhóm | Vấn đề xác nhận từ source trước sửa | Kết quả hiện tại |
| --- | --- | --- |
| Room/Post | Tạo Post còn tạo Room; form đăng tin còn chứa dữ liệu phòng | PASS: tạo Room trước, chọn Room của mình, gửi roomId; tạo Post không thêm Room |
| Nguồn dữ liệu | Một số projection dùng Post.DisplayPrice | PASS: giá hiện tại và các thuộc tính vật lý lấy Room; hợp đồng/hóa đơn giữ snapshot riêng |
| Moderation | Sửa Room có thể giữ Post Approved | PASS: so sánh dữ liệu công khai trước/sau; Approved chuyển Pending khi có thay đổi thực |
| Uniqueness | Chưa có bảo vệ thống nhất Pending/Approved theo Room | PASS: BLL kiểm tra, SQL unique filtered index chặn cả request đồng thời và insert trực tiếp |
| Public search | Policy cũ còn cho phòng Reserved/Rented vào danh sách | PASS: chỉ Approved AND Available; URL chi tiết hợp lệ vẫn đọc được Reserved/Rented |
| Radius | Có thể cho kết quả thiếu tọa độ vào bán kính | PASS: tọa độ hợp lệ, lọc latitude-band trước, tính khoảng cách thực, loại kết quả thiếu/ngoài bán kính |
| Pagination | API danh sách không trả tổng số kết quả; UI chưa có phân trang thật | PASS: endpoint paged mới, totalCount trước phân trang, filter/page lưu trên URL |
| RentalRequest | Serializable đã có; cần kết quả ổn định khi đồng thời và kiểm tra lại Post | PASS: giữ flow cũ, bổ sung khóa giao dịch và kiểm tra Approved trước giữ phòng |
| Deposit expiry | Chưa bảo vệ đầy đủ hợp đồng chờ bắt đầu khi mở lại phòng | PASS: bảo vệ PendingSignature/PendingStart/Active và reservation khác; chạy lặp không mở phòng sai |
| Payment timeout | Pending không callback có thể chặn retry lâu dài | PASS: attempt hết hạn sau 30 phút; retry nếu đối tượng còn được thanh toán |
| Contract | Hai bên ký có thể Active ngay dù chưa đến StartDate | PASS: thêm PendingStart=5; worker kích hoạt đúng ngày; yêu cầu Room Reserved |
| MonthlyBill | Chưa kiểm tra đầy đủ kỳ hợp đồng, continuity và reconcile định kỳ | PASS: kiểm tra kỳ, tự lấy đầu kỳ trước, bảo vệ bill đã dùng làm mốc, reconcile Overdue |
| Admin/Landlord | Helper Room/Post/Appointment còn có thể chấp nhận Admin | PASS: helper chỉ Landlord; Admin dùng API quản trị riêng, không tự sinh LandlordProfile |
| JWT | Token cũ có thể tiếp tục dùng sau khi tài khoản bị khóa | PASS: OnTokenValidated kiểm tra trạng thái và role hiện tại trong DB |
| Favorite/Chat/Report | Kiểm tra tính hợp lệ của Post chưa đồng nhất | PASS: không mở mới bằng Post private; favorite không trả payload Hidden/Rejected; lịch sử hợp lệ không bị xóa |
| Appointment | Confirm/Complete chưa kiểm tra đủ trạng thái phòng/thời điểm | PASS: Confirm kiểm tra Approved+Available+giờ tương lai; Complete không trước giờ |
| Room ownership | GET chi tiết chưa kiểm tra object ownership đầy đủ | PASS: Landlord chỉ đọc/sửa/tạm ngưng phòng thuộc mình; Admin đọc theo policy quản trị |
| Room mutation | Collection bị bỏ qua có thể xóa ảnh/tiện ích; tạo Room lưu nhiều bước | PASS: bỏ qua collection giữ nguyên, danh sách rỗng mới xóa; Room/ảnh/tiện ích lưu cùng SaveChanges |
| Room status | Thay đổi thủ công có thể tranh chấp với reservation | PASS: cùng transaction/khóa workflow; không đổi thủ công khi có hợp đồng cần giữ phòng |

## 2. Các phần đã đúng và được giữ nguyên

- Landlord duyệt yêu cầu thuê với số tiền cọc và deadline; Deposit được tạo bởi backend trong transaction, không bởi Tenant.
- Tenant không quyết định số tiền cọc. Frontend không gọi tạo cọc lần thứ hai sau khi request được duyệt.
- Các kiểm tra participant/ownership của yêu cầu thuê, cọc, hợp đồng và hóa đơn; chống overlap; snapshot đơn giá hợp đồng.
- Unique bill theo Contract+Month+Year, chỉ số mới không nhỏ hơn chỉ số cũ, bill Paid không sửa/xóa.
- Kiểm tra chữ ký và số tiền VNPay, các kiểm tra sender/participant chat và duplicate favorite/report đã có.
- Trang chi tiết phòng, gallery, bản đồ và picker địa chỉ hai chiều đã có. Không viết lại toàn website.
- Không thêm quy trình xác minh chủ trọ mới hoặc thay hệ thống authentication.

## 3. Policy Room/Post cuối cùng

### Room là nguồn sự thật

Room giữ giá, diện tích, loại phòng, địa chỉ/tỉnh/huyện/xã, sức chứa, số phòng ngủ/tắm, tầng, tiện ích, ảnh, tọa độ và phí điện/nước/dịch vụ. Post giữ RoomId, title/content và moderation theo entity đang có.

Cột Post.DisplayPrice vẫn giữ để tương thích dữ liệu cũ, không còn là nguồn giá hiển thị trong các projection BLL. Contract/Bill không đọc lại giá Room để thay đổi các snapshot đã chốt.

### Các field khiến Approved phải về Pending

- RoomName, Description, Price, Area, CategoryId, MaxOccupants.
- Bedrooms, Bathrooms, Floor.
- Address, Province, District, Ward.
- Latitude, Longitude.
- ElectricityPrice, WaterPrice, ServiceFee.
- Tập AmenityIds; thứ tự tập không làm phát sinh thay đổi giả.
- Danh sách ảnh theo ảnh chính/thứ tự hiển thị.
- Chỉnh title/content của chính Post cũng phải gửi duyệt lại nếu nội dung thực sự đổi.

Không reapprove chỉ vì Id, LandlordId, CreatedAt/UpdatedAt, CurrentOccupants hoặc RoomStatus. Các field này không phải tất cả đều được phép sửa từ frontend. Thay đổi RoomStatus vẫn ảnh hưởng khả năng thuê/public search. Gửi lại giá trị y hệt không reset moderation. Khi reapprove, xóa ApprovedAt và lý do từ chối cũ.

### Ownership và active slot

- Landlord chỉ chọn Room của mình, Available, chưa có Pending/Approved khác.
- Reserved/Rented/TemporarilyUnavailable không được tạo Post mới hoặc gửi duyệt lại.
- Mỗi Room tối đa một Post trong tập Pending/Approved. Hidden/Rejected/Expired vẫn được giữ làm lịch sử.
- Admin duyệt Pending phải kiểm tra lại Room Available và active slot.
- Admin không dùng các endpoint mutation của Landlord; GET /api/phong/{id} cho Admin đọc để quản trị.
- DELETE Room là tạm ngưng, không xóa vật lý dữ liệu/lịch sử. Trạng thái Reserved/Rented không được đặt thủ công.

### Chi tiết và tìm kiếm

Search chỉ Approved+Available. Chi tiết công khai cho Approved với Room Available/Reserved/Rented; không công khai Pending/Hidden/Rejected/Expired hoặc Room TemporarilyUnavailable.

Reserved hiển thị “Đang giữ chỗ”, Rented hiển thị “Đã thuê”. Không mở đặt lịch/yêu cầu thuê mới từ các trạng thái này; backend cũng từ chối, không chỉ dựa vào nút frontend. Favorite của tin đã thuê còn có thể đọc, nhưng payload tin private bị loại khỏi danh sách. Conversation lịch sử đã được phân quyền không bị xóa; không mở chat mới bằng Post private.

## 4. API thay đổi

### Tạo và sửa Post

POST /api/bai-dang, chỉ Landlord:

```json
{ "roomId": 11, "title": "Phòng có cửa sổ", "description": "Nội dung tin đăng" }
```

Backend không tạo Room. Title/TieuDe và Description/MoTa còn hỗ trợ alias theo DTO. PUT /api/bai-dang/{id} chỉ sửa title/content; payload sửa thuộc tính vật lý qua Post bị từ chối và cần chuyển sang API Room. Đây là thay đổi DTO chủ động đã được người dùng đồng ý, không phải endpoint tạo Post ngầm giữ compatibility với payload cũ.

PUT /api/phong/{id}: collection bị bỏ qua/null giữ dữ liệu hiện có; `amenityIds: []`/`imageUrls: []` hoặc các alias tương ứng xóa rõ ràng. Tạo/sửa Room kiểm tra giá, diện tích, số người, địa chỉ, tọa độ, phí không âm, category/amenity tồn tại và ảnh không rỗng.

### Search contract

GET /api/bai-dang/search trả paged result trong ApiResponse.data. GET /api/bai-dang cũ vẫn trả mảng để không phá các caller cũ, nhưng áp dụng cùng policy Approved+Available.

| Parameter | Ý nghĩa |
| --- | --- |
| keyword | Tìm title/content và địa chỉ |
| province, district, ward | Các thành phần địa chỉ |
| minPrice, maxPrice | Khoảng giá Room |
| minArea, maxArea | Khoảng diện tích |
| categoryId | Loại phòng |
| maxOccupants | Room có sức chứa ít nhất số người yêu cầu |
| amenityIds | Phải có tất cả tiện ích được chọn; truyền lặp `amenityIds=1&amenityIds=2` |
| latitude, longitude | Phải truyền theo cặp; latitude [-90,90], longitude [-180,180] |
| radiusInKm | Lớn hơn 0, tối đa 500km; cần tọa độ; mặc định 50km khi đã có tọa độ |
| sortBy | new, price_asc, price_desc, distance; backend còn hỗ trợ area_asc/area_desc/views |
| pageNumber, pageSize | Page bắt đầu từ 1; size mặc định 20, tối đa 100; UI dùng 12 |

Không có filter “bao gồm phòng hết/giữ chỗ”. Parameter status cũ không nới lỏng policy public. Tìm distance cần vị trí; có vị trí và không chỉ định sort thì xếp gần nhất. Tính khoảng cách bằng Haversine, lọc theo giá trị chưa làm tròn, chỉ làm tròn 2 chữ số để hiển thị. Không load toàn DB để tìm radius: các filter và latitude-band được thực hiện trước ở SQL; các ứng viên còn lại mới tính khoảng cách trong ứng dụng.

Ví dụ response bên trong ApiResponse:

```json
{
  "success": true,
  "data": {
    "items": [],
    "totalCount": 25,
    "pageNumber": 4,
    "pageSize": 12,
    "totalPages": 3
  }
}
```

Trang vượt số trang có items rỗng nhưng vẫn giữ totalCount. UI có Previous/Next, giữ filter khi đổi trang, reset trang 1 khi apply filter/sort. URL dùng cùng tên parameter, reload/back/share giữ trạng thái.

## 5. State machine cuối cùng

| Entity | Giá trị trạng thái |
| --- | --- |
| Room | 0 Available; 1 Rented; 2 Reserved; 3 TemporarilyUnavailable |
| Post | 0 Pending; 1 Approved; 2 Rejected; 3 Hidden; 4 Expired |
| RentalRequest | 0 Pending; 1 Approved; 2 Rejected; 3 Cancelled; 4 ConvertedToContract; 5 Expired |
| Deposit | 0 Pending; 1 Paid; 2 Confirmed; 3 RefundRequested; 4 Refunded; 5 Cancelled; 6 Expired |
| PaymentTransaction | 0 Pending; 1 Succeeded; 2 Failed; 3 Expired |
| Contract | 0 PendingSignature; 1 Active; 2 Terminated; 3 Expired; 4 Cancelled; 5 PendingStart |
| MonthlyBill | 0 Unpaid; 1 Paid; 2 Cancelled; 3 PendingPayment; 4 Overdue |

Luồng thuê chính:

```text
Room Available -> Post Pending -> Admin Approved -> Search/Detail
Tenant RentalRequest Pending
Landlord approve: Request Approved + Room Reserved + Deposit Pending (cùng transaction)
Thanh toán cọc thành công: Deposit Paid -> Landlord xác nhận: Confirmed
Tạo Contract: Request ConvertedToContract + Contract PendingSignature, Room vẫn Reserved
Đủ chữ ký và StartDate ở tương lai: Contract PendingStart, Room vẫn Reserved
Đến StartDate: Contract Active, Room Rented
Bill Unpaid -> PendingPayment -> Paid
Bill quá hạn: Unpaid -> Overdue (vẫn được thanh toán)
Contract Expired/Terminated -> Room Available nếu không còn hợp đồng/reservation cần giữ phòng
```

- PendingSignature/PendingStart/Active đều chiếm phòng và tham gia kiểm tra overlap; không đổi số trạng thái cũ.
- PendingSignature quá hạn ký dùng policy hiện có (mặc định 72 giờ). Worker không kích hoạt hợp đồng hết hạn.
- Deposit Pending quá DueAt chuyển Expired; Request Approved tương ứng chuyển Expired. Chỉ mở Reserved nếu không còn contract effective/reservation khác. Paid/Confirmed không bị expire như khoản chưa thanh toán.
- Payment Pending quá 30 phút chuyển Expired; retry tạo attempt mới nếu target còn hợp lệ. Đây không phải Deposit deadline. Bill PendingPayment được trả Unpaid/Overdue; không tự expire Deposit còn hạn.
- Callback trùng của payment Succeeded trả kết quả đã xử lý, không ghi lại PaidAt hoặc thanh toán hai lần.
- Khi giữ phòng, các appointment Pending/Confirmed tương lai được hủy với lý do phòng đã được giữ.
- Bill cần Contract Active và tháng có giao với thời hạn hợp đồng; không cho kỳ hoàn toàn ngoài hợp đồng. Không prorate giá theo ngày trong đợt này.
- Bill tiếp theo lấy chỉ số mới cuối kỳ trước làm chỉ số cũ. Tạo theo thứ tự thời gian; không sửa/hủy mốc mà bill sau đang dùng. Giữ snapshot giá Contract.
- Worker RentalLifecycle chạy định kỳ (mặc định 5 phút khi enabled), xử lý cọc, contract, payment timeout và bill overdue; không cần user mở trang.

Các thao tác state chính dùng Serializable và SQL transaction-owned sp_getapplock `RoomRental.Workflow`. Timeout khóa trả Conflict để tải lại, không âm thầm ghi đè. Khóa này serialize theo toàn workflow, phù hợp tải đồ án; không khẳng định khả năng chịu tải production lớn.

## 6. Database và cách triển khai

### Database hiện tại

- Đã kiểm tra cột DatCoc.HanThanhToan tồn tại; không phát hiện constraint status cũ cản Contract.PendingStart hoặc Payment.Expired.
- Đã preflight không có Room trùng Pending/Approved và áp dụng `backend/RoomRental.BackEnd/SQL/05_RoomPostPublication.sql` trên RoomRentalDB local.
- Script tạo `UX_TinDang_Room_Active`, unique PhongTroId với filter TrangThai IN (0,1). Nếu DB khác có dữ liệu trùng, script liệt kê và dừng, không tự xóa/ẩn dữ liệu.
- Không cần thêm cột cho hai status mới vì cột trạng thái là int. Không drop HopDongThue_Legacy hoặc xóa lịch sử Room/Post.

### Database trắng

Chọn đúng database trống trong SSMS rồi chạy `backend/RoomRental.BackEnd/SQL/00_CreateCurrentSchema.sql`. Script được sinh từ EF model hiện tại, tạo 24 bảng cùng FK/check/index, có deadline và active-post index. Đã chạy trên DB kiểm thử trắng, chạy lại lần hai thành công. Đây chỉ là bootstrap schema, không seed tài khoản/dữ liệu demo.

Không dùng bootstrap để sửa một DB đã có TaiKhoan hoặc schema đang tạo dở: bootstrap sẽ bỏ qua. DB cũ dùng runbook preflight/upgrade/verify hiện có, bổ sung `04_AddDepositDeadline.sql` nếu cần rồi `05_RoomPostPublication.sql`; xác nhận USE của các upgrade script cũ là RoomRentalDB trước khi chạy. Không chạy `RoomRentalDb_Schema_And_Data.sql`: đó là schema demo cũ có thao tác drop database.

SQL/README.md đã bổ sung hướng dẫn hiện tại; nội dung demo phía dưới được đánh dấu lịch sử. Các database `RoomRental_Verification_*` tạo để test được giữ riêng, không đổi connection string của đồ án và không drop DB sau kiểm tra.

## 7. Regression test bắt buộc

| # | Trường hợp | Kết quả / bằng chứng |
| --- | --- | --- |
| 1 | Create Post không tạo Room mới | PASS: RoomPublicationTests; SQL integration |
| 2 | Không tạo Post cho Room chủ trọ khác | PASS: RoomPublicationTests, lỗi 403 |
| 3 | Sửa public Room phải duyệt lại | PASS: 19 nhóm field; duyệt lại bằng Admin thành công |
| 4 | Không có hai Pending/Approved cùng Room | PASS: BLL, SQL request đồng thời và insert bypass BLL bị index chặn |
| 5 | Radius loại thiếu/ngoài tọa độ | PASS: WorkflowRegressionTests |
| 6 | totalCount trước paging | PASS: WorkflowRegressionTests; live API |
| 7 | Filter/page/URL/reload giữ đúng | PASS: final-workflow-test.mjs, bốn viewport |
| 8 | Hai request approve đồng thời, một thành công/một Conflict | PASS: SQL Server thật trong DB kiểm thử riêng |
| 9 | Deposit expiry mở lại đúng | PASS: WorkflowRegressionTests, gọi reconcile lặp |
| 10 | Không mở khi contract còn chiếm phòng | PASS: deposit regression; Room status guard cho PendingSignature/PendingStart/Active |
| 11 | Payment timeout retry được | PASS: WorkflowRegressionTests |
| 12 | Callback trùng idempotent | PASS: callback query có chữ ký fixture, PaidAt không đổi |
| 13 | Hợp đồng tương lai không Active sớm | PASS: PendingStart và kích hoạt khi đến ngày |
| 14 | Contract overlap bị chặn | PASS: WorkflowRegressionTests |
| 15 | Bill ngoài thời hạn hợp đồng bị chặn | PASS: WorkflowRegressionTests |
| 16 | Meter continuity | PASS: unit regression và SQL integration |
| 17 | Bill Paid không sửa/xóa | PASS: WorkflowRegressionTests |
| 18 | Account blocked không dùng token cũ | PASS: gọi chính event TokenValidated trước/sau khóa; không phải HTTP login bằng tài khoản thật |
| 19 | Landlord A không đọc/sửa/xóa/status Room B | PASS: RoomPublicationTests, 403 |
| 20 | Appointment không Complete trước giờ | PASS: WorkflowRegressionTests |

Thêm coverage: Room/Post cũ không bị đổi khi tạo tin mới; các tổ hợp Post/Room visibility; Reserved/Rented bị chặn đặt lịch/yêu cầu thuê; Admin không thành Landlord; no-op không reset duyệt; dữ liệu Room sai không lưu phòng dở; ảnh/tiện ích bỏ qua được giữ nguyên; đơn giá appointment lấy Room.

## 8. Build và kiểm tra thực tế

- Backend Debug build: PASS, 0 warning, 0 error.
- Backend Release build: PASS, 0 warning, 0 error.
- Backend tests: PASS, 101 passed, 0 failed, 0 skipped, bao gồm SQL Server integration với ROOM_RENTAL_TEST_SQL được cấu hình vào DB riêng.
- Frontend npm run lint: PASS.
- Frontend npm run build: PASS, TypeScript/Vite production build thành công.
- Browser Edge/Playwright: PASS 28 nhóm, ở 1440/1200/768/375px. Search/count/filter/page/reload; selector Room và payload tạo Post; detail unavailable; form Room/map; protected route anonymous/sai role; bảng quản trị và modal; kiểm tra document overflow.
- Browser các trang đã kiểm tra: không page error, console error, warning hoặc HTTP/request failure ngoài hub được chủ động trả 503 trong fixture. SignalR live chat không được xác nhận bởi bộ fixture này.
- Kết quả và ảnh: `docs/ui-audit/final-workflow/results.json`, cùng thư mục chứa search/detail/room-form/admin-table screenshots. Không phải kiểm tra mọi route của toàn website hoặc xác nhận mọi tài khoản/dữ liệu thật.
- Backend mới đang chạy: Swagger 200 tại http://localhost:5000/swagger/index.html.
- Live search pageSize=1: totalCount=2, totalPages=2, items=1, toàn Approved+Available. Bài cũ id=3 vẫn đọc được với RoomStatus=2 (Reserved).
- Không tạo thuê/cọc/hợp đồng/hóa đơn trên dữ liệu thật để thử. Browser dùng API fixtures; tests nghiệp vụ dùng InMemory và SQL DB kiểm thử.

### Chạy lại kiểm tra

```powershell
dotnet build backend/RoomRental.sln
dotnet test backend/RoomRental.sln
cd frontend
npm run lint
npm run build
```

SQL integration mặc định SKIP nếu không có ROOM_RENTAL_TEST_SQL. Muốn chạy đầy đủ, cung cấp connection string của SQL Server test với database tên bắt đầu `RoomRental_Verification_`; test từ chối DB khác trước khi ghi. Không đặt biến này trỏ vào RoomRentalDB đang dùng.

Browser script cần Playwright và Edge; đặt UI_AUDIT_NODE_MODULES đến thư mục node_modules chứa Playwright, UI_AUDIT_URL nếu dev server không ở http://127.0.0.1:5173, rồi chạy `node scripts/final-workflow-test.mjs` từ frontend. Các script availability/location cũ có assertion theo form/policy cũ, không dùng kết quả của chúng để chứng minh policy mới; bộ final-workflow là kiểm tra giao diện của đợt này.

## 9. File thay đổi

- Room/Post: BLL/RoomBLL.cs, PostBLL.cs, AdminBLL.cs, RoomPublicationPolicy.cs; Controllers/RoomController.cs, PostController.cs; DTO/Room/CreateRoomDto.cs, DTO/Post/CreatePostDto.cs, PostSearchResult.cs; DAL/Configurations/PostConfiguration.cs; Interfaces/IRoomService.cs, IPostService.cs.
- Workflow: BLL/RentalFeaturesBLL.cs, PaymentBLL.cs, MonthlyBillBLL.cs, WorkflowLock.cs; Models/Enums/RentalWorkflowStatus.cs; Services/RentalLifecycleHostedService.cs; Interfaces/IPaymentService.cs, IMonthlyBillService.cs.
- Quyền và core phụ trợ: Program.cs, Services/AccountTokenValidation.cs; BLL/FavoriteBLL.cs, ChatBLL.cs, ReportBLL.cs, ViewingAppointmentBLL.cs.
- Frontend: pages/landlord/CreatePostPage.tsx, LandlordRoomManagementPage.tsx, LandlordContractsPage.tsx; pages/public/RoomListPage.tsx, RoomSearch.css; pages/tenant/TenantRentalsPage.tsx; services/postService.ts; types/post.types.ts, room.types.ts; utils/constants.ts, roomAvailability.ts.
- Regression: PostBLLTests.cs, RoomPublicationTests.cs, WorkflowRegressionTests.cs, SqlServerWorkflowTests.cs; frontend/scripts/final-workflow-test.mjs và docs/ui-audit/final-workflow.
- SQL/docs: SQL/00_CreateCurrentSchema.sql, 05_RoomPostPublication.sql, README.md và báo cáo này.

## 10. Giới hạn và phần còn cần xác nhận

- Chưa thực hiện giao dịch VNPay/ngân hàng thật. Cần merchant config, ReturnUrl/IPN truy cập được và kiểm tra end-to-end với sandbox được cấp. Test chữ ký dùng secret fixture, không dùng tiền thật.
- Callback thành công đến sau attempt đã Expired hoặc sau Deposit deadline không tự mở lại target/đánh dấu Paid. Nếu ngân hàng đã trừ tiền, cần đối soát/hoàn tiền thủ công; đợt này không thêm API tra soát ngân hàng. Đây là giới hạn cần lưu ý khi triển khai thanh toán thực tế.
- Worker cần backend chạy và RentalLifecycle:Enabled=true. Khi host ngừng, không có job chạy độc lập ngoài ứng dụng; khi khởi động lại sẽ reconcile dữ liệu đến hạn.
- SQL global workflow lock và tính khoảng cách trên candidate list thích hợp đồ án, chưa load test production lớn. Có thể tối ưu per-room lock/spatial query sau khi có yêu cầu tải thực tế, không refactor ở vòng này.
- Bootstrap không tự sửa DB đang có schema thiếu bảng; không seed admin/catalog. Cần kiểm tra runbook/onboarding trên máy triển khai mới.
- Browser đã kiểm tra phạm vi thay đổi và các UI đại diện, không khẳng định mọi route hoặc SignalR/VNPay thật đã end-to-end PASS.
- Chưa tự commit/push lên GitHub trong đợt này. Không chứa connection string, JWT, mật khẩu hay merchant secret trong báo cáo.

Kết luận: nhóm Room/Post đã được kiểm thử trước khi chuyển sang các nhóm tiếp theo. Các regression yêu cầu và build cuối PASS; dữ liệu cũ giữ nguyên. Luồng core Search -> Detail -> Favorite/Chat -> Appointment -> RentalRequest được giữ rõ, phần cọc/hợp đồng/hóa đơn hoàn thiện phía sau. Các giới hạn tích hợp thật ở trên không được coi là đã kiểm chứng.
