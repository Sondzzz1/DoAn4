# 01. Architecture Audit

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Pham vi va cach kiem tra

- **PASS**: da doc source .NET 8, React/Vite, DI, route, DTO, EF Core configuration va scripts SQL.
- **NOT VERIFIED**: khong ket noi SQL Server, khong chay migration, khong gui request ghi du lieu theo yeu cau audit-only.
- **PASS**: `dotnet build backend/RoomRental.BackEnd/RoomRental.BackEnd.csproj --no-restore` thanh cong (0 warning, 0 error). `npm run build` thanh cong khi chay ngoai sandbox Windows.
- **ISSUE**: `npm run lint` that bai: 122 error, 10 warning. Day la gate chat luong code, khong dong nghia tat ca la loi runtime.

## Danh gia tong quan

Backend co cau truc Controller -> BLL -> EF Core `ApplicationDbContext`, DTO tach khoi entity va DI theo interface. Frontend tach route lazy-load, layouts, pages, services va components dung chung. Day la nen tang hop ly cho do an, nhung co mot so diem lech chuan lam tang chi phi bao tri va rui ro API.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| ARCH-01 | Error handling | Cach chuyen exception thanh HTTP response khong dong nhat. | `BusinessExceptionFilter` chi xu ly exception chua bi controller bat; nhieu controller cu bat `Exception` va tra `BadRequest`/`NotFound`, trong khi controller moi dung `BusinessError<T>`. | P2 | Client co the nhan 400 thay vi 403/404/409 cho cung mot loi nghiep vu. | Dua toan bo controller ve mot middleware/filter hoac bo helper duy nhat; khong bat `Exception` chung trong action. | `Controllers/BusinessErrorHandling.cs`, `PostController.cs`, `UserController.cs`, `RentalManagementControllers.cs` |
| ARCH-02 | BLL ownership | Nam dich vu nghiep vu khac nhau nam chung trong mot file lon. | `RentalFeaturesBLL.cs` chua `RentalRequestBLL`, `DepositBLL`, `RentalContractBLL`, `IncidentBLL`, `RoomReviewBLL`. | P2 | Kho doc, review, test va tach dependency theo tung luong. | Tach moi service va interface/DTO lien quan theo feature folder; giu public API khong doi. | `BLL/RentalFeaturesBLL.cs`, `BLL/Interfaces/RentalFeaturesInterfaces.cs` |
| ARCH-03 | Role model | Entity `Role` duoc map sang `__UnusedRoles`, trong khi quyen thuc te dua vao `User.RoleId` va claim string. | `RoleConfiguration.ToTable("__UnusedRoles")`; `AuthBLL` gan `RoleId`; `[Authorize(Roles=...)]` dung `RoleName`. | P2 | Mo hinh du lieu de gay nham lan va de script/database drift. | Chon mot mo hinh: FK `Roles` thuc su, hoac enum RoleId va xoa entity/config legacy trong mot migration co kiem soat. | `Models/Role.cs`, `DAL/Configurations/RoleConfiguration.cs`, `Models/User.cs` |
| ARCH-04 | Background processing | Het han hop dong va qua han hoa don chi duoc dong bo khi co request doc. | `ExpireEndedContractsAsync()` duoc goi tu lay hop dong; `ReconcileOverdueBillsAsync()` duoc goi trong cac API hoa don. | P2 | Trang thai phong/hoa don co the cu neu khong co ai mo man hinh tuong ung. | Dung `BackgroundService`/Hangfire/Quartz co idempotency va metric; giu kiem tra tai request nhu lop bao ve thu hai. | `BLL/RentalFeaturesBLL.cs`, `BLL/MonthlyBillBLL.cs` |
| ARCH-05 | Resilience | Khong tim thay Error Boundary React toan cuc. | Tim kiem `ErrorBoundary`/`componentDidCatch` trong `frontend/src` khong co ket qua. | P3 | Loi render bat ngo co the lam trang trong thay vi co man hinh phuc hoi. | Them Error Boundary o App root va route-level fallback co retry/bao loi an toan. | `frontend/src/App.tsx`, `frontend/src/routes/AppRoutes.tsx` |

## Diem dat

- **PASS**: JWT authentication, authorization va SignalR hubs duoc cau hinh tai mot diem trong `Program.cs`.
- **PASS**: Cac service la scoped va phu thuoc qua interface; DTO khong tra truc tiep EF entity.
- **PASS**: Frontend dung lazy route va `Suspense` fallback; nhieu page da co loading/error/empty state.
- **SUGGESTION**: Dat convention ve ten file/lop va quy tac exception truoc khi them feature moi.

## Ket luan

Kien truc hien tai du dung de hoan thien do an, nhung can xu ly `ARCH-01`, `ARCH-03`, `ARCH-04` truoc khi coi la nen tang san sang mo rong.
