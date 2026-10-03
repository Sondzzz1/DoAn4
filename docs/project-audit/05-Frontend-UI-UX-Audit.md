# 05. Frontend UI/UX Audit

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Phuong phap

- **PASS**: build production thanh cong khi chay ngoai sandbox Windows (237 modules).
- **ISSUE**: lint that bai `122 errors, 10 warnings`.
- **PASS**: quan sat read-only trang chu o desktop va 375 x 812: khong co horizontal overflow (`scrollWidth == clientWidth`) va console khong co warning/error trong phien quan sat.
- **NOT VERIFIED**: dashboard co login, cac form ghi du lieu, thanh toan, map provider va danh sach lay tu backend that khong duoc thao tac trong audit nay.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| UI-01 | Trang chu | "Tin noi bat" va "Tin moi dang" la mock hard-code, khong lay bai dang Approved tu API. | `FEATURED` va `RECENT` la constant trong `HomePage`; quan sat UI hien noi dung nay ngay ca khi khong khoi dong backend. | P1 | Nguoi dung thay du lieu demo co the sai/het han va click vao ID khong ton tai. | Goi API public, co loading/empty/error state, chi hien badge duoc xac dinh tu backend; bo demo data khoi production. | `frontend/src/pages/public/HomePage.tsx` |
| UI-02 | Tim kiem trang chu | Form chi gui `search` va `province`; trang danh sach doc `keyword`, bo qua `search`; filter price/area/type duoc luu state nhung khong dua vao URL/API. | `HomePage.handleSearch`; `RoomListPage` khoi tao `keyword` tu `searchParams.get('keyword')`. | P1 | Chuc nang tim phong o entry point chinh khong phan anh input nguoi dung. | Dung mot query schema; map price/area/category thanh API params; test URL va ket qua filter. | `HomePage.tsx`, `RoomListPage.tsx`, `services/postService.ts` |
| UI-03 | Related rooms | Fallback "phong tuong tu" dung mock va TODO thay vi query theo post hien tai. | `SimilarRooms.tsx` co `MOCK_SIMILAR_ROOMS`, TODO va fallback sau loi API. | P2 | Thong tin khong dang tin that, lam giam do tin cay. | Them API query tuong tu hoac bo section khi API loi; khong hien mock trong production. | `components/room/SimilarRooms.tsx` |
| UI-04 | Quality gate | Lint khong pass; nhieu `any`, unused import/variable, `setState` trong effect va rule immutability khi gan `window.location.href`. | `npm run lint`: 132 problems; vi du `TenantRentalsPage.tsx`, `TenantProfilePage.tsx`, `TenantFavoritesPage.tsx`, `AppRoutes.tsx`. | P2 | CI khong the dung lint lam gate, rui ro code smell va regression tang. | Sua theo nhom: typed Axios error, tach load async trong effect, dung API dieu huong phu hop, bo unused; bat lint trong CI sau khi ve 0. | `frontend/src/pages/**`, `frontend/src/routes/AppRoutes.tsx`, `frontend/src/utils/apiError.ts` |
| UI-05 | Error recovery | Chua co Error Boundary toan cuc; 401 dung hard redirect. | Khong tim thay ErrorBoundary; `api.ts` set `window.location.href = '/login'`. | P3 | Loi render hay phien het han co the mat state va trai nghiem dot ngot. | Them Error Boundary va session-expired flow thong nhat; cho phep quay lai URL truoc do an toan. | `frontend/src/App.tsx`, `frontend/src/services/api.ts` |
| UI-06 | Link integrity | Nhieu link footer dung `/#` va noi dung support/legal chi la placeholder. | AX tree trang chu va markup footer cho App Store/Google Play/FAQ/terms/privacy dung `#`. | P3 | User click nhung khong den duoc noi dung da hua. | Tao route/noi dung that hoac an link chua san sang. | `layouts/PublicLayout` va components footer lien quan |

## Diem dat

- **PASS**: responsive navigation chuyen thanh nut "Mo menu" tren 375 px; khong co tran ngang o hai viewport da kiem tra.
- **PASS**: lazy routes co loading fallback; nhieu tenant/landlord pages dung `PageState` va toast cho loading/error/empty.
- **PASS**: image gallery dung lazy loading va UI co icon/label hop ly o nhieu thao tac.

## SUGGESTION

Tao checklist visual regression cho 375, 768, 1280 px voi route public va dashboard sau khi co test account rieng. Khong dung mock data cho bat ky card co the dan den transaction/booking.
