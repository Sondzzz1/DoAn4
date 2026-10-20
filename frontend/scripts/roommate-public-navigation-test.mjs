import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const base = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5175';
const api = process.env.ROOMMATE_LIVE_API || 'http://localhost:5000';
const output = path.resolve(process.env.UI_AUDIT_OUTPUT || '../artifacts/roommate-public-navigation-20261020');
const widths = (process.env.ROOMMATE_UI_WIDTHS || '1440,768,375').split(',').map(Number);
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const checks = [];
let currentPage;
const reader = await browser.newContext();

try {
  const response = await reader.request.get(api + '/api/o-ghep?pageSize=50');
  assert.equal(response.status(), 200);
  const body = await response.json();
  assert.equal(body.success, true);
  const sample = body.data.items[0];
  assert.ok(sample, 'Requires an existing open roommate post; this script never creates production data.');
  const ownerPost = { ...sample, contractId: 4242 };
  const pending = { ...ownerPost, id: sample.id + 100000, title: 'QA tin chờ duyệt', status: 0 };

  const setup = async (width, role) => {
    const context = await browser.newContext({ viewport: { width, height: 960 } });
    if (role) await context.addInitScript(value => {
      localStorage.setItem('room_rental_token', 'roommate-navigation-fixture');
      localStorage.setItem('room_rental_user', JSON.stringify({ id: 20, userId: 20, fullName: 'Tài khoản QA', role: value }));
    }, role);
    const page = await context.newPage(); currentPage = page;
    const errors = [], writes = [], liveReads = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'Navigation fixture' }));
    await page.route('**/api/**', async route => {
      const request = route.request(), url = new URL(request.url());
      const p = url.pathname;
      if (request.method() !== 'GET') {
        writes.push(request.method() + ' ' + p);
        return route.fulfill({ status: 405, json: { success: false, message: 'Read-only regression test' } });
      }
      if (p === '/api/o-ghep' || /^\/api\/o-ghep\/\d+$/.test(p)) {
        const real = await reader.request.get(api + p + url.search);
        assert.equal(real.status(), 200); liveReads.push(p);
        return route.fulfill({ json: await real.json() });
      }
      let data = [];
      if (p.endsWith('/o-ghep/cua-toi') || p.endsWith('/o-ghep/chu-tro')) data = [ownerPost, pending];
      if (p.endsWith('/o-ghep/hop-dong/4242')) data = { contractId: 4242, contractStatus: 1, currentOccupants: sample.currentOccupants,
        maxOccupants: sample.maxOccupants, availableSlots: sample.availableSlots, occupants: [] };
      if (p.endsWith('/danh-muc')) data = [{ id: 901, name: 'Ở ghép', isActive: true }];
      if (p.endsWith('/bai-dang/search')) data = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0 };
      return route.fulfill({ json: { success: true, data } });
    });
    const inspect = async name => {
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, name + ': horizontal overflow');
      assert.deepEqual(errors, []); assert.deepEqual(writes, []);
      checks.push({ width, role: role || 'Anonymous', name, status: 'PASS' });
      console.log(`PASS ${width} ${role || 'Anonymous'}: ${name}`);
    };
    return { context, page, inspect, liveReads };
  };

  for (const width of widths) {
    const { context, page, inspect, liveReads } = await setup(width);
    await page.goto(base + '/');
    const category = page.getByRole('navigation', { name: 'Loại phòng', exact: true }).getByRole('link', { name: 'Phòng cho nhóm', exact: true });
    await category.waitFor();
    assert.equal(await category.getAttribute('href'), '/rooms?categoryId=901');
    assert.equal(await page.getByLabel('Loại phòng', { exact: true }).locator('option[value="901"]').innerText(), 'Phòng cho nhóm');
    await inspect('Home category and filter distinguish whole-room group rentals from roommate recruitment');
    const navigation = async () => {
      if (width > 1024) return page.locator('.site-nav');
      await page.getByRole('button', { name: 'Mở menu', exact: true }).click();
      return page.locator('.site-mobile-menu');
    };
    const nav = await navigation();
    const roommates = nav.getByRole('link', { name: 'Ở ghép', exact: true });
    assert.equal(await roommates.getAttribute('href'), '/roommates');
    await roommates.click(); await page.waitForURL('**/roommates');
    if (width <= 1024) assert.equal(await page.locator('.site-mobile-menu').count(), 0);
    await page.getByRole('heading', { name: 'Tìm bạn ở ghép', exact: true }).waitFor();
    await page.locator('.rm-card h2 a').filter({ hasText: sample.title }).waitFor();
    await page.locator('.rm-card img').first().scrollIntoViewIfNeeded();
    await page.waitForFunction(() => [...document.querySelectorAll('.rm-card img')].every(img => img.complete && img.naturalWidth > 0), null, { timeout: 20000 });
    await page.evaluate(() => window.scrollTo(0, 0));
    assert.ok(liveReads.includes('/api/o-ghep'));
    await inspect('Roommate navigation shows the approved post from the real public API');
    await page.screenshot({ path: path.join(output, `public-${width}.png`), fullPage: true });
    await page.locator(`.rm-card h2 a[href="/roommates/${sample.id}"]`).click();
    await page.getByRole('heading', { level: 1, name: sample.title, exact: true }).waitFor();
    assert.ok(liveReads.includes('/api/o-ghep/' + sample.id));
    await page.waitForFunction(() => [...document.querySelectorAll('.rm-gallery img')].every(img => img.complete && img.naturalWidth > 0), null, { timeout: 20000 });
    await page.screenshot({ path: path.join(output, `detail-${width}.png`), fullPage: true });
    await inspect('Approved public detail remains readable without authentication');
    const groupNav = await navigation();
    const groups = groupNav.getByRole('link', { name: 'Phòng cho nhóm', exact: true });
    assert.equal(await groups.getAttribute('href'), '/rooms?type=share');
    await groups.click();
    await page.waitForURL(url => url.pathname === '/rooms' && url.searchParams.get('categoryId') === '901');
    await page.getByRole('heading', { name: 'Phòng trọ cho thuê', exact: true }).waitFor();
    await page.locator('#desktop-categoryId option[value="901"]').waitFor({ state: 'attached' });
    assert.equal(await page.locator('#desktop-categoryId option[value="901"]').innerText(), 'Phòng cho nhóm');
    await inspect('Whole-room group rental category keeps its existing route and filter');
    await context.close();

    for (const role of ['Tenant', 'Landlord']) {
      const owner = await setup(width, role);
      await owner.page.goto(base + (role === 'Tenant' ? '/tenant/roommates' : '/landlord/roommates'));
      await owner.page.getByRole('button', { name: 'Chi tiết / Yêu cầu', exact: true }).first().waitFor();
      await owner.page.screenshot({ path: path.join(output, `${role.toLowerCase()}-overview-${width}.png`), fullPage: true });
      await owner.page.getByRole('button', { name: 'Chi tiết / Yêu cầu', exact: true }).first().click();
      const publicLink = owner.page.getByRole('link', { name: 'Xem tin công khai', exact: true });
      await publicLink.waitFor();
      assert.equal(await publicLink.count(), 1, 'Only the open post exposes a public-detail link');
      assert.equal(await publicLink.getAttribute('href'), '/roommates/' + sample.id);
      await owner.inspect('Workspace exposes the approved public post, not the pending fixture');
      await owner.page.screenshot({ path: path.join(output, `${role.toLowerCase()}-${width}.png`), fullPage: true });
      await owner.page.getByRole('dialog').getByRole('button', { name: 'Đóng', exact: true }).last().click();
      await owner.page.getByRole('button', { name: 'Chi tiết / Yêu cầu', exact: true }).nth(1).click();
      await owner.page.getByRole('dialog').getByText('Chưa có yêu cầu ứng tuyển.', { exact: true }).waitFor();
      assert.equal(await publicLink.count(), 0);
      await owner.inspect('Pending post does not expose a public-detail link');
      await owner.page.getByRole('dialog').getByRole('button', { name: 'Đóng', exact: true }).last().click();
      await owner.page.getByRole('button', { name: 'Chi tiết / Yêu cầu', exact: true }).first().click();
      await publicLink.click();
      await owner.page.getByRole('heading', { level: 1, name: sample.title, exact: true }).waitFor();
      await owner.inspect('Workspace public link opens the real approved detail');
      await owner.context.close();
    }
  }
} catch (error) {
  if (currentPage && !currentPage.isClosed()) await currentPage.screenshot({ path: path.join(output, 'failure.png'), fullPage: true });
  throw error;
} finally {
  await reader.close(); await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({
    realPublicApi: true, privateWorkspaceData: 'fixtures', databaseWrites: false, checks,
  }, null, 2));
}
console.log(`Passed ${checks.length} roommate navigation checks.`);
