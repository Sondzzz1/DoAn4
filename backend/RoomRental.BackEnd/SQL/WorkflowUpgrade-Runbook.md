# Workflow Upgrade Runbook

Ap dung cho database `RoomRentalDB` dang dung schema tieng Viet cua backend (`TinDang`, `PhongTro`, `HopDongThue`). Bo script nay khong dung cho schema demo cu co cac bang `Posts`, `Rooms`, `Users`.

## Thu tu chay trong SSMS

1. Tao backup bang giao dien SSMS: chuot phai `RoomRentalDB` -> `Tasks` -> `Back Up...` -> `Full`.
2. Mo va chay [00_Preflight_WorkflowUpgrade.sql](00_Preflight_WorkflowUpgrade.sql). Khong chay buoc tiep theo neu co `ProblemCount > 0` o duplicate/orphan check.
3. Mo va chay [01_Deploy_WorkflowUpgrade.sql](01_Deploy_WorkflowUpgrade.sql). Script tu rollback toan bo neu co loi.
4. Mo va chay [02_Verify_WorkflowUpgrade.sql](02_Verify_WorkflowUpgrade.sql). Tat ca FK va index duoc liet ke, hai dong orphan phai co `ProblemCount = 0`.
5. Khoi dong backend va thu luong: gui yeu cau thue -> duyet -> tao/xac nhan dat coc -> tao/xac nhan hop dong -> lap hoa don.

## Nang cap han thanh toan dat coc

Neu ban da tung chay `01_Deploy_WorkflowUpgrade.sql` truoc khi co chuc nang chu tro thiet lap dat coc, chay them [04_AddDepositDeadline.sql](04_AddDepositDeadline.sql) mot lan. Script bo sung `HanThanhToan` cho `DatCoc`, cap 24 gio xu ly cho cac khoan coc cu dang cho thanh toan va tao index cho worker kiem tra qua han.

Sau khi chay script, backend tu dong kiem tra moi 5 phut. Khoan coc chua thanh toan qua han se chuyen sang het han, yeu cau thue het han va phong duoc tra lai trang thai con trong.

## Canh bao quan trong

- Script dat dung database `RoomRentalDB`. Neu database cua ban co ten khac, sua dong `USE [RoomRentalDB]` trong ca ba file truoc khi chay.
- Neu `00_Preflight` bao `Legacy contract table`, script deploy se doi ten bang cu thanh `HopDongThue_Legacy` va tao bang workflow moi. Chi chay tren database demo/test, hoac khi ban da co backup va da chap nhan tach du lieu hop dong cu.
- Khong chay `RoomRentalDb_Schema_And_Data.sql` cua schema demo cu tren `RoomRentalDB`: file do co the tao/reset schema khac voi backend hien tai.
- Khong can chay lai `Phase2_P0_WorkflowSafety.sql` hay `Phase3_P1_RentalReservation.sql` sau khi da chay `01_Deploy_WorkflowUpgrade.sql`; script tong hop da bao gom cac rang buoc cua hai phase nay.
