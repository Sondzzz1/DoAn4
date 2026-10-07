import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const base = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5173';
const output = path.resolve('../docs/ui-audit/final-workflow');
await mkdir(output, { recursive: true });
const post = { id: 1, roomId: 1, title: 'Phòng có cửa sổ', description: 'Phòng sạch, bếp riêng', price: 3000000, area: 25, maxOccupants: 2,
  status: 1, roomStatus: 0, categoryId: 1, categoryName: 'Phòng trọ', address: '42 Nguyễn Lân', province: 'Hà Nội', district: 'Thanh Xuân', ward: 'Phương Mai',
  latitude: 21, longitude: 105, landlordId: 20, landlordAccountId: 20, landlordName: 'Chủ trọ', landlordPhone: '0901234567',
  thumbnailUrl: '/room-placeholder.svg', imageUrls: ['/room-placeholder.svg'], amenities: [{ id: 1, name: 'Wifi' }], createdAt: new Date().toISOString() };
const rooms = [
  { ...post, id: 11, roomName: 'Phòng đủ điều kiện', status: 0, activePostId: null, amenityIds: [1] },
  { ...post, id: 12, roomName: 'Phòng đã có tin', status: 0, activePostId: 2, amenityIds: [] },
  { ...post, id: 13, roomName: 'Phòng giữ chỗ', status: 2, activePostId: null, amenityIds: [] },
];
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const checks = [];
try {
  for (const width of [1440, 1200, 768, 375]) {
    const context = await browser.newContext({ viewport: { width, height: 900 } });
    await context.addInitScript(() => {
      if (!localStorage.getItem('room_rental_token')) {
        localStorage.setItem('room_rental_token', 'fixture');
        localStorage.setItem('room_rental_user', JSON.stringify({ id: 20, userId: 20, fullName: 'Chủ trọ', role: 'Landlord' }));
      }
    });
    const page = await context.newPage();
    const errors = [], failed = [], requests = [], payloads = [], consoleErrors = [], warnings = [], badResponses = [];
    page.on('pageerror', e => errors.push(e.message));
    page.on('console', message => {
      if (/503|negotiat|SignalR|WebSocket/.test(message.text())) return;
      if (message.type() === 'error') consoleErrors.push(message.text());
      if (message.type() === 'warning') warnings.push(message.text());
    });
    page.on('response', response => {
      if (response.status() >= 400 && !response.url().includes('/hubs/')) badResponses.push(response.url() + ': ' + response.status());
    });
    page.on('requestfailed', r => { if (!r.url().includes('/hubs/')) failed.push(r.url()); });
    await page.route('**/hubs/**', r => r.fulfill({ status: 503, body: 'Fixture' }));
    await page.route('**/api/**', async route => {
      const request = route.request(), url = new URL(request.url()), p = url.pathname;
      requests.push(url);
      let data = [];
      if (p.endsWith('/bai-dang/search')) {
        const pageNumber = Number(url.searchParams.get('pageNumber') || 1);
        data = { items: [post], totalCount: 25, pageNumber, pageSize: 12, totalPages: 3 };
      } else if (p.includes('danh-gia')) data = [];
      else if (p.endsWith('/phong')) data = rooms;
      else if (p.includes('bai-dang') && request.method() === 'POST') { payloads.push(request.postDataJSON()); data = post; }
      else if (p.includes('bai-dang/cua-toi')) data = [];
      else if (/bai-dang\/\d+$/.test(p)) data = { ...post, roomStatus: Number(p.split('/').at(-1)) === 3 ? 2 : 1 };
      else if (p.includes('bai-dang')) data = [post];
      else if (p.includes('danh-muc')) data = [{ id: 1, name: 'Phòng trọ' }];
      else if (p.includes('tien-ich')) data = [{ id: 1, name: 'Wifi', isActive: true }];
      else if (p.includes('chua-doc') || p.includes('unread')) data = 0;
      return route.fulfill({ json: { success: true, data, message: 'OK' } });
    });
    const layout = async () => assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'Overflow at ' + width);
    await page.goto(base + '/rooms?province=H%C3%A0+N%E1%BB%99i&pageNumber=2');
    await page.getByRole('status').filter({ hasText: '25 phòng phù hợp' }).waitFor();
    assert.ok(requests.some(u => u.pathname.endsWith('/bai-dang/search') && u.searchParams.get('pageNumber') === '2'));
    await page.getByRole('button', { name: 'Trang sau', exact: true }).click();
    await page.waitForURL('**pageNumber=3');
    assert.equal(new URL(page.url()).searchParams.get('province'), 'Hà Nội');
    await page.reload();
    await page.getByRole('status').filter({ hasText: '25 phòng phù hợp' }).waitFor();
    if (width <= 900) await page.getByRole('button', { name: 'Bộ lọc', exact: true }).click();
    const scope = width <= 900 ? page.getByRole('dialog') : page.locator('.search-desktop-filter');
    await scope.getByLabel('Quận/Huyện', { exact: true }).fill('Thanh Xuân');
    await scope.getByLabel('Wifi', { exact: true }).check();
    await scope.getByRole('button', { name: 'Tìm phòng', exact: true }).click();
    await page.waitForURL(u => u.searchParams.get('district') === 'Thanh Xuân');
    assert.equal(new URL(page.url()).searchParams.get('pageNumber'), '1');
    assert.equal(new URL(page.url()).searchParams.get('amenityIds'), '1');
    await page.getByRole('status').filter({ hasText: '25 phòng phù hợp' }).waitFor();
    await layout();
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: path.join(output, 'search-' + width + '.png'), fullPage: true });
    checks.push({ width, test: 'Search real count, page/reload/filter URL and mobile modal' });
    await page.goto(base + '/landlord/posts/create');
    await page.getByLabel('Phòng trọ', { exact: true }).waitFor();
    assert.equal(await page.locator('#post-room option').count(), 2);
    await page.getByLabel('Phòng trọ', { exact: true }).selectOption('11');
    await page.getByRole('button', { name: 'Gửi duyệt', exact: true }).click();
    await page.waitForURL('**/landlord/posts');
    assert.deepEqual(payloads.at(-1), { roomId: 11, title: rooms[0].roomName, description: post.description });
    checks.push({ width, test: 'Owned available room selector and RoomId-only publication payload' });
    await page.goto(base + '/rooms/1');
    await page.getByRole('heading', { name: post.title, exact: true, level: 1 }).waitFor();
    assert.equal(await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).count(), 0);
    assert.equal(await page.getByRole('button', { name: 'Gửi yêu cầu thuê phòng', exact: true }).count(), 0);
    assert.ok(await page.getByText('Đã thuê', { exact: true }).count() > 0);
    await layout();
    await page.screenshot({ path: path.join(output, 'detail-' + width + '.png') });
    await page.goto(base + '/rooms/3');
    await page.getByRole('heading', { name: post.title, exact: true, level: 1 }).waitFor();
    assert.ok(await page.getByText('Đang giữ chỗ', { exact: true }).count() > 0);
    assert.equal(await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).count(), 0);
    checks.push({ width, test: 'Rented/reserved detail remains readable without new booking actions' });
    await page.goto(base + '/landlord/rooms');
    await page.getByLabel('Tên phòng').waitFor();
    await page.getByLabel('Giá điện (đ/kWh)').fill('3500');
    await page.getByLabel('Ảnh phòng', { exact: true }).fill('/room-placeholder.svg');
    await page.getByLabel('Wifi', { exact: true }).check();
    await page.locator('.leaflet-container').waitFor();
    await layout();
    await page.screenshot({ path: path.join(output, 'room-form-' + width + '.png'), fullPage: true });
    checks.push({ width, test: 'Room form category, rates, amenities, images, map and layout' });
    await page.evaluate(() => localStorage.setItem('room_rental_user', JSON.stringify({ id: 10, fullName: 'Tenant', role: 'Tenant' })));
    await page.goto(base + '/landlord/posts/create');
    await page.waitForURL('**/rooms');
    await page.getByRole('status').filter({ hasText: '25 phòng phù hợp' }).waitFor();
    assert.equal(await page.locator('#post-room').count(), 0);
    const anonymous = await browser.newContext({ viewport: { width, height: 900 } });
    const anonymousPage = await anonymous.newPage();
    await anonymousPage.goto(base + '/landlord/rooms');
    await anonymousPage.waitForURL('**/login');
    assert.ok(await anonymousPage.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
    await anonymous.close();
    checks.push({ width, test: 'Protected routes reject anonymous and incorrect role' });
    await page.evaluate(() => localStorage.setItem('room_rental_user', JSON.stringify({ id: 30, fullName: 'Admin', role: 'Admin' })));
    await page.goto(base + '/admin/rooms');
    await page.getByRole('cell', { name: 'Phòng đủ điều kiện', exact: false }).waitFor();
    assert.equal(await page.locator('table tbody tr').count(), 3);
    await layout();
    await page.screenshot({ path: path.join(output, 'admin-table-' + width + '.png'), fullPage: true });
    await page.goto(base + '/admin/categories');
    await page.getByRole('button', { name: 'Thêm mới', exact: true }).click();
    await page.getByRole('dialog').waitFor();
    await layout();
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click();
    checks.push({ width, test: 'Admin table and catalog modal layout' });
    assert.deepEqual(errors, []);
    assert.deepEqual(failed, []);
    assert.deepEqual(consoleErrors, []);
    assert.deepEqual(badResponses, []);
    checks.push({ width, test: 'Console and HTTP errors', warnings });
    await context.close();
    console.log('PASS width ' + width);
  }
} finally {
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({ fixtureMode: true, checks }, null, 2));
}
console.log('PASS ' + checks.length + ' browser groups');
