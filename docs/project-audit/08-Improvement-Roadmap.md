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
| UI-04 | Frontend quality | 122 lint errors, 10 warnings | `npm run lint` | P2 | CI/chat luong code | Sua ve 0 va bat gate | `05-Frontend-UI-UX-Audit.md` |

## Phase 0 - Khoa rui ro ngay (1-2 ngay)

- Rotate JWT/VNPay/SQL credentials; xoa secret khoi tracked appsettings, them template config va secret scan.
- Sua public post search/detail: public chi thay Approved + Available; admin/landlord dung endpoint rieng.
- Them regression test toi thieu cho hai loi tren truoc khi merge.
- **Exit criteria**: secret cu da vo hieu; anonymous khong doc duoc Pending/Rejected/Hidden.

### Trang thai implementation Phase 0 (03/10/2026)

- **PASS**: Da loai bo connection string, JWT secret, VNPay terminal/hash secret va fallback hard-code khoi tracked appsettings/source; them `appsettings.Local.json` ignored va `appsettings.Local.example.json`.
- **PASS**: Public search luon ep `Approved + Available`; public detail tra 404 cho tin khong public, trong khi API noi bo van giu duoc detail cua landlord/admin.
- **NOT VERIFIED**: Chua the rotate credential VNPay/JWT that hoac thu API voi database ma khong co secret moi va environment test rieng.
- **NOT IMPLEMENTED**: Regression/integration tests cho public visibility chua duoc them do repository chua co test project.
- **SUGGESTION**: Rotate cac secret da lo va them test database rieng truoc khi merge/push.

## Phase 1 - Chuan hoa luong thue va DB (3-5 ngay)

- Chot policy khi nhieu tenant cung request phong: reserve/approve/cancel/refund.
- Implement transaction/concurrency control, FK day du, rowversion hoac lock phu hop.
- Hop nhat database delivery thanh migration chain duy nhat; fresh install tren DB test.
- **Exit criteria**: concurrency test pass; khong orphan; fresh DB setup pass.

## Phase 2 - Sua trai nghiem core (2-4 ngay)

- Thay HomePage mock bang public API va state loading/error/empty.
- Chuan hoa URL/query tim kiem; truyen va xu ly price/area/category/province.
- Loai mock related rooms hoac hoan thien API tuong tu.
- **Exit criteria**: click card, search, filter va empty/error state da duoc test o desktop/mobile.

## Phase 3 - Hoan thien van hanh (3-5 ngay)

- Incident state machine, notification/audit; renewal/refund deposit policy.
- Background job cho expiry/overdue; xu ly lien tuc chi so dien nuoc.
- Pagination admin va error contract thong nhat.
- **Exit criteria**: state matrix test pass; du lieu van hanh cap nhat khong phu thuoc nguoi mo trang.

## Phase 4 - Chat luong va bao mat lien tuc (lien tuc)

- Dua frontend lint ve 0; them Error Boundary va session behavior.
- Them unit/integration/E2E test DB tach biet, CI build/lint/test/secret scan.
- Rate limiting, structured logging trace id, monitoring payment callbacks/background jobs.
- **Exit criteria**: CI xanh va co dashboard theo doi loi/queue/failed payment.

## Phan da on va khong can sua ngay

- **PASS**: Bcrypt password hashing, JWT validation co issuer/audience/lifetime, CORS localhost allowlist.
- **PASS**: Contract confirmation/payment success dung transaction va co cac validation quan trong.
- **PASS**: Financial columns dung decimal va payment target co DB CHECK constraint.
- **PASS**: UI public quan sat khong tran ngang o desktop va 375 px; console sach trong phien kiem tra.

## STOP - Can xac nhan truoc khi thuc hien

Audit da hoan tat. Can xac nhan pham vi implementation phase nao truoc khi sua source, doi secrets, tao migration hoac chay test co ghi database.
