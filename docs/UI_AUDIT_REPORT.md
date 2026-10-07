# Báo cáo UI RoomRentalSystem

Ngày: 07/10/2026.

## Phạm vi

Giữ bố cục và màu thương hiệu hiện có; nâng chất lượng trang người dùng, chủ trọ và quản trị. Đợt này không sửa backend, SQL, API endpoint, DTO, JWT, quyền truy cập, VNPay hoặc giao thức SignalR. Các thay đổi backend/service/constants đã có trong working tree trước đợt UI được giữ nguyên.

Frontend đang chạy tại [http://127.0.0.1:5173](http://127.0.0.1:5173).

**Giới hạn xác minh:** backend localhost:5000 không kết nối được trong lần kiểm tra. Browser dùng dữ liệu fixture và tài khoản giả trong context riêng, không dùng tài khoản thật và không ghi vào database. Kết quả dưới đây xác minh UI, không thay thế kiểm thử nghiệp vụ với SQL/VNPay/SignalR thật.

## Route thực tế

| Nhóm | Route |
|---|---|
| Public | `/`, `/rooms`, `/rooms/:id`, `/blog`, `/blog/:id`, `/payment/result`, `/login`, `/register` |
| Tenant | `/tenant/profile`, `/tenant/favorites`, `/tenant/appointments`, `/tenant/rentals` |
| Landlord | `/landlord/dashboard`, `/landlord/posts`, `/landlord/posts/create`, `/landlord/posts/:id/edit`, `/landlord/appointments`, `/landlord/rooms`, `/landlord/contracts` |
| Admin | `/admin/dashboard`, `/admin/users`, `/admin/rooms`, `/admin/posts`, `/admin/posts/approval`, `/admin/categories`, `/admin/amenities`, `/admin/reports` |
| Fallback | `*`: 404 |

Có 27 route cụ thể và một route wildcard. Kiểm thử dùng 30 trường hợp: các route trên với ID fixture, 404, và ba tab `requests/contracts/bills` của trang hợp đồng chủ trọ.

## A. Lỗi Đã Phát Hiện

| Route/khu vực | Lỗi | Nguyên nhân | File | Xử lý |
|---|---|---|---|---|
| Toàn frontend | Padding/margin Tailwind mất hiệu lực; đo `.p-6` được 0px | Reset `* { padding:0; margin:0 }` ngoài layer ghi đè utility | [src/index.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/index.css) | Đưa base reset vào layer; giữ Tailwind preflight |
| Form Admin | Modal danh mục có thể nằm dưới sidebar | Modal cũ z-index 10, sidebar 100 | [src/pages/admin/AdminManagementContent.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/admin/AdminManagementContent.tsx) | Modal portal dùng z-index 2000 |
| Hợp đồng, sự cố, đánh giá | Modal không cùng chuẩn lớp hiển thị/cuộn | Modal inline z-50; một số không giới hạn chiều cao | [src/components/common/Modal.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Modal.tsx) | Giới hạn theo dvh, cuộn trong panel, header/X, focus/ESC |
| Modal dùng chung | Modal đóng có thể làm mất khóa cuộn của modal khác | Cleanup đặt lại overflow kể cả component không mở | [src/components/common/Modal.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Modal.tsx) | Chỉ khóa/phục hồi khi thực sự mở |
| Header người dùng | Mobile không có chuông thông báo | Chuông nằm trong nhóm actions bị ẩn | [src/components/common/Header.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Header.tsx) | Tách chuông khỏi nhóm desktop |
| Trang chủ | Nhãn nổi bật/trái tim không gắn đúng ảnh; trái tim không thao tác | Element absolute ngoài khung ảnh tương đối; handler rỗng | [src/components/room/RoomCard.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/RoomCard.tsx) | Gắn trong ảnh; gọi favoriteService sẵn có |
| Trang chủ | Ảnh tin mới kéo chiều cao card khi chuẩn hóa spacing | Chiều cao ảnh phụ thuộc kích thước nội tại | [src/pages/public/HomePage.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/public/HomePage.css) | Khung ảnh 148px; object-fit; nội dung có min-height |
| Đặt lịch | Nút xác nhận ngoài form không đi qua native validation | Gọi handler từ onClick ở footer | [src/components/room/BookingModal.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/BookingModal.tsx) | Submit trong form; validation từng field |
| Quản lý phòng | Field chỉ có placeholder, không rõ đơn vị; chưa khóa khi lưu | Form viết riêng, thiếu trạng thái gửi | [src/pages/landlord/LandlordRoomManagementPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/landlord/LandlordRoomManagementPage.tsx) | Label, đơn vị, required/min, lỗi inline và loading |
| Quản lý phòng | Bộ lọc “tạm ngưng” gửi giá trị giữ chỗ | Option dùng 2 trong khi trạng thái tạm ngưng là 3 | [src/pages/landlord/LandlordRoomManagementPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/landlord/LandlordRoomManagementPage.tsx) | Hai option riêng; giữ nguyên enum/backend |
| Admin danh mục | Tìm kiếm không lọc dữ liệu | API danh mục không nhận keyword và client chưa lọc | [src/pages/admin/AdminManagementContent.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/admin/AdminManagementContent.tsx) | Lọc danh sách đã tải; phân trang 10 dòng |
| Admin báo cáo | Lỗi cập nhật trạng thái có thể thành promise không xử lý | onChange await API không catch | [src/pages/admin/AdminManagementContent.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/admin/AdminManagementContent.tsx) | Dùng hành động có guard/loading/catch |
| Profile/yêu thích/thuê phòng | Lỗi API có thể trông như dữ liệu rỗng | Chỉ toast rồi bỏ trạng thái lỗi trang | [src/pages/tenant/TenantProfilePage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/tenant/TenantProfilePage.tsx) | PageState lỗi/thử lại; không giả báo rỗng |
| Đăng/sửa tin | Lỗi React đọc attribution; cả form bị ErrorBoundary thay thế | Nhiều tileerror đồng thời tăng providerIndex vượt mảng | [src/components/map/TileLayerWithFallback.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/map/TileLayerWithFallback.tsx) | Chỉ chuyển một lần mỗi nguồn; thất bại hết thì báo lỗi bản đồ |
| Admin typography | Heading vẫn dùng font/kích thước cũ sau CSS chung | CSS lazy page có cùng specificity và nạp sau | [src/styles/ui.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/styles/ui.css) | Selector có phạm vi cụ thể, typography cùng hệ thống |

Các gia cố bổ sung: main và sidebar dùng chung biến chiều rộng; drawer có backdrop/ESC/giữ focus; menu đóng không nhận focus; text dài co giãn; popover không vượt mobile; table chỉ cuộn trong wrapper; chat vừa màn hình ngắn; footer tránh đè nút chat. Không thêm `overflow-x:hidden` trên body.

## B. Form Đã Kiểm Tra

“Đạt” ở ba cột màn hình chỉ đánh giá bố cục. Không có phép thử ghi dữ liệu thật.

| Form/thao tác | Desktop | Tablet | Mobile | Validation/feedback | Kết quả |
|---|---|---|---|---|---|
| Đăng nhập | Đạt | Đạt | Đạt | Thiếu thông tin: lỗi inline; nút gửi khóa khi đang gửi | Browser thử form rỗng; không gọi đăng nhập thật |
| Đăng ký | Đạt | Đạt | Đạt | Tên/email/điện thoại/mật khẩu/xác nhận/đồng ý | Browser kiểm tra sáu thông báo lỗi |
| Tìm kiếm trang chủ/danh sách | Đạt | Đạt | Đạt | Label, select đúng kích thước | Browser kiểm tra link ngân sách và query |
| Thêm/sửa danh mục, tiện ích | Đạt | Đạt | Đạt | Tên required/trim; lỗi API inline; guard gửi | Mở modal, Tab/ESC; lọc và phân trang kiểm thử |
| Duyệt/từ chối tin | Đạt | Đạt | Đạt | Từ chối có lý do required; khóa thao tác | Browser mở modal từ chối; lưu thật chưa kiểm tra |
| Cập nhật trạng thái báo cáo | Đạt | Đạt | Đạt | Catch lỗi và khóa thao tác | Rà soát mã; không cập nhật database |
| Tạo/sửa bài đăng | Đạt | Đạt | Đạt | Lỗi từng field, giá/diện tích >0, người >=1 | Browser form rỗng, trang sửa, bản đồ mất mạng |
| Tạo/sửa phòng | Đạt | Đạt | Đạt | Tên/địa chỉ, giá/diện tích/số người, không âm | Rà soát validation và browser bố cục; lưu thật chưa kiểm tra |
| Duyệt yêu cầu/thiết lập cọc | Đạt | Đạt | Đạt | Tiền >0, hạn thanh toán, loading/error | Browser mở/đóng modal |
| Tạo hợp đồng | Đạt | Đạt | Đạt | Ngày kết thúc sau bắt đầu; tiền thuê >0 | Browser mở/đóng; không tạo hợp đồng thật |
| Tạo hóa đơn | Đạt | Đạt | Đạt | Chọn hợp đồng; chỉ số mới >= cũ; tổng tiền hiện có | Browser modal và cuộn ở màn hình ngắn |
| Sửa hóa đơn | Đạt | Đạt | Đạt | Nạp dữ liệu bill; không âm; chỉ số mới >= cũ | Browser modal; payload cũ giữ nguyên |
| Đặt lịch xem | Đạt | Đạt | Đạt | Ngày/giờ tương lai, điện thoại; lỗi từng field | Browser thử form rỗng; không gửi lịch thật |
| Yêu cầu thuê | Đạt | Đạt | Đạt | Nội dung giữ lại khi lỗi; submit khóa | API 400 giả lập; lần submit thứ hai không tạo request |
| Hồ sơ cá nhân | Đạt | Đạt | Đạt | Tên/điện thoại; lỗi dưới field và lỗi API | Browser thử bỏ tên |
| Đổi mật khẩu | Đạt | Đạt | Đạt | Mật khẩu hiện tại/mới/xác nhận | Browser thử ba lỗi; không đổi mật khẩu thật |
| Sự cố/đánh giá | Đạt | Đạt | Đạt | Required ở sự cố; chọn sao có nhãn; khóa gửi | Browser modal, label, Tab/ESC |
| Chat | Đạt | Đạt | Đạt | Nội dung không rỗng; khóa gửi; giữ khi lỗi | Kiểm tra khung; không xác minh truyền tin thật |

Các xác nhận/hủy và prompt nghiệp vụ có sẵn vẫn dùng hộp thoại của browser, không tự đổi quy trình quyết định.

## C. Responsive

| Width | Public | Tenant | Landlord | Admin |
|---|---|---|---|---|
| 1440px | Đạt | Đạt | Đạt | Đạt |
| 1200px | Đạt | Đạt | Đạt | Đạt |
| 1024px | Đạt | Đạt | Đạt | Đạt |
| 768px | Đạt | Đạt | Đạt | Đạt |
| 375px | Đạt | Đạt | Đạt | Đạt |

- 150 phép đo trang/viewport; kiểm tra document scrollWidth và phần tử vượt viewport, loại trừ vùng bảng được phép cuộn.
- 20 nhóm kiểm tra thao tác (4 vai trò x 5 kích thước), mỗi nhóm chứa nhiều thao tác.
- Modal ở mobile kiểm tra thêm chiều cao 560px, lớp hiển thị, khóa cuộn, label và Tab/ESC.
- Kiểm tra phân trang tài khoản 14 bản ghi: trang hai có 4 dòng.
- Kiểm tra mất mạng mọi nguồn nền bản đồ: form đăng tin còn tồn tại, có trạng thái lỗi và không sập React.
- Ảnh chụp dưới đây dùng dữ liệu mô phỏng; ảnh/tên/giá không phải bản ghi được thêm vào hệ thống.

## D. File Chỉnh Sửa

| File | Lý do |
|---|---|
| [src/index.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/index.css) | Đưa reset vào CSS base layer; trả lại ưu tiên cho padding/margin của Tailwind. |
| [src/main.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/main.tsx) | Nạp stylesheet chung cho các điều chỉnh UI. |
| [src/styles/ui.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/styles/ui.css) | Chuẩn hóa typography, chiều rộng, tab, form, bảng, popover và các breakpoint. |
| [src/layouts/PublicLayout.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/layouts/PublicLayout.tsx) | Bổ sung điều hướng tài khoản người thuê, giữ Header/Main/Footer hiện có. |
| [src/layouts/AdminDashboardLayout.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/layouts/AdminDashboardLayout.tsx) | Drawer mobile, backdrop, ESC, focus, inert khi đóng và trạng thái menu hiện hành. |
| [src/layouts/AdminDashboardLayout.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/layouts/AdminDashboardLayout.css) | Dùng cùng biến chiều rộng sidebar/main; topbar co giãn; khoảng cách nội dung. |
| [src/components/common/Header.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Header.tsx) | Menu tài khoản hoạt động bằng click/ESC; tên dài rút gọn; chuông thông báo trên mobile. |
| [src/components/common/Footer.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Footer.tsx) | Footer gọn, cùng thương hiệu; liên kết khám phá/khu vực và tel/mailto hoạt động. |
| [src/components/common/Input.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Input.tsx) | Label gắn input bằng ID; lỗi gắn aria-describedby/aria-invalid. |
| [src/components/common/Modal.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/Modal.tsx) | Portal, z-index, chiều cao theo viewport, X, ESC, giữ focus và phục hồi cuộn. |
| [src/components/common/NotificationBell.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/common/NotificationBell.tsx) | Popover theo viewport, danh sách cuộn, ESC, nút có nhãn truy cập. |
| [src/components/chat/ChatDrawer.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/chat/ChatDrawer.tsx) | Khung chat vừa màn hình ngắn; nội dung xuống dòng; giữ tin nhắn khi gửi thất bại; khóa gửi khi đang gửi. |
| [src/components/map/TileLayerWithFallback.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/map/TileLayerWithFallback.tsx) | Chặn nhiều tileerror cùng tăng chỉ số nguồn; hiển thị lỗi nền bản đồ thay vì làm sập trang. |
| [src/components/room/RoomCard.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/RoomCard.tsx) | Card dùng ảnh bài đăng, giá/diện tích/địa chỉ; nút lưu gọi dịch vụ yêu thích hiện có. |
| [src/components/room/RoomImageGallery.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/RoomImageGallery.tsx) | Lightbox qua portal; ESC/phím mũi tên, khóa cuộn, focus và nhãn thumbnail. |
| [src/components/room/RoomInformation.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/RoomInformation.tsx) | Grid thông tin không chia bốn cột quá sớm trong cột nội dung hẹp. |
| [src/components/room/RoomLocation.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/RoomLocation.tsx) | Nhãn bản đồ phù hợp Leaflet, không gọi nhầm Google Maps. |
| [src/components/room/LandlordContactCard.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/LandlordContactCard.tsx) | Tên dài không ép ngang; bỏ khẳng định xác thực không có dữ liệu chứng minh; khóa gọi nếu thiếu số. |
| [src/components/room/BookingModal.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/BookingModal.tsx) | Form nằm trong modal chung; lỗi ngay dưới field; kiểm tra ngày/giờ tương lai và số điện thoại; giữ payload cũ. |
| [src/components/room/RentalRequestModal.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/components/room/RentalRequestModal.tsx) | Modal chung, phản hồi lỗi inline, giữ nội dung và trạng thái đang gửi. |
| [src/pages/public/HomePage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/public/HomePage.tsx) | Thêm lối vào theo ngân sách/khu vực/danh mục, tin nổi bật/tin mới/blog từ API hiện có. |
| [src/pages/public/HomePage.css](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/public/HomePage.css) | Bố cục trang chủ responsive; ảnh/card có kích thước ổn định; spacing và hierarchy. |
| [src/pages/public/RoomListPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/public/RoomListPage.tsx) | Thêm nhãn truy cập cho ô tìm kiếm. |
| [src/pages/admin/AdminManagementContent.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/admin/AdminManagementContent.tsx) | Modal danh mục/tiện ích/từ chối, tìm kiếm danh mục, phân trang, khóa thao tác, catch lỗi cập nhật báo cáo. |
| [src/pages/auth/LoginPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/auth/LoginPage.tsx) | Validation inline, nhãn lỗi truy cập được và chặn gửi khi đang đăng nhập; không đổi cơ chế đăng nhập. |
| [src/pages/auth/RegisterPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/auth/RegisterPage.tsx) | Validation inline đầy đủ, giữ dữ liệu khi lỗi, khóa nút gửi; payload và vai trò đăng ký giữ nguyên. |
| [src/pages/landlord/CreatePostPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/landlord/CreatePostPage.tsx) | Label/ID, lỗi dưới field, spacing form, khoảng trống đơn vị tiền/diện tích và chống gửi khi đang lưu. |
| [src/pages/landlord/LandlordRoomManagementPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/landlord/LandlordRoomManagementPage.tsx) | Label/đơn vị, form một cột ở tablet/mobile, validation, loading/error và phân biệt giữ chỗ/tạm ngưng. |
| [src/pages/landlord/LandlordContractsPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/landlord/LandlordContractsPage.tsx) | Chuyển bốn modal sang Modal chung; nhãn field, kiểm tra chỉ số/ngày, loading/error và chống gửi lặp. |
| [src/pages/tenant/TenantAppointmentsPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/tenant/TenantAppointmentsPage.tsx) | Áp dụng container UI thống nhất cho người thuê. |
| [src/pages/tenant/TenantFavoritesPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/tenant/TenantFavoritesPage.tsx) | Container chung; phân biệt lỗi tải với danh sách rỗng; nút thử lại. |
| [src/pages/tenant/TenantProfilePage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/tenant/TenantProfilePage.tsx) | Label/autocomplete, validation inline, lỗi tải/thử lại và chống gửi lặp. |
| [src/pages/tenant/TenantRentalsPage.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/pages/tenant/TenantRentalsPage.tsx) | Tab responsive; modal sự cố/đánh giá chung, nhãn field/sao, lỗi tải và trạng thái đang gửi. |
| [src/routes/AppRoutes.tsx](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/src/routes/AppRoutes.tsx) | Trang 404 có thông báo và lối về trang chủ; không đổi đường dẫn hay phân quyền. |
| [scripts/ui-audit.mjs](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/frontend/scripts/ui-audit.mjs) | Kiểm tra browser bằng fixture cô lập, năm kích thước, modal/form/menu/console và ảnh chụp. |

Không liệt kê các file backend, services/constants và LandlordPostsPage đã thay đổi từ công việc trước, vì không chỉnh chúng trong đợt UI này.

## E. Build Và Lint

- `npm run build`: thành công với TypeScript và Vite.
- `npm run lint`: thành công, không có lỗi ESLint.
- Vite có thông tin profiling thời gian plugin trong một lượt build; không phải lỗi compile.
- Browser test: [kết quả JSON](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/final/results.json).
- Script kiểm thử chỉ là công cụ phát triển, không được nạp vào ứng dụng production.

Chạy lại từ thư mục frontend khi dev server đang chạy:

```powershell
$env:UI_AUDIT_NODE_MODULES='C:\Users\huanp\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
node scripts/ui-audit.mjs final
```

Có thể dùng Playwright của máy thay cho biến trên nếu đã cài. Script dùng Edge hệ thống; không thêm dependency vào package.json.

## F. Console Và Ảnh

- Không còn lỗi React, duplicate key, unhandled page error hoặc React warning trong lượt kiểm tra ổn định.
- SignalR 503 là lỗi được cố ý mô phỏng do không dùng backend trong bài test, được ghi riêng trong JSON; không coi là kết nối thời gian thực đã thành công.
- HTTP 400 khi gửi yêu cầu thuê được cố ý mô phỏng để kiểm tra lỗi inline/chống gửi lặp.
- Lỗi tải tile được cố ý tạo trong kiểm thử offline; trang chuyển sang trạng thái bản đồ không khả dụng.
- Kiểm tra ảnh chụp chờ img tải xong và ghi trạng thái imageCheck; cần Internet cho ảnh và nền bản đồ bên ngoài.

Ảnh minh họa:

- [Trang chủ desktop](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/final/Public-_-1440.png)
- [Trang chủ mobile](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/final/Public-_-375.png)
- [Chi tiết phòng mobile](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/final/Public-_rooms_1-375.png)
- [Danh mục Admin mobile](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/final/Admin-_admin_categories-375.png)
- [Đặt lịch ở màn hình ngắn](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/final/Tenant-dialog-Đặt_lịch_xem_phòng-375.png)

## G. Việc Còn Lại

1. Cần chạy backend/SQL để kiểm tra bằng tài khoản và dữ liệu thật: tạo/sửa, phân quyền, duyệt yêu cầu, cọc, hợp đồng, hóa đơn, thanh toán và xuất file. Chưa khẳng định nghiệp vụ end-to-end hoàn thành.
2. Cần kiểm tra SignalR thật cho chat và thông báo, cùng callback VNPay Sandbox; không đổi những tích hợp này.
3. Link `/forgot-password`, `/terms`, `/privacy` trong trang auth chưa có route thực tế. Không tự tạo quy trình khôi phục mật khẩu hoặc nội dung chính sách.
4. Header hiện còn link lọc bằng `type=house/apartment/share`, trong khi RoomList đọc `categoryId`. Đây là phần ánh xạ bộ lọc cần xác nhận theo danh mục thật; các lối vào danh mục mới trên trang chủ đã dùng `categoryId`.
5. Ô điện thoại trong đặt lịch được kiểm tra ở UI nhưng DTO đặt lịch hiện không có trường điện thoại. Payload được giữ nguyên; cần xác nhận nguồn số liên hệ từ hồ sơ trước khi sửa API.
6. Thông tin liên hệ footer kế thừa dữ liệu hiển thị cũ; cần chủ đồ án xác nhận số điện thoại/email khi triển khai thật.

## Bổ Sung: Địa Chỉ Và Bản Đồ Hai Chiều

Phần bổ sung này được thực hiện theo yêu cầu tiếp theo, độc lập với phạm vi audit UI ban đầu ở trên.

- Áp dụng cho tạo/sửa tin đăng và thêm/sửa phòng trong trang Quản lý phòng.
- Nhập xong rồi rời ô địa chỉ/phường/quận/tỉnh sẽ tìm vị trí. Có nút tìm lại, danh sách kết quả tương tự và nút định vị hiện tại.
- Bấm bản đồ hoặc kéo ghim sẽ cập nhật tọa độ ngay, rồi tra địa chỉ cụ thể/phường/quận/tỉnh qua `GET /api/location/reverse?lat=...&lng=...`.
- Không tra cứu theo từng phím gõ, không tra cứu ngược rồi tự tìm xuôi lặp lại, không tự thay ghim đã lưu khi mở trang sửa.
- Kết quả cũ bị hủy khi đổi địa chỉ/chọn điểm mới/reset form; nút lưu bị khóa trong lúc đang tra cứu.
- Đổi địa chỉ mà không có tọa độ mới sẽ bỏ ghim cũ trong backend; chỉ sửa nội dung/tên phòng vẫn giữ ghim. Trường phường/quận trống được lưu đúng, không giữ lại địa chỉ hành chính cũ.
- Không thay schema SQL, không cần chạy script database mới cho chức năng này.
- Build frontend, lint và build backend đạt; bộ test backend đạt 22/22. Test trình duyệt: 36 tình huống trên 1440/768/375px, gồm payload lưu tin/phòng, lỗi mạng, race condition, reset và ảnh ghim thực tế. [Kết quả](C:/Users/huanp/Downloads/12523112_PhanDuySon_DoAn4/RoomRentalSystem/docs/ui-audit/location-picker/results.json).
- Các bài test dùng HTTP fixture/fake provider và database InMemory, không tạo/sửa dữ liệu SQL thật. Cần khởi động lại backend để endpoint mới hoạt động và kiểm tra với dữ liệu thật.

Backend dùng chung một hàng đợi giới hạn yêu cầu tới provider, cache tối đa 512 kết quả trong 6 giờ, timeout HTTP 10 giây và User-Agent nhận diện ứng dụng. Giới hạn áp dụng cho một tiến trình backend; triển khai nhiều tiến trình cần bộ giới hạn dùng chung hoặc provider riêng.

Provider hiện kế thừa Nominatim đã có trong project. [Chính sách sử dụng](https://operations.osmfoundation.org/policies/nominatim/) giới hạn toàn ứng dụng tối đa 1 yêu cầu/giây, cấm autocomplete theo phím gõ và yêu cầu attribution. Không gửi dữ liệu riêng tư/bí mật; chỉ tra địa chỉ trọ dự định công khai. Có thể đổi URL dịch vụ tương thích bằng `Geocoding:BaseUrl` trong cấu hình backend mà không sửa code.

Theo [tài liệu tra cứu ngược](https://nominatim.org/release-docs/latest/api/Reverse/), địa chỉ mô tả đối tượng gần nhất trong dữ liệu OSM, không bảo đảm có số nhà hoặc đủ cấp hành chính. Backend giữ nguyên tọa độ người dùng đã chọn, không thay bằng tọa độ đối tượng gần nhất; chủ trọ cần kiểm tra và bổ sung địa chỉ thiếu trước khi lưu.
