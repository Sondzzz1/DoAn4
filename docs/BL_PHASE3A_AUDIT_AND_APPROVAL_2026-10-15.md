# Giai đoạn 3A: Hợp đồng, trả phòng và tất toán sau hợp đồng

Ngày 15/10/2026. **Đã kiểm tra tương thích và sửa phần GET ghi dữ liệu. Chưa triển khai toàn bộ Giai đoạn 3A.** Schema, state machine và API lớn dưới đây là **đề xuất chờ duyệt**, không phải chức năng đã có.

Không commit/push. Không chạy migration trên RoomRentalDB. Không gọi cổng thanh toán/ngân hàng hoặc API hoàn tiền. Dừng tại điểm xin duyệt của Giai đoạn 3A, không chuyển Giai đoạn 3B/4.

## 1. Hiện trạng luồng hợp đồng

### Source và tài liệu đã đối chiếu

- Git status, diff hiện tại và log HEAD `3fc2511`; giữ nguyên các thay đổi chưa commit của Giai đoạn 2.
- `docs/BL_PHASE2_COMPLETION_2026-10-15.md`: báo cáo triển khai hiện hành.
- `docs/BL_PHASE2_IPN_AND_SCHEMA_PROPOSAL_2026-10-15.md`: tài liệu lịch sử trước duyệt; phần OPEN trong tài liệu này không thay thế kết quả completion mới hơn.
- SQL 08, PaymentSafety entity/configuration/DTO/controller, DepositRefundBLL, PaymentEventBLL và các diff PaymentBLL/RoomBLL/Program/DbContext.
- RentalContractBLL, ContractOccupancyPolicy, RoommateBLL, MonthlyBillBLL, RentalLifecycleHostedService, contract controllers, tenant/landlord pages và regression tests.

### Luồng hiện có

| Bước | Điều kiện / tác động thực tế |
|---|---|
| Tạo hợp đồng | Đúng Landlord, request Approved, cọc Confirmed, Room Reserved, không trùng hợp đồng hiệu lực; request chuyển ConvertedToContract |
| Ký | PendingSignature; kiểm tra hai bên, ngày kết thúc và phòng giữ chỗ; đủ chữ ký thì Active hoặc PendingStart |
| Chờ bắt đầu | PendingStart, Room Reserved; worker đến ngày bắt đầu chuyển Active/Rented |
| Hết hạn | Active/PendingStart có EndDate trước ngày Việt Nam hiện tại chuyển Expired |
| Chấm dứt | API hiện cho Landlord/Admin theo quyền hiện có, chỉ Active/PendingStart; chuyển Terminated ngay |
| Tác động kết thúc hiện tại | Đóng tin ở ghép, hủy request ở ghép, đánh dấu người ở cùng Left, tính lại số cư trú; Room có thể Available ngay nếu không còn effective contract/Approved request |
| Cọc sau hợp đồng | Vẫn là lịch sử cọc Confirmed, không có quy trình khấu trừ/tất toán sau hợp đồng |
| Bàn giao | Chưa có entity, API hoặc bước hai bên xác nhận trả phòng |

Mã Contract được giữ nguyên: PendingSignature=0, Active=1, Terminated=2, Expired=3, Cancelled=4, PendingStart=5. EffectiveStatuses hiện là [0,5,1]. EndDate là **ngày thuê cuối cùng, inclusive theo Việt Nam**; 31/10 chỉ hết hiệu lực từ 01/11 00:00 giờ Việt Nam.

Worker có sẵn, mặc định bật, chạy khi khởi động và mỗi 5 phút; interval cấu hình tối thiểu 1 phút. Các workflow dùng transaction Serializable và khóa ứng dụng `RoomRental.Workflow`. Không cần tạo thêm worker cạnh tranh để sửa GET.

### Tương thích Giai đoạn 2

- Không thêm/đổi mã enum Deposit, RentalRequest, Contract, Room trong Giai đoạn 2; có **transition mới** RefundRequested/Refunded và Request Cancelled khi hoàn trước hợp đồng hoàn tất. Không được hiểu việc giữ mã enum là không đổi trạng thái dữ liệu.
- Hoàn trước hợp đồng chỉ nhận Paid/Confirmed chưa gắn **bất kỳ** contract nào. Có Cancelled/Expired/Terminated cũng bị chặn đúng chủ đích. Confirmed thuộc request ConvertedToContract không tự là cọc kẹt hoặc đã hoàn.
- SQL 08 chỉ thêm `SuKienThanhToan`, `YeuCauHoanCoc`, index/check/FK; không UPDATE/DELETE/DROP bảng nghiệp vụ cũ. Có transaction, schema lock, guard tên database, explicit opt-in cho RoomRentalDB và fail-closed khi chỉ có một bảng/schema không phù hợp.
- Check tiền/trạng thái/chứng từ, FK RESTRICT, RowVersion, unique event dedup và filtered unique yêu cầu hoàn đang mở phù hợp policy Giai đoạn 2.
- Đã chạy lại SQL 08 hai lần trên QA có dữ liệu: PASS, SHA-256 snapshot đầy đủ 11 bảng nghiệp vụ trước/sau không đổi. Kết quả nâng cấp QA cũ chưa có hai bảng của phiên Giai đoạn 2 vẫn nằm trong báo cáo completion; lần này kiểm tra lại đường chạy idempotent trên schema hiện hành, không giả nhận đã thử mọi schema bị sửa tay.
- Giới hạn kiểm tra script: kiểm tra schema hiện có chủ yếu theo tên/type/nullability, index tồn tại và tổng số check/FK. Chưa chứng minh phát hiện mọi trường hợp index filter/check/FK bị sửa định nghĩa nhưng vẫn giữ tên/số lượng; trước deploy phải đối chiếu DDL nếu database từng được chỉnh tay. Không sửa lại SQL 08 khi chưa có bằng chứng lỗi trên schema chuẩn.
- Metadata SELECT trên DB chính: **0/2 bảng PaymentSafety** tại lần kiểm tra này. Chưa triển khai SQL 08 vào DB chính; không tự chạy SQL 07/08.
- Khi thiếu SQL 08, chức năng hoàn/đối soát mới và tạo URL thanh toán bị guard yêu cầu nâng cấp, không được bỏ guard để chạy tiếp thanh toán mà thiếu ledger.

## 2. Những lỗi và phụ thuộc đã xác nhận

| Mã | Bằng chứng / rủi ro | Kết quả lần này |
|---|---|---|
| BL-014 GET có ghi | LayCuaToiAsync/LayChiTietAsync gọi ExpireEndedContractsAsync; kể cả GET trả 403/404 vẫn có thể kết thúc hợp đồng khác, đổi Room và người cư trú | **FIXED phần GET**; 11 case mới FAIL trước sửa, PASS sau sửa; thêm SQL và HTTP thực |
| BL-005 mở phòng quá sớm | ChamDutAsync đổi Terminated và Available mà không kiểm tra bàn giao, nợ, cọc; không có dữ liệu bàn giao để kiểm tra | **OPEN, cần duyệt quy trình/schema** |
| BL-014 worker mở phòng | ExpireEndedContractsAsync vẫn có nhánh Available sau Expired, đồng thời gọi EndAsync | **OPEN, cần duyệt quy trình/schema** |
| Lưu vết chấm dứt | Lý do chỉ gửi notification; không lưu requester/confirmer/effective date/history trên hợp đồng | **OPEN** |
| Cư trú bị kết thúc theo thời hạn | EndAsync đặt ContractOccupant.LeftAt dù chưa bàn giao; CountAsync chỉ tính Active; Roommate worker cũng tự End mọi contract terminal | **OPEN, phải sửa đồng bộ tối thiểu sau duyệt**, không chỉ bỏ một dòng Available |
| Guard đổi trạng thái phòng | RoomBLL chỉ chặn effective contract và một số quyền/cọc trước hợp đồng; chưa nhận biết terminal contract còn cư trú/chưa tất toán | **OPEN**, cần policy kiểm tra chung |
| Tất toán sau hợp đồng | YeuCauHoanCoc chỉ hỗ trợ hoàn đủ trước contract, không có allocation/bồi thường/chấp thuận hai bên | **OPEN**, không nới EnsureBeforeContractAsync để dùng nhầm |
| Hóa đơn cuối | MonthlyBillBLL đang dùng UpdatedAt UTC của Terminated làm mốc cuối; nếu lưu bàn giao/tất toán bằng cách sửa contract sẽ làm lệch kỳ thuê | **Cần thay đọc mốc kết thúc chuyên biệt**, không sửa Paid history |
| UI hiện có | Landlord đang xác nhận/toast rằng chấm dứt là trả về còn trống; Tenant không có request chấm dứt/bàn giao | **OPEN**, chỉ đổi thông điệp và thao tác khi API mới được duyệt/triển khai |
| Hợp đồng chờ ký bị hủy | Worker trả request về Approved và giữ Confirmed deposit/Reserved; refund Giai đoạn 2 vẫn chặn do đã tồn tại contract Cancelled | **OPEN có chủ đích**, cần nhánh hủy hợp đồng chưa nhận phòng của Giai đoạn 3 |

Không có bằng chứng cọc Confirmed tự bị đổi Refunded khi contract Terminated trong code hiện tại. Lỗi là thiếu tất toán và mở phòng sớm, không phải cần sửa payment thành công cũ thành thất bại/hoàn tiền.

Phụ thuộc Roommate và refund trước/sau contract đã được thông báo trước khi sửa GET. Không thay state machine cũ hoặc cập nhật trạng thái dữ liệu thật trong lần này.

## 3. State Machine Đề Xuất, Chờ Duyệt

Đây là policy kỹ thuật cần hai bên thỏa thuận, không phải kết luận pháp lý về quyền đơn phương chấm dứt hay khấu trừ.

### Tách các trục, giữ mã trạng thái cũ

1. **Contract:** PendingSignature -> PendingStart/Active -> Expired hoặc Terminated. Cancelled chỉ dùng luồng chưa đủ chữ ký theo policy hiện có. Không đổi EndDate gốc.
2. **Yêu cầu kết thúc sớm:** Requested -> Agreed -> Effective; Requested -> Rejected/Withdrawn; đề nghị bị thay thế/hết ý nghĩa -> Superseded. Một bên không đồng ý thì contract không tự Terminated.
3. **Bàn giao:** NotRecorded -> Draft -> PendingConfirmation -> Confirmed; bất đồng -> Disputed. Sửa biên bản tạo revision mới, hủy xác nhận cũ và lưu snapshot revision trước.
4. **Tất toán:** Draft -> Proposed -> Agreed -> TransferReported -> Completed; bất đồng -> Disputed; đề nghị cũ -> Superseded. Không đổi phương án đã Completed.
5. **Khả năng cho thuê lại:** Blocked -> ReadyForRelease -> Released. Chỉ đúng Landlord bấm xác nhận sẵn sàng cho thuê; không tự Available chỉ do expiry/termination/tiền hoàn được báo đã chuyển.

### Chấm dứt và các mốc ngày

- Tenant hoặc Landlord đúng contract gửi lý do và thời điểm kết thúc mong muốn. Bên còn lại chấp thuận/từ chối; người gửi không tự duyệt. Admin hỗ trợ đối soát, không mặc định ký thay hoặc tự tạo quyền đơn phương khấu trừ.
- Thời gian nhập hiển thị Việt Nam, server lưu UTC. Natural expiry: EffectiveEndAtUtc là 00:00 ngày sau EndDate tại Việt Nam. Early termination: hai bên thống nhất một thời điểm hiệu lực; nếu ở tương lai thì chỉ ghi Agreed, worker chuyển Terminated khi đến hạn.
- Lưu riêng LastRentalDate theo thời điểm kết thúc: mốc 00:00 là ranh giới loại trừ nên ngày thuê cuối là ngày trước; mốc trong ngày thuộc ngày thuê đó. Không tự tính tiền theo ngày hay phí ngoài hợp đồng trong bước này.
- Ngày trả thực tế độc lập EffectiveEndAtUtc. Đã Terminated/Expired nhưng chưa trả: vẫn chặn cho thuê, giữ thông tin cư trú; không tự gia hạn contract hoặc tự tính phạt lưu trú.
- Chỉ được chấp thuận kết thúc không muộn hơn mốc expiry tự nhiên. Nếu expiry xảy ra trước đề nghị còn treo thì supersede đề nghị bằng event, không thực hiện hai lần kết thúc.
- Trường hợp chưa nhận phòng, kể cả PendingStart kết thúc sớm hoặc Cancelled chưa ký: hai bên xác nhận **NeverTaken** có bằng chứng/thông tin kiểm tra; không tự ghi ngày bàn giao hay chỉ số giả. Nếu đã nhận phòng thì bắt buộc Returned và biên bản thực tế.

### Bàn giao và cư trú

- Biên bản Returned có ngày trả thực tế, tình trạng, điện/nước chốt, hư hỏng và chứng cứ. Chỉ số không âm, không thấp hơn chỉ số cuối đã chốt; chặn tạo/chỉnh hóa đơn làm vượt chỉ số bàn giao đã được hai bên xác nhận.
- Hai bên xác nhận cùng revision. Sửa số đo/tình trạng/chứng cứ phải reset xác nhận, ghi history; không ghi đè âm thầm tài liệu đã đồng thuận.
- Expiry/termination đóng quảng cáo và request ở ghép còn mở, nhưng **không** tự đặt người cư trú Left. Chỉ khi bàn giao Confirmed mới ghi rời phòng theo mốc trả thực tế.
- Nếu trả trước khi hết hiệu lực thì cũng ngừng nhận người ở ghép; không tái mở quảng cáo chỉ vì Contract còn Active.
- CountAsync/reconcile cần tách tình trạng hiệu lực và cư trú. Không hồi sinh người ở ghép Left lịch sử hoặc tự suy đoán số người ở hợp đồng cũ từ việc thiếu biên bản.

### Tất toán cọc và công nợ

- Giữ nguyên quy trình trước hợp đồng của Giai đoạn 2. Có thể tái sử dụng mẫu modal, thông báo, RowVersion, khóa workflow, kiểm tra chứng từ và ledger đối soát; không dùng lại lệnh hoàn trước hợp đồng cho cọc đã gắn contract.
- Phương án có snapshot cọc và từng hóa đơn chưa tất toán, khoản thiệt hại kèm chứng cứ; khoản khấu trừ chỉ hợp lệ sau đồng thuận hai bên trên cùng revision. Admin ghi nhận hòa giải/chứng cứ, không tự biến một khoản tranh chấp thành khoản nợ được khấu trừ.
- Tổng khoản được hai bên chấp thuận = C. Cọc hợp lệ chưa dùng = D. Tiền hoàn = max(D-C,0); tiền cần trả thêm = max(C-D,0). Không vừa hoàn vừa đòi thêm trên cùng tổng phương án.
- Landlord báo hoàn tiền, Tenant xác nhận nhận đủ. Khoản trả thêm: Tenant báo trả, Landlord xác nhận nhận đủ. Có chứng từ, actor, thời gian; không gọi API refund hoặc tạo giao dịch VNPay giả.
- Khi không có chuyển tiền phải thực hiện (cọc khấu trừ hết đúng bằng khoản đã đồng thuận), hai bên vẫn xác nhận quyết toán; không tạo chứng từ chuyển tiền giả.
- Deposit gốc Confirmed được giữ như lịch sử tiền đã nhận cho luồng sau hợp đồng; số dư/hoàn/khấu trừ nằm trong ledger tất toán mới. DTO/UI hiển thị **trạng thái tất toán riêng**, không dùng Confirmed làm bằng chứng cọc chưa hoàn và không thêm mã enum mơ hồ cho hoàn một phần. Luồng hoàn trước contract vẫn có thể dùng Refunded như Giai đoạn 2.
- Tạo/ký contract phải kiểm tra cọc chưa được phân bổ/tất toán cho contract khác, không chỉ nhìn Confirmed. Hoàn tất đóng hồ sơ không tự trả request ConvertedToContract về Approved để tái dùng cọc lịch sử; nhánh chưa nhận phòng hủy request chỉ khi không còn contract/quyền khác liên quan.
- Sau biên bản bàn giao, Landlord lập các bill cuối được phép và xác nhận chốt công nợ. Phương án tất toán phải bao gồm toàn bộ bill còn dư nợ, không bỏ sót bill để hoàn cọc trước. Lưu revision chốt bill; không nhận thêm bill hoặc sửa bill sau khi quyết toán đã hoàn tất. Nếu dữ liệu cuối chưa đủ thì chưa được chốt, không tạo bill/số đo giả.
- Chỉ hoàn tất phương án khi cả hai đồng thuận, đủ xác nhận nhận tiền thực tế và không có sự kiện RequiresReview/lượt thanh toán pending xung đột. Paid bill/payment gốc tuyệt đối không rewrite.
- Không phân bổ một phần vào bill đang mở rồi vẫn cho thanh toán toàn bộ bill qua API cũ. Đề xuất Giai đoạn 3A: phương án bao gồm toàn bộ dư nợ các bill được chọn, cọc + khoản trả thêm đủ mới hoàn tất từng bill trong transaction với PaymentMethod=`ContractSettlement`, liên kết allocation/chứng từ. Không tạo Succeeded VNPay cho khoản này.
- Bill được thanh toán riêng hoặc phát sinh/thay đổi trước khi phương án hoàn tất làm phương án mất hiệu lực; yêu cầu revision/đồng thuận mới, không được thu hai lần. Các API tạo/cập nhật/thanh toán bill và Payment tạo URL/callback phải có guard tương ứng, không sửa lại thuật toán IPN đã PASS.
- Không thêm kỳ thuê sau LastRentalDate, không tự tính phí ở quá hạn/khấu hao/bồi thường mặc định. Quy tắc tính kỳ/prorate thuộc phần cần chốt riêng nếu chưa có.

### Guard duy nhất trước khi cho thuê lại

Đề xuất `CanReleaseRoom` kiểm tra trong transaction/lock: contract đã hết hiệu lực; Returned/NeverTaken được xác nhận đúng revision; không còn người cư trú chưa rời; bill cuối đã chốt theo dữ liệu có căn cứ; không còn nợ chưa xử lý, cọc chưa tất toán, tranh chấp, payment pending hoặc RequiresReview; không có contract/request/quyền giữ chỗ khác. Sau đó Landlord xác nhận phòng sẵn sàng.

Áp dụng tối thiểu ở room status/delete-as-suspend, tạo/duyệt request, tạo/ký contract, callback cấp quyền, hoàn cọc mở phòng và các worker giải phóng. Public search/detail/appointment phải không coi Room Available bị lệch dữ liệu là phòng còn thuê được. Không reset Approved của Post chỉ do thay đổi trạng thái kỹ thuật.

Room đang TemporarilyUnavailable do bảo trì không được worker ép Available. Chỉ xác nhận tất toán không đồng nghĩa hết bảo trì hoặc mặc định đủ an toàn cho người mới.

## 4. File Đã Sửa Trong Lần Này

| File | Thay đổi |
|---|---|
| `backend/RoomRental.BackEnd/BLL/RentalFeaturesBLL.cs` | Bỏ đúng hai lệnh gọi ExpireEndedContractsAsync trong LayCuaToiAsync và LayChiTietAsync; không đổi API/DTO/schema hoặc nhánh worker/chấm dứt |
| `backend/RoomRental.BackEnd.Tests/ContractReadSafetyTests.cs` | 11 case InMemory + 1 SQL integration: GET list/detail, hai bên, PendingStart đến ngày bắt đầu, Active quá ngày cuối, 403/404, cư trú/cọc/nợ/payment không bị thay đổi |
| `docs/BL_PHASE3A_AUDIT_AND_APPROVAL_2026-10-15.md` | Báo cáo và phương án xin duyệt này |

Các file dirty Giai đoạn 2 vẫn giữ nguyên. Không nhận chúng là thay đổi mới của Giai đoạn 3A. Không có chỉnh sửa frontend trong lần này.

Sau bản sửa GET, trạng thái trả về là trạng thái **đang lưu**, có thể chậm tối đa một chu kỳ worker khi worker hoạt động bình thường; nếu worker tắt/lỗi sẽ không tự cập nhật chỉ bằng refresh. XacNhanAsync đã kiểm tra EndDate độc lập nên GET không cần ghi dữ liệu để chặn ký hợp đồng quá hạn. Roommate có kiểm tra ngày hết hạn trước khi nhận/duyệt người vào. Không giả hiển thị Expired nhưng DB vẫn Active.

## 5. Những Thay Đổi Cần Duyệt

**Chưa thực hiện bất kỳ phần nào sau đây:**

1. Năm bảng additive ở mục 6, state machine các trục và API ở mục 7.
2. Chấm dứt sớm cần hai bên đồng ý; Admin hòa giải, không tự quyết định khoản khấu trừ đang tranh chấp.
3. Cọc sau contract có ledger tất toán riêng, giữ lịch sử Confirmed và tất toán theo số dư, không dùng nhầm YeuCauHoanCoc.
4. Policy chặt: chỉ mở phòng sau bàn giao + tất toán + hết quyền khác + Landlord xác nhận sẵn sàng; khoản tranh chấp/nợ còn mở vẫn giữ chặn cho thuê.
5. Tích hợp nhỏ bắt buộc với Roommate, Room/Request/Contract guards, bill cuối và Payment chống thu trùng; không triển khai trọn BL-006/007/015/016.
6. Dữ liệu terminal lịch sử thiếu chứng cứ được đánh dấu NeedsReview ở DTO/policy, không backfill đã bàn giao/đã hoàn tiền. Việc thiếu hồ sơ không tự chứng minh đủ điều kiện mở phòng.

Cho phép hoàn thiện theo thiết kế trên phải được xác nhận trước khi viết entity/DDL/API hoặc thay đổi các transition có ảnh hưởng lớn. Nếu không duyệt năm bảng/policy chặt thì dừng ở bản sửa GET và thiết kế này.

## 6. Schema Dự Kiến, Chưa Tạo

Đề xuất **5 bảng mới, không thêm/xóa/đổi cột bảng cũ**. Số bảng tách theo hồ sơ, đề nghị, phương án, dòng phân bổ và lịch sử để không nhét khấu trừ thành text hoặc ghi đè phương án đã ký.

Quy ước: Id int IDENTITY, các FK int RESTRICT; riêng history Id bigint IDENTITY. Timestamp datetime2(7) UTC; số tiền/chỉ số decimal(18,2); trạng thái tinyint có CHECK; RowVersion rowversion cho bốn bảng mutable. Các dấu xác nhận lưu cả actor và thời điểm, không chỉ bool.

### A. HoSoKetThucHopDong (ContractClosure)

- ContractId NOT NULL UNIQUE FK HopDongThue; RoomId NOT NULL FK PhongTro. Một hồ sơ cho một contract.
- EndKind tinyint NULL (Natural/EarlyAgreement/UnsignedCancelled); EffectiveEndAtUtc datetime2 NULL; LastRentalDate date NULL. Không dùng UpdatedAt để suy ra mốc tất toán mới.
- HandoverStatus tinyint NOT NULL DEFAULT NotRecorded; HandoverKind tinyint NULL (Returned/NeverTaken); HandoverRevision int NOT NULL DEFAULT 0.
- ActualHandoverAtUtc datetime2 NULL; Condition nvarchar(2000) NULL; ClosingElectricity/ClosingWater decimal(18,2) NULL; DamageNotes nvarchar(2000) NULL; EvidenceJson nvarchar(max) NULL.
- TenantHandoverConfirmedByAccountId/LandlordHandoverConfirmedByAccountId int NULL FK TaiKhoan; hai ConfirmedAtUtc datetime2 NULL; xác nhận gắn HandoverRevision.
- BillingRevision int NOT NULL DEFAULT 0; BillingFinalizedAtUtc datetime2 NULL; BillingFinalizedByAccountId int NULL FK TaiKhoan. Chỉ finalize khi bàn giao/chỉ số/bill cuối đã có căn cứ; bill thay đổi trước tất toán phải hủy mốc chốt, tăng revision và invalidate đồng thuận phương án liên quan.
- ReleasedAtUtc datetime2 NULL; ReleasedByAccountId int NULL FK TaiKhoan; CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(); UpdatedAtUtc datetime2 NULL; RowVersion.
- CHECK chỉ số không âm, revision không âm, trạng thái/loại hợp lệ, JSON hợp lệ nếu có. Returned Confirmed bắt buộc ngày thực tế/chỉ số/tình trạng/hai bên; NeverTaken Confirmed bắt buộc xác nhận không nhận phòng, không tạo số đo giả. Quyền/so sánh chỉ số/thời điểm và điều kiện Released kiểm tra BLL trong khóa workflow.

### B. YeuCauKetThucHopDong (ContractEndRequest)

- ClosureId int NOT NULL FK HoSoKetThucHopDong; Status tinyint NOT NULL DEFAULT Requested.
- RequestedByAccountId int NOT NULL FK TaiKhoan; RequestedAtUtc datetime2 NOT NULL; Reason nvarchar(1000) NOT NULL; ProposedEffectiveEndAtUtc datetime2 NOT NULL.
- RespondedByAccountId int NULL FK TaiKhoan; RespondedAtUtc datetime2 NULL; ResponseNote nvarchar(1000) NULL; AgreedEffectiveEndAtUtc datetime2 NULL; EffectiveAtUtc datetime2 NULL; RowVersion.
- Status: Requested/Agreed/Rejected/Withdrawn/Superseded/Effective. Filtered unique ClosureId khi Requested hoặc Agreed. Giữ các đề nghị bị từ chối/rút trong lịch sử.
- CHECK reason không rỗng, Agreed có người/thời điểm/hiệu lực thỏa thuận; Effective có mốc áp dụng. Đúng bên phản hồi và giới hạn ngày là BLL, không dựa role đơn thuần.

### C. PhuongAnTatToanHopDong (ContractSettlement)

- ClosureId int NOT NULL FK hồ sơ; DepositId int NOT NULL FK DatCoc; Revision int NOT NULL; Status tinyint NOT NULL DEFAULT Draft.
- DepositAmountSnapshot/TotalCharges/RefundDue/ExtraDue decimal(18,2) NOT NULL; tổng server tính, không tin total từ client.
- BillingRevisionSnapshot int NOT NULL; chặn phương án dùng công nợ đã thay đổi hoặc chưa chốt.
- ProposedByAccountId int NOT NULL FK TaiKhoan; ProposedAtUtc datetime2 NULL; TenantAgreedByAccountId/LandlordAgreedByAccountId int NULL FK TaiKhoan; hai AgreedAtUtc datetime2 NULL.
- RefundReference nvarchar(500) NULL; RefundReportedByAccountId/RefundReceivedByAccountId int NULL FK TaiKhoan; hai timestamp NULL. Nhóm ExtraReference/ExtraReportedByAccountId/ExtraReceivedByAccountId và timestamp tương tự.
- CompletedAtUtc datetime2 NULL; CompletedByAccountId int NULL FK TaiKhoan; Note nvarchar(2000) NULL; CreatedAtUtc datetime2 NOT NULL; RowVersion.
- UNIQUE (ClosureId,Revision); unique filtered ClosureId và DepositId cho trạng thái khác Superseded, gồm cả Completed, để cọc không được tất toán hai lần qua hợp đồng khác. Completed không được chuyển Superseded tại BLL.
- CHECK số không âm, refund/extra không đồng thời dương, công thức dư cọc, revision > 0; trạng thái đồng thuận/hoàn tất có actor/thời gian/chứng từ cần thiết. Tạo revision mới lưu nguyên bản cũ.

### D. KhoanMucTatToanHopDong (ContractSettlementLine)

- SettlementId int NOT NULL FK PhuongAnTatToanHopDong; Kind tinyint NOT NULL (BillDebt/AgreedDamage); MonthlyBillId int NULL FK HoaDonHangThang.
- Amount decimal(18,2) NOT NULL; DepositAppliedAmount decimal(18,2) NOT NULL; Description nvarchar(1000) NOT NULL; EvidenceJson nvarchar(max) NULL.
- BillStatusSnapshot int NULL; BillUpdatedAtSnapshot datetime2 NULL; BillTotalSnapshot decimal(18,2) NULL. Không nhận ảnh/chứng cứ Base64; kiểm tra URL/giới hạn payload tại BLL.
- CHECK Amount > 0, 0 <= DepositAppliedAmount <= Amount; BillDebt có BillId/snapshot, AgreedDamage có chứng cứ; JSON hợp lệ. UNIQUE (SettlementId,MonthlyBillId) WHERE MonthlyBillId IS NOT NULL.
- Chỉ sửa dòng khi Draft; Proposed trở đi phải tạo revision mới. Kiểm tra tổng header, bill đã Paid, pending callback và phân bổ cùng bill thuộc phương án khác bằng transaction/khóa, không chỉ unique trong một phương án.

### E. LichSuKetThucHopDong (ContractClosureEvent)

- Id bigint IDENTITY; ClosureId int NOT NULL FK hồ sơ; EndRequestId/SettlementId int NULL FK tương ứng.
- EventType nvarchar(80) NOT NULL; ActorAccountId int NULL FK TaiKhoan (NULL chỉ event system); Source tinyint NOT NULL (User/Worker); OccurredAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME().
- FromState/ToState nvarchar(60) NULL; Reason nvarchar(2000) NULL; SnapshotJson nvarchar(max) NULL; IdempotencyKey nvarchar(120) NOT NULL UNIQUE.
- Index (ClosureId,OccurredAtUtc,Id), JSON CHECK; append-only qua application. Snapshot giữ revision bàn giao, phương án và nội dung tranh chấp/đối soát; không lưu JWT/secret, không cho xóa lịch sử qua API.

### Migration và dữ liệu cũ

- Chỉ viết SQL 09 sau khi duyệt; transaction + schema lock + kiểm tra kiểu/schema/constraint/index + guard database như SQL 08, QA thử tạo và chạy lại. Không chạy script mới lên RoomRentalDB thay người dùng.
- Kiểm tra prerequisite SQL 07/08 thực tế, không giả định DB chính đã có Roommate/PaymentSafety. Đổi code mà thiếu schema phải báo rõ/chặn mutation, không fallback worker cũ tự mở phòng.
- Bảng mới ban đầu rỗng, không backfill đã nhận/đã trả/đã hoàn hoặc revive occupant Left. Legacy thiếu hồ sơ cần rà soát có chứng cứ, không tự sửa trạng thái Room/Contract/Deposit.
- Hồ sơ terminal cũ thiếu chứng cứ phải chặn điều kiện reletting cho đến khi được rà soát, nhưng không được hủy/đổi quyền contract mới đã tồn tại. Không tự đếm lại người ở của contract lịch sử thành người đang ở; hiển thị chưa xác minh và kiểm tra xung đột trước khi tạo hồ sơ.
- Phải duyệt tác động có thể làm phòng Available lịch sử chưa rõ bàn giao không còn được xem là thuê được. Không tự biến absence của hồ sơ thành Released.
- Có backup, kiểm kê aggregate read-only trước deploy; thử bằng bản sao QA hoặc fixture QA. Không rollback bằng DROP bảng có sự kiện tài chính. Khi rollback code phải tạm chặn thao tác cấp phòng và dừng worker cũ có nhánh mở phòng sai, không chỉ hạ DLL.

## 7. API Và Frontend Liên Quan, Chưa Triển Khai

| API đề xuất | Nội dung |
|---|---|
| GET /api/hop-dong/{id}/ket-thuc | Hồ sơ, khả năng thao tác, trạng thái cọc/nợ, lý do chặn cho thuê lại; ownership, chỉ đọc |
| POST /api/hop-dong/{id}/yeu-cau-ket-thuc | Tenant/Landlord của contract đề nghị lý do/thời điểm |
| PUT /api/yeu-cau-ket-thuc/{id}/{operation} | agree/reject/withdraw; RowVersion và đúng bên |
| PUT /api/hop-dong/{id}/ban-giao | Draft/revision biên bản, giữ input khi lỗi |
| POST /api/hop-dong/{id}/ban-giao/{operation} | confirm/dispute; chấp thuận cùng revision, không tự ghi ngày rời vì expiry |
| POST /api/hop-dong/{id}/chot-cong-no | Landlord kiểm tra và chốt toàn bộ bill cuối, số đo, revision; không tạo tiền/phí mặc định |
| POST /api/hop-dong/{id}/tat-toan | Draft/revision phương án có khoản mục, server tính tổng |
| PUT /api/tat-toan/{id}/{operation} | propose/agree/dispute/report-refund/confirm-refund/report-extra/confirm-extra/complete, chỉ đúng actor/trạng thái |
| POST /api/hop-dong/{id}/san-sang-cho-thue | Đúng Landlord, kiểm tra mọi điều kiện; retry không lặp release/history |
| POST /api/hop-dong/{id}/ho-tro-doi-soat | Admin ghi bằng chứng/hòa giải, không tự quyết khấu trừ hay giả xác nhận hai bên |

Tên route là đề xuất, cần chốt với source lúc triển khai; không dùng tên route parameter MVC dành riêng `action`. Các lệnh mutation có RowVersion, idempotency key, transaction/WorkflowLock, history cùng transaction và notification sau commit. Claims phải parse tài khoản hợp lệ, không coi thiếu claim là Admin `0` cho API mới.

Endpoint chấm dứt cũ phải đổi cùng frontend để không còn đường đi tắt mở phòng. Đề xuất giữ alias cho client cũ nhưng trả thông điệp cần gửi đề nghị theo quy trình mới, không âm thầm báo chấm dứt thành công khi chỉ tạo Requested. Đây là thay đổi contract API cần duyệt.

TenantRentalsPage và LandlordContractsPage tái sử dụng Modal/forms/error handling; thêm trạng thái hết hiệu lực, bàn giao, công nợ và tất toán tách biệt. Hiển thị lý do, ngày hiệu lực/ngày trả thực tế và history. Admin chỉ đối soát/hòa giải có endpoint thật; không thêm nút giả hoặc tự điều hướng Giai đoạn 3B.

Không sửa giao diện lúc chưa có API mới. Toast hiện tại về còn trống phải đổi cùng lệnh chấm dứt mới. Responsive 375/768/desktop cần test với API/QA thật sau triển khai, không dùng response mock để thay kiểm thử nghiệp vụ.

## 8. Kiểm Thử Đã Chạy Thực Tế

| Kiểm tra | Kết quả lần này |
|---|---|
| 11 regression InMemory mới trước sửa | **11 FAIL / 0 PASS**; đúng lỗi GET tự chuyển Active/PendingStart và thay đổi DB kể cả 403/404 |
| Full backend sau sửa, có SQL QA | **290 PASS / 0 FAIL / 0 SKIP**, gồm 278 test trước và 12 test mới |
| Backend Release build | **PASS**, 0 warning / 0 error |
| Frontend lint | **PASS** |
| Frontend production build | **PASS**; có thông tin plugin timing Vite, không phải build failure |
| HTTP/SQL bổ sung | **11/11 PASS**: SQL 08 hai lần, metadata main chỉ đọc, 7 HTTP theo quyền/trường hợp, một nhóm GET lặp; hash 11 bảng QA không đổi |
| Git diff --check | PASS; cảnh báo CRLF Windows không phải lỗi whitespace |
| Schema/state machine/API tất toán 3A | **CHƯA TẠO / CHƯA TEST**, chờ duyệt |
| Full E2E bàn giao/khấu trừ/cho thuê lại mới | **CHƯA THỂ CHẠY**, chưa có schema/API được duyệt |
| VNPay real sandbox bank transaction / refund | **NOT VERIFIED** |

HTTP dùng bản build mới, backend QA cổng 5096 tạm thời, worker tắt để chỉ đo tác động GET; JWT tự ký bằng secret QA cho tài khoản QA và đi qua authentication/active-account validation thực, không kiểm tra luồng login và không gọi VNPay. API QA đã tự dừng sau kiểm thử, không dừng server của người dùng. Lần khởi chạy harness đầu sai đường dẫn artifact sandbox, đã sửa harness và chạy lại PASS; đây không phải lỗi source ứng dụng.

QA mới: `RoomRental_Verification_Contract3A_20261015_01` và hậu tố các SQL tests; HTTP dùng hậu tố `_ContractReads`. RoomRentalDB chỉ SELECT metadata. Không dùng mutation dữ liệu thật.

Bằng chứng ở `C:/Users/huanp/AppData/Local/Temp/contract-lifecycle-verification/`: `contract-read-before.trx`, `stage3a-full.trx`, `http-sql-results.json`, `verify-http-sql.ps1`, log API QA. JWT và secret không được ghi trong báo cáo/kết quả.

### Đối Chiếu 15 Tình Huống Người Dùng Yêu Cầu

| # | Tình huống | Mức kiểm tra hiện tại |
|---|---|---|
| 1 | Active chưa bàn giao | GET không thay đổi cư trú/Room/cọc/nợ PASS; chưa có trạng thái bàn giao mới |
| 2 | Hết thời hạn còn bill chưa trả | GET trên dữ liệu quá ngày cuối, unpaid bill và người cư trú PASS; worker vẫn có lỗi mở phòng, OPEN |
| 3 | Expired đã bàn giao | Chờ schema/API bàn giao, NOT RUN |
| 4 | Chấm dứt sớm có lý do lưu bền vững | Root cause đã xác nhận thiếu persistence; workflow mới NOT RUN |
| 5 | Tenant yêu cầu kết thúc contract người khác | API đề nghị mới chưa có, NOT RUN; detail ownership đã test 403, không thay thế test này |
| 6 | Landlord tác động contract không thuộc mình | GET detail 403 không ghi PASS; không nhận là test đầy đủ mutation bàn giao/tất toán |
| 7 | Confirmed chưa tất toán | GET không đổi cọc/payment PASS, guard chặn reletting toàn hệ thống mới chưa triển khai |
| 8 | Hoàn đủ sau contract | NOT RUN; test hoàn trước contract Giai đoạn 2 không thay thế |
| 9 | Khấu trừ hợp lệ đã đồng thuận | NOT RUN |
| 10 | Tranh chấp tất toán | NOT RUN |
| 11 | Hai request kết thúc đồng thời | NOT RUN cho workflow mới; lock hiện tại không chứng minh policy mới đúng |
| 12 | Worker chạy lại | Existing regression PASS cho transition hiện có; cần test bổ sung không tự bàn giao/mở phòng sau duyệt |
| 13 | GET không giải phóng phòng | **PASS** InMemory, SQL integration, HTTP thực và GET lặp |
| 14 | Chỉ cho thuê lại khi đủ điều kiện | **OPEN**, cần schema/policy chung; chưa tuyên bố PASS |
| 15 | Giai đoạn 2 không regression | Full backend bao gồm IPN/refund/ledger/SQL concurrency PASS; không chạy lại 25 UI cases cũ hoặc ngân hàng thật trong phiên này |

Một số regression cũ đang xác nhận hành vi expiry/termination -> Available và occupant Left. Chúng PASS bảo đảm bản sửa GET chưa đổi workflow, **không chứng minh hành vi đó đúng nghiệp vụ 3A**. Sau duyệt phải thay expected bằng policy mới và giữ boundary/ngày, ownership, tài chính/idempotency liên quan.

## 9. Nghiệp Vụ Còn Chưa Hoàn Thiện

- BL-005 và phần worker/release của BL-014 vẫn OPEN, chưa có bàn giao/đồng thuận/tất toán sau hợp đồng.
- Chưa lưu lịch sử chấm dứt hoặc mốc kết thúc chuyên biệt; lý do hiện chỉ ở thông báo.
- Chưa có reconciliation hồ sơ lịch sử thiếu bàn giao/chứng từ; không suy đoán hoặc sửa dữ liệu này.
- Chưa triển khai SQL 08 ở main; main chưa sẵn sàng dùng toàn bộ chức năng Giai đoạn 2.
- Chưa có UI mới để responsive/E2E bàn giao. Lint/build PASS không thay cho xác minh 375/768 của workflow chưa tồn tại.
- Không công bố Giai đoạn 3A đã hoàn thành hoặc thanh toán/hoàn ngân hàng fully verified.

## 10. Rủi Ro Và Kế Hoạch Giai Đoạn 3B

Chỉ ghi kế hoạch, **không tự triển khai**.

- BL-006/007: cần rà soát quyền Admin/chữ ký/chấm dứt, trạng thái tranh chấp và notification sau commit khi policy 3A được chốt; không ngầm dùng actor=0 để vượt ownership trong API mới.
- BL-015: snapshot/PDF/điều khoản/chứng cứ ký là nhánh riêng, không nhân tiện viết lại hợp đồng/PDF trong 3A.
- BL-016: bill cuối/prorate, kỳ trùng Cancelled và initial meter cần quyết định/schema riêng; 3A chỉ thay mốc kết thúc và chống phân bổ/thu trùng cần thiết. Không thêm cột initial meter chưa được duyệt.
- Khi có tất toán bằng cọc/tiền ngoài hệ thống, báo cáo doanh thu không được cộng thêm event callback hay cộng cả allocation và bill Paid thành hai khoản thu. Cần test báo cáo ở nhánh được giao tiếp theo.
- Chưa có bằng chứng tiền thật; receipt manual là chứng từ khai báo/xác nhận được lưu, không chứng nhận ngân hàng.

**Điểm dừng:** chờ người dùng phê duyệt mục 3, 5, 6, 7 trước khi triển khai phần lớn còn lại của Giai đoạn 3A. Chưa chuyển 3B/4.
