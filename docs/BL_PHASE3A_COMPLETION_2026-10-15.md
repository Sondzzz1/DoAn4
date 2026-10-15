# Giai đoạn 3A: Kết thúc hợp đồng, trả phòng và tất toán

Ngày 15/10/2026. Triển khai sau xác nhận của người dùng: **đồng ý 5 bảng và quy trình đề xuất** trong `BL_PHASE3A_AUDIT_AND_APPROVAL_2026-10-15.md`.

**Kết quả: source và QA đã hoàn thiện trong phạm vi 3A đã duyệt. Backend 317/317 PASS; HTTP/UI 20/20 PASS; backend Release build, frontend lint/build PASS. Chưa triển khai database chính. VNPay real sandbox bank transaction/refund: NOT VERIFIED.**

Không commit/push, không chạy SQL lên RoomRentalDB, không chỉnh sửa dữ liệu thật, không chuyển sang Giai đoạn 3B/4.

## 1. Hiện Trạng Luồng Hợp Đồng Sau Sửa

1. Room -> Post -> Admin duyệt -> Tenant tìm/đặt lịch/gửi yêu cầu -> Landlord duyệt và cọc -> thanh toán cọc -> xác nhận cọc -> tạo và ký hợp đồng: giữ luồng hiện có, thêm kiểm tra không cấp lại phòng/cọc chưa xử lý.
2. Tạo contract đồng thời tạo hồ sơ kết thúc, ghi lịch sử tạo/ký. Hai bên ký vẫn dẫn đến PendingStart hoặc Active theo ngày Việt Nam; worker ghi nhận bắt đầu có hiệu lực.
3. Tenant hoặc Landlord đúng hợp đồng đề nghị kết thúc sớm, lưu lý do, requester, thời điểm đề nghị. Người còn lại đồng ý/từ chối; người gửi có thể rút khi chưa được đồng ý.
4. Đến ngày hết thời hạn tự nhiên hoặc mốc chấm dứt đã đồng thuận: contract chuyển Expired/Terminated nhưng **không** tự Available, không tự ghi người ở cùng đã rời phòng, không tự hoàn cọc.
5. Hai bên lập/xác nhận cùng revision biên bản Returned hoặc NeverTaken. Returned có ngày trả thực tế, tình trạng, điện/nước cuối, hư hỏng/chứng cứ. NeverTaken phải có chứng cứ và không có lịch sử bill/cư trú mâu thuẫn, không tạo chỉ số/ngày trả giả.
6. Bàn giao đủ hai xác nhận mới ghi người ở cùng Left theo ngày thực tế và tính lại số cư trú. Quảng cáo/yêu cầu ở ghép đã được đóng; không nhận thêm người khi hồ sơ đang xử lý bàn giao.
7. Landlord chốt bill cuối/công nợ rồi lập phương án tất toán: bill còn nợ được lấy từ server, thiệt hại có chứng cứ, tiền cọc khớp snapshot hợp đồng và đúng hai bên. Không có quyền khấu trừ chỉ từ vai trò Landlord.
8. Hai bên đồng ý phương án; bên trả báo chứng từ và bên nhận xác nhận thực nhận. Không có chuyển tiền thì không tạo chứng từ chuyển tiền giả.
9. Hoàn tất phương án mới thanh toán các bill còn mở thuộc phương án bằng `ContractSettlement`. Không sửa bill đã Paid, không tạo Succeeded VNPay giả, không sửa Amount/TransactionCode/PaidAt gốc để giả hoàn tiền.
10. Landlord xác nhận sẵn sàng sau khi mọi hồ sơ/quyền/cư trú/công nợ/cọc/thanh toán cần đối soát đều được xử lý. Lúc này mới cho Room Available; nếu đang bảo trì/TemporarilyUnavailable thì giữ tạm ngưng.

Luồng trước hợp đồng của Giai đoạn 2 vẫn riêng: `DepositRefundBLL.EnsureBeforeContractAsync` không bị nới điều kiện để nhận cọc đã gắn contract.

## 2. Lỗi Đã Xác Nhận Và Kết Quả

| Mã / lỗi | Kết quả trong source và QA |
|---|---|
| BL-005 chấm dứt là mở phòng ngay | **FIXED**: bỏ nhánh chấm dứt đơn phương/mở phòng; có đề nghị, đồng thuận, bàn giao, chốt nợ, tất toán và release riêng |
| BL-014 expiry là mở phòng/người ở cùng tự rời | **FIXED**: worker chỉ kết thúc hiệu lực, đóng tuyển ở ghép và ghi history; không Available hoặc Left chỉ vì quá ngày |
| BL-014 GET hợp đồng ghi dữ liệu | **FIXED**, giữ bản sửa trước; GET list/detail/hồ sơ chỉ đọc, không gọi reconcile |
| Lý do/ngày/người chấm dứt không được lưu | **FIXED**: EndRequest + hồ sơ + event append-only theo command |
| Room Available bị lệch có thể cấp lại dù còn hồ sơ terminal | **FIXED**: chặn search, thao tác cấp phòng, lịch xem, đổi trạng thái thủ công, callback cấp quyền và các nhánh release liên quan |
| Cọc Confirmed bị coi là số dư có thể dùng lại | **FIXED** cho luồng liên quan: kiểm tra settlement đang mở/hoàn tất, snapshot và đúng hai bên; không tái sử dụng cọc đã đưa vào tất toán |
| Bill cuối dùng UpdatedAt làm ngày kết thúc | **FIXED trong luồng 3A**: đọc LastRentalDate chuyên biệt; dữ liệu cũ chưa có mốc được chặn để xác minh, không suy đoán từ UpdatedAt |
| Thu bill riêng trong lúc phương án vẫn được coi đã đồng ý | **FIXED**: thay đổi trước chuyển tiền làm phương án Superseded, bỏ chốt nợ và tăng revision; đã báo tiền/hoàn tất thì chặn sửa/thu lại |
| Tranh chấp sau khi báo chuyển tiền bị kẹt hoặc có nguy cơ chuyển lần hai | **FIXED trong policy đã triển khai**: hai bên đồng ý lại cùng phương án; giữ chứng từ/mốc báo tiền, không báo chuyển lần hai; Admin không ký thay |
| Nhiều hồ sơ terminal cùng phòng chặn lẫn nhau mãi | **FIXED**: xác minh mọi hồ sơ trước; một lần release xử lý các hồ sơ đã đủ bàn giao/tất toán cùng phòng, nhưng không mở qua quyền thuê Active khác |
| Cọc Confirmed của contract chưa ký đã tất toán vẫn bị coi là giữ chỗ | **FIXED**: Room/closure/refund nhận biết ledger Completed; không chặn đổi trạng thái phòng hợp lệ hoặc hoàn cọc của lượt thuê tiếp theo chỉ vì cọc lịch sử |
| UI chấm dứt báo ngay phòng còn trống | **FIXED**: bỏ nút/toast cũ, chuyển sang modal hồ sơ và thao tác backend cho phép |

Các chữ FIXED không có nghĩa dữ liệu thật đã được nâng cấp, Admin được quyền quyết định tranh chấp pháp lý, hoặc hệ thống đã chứng nhận giao dịch ngân hàng.

## 3. State Machine Đã Triển Khai

| Trục | Trạng thái / chuyển tiếp |
|---|---|
| Contract | Giữ nguyên mã 0..5; PendingSignature -> PendingStart/Active -> Expired/Terminated; Cancelled cho hợp đồng chưa đủ ký bị hủy |
| Đề nghị kết thúc | Requested=0 -> Agreed=1 -> Effective=5; hoặc Rejected=2 / Withdrawn=3 / Superseded=4 |
| Bàn giao | NotRecorded=0; Draft=1 dự phòng; lưu biên bản để xác nhận ở PendingConfirmation=2 -> Confirmed=3; bất đồng -> Disputed=4 |
| Tất toán | Draft=0 -> Proposed=1 -> Agreed=2 -> TransferReported=3 -> Completed=4; Disputed=5; phiên bản thay thế Superseded=6 |
| Đồng thuận lại khi tranh chấp | Disputed giữ nguyên số tiền/chứng từ; hai xác nhận mới đưa về Agreed hoặc TransferReported. Không đổi số tiền sau khi đã báo chuyển tiền |
| Release | Trạng thái tính từ dữ liệu: còn blocker / đủ điều kiện / đã Landlord xác nhận ReleasedAtUtc |

EndDate vẫn là ngày thuê cuối inclusive theo Việt Nam. Hết hạn từ 00:00 ngày tiếp theo; mốc UTC và LastRentalDate tách riêng. Ngày trả thực tế không bị gán bằng ngày hết hạn. Mốc early termination ở tương lai được worker áp dụng khi đến hạn, không kết thúc ngay lúc người còn lại đồng ý.

Hủy PendingSignature quá hạn không đưa request trở lại Approved để dùng lại cọc. Room vẫn giữ; cần xác nhận NeverTaken và tất toán. Các dữ liệu terminal lịch sử thiếu mốc không được tự tạo ngày kết thúc là hôm nay: cần đề nghị xác minh ngày, chứng cứ và hai bên đồng ý; không đổi lại mã trạng thái lịch sử.

Giá trị Contract.CreatedAt mới được ghi UTC để khớp worker timeout; không migration/rewrite CreatedAt cũ. Các timestamp nghiệp vụ mới dùng UTC, UI hiển thị Việt Nam.

## 4. File Thay Đổi

### Backend mới

- `backend/RoomRental.BackEnd/Models/ContractClosure.cs`: đúng 5 entity và các mã trạng thái riêng, không đổi enum cũ.
- `backend/RoomRental.BackEnd/DAL/Configurations/ContractClosureConfiguration.cs`: FK RESTRICT, tiền/chỉ số/JSON/đồng thuận/chứng từ CHECK, unique index, RowVersion.
- `backend/RoomRental.BackEnd/DTO/Rental/ContractClosureDto.cs`: command, validation và view theo quyền/trạng thái.
- `backend/RoomRental.BackEnd/BLL/ContractClosureBLL.cs`: đề nghị, bàn giao, chốt nợ, phương án/đồng thuận/chứng từ/nhận tiền, tranh chấp, hỗ trợ Admin, release.
- `backend/RoomRental.BackEnd/BLL/ContractClosurePolicy.cs`: schema guard, bảo vệ quyền thuê, mốc ngày, invalidation bill, history và reconcile.
- `backend/RoomRental.BackEnd/Controllers/ContractClosureController.cs`: API hồ sơ và command; claim tài khoản phải là số dương, không dùng fallback Admin=0.
- `backend/RoomRental.BackEnd/SQL/09_ContractClosure.sql`: upgrade additive được xuất từ EF model và kiểm tra bằng SQL Server QA.

### Backend tích hợp tối thiểu

- `DAL/ApplicationDbContext.cs`, `Program.cs`: 5 DbSet và DI cho service mới.
- `BLL/RentalFeaturesBLL.cs`: tạo/ký/history, bỏ terminate đi tắt, worker/expiry giữ phòng, timeout chưa ký và thông báo đúng nội dung.
- `BLL/ContractOccupancyPolicy.cs`, `BLL/RoommateBLL.cs`: tách hiệu lực và cư trú; không tự ghi Left; chặn tuyển người khi bàn giao.
- `BLL/RoomBLL.cs`, `BLL/PostBLL.cs`, `BLL/ViewingAppointmentBLL.cs`: bảo vệ dữ liệu lệch Available/terminal chưa xử lý, giữ URL chi tiết có trạng thái không thuê được.
- `BLL/MonthlyBillBLL.cs`: ngày thuê cuối chuyên biệt, giới hạn chỉ số bàn giao, invalidation và đóng băng quyết toán; không sửa thuật toán tính tiền/prorate.
- `BLL/PaymentBLL.cs`, `BLL/DepositRefundBLL.cs`: guard cấp quyền/thu bill/release; không viết lại thuật toán ký/ACK/dedup/ledger Giai đoạn 2.

### Frontend

- `frontend/src/services/contractClosureService.ts`: API và types.
- `frontend/src/components/rental/ContractClosurePanel.tsx`, `ContractClosure.css`: modal dùng chung, form theo operation, số tiền/chứng từ/nợ/history, lỗi rõ và giữ input.
- `frontend/src/pages/tenant/TenantRentalsPage.tsx`, `frontend/src/pages/landlord/LandlordContractsPage.tsx`: mở hồ sơ từ contract có sẵn, refresh dữ liệu sau thành công, bỏ thao tác terminate/toast sai.
- `frontend/src/components/payment/PaymentSafetyPanel.tsx`: chỉ thêm tra cứu/hỗ trợ contract cho Admin trong trang đối soát hiện có, không thêm trang quản trị giả.

### Tests và báo cáo

- `backend/RoomRental.BackEnd.Tests/ContractClosureTests.cs`: 25 case mới InMemory.
- `backend/RoomRental.BackEnd.Tests/ContractClosureSqlTests.cs`: 2 case SQL integration gồm nhiều bước và đồng thời, xuất SQL 09 bằng model hiện hành.
- `AuditRegressionTests.cs`, `RoomStatusTests.cs`, `RoommateTests.cs`: đổi expected tương ứng policy mới và sửa fixture notification thành phòng thực sự hết quyền giữ chỗ. Không xóa test để giấu failure.
- Báo cáo này và banner liên kết ở báo cáo xin duyệt trước.

Trong phiên HEAD có commit **13cd2d5** từ bên ngoài ghi nhận công việc trước đó, sau HEAD ban đầu 3fc2511. Trợ lý không thực hiện commit/push, không reset/revert commit hoặc thay đổi đó; công việc 3A hiện vẫn ở working tree.

## 5. Phê Duyệt Và Phạm Vi

- Đã dùng đúng phê duyệt 5 bảng, hai bên đồng ý chấm dứt/khấu trừ, bàn giao và tất toán trước khi mở phòng.
- Không thêm bảng thứ sáu hoặc cột initial meter vào contract; không sửa schema bảng tiền/hợp đồng cũ.
- Không dùng Admin mediation để giả xác nhận Tenant/Landlord, tự chọn số tiền khấu trừ hay gọi ngân hàng.
- Các tích hợp Roommate/bill/Payment/Room là phần tối thiểu để bảo vệ quy trình, không triển khai toàn bộ BL-006/007/015/016.
- Không có schema bổ sung cần duyệt tiếp cho bản 3A này. **Triển khai database chính vẫn là công việc riêng chưa thực hiện**, đặc biệt SQL 07 hiện đang giới hạn QA.

## 6. Database Và Migration

5 bảng mới: `HoSoKetThucHopDong`, `YeuCauKetThucHopDong`, `PhuongAnTatToanHopDong`, `KhoanMucTatToanHopDong`, `LichSuKetThucHopDong`.

- Một hồ sơ/contract; một Requested/Agreed end request/closure; một phương án chưa Superseded trên mỗi closure và deposit, bao gồm cả Completed để không tất toán cọc hai lần.
- Phương án có revision và dòng bill/thiệt hại với snapshot, phân bổ cọc và chứng cứ; dữ liệu tiền server tính bằng decimal(18,2).
- Bốn bảng mutable có rowversion; event append-only qua application, có unique IdempotencyKey. Command dùng namespace `user:` để không chiếm key worker.
- Các cột thời gian mới là UTC datetime2, LastRentalDate là date. Application gán thời gian server, không nhận requester/confirmer tùy ý từ client.
- FK RESTRICT, không cascade xóa lịch sử tài chính.
- SQL 09: guard database, explicit opt-in main mặc định tắt; kiểm tra đầy đủ 3 bảng SQL 07 và 2 bảng SQL 08; transaction + schema lock; tạo nếu cả 5 chưa có, dừng nếu thiếu một phần hoặc schema/index/check/FK không khớp.
- SQL 09 không UPDATE/DELETE/DROP hoặc thêm cột bảng nghiệp vụ cũ. Test cố ý thay index **chỉ trên DB QA** để chứng minh verifier phát hiện; thao tác đó không nằm trong script upgrade.
- Script được tạo từ EF metadata, CHECK được SQL Server canonicalize qua bảng tạm QA khi xuất; không dựa tự suy đoán chuỗi IN/BETWEEN của SQL Server.

### Dữ liệu thật

Metadata SELECT xác nhận RoomRentalDB hiện có **0/10 bảng** thuộc SQL 07/08/09. Bằng chứng `main-schema-readonly.json`. Không sửa một dòng dữ liệu thật và không thực thi script nâng cấp tại đây.

**Không chạy ngay SQL 09 trên main rồi kỳ vọng đủ:** main đang thiếu prerequisite. `07_RoommateModule.sql` vẫn QA-only, chưa có gói triển khai main được review trong phạm vi này. Không xóa guard để chạy cho qua. Cần backup và chuẩn bị/review gói deploy main 07 -> 08 -> 09 riêng, sau đó mới đổi app/worker sang source mới. SQL 08/09 cũng cần opt-in rõ ràng khi người dùng chủ động deploy main.

Không backfill Released, Refunded, ngày trả, ngày kết thúc cũ hoặc người cư trú từ suy đoán. Nếu snapshot cọc không khớp, hồ sơ bị chặn đối soát thay vì sửa tiền lịch sử. Không rollback bằng DROP bảng ledger; không chạy đồng thời worker cũ có nhánh tự mở phòng với code mới.

## 7. API Và Giao Diện

Để tái sử dụng cùng validation/transaction và hạn chế route trùng, API thực tế được gom thành:

```text
GET  /api/hop-dong/{contractId}/ket-thuc
POST /api/hop-dong/{contractId}/ket-thuc/{operation}
```

Operations: request-end, agree-end, reject-end, withdraw-end, save-handover, confirm-handover, dispute-handover, finalize-bills, create-settlement, propose-settlement, agree-settlement, dispute-settlement, resume-settlement, report-refund, confirm-refund, report-extra, confirm-extra, complete-settlement, release, mediate.

DTO có IdempotencyKey, RowVersion hồ sơ, TargetId đề nghị/phương án và các trường cần cho operation. Backend tự xác định số tiền, actor và quyền, không tin nút frontend. Retry đúng key/nội dung trả kết quả đã lưu, không lặp chuyển tiền/release; key khác nội dung, stale version, sai bên hoặc sai trạng thái bị từ chối.

GET trả trạng thái đang lưu và AllowedOperations/ReleaseBlockers; không tạo hồ sơ hoặc tự chạy job. Worker tắt/lỗi có thể khiến trạng thái lưu chưa cập nhật; refresh GET không thay thế worker. API ký và Roommate kiểm tra ngày độc lập.

Alias terminate/cham-dut cũ trả **409 với hướng dẫn quy trình mới**, không âm thầm trả success khi chỉ tạo đề nghị. UI mới không gọi alias này.

Form dùng Modal hiện có, xử lý nhiều bước trong cùng hồ sơ, không điều hướng mất dữ liệu; bản sửa biên bản điền lại dữ liệu cũ/chứng cứ. Lỗi API giữ nội dung, hiển thị ngay gần nút xác nhận và cuộn tới lỗi; không phát success khi API từ chối. Hai bên chỉ thấy thao tác hợp lệ, Admin tra cứu mã contract và ghi chứng cứ hòa giải trong trang `/admin/payments`.

## 8. Kiểm Thử Thực Tế

| Hạng mục | Kết quả cuối |
|---|---|
| Baseline trước triển khai | 290 backend PASS từ phần audit/GET đã làm trước |
| Full backend cuối, InMemory + SQL QA | **317 PASS, 0 FAIL, 0 SKIP**; 27 case mới |
| Backend Release | **PASS, 0 warning / 0 error** |
| Frontend lint / production build | **PASS / PASS** |
| HTTP + Playwright dùng API/SQL thật | **20/20 PASS**, không mock response |
| Responsive | Tenant/Landlord 375/768/1440 và modal Admin 375; kiểm tra kích thước/overflow, mở ảnh chụp kiểm tra |
| SQL 09 từ schema cũ không có 5 bảng | Tạo additive PASS; snapshot các trường contract/room/request/cư trú/cọc/bill/payment đã kiểm tra không đổi |
| SQL 09 chạy lại | PASS, không tạo lại hoặc đổi dữ liệu cũ |
| Schema/index bị thay đổi | Dừng an toàn PASS; thiếu SQL 09 trả 503 rõ ràng PASS |
| SQL concurrent | Hai đề nghị/hai lệnh kết thúc chỉ một lệnh thay đổi hợp lệ; cùng key complete retry 200/200, một history, không quyết toán hai lần |
| Giai đoạn 2 | Toàn bộ tests IPN/refund/ledger/SQL concurrency trong full suite PASS; không viết lại expected giao thức VNPay |
| Git diff --check | PASS, chỉ cảnh báo CRLF của Windows |
| VNPay real sandbox bank transaction/refund | **NOT VERIFIED** |

E2E thực sự đi Room -> Post -> duyệt -> search filter/radius -> detail -> appointment -> rental request -> approve + cọc -> callback **tự ký QA** -> xác nhận cọc -> contract -> hai bên ký -> đề nghị kết thúc trên UI -> đồng ý -> bàn giao -> bill cuối -> đồng thuận khấu trừ -> báo/nhận hoàn -> tất toán -> release -> search lại có phòng. Có nhánh NeverTaken hoàn đủ, nhánh trả thêm và nhánh tranh chấp/đồng thuận lại sau báo chuyển tiền.

Đây không phải xác nhận đã đi qua trang ngân hàng, thẻ, OTP hoặc refund provider. Bill trong luồng kết thúc được xử lý bằng ContractSettlement, không được gọi là E2E trả bill bằng ngân hàng thực.

### 15 Tình Huống Bắt Buộc

| # | Tình huống | Bằng chứng đã chạy |
|---|---|---|
| 1 | Active chưa bàn giao | Unit + HTTP: không release; giữ Rented/cư trú |
| 2 | Hết hạn còn bill/nợ | Worker/guard + test bill: không mở phòng, chưa đủ điều kiện chốt/tất toán thì bị chặn |
| 3 | Hết hạn và đã bàn giao | Test riêng Natural expiry -> Expired -> hai bên bàn giao -> vẫn Rented -> bill cuối/tất toán -> release; giữ nguyên Expired lịch sử |
| 4 | Chấm dứt sớm có lý do | Unit + UI, có EndRequest/history/actor/date |
| 5 | Tenant không đúng contract | Unit + HTTP GET/mutation 403 |
| 6 | Landlord không đúng contract | Unit + HTTP GET/mutation 403 |
| 7 | Confirmed chưa tất toán | Unit/HTTP giữ Confirmed và Room bị chặn |
| 8 | Hoàn đủ sau contract | Unit + HTTP PendingStart/NeverTaken, không tạo bill/số đo giả |
| 9 | Khấu trừ hợp lệ | Unit + HTTP bill + damage evidence, hai bên đồng ý |
| 10 | Tranh chấp | Unit + HTTP giữ chặn; Admin không ép đồng ý; có hòa giải và đồng thuận lại cùng phương án |
| 11 | Hai request kết thúc đồng thời | SQL hai agree-end cùng version, một 200/một 409; chỉ kết thúc một lần |
| 12 | Worker chạy lại | Service xử lý worker được gọi lặp trong unit/SQL; không lặp ending event, không ghi Left/Available. Không đợi scheduler 5 phút trên preview |
| 13 | GET không giải phóng | Các regression GET cũ + unit/SQL/HTTP mới PASS |
| 14 | Đủ điều kiện mới cho thuê tiếp | HTTP release sau settlement và unit nhiều hồ sơ/quyền Active; search trở lại sau release |
| 15 | Stage 2 regression | Full suite giữ IPN/refund/ledger/financial safety PASS |

Boundary EndDate 31/10 -> 01/11 00:00 Việt Nam vẫn PASS. Không còn dùng expected “Expired là Available” hoặc “tất cả occupant phải Left” trong các test đã được thay policy.

### Bằng Chứng Và Lỗi Phát Hiện Khi Làm

Artifacts: `C:/Users/huanp/AppData/Local/Temp/contract-closure-implementation/` gồm `closure-final.trx`, `http-ui-results.json`, `main-schema-readonly.json`, `http-ui.mjs`, `fixture-ids.json`, ảnh và log preview. Không lưu JWT/secret trong kết quả báo cáo.

- Đã sửa lỗi biên dịch thiếu using/ngoặc truy vấn trong quá trình viết trước khi chốt build.
- Đã sửa verifier SQL ban đầu so sánh chuỗi CHECK/index chưa đúng cách SQL Server canonicalize; QA sau sửa PASS tạo/chạy lại và phát hiện index sai.
- Đã sửa lint ô evidence và nhãn truy cập ổn định của textarea; đưa lỗi form xuống vùng xác nhận để lỗi nhìn thấy được khi modal đang cuộn.
- Kiểm tra cuối phát hiện guard cũ nhầm cọc Confirmed lịch sử của request Cancelled là giữ chỗ dù ledger đã Completed. Đã sửa ba query liên quan; regression đi từ hủy chờ ký -> hoàn/tất toán -> manual suspend/reopen -> khách mới gửi request -> hoàn cọc trước contract và mở lại phòng PASS.
- Harness ban đầu chọn trùng nút X/Đóng, đã sửa selector và chạy lại đầy đủ. Không báo failure của harness đó thành lỗi nghiệp vụ đã phát hành.
- Có một lần timeout chờ nút mở hồ sơ sau tra cứu Admin trong một lượt E2E; kiểm tra riêng và chạy lại toàn bộ cùng source cuối đạt 20/20. Chưa tái hiện để kết luận nguyên nhân, không tuyên bố đã sửa một root cause chưa được chứng minh; giữ đây là rủi ro UI timing cần theo dõi, không che lần chạy timeout.
- Không còn failure test/build/lint được ghi nhận ở bộ chạy cuối. Không suy từ điều này ra mọi tình huống ngoài bộ test đều đã được chứng minh.

SQL full-suite dùng các QA mới có prefix `RoomRental_Verification_Closure_20261015_...`, lần cuối `_10` và hậu tố test. Preview/E2E dùng `_04_PaymentSafety`, không phải RoomRentalDB. Một số `_ClosureUpgrade` QA được cố ý sửa index để test fail-closed, không sử dụng làm preview.

## 9. Việc Còn OPEN / Giới Hạn

- **Triển khai main chưa làm**: thiếu cả SQL 07/08/09. Không tuyên bố server/database thật đã được sửa chỉ từ QA PASS.
- Dữ liệu lịch sử thiếu chứng cứ/mốc/khớp tiền phải được hai bên xác minh hoặc đối soát; không tự cấp trạng thái hoàn tất. Những dòng thật cần sửa tiền/snapshot bị lệch chưa có cơ chế tự repair, không dùng mediation để lén sửa lịch sử.
- Tranh chấp không thống nhất vẫn giữ chặn là hành vi mong muốn. Sau khi đã báo chuyển tiền, chỉ đồng thuận lại cùng số tiền/phương án; thay số tiền sau tiền thực chuyển cần quy trình điều chỉnh/đối soát riêng, không giả hoàn/thu lại tự động.
- Không có giải quyết pháp lý bắt buộc hoặc Admin đơn phương khấu trừ. Chứng từ báo/nhận tiền là dữ liệu khai báo có audit, không chứng nhận ngân hàng.
- Chưa test lại toàn bộ 25 UI case Giai đoạn 2 hoặc giao dịch ngân hàng; hồi quy backend đầy đủ đã chạy.
- GET hóa đơn vẫn có reconcile Overdue như source cũ; phần sửa GET ở đây là hợp đồng/hồ sơ kết thúc, không tự refactor toàn bộ GET của hệ thống.
- Không sửa schema initial meter, PDF/signature snapshot hoặc tính rent prorate. Nếu nghiệp vụ tính kỳ cuối cần khác cách tính hiện tại thì phải chốt riêng.

## 10. Rủi Ro Và Kế Hoạch 3B

Không triển khai các việc sau trong phiên này:

- Review gói deploy main SQL 07/08/09, backup, kiểm kê lịch sử và cutover đồng bộ worker/API/frontend; không bỏ guard QA của SQL 07 cho qua.
- BL-006/007: quyền Admin/chữ ký/role thay đổi theo thời gian và notification/outbox cần đánh giá riêng, không mặc định Admin thay hai bên.
- BL-015: snapshot tài liệu/điều khoản/PDF/chứng cứ ký; không nhân tiện sửa PDF hiện tại trong 3A.
- BL-016 và initial meter: hóa đơn cuối/prorate/kỳ trùng đã Cancelled/chỉ số bắt đầu cần quyết định và schema riêng nếu cần; 3A chỉ bảo vệ mốc cuối, số đo bàn giao và chống thu hai lần.
- Báo cáo dòng tiền/doanh thu phải phân biệt cọc, chuyển cọc sang trả bill, tiền bổ sung và hoàn; không cộng payment gốc với allocation/bill Paid như hai lần thu và không cộng callback event vào doanh thu. Module báo cáo không được viết lại trong phiên này.
- Điều chỉnh sau chuyển tiền thực tế và giao dịch VNPay ngân hàng thật cần kiểm thử/phê duyệt riêng.

**Dừng tại Giai đoạn 3A. Không commit/push, không chuyển sang 3B/4 và không thực hiện migration main.**

Preview QA: http://127.0.0.1:5173 ; Swagger QA: http://127.0.0.1:5097/swagger . Worker preview tắt để kiểm soát test; SQL/unit đã kiểm tra handler worker riêng. Tài khoản QA: `tenant@safety.test`, `owner@safety.test`, `admin@safety.test`; mật khẩu test `SafetyOnly-2026!`. Không dùng các tài khoản/mật khẩu này cho production.
