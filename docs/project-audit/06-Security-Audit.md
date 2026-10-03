# 06. Security Audit

> **PASS** = da xac nhan dat; **ISSUE** = loi/rui ro co bang chung; **NOT VERIFIED** = chua the kiem tra trong audit; **NOT IMPLEMENTED** = chua co trong source; **SUGGESTION** = de xuat cai tien.

## Gioi han

- **NOT VERIFIED**: khong pentest, khong thu brute-force, khong gui payment callback gia, khong doc database production.
- Ket luan ben duoi la code review, khong phai chung nhan bao mat.

| ID | Module | Van de | Bang chung | Muc do | Anh huong | Giai phap | File lien quan |
|---|---|---|---|---|---|---|---|
| SEC-01 | Secrets | JWT signing key, SQL connection string va VNPay sandbox hash secret duoc commit trong `appsettings.json` va `appsettings.Development.json`; PaymentBLL con co fallback VNPay secret. | Gia tri ro rang trong appsettings; `Program.cs` co JWT fallback; `PaymentBLL` co TmnCode/HashSecret fallback. | P1 | Bi lo key neu repo/public backup bi truy cap; kho rotate; co nguy co dung sai config production. | Revoke/rotate secrets da commit, dung user-secrets/Environment/secret store, commit `appsettings.example.json` khong secret, fail-fast khi thieu secret va quet secret trong CI. | `appsettings.json`, `appsettings.Development.json`, `Program.cs`, `BLL/PaymentBLL.cs` |
| SEC-02 | Authorization/visibility | Public post API co the bypass state visibility. | Xem `API-01`: anonymous status filter va detail khong state gate. | P1 | Lo tin chua duyet/bi an, rejection reason va thong tin landlord. | Ep policy public o service; test anonymous cho Pending/Rejected/Hidden/Rented. | `Controllers/PostController.cs`, `BLL/PostBLL.cs` |
| SEC-03 | Abuse protection | Khong thay rate limiting cho login, register, location proxy, search hay payment URL. | `Program.cs` khong dang ky rate limiter; Auth/Location/Payment co public/unauth endpoint. | P2 | Brute force, spam tai khoan, API abuse va ton tai nguyen external geocoding. | Dung ASP.NET rate limiter theo IP/account; lockout co kiem soat; audit log va alert. | `Program.cs`, `Controllers/AuthController.cs`, `LocationController.cs`, `PaymentController.cs` |
| SEC-04 | Error disclosure | Mot so action tra tructiep `ex.Message`; exception chung co the lo chi tiet noi bo. | `PostController`, `UserController` va helpers tao `ApiResponse.ErrorResponse(ex.Message)`. | P2 | Client co the nhan thong tin implementation/DB, log va UX khong dong nhat. | Tra public error code/message curated; log exception + trace id phia server; khong tra raw exception. | `Controllers/*.cs`, `Controllers/BusinessErrorHandling.cs` |
| SEC-05 | Session | JWT lifetime 1440 phut, khong thay refresh/revocation strategy. | `JwtSettings.ExpiryInMinutes = 1440`; `AuthBLL` tao access token, khong co refresh token model/endpoint. | P3 | Token bi lo co cua so su dung dai; khoa tai khoan sau login khong nhat thiet thu hoi token ngay. | Rut ngan access token, refresh token rotation/revocation neu scope yeu cau; kiem tra `IsActive` khi can. | `appsettings.json`, `BLL/AuthBLL.cs` |

## Diem dat

- **PASS**: Mat khau dung BCrypt hash/verify, khong thay luu plaintext.
- **PASS**: JWT validate issuer, audience, lifetime, signing key va `ClockSkew = TimeSpan.Zero`.
- **PASS**: CORS dung allowlist localhost cu the va `AllowCredentials`, khong dung `AllowAnyOrigin`.
- **PASS**: VNPay callback kiem tra signature, amount, order va transaction code truoc khi danh dau thanh cong.
- **PASS**: Nhieu BLL kiem tra ownership, khong chi dua vao role attribute.

## Thu tu xu ly

1. Rotate secret va loai bo khoi Git.
2. Sua public post visibility va viet regression tests.
3. Them rate limiting va public error contract.
