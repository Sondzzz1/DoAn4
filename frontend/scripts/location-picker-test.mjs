import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.UI_AUDIT_NODE_MODULES
  ? path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright') : 'playwright');
const baseURL = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5173';
const output = path.resolve('../docs/ui-audit/location-picker');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const checks = [];
const saved = { latitude: 21.03, longitude: 105.84, displayName: '42 Nguyễn Lân, Hà Nội',
  address: '42 Nguyễn Lân', province: 'Hà Nội', district: 'Thanh Xuân', ward: 'Phương Mai' };
const selected = { ...saved, address: '88 Đường mới', province: 'Hưng Yên', district: 'Mỹ Hào', ward: 'Bần Yên Nhân', displayName: '88 Đường mới, Bần Yên Nhân, Mỹ Hào, Hưng Yên' };

try {
  for (const width of [1440, 768, 375]) {
    const context = await browser.newContext({ viewport: { width, height: 900 }, permissions: ['geolocation'], geolocation: { latitude: 20.94, longitude: 106.10 } });
    await context.addInitScript(() => {
      localStorage.setItem('room_rental_token', 'location-test-fixture');
      localStorage.setItem('room_rental_user', JSON.stringify({ id: 20, userId: 20, fullName: 'Chủ trọ kiểm thử', role: 'Landlord', email: 'landlord@example.test' }));
    });
    const page = await context.newPage();
    const pageErrors = [];
    page.on('pageerror', error => pageErrors.push(error.stack || error.message));
    const searchCalls = [];
    const reverseCalls = [];
    let searchDelay = 0;
    let reverseDelay = 0;
    let failSearch = false;
    let failReverse = false;
    let savedPayload;
    let roomPayload;
    await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'No backend in fixture test' }));
    await page.route('**/api/**', async route => {
      const url = new URL(route.request().url());
      const p = url.pathname;
      const ok = data => route.fulfill({ json: { success: true, data, message: 'OK' } });
      try {
        if (p.endsWith('/location/search')) {
          searchCalls.push(url.searchParams.get('q'));
          const delay = searchDelay;
          const failed = failSearch;
          const query = url.searchParams.get('q');
          if (delay) await new Promise(resolve => setTimeout(resolve, delay));
          if (failed) return route.fulfill({ status: 503, json: { success: false, message: 'Dịch vụ địa chỉ đang gián đoạn.' } });
          if (query.includes('Không có')) return ok([]);
          return ok([{ ...saved, latitude: query.includes('Địa chỉ B') ? 21.1 : saved.latitude }, { ...saved, latitude: 21.05, displayName: 'Địa chỉ tương tự khác' }]);
        }
        if (p.endsWith('/location/reverse')) {
          const lat = Number(url.searchParams.get('lat'));
          const lng = Number(url.searchParams.get('lng'));
          const index = reverseCalls.push({ lat, lng });
          const delay = reverseDelay;
          const failed = failReverse;
          if (delay) await new Promise(resolve => setTimeout(resolve, delay));
          if (failed) return route.fulfill({ status: 503, json: { success: false, message: 'Dịch vụ địa chỉ đang gián đoạn.' } });
          return ok({ ...selected, latitude: lat, longitude: lng, address: selected.address + ' ' + index });
        }
        if (p.endsWith('/bai-dang') && route.request().method() === 'POST') {
          savedPayload = route.request().postDataJSON();
          return ok({ id: 99 });
        }
        if (p.endsWith('/phong') && ['POST', 'PUT'].includes(route.request().method())) {
          roomPayload = route.request().postDataJSON();
          return ok({ id: 99 });
        }
        if (p.includes('/bai-dang/cua-toi/1')) return ok({ ...saved, title: 'Tin đã lưu', description: 'Phòng trọ có cửa sổ', price: 3000000, area: 25, maxOccupants: 2, amenities: [], imageUrls: [] });
        return ok([]);
      } catch (error) {
        if (!/closed|cancel|abort|Target|Invalid/.test(String(error))) throw error;
      }
    });
    const address = page.locator('#post-address');
    const blurAddress = () => page.locator('#post-title').focus();
    const coordinateText = () => page.locator('.selected-location').innerText();
    const waitAddress = async expected => { await page.waitForFunction(value => document.querySelector('#post-address')?.value === value, expected); };
    const mapClick = async (x, y) => {
      const map = page.locator('.leaflet-map-picker .leaflet-container');
      await map.scrollIntoViewIfNeeded();
      await map.click({ position: { x, y } });
    };

    await page.goto(baseURL + '/landlord/posts/create');
    await address.waitFor();
    await page.locator('#post-province').fill('Hà Nội');
    await page.locator('#post-district').fill('Thanh Xuân');
    await page.locator('#post-ward').fill('Phương Mai');
    await address.fill('42 Nguyễn Lân');
    await page.waitForTimeout(900);
    assert.equal(searchCalls.length, 0, 'No per-keystroke geocoding');
    await blurAddress();
    await page.locator('.leaflet-marker-icon').waitFor();
    assert.equal(searchCalls.length, 1);
    assert.equal(searchCalls[0], '42 Nguyễn Lân, Phương Mai, Thanh Xuân, Hà Nội, Việt Nam');
    assert.match(await coordinateText(), /21\.030000/);
    await page.getByRole('button', { name: 'Địa chỉ tương tự khác' }).click();
    assert.match(await coordinateText(), /21\.050000/);
    checks.push({ width, check: 'Completed address search, pin, alternatives, no typeahead', passed: true });

    await mapClick(180, 130);
    await waitAddress('88 Đường mới 1');
    assert.equal(await page.locator('#post-province').inputValue(), 'Hưng Yên');
    assert.equal(await page.locator('#post-district').inputValue(), 'Mỹ Hào');
    assert.equal(await page.locator('#post-ward').inputValue(), 'Bần Yên Nhân');
    await page.waitForTimeout(400);
    assert.equal(searchCalls.length, 1, 'Reverse lookup must not trigger a forward-search loop');
    assert.match(await coordinateText(), new RegExp(reverseCalls[0].lat.toFixed(6).replace('.', '\\.')));
    checks.push({ width, check: 'Map click updates address parts and exact coordinates', passed: true });

    const marker = page.locator('.leaflet-marker-icon');
    await marker.scrollIntoViewIfNeeded();
    const box = await marker.boundingBox();
    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width / 2 + 45, box.y + box.height / 2 + 30, { steps: 12 });
    await page.mouse.up();
    await waitAddress('88 Đường mới 2');
    checks.push({ width, check: 'Marker drag reverse lookup', passed: true });

    reverseDelay = 700;
    await mapClick(170, 140);
    await page.waitForTimeout(80);
    reverseDelay = 20;
    await mapClick(220, 170);
    await waitAddress('88 Đường mới 4');
    await page.waitForTimeout(850);
    assert.equal(await address.inputValue(), '88 Đường mới 4');
    checks.push({ width, check: 'Latest map selection wins over stale requests', passed: true });

    reverseDelay = 500;
    await mapClick(180, 130);
    await page.waitForTimeout(80);
    await address.fill('Địa chỉ nhập thủ công');
    await page.waitForTimeout(650);
    assert.equal(await address.inputValue(), 'Địa chỉ nhập thủ công');
    assert.equal(await marker.count(), 0, 'Editing address invalidates stale coordinates');
    checks.push({ width, check: 'Manual edits are not overwritten by pending reverse requests', passed: true });

    searchDelay = 600;
    await address.fill('Địa chỉ A');
    await blurAddress();
    await page.waitForTimeout(100);
    searchDelay = 10;
    await address.fill('Địa chỉ B');
    await blurAddress();
    await page.waitForFunction(() => document.querySelector('.selected-location')?.textContent.includes('21.100000'));
    await page.waitForTimeout(750);
    assert.match(await coordinateText(), /21\.100000/);
    checks.push({ width, check: 'Latest address wins over stale forward lookup', passed: true });

    await address.fill('Không có địa chỉ này');
    await blurAddress();
    await page.getByRole('alert').filter({ hasText: 'Không tìm thấy vị trí' }).waitFor();
    assert.equal(await address.inputValue(), 'Không có địa chỉ này');
    failSearch = true;
    await page.getByRole('button', { name: 'Tìm địa chỉ trên bản đồ' }).click();
    await page.getByRole('alert').filter({ hasText: 'Dịch vụ địa chỉ đang gián đoạn' }).waitFor();
    failSearch = false;
    failReverse = true;
    reverseDelay = 10;
    await mapClick(170, 130);
    await page.getByRole('alert').filter({ hasText: 'chưa cập nhật được địa chỉ' }).waitFor();
    assert.equal(await address.inputValue(), 'Không có địa chỉ này');
    checks.push({ width, check: 'No results and provider errors keep entered address', passed: true });

    failReverse = false;
    await page.getByRole('button', { name: 'Vị trí hiện tại', exact: true }).click();
    await waitAddress('88 Đường mới 7');
    assert.match(await coordinateText(), /20\.940000, 106\.100000/);
    checks.push({ width, check: 'Geolocation reverse lookup', passed: true });
    await page.locator('#post-title').fill('Phòng trọ kiểm thử bản đồ');
    await page.locator('#post-description').fill('Phòng thoáng, cửa sổ và bếp riêng.');
    await page.locator('#post-price').fill('3000000');
    await page.locator('#post-area').fill('25');
    await page.getByRole('button', { name: 'Đăng tin ngay' }).click();
    await page.waitForURL('**/landlord/posts');
    assert.equal(savedPayload.latitude, 20.94);
    assert.equal(savedPayload.longitude, 106.1);
    assert.equal(savedPayload.address, '88 Đường mới 7');
    assert.equal(savedPayload.province, 'Hưng Yên');
    checks.push({ width, check: 'Submit preserves selected address and coordinates in existing DTO', passed: true });

    const searchCount = searchCalls.length;
    await page.goto(baseURL + '/landlord/posts/1/edit');
    await waitAddress(saved.address);
    await address.focus();
    await blurAddress();
    await page.waitForTimeout(500);
    assert.equal(searchCalls.length, searchCount, 'Edit mode must preserve saved pin without automatic search');
    assert.match(await coordinateText(), /21\.030000, 105\.840000/);
    const pageWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    assert.ok(pageWidth <= width + 1, 'Page must not overflow');
    await page.locator('.leaflet-map-picker').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(output, 'map-' + width + '.png') });
    assert.equal(await marker.evaluate(image => image.complete && image.naturalWidth > 0), true, 'Bundled marker icon renders');
    assert.deepEqual(pageErrors, []);
    checks.push({ width, check: 'Edit-mode pin, responsive layout and no React crash', passed: true });

    await page.goto(baseURL + '/landlord/rooms');
    await page.getByRole('textbox', { name: /^Tên phòng/ }).fill('Phòng có bản đồ');
    await page.getByRole('spinbutton', { name: /^Giá thuê/ }).fill('3000000');
    await page.getByRole('spinbutton', { name: /^Diện tích/ }).fill('25');
    const roomAddress = page.locator('#room-address');
    await roomAddress.fill('42 Nguyễn Lân');
    await page.getByRole('textbox', { name: /^Tên phòng/ }).focus();
    await page.locator('.leaflet-marker-icon').waitFor();
    const reverseCount = reverseCalls.length;
    await mapClick(180, 130);
    await page.waitForFunction(value => document.querySelector('#room-address')?.value === value, '88 Đường mới ' + (reverseCount + 1));
    assert.equal(await page.getByLabel('Tỉnh / thành phố', { exact: true }).inputValue(), 'Hưng Yên');
    assert.equal(await page.getByLabel('Quận / huyện', { exact: true }).inputValue(), 'Mỹ Hào');
    assert.equal(await page.getByLabel('Phường / xã', { exact: true }).inputValue(), 'Bần Yên Nhân');
    await page.waitForFunction(() => Array.from(document.querySelectorAll('.leaflet-tile')).filter(image => image.complete && image.naturalWidth > 0).length >= 2, undefined, { timeout: 15000 });
    await page.waitForTimeout(350);
    await page.screenshot({ path: path.join(output, 'room-form-' + width + '.png') });
    await page.locator('#room-form').getByRole('button', { name: 'Thêm phòng', exact: true }).click();
    await page.waitForFunction(() => document.querySelector('#room-address')?.value === '');
    assert.equal(roomPayload.diaChi, '88 Đường mới ' + (reverseCount + 1));
    assert.equal(roomPayload.latitude, reverseCalls.at(-1).lat);
    assert.equal(roomPayload.longitude, reverseCalls.at(-1).lng);
    assert.equal(await page.locator('.leaflet-marker-icon').count(), 0, 'Saved room form resets map');
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth) <= width + 1);
    checks.push({ width, check: 'Room-management address, reverse lookup and saved coordinates', passed: true });

    reverseDelay = 500;
    await mapClick(180, 130);
    await page.waitForTimeout(80);
    await page.getByRole('button', { name: 'Xóa nội dung biểu mẫu' }).click();
    await page.waitForTimeout(650);
    assert.equal(await roomAddress.inputValue(), '');
    assert.equal(await page.locator('.leaflet-marker-icon').count(), 0);
    assert.deepEqual(pageErrors, []);
    checks.push({ width, check: 'Reset cancels pending lookup in room form', passed: true });
    await context.close();
    console.log('Passed location picker tests at ' + width + 'px');
  }
} finally {
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({ fixtureMode: true, checks }, null, 2));
}
console.log(JSON.stringify({ passed: checks.length, checks }, null, 2));
