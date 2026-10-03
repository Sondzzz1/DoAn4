# 04. Backend API Audit

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Ket qua build va pham vi

- **PASS**: backend .NET 8 build thanh cong voi 0 warning/0 error.
- **NOT VERIFIED**: khong goi API dang nhap, thanh toan hay API ghi du lieu de tranh thay doi database.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| API-01 | Public posts | Endpoint anonymous cho phep truyen `status`; khi co status, BLL bo qua dieu kien default `Approved && Available`. Endpoint chi tiet anonymous cung tra bat ky tin nao theo ID. | `PostController.SearchPosts`/`GetPostById` co `[AllowAnonymous]`; `PostQueryParameters.Status` public; `PostBLL` loc status client gui, va `GetPostByIdAsync` khong loc state. | P1 | Lo tin Pending/Rejected/Hidden, ly do tu choi, thong tin lien he chu tro va tang view count cua tin khong cong khai. | Public API phai ep Approved + Available; tach admin/landlord query status sang endpoint co authorize; detail public tra 404 cho tin khong public. | `Controllers/PostController.cs`, `BLL/PostBLL.cs`, `DTO/Post/PostQueryParameters.cs` |
| API-02 | HTTP contract | Nhieu controller nuot `BusinessRuleException` thanh 400 hoac 404 tuy y. | `PostController`, `UserController`, `AuthController` bat `Exception`; controller moi dung `BusinessError<T>`. | P2 | Frontend khong phan biet reliably validation, forbidden, not found, conflict. | Dung problem response/ApiResponse dong nhat va filter/middleware duy nhat; them integration tests cho 400/401/403/404/409. | `Controllers/BusinessErrorHandling.cs`, `PostController.cs`, `UserController.cs`, `AuthController.cs` |
| API-03 | Pagination | Cac danh sach quan tri chua the hien pagination; response co the tang khong gioi han. | `AdminController` nhan keyword/role/status nhung API tra `List<>`; BLL list khong co page parameter. | P2 | Cham, payload lon va UI kho quet khi du lieu tang. | Dung query DTO chung `page/pageSize`, total count, cap max page size, sort whitelist. | `Controllers/AdminController.cs`, `BLL/AdminBLL.cs`, `DTO/Admin/AdminDto.cs` |
| API-04 | Input contract | Public `PostQueryParameters` expose ca field admin (`Status`, `LandlordId`) va alias song song Anh/Viet. | DTO co `Status`, `TrangThai`, `LandlordId`, `ChuTroId` tren public route. | P2 | Tang attack surface va ambiguity binding. | Tach `PublicPostSearchQuery`, `AdminPostQuery`, `LandlordPostQuery`; bo alias sau giai doan compatibility co versioning. | `DTO/Post/PostQueryParameters.cs` |
| API-05 | API docs/test | Swagger chi trong Development va khong tim thay test project/API contract test. | `Program.cs` chi Map Swagger trong Development; `rg --files` khong co project test. | P3 | Kho bao dam route/response khong regression. | Them OpenAPI export trong CI va integration tests `WebApplicationFactory` voi DB test. | `Program.cs`, repository root |

## Diem dat

- **PASS**: Cac endpoint nhay cam admin/landlord/tenant da dung `[Authorize]`; BLL thuong kiem tra ownership them mot lop.
- **PASS**: Payment return/IPN la anonymous nhung xu ly lai kiem tra signature va amount trong service.
- **PASS**: SignalR JWT query token chi ap dung cho hai hub duoc chi dinh.
- **SUGGESTION**: Document endpoint matrix theo role va state transition, sau do test 401/403/404/409 tu matrix nay.
