# 08. Improvement Roadmap

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Nguyen tac

Khong sua production database truc tiep. Moi phase can co branch, test database rieng, migration/script versioned, backup va UAT theo role truoc khi merge.

## Top 10 van de thuc te

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| SEC-01 | Security | Secrets commit va fallback code | `appsettings*.json`, `Program.cs`, `PaymentBLL.cs` | P1 | Lo credential va kho rotate | Phase 0 rotate, secret store, fail-fast config | `06-Security-Audit.md` |
| API-01 | Public posts | Lo tin khong public | Anonymous status/detail code path | P1 | Lo thong tin tin/landlord | Phase 0 ep public visibility policy | `04-Backend-API-Audit.md` |
| UI-01 | Home | Tin demo hard-code | `FEATURED`, `RECENT` | P1 | Hien du lieu sai/het han | Goi API that va state UI | `05-Frontend-UI-UX-Audit.md` |
| UI-02 | Search | Sai key, filter khong duoc gui | `search` vs `keyword`; 3 filter ignored | P1 | Tim phong khong dung | Chuan hoa query schema | `05-Frontend-UI-UX-Audit.md` |
| BL-01 | Rental request | Nhieu request/dat coc cung phong | Approval khong reserve/lock | P1 | Thu tien sai nguoi | Transaction + state policy | `02-Business-Logic-Audit.md` |
| DB-01 | Database delivery | Script schema drift, khong migration chain | English schema va Vietnamese Phase 2 | P1 | Kho deploy/fresh install | Versioned migration chain | `03-Database-Audit.md` |
| TEST-01 | Testing | Khong co regression suite/DB isolation | Khong tim thay test project | P1 | Regression va E2E khong an toan | Tao test DB va CI | `07-Testing-Plan.md` |
| BL-02 | Incident | Khong gate Active/state machine/audit | Gan raw status | P2 | Xu ly su co sai ngu canh | State machine + notification | `02-Business-Logic-Audit.md` |
| ARCH-01 | API contract | Error HTTP khong dong nhat | Controller bat Exception khac nhau | P2 | Frontend xu ly loi khong on dinh | Middleware/filter duy nhat | `01-Architecture-Audit.md` |
| UI-04 | Frontend quality | 0 lint errors, 0 warnings | `npm run lint` ngay 03/10/2026 | P2 | CI/chat luong code | Da dat 0, can bat gate CI | `05-Frontend-UI-UX-Audit.md` |

## Phase 0 - Khoa rui ro ngay (1-2 ngay)

- Rotate JWT/VNPay/SQL credentials; xoa secret khoi tracked appsettings, them template config va secret scan.
- Sua public post search/detail: public chi thay Approved + Available; admin/landlord dung endpoint rieng.
- Them regression test toi thieu cho hai loi tren truoc khi merge.
- **Exit criteria**: secret cu da vo hieu; anonymous khong doc duoc Pending/Rejected/Hidden.

### Trang thai implementation Phase 0 (03/10/2026)

- **PASS**: Da loai bo connection string, JWT secret, VNPay terminal/hash secret va fallback hard-code khoi tracked appsettings/source; them `appsettings.Local.json` ignored va `appsettings.Local.example.json`.
- **PASS**: Public search luon ep `Approved + Available`; public detail tra 404 cho tin khong public, trong khi API noi bo van giu duoc detail cua landlord/admin.
- **NOT VERIFIED**: Chua the rotate credential VNPay/JWT that hoac thu API voi database ma khong co secret moi va environment test rieng.
- **PASS**: Da them `RoomRental.BackEnd.Tests` voi regression test in-memory cho public search/chi tiet: chi tin `Approved + Available` duoc public; tin khong public tra `404`.
- **SUGGESTION**: Rotate cac secret da lo va them test database rieng truoc khi merge/push.

## Phase 1 - Chuan hoa luong thue va DB (3-5 ngay)

- Chot policy khi nhieu tenant cung request phong: reserve/approve/cancel/refund.
- Implement transaction/concurrency control, FK day du, rowversion hoac lock phu hop.
- Hop nhat database delivery thanh migration chain duy nhat; fresh install tren DB test.
- **Exit criteria**: concurrency test pass; khong orphan; fresh DB setup pass.

### Trang thai implementation Phase 1 (03/10/2026)

- **PASS**: Duyet yeu cau, huy yeu cau va tao dat coc deu chay transaction `Serializable`; mot phong chi co mot yeu cau `Approved`, phong duoc chuyen sang `Reserved` khi duyet/tao dat coc.
- **PASS**: Tao/xac nhan hop dong tuong thich voi du lieu legacy `Available` va `Reserved`; phong chuyen `Rented` khi hop dong kich hoat. Chu tro khong the sua/xoa phong dang `Reserved` bang endpoint quan ly phong.
- **PASS**: Da them FK mapping EF va script versioned `backend/RoomRental.BackEnd/SQL/Phase3_P1_RentalReservation.sql` cho `DatCoc -> YeuCauThuePhong`, `HopDongThue -> YeuCauThuePhong`, `HopDongThue -> TinDang`.
- **NOT VERIFIED**: Script chua chay tren SQL Server theo yeu cau khong thay doi database; chua co concurrency test voi hai session/transaction that va chua fresh-install database test.
- **NOT IMPLEMENTED**: Chua co expiry/auto-release cho hop dong cho ky hoac yeu cau dat coc bi bo do; can state machine/background job o Phase 3.

## Phase 2 - Sua trai nghiem core (2-4 ngay)

- Thay HomePage mock bang public API va state loading/error/empty.
- Chuan hoa URL/query tim kiem; truyen va xu ly price/area/category/province.
- Loai mock related rooms hoac hoan thien API tuong tu.
- **Exit criteria**: click card, search, filter va empty/error state da duoc test o desktop/mobile.

### Trang thai implementation Phase 2 (03/10/2026)

- **PASS**: `HomePage` lay tin noi bat, tin moi dang va danh muc tu public API; bo du lieu phong mau hard-code va co state loading/error/empty image.
- **PASS**: Tim kiem dung key `keyword`; URL va `RoomListPage` truyen `province`, `categoryId`, `minPrice`, `maxPrice`, `minArea`, `maxArea`, `sortBy` den API. Danh muc duoc lay dong tu `/api/danh-muc`, khong hard-code ID.
- **PASS**: `SimilarRooms` truy van public API theo danh muc cua tin dang hien tai, loai tru tin dang dang xem va khong con fallback sang phong mau khi API loi.
- **PASS**: Trang chi tiet khong con hien tin gia khi API fail/tra 404; hien thi state loi thay vi du lieu preview hard-code.
- **PASS**: Blog lay danh sach/chi tiet tu `/api/bai-viet`; da bo bai viet demo va them route `/blog/:id` de nut doc tiep khong con dan den 404.
- **PASS**: Endpoint Blog public chi doc bai `Published`; doc bai nhap/an bang ID hoac slug tra 404 thay vi lo noi dung va tang luot xem.
- **NOT VERIFIED**: Chua E2E voi public API/database that va chua kiem tra thu cong desktop/mobile trong browser do backend hien dang duoc cau hinh bang local secret rieng.

## Phase 3 - Hoan thien van hanh (3-5 ngay)

- Incident state machine, notification/audit; renewal/refund deposit policy.
- Background job cho expiry/overdue; xu ly lien tuc chi so dien nuoc.
- Pagination admin va error contract thong nhat.
- **Exit criteria**: state matrix test pass; du lieu van hanh cap nhat khong phu thuoc nguoi mo trang.

### Trang thai implementation Phase 3 (03/10/2026)

- **PASS**: Da them state machine cho su co: `Pending -> InProgress/Resolved/Rejected`, `InProgress -> Resolved/Rejected`; khong the mo lai hoac nhay trang thai terminal.
- **PASS**: Tenant chi bao su co tren hop dong `Active`; title/mo ta/huong xu ly duoc validate. Chu tro dung hop dong hoac Admin deu co the xu ly, va dong su co bat buoc co huong xu ly.
- **PASS**: Luong yeu cau thue, hop dong va su co gui notification sau khi giao dich chinh commit; loi SignalR/thong bao chi duoc log va khong lam rollback nghiep vu.
- **PASS**: Da them worker lifecycle cho hop dong. Worker mac dinh `Enabled=false`; khi bat, hop dong cho ky qua 72 gio se bi huy va yeu cau thue quay lai `Approved` de xu ly tiep, khong giai phong hay tu dong hoan coc.
- **NOT IMPLEMENTED**: Chua co audit-log rieng, policy refund/renewal. Worker lifecycle chua duoc chay/test tren SQL Server that.

## Phase 4 - Chat luong va bao mat lien tuc (lien tuc)

- Dua frontend lint ve 0; them Error Boundary va session behavior.
- Them unit/integration/E2E test DB tach biet, CI build/lint/test/secret scan.
- Rate limiting, structured logging trace id, monitoring payment callbacks/background jobs.
- **Exit criteria**: CI xanh va co dashboard theo doi loi/queue/failed payment.

### Trang thai kiem tra Phase 4 (03/10/2026)

- **VERIFIED**: `npm run lint` da pass ngay 03/10/2026 voi 0 loi va 0 canh bao. Khong tat lint rule de che loi.
- **PASS**: Chat va Notification SignalR dung `VITE_API_BASE_URL` thay vi hard-code `localhost:5000`; token duoc lay lai khi hub reconnect.
- **PASS**: Da them Error Boundary bao quanh authentication va router; loi render khong lam trang trang toan bo va nguoi dung co the tai lai trang de khoi phuc.
- **PASS**: Da them `.github/workflows/ci.yml` de build/test backend va lint/build frontend moi khi push/PR.
- **NOT VERIFIED**: Workflow CI chua the duoc chay tren GitHub trong workspace local nay.

## Phan da on va khong can sua ngay

- **PASS**: Bcrypt password hashing, JWT validation co issuer/audience/lifetime, CORS localhost allowlist.
- **PASS**: Contract confirmation/payment success dung transaction va co cac validation quan trong.
- **PASS**: Financial columns dung decimal va payment target co DB CHECK constraint.
- **PASS**: UI public quan sat khong tran ngang o desktop va 375 px; console sach trong phien kiem tra.

## STOP - Can xac nhan truoc khi thuc hien

Audit da hoan tat. Can xac nhan pham vi implementation phase nao truoc khi sua source, doi secrets, tao migration hoac chay test co ghi database.
