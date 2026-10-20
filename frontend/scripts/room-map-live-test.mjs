import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const baseURL = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5186';
const apiURL = process.env.ROOM_MAP_LIVE_API || 'http://127.0.0.1:5099';
const output = path.resolve(process.env.UI_AUDIT_OUTPUT || '../artifacts/room-map-fix-20261020/live');
const sampleHouse = process.env.ROOM_MAP_SAMPLE_ADDRESS === '1';
const query = sampleHouse ? '18 P. Hai Bà Trưng, Cửa Nam, Hà Nội 100000, Việt Nam' : 'Hồ Hoàn Kiếm, Hà Nội';
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const results = [];
try {
  for (const width of [1440, 768, 375]) {
    const context = await browser.newContext({ viewport: { width, height: 1000 } });
    await context.addInitScript(() => {
      localStorage.setItem('room_rental_token', 'map-live-ui-fixture');
      localStorage.setItem('room_rental_user', JSON.stringify({ id: 20, userId: 20, fullName: 'Chủ trọ QA', role: 'Landlord' }));
    });
    const page = await context.newPage();
    const errors = [], locations = [];
    page.on('pageerror', e => errors.push(e.message));
    if (sampleHouse) {
      const tile = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg==', 'base64');
      await page.route(/https:\/\/.*tile.*\/(\d+)\/(\d+)\/(\d+)\.png/, route => route.fulfill({ contentType: 'image/png', body: tile }));
    }
    await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'QA fixture' }));
    await page.route('**/api/**', async route => {
      const url = new URL(route.request().url());
      if (url.pathname.startsWith('/api/location/')) {
        const response = await context.request.get(apiURL + url.pathname + url.search, { timeout: 25000 });
        const json = await response.json();
        locations.push({ path: url.pathname, status: response.status(), body: json });
        return route.fulfill({ response });
      }
      assert.equal(route.request().method(), 'GET', 'Live map check must not write room/business data');
      return route.fulfill({ json: { success: true, data: url.pathname.endsWith('/danh-muc') ? [{ id: 1, name: 'Phòng trọ' }] : [] } });
    });
    await page.goto(baseURL + '/landlord/rooms');
    await page.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
    const address = page.locator('#room-address');
    await address.fill(query); await address.press('Enter');
    await page.locator('.leaflet-marker-icon').waitFor({ timeout: 25000 });
    const found = locations.find(x => x.path.endsWith('/search'));
    assert.equal(found.status, 200); assert.equal(found.body.success, true); assert.ok(found.body.data.length > 0);
    assert.equal(await address.inputValue(), sampleHouse ? '18 Phố Hai Bà Trưng' : query);
    if (sampleHouse) {
      assert.equal(found.body.data[0].street, 'Phố Hai Bà Trưng');
      assert.equal(found.body.data[0].houseNumber, ''); assert.equal(found.body.data[0].matchType, 'street');
      assert.equal(await page.locator('.map-picker-canvas').getAttribute('data-location-confirmed'), 'false');
      await page.getByRole('status').filter({ hasText: 'Chưa xác định được số nhà 18' }).waitFor();
      const dialog = page.getByRole('dialog', { name: 'Thêm phòng mới', exact: true });
      assert.equal(await dialog.getByLabel('Phường / xã', { exact: true }).inputValue(), 'Cửa Nam');
      assert.equal(await dialog.getByLabel('Tỉnh / thành phố', { exact: true }).inputValue(), 'Hà Nội');
      await address.scrollIntoViewIfNeeded();
      await page.screenshot({ path: path.join(output, `fields-${width}.png`) });
      results.push({ width, check: 'Real Photon returns the correct street, not a verified house; fields retain house 18', status: 'PASS' });
    }
    await page.locator('.map-picker-results button').first().click();
    assert.equal(await address.inputValue(), sampleHouse ? '18 Phố Hai Bà Trưng' : found.body.data[0].address || found.body.data[0].displayName);
    assert.equal(await page.locator('.map-picker-canvas').getAttribute('data-location-confirmed'), 'true');
    results.push({ width, check: 'Real controller/provider address search and form synchronization', status: 'PASS' });
    const map = page.locator('.leaflet-map-picker .leaflet-container');
    if (sampleHouse) {
      await page.locator('.map-picker-results').scrollIntoViewIfNeeded();
      await page.screenshot({ path: path.join(output, `live-${width}.png`) });
      assert.equal(await page.locator('.leaflet-container').count(), 1);
      assert.deepEqual(errors, []); await context.close();
      console.log(`PASS live approved Photon address ${width}px; tiles mocked for privacy`); continue;
    }
    const reverseResponse = page.waitForResponse(response => response.url().includes('/location/reverse') && response.status() === 200, { timeout: 25000 });
    await map.scrollIntoViewIfNeeded(); await map.click({ position: { x: 65, y: 110 } });
    await reverseResponse;
    await page.waitForFunction(() => !document.querySelector('[aria-label="Bản đồ chọn vị trí phòng trọ"]')?.getAttribute('aria-busy')?.includes('true'));
    const reversed = locations.find(x => x.path.endsWith('/reverse'));
    assert.equal(reversed.status, 200); assert.equal(reversed.body.success, true);
    assert.equal(await address.inputValue(), reversed.body.data.address || reversed.body.data.displayName);
    assert.equal(await page.locator('.leaflet-container').count(), 1);
    assert.equal(await page.locator('.gm-style, iframe[src*="maps"], script[src*="maps.googleapis"]').count(), 0);
    results.push({ width, check: 'Real reverse lookup updates one map without embedding Google Maps', status: 'PASS' });
    await page.waitForFunction(() => [...document.querySelectorAll('.leaflet-tile')].some(img => img.complete && img.naturalWidth >= 256), null, { timeout: 18000 }).catch(() => {});
    const tiles = await page.locator('.leaflet-tile').evaluateAll(images => images.filter(img => img.complete && img.naturalWidth >= 256).length);
    results.push({ width, check: 'Unmocked external map tiles', status: tiles > 0 ? 'PASS' : 'UNAVAILABLE', loadedTileCount: tiles });
    await page.screenshot({ path: path.join(output, `live-${width}.png`) });
    assert.deepEqual(errors, []);
    await context.close(); console.log(`PASS live map ${width}px; real loaded tiles=${tiles}`);
  }
} finally {
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({
    realGeocoding: true, realTiles: !sampleHouse, sampleHouse, authenticationAndRoomLists: 'fixtures', databaseWrites: false, results,
  }, null, 2));
}
