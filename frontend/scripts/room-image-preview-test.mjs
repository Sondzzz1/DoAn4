import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { execFileSync } from 'node:child_process';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { isRoomImageUrl, normalizeRoomImageUrls } from '../src/utils/roomImageUrls.ts';

const require = createRequire(import.meta.url);
const { chromium } = require(path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright'));
const auth = JSON.parse(await readFile(process.env.ROOM_IMAGE_TEST_AUTH, 'utf8'));
const urls = JSON.parse(await readFile(process.env.ROOM_IMAGE_TEST_URLS, 'utf8'));
const output = process.env.ROOM_IMAGE_TEST_OUTPUT;
assert.ok(output);
await mkdir(output, { recursive: true });
assert.ok(urls.length >= 2);
const checks = [];
const pass = name => { checks.push({ name, status: 'PASS' }); console.log('PASS ' + name); };
for (const value of ['https://example.com/a.jpg', 'http://example.com/b.webp', '/uploads/a.jpg']) assert.ok(isRoomImageUrl(value));
for (const value of ['data:image/png;base64,AAAA', 'blob:https://example.com/id', 'javascript:alert(1)', '//example.com/a.jpg', '/uploads/../private/a.jpg', 'invalid', '']) assert.equal(isRoomImageUrl(value), false);
assert.deepEqual(normalizeRoomImageUrls(['  ' + urls[0] + '  ', urls[0], urls[1]]), urls);
assert.throws(() => normalizeRoomImageUrls(['data:image/png;base64,AAAA']));
pass('URL validation, trimming, duplicate removal and Base64 rejection');

const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
await context.addInitScript(session => {
  const { token, ...user } = session;
  localStorage.setItem('room_rental_token', token);
  localStorage.setItem('room_rental_user', JSON.stringify(user));
}, auth);
const page = await context.newPage();
const runtimeErrors = [];
page.on('pageerror', error => runtimeErrors.push(error.message));
const root = path.resolve('..');
const name = 'QA URL preview ' + Date.now();
let roomId;
const apiHeaders = { Authorization: 'Bearer ' + auth.token };
const dbImages = id => JSON.parse(execFileSync('pwsh', ['-NoProfile', '-Command', `
  $ErrorActionPreference='Stop'
  $config=Get-Content -Raw backend/RoomRental.BackEnd/appsettings.Local.json | ConvertFrom-Json
  $connection=[System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.DefaultConnection)
  $connection.Open()
  try {
    $command=$connection.CreateCommand()
    $command.CommandText='SELECT DuongDan,LaAnhDaiDien FROM HinhAnhPhong WHERE PhongTroId=@id ORDER BY ThuTu'
    $null=$command.Parameters.Add('@id',[System.Data.SqlDbType]::Int)
    $command.Parameters['@id'].Value=[int]$env:ROOM_IMAGE_TEST_ID
    $reader=$command.ExecuteReader()
    $items=@()
    while($reader.Read()){$items+=@{url=[string]$reader['DuongDan'];thumbnail=[bool]$reader['LaAnhDaiDien']}}
    $reader.Close()
    ConvertTo-Json -InputObject $items -Compress
  } finally { $connection.Dispose() }
`], { cwd: root, env: { ...process.env, ROOM_IMAGE_TEST_ID: String(id) }, encoding: 'utf8' }).trim());

try {
  await page.goto('http://127.0.0.1:5173/landlord/rooms');
  await page.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
  const dialog = page.getByRole('dialog');
  const input = page.getByLabel('URL ảnh', { exact: true });
  const add = page.getByRole('button', { name: 'Thêm ảnh', exact: true });
  await input.fill('  ' + urls[0] + '  ');
  const preview = page.getByRole('img', { name: 'Ảnh xem trước từ URL', exact: true });
  await preview.waitFor({ state: 'visible', timeout: 60000 });
  assert.equal(await preview.getAttribute('src'), urls[0]);
  assert.ok(await preview.evaluate(img => img.naturalWidth > 0));
  assert.equal(await preview.evaluate(img => getComputedStyle(img).objectFit), 'cover');
  pass('Bing URL previews immediately through img src, without conversion');
  await add.click();
  await page.getByRole('img', { name: 'Ảnh phòng 1', exact: true }).waitFor();
  assert.equal(await input.inputValue(), '');
  await input.fill(urls[0]);
  await page.getByText('URL ảnh này đã có trong danh sách.').waitFor();
  assert.ok(await add.isDisabled());
  await input.fill('data:image/png;base64,AAAA');
  assert.ok(await add.isDisabled());
  assert.equal(await page.locator('.room-image-draft img').count(), 0);
  pass('Duplicate and Base64 URL cannot be added');
  await input.fill('http://127.0.0.1:5173/missing-room-image-qa.png');
  await page.locator('.room-image-draft').getByText('Không thể tải ảnh từ URL này.').waitFor();
  pass('Broken image shows the required warning');
  await input.fill(urls[1]);
  await preview.waitFor({ state: 'visible', timeout: 60000 });
  await add.click();
  await page.getByRole('radio', { name: 'Chọn ảnh 2 làm ảnh đại diện' }).click();
  assert.ok(await page.getByRole('radio', { name: 'Chọn ảnh 1 làm ảnh đại diện' }).isChecked());
  assert.equal(await page.getByRole('img', { name: 'Ảnh phòng 1', exact: true }).getAttribute('src'), urls[1]);
  pass('Thumbnail selection reorders the original URLs');
  for (const width of [1440, 768, 375]) {
    await page.setViewportSize({ width, height: 1000 });
    await page.locator('.room-image-grid').scrollIntoViewIfNeeded();
    assert.equal(await dialog.evaluate(el => el.scrollWidth > el.clientWidth + 1), false);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false);
    await page.screenshot({ path: path.join(output, 'image-grid-' + width + '.png') });
    pass('Image grid and modal fit width ' + width);
  }
  await page.getByLabel('Tên phòng').fill(name);
  await page.getByLabel('Giá thuê (VNĐ/tháng)').fill('3000000');
  await page.getByLabel('Diện tích (m²)').fill('20');
  await page.getByLabel('Địa chỉ', { exact: false }).first().fill('42 Nguyễn Lân');
  const created = page.waitForResponse(r => new URL(r.url()).pathname === '/api/phong' && r.request().method() === 'POST');
  await dialog.getByRole('button', { name: 'Thêm phòng', exact: true }).click();
  const createResponse = await created;
  assert.equal(createResponse.ok(), true, await createResponse.text());
  const createBody = createResponse.request().postDataJSON();
  assert.deepEqual(createBody.imageUrls, [urls[1], urls[0]]);
  assert.equal('danhSachAnh' in createBody, false);
  roomId = (await createResponse.json()).data.id;
  assert.deepEqual(dbImages(roomId), [{ url: urls[1], thumbnail: true }, { url: urls[0], thumbnail: false }]);
  pass('Create Room sends imageUrls unchanged and SQL stores exactly those URLs');
  await dialog.waitFor({ state: 'hidden' });
  await page.reload();
  await page.getByLabel('Tìm phòng của tôi').fill(name);
  await page.getByRole('button', { name: 'Sửa', exact: true }).click();
  await page.getByRole('img', { name: 'Ảnh phòng 1', exact: true }).waitFor({ timeout: 60000 });
  assert.equal(await page.getByRole('img', { name: 'Ảnh phòng 1', exact: true }).getAttribute('src'), urls[1]);
  pass('Reload and edit restore the persisted image previews');
  await page.getByRole('button', { name: 'Xóa ảnh 1', exact: true }).click();
  assert.equal(await page.locator('.room-image-item').count(), 1);
  assert.equal(await page.getByRole('img', { name: 'Ảnh phòng 1', exact: true }).getAttribute('src'), urls[0]);
  const updated = page.waitForResponse(r => new URL(r.url()).pathname === '/api/phong/' + roomId && r.request().method() === 'PUT');
  await page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
  const updateResponse = await updated;
  assert.equal(updateResponse.ok(), true, await updateResponse.text());
  assert.deepEqual(updateResponse.request().postDataJSON().imageUrls, [urls[0]]);
  assert.deepEqual(dbImages(roomId), [{ url: urls[0], thumbnail: true }]);
  pass('Delete image removes it from preview, edit payload and SQL; remaining image is thumbnail');
  await dialog.waitFor({ state: 'hidden' });
  await page.reload();
  await page.getByLabel('Tìm phòng của tôi').fill(name);
  await page.getByRole('button', { name: 'Sửa', exact: true }).click();
  await page.getByRole('img', { name: 'Ảnh phòng 1', exact: true }).waitFor({ timeout: 60000 });
  assert.equal(await page.locator('.room-image-item').count(), 1);
  assert.deepEqual(runtimeErrors, []);
  pass('Edited image list survives reload without JavaScript errors');
} finally {
  if (roomId) {
    const response = await fetch('http://localhost:5000/api/phong/' + roomId, { method: 'DELETE', headers: apiHeaders });
    checks.push({ name: 'Dedicated QA room suspended after test', status: response.ok ? 'PASS' : 'FAIL' });
  }
  await browser.close();
  await writeFile(path.join(output, 'results.json'), JSON.stringify({ roomId, roomName: name, checks }, null, 2));
}
