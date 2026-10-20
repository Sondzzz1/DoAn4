import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.UI_AUDIT_NODE_MODULES
  ? path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright') : 'playwright');
const baseURL = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5186';
const output = path.resolve(process.env.UI_AUDIT_OUTPUT || '../artifacts/room-map-fix-20261020');
await mkdir(output, { recursive: true });
const results = [];
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const location = { latitude: 21.03, longitude: 105.84, address: '42 Nguyễn Lân', ward: 'Phương Liệt',
  district: 'Thanh Xuân', province: 'Hà Nội', displayName: '42 Nguyễn Lân, Phương Liệt, Thanh Xuân, Hà Nội' };
const alternative = { ...location, latitude: 21.05, address: '84 Nguyễn Lân', displayName: '84 Nguyễn Lân, Hà Nội' };
const reverse = { ...location, address: '88 Đường mới', ward: 'Bần Yên Nhân', district: 'Mỹ Hào', province: 'Hưng Yên', displayName: '88 Đường mới, Hưng Yên' };
const sampleAddress = '18 P. Hai Bà Trưng, Cửa Nam, Hà Nội 100000, Việt Nam';
const sampleStreet = { latitude: 21.024901, longitude: 105.8506901, address: 'Phố Hai Bà Trưng',
  street: 'Phố Hai Bà Trưng', houseNumber: '', ward: 'Cửa Nam', district: '', province: 'Hà Nội',
  displayName: 'Phố Hai Bà Trưng, Cửa Nam, Hà Nội, Việt Nam', matchType: 'street' };
const tile = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg==', 'base64');

try {
  for (const width of [1440, 768, 375]) {
    const context = await browser.newContext({ viewport: { width, height: 900 }, permissions: ['geolocation'],
      geolocation: { latitude: 20.94, longitude: 106.1 } });
    await context.addInitScript(() => {
      localStorage.setItem('room_rental_token', 'room-map-regression-fixture');
      localStorage.setItem('room_rental_user', JSON.stringify({ id: 20, userId: 20, fullName: 'Chủ trọ QA', role: 'Landlord' }));
    });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const calls = { search: [], reverse: [], saves: [] };
    let searchDelay = 0;
    let reverseDelay = 0;
    let searchFailure = false;
    let reverseFailure = false;
    let noResults = false;
    let singleResult = false;
    const room = { id: 1, roomName: 'Phòng QA đã lưu', categoryId: 1, address: location.address, ward: location.ward,
      district: location.district, province: location.province, latitude: location.latitude, longitude: location.longitude,
      price: 3000000, area: 25, maxOccupants: 3, status: 0, imageUrls: [], amenityIds: [] };
    await page.route(/https:\/\/.*tile.*\/(\d+)\/(\d+)\/(\d+)\.png/, route => route.fulfill({ contentType: 'image/png', body: tile }));
    await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'Fixture only' }));
    await page.route('**/api/**', async route => {
      const url = new URL(route.request().url());
      const p = url.pathname;
      const ok = data => route.fulfill({ json: { success: true, data, message: 'OK' } });
      try {
        if (p.endsWith('/location/search')) {
          const query = url.searchParams.get('q'); calls.search.push(query);
          const delay = searchDelay, failure = searchFailure, empty = noResults;
          const candidates = query.includes('Hai Bà Trưng') ? [sampleStreet]
            : [{ ...location, latitude: query.includes('Địa chỉ B') ? 21.1 : location.latitude }, ...singleResult ? [] : [alternative]];
          if (delay) await new Promise(resolve => setTimeout(resolve, delay));
          return failure ? route.fulfill({ status: 503, json: { success: false, message: 'Dịch vụ địa chỉ đang gián đoạn.' } })
            : ok(empty ? [] : candidates);
        }
        if (p.endsWith('/location/reverse')) {
          const lat = Number(url.searchParams.get('lat')), lng = Number(url.searchParams.get('lng'));
          calls.reverse.push({ lat, lng }); const delay = reverseDelay, failure = reverseFailure;
          if (delay) await new Promise(resolve => setTimeout(resolve, delay));
          return failure ? route.fulfill({ status: 503, json: { success: false, message: 'Dịch vụ địa chỉ đang gián đoạn.' } })
            : ok({ ...reverse, latitude: lat, longitude: lng });
        }
        if (/\/phong(?:\/\d+)?$/.test(p) && ['POST', 'PUT'].includes(route.request().method())) {
          calls.saves.push(route.request().postDataJSON()); return ok({ id: 2 });
        }
        if (p.endsWith('/phong') && route.request().method() === 'GET') return ok([room]);
        if (p.endsWith('/danh-muc') || p.endsWith('/danh-muc/active')) return ok([{ id: 1, name: 'Phòng trọ', isActive: true }]);
        return ok([]);
      } catch (error) {
        if (!/closed|cancel|abort|Target|Invalid/.test(String(error))) throw error;
      }
    });
    const passed = name => { results.push({ width, name, status: 'PASS' }); console.log(`PASS ${width}: ${name}`); };
    const dialog = page.getByRole('dialog', { name: 'Thêm phòng mới', exact: true });
    const address = page.locator('#room-address');
    const search = () => page.getByRole('button', { name: 'Tìm địa chỉ trên bản đồ', exact: true }).click();
    const coordinate = () => page.locator('.leaflet-map-picker .selected-location').innerText();
    const oneMap = async () => {
      assert.equal(await page.locator('.leaflet-container').count(), 1);
      assert.equal(await page.locator('iframe[src*="maps"], .gm-style, script[src*="maps.googleapis"]').count(), 0);
    };
    await page.goto(baseURL + '/landlord/rooms');
    await page.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
    await dialog.getByLabel('Tên phòng', { exact: false }).fill('TEST-MAP');
    await dialog.getByLabel('Giá thuê (VNĐ/tháng)', { exact: false }).fill('3000000');
    await dialog.getByLabel('Diện tích (m²)', { exact: false }).fill('25');
    searchDelay = 600;
    await address.fill(sampleAddress); await address.press('Enter');
    await dialog.getByLabel('Tên phòng', { exact: false }).fill('TEST-MAP đang nhập');
    await dialog.getByLabel('Giá thuê (VNĐ/tháng)', { exact: false }).fill('3500000');
    await dialog.getByLabel('Diện tích (m²)', { exact: false }).fill('27');
    await dialog.locator('textarea').fill('Thông tin vẫn được giữ khi tra địa chỉ.');
    await page.getByRole('status').filter({ hasText: 'Chưa xác định được số nhà 18' }).waitFor();
    searchDelay = 0;
    assert.equal(await address.inputValue(), '18 Phố Hai Bà Trưng');
    assert.equal(await dialog.getByLabel('Phường / xã', { exact: true }).inputValue(), 'Cửa Nam');
    assert.equal(await dialog.getByLabel('Tỉnh / thành phố', { exact: true }).inputValue(), 'Hà Nội');
    assert.equal(await dialog.getByLabel('Tên phòng', { exact: false }).inputValue(), 'TEST-MAP đang nhập');
    assert.equal(await dialog.getByLabel('Giá thuê (VNĐ/tháng)', { exact: false }).inputValue(), '3500000');
    assert.equal(await dialog.getByLabel('Diện tích (m²)', { exact: false }).inputValue(), '27');
    assert.equal(await dialog.locator('textarea').inputValue(), 'Thông tin vẫn được giữ khi tra địa chỉ.');
    assert.equal(await page.locator('.map-picker-canvas').getAttribute('data-location-confirmed'), 'false');
    assert.match(await coordinate(), /Vị trí gợi ý/); assert.equal(calls.saves.length, 0);
    await page.getByRole('button', { name: /Phố Hai Bà Trưng.*Tuyến đường/ }).click();
    assert.equal(await address.inputValue(), '18 Phố Hai Bà Trưng');
    await oneMap();
    passed('Full pasted address fills street/ward/province, preserves other inputs and warns about an unverified house number');
    await dialog.getByLabel('Quận / huyện', { exact: true }).fill('Hoàn Kiếm'); await search();
    await page.getByRole('status').filter({ hasText: 'Chưa xác định được số nhà 18' }).waitFor();
    await page.getByRole('button', { name: /Phố Hai Bà Trưng.*Tuyến đường/ }).click();
    assert.equal(await dialog.getByLabel('Quận / huyện', { exact: true }).inputValue(), 'Hoàn Kiếm');
    assert.equal(await address.inputValue(), '18 Phố Hai Bà Trưng');
    passed('Selecting a street without district data preserves the house number and manually entered district');
    await address.fill('Nguyễn Lân, Hà Nội');
    await address.press('Enter');
    await page.waitForTimeout(500);
    assert.equal(calls.saves.length, 0, 'Enter in address must search, never create a room');
    await dialog.waitFor(); await page.locator('.leaflet-marker-icon').waitFor(); await oneMap();
    assert.equal(await address.inputValue(), 'Nguyễn Lân, Hà Nội');
    passed('Enter searches without submitting the room form or overwriting the entered address');

    await page.getByRole('button', { name: alternative.displayName, exact: true }).click();
    assert.equal(await address.inputValue(), alternative.address);
    assert.equal(await dialog.getByLabel('Tỉnh / thành phố', { exact: true }).inputValue(), alternative.province);
    assert.match(await coordinate(), /21\.050000/); await oneMap();
    passed('Selecting a search result updates address fields and the existing pin');

    singleResult = true;
    await address.fill('42 Nguyễn Lân, căn 201'); await address.press('Enter');
    await page.getByRole('button', { name: location.displayName, exact: true }).waitFor();
    assert.equal(await address.inputValue(), '42 Nguyễn Lân, căn 201');
    await page.getByRole('button', { name: location.displayName, exact: true }).click();
    assert.equal(await address.inputValue(), '42 Nguyễn Lân, căn 201'); singleResult = false;
    passed('A single candidate remains selectable without silently discarding the house/room number');

    const before = calls.search.length;
    await address.fill('Địa chỉ nhập tay');
    await dialog.getByLabel('Phường / xã', { exact: true }).fill('Phường mới');
    await dialog.getByLabel('Quận / huyện', { exact: true }).fill('Quận mới');
    await dialog.getByLabel('Tỉnh / thành phố', { exact: true }).fill('Hà Nội');
    await page.waitForTimeout(150);
    assert.equal(calls.search.length, before, 'Moving between address fields must not start duplicate searches');
    await search(); await page.getByRole('button', { name: alternative.displayName, exact: true }).waitFor();
    assert.equal(calls.search.length, before + 1, JSON.stringify(calls.search)); await oneMap();
    passed('One explicit click issues one search after editing the address group');

    const map = page.locator('.leaflet-map-picker .leaflet-container');
    await map.scrollIntoViewIfNeeded();
    await map.click({ position: { x: 60, y: 110 } });
    await page.waitForFunction(value => document.querySelector('#room-address')?.value === value, reverse.address);
    assert.equal(await dialog.getByLabel('Tỉnh / thành phố', { exact: true }).inputValue(), reverse.province);
    assert.equal(await dialog.getByLabel('Phường / xã', { exact: true }).inputValue(), reverse.ward);
    await oneMap(); passed('Map click reverse-geocodes the chosen coordinates and address');

    reverseFailure = true;
    await map.click({ position: { x: 70, y: 120 } });
    await page.getByRole('alert').filter({ hasText: 'chưa cập nhật được địa chỉ' }).waitFor();
    assert.equal(await address.inputValue(), reverse.address); reverseFailure = false;
    passed('Reverse errors preserve the entered address and warn the user');

    noResults = true;
    await address.fill('Không tồn tại'); await search();
    await page.getByRole('alert').filter({ hasText: 'Không tìm thấy vị trí' }).waitFor();
    assert.equal(await address.inputValue(), 'Không tồn tại'); noResults = false;
    searchFailure = true; await search();
    await page.getByRole('alert').filter({ hasText: 'Dịch vụ địa chỉ đang gián đoạn' }).waitFor();
    assert.equal(await address.inputValue(), 'Không tồn tại'); searchFailure = false;
    passed('Empty results and service errors show a warning, not a fake selection');

    searchDelay = 600;
    await address.fill('Địa chỉ A'); await address.press('Enter'); await page.waitForTimeout(100);
    searchDelay = 10;
    await address.fill('Địa chỉ B'); await address.press('Enter');
    await page.waitForFunction(() => document.querySelector('.map-picker-canvas')?.dataset.previewLatitude === '21.1');
    await page.waitForTimeout(650);
    assert.equal(await page.locator('.map-picker-canvas').getAttribute('data-preview-latitude'), '21.1');
    assert.equal(await page.locator('.map-picker-canvas').getAttribute('data-location-confirmed'), 'false'); await oneMap();
    passed('New searches supersede stale requests without adding another map');

    reverseDelay = 600;
    await map.click({ position: { x: 60, y: 110 } }); await page.waitForTimeout(70);
    await address.fill('Địa chỉ giữ lại'); await address.press('a');
    await page.waitForTimeout(700);
    assert.equal(await address.inputValue(), 'Địa chỉ giữ lạia');
    assert.equal(await page.locator('.leaflet-marker-icon').count(), 0);
    reverseDelay = 0; passed('Typing a new address invalidates the old pin and stale reverse lookup');

    await page.getByRole('button', { name: 'Vị trí hiện tại', exact: true }).click();
    await page.waitForFunction(() => document.querySelector('.leaflet-map-picker .selected-location')?.textContent.includes('20.940000, 106.100000'));
    await page.waitForFunction(value => document.querySelector('#room-address')?.value === value, reverse.address);
    await oneMap(); passed('GPS updates the same map and fills the reverse-geocoded address');

    const marker = page.locator('.leaflet-marker-icon');
    await marker.scrollIntoViewIfNeeded(); const markerBox = await marker.boundingBox();
    const dragResponse = page.waitForResponse(response => response.url().includes('/location/reverse'));
    await page.mouse.move(markerBox.x + markerBox.width / 2, markerBox.y + markerBox.height / 2);
    await page.mouse.down();
    await page.mouse.move(markerBox.x + markerBox.width / 2 + 45, markerBox.y + markerBox.height / 2 + 25, { steps: 12 });
    await page.mouse.up(); await dragResponse;
    await page.waitForFunction(value => document.querySelector('.leaflet-map-picker .selected-location')?.textContent.includes(value), calls.reverse.at(-1).lat.toFixed(6));
    await oneMap(); passed('Dragging the marker updates coordinates without constructing another map');

    await search(); await page.locator('.leaflet-marker-icon').waitFor();
    await page.getByRole('button', { name: alternative.displayName, exact: true }).waitFor();
    await page.getByRole('button', { name: location.displayName, exact: true }).click();
    assert.equal(await page.locator('.map-picker-message.warning').count(), 0);
    await oneMap(); await map.scrollIntoViewIfNeeded();
    assert.ok(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth + 1), 'Modal content must not overflow');
    const viewport = await map.boundingBox(); assert.ok(viewport.width > 200 && viewport.height >= 250);
    await page.screenshot({ path: path.join(output, `picker-${width}.png`) });
    await dialog.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
    await dialog.waitFor({ state: 'hidden' });
    assert.equal(calls.saves.length, 1);
    assert.equal(calls.saves[0].diaChi, location.address); assert.equal(calls.saves[0].latitude, location.latitude);
    assert.equal(calls.saves[0].tenPhong, 'TEST-MAP đang nhập'); assert.equal(calls.saves[0].gia, 3500000);
    assert.equal(calls.saves[0].dienTich, 27); assert.equal(calls.saves[0].moTa, 'Thông tin vẫn được giữ khi tra địa chỉ.');
    passed('Submit retains selected address/coordinates in the unchanged CreateRoom payload');

    await page.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
    await dialog.waitFor(); await oneMap();
    assert.equal(await address.inputValue(), ''); assert.equal(await page.locator('.leaflet-marker-icon').count(), 0);
    reverseDelay = 600;
    await map.click({ position: { x: 60, y: 110 } }); await page.waitForTimeout(70);
    await dialog.getByRole('button', { name: 'Xóa nội dung biểu mẫu', exact: true }).click();
    await page.waitForTimeout(700);
    assert.equal(await address.inputValue(), ''); assert.equal(await page.locator('.leaflet-marker-icon').count(), 0);
    reverseDelay = 0; await oneMap(); passed('Reset cancels pending requests and keeps exactly one clean map');
    reverseDelay = 600;
    await map.click({ position: { x: 60, y: 110 } }); await page.waitForTimeout(70);
    await dialog.getByRole('button', { name: 'Hủy', exact: true }).click();
    await page.waitForTimeout(700); reverseDelay = 0;
    assert.equal(await page.locator('.leaflet-container').count(), 0);
    passed('Closing while lookup is pending cancels requests and unmounts old maps');

    const searchCount = calls.search.length;
    await page.getByRole('button', { name: 'Sửa', exact: true }).click();
    const edit = page.getByRole('dialog', { name: 'Cập nhật phòng', exact: true });
    await edit.waitFor(); await page.locator('.leaflet-marker-icon').waitFor(); await oneMap();
    assert.equal(await address.inputValue(), room.address); assert.match(await coordinate(), /21\.030000/);
    assert.equal(calls.search.length, searchCount, 'Editing must not re-geocode saved coordinates automatically');
    await search(); await page.getByRole('button', { name: alternative.displayName, exact: true }).click();
    await edit.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click(); await edit.waitFor({ state: 'hidden' });
    assert.equal(calls.saves.length, 2); assert.equal(calls.saves[1].diaChi, alternative.address);
    assert.equal(calls.saves[1].latitude, alternative.latitude);
    passed('Edit preserves saved coordinates, then saves a newly selected address through the existing API');
    await page.getByRole('button', { name: 'Sửa', exact: true }).click(); await edit.waitFor();
    await address.fill(sampleAddress); await address.press('Enter');
    await page.getByRole('status').filter({ hasText: 'Chưa xác định được số nhà 18' }).waitFor();
    assert.equal(await page.locator('.map-picker-canvas').getAttribute('data-location-confirmed'), 'false');
    await page.getByRole('button', { name: /Phố Hai Bà Trưng.*Tuyến đường/ }).click();
    await edit.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click(); await edit.waitFor({ state: 'hidden' });
    assert.equal(calls.saves.length, 3); assert.equal(calls.saves[2].diaChi, '18 Phố Hai Bà Trưng');
    assert.equal(calls.saves[2].phuong, 'Cửa Nam'); assert.equal(calls.saves[2].thanhPho, 'Hà Nội');
    assert.equal(calls.saves[2].latitude, sampleStreet.latitude);
    passed('Edit payload retains the entered house number and explicitly confirmed street coordinates');
    assert.deepEqual(errors, []); passed('No browser runtime errors');
    await context.close();
  }
} finally {
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({ fixtureMode: true, realGeocodingVerified: false, results }, null, 2));
}
console.log(`Passed ${results.length} room map UI checks.`);
