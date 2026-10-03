# 07. Testing Plan

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Hien trang

- **PASS**: Backend build pass: 0 warning, 0 error.
- **PASS**: Frontend production build pass khi thuc thi ngoai sandbox Windows.
- **ISSUE**: Frontend lint fail: 122 error, 10 warning.
- **NOT IMPLEMENTED**: khong tim thay test project/thư muc unit/integration test trong repository inventory.
- **NOT VERIFIED**: khong chay E2E vi yeu cau khong lam thay doi database that.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| TEST-01 | Backend tests | Khong co test project duoc phat hien. | `rg --files` khong tim thay `*Tests*`/test project; backend chi la web project. | P1 | Khong co regression protection cho permission va state machine. | Them `RoomRental.BackEnd.Tests` (unit) va `RoomRental.BackEnd.IntegrationTests` voi database test tach biet. | repository root, `backend/RoomRental.BackEnd` |
| TEST-02 | Frontend lint | Lint khong dat. | `npm run lint` = 122 errors, 10 warnings. | P2 | Chat luong code khong duoc gate truoc merge. | Dua lint ve 0 va fail CI neu co loi moi. | `frontend/src/**` |
| TEST-03 | E2E/data isolation | Chua co evidence test database isolation/reset strategy. | Khong co test project/fixture duoc phat hien. | P1 | E2E co the lam ban du lieu that hoac khong determinisitic. | Tao SQL Server/container DB rieng, fixture reset co kiem soat, data factory va test account. | future test infrastructure |

## Test matrix uu tien

| Nhom | Case bat buoc | Ky vong |
|---|---|---|
| Public posts | Anonymous search voi status Pending/Rejected/Hidden; direct detail cua tin Hidden | Khong lo du lieu, tra 404/empty theo contract |
| Rental request | Hai tenant/request cung phong, approve dong thoi | Duy nhat mot request giu cho/duoc approve theo policy |
| Deposit/contract | Deposit chua paid, paid, confirmed; ky hop dong hai ben; overlap room/date | Transition hop le, conflict 409, room status nhat quan |
| Bill | Ky trung, chi so am/giam, chi so cu khac ky truoc, overdue | Tao/doi dung, tong tien dung, state dung |
| Payment | Invalid signature, wrong amount, duplicate IPN, success/failure return | Khong cap nhat sai; idempotent; restore bill khi that bai |
| Authorization | Moi endpoint voi anonymous/tenant/landlord/admin sai role va sai owner | 401/403/404/409 dung contract |
| Incident | Contract inactive, transition status sai, landlord khac | Bi chan; audit/notification khi them feature |
| UI | Home search URL, filters, API failure, mobile 375/768/1280 | Param dung, loading/empty/error ro, khong tran ngang |

## De xuat pipeline

1. `dotnet format --verify-no-changes` va `dotnet test` tren unit/integration project.
2. `npm run lint`, `npm run build`, unit test frontend (Vitest/RTL).
3. Integration test voi SQL Server test database tao/rut gon trong pipeline, khong dung `RoomRentalDB` local.
4. Playwright read/write chi trong database test, co screenshot khi fail.
5. Secret scan va dependency audit truoc merge.

## Tieu chi dat truoc khi goi la release-ready

- **SUGGESTION**: 0 lint errors, build xanh, regression test cho P1/P2, fresh-install database pass, va manual UAT cua 3 role tren du lieu test.
