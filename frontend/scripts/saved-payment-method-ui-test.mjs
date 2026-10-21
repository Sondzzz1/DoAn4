import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

// All identities, saved cards, callbacks, API writes and VNPay navigation are synthetic.
// No database or real payment provider is contacted.
const require = createRequire(import.meta.url);
const moduleRoot = process.env.UI_AUDIT_NODE_MODULES || 'C:/Users/huanp/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const { chromium } = require(path.join(moduleRoot, 'playwright'));
const frontend = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = process.env.SAVED_CARD_UI_OUTPUT || path.resolve(frontend, '../artifacts/saved-payment-ui');
const base = process.env.SAVED_CARD_UI_URL || 'http://127.0.0.1:5179';
const checks = [], writes = [], runtimeErrors = [], unexpectedRequests = [], consoleErrors = [], expectedNetworkErrors = [];
let browser, server, serverOutput = '';
const now = new Date().toISOString();
const future = new Date(Date.now() + 86400000).toISOString();

function fixture() {
  return { enabled: true, failList: false, methods: [{ id: 7, bankCode: 'NCB', maskedCardNumber: '•••• 2198', cardType: '01', createdAtUtc: now }],
    bills: [{ id: 101, hopDongId: 401, thang: 10, nam: 2026, soDienCu: 0, soDienMoi: 20, giaDien: 3500,
      soNuocCu: 0, soNuocMoi: 2, giaNuoc: 20000, tienPhong: 3000000, tongTien: 3110000, phiDichVu: 0,
      chiPhiKhac: 0, trangThai: 0, hanThanhToan: future, ngayTao: now, tenPhong: 'Phòng thử thẻ đã lưu',
      tenChuTro: 'Chủ trọ kiểm thử', sdtChuTro: '0900000000' }],
    deposits: [{ id: 201, yeuCauThueId: 301, soTien: 3000000, trangThai: 0, hanThanhToan: future, ngayTao: now }],
    requests: [{ id: 301, baiDangId: 501, nguoiThueId: 901, chuTroId: 902, tieuDeBaiDang: 'Phòng thử cọc bằng thẻ',
      tenNguoiThue: 'Người thuê kiểm thử', giaThue: 3000000, trangThai: 1, ngayTao: now }] };
}

async function installMocks(context, state) {
  await context.addInitScript(() => {
    localStorage.setItem('room_rental_token', 'synthetic-saved-card-ui-token');
    localStorage.setItem('room_rental_user', JSON.stringify({ userId: 901, id: 901, fullName: 'Tenant UI', email: 'tenant@example.test', role: 'Tenant' }));
  });
  await context.routeWebSocket(/\/hubs\/(chat|notifications)/, socket => {
    socket.onMessage(message => { if (String(message).includes('"protocol"')) socket.send('{}\u001e'); });
  });
  await context.route('**/*', async route => {
    const request = route.request(), url = new URL(request.url()), method = request.method();
    if (url.pathname.startsWith('/__saved-card-')) return route.fulfill({ contentType: 'text/html', body: `<h1>${url.pathname}</h1>` });
    if (url.pathname.startsWith('/hubs/')) return route.fulfill({ json: { negotiateVersion: 1, connectionId: 'mock-card', connectionToken: 'mock-card',
      availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text', 'Binary'] }] } });
    if (!url.pathname.startsWith('/api/')) {
      if (url.origin === base) return route.continue();
      unexpectedRequests.push(`${method} ${url.origin}${url.pathname}`); return route.abort('blockedbyclient');
    }
    if (method === 'OPTIONS') return route.fulfill({ status: 204 });
    const endpoint = url.pathname.slice(4), body = method === 'GET' || method === 'DELETE' ? undefined : request.postDataJSON();
    let data = [];
    if (method === 'GET') {
      if (endpoint === '/thanh-toan/the-da-luu') {
        if (state.failList) return route.fulfill({ status: 503, json: { success: false, message: 'Synthetic unavailable saved-card endpoint' } });
        data = { enabled: state.enabled, methods: state.methods, unavailableReason: state.enabled ? null : 'Merchant chưa bật dịch vụ.' };
      } else if (endpoint === '/hoa-don/cua-toi') data = state.bills;
      else if (endpoint === '/dat-coc/cua-toi') data = state.deposits;
      else if (endpoint.startsWith('/yeu-cau-thue/')) data = state.requests;
      else if (endpoint.includes('chua-doc') || endpoint.includes('unread')) data = 0;
    } else {
      writes.push({ method, endpoint, body });
      if (endpoint === '/thanh-toan/the-da-luu/lien-ket' && method === 'POST') {
        assert.deepEqual(Object.keys(body), ['cardType']); assert.ok(['01', '02'].includes(body.cardType));
        data = { redirectUrl: `${base}/__saved-card-link-stub`, reference: 'synthetic-link-reference' };
      } else if (endpoint === '/thanh-toan/the-da-luu/7' && method === 'DELETE') state.methods = [];
      else if (endpoint === '/thanh-toan/vnpay-tao-url' && method === 'POST') {
        assert.equal(body.savedPaymentMethodId, 7); assert.ok(body.monthlyBillId === 101 || body.depositId === 201);
        data = { paymentUrl: `${base}/__saved-card-payment-stub` };
      } else throw new Error(`Unexpected synthetic API write: ${method} ${endpoint}`);
    }
    return route.fulfill({ json: { success: true, data }, headers: { 'access-control-allow-origin': base } });
  });
}

async function capture(page, name, width) {
  await page.evaluate(() => document.fonts.ready);
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, `${name}: document overflow`);
  const dialog = page.getByRole('dialog');
  if (await dialog.count()) {
    assert.equal(await dialog.evaluate(el => el.scrollWidth > el.clientWidth + 1), false, `${name}: dialog overflow`);
    const bounds = await dialog.boundingBox(); assert.ok(bounds && bounds.x >= 0 && bounds.x + bounds.width <= width + 1);
  }
  await page.screenshot({ path: path.join(output, `${name}-${width}.png`), fullPage: !(await dialog.count()) });
  checks.push({ name, width, status: 'PASS' });
}

async function runWidth(width) {
  const state = fixture(), context = await browser.newContext({ viewport: { width, height: 960 } });
  await installMocks(context, state);
  const page = await context.newPage(); page.setDefaultTimeout(15000);
  page.on('pageerror', error => runtimeErrors.push(error.message));
  page.on('console', message => {
    if (message.type() !== 'error') return;
    if (state.failList && message.location().url.includes('/thanh-toan/the-da-luu')) expectedNetworkErrors.push(message.text());
    else consoleErrors.push(message.text());
  });
  const panel = page.getByRole('region', { name: 'Thẻ VNPay đã liên kết' });
  await page.goto(`${base}/tenant/rentals`);
  await panel.getByText('•••• 2198', { exact: true }).waitFor();
  await capture(page, 'saved-cards-panel', width);
  await panel.getByRole('button', { name: 'Liên kết thẻ VNPay', exact: true }).click();
  const dialog = page.getByRole('dialog');
  assert.equal(await dialog.locator('input').count(), 0, 'Card details are never entered in this application');
  await dialog.getByLabel('Loại thẻ').selectOption('02');
  await dialog.getByText(/tạm thu rồi hoàn khoản tiền xác thực/).waitFor();
  await capture(page, 'link-card-dialog', width);
  await dialog.getByRole('button', { name: 'Tiếp tục trên VNPay', exact: true }).click();
  await page.waitForURL('**/__saved-card-link-stub');
  assert.equal(writes.at(-1).body.cardType, '02');
  await page.goto(`${base}/tenant/rentals?cardLink=success`);
  await page.getByText('Đã liên kết thẻ VNPay. Bạn có thể chọn thẻ này khi thanh toán.', { exact: true }).waitFor();
  await page.waitForURL('**/tenant/rentals');
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).first().click();
  await dialog.getByRole('button', { name: 'Thanh toán bằng NCB •••• 2198', exact: true }).waitFor();
  await dialog.getByRole('button', { name: 'Thanh toán VNPay', exact: false }).waitFor();
  await dialog.getByRole('button', { name: /Tiền mặt trực tiếp/ }).waitFor();
  await capture(page, 'saved-card-bill-choice', width);
  await dialog.getByRole('button', { name: 'Thanh toán bằng NCB •••• 2198', exact: true }).click();
  await page.waitForURL('**/__saved-card-payment-stub');
  assert.equal(writes.at(-1).body.monthlyBillId, 101);
  assert.equal(writes.at(-1).body.orderInfo, undefined);
  await page.goto(`${base}/tenant/rentals`);
  await page.getByRole('button', { name: /Yêu cầu & Cọc/ }).click();
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).first().click();
  await dialog.getByRole('button', { name: 'Thanh toán bằng NCB •••• 2198', exact: true }).click();
  await page.waitForURL('**/__saved-card-payment-stub');
  assert.equal(writes.at(-1).body.depositId, 201);
  checks.push({ name: 'saved-card-deposit-choice', width, status: 'PASS' });
  await page.goto(`${base}/payment/result?success=true&method=token&orderId=synthetic-order&amount=3110000`);
  await page.getByText('Thẻ VNPay đã liên kết', { exact: true }).waitFor();
  await capture(page, 'saved-card-payment-result', width);
  await page.goto(`${base}/tenant/rentals`);
  await panel.getByRole('button', { name: 'Gỡ thẻ NCB •••• 2198', exact: true }).click();
  await dialog.getByText(/không đóng tài khoản ngân hàng/).waitFor();
  await capture(page, 'unlink-card-dialog', width);
  await dialog.getByRole('button', { name: 'Gỡ thẻ khỏi hệ thống', exact: true }).click();
  await panel.getByText('Bạn chưa liên kết thẻ VNPay nào.', { exact: true }).waitFor();
  assert.equal(state.methods.length, 0); assert.equal(state.bills[0].trangThai, 0);
  state.enabled = false;
  await panel.getByRole('button', { name: 'Tải lại thẻ VNPay', exact: true }).click();
  await panel.getByText(/Tính năng liên kết thẻ chưa được bật/).waitFor();
  assert.equal(await panel.getByRole('button', { name: 'Liên kết thẻ VNPay', exact: true }).count(), 0);
  await capture(page, 'saved-cards-disabled', width);
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).first().click();
  await dialog.getByRole('button', { name: /Thanh toán VNPay/ }).waitFor();
  await dialog.getByRole('button', { name: /Tiền mặt trực tiếp/ }).waitFor();
  assert.equal(await dialog.getByRole('button', { name: /Thanh toán bằng NCB/ }).count(), 0);
  await dialog.getByRole('button', { name: 'Đóng', exact: true }).click();
  state.failList = true;
  await panel.getByRole('button', { name: 'Tải lại thẻ VNPay', exact: true }).click();
  await panel.getByRole('alert').waitFor();
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).first().click();
  await dialog.getByText('Chưa thể tải thẻ đã liên kết. Bạn vẫn có thể chọn các phương thức bên dưới.', { exact: true }).waitFor();
  assert.equal(await dialog.getByRole('button', { name: /Thanh toán VNPay/ }).isEnabled(), true);
  assert.equal(await dialog.getByRole('button', { name: /Tiền mặt trực tiếp/ }).isEnabled(), true);
  await capture(page, 'saved-cards-failure-isolated', width);
  await context.close();
}

await mkdir(output, { recursive: true });
try {
  if (!process.env.SAVED_CARD_UI_URL) {
    server = spawn(process.execPath, [path.join(frontend, 'node_modules/vite/bin/vite.js'), '--host', '127.0.0.1', '--port', '5179', '--strictPort'], { cwd: frontend, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
    server.stdout.on('data', chunk => { serverOutput += chunk; }); server.stderr.on('data', chunk => { serverOutput += chunk; });
    for (let attempt = 0; attempt < 100; attempt++) {
      if (server.exitCode !== null) throw new Error(`Vite exited: ${serverOutput}`);
      if (await fetch(base).then(response => response.ok).catch(() => false)) break;
      if (attempt === 99) throw new Error(`Vite not ready: ${serverOutput}`);
      await new Promise(resolve => setTimeout(resolve, 200));
    }
  }
  browser = await chromium.launch({ channel: 'msedge', headless: true });
  for (const width of [1440, 375]) await runWidth(width);
  assert.deepEqual(runtimeErrors, []); assert.deepEqual(consoleErrors, []); assert.deepEqual(unexpectedRequests, []);
  await writeFile(path.join(output, 'report.json'), JSON.stringify({ status: 'PASS', checks, writes, runtimeErrors, consoleErrors, unexpectedRequests, expectedNetworkErrors, safety: 'Synthetic sessions, mocked APIs and local VNPay navigation only. No real database/provider access.' }, null, 2));
  console.log(`PASS: ${checks.length} saved-card UI/layout checks. Artifacts: ${output}`);
} catch (error) {
  await writeFile(path.join(output, 'report.json'), JSON.stringify({ status: 'FAIL', error: error.stack, checks, writes, runtimeErrors, consoleErrors, unexpectedRequests, serverOutput }, null, 2));
  throw error;
} finally {
  if (browser) await browser.close();
  if (server && server.exitCode === null) { server.kill(); await new Promise(resolve => { server.once('exit', resolve); setTimeout(resolve, 3000).unref(); }); }
}
