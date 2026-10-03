# 02. Business Logic Audit

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Trang thai kiem tra

- **PASS**: da doc cac luong dang tin, dat lich, yeu cau thue, dat coc, hop dong, hoa don, thanh toan, su co, danh gia, chat va thong bao.
- **NOT VERIFIED**: khong chay E2E/concurrency test tren database that; cac ket luan ve race condition la suy luan tu ma nguon va can test transaction sau khi sua.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| BL-01 | Yeu cau thue/dat coc | Chu tro co the duyet nhieu request cho mot phong; cac request khac khong bi huy/giu cho, va moi request da duyet deu co the tao dat coc khi phong van `Available`. | `CapNhatTrangThaiAsync` chi doi mot request Pending thanh Approved; `DepositBLL.TaoAsync` chi kiem tra request Approved va phong Available. Khong co serializable transaction hay unique constraint cho mot request duoc duyet theo phong. | P1 | Co the thu tien coc cho nhieu khach truoc khi hop dong dau tien duoc ky. | Trong transaction Serializable, lock phong/request, chap nhan duy nhat mot request, tu choi/cancel request con lai, va dinh nghia chinh sach hoan coc. | `BLL/RentalFeaturesBLL.cs`, `DAL/Configurations/RentalFeaturesConfiguration.cs` |
| BL-02 | Su co | Nguoi thue co the bao su co cho hop dong bat ky cua minh, ke ca PendingSignature/Terminated/Expired; chu tro co the gui bat ky so `TrangThai`. | `IncidentBLL.TaoAsync` chi loc `Id` va `TenantAccountId`; `XuLyAsync` gan truc tiep `item.Status=dto.TrangThai`. | P2 | Bao su co sai ngu canh, state khong hop le, khong co audit trail hay thong bao. | Chi chap nhan hop dong Active; dung enum va transition cho phep; luu handledBy/handledAt/history; gui notification. | `BLL/RentalFeaturesBLL.cs`, `DTO/RentalFeaturesDtos.cs`, `Models/RentalFeatures.cs` |
| BL-03 | Hop dong | Hop dong het han chi duoc expire khi goi API lay hop dong; khi expire phong duoc mo lai ma khong kiem tra chinh sach hoa don chua thanh toan. | `ExpireEndedContractsAsync()` chi duoc goi tai `LayCuaToiAsync`/`LayChiTietAsync`, sau do set room Available neu khong co contract Active. | P2 | Du lieu van hanh tre va co the mo phong lai trong khi con cong no. | Background job idempotent; quy dinh no can thanh toan/canh bao truoc khi giai phong; them event log. | `BLL/RentalFeaturesBLL.cs` |
| BL-04 | Hoa don | Chi kiem tra chi so moi >= chi so cu trong cung mot hoa don, khong doi chieu chi so cu voi chi so moi cua ky gan nhat. | `ValidateReadings` la static va chi nhan 4 gia tri DTO; `TaoAsync` khong truy van hoa don truoc. | P2 | Chu tro co the lap hoa don lien ky co chi so dut doan, tong tien sai. | Khi tao/update ky moi, lay ky gan nhat cua contract va bat buoc old reading bang last new reading, co ngoai le co ly do/audit. | `BLL/MonthlyBillBLL.cs` |
| BL-05 | Hoa don | Qua han chi duoc reconcile luc doc hoa don, khong co job chu dong. | `ReconcileOverdueBillsAsync` loc `Unpaid` theo `DueDate < DateTime.Now` trong service. | P2 | Tenant/landlord co the thay trang thai cu va notification qua han cham. | Chay scheduled job va test boundary thoi gian. | `BLL/MonthlyBillBLL.cs` |
| BL-06 | Hoan coc/gia han | Chua thay endpoint/transition xu ly refund deposit hoac renewal contract. | Enum co trang thai refund; `DepositController` chi Tao/Lay/Confirm. `ContractController` khong co renewal. | P2 | Ket thuc hop dong chua co quy trinh tai chinh day du. | Bo sung use case refund/forfeit va renewal, phan quyen, event/audit, test. | `Controllers/RentalManagementControllers.cs`, `BLL/RentalFeaturesBLL.cs` |
| BL-07 | Danh gia | Chi co tao va xem danh gia; khong co edit/delete hay moderation policy. | `RoomReviewBLL` co Tao/LayTheoBaiDang/LayCuaToi; interface/controller tuong ung khong co update/delete. | P3 | Khong co cach sua noi dung sai hoac xu ly report danh gia. | Quy dinh thoi han edit, soft-delete/moderation va log nguoi xu ly. | `BLL/RentalFeaturesBLL.cs`, `Controllers/RentalManagementControllers.cs` |

## Luong da kiem tra tot

- **PASS**: Dat lich xem phong kiem tra tin Approved, phong Available, thoi gian tuong lai, trung lich tenant/landlord va khong tu dat phong cua minh.
- **PASS**: Tao hop dong va xac nhan hop dong dung transaction Serializable, kiem tra deposit Confirmed va overlap contract Active truoc khi chuyen phong sang Rented.
- **PASS**: Hoa don lay snapshot gia tu contract, khong tin gia dien/nuoc client gui len, va unique `(ContractId, Month, Year)`.
- **PASS**: Thanh toan VNPay kiem tra signature, amount, order, transaction code va commit thanh cong trong transaction.
- **PASS**: Danh gia yeu cau tenant dung chu hop dong va 1-5 sao.

## SUGGESTION

Luu state-transition table nhu mot artifact chinh thuc (request, deposit, contract, bill, incident, payment) va viet test cho moi canh hop le/khong hop le. Day la cach ngan regression hieu qua nhat cho nhom nghiep vu nay.
