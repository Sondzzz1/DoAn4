import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const session = JSON.parse(await readFile(process.env.LOCAL_SMOKE_AUTH, 'utf8'));
const base = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5173';
const output = path.resolve('../docs/ui-audit/local-workflow');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const checks = [];
try {
  for (const width of [375, 768]) {
    for (const role of ['tenant', 'landlord', 'admin']) {
      const context = await browser.newContext({ viewport: { width, height: 900 } });
      await context.addInitScript(auth => {
        const { token, ...user } = auth;
        localStorage.setItem('room_rental_token', token);
        localStorage.setItem('room_rental_user', JSON.stringify({ ...user, id: user.userId }));
      }, session.roles[role]);
      const page = await context.newPage();
      const errors = [], warnings = [], failed = [], badResponses = [];
      page.on('pageerror', e => errors.push(e.message));
      page.on('console', message => {
        if (/SignalR|WebSocket|connection.*(closed|stop)|negotiation.*abort/i.test(message.text())) return;
        if (message.type() === 'error') errors.push(message.text());
        if (message.type() === 'warning') warnings.push(message.text());
      });
      page.on('requestfailed', r => {
        if (!r.url().includes('/hubs/') && r.failure()?.errorText !== 'net::ERR_ABORTED') failed.push(r.url());
      });
      page.on('response', r => { if (r.status() >= 400 && new URL(r.url()).pathname.startsWith('/api/')) badResponses.push(r.url() + ': ' + r.status()); });
      const inspect = async name => {
        await page.evaluate(() => window.scrollTo(0, 0));
        await page.screenshot({ path: path.join(output, `${role}-${name}-${width}.png`), fullPage: true });
        const overflow = await page.evaluate(() => ({
          page: document.documentElement.scrollWidth > innerWidth + 1,
          elements: [...document.querySelectorAll('body *')].filter(e => e.getBoundingClientRect().right > innerWidth + 2).slice(0, 8).map(e => ({ tag: e.tagName, className: String(e.className).slice(0, 120), text: e.textContent.slice(0, 60) })),
        }));
        assert.equal(overflow.page, false, JSON.stringify({ width, role, name, overflow }));
        checks.push({ width, role, name, status: 'PASS' });
      };
      if (role === 'tenant') {
        await page.goto(base + '/rooms?keyword=' + session.prefix + '&minPrice=3000000&pageNumber=1');
        await page.getByRole('status').filter({ hasText: '1 phòng phù hợp' }).waitFor();
        await inspect('search');
        await page.getByRole('button', { name: 'Bộ lọc', exact: true }).click();
        await page.getByRole('dialog').getByLabel('Quận/Huyện', { exact: true }).fill('Thanh Xuân');
        await page.getByRole('dialog').getByRole('button', { name: 'Tìm phòng', exact: true }).click();
        await page.waitForURL(u => u.searchParams.get('district') === 'Thanh Xuân');
        await page.getByRole('status').filter({ hasText: '1 phòng phù hợp' }).waitFor();
        await page.reload();
        await page.getByRole('status').filter({ hasText: '1 phòng phù hợp' }).waitFor();
        await inspect('filter-reload');
        await page.goto(base + '/rooms/' + session.postId);
        await page.getByRole('heading', { level: 1, name: new RegExp(session.prefix) }).waitFor();
        await page.locator('.leaflet-container').waitFor();
        await page.waitForFunction(() => document.querySelector('.leaflet-tile-loaded') !== null);
        await inspect('detail-map');
        await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).click();
        await page.getByRole('dialog').waitFor();
        await inspect('appointment-modal');
        await page.goto(base + '/tenant/rentals');
        await page.getByText('Đã thanh toán', { exact: true }).first().waitFor();
        await inspect('paid-bill');
        await page.getByRole('button', { name: /Hợp đồng \(/ }).click();
        await page.getByText('Đã chấm dứt', { exact: true }).waitFor();
        await inspect('terminated-contract');
        await page.getByRole('button', { name: /Yêu cầu & Cọc/ }).click();
        await page.getByText(/Thông tin giá hoặc điều kiện phòng đã thay đổi/).waitFor();
        await inspect('request-reason');
      } else if (role === 'landlord') {
        await page.goto(base + '/landlord/rooms');
        await page.getByLabel('Tên phòng').waitFor();
        await page.getByText('QA phong thuc ' + session.prefix, { exact: true }).first().waitFor();
        await page.locator('.leaflet-container').waitFor();
        await inspect('room-form');
        await page.goto(base + '/landlord/contracts');
        await page.getByRole('button', { name: /Danh Sách Hợp Đồng/ }).click();
        await page.getByText(/Đã chấm dứt/).first().waitFor();
        await inspect('workflow');
      } else {
        await page.goto(base + '/admin/posts');
        await page.getByText('QA phòng có cửa sổ ' + session.prefix, { exact: true }).waitFor();
        await inspect('post-table');
      }
      assert.deepEqual(errors, []);
      assert.deepEqual(warnings, []);
      assert.deepEqual(badResponses, []);
      assert.deepEqual(failed, []);
      checks.push({ width, role, name: 'Console, warnings, API errors and failed requests', status: 'PASS' });
      await context.close();
      console.log('PASS live data ' + role + ' width ' + width);
    }
  }
} finally {
  await browser.close();
  await writeFile(path.join(output, 'ui-results.json'), JSON.stringify({ fixtureMode: false, database: 'RoomRentalDB', postId: session.postId, checks }, null, 2));
}
console.log('PASS ' + checks.length + ' real-data browser checks');
