# RoomRentalSystem - Authorized Fix Verification

Verified 2026-10-09, Asia/Saigon. This report covers the authorized audit fixes, not a claim that every possible system behavior has been verified.

## Result

12 issues FIXED and checked: ISSUE-01 through ISSUE-11, and ISSUE-13.
ISSUE-12 remains OPEN, pending explicit approval of the schema proposal below.
VNPay real sandbox bank transaction: **NOT VERIFIED**.

| Issue | Fix and regression evidence |
| --- | --- |
| 01 Admin bootstrap | Explicit Admin role 0 is no longer replaced by EF's default role 1; fresh SQL seed and repeat startup checked. |
| 02 Room image validation | Create/update accept trimmed HTTP(S) or /uploads/ URLs, deduplicate, reject Base64 and invalid protocols before persistence. Legacy rows are not rewritten. |
| 03 Final bill | Active/Expired/Terminated contracts can be billed for a period overlapping actual rental time; ownership, unique period, and paid-history guards retained. Future periods after termination rejected. |
| 04 PDF font | Use bundled Lato; actual PDF generated, extracted and visually inspected with Vietnamese text. |
| 05 PDF snapshot | Deposit/electricity/water/service amounts come from contract snapshots; PDF extraction verifies values after Room prices change. |
| 06 Review length | 2000 characters accepted, 2001 rejected before DB write. |
| 07 Private blog comment | Draft/Hidden articles cannot receive new public comments; Published remains functional. |
| 08 API errors | Known business errors retain 400/401/403/404/409; unexpected failures log the full exception server-side and return generic 500 messages. |
| 09 Room review semantics | Reviews aggregate through existing Post.RoomId, including an old hidden post and its approved replacement; other rooms excluded. No migration. |
| 10 Appointment datetime | Require explicit appointment datetime or both valid date/time aliases; remove silently invented date/time defaults. |
| 11 Inclusive EndDate | Vietnam calendar dates; Oct 31 remains rented through 23:59:59 +07 and expires Nov 1 00:00 +07. Signing/lifecycle/overlap use this rule. |
| 13 Appointment notifications | Notify affected counterpart on creation, cancellation, confirmation, rejection, completion and reservation-triggered cancellation; notification failures do not undo committed actions. |

Final bill compatibility detail: for Terminated contracts, the existing terminal UpdatedAt timestamp is interpreted as UTC and converted to a Vietnam calendar day, capped by EndDate. Terminal contracts cannot be edited through the existing workflow. Missing legacy termination timestamps fail closed rather than being invented. This is not a new dedicated termination-history column and cannot recover timestamps altered outside the application.

## Before / After

- Baseline: 108 backend tests passed, but audit identified 13 issues; 10 failed audit checks represented observed failures, with additional policy gaps.
- After: 144/144 backend tests passed, 0 failed, 0 skipped. Includes real SQL Server integration and 36 additional regression cases.
- Module checks were run after each fix group: bootstrap; Room/publication; PDF; review; blog; error handling; appointments/notifications; final bill; calendar boundaries.
- Backend Release build: PASS, 0 warnings, 0 errors.
- Frontend lint: PASS. Frontend production build: PASS.
- Full HTTP workflow: 121/121 PASS.
- Additional HTTP checks: 41/41 PASS.
- Final-bill/boundary/realtime checks: 19/19 PASS.
- UI render checks: 112/112 PASS, 28 routes at 375, 768, 1200 and 1440px.
- UI interactions: 11/11 PASS, including real Bing URL preview, failed URL warning, duplicate/Base64 rejection, image removal, actual map tiles and role guards.
- Terminated-contract bill modal: 2/2 PASS at 375/768px; existing contract preselected, modal in viewport, no console/page errors in these checks.
- PDF snapshot extraction and visual inspection: PASS.
- SQL image evidence: 2 image rows in isolated QA DB, 0 data-URL rows; /uploads/test.jpg and https://example.com/qa.jpg stored unchanged.

The E2E exercised Room -> Post -> moderation -> filtered search/detail -> appointment -> rental request -> approval/deposit -> Reserved -> local signed payment callback -> deposit confirmation -> contract -> both signatures -> Active/Rented -> bill/payment -> termination -> Available. Editing approved Room pricing also removed it from search, reset moderation and invalidated the stale rental request before reapproval.

Payment callbacks were generated locally using isolated QA credentials. No bank checkout/payment was performed. Passing local signature/idempotency/ownership checks does not establish VNPay sandbox bank verification.

## Database Scope

- No production/local main database schema changes or destructive migrations.
- No new application migration or columns for these fixes.
- Test data was written only to the separate verification database RoomRental_Verification_Fixes_20261008222440.
- Existing Room/Post rows and legacy image values were not rewritten or deleted.
- UserConfiguration changes EF value generation, not SQL schema.
- QA database is retained as test evidence, not promoted to the application database.

## ISSUE-12 Schema Proposal - NOT Implemented

Approval requested for table dbo.HopDongThue:

| New column | SQL type | Existing rows | Model property |
| --- | --- | --- | --- |
| ChiSoDienBanDau | decimal(18,2) NULL | NULL, no default/backfill | InitialElectricityMeter: decimal? |
| ChiSoNuocBanDau | decimal(18,2) NULL | NULL, no default/backfill | InitialWaterMeter: decimal? |

Proposed check constraints:

- CK_HopDongThue_ChiSoDienBanDau_NonNegative: ChiSoDienBanDau IS NULL OR ChiSoDienBanDau >= 0.
- CK_HopDongThue_ChiSoNuocBanDau_NonNegative: ChiSoNuocBanDau IS NULL OR ChiSoNuocBanDau >= 0.
- CK_HopDongThue_InitialMeters_Paired: both readings NULL or both NOT NULL.

No indexes or foreign keys needed. No default zero: zero must be an explicitly recorded reading, not invented legacy data.

After approval, proposed application rule: collect both readings on new contract creation, include them in the contract snapshot before signing, and require them before activation. First bill uses contract initial readings; subsequent bills use the previous bill's new readings. Readings become immutable after signing/activation and cannot rewrite paid history. Existing contracts with NULL readings need explicit verified readings or a separately approved reconciliation based on historical bills; do not silently backfill.

No model, DTO, migration, SQL column, or activation behavior for ISSUE-12 has been changed. Current first-bill initial-meter behavior remains an acknowledged open gap until approval and implementation.

## Files Changed for These Fixes

Paths relative to RoomRentalSystem (prior landlord redesign/image UI changes are excluded):

```text
backend/RoomRental.BackEnd/DAL/Configurations/UserConfiguration.cs
backend/RoomRental.BackEnd/DTO/ViewingAppointment/CreateAppointmentDto.cs
backend/RoomRental.BackEnd/Helpers/RentalCalendar.cs
backend/RoomRental.BackEnd/BLL/AdminBLL.cs
backend/RoomRental.BackEnd/BLL/AmenityBLL.cs
backend/RoomRental.BackEnd/BLL/AuthBLL.cs
backend/RoomRental.BackEnd/BLL/BlogBLL.cs
backend/RoomRental.BackEnd/BLL/CategoryBLL.cs
backend/RoomRental.BackEnd/BLL/ExportBLL.cs
backend/RoomRental.BackEnd/BLL/FavoriteBLL.cs
backend/RoomRental.BackEnd/BLL/MonthlyBillBLL.cs
backend/RoomRental.BackEnd/BLL/PostBLL.cs
backend/RoomRental.BackEnd/BLL/RentalFeaturesBLL.cs
backend/RoomRental.BackEnd/BLL/RoomBLL.cs
backend/RoomRental.BackEnd/BLL/UserBLL.cs
backend/RoomRental.BackEnd/BLL/ViewingAppointmentBLL.cs
backend/RoomRental.BackEnd/Controllers/AdminController.cs
backend/RoomRental.BackEnd/Controllers/AmenityController.cs
backend/RoomRental.BackEnd/Controllers/AuthController.cs
backend/RoomRental.BackEnd/Controllers/BlogController.cs
backend/RoomRental.BackEnd/Controllers/BusinessErrorHandling.cs
backend/RoomRental.BackEnd/Controllers/CategoryController.cs
backend/RoomRental.BackEnd/Controllers/ExportController.cs
backend/RoomRental.BackEnd/Controllers/LandlordController.cs
backend/RoomRental.BackEnd/Controllers/NotificationController.cs
backend/RoomRental.BackEnd/Controllers/PaymentController.cs
backend/RoomRental.BackEnd/Controllers/PostController.cs
backend/RoomRental.BackEnd/Controllers/UserController.cs
backend/RoomRental.BackEnd.Tests/AuditRegressionTests.cs
backend/RoomRental.BackEnd.Tests/SqlServerWorkflowTests.cs
frontend/src/pages/landlord/LandlordContractsPage.tsx
tools/verify_contract_pdf.py
docs/AUDIT_FIX_VERIFICATION_2026-10-09.md
```

BLL/controller changes outside the affected domain methods classify existing business exceptions and route existing catch blocks through consistent status/log handling; they do not introduce new workflows. SqlServerWorkflowTests has collection serialization to avoid fresh-database setup races with the new SQL bootstrap regression.

## Evidence and Remaining Limitations

Local evidence directory: C:/Users/huanp/AppData/Local/Temp/room-rental-system-fixes.
Files: fixes.trx, api-results.json, extra-results.json, final-results.json, ui-results.json, interaction-results.json, final-bill-ui-results.json, screenshots and contract-pdf.png.
PDF: C:/Users/huanp/AppData/Local/Temp/room-rental-snapshot-fixed.pdf.

- No new confirmed regression was found in the executed checks. Render checks are smoke tests, not exhaustive coverage of every control/console warning.
- Vite shutdown output again contained the existing React duplicate-key `1` warning during the broad route checks. It did not appear in the focused final-bill checks. Its originating component remains untriaged; this is an OPEN pre-existing warning, not evidence that every UI console is clean. No unrelated fix was made.
- QA backend logs also contain HTTP-only host HTTPS redirect and EF multiple-collection query warnings. These are recorded environment/performance limitations, not new failed workflow assertions.
- ISSUE-12 and VNPay real bank verification remain explicitly open.
- Agent did not execute commit or push. During verification, repository HEAD changed externally to 6a46a82 (test cac chuc nang), containing the fixes and prior frontend work. That commit was not reverted or modified.
```text
Suggested next action: approve or revise the exact ISSUE-12 schema and legacy reconciliation policy above.
```
