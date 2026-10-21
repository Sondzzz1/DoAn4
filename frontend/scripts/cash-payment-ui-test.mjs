import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

// Synthetic sessions, in-memory API responses and a local VNPay navigation stub.
// No real API, database, payment gateway or user account is contacted.
const require = createRequire(import.meta.url);
const moduleRoot = process.env.UI_AUDIT_NODE_MODULES || 'C:/Users/huanp/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const { chromium } = require(path.join(moduleRoot, 'playwright'));
const frontend = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = process.env.CASH_UI_OUTPUT || path.resolve(frontend, '../artifacts/cash-ui');
const base = process.env.CASH_UI_URL || 'http://127.0.0.1:5178';
const now = new Date();
const year = now.getFullYear();
const future = new Date(now.getTime() + 86400000).toISOString();
const selectedAtUtc = now.toISOString();
const checks = [];
const runtimeErrors = [];
const consoleErrors = [];
const blockedRequests = [];
const writes = [];
let server;
let browser;
let serverOutput = '';

function fixture() {
  const bill = (id, name, status = 0) => ({ id, hopDongId: 401, thang: now.getMonth() + 1, nam: year,
    soDienCu: 0, soDienMoi: 20, soDienTieuThu: 20, giaDien: 3500, tienDien: 70000,
    soNuocCu: 0, soNuocMoi: 2, soNuocTieuThu: 2, giaNuoc: 20000, tienNuoc: 40000,
    tienPhong: 3000000, phiDichVu: 0, chiPhiKhac: 0, tongTien: 3110000, trangThai: status,
    hanThanhToan: future, ngayTao: selectedAtUtc, tenPhong: name, tenNguoiThue: 'Người thuê kiểm thử',
    tenChuTro: 'Chủ trọ kiểm thử', sdtChuTro: '0900000000' });
  return { cash: [], bills: [bill(101, 'Phòng thử A'), bill(102, 'Phòng thử B', 3), bill(103, 'Phòng thử C')],
    deposits: [{ id: 201, yeuCauThueId: 301, soTien: 3000000, trangThai: 0, hanThanhToan: future, ngayTao: selectedAtUtc }],
    requests: [{ id: 301, baiDangId: 501, nguoiThueId: 901, chuTroId: 902, tieuDeBaiDang: 'Phòng thử đặt cọc',
      tenNguoiThue: 'Người thuê kiểm thử', giaThue: 3000000, trangThai: 1, ngayTao: selectedAtUtc }],
    nextId: 1, vnpayCalls: [] };
}

function makeReceipt(state, target) {
  const bill = state.bills.find(item => item.id === target.monthlyBillId);
  const deposit = state.deposits.find(item => item.id === target.depositId);
  assert.ok(bill || deposit, 'Fixture target must exist');
  return { id: state.nextId++, ...target, amount: bill?.tongTien ?? deposit.soTien, status: 0,
    tenantAccountId: 901, landlordAccountId: 902, tenantName: 'Người thuê kiểm thử', landlordName: 'Chủ trọ kiểm thử',
    landlordPhone: '0900000000', targetTitle: bill ? `Hóa đơn · ${bill.tenPhong}` : 'Tiền cọc · Phòng thử đặt cọc',
    selectedAtUtc, targetDueAt: future, canConfirm: true };
}

function confirmReceipt(state, receipt, note) {
  receipt.status = 1; receipt.confirmedAtUtc = new Date().toISOString(); receipt.receiptNumber = `TM-UI-${receipt.id}`;
  receipt.note = note; receipt.canConfirm = false;
  const bill = state.bills.find(item => item.id === receipt.monthlyBillId);
  if (bill) { bill.trangThai = 1; bill.phuongThucThanhToan = 'Tiền mặt'; bill.ngayThanhToan = receipt.confirmedAtUtc; }
  const deposit = state.deposits.find(item => item.id === receipt.depositId);
  if (deposit) { deposit.trangThai = 2; deposit.ngayThanhToan = receipt.confirmedAtUtc; }
  return receipt;
}

async function installMocks(context, state, role) {
  await context.addInitScript(({ role }) => {
    localStorage.setItem('room_rental_token', 'synthetic-ui-test-token-no-real-credentials');
    localStorage.setItem('room_rental_user', JSON.stringify({ userId: role === 'Tenant' ? 901 : 902,
      id: role === 'Tenant' ? 901 : 902, fullName: `${role} UI`, email: `${role.toLowerCase()}@example.test`, role }));
  }, { role });
  await context.routeWebSocket(/\/hubs\/(chat|notifications)/, socket => {
    socket.onMessage(message => {
      const text = String(message);
      if (text.includes('"protocol"')) socket.send('{}\u001e');
      else if (text.includes('"type":6')) socket.send('{"type":6}\u001e');
    });
  });
  await context.route('**/*', async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.pathname === '/__cash-ui-vnpay-stub') {
      assert.equal(url.origin, base);
      return route.fulfill({ contentType: 'text/html', body: '<h1>VNPay navigation stub</h1>' });
    }
    if (url.pathname.startsWith('/hubs/')) {
      return route.fulfill({ json: { negotiateVersion: 1, connectionId: `mock-${role}`, connectionToken: `mock-${role}`,
        availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text', 'Binary'] }] } });
    }
    if (!url.pathname.startsWith('/api/')) {
      if (url.origin === base) return route.continue();
      blockedRequests.push(`${request.method()} ${url.origin}${url.pathname}`);
      return route.abort('blockedbyclient');
    }
    const endpoint = url.pathname.slice(4);
    const method = request.method();
    const body = method === 'GET' ? undefined : request.postDataJSON();
    let data = [];
    if (method === 'OPTIONS') return route.fulfill({ status: 204 });
    if (method === 'GET') {
      if (endpoint === '/thanh-toan/tien-mat') data = state.cash;
      else if (endpoint === '/hoa-don/cua-toi') data = state.bills;
      else if (endpoint === '/dat-coc/cua-toi') data = state.deposits;
      else if (endpoint.startsWith('/yeu-cau-thue/')) data = state.requests;
      else if (endpoint.includes('chua-doc') || endpoint.includes('unread')) data = 0;
    } else {
      writes.push({ role, method, endpoint, body });
      if (endpoint === '/thanh-toan/tien-mat/chon' && method === 'POST') {
        data = makeReceipt(state, body); state.cash.unshift(data);
      } else if (/^\/thanh-toan\/tien-mat\/\d+\/huy$/.test(endpoint) && method === 'POST') {
        data = state.cash.find(item => item.id === Number(endpoint.split('/')[3]));
        assert.ok(data); data.status = 2; data.canConfirm = false;
      } else if (/^\/thanh-toan\/tien-mat\/\d+\/xac-nhan$/.test(endpoint) && method === 'POST') {
        data = state.cash.find(item => item.id === Number(endpoint.split('/')[3]));
        assert.ok(data); confirmReceipt(state, data, body.note);
      } else if (endpoint === '/thanh-toan/vnpay-tao-url' && method === 'POST') {
        state.vnpayCalls.push(body);
        data = { paymentUrl: `${base}/__cash-ui-vnpay-stub?bill=${body.monthlyBillId || ''}&deposit=${body.depositId || ''}` };
      } else if (/^\/hoa-don\/\d+\/thanh-toan$/.test(endpoint) && method === 'PUT') {
        assert.equal(body.phuongThucThanhToan, 'Tiền mặt');
        const monthlyBillId = Number(endpoint.split('/')[2]);
        assert.equal(body.expectedAmount, state.bills.find(item => item.id === monthlyBillId).tongTien, 'Cash confirmation submits the displayed amount snapshot');
        let receipt = state.cash.find(item => item.monthlyBillId === monthlyBillId && item.status === 0);
        if (!receipt) { receipt = makeReceipt(state, { monthlyBillId }); state.cash.unshift(receipt); }
        confirmReceipt(state, receipt, body.ghiChu);
        data = state.bills.find(item => item.id === monthlyBillId);
      } else throw new Error(`Unexpected mocked API mutation: ${method} ${endpoint}`);
    }
    return route.fulfill({ json: { success: true, message: 'Synthetic UI fixture', data }, headers: {
      'access-control-allow-origin': base, 'access-control-allow-headers': '*', 'access-control-allow-methods': '*' } });
  });
}

async function inspect(page, name, width) {
  await page.evaluate(() => document.fonts.ready);
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, `${name}: document overflow`);
  const dialog = page.getByRole('dialog');
  if (await dialog.count()) {
    const bounds = await dialog.boundingBox();
    assert.ok(bounds && bounds.x >= 0 && bounds.x + bounds.width <= width + 1, `${name}: dialog bounds`);
    assert.equal(await dialog.evaluate(el => el.scrollWidth > el.clientWidth + 1), false, `${name}: dialog horizontal overflow`);
  }
  await page.screenshot({ path: path.join(output, `${name}-${width}.png`), fullPage: !(await dialog.count()) });
  checks.push({ name, width, status: 'PASS' });
}

async function openSession(role, state, width) {
  const context = await browser.newContext({ viewport: { width, height: 960 } });
  await installMocks(context, state, role);
  const page = await context.newPage();
  page.on('pageerror', error => runtimeErrors.push({ role, width, error: error.message }));
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push({ role, width, error: message.text() }); });
  page.setDefaultTimeout(15000);
  return { context, page };
}

async function closeDialog(page) {
  await page.getByRole('dialog').getByRole('button', { name: 'Đóng', exact: true }).first().click();
  await page.getByRole('dialog').waitFor({ state: 'hidden' });
}

async function runWidth(width) {
  const state = fixture();
  const tenant = await openSession('Tenant', state, width);
  const page = tenant.page;
  await page.goto(`${base}/tenant/rentals`);
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).first().waitFor();
  await inspect(page, 'tenant-bills', width);
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).first().click();
  let dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: /^Thanh toán VNPay/ }).waitFor();
  await dialog.getByRole('button', { name: /^Tiền mặt trực tiếp/ }).waitFor();
  await inspect(page, 'tenant-bill-payment-methods', width);
  state.bills[0].tongTien = 3210000;
  await dialog.getByRole('button', { name: /^Tiền mặt trực tiếp/ }).click();
  await dialog.getByText('Đã chọn tiền mặt · Chưa thanh toán', { exact: true }).waitFor();
  assert.equal(state.bills[0].trangThai, 0, 'Choosing cash must not mark a bill paid');
  assert.equal(state.cash[0].status, 0);
  assert.equal((await dialog.locator('.cash-payment-summary strong').textContent()).replace(/\D/g, ''), '3210000', 'Selected cash summary uses authoritative server receipt amount');
  await dialog.getByText('Chủ trọ: Chủ trọ kiểm thử', { exact: true }).waitFor();
  checks.push({ width, name: 'cash-selection-displays-updated-server-amount', status: 'PASS' });
  await inspect(page, 'tenant-bill-cash-selected', width);
  await dialog.getByRole('button', { name: 'Hủy lựa chọn để đổi phương thức', exact: true }).click();
  await dialog.getByRole('button', { name: /^Thanh toán VNPay/ }).waitFor();
  assert.equal(state.cash[0].status, 2);
  assert.equal(state.bills[0].trangThai, 0);
  assert.equal((await dialog.locator('.cash-payment-summary strong').textContent()).replace(/\D/g, ''), '3210000', 'Cancel/switch retains the refreshed bill amount');
  await Promise.all([page.waitForURL('**/__cash-ui-vnpay-stub*'), dialog.getByRole('button', { name: /^Thanh toán VNPay/ }).click()]);
  assert.equal(state.vnpayCalls.at(-1).monthlyBillId, 101);
  checks.push({ width, name: 'cash-cancel-restores-vnpay-navigation', status: 'PASS' });

  await page.goto(`${base}/tenant/rentals`);
  await page.getByRole('button', { name: /Yêu cầu & Cọc/ }).click();
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).waitFor();
  await page.getByRole('button', { name: 'Thanh toán', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: /^Thanh toán VNPay/ }).waitFor();
  await dialog.getByRole('button', { name: /^Tiền mặt trực tiếp/ }).waitFor();
  await inspect(page, 'tenant-deposit-payment-methods', width);
  await dialog.getByRole('button', { name: /^Tiền mặt trực tiếp/ }).click();
  await dialog.getByText('Đã chọn tiền mặt · Chưa thanh toán', { exact: true }).waitFor();
  assert.equal(state.deposits[0].trangThai, 0, 'Choosing cash must not confirm a deposit');
  assert.equal(state.deposits[0].hanThanhToan, future, 'Choosing cash must not extend deadline');
  await inspect(page, 'tenant-deposit-cash-selected', width);
  await closeDialog(page);

  const landlord = await openSession('Landlord', state, width);
  const ownerPage = landlord.page;
  await ownerPage.goto(`${base}/landlord/contracts?tab=requests&section=deposits`);
  await ownerPage.getByRole('button', { name: 'Xác nhận đã nhận tiền mặt', exact: true }).click();
  const ownerDialog = ownerPage.getByRole('dialog');
  const confirm = ownerDialog.getByRole('button', { name: 'Xác nhận và tạo phiếu thu', exact: true });
  assert.equal(await confirm.isEnabled(), false, 'Actual receipt acknowledgement is required');
  await ownerDialog.getByLabel('Ghi chú phiếu thu').fill('Đã nhận đủ tiền cọc tại phòng kiểm thử.');
  await ownerDialog.getByRole('checkbox').check();
  assert.equal(await confirm.isEnabled(), true);
  await inspect(ownerPage, 'landlord-deposit-confirmation', width);
  await confirm.click();
  await ownerPage.getByRole('dialog').waitFor({ state: 'hidden' });
  const depositReceipt = state.cash.find(item => item.depositId === 201);
  assert.equal(depositReceipt.status, 1);
  assert.equal(state.deposits[0].trangThai, 2);
  assert.equal(depositReceipt.note, 'Đã nhận đủ tiền cọc tại phòng kiểm thử.');
  await ownerPage.getByText(depositReceipt.receiptNumber, { exact: true }).waitFor();
  await inspect(ownerPage, 'landlord-deposit-receipt', width);

  await ownerPage.goto(`${base}/landlord/contracts?tab=bills`);
  await ownerPage.getByRole('button', { name: 'Thu tiền mặt', exact: true }).first().waitFor();
  assert.equal(await ownerPage.locator('.bill-card').filter({ hasText: 'Phòng thử B' }).getByRole('button', { name: 'Thu tiền mặt', exact: true }).count(), 0, 'No manual cash action while VNPay pending');
  await ownerPage.getByRole('button', { name: 'Thu tiền mặt', exact: true }).first().click();
  const billDialog = ownerPage.getByRole('dialog');
  const billConfirm = billDialog.getByRole('button', { name: 'Xác nhận và tạo phiếu thu', exact: true });
  assert.equal(await billConfirm.isEnabled(), false);
  await billDialog.getByLabel('Ghi chú phiếu thu').fill('Thu đủ tiền phòng và điện nước.');
  await billDialog.getByRole('checkbox').check();
  await inspect(ownerPage, 'landlord-bill-cash-confirmation', width);
  await billConfirm.click();
  await ownerPage.getByRole('dialog').waitFor({ state: 'hidden' });
  const billReceipt = state.cash.find(item => item.monthlyBillId === 101 && item.status === 1);
  assert.ok(billReceipt);
  assert.equal(state.bills[0].trangThai, 1);
  assert.equal(billReceipt.note, 'Thu đủ tiền phòng và điện nước.');
  await ownerPage.getByText(billReceipt.receiptNumber, { exact: true }).waitFor();
  await inspect(ownerPage, 'landlord-bill-receipt', width);

  await page.goto(`${base}/tenant/rentals`);
  await page.getByText(billReceipt.receiptNumber, { exact: true }).waitFor();
  await inspect(page, 'tenant-received-bill-receipt', width);
  await page.getByRole('button', { name: /Yêu cầu & Cọc/ }).click();
  await page.getByText(depositReceipt.receiptNumber, { exact: true }).waitFor();
  await inspect(page, 'tenant-received-deposit-receipt', width);
  await tenant.context.close();
  await landlord.context.close();
}

await mkdir(output, { recursive: true });
try {
  if (!process.env.CASH_UI_URL) {
    server = spawn(process.execPath, [path.join(frontend, 'node_modules/vite/bin/vite.js'), '--host', '127.0.0.1', '--port', '5178', '--strictPort'],
      { cwd: frontend, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
    server.stdout.on('data', chunk => { serverOutput += chunk; });
    server.stderr.on('data', chunk => { serverOutput += chunk; });
    for (let attempt = 0; attempt < 100; attempt++) {
      if (server.exitCode !== null) throw new Error(`Isolated Vite exited: ${serverOutput}`);
      if (await fetch(base).then(response => response.ok).catch(() => false)) break;
      if (attempt === 99) throw new Error(`Isolated Vite was not ready: ${serverOutput}`);
      await new Promise(resolve => setTimeout(resolve, 200));
    }
  }
  browser = await chromium.launch({ channel: 'msedge', headless: true });
  for (const width of [1440, 375]) await runWidth(width);
  assert.deepEqual(runtimeErrors, [], 'No browser runtime errors');
  assert.deepEqual(consoleErrors, [], 'No browser console errors');
  assert.deepEqual(blockedRequests, [], 'Every network request must be local or mocked');
  await writeFile(path.join(output, 'report.json'), JSON.stringify({ status: 'PASS', checks, runtimeErrors, consoleErrors,
    blockedRequests, mockedWrites: writes, safety: 'Synthetic auth and mocked API only. No database or real VNPay access.' }, null, 2));
  console.log(`PASS: ${checks.length} UI/layout checks at 1440px and 375px; all payment writes mocked. Artifacts: ${output}`);
} catch (error) {
  await writeFile(path.join(output, 'report.json'), JSON.stringify({ status: 'FAIL', error: error.stack, checks, runtimeErrors, consoleErrors,
    blockedRequests, mockedWrites: writes, serverOutput }, null, 2));
  throw error;
} finally {
  if (browser) await browser.close();
  if (server && server.exitCode === null) {
    server.kill();
    await new Promise(resolve => { server.once('exit', resolve); setTimeout(resolve, 3000).unref(); });
  }
}
