import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.UI_AUDIT_NODE_MODULES
  ? path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright') : 'playwright');
const baseURL = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5173';
const output = path.resolve('../docs/ui-audit/room-availability');
await mkdir(output, { recursive: true });
const posts = [1, 0, 2].map((roomStatus, index) => ({
  id: index + 1, title: ['Phòng Bích Tùng đã có người thuê', 'Phòng đang còn trống', 'Phòng đang được giữ chỗ'][index],
  roomStatus, status: 1, price: 3000000, area: 25, maxOccupants: 2, categoryId: 1, categoryName: 'Phòng trọ',
  address: '42 Nguyễn Lân', ward: 'Phương Mai', district: 'Thanh Xuân', province: 'Hà Nội',
  landlordId: 20, landlordAccountId: 20, landlordName: 'Chủ trọ Bích Tùng', landlordPhone: '0901234567',
  thumbnailUrl: '/room-placeholder.svg', imageUrls: ['/room-placeholder.svg'], amenities: [],
  description: 'Phòng trọ có cửa sổ và bếp riêng.', createdAt: new Date().toISOString(), viewCount: 30,
}));
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const checks = [];
let currentPage;
try {
  for (const width of [1440, 768, 375]) {
    const context = await browser.newContext({ viewport: { width, height: 900 } });
    await context.addInitScript(() => {
      localStorage.setItem('room_rental_token', 'availability-fixture');
      localStorage.setItem('room_rental_user', JSON.stringify({ id: 21, userId: 21, fullName: 'Người thuê kiểm thử', role: 'Tenant' }));
    });
    const page = await context.newPage();
    currentPage = page;
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'Fixture test' }));
    await page.route('**/api/**', route => {
      const p = new URL(route.request().url()).pathname;
      let data = [];
      if (p.includes('danh-gia')) data = [];
      else if (p.includes('bai-dang')) data = /\/\d+$/.test(p) ? posts.find(post => post.id === Number(p.split('/').at(-1))) : posts;
      else if (p.includes('kiem-tra')) data = false;
      else if (p.includes('yeu-thich/cua-toi')) data = posts;
      else if (p.includes('danh-muc')) data = [{ id: 1, name: 'Phòng trọ' }];
      return route.fulfill({ json: { success: true, data, message: 'OK' } });
    });
    const assertLayout = async () => {
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'Horizontal overflow');
      const clipped = await page.locator('.room-availability-badge').evaluateAll(items => items.some(el => el.scrollWidth > el.clientWidth + 1 || el.scrollHeight > el.clientHeight + 1));
      assert.equal(clipped, false, 'Availability label is clipped');
    };
    await page.goto(baseURL);
    const homeCard = page.locator('.room-card').filter({ hasText: posts[0].title });
    await homeCard.waitFor();
    assert.equal(await homeCard.locator('.room-availability-badge').innerText(), 'Hết phòng');
    assert.equal(await page.locator('.recent-card').filter({ hasText: posts[0].title }).locator('.room-availability-badge').innerText(), 'Hết phòng');
    await assertLayout();
    await homeCard.scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(output, 'home-' + width + '.png') });
    checks.push({ width, test: 'Home featured and recent rented posts remain visible with sold-out label' });

    await page.goto(baseURL + '/rooms');
    const rentedCard = page.locator('.room-list-card').filter({ hasText: posts[0].title });
    await rentedCard.waitFor();
    assert.equal(await rentedCard.locator('.room-availability-badge').innerText(), 'Hết phòng');
    assert.equal(await page.locator('.room-list-card').filter({ hasText: posts[1].title }).locator('.room-availability-badge').innerText(), 'Còn trống');
    await assertLayout();
    await rentedCard.scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(output, 'list-' + width + '.png') });
    await rentedCard.click();
    await page.getByRole('heading', { name: posts[0].title, exact: true, level: 1 }).waitFor();
    assert.equal(await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).count(), 0);
    assert.equal(await page.getByRole('button', { name: 'Gửi yêu cầu thuê phòng', exact: true }).count(), 0);
    assert.ok(await page.getByText('Hết phòng', { exact: true }).count() >= 1);
    await assertLayout();
    await page.locator('.room-information-grid').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(output, 'detail-' + width + '.png') });
    checks.push({ width, test: 'Rented listing opens detail and cannot book or request rental' });

    await page.goto(baseURL + '/rooms/3');
    await page.getByRole('heading', { name: posts[2].title, exact: true, level: 1 }).waitFor();
    assert.ok(await page.getByText('Đã giữ chỗ', { exact: true }).count() >= 1);
    assert.equal(await page.getByRole('button', { name: 'Gửi yêu cầu thuê phòng', exact: true }).count(), 0);
    assert.equal(await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).count(), 0);
    checks.push({ width, test: 'Reserved detail is read-only for new bookings and rentals' });

    await page.goto(baseURL + '/rooms/2');
    await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).waitFor();
    await page.getByRole('button', { name: 'Gửi yêu cầu thuê phòng', exact: true }).click();
    await page.getByRole('dialog').waitFor();
    await page.keyboard.press('Escape');
    const similar = page.locator('.room-similar-section a[href="/rooms/1"]');
    await similar.waitFor();
    assert.equal(await similar.locator('.room-availability-badge').innerText(), 'Hết phòng');
    await assertLayout();
    checks.push({ width, test: 'Available room keeps rental actions; similar rented room is correctly labelled' });

    await page.goto(baseURL + '/tenant/favorites');
    await page.getByRole('link', { name: posts[0].title, exact: true }).waitFor();
    assert.equal(await page.getByText('Hết phòng', { exact: true }).count(), 1);
    await assertLayout();
    assert.deepEqual(errors, []);
    checks.push({ width, test: 'Favorites show sold-out room; no React errors or mobile overflow' });
    await context.close();
    console.log('Passed availability checks at ' + width + 'px');
  }
} catch (error) {
  if (currentPage && !currentPage.isClosed()) {
    await currentPage.screenshot({ path: path.join(output, 'failure.png'), fullPage: true });
    console.log({ url: currentPage.url(), badges: await currentPage.locator('.room-availability-badge').allTextContents(), body: (await currentPage.locator('body').innerText()).slice(0, 3000) });
  }
  throw error;
} finally {
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({ fixtureMode: true, checks }, null, 2));
}
console.log('Passed ' + checks.length + ' availability checks');
