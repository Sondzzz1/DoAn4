# Local Workflow Verification

Completed: 2026-10-08. Follow-up to FINAL_WORKFLOW_AUDIT.md.

## Request Terms Policy

- Meaningful public Room changes cancel Pending requests across its posts. CancellationReason is separate from the original tenant Note. Tenants must review the new conditions and submit again after moderation.
- An Approved request awaiting conversion to a contract blocks public Room edits, including while its deposit is Pending, Paid or Confirmed. The API returns 409 without changing the room.
- Converted requests retain their contract snapshots. Later Room edits do not rewrite signed terms.
- No-op edits do not cancel requests or reset moderation. Request creation and Room updates share the workflow lock and transactional checks.
- Approved posts return to Pending after public Room changes and leave public search until reapproved. Public search remains Approved + Available only.

## Legacy Deposit Repair

ThietLapDatCocAsync still supports historical Approved requests without a deposit, so deleting it would break recovery of old data. It is marked obsolete, hidden from Swagger and retained for an ownership-checked legacy repair action. New approval creates the deposit atomically and does not call this endpoint separately. Duplicate deposits, conflicting reservations and existing contracts are rejected.

## Database

06_RentalRequestCancellationReason.sql adds nullable YeuCauThuePhong.LyDoHuy (nvarchar(500)); it does not overwrite Note or delete history. Applied successfully to the local RoomRentalDB and rerun for idempotency. The current bootstrap includes the column. A separate verification database passed bootstrap twice plus script 06, with 24 tables and a 1000-byte Unicode reason column.

## Verification Results

- Backend Release build: PASS, zero warnings/errors. Backend suite: 108 passed, zero failed/skipped, including seven request-policy regression cases.
- Frontend production build and lint: PASS.
- Real local RoomRentalDB HTTP scenario: 18 checks PASS. This is an API-driven workflow with subsequent browser interactions, not a claim that every lifecycle step was clicked manually.
- Real-data Playwright browser checks: 26 PASS across 375px and 768px, Tenant/Landlord/Admin, with no fixtures. Search filters survive reload; detail map tiles load; appointment modal, cancellation reason, paid bill, terminated contract and management pages render. No document-level horizontal overflow or unexpected console/API/request failures. Management tables retain internal horizontal scrolling.
- Price edit from 3,000,000 to 3,250,000: Post becomes Pending, disappears from search, old Pending request is cancelled with its original note retained; Admin reapproval permits a new request.
- Workflow covered Room -> Post -> approval -> filtered search/detail -> appointment -> request approval/deposit -> Reserved -> deposit payment/confirmation -> contract/both signatures -> Active/Rented -> bill/payment -> termination -> Available and visible in search again.

## Payment Limitation

Deposit and bill payments used signed local test IPN callbacks, including duplicate callback checks. A temporary backend process had temporary test merchant settings; the normal server configuration was not changed and no bypass endpoint was introduced. This verifies application processing, NOT a transaction through the VNPay sandbox bank. A configured sandbox merchant and interactive bank checkout remain necessary for that external integration check.

## Evidence and Cleanup

- API evidence: [api-results.json](ui-audit/local-workflow/api-results.json).
- Browser evidence: [ui-results.json](ui-audit/local-workflow/ui-results.json); screenshots are in the same directory.
- Run prefix: qa20261007235319. Room 11, Post 7, old/new requests 6/7, Deposit 4, Contract 3, Bill 2, Appointment 7.
- After successful browser verification, --finish disabled the three dedicated QA accounts, hid the QA post and deleted the private temporary auth manifest. Existing user records were not changed. Business history is retained; the QA post is now deliberately hidden despite the earlier successful Available/search assertion.
- Test Admin provisioning is restricted to the newly created QA account. Credentials/tokens are not written into the report or repository.

## Reproduce

Back up the database before local test writes. From the project root, build the backend Debug target and the tools/RoomRental.LocalSmoke project, then run:

```powershell
dotnet run --project tools/RoomRental.LocalSmoke/RoomRental.LocalSmoke.csproj --no-build --no-restore -- --allow-local-test-writes
```

Start the normal backend on port 5000 and frontend on port 5173. From frontend, set UI_AUDIT_NODE_MODULES to the installed dependency directory containing Playwright, and LOCAL_SMOKE_AUTH to the OS temp file room-rental-local-smoke-auth.json. Run node scripts/local-workflow-ui-test.mjs. Tokens are used only for the dedicated QA session.

Finally, from the project root, always clean up the successful QA session:

```powershell
dotnet run --project tools/RoomRental.LocalSmoke/RoomRental.LocalSmoke.csproj --no-build --no-restore -- --allow-local-test-writes --finish
```
