# 03. Database Audit

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Gioi han

- **NOT VERIFIED**: khong truy cap database `RoomRentalDB`, khong chay script/migration va khong thay doi schema theo yeu cau audit-only.
- Danh gia nay dua tren EF configurations va hai bo SQL co trong repository.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| DB-01 | Schema delivery | Khong co EF `Migrations`; co hai full schema script cu dung bang English (`Posts`, `Rooms`, `Roles`) trong khi code/Phase 2 dung bang Vietnamese (`TinDang`, `PhongTro`, ...). | Khong tim thay thu muc migration; `RoomRentalDb_Schema_And_Data.sql` tao `Posts`, trong khi `Phase2_P0_WorkflowSafety.sql` FK toi `dbo.TinDang`. | P1 | Kho tao moi/khac phuc DB, de fail khi chay script theo thu tu khac nhau, schema moi truong bi drift. | Chon EF migrations hoac versioned SQL migrations duy nhat; tao fresh-install test trong CI; danh dau script cu la archive. | `database/RoomRentalDb_Schema_And_Data.sql`, `backend/RoomRental.BackEnd/SQL/RoomRentalDb_Schema_And_Data.sql`, `SQL/Phase2_P0_WorkflowSafety.sql` |
| DB-02 | Referential integrity | Deposit va contract co ID tham chieu request, nhung EF configuration khong khai bao FK/navigation tuong ung; Phase 2 tao cot `RentalRequestId` ma khong co FK trong phan tao bang. | `DepositConfiguration` chi unique index `RentalRequestId`; `RentalContractConfiguration` khong khai bao FK request/post; script Phase 2 chi FK contract -> TinDang. | P2 | Co the ton tai deposit/contract orphan neu ghi du lieu ngoai BLL; DB khong tu bao ve invariant. | Them FK Restrict cho Deposit->RentalRequest, Contract->RentalRequest va cac FK User can thiet; migration kiem tra orphan truoc. | `DAL/Configurations/RentalFeaturesConfiguration.cs`, `Models/RentalFeatures.cs`, `SQL/Phase2_P0_WorkflowSafety.sql` |
| DB-03 | Concurrency | Khong co concurrency token (`rowversion`) tren entity stateful; mot so luong da dung Serializable, nhung request approval va incident thi khong. | Entity/config khong khai bao rowversion; `RentalRequestBLL.CapNhatTrangThaiAsync` goi SaveChanges truc tiep. | P2 | Lost update/race condition khi nhieu thao tac cung luc. | Them rowversion cho request/deposit/contract/bill/room hoac lock pessimistic co chu dich; map `DbUpdateConcurrencyException` sang 409. | `Models/RentalFeatures.cs`, `DAL/Configurations/RentalFeaturesConfiguration.cs`, `BLL/RentalFeaturesBLL.cs` |
| DB-04 | Time model | He thong tron `DateTime.Now` voi `DateTime.UtcNow`; `SaveChangesAsync` tu dong set UpdatedAt UTC trong khi nhieu BLL set local time. | `ApplicationDbContext` dung `DateTime.UtcNow`; nhieu model/BLL dung `DateTime.Now`. | P2 | Sai lech sap xep, due date va audit khi deploy khac timezone/DST. | Luu UTC nhat quan, chuyen sang `Asia/Ho_Chi_Minh` tai presentation; dung `TimeProvider`. | `DAL/ApplicationDbContext.cs`, `BLL/*.cs`, `Models/*.cs` |
| DB-05 | Legacy role table | `Role` map den `__UnusedRoles`, khong phu hop vai tro source of truth hien tai. | `RoleConfiguration` map explicit den bang legacy. | P3 | Tang du lieu/maintenance khong can thiet, de nham script. | Dọn sau khi chon model quyen; khong xoa bang truoc khi migration va backup duoc duyet. | `DAL/Configurations/RoleConfiguration.cs` |

## Diem dat

- **PASS**: Nhieu truong tien dung `decimal(18,2)`; unique index hoa don theo contract/ky va favorite theo tenant/post da co.
- **PASS**: Payment transaction co unique `OrderId`, unique transaction code co filter, FK toi deposit/bill va CHECK "exactly one target".
- **PASS**: EF dung `DeleteBehavior.Restrict` cho nhieu quan he nghiep vu de tranh xoa day chuyen.

## NOT VERIFIED can thuc hien sau khi duoc phep

1. So sanh schema `INFORMATION_SCHEMA`/FK/index thuc te voi EF model.
2. Kiem tra orphan, duplicate approved request va duplicate active-contract bang query chi doc.
3. Backup va khoi tao database trong moi truong rieng tu migration chain duy nhat.
