import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const sessions = JSON.parse(await readFile(process.env.LANDLORD_UI_AUTH, 'utf8'));
const output = process.env.LANDLORD_UI_OUTPUT;
assert.ok(output, 'Set LANDLORD_UI_OUTPUT to a local artifact directory.');
await mkdir(output, { recursive: true });
const base = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5173';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const checks = [], skipped = [];
const pages = [
  ['overview', '/landlord/dashboard'], ['rooms', '/landlord/rooms'], ['posts', '/landlord/posts'],
  ['appointments', '/landlord/appointments'], ['requests', '/landlord/contracts?tab=requests'],
  ['deposits', '/landlord/contracts?tab=requests&section=deposits'],
  ['contracts', '/landlord/contracts?tab=contracts'], ['bills', '/landlord/contracts?tab=bills'],
];
try {
  for (const width of [1440, 768, 375]) {
    const context = await browser.newContext({ viewport: { width, height: 960 } });
    await context.addInitScript(auth => {
      const { token, ...user } = auth;
      localStorage.setItem('room_rental_token', token);
      localStorage.setItem('room_rental_user', JSON.stringify(user));
    }, sessions.landlord);
    const page = await context.newPage();
    const errors = [], apiErrors = [], writes = [];
    page.on('pageerror', e => errors.push(e.message));
    page.on('dialog', async dialog => { errors.push('Unexpected native dialog: ' + dialog.type()); await dialog.dismiss(); });
    page.on('response', r => { if (r.status() >= 400 && new URL(r.url()).pathname.startsWith('/api/')) apiErrors.push(new URL(r.url()).pathname + ':' + r.status()); });
    page.on('request', r => { if (/\/api\//.test(r.url()) && !['GET', 'OPTIONS'].includes(r.method())) writes.push(r.method() + ' ' + new URL(r.url()).pathname); });
    const inspect = async name => {
      await page.evaluate(() => window.scrollTo(0, 0));
      await page.evaluate(() => document.fonts.ready);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, name + ' document overflow');
      const dialog = page.getByRole('dialog');
      if (await dialog.count()) {
        const bounds = await dialog.boundingBox();
        assert.ok(bounds && bounds.x >= 0 && bounds.y >= 0 && bounds.x + bounds.width <= width + 1 && bounds.y + bounds.height <= 961, name + ' dialog bounds');
        assert.equal(await dialog.evaluate(el => el.scrollWidth > el.clientWidth + 1), false, name + ' dialog horizontal overflow');
        assert.equal(await page.evaluate(() => document.body.style.overflow), 'hidden');
      }
      await page.screenshot({ path: path.join(output, `${name}-${width}.png`), fullPage: true });
      checks.push({ width, name, status: 'PASS' });
    };
    const closeDialog = async () => {
      await page.getByRole('dialog').getByRole('button', { name: 'Đóng', exact: true }).first().click();
      await page.getByRole('dialog').waitFor({ state: 'hidden' });
      assert.notEqual(await page.evaluate(() => document.body.style.overflow), 'hidden');
    };
    for (const [name, route] of pages) {
      await page.goto(base + route);
      await page.getByRole('heading', { level: 1 }).waitFor();
      await page.getByText(/Đang tải/).first().waitFor({ state: 'hidden' });
      await inspect(name);
      if (name === 'overview') {
        await page.getByRole('link', { name: 'Đăng tin mới', exact: true }).first().click();
        await page.getByLabel('Tiêu đề tin đăng').waitFor();
        assert.equal(new URL(page.url()).pathname, '/landlord/dashboard');
        await inspect('overview-create-post-modal'); await closeDialog();
        if (width < 1025) {
          await page.getByRole('button', { name: 'Mở menu', exact: true }).click();
          assert.equal(await page.locator('nav a').count(), 8);
          await inspect('sidebar');
          await page.keyboard.press('Escape');
        } else assert.equal(await page.locator('nav a').count(), 8);
        await page.locator('.landlord-account-menu summary').click();
        await inspect('account-menu');
        await page.locator('.landlord-account-menu summary').click();
      }
      if (name === 'rooms') {
        await page.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
        await page.getByRole('dialog').waitFor();
        await page.getByLabel('Tên phòng').fill('Bản nháp kiểm tra giao diện');
        await inspect('room-create-modal');
        await page.keyboard.press('Escape');
        await page.getByRole('dialog').waitFor({ state: 'hidden' });
        const edit = page.getByRole('button', { name: 'Sửa', exact: true }).first();
        if (await edit.count()) {
          await edit.click();
          assert.notEqual(await page.getByLabel('Tên phòng').inputValue(), '');
          await inspect('room-edit-modal');
          const roomName = await page.getByLabel('Tên phòng').inputValue();
          await page.getByLabel('Giá thuê (VNĐ/tháng)').fill('0');
          await page.getByRole('dialog').locator('form').evaluate(form => { form.noValidate = true; });
          await page.getByRole('dialog').getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
          await page.getByRole('dialog').getByRole('alert').waitFor();
          assert.equal(await page.getByLabel('Tên phòng').inputValue(), roomName);
          assert.equal(await page.getByLabel('Giá thuê (VNĐ/tháng)').inputValue(), '0');
          await inspect('room-validation-retains-data');
          await closeDialog();
        }
        const suspend = page.getByRole('button', { name: 'Tạm ngưng', exact: true }).and(page.locator(':enabled')).first();
        if (await suspend.count()) { await suspend.click(); await inspect('room-suspend-confirm'); await closeDialog(); }
        const status = page.getByRole('button', { name: 'Đổi trạng thái', exact: true }).and(page.locator(':enabled')).first();
        if (await status.count()) { await status.click(); await inspect('room-status-confirm'); await closeDialog(); }
      }
      if (name === 'posts') {
        await page.getByRole('button', { name: 'Đăng tin mới', exact: true }).click();
        await page.getByLabel('Tiêu đề tin đăng').waitFor();
        await page.getByLabel('Tiêu đề tin đăng').fill('Bản nháp chưa gửi');
        await inspect('post-create-modal');
        await closeDialog();
        const edit = page.getByRole('button', { name: /^Chỉnh sửa / }).first();
        if (await edit.count()) {
          await edit.click();
          const confirm = page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true });
          if (await confirm.count()) { await inspect('post-edit-warning'); await confirm.click(); }
          await page.getByLabel('Tiêu đề tin đăng').waitFor();
          assert.notEqual(await page.getByLabel('Tiêu đề tin đăng').inputValue(), '');
          await inspect('post-edit-modal');
          await page.getByLabel('Tiêu đề tin đăng').fill('   ');
          await page.getByRole('dialog').getByRole('button', { name: 'Lưu tin đăng', exact: true }).click();
          await page.getByRole('dialog').getByRole('alert').waitFor();
          assert.equal(await page.getByLabel('Tiêu đề tin đăng').inputValue(), '   ');
          await inspect('post-validation-retains-data');
          await closeDialog();
        }
        const remove = page.getByRole('button', { name: /^Xóa / }).first();
        if (await remove.count()) { await remove.click(); await inspect('post-delete-confirm'); await closeDialog(); }
        const publicRow = page.locator('tbody tr').filter({ hasText: 'Đã duyệt' }).filter({ hasText: 'Còn trống' }).first();
        if (await publicRow.count()) {
          await publicRow.getByRole('button', { name: /^Xem chi tiết / }).click();
          await page.getByRole('dialog').getByRole('link', { name: 'Xem tin công khai' }).waitFor();
          await inspect('post-detail-modal'); await closeDialog();
        } else skipped.push({ width, name, label: 'Public post detail', reason: 'No Approved + Available post' });
      }
      if (name === 'bills') {
        await page.getByRole('button', { name: /Nhập Số Điện Nước/ }).click();
        await inspect('bill-create-modal'); await closeDialog();
        const edit = page.getByRole('button', { name: 'Sửa', exact: true }).first();
        if (await edit.count()) { await edit.click(); await inspect('bill-edit-modal'); await closeDialog(); }
      }
      const actions = name === 'appointments' ? ['Chấp nhận', 'Từ chối', 'Hoàn thành']
        : name === 'requests' ? ['Duyệt Yêu Cầu', 'Từ Chối', 'Tạo Hợp Đồng Thuê', 'Bổ sung cọc (dữ liệu cũ)']
        : name === 'deposits' ? ['Xác nhận đã nhận cọc']
        : name === 'contracts' ? ['Ký hợp đồng', 'Chấm Dứt HĐ']
        : name === 'bills' ? ['Đã thu', 'Hủy'] : [];
      for (const label of actions) {
        const button = page.getByRole('button', { name: label, exact: true }).first();
        if (!await button.count()) { skipped.push({ width, name, label, reason: 'No existing record with permitted action' }); continue; }
        await button.click();
        await inspect(name + '-action-' + actions.indexOf(label));
        await closeDialog();
      }
    }
    await page.goto(base + '/landlord/posts/create');
    await page.getByLabel('Tiêu đề tin đăng').waitFor();
    await inspect('post-direct-route');
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click();
    await page.waitForURL('**/landlord/posts');
    assert.deepEqual(errors, []);
    assert.deepEqual(apiErrors, []);
    assert.deepEqual(writes, [], 'Read-only audit must not change application data');
    checks.push({ width, name: 'No native dialogs, runtime errors, failed API responses or API writes', status: 'PASS' });
    console.log('PASS landlord width ' + width);
    await context.close();
  }
  for (const role of ['admin', 'tenant']) {
    const context = await browser.newContext({ viewport: { width: 375, height: 900 } });
    await context.addInitScript(auth => {
      const { token, ...user } = auth;
      localStorage.setItem('room_rental_token', token);
      localStorage.setItem('room_rental_user', JSON.stringify(user));
    }, sessions[role]);
    const page = await context.newPage();
    await page.goto(base + (role === 'admin' ? '/admin/posts' : '/rooms'));
    await page.getByRole('heading', { level: 1 }).waitFor();
    assert.equal(await page.locator('.landlord-dashboard-shell').count(), 0);
    await page.screenshot({ path: path.join(output, role + '-unchanged-375.png'), fullPage: true });
    checks.push({ role, name: 'Landlord styles not applied', status: 'PASS' });
    await context.close();
  }
} finally {
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({ dataMode: 'existing-local-data', databaseWrites: false, checks, skipped }, null, 2));
}
console.log('PASS ' + checks.length + ' UI checks; ' + skipped.length + ' unavailable state-specific actions recorded.');
