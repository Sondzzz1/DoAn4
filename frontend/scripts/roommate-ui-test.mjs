import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const base = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5175';
const output = path.resolve(process.env.UI_AUDIT_OUTPUT || '../artifacts/roommate-ui-20261020/fixtures');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const checks = [];
let currentPage;
const post = { id: 101, contractId: 7, title: 'QA tìm người ở cùng tại Quận 7', description: 'Phòng QA có ban công và cửa sổ.',
  requirements: 'Giữ gìn vệ sinh chung.', estimatedShare: 1500000, requestedSlots: 1, remainingRequestedSlots: 1, status: 1,
  creatorName: 'Người thuê chính QA', roomName: 'Phòng QA', address: 'Địa chỉ kiểm thử', district: 'Quận 7', province: 'Hồ Chí Minh',
  area: 30, currentOccupants: 2, maxOccupants: 3, availableSlots: 1, imageUrls: ['/room-placeholder.svg'], amenities: ['WiFi', 'Điều hòa', 'Giường'], createdAt: new Date().toISOString() };

async function setup(width, role, identity = 20) {
  const context = await browser.newContext({ viewport: { width, height: 960 } });
  if (role) await context.addInitScript(({ role, identity }) => {
    localStorage.setItem('room_rental_token', 'roommate-ui-fixture-only');
    localStorage.setItem('room_rental_user', JSON.stringify({ id: identity, userId: identity, fullName: 'Tài khoản QA', role }));
  }, { role, identity });
  const state = { posts: [{ ...post }, { ...post, id: 102, contractId: 8, title: 'QA tin chờ duyệt', status: 0 }],
    contracts: [{ contractId: 7, roomName: 'Phòng QA', contractStatus: 1, maxOccupants: 3, currentOccupants: 2, availableSlots: 1, canCreatePost: false,
      occupants: [{ id: 601, tenantAccountId: 4, tenantName: 'Người ở cùng QA', status: 0, joinedAt: post.createdAt }] },
    { contractId: 9, roomName: 'Phòng QA mới', contractStatus: 1, maxOccupants: 3, currentOccupants: 1, availableSlots: 2, canCreatePost: true, occupants: [] }],
    application: { id: 501, roommatePostId: 101, applicantTenantAccountId: 20, applicantName: 'Người ứng tuyển QA', creatorTenantAccountId: 2,
      creatorName: post.creatorName, postTitle: post.title, message: 'Lời nhắn QA', status: 0 },
    request: { id: 401, roommatePostId: 101, applicantTenantAccountId: 20, applicantName: 'Người ứng tuyển QA', message: 'Xin ở cùng.', status: role === 'Landlord' ? 1 : 0 },
    failSave: false, failApply: false, failDecision: false, calls: [], searches: [] };
  const page = await context.newPage(); currentPage = page;
  const errors = []; page.on('pageerror', e => errors.push(e.message));
  await page.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'UI fixture only' }));
  await page.route('**/api/**', async route => {
    const req = route.request(), url = new URL(req.url()), p = url.pathname.replace('/api/o-ghep', '');
    const ok = data => route.fulfill({ json: { success: true, data } });
    const fail = message => route.fulfill({ status: 409, json: { success: false, message } });
    if (!url.pathname.startsWith('/api/o-ghep')) return ok([]);
    if (req.method() === 'GET') {
      if (p === '') {
        const keyword = url.searchParams.get('keyword') || ''; state.searches.push(Object.fromEntries(url.searchParams));
        if (keyword === 'loi') return fail('QA không tải được danh sách.');
        return ok({ items: keyword === 'khong' ? [] : [post], totalCount: keyword === 'khong' ? 0 : 13,
          pageNumber: Number(url.searchParams.get('pageNumber')) || 1, pageSize: 12, totalPages: keyword === 'khong' ? 0 : 2 });
      }
      if (/^\/\d+$/.test(p)) return ok(post);
      if (p === '/cua-toi' || p === '/chu-tro') return ok(state.posts);
      if (p === '/hop-dong') return ok(state.contracts);
      if (p.startsWith('/hop-dong/')) return ok(state.contracts.find(c => c.contractId === Number(p.split('/').at(-1))) || state.contracts[1]);
      if (p === '/ung-tuyen') return ok([state.application]);
      if (p.endsWith('/yeu-cau')) return ok(Number(p.split('/')[1]) === 101 ? [state.request] : []);
      return ok([]);
    }
    const data = req.postDataJSON(); state.calls.push({ method: req.method(), path: p, data });
    if (p === '/101/yeu-cau') return state.failApply ? fail('QA gửi yêu cầu thất bại.') : ok({ id: 900 });
    if (p === '' || /^\/\d+$/.test(p)) {
      if (state.failSave) return fail('QA lưu tin thất bại.');
      const id = p ? Number(p.slice(1)) : 103;
      const updated = { ...post, ...data, id, status: 0 };
      state.posts = [...state.posts.filter(x => x.id !== id), updated]; return ok(updated);
    }
    if (p.endsWith('/duyet')) {
      if (state.failDecision || (!data.approve && !data.reason?.trim())) return fail('QA cần kiểm tra lý do duyệt tin.');
      const item = state.posts.find(x => x.id === Number(p.split('/')[1])); item.status = data.approve ? 1 : 2; return ok(item);
    }
    if (p.endsWith('/dong')) { state.posts.find(x => x.id === Number(p.split('/')[1])).status = 4; return ok({}); }
    if (p.endsWith('/huy')) { state.application.status = 4; return ok(state.application); }
    if (p.endsWith('/roi-phong')) { state.contracts[0].occupants[0].status = 1; return ok(state.contracts[0]); }
    if (p.includes('/yeu-cau/')) { state.request.status = data.approve ? role === 'Landlord' ? 2 : 1 : 3; return ok(state.request); }
    throw new Error('Unhandled fixture request: ' + req.method() + ' ' + p);
  });
  const inspect = async (name, screenshot = false) => {
    assert.deepEqual(errors, []);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, name + ': document overflow');
    const dialog = page.getByRole('dialog');
    if (await dialog.count()) {
      const bounds = await dialog.boundingBox();
      assert.ok(bounds.x >= 0 && bounds.y >= 0 && bounds.x + bounds.width <= width + 1 && bounds.y + bounds.height <= 961);
      assert.equal(await dialog.evaluate(el => el.scrollWidth > el.clientWidth + 1), false, name + ': modal overflow');
    }
    checks.push({ width, role: role || 'Anonymous', name, status: 'PASS' }); console.log(`PASS ${width} ${role || 'Anonymous'}: ${name}`);
    if (screenshot) await page.screenshot({ path: path.join(output, `${role || 'public'}-${name}-${width}.png`), fullPage: true });
  };
  const close = async () => { await page.getByRole('dialog').getByRole('button', { name: /^(Đóng|Hủy)$/ }).last().click(); await page.getByRole('dialog').waitFor({ state: 'hidden' }); };
  const confirm = async title => { const modal = page.getByRole('dialog', { name: title, exact: true }); await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await modal.waitFor({ state: 'hidden' }); };
  const openPost = async id => { const title = state.posts.find(x => x.id === id).title; await page.locator('tbody tr').filter({ hasText: title }).getByRole('button', { name: 'Chi tiết / Yêu cầu', exact: true }).click(); await page.getByRole('dialog').getByText('Yêu cầu ứng tuyển', { exact: true }).waitFor(); };
  return { context, page, state, inspect, close, confirm, openPost };
}

try {
  for (const width of [1440, 768, 375]) {
    const applicant = await setup(width, 'Tenant');
    let { page, state, inspect } = applicant;
    await page.goto(base + '/roommates'); await page.locator('.rm-card img').waitFor();
    await page.waitForFunction(() => document.querySelector('.rm-card img')?.complete);
    await inspect('list', true);
    await page.getByLabel('Tìm tin ở ghép').fill('Quận 7'); await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
    await page.waitForURL(url => url.searchParams.get('keyword') === 'Quận 7');
    const nextPage = page.waitForResponse(response => new URL(response.url()).searchParams.get('pageNumber') === '2');
    await page.getByRole('button', { name: 'Trang sau', exact: true }).click(); await page.waitForURL(url => url.searchParams.get('page') === '2'); await nextPage;
    assert.equal(state.searches.at(-1).pageNumber, '2'); await inspect('search-pagination');
    await page.getByLabel('Tìm tin ở ghép').fill('khong'); await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
    await page.getByText('Chưa có tin tìm người ở ghép phù hợp.', { exact: true }).waitFor(); await inspect('empty');
    await page.getByLabel('Tìm tin ở ghép').fill('loi'); await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
    await page.getByText('QA không tải được danh sách.', { exact: true }).waitFor(); await inspect('load-error');
    await page.goto(base + '/roommates/101'); await page.getByRole('heading', { level: 1, name: post.title }).waitFor();
    await inspect('detail', true);
    await page.getByRole('button', { name: 'Gửi yêu cầu ở ghép', exact: true }).click();
    let modal = page.getByRole('dialog', { name: 'Gửi yêu cầu ở ghép', exact: true });
    const message = '  Tôi muốn trao đổi thêm.  '; await modal.getByLabel('Lời nhắn').fill(message);
    state.failApply = true; await modal.getByRole('button', { name: 'Gửi yêu cầu', exact: true }).click();
    await modal.getByText('QA gửi yêu cầu thất bại.', { exact: true }).waitFor();
    assert.equal(await modal.getByLabel('Lời nhắn').inputValue(), message); await inspect('apply-error-retains-message', true);
    state.failApply = false; await modal.getByRole('button', { name: 'Gửi yêu cầu', exact: true }).click(); await modal.waitFor({ state: 'hidden' });
    assert.equal(state.calls.at(-1).data.message, message.trim()); assert.equal(state.calls.at(-1).path, '/101/yeu-cau');
    assert.equal(await page.getByRole('button', { name: 'Đã gửi yêu cầu', exact: true }).isDisabled(), true); await inspect('apply-success');
    await applicant.context.close();

    const tenant = await setup(width, 'Tenant', 2); ({ page, state, inspect } = tenant);
    await page.goto(base + '/tenant/roommates'); await page.getByRole('button', { name: 'Tạo tin ở ghép', exact: true }).click();
    modal = page.getByRole('dialog', { name: 'Tạo tin tìm người ở ghép', exact: true });
    await modal.getByLabel('Tiêu đề ở ghép').fill('QA tin mới'); await modal.getByLabel('Mô tả ở ghép').fill('Nội dung kiểm thử giao diện.');
    await modal.getByLabel('Chi phí dự kiến', { exact: true }).fill('1200000');
    state.failSave = true; await modal.getByRole('button', { name: 'Gửi chủ trọ duyệt', exact: true }).click();
    await modal.getByText('QA lưu tin thất bại.', { exact: true }).waitFor();
    assert.equal(await modal.getByLabel('Tiêu đề ở ghép').inputValue(), 'QA tin mới'); await inspect('create-validation', true);
    state.failSave = false; await modal.getByRole('button', { name: 'Gửi chủ trọ duyệt', exact: true }).click(); await modal.waitFor({ state: 'hidden' });
    assert.equal(state.calls.at(-1).data.contractId, 9); assert.equal(state.calls.at(-1).data.estimatedShare, 1200000); await inspect('create-payload');
    await page.getByRole('button', { name: 'Sửa tin 101', exact: true }).click();
    modal = page.getByRole('dialog', { name: 'Sửa tin tìm người ở ghép', exact: true });
    assert.equal(await modal.getByLabel('Tiêu đề ở ghép').inputValue(), post.title);
    assert.equal(await modal.getByLabel('Hợp đồng ở ghép').isDisabled(), true);
    await modal.getByLabel('Tiêu đề ở ghép').fill('QA tin đã sửa'); await inspect('edit-prefill', true);
    await modal.getByRole('button', { name: 'Gửi chủ trọ duyệt', exact: true }).click(); await modal.waitFor({ state: 'hidden' });
    assert.equal(state.calls.at(-1).method, 'PUT'); assert.equal(state.calls.at(-1).path, '/101'); await inspect('edit-payload');
    await page.getByRole('tab', { name: /^Yêu cầu đã gửi/ }).click();
    await page.locator('tbody').getByRole('button', { name: 'Hủy', exact: true }).click(); await tenant.confirm('Hủy yêu cầu ở ghép');
    assert.equal(state.calls.at(-1).path, '/yeu-cau/501/huy'); await inspect('cancel-application');
    await page.getByRole('tab', { name: /^Người ở cùng/ }).click(); await page.getByRole('button', { name: 'Rời phòng', exact: true }).click();
    await tenant.confirm('Xác nhận rời phòng'); assert.equal(state.calls.at(-1).path, '/nguoi-o/601/roi-phong'); await inspect('occupants');
    await page.getByRole('tab', { name: /^Tin của tôi/ }).click();
    await page.getByRole('button', { name: 'Đóng tin 103', exact: true }).click(); await tenant.confirm('Đóng tin ở ghép');
    assert.equal(state.calls.at(-1).path, '/103/dong'); await inspect('close-post', true);
    await tenant.context.close();

    const landlord = await setup(width, 'Landlord', 1); ({ page, state, inspect } = landlord);
    await page.goto(base + '/landlord/roommates'); await landlord.openPost(102);
    await page.getByRole('button', { name: 'Duyệt tin', exact: true }).click(); await landlord.confirm('Duyệt tin ở ghép');
    assert.deepEqual(state.calls.at(-1).data, { approve: true }); assert.equal(state.posts.find(x => x.id === 102).status, 1); await inspect('approve-post', true);
    state.posts.find(x => x.id === 102).status = 0; await page.reload(); await landlord.openPost(102);
    await page.getByRole('button', { name: 'Từ chối tin', exact: true }).click();
    modal = page.getByRole('dialog', { name: 'Từ chối tin ở ghép', exact: true });
    const reason = 'Cần bổ sung nội dung.'; await modal.getByLabel('Lý do từ chối').fill(reason); state.failDecision = true;
    await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await modal.getByText('QA cần kiểm tra lý do duyệt tin.', { exact: true }).waitFor();
    assert.equal(await modal.getByLabel('Lý do từ chối').inputValue(), reason); await inspect('moderation-error', true);
    state.failDecision = false; await landlord.confirm('Từ chối tin ở ghép');
    assert.deepEqual(state.calls.at(-1).data, { approve: false, reason }); await inspect('reject-post');
    await landlord.openPost(101); await page.getByRole('button', { name: 'Xác nhận vào ở', exact: true }).click();
    await landlord.confirm('Xác nhận người vào ở');
    assert.equal(state.calls.at(-1).path, '/yeu-cau/401/xac-nhan'); assert.equal(state.calls.at(-1).data.approve, true);
    await inspect('approve-occupant');
    await landlord.context.close();
  }
} catch (error) {
  if (currentPage && !currentPage.isClosed()) await currentPage.screenshot({ path: path.join(output, 'failure.png'), fullPage: true });
  throw error;
} finally {
  await browser.close(); await writeFile(path.join(output, 'results.json'), JSON.stringify({ fixtureMode: true, realDatabaseWrites: false, checks }, null, 2));
}
console.log(`Passed ${checks.length} roommate UI regression checks.`);
