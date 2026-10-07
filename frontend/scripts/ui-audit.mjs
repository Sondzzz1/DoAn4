import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.UI_AUDIT_NODE_MODULES
  ? path.join(process.env.UI_AUDIT_NODE_MODULES, 'playwright') : 'playwright');
const baseURL = process.env.UI_AUDIT_URL || 'http://127.0.0.1:5173';
const output = path.resolve('../docs/ui-audit', process.argv[2] || 'after');
await mkdir(output, { recursive: true });
const photo = 'https://images.unsplash.com/photo-1618219908412-a29a1bb7b86e?w=800&fit=crop';
const title = 'Nhà trọ Bích Tùng - phòng có ban công, đầy đủ tiện nghi';
const date = new Date().toISOString();
const categories = ['Phòng trọ', 'Nhà nguyên căn', 'Căn hộ', 'Ở ghép'].map((name, index) => ({ id: index + 1, name, isActive: true, description: 'Phòng ở phù hợp cho sinh viên và người đi làm.' }));
const posts = Array.from({ length: 4 }, (_, index) => ({ id: index + 1, title: index ? `Phòng ${index + 1} thoáng sáng, gần trường đại học` : title, price: 3200000 + index * 300000, status: 1, area: 28, maxOccupants: 2, roomStatus: 0, address: '123 Nguyễn Văn Linh', province: 'Hồ Chí Minh', district: 'Bình Thạnh', ward: 'Phường 12', categoryId: 1, categoryName: 'Phòng trọ', landlordName: 'Nguyễn Thị Bích Tùng', landlordId: 2, landlordAccountId: 20, landlordPhone: '0901234567', thumbnailUrl: photo, imageUrls: [photo, photo], amenities: categories.slice(0, 2), description: 'Phòng có cửa sổ lớn, ban công và bếp riêng. Khu dân cư yên tĩnh, thuận tiện đi lại.', roomId: 1, createdAt: date, updatedAt: date }));
const rooms = posts.map(p => ({ ...p, roomName: p.title, status: 0, amenityIds: [1], bedrooms: 1, bathrooms: 1, floor: 2 }));
const requests = [0, 1, 1, 4].map((trangThai, i) => ({ id: i + 1, baiDangId: i + 1, nguoiThueId: 21, chuTroId: 20, tieuDeBaiDang: title, tenNguoiThue: 'Nguyễn Minh Anh', sdtNguoiThue: '0901234567', giaThue: 3200000, anhPhong: photo, diaChi: '123 Nguyễn Văn Linh, Bình Thạnh', trangThai, ngayTao: date }));
const contracts = [{ id: 1, yeuCauThueId: 4, baiDangId: 1, nguoiThueId: 21, chuTroId: 20, ngayBatDau: '2026-10-01', ngayKetThuc: '2027-10-01', tienThueHangThang: 3200000, tienDatCoc: 3200000, giaDien: 3500, giaNuoc: 15000, phiDichVu: 100000, dieuKhoan: 'Thanh toán tiền thuê trước ngày 5 mỗi tháng.', trangThai: 1, nguoiThueDaXacNhan: true, chuTroDaXacNhan: true, tenPhong: title, tenNguoiThue: 'Nguyễn Minh Anh', tenChuTro: 'Nguyễn Thị Bích Tùng', diaChiPhong: '123 Nguyễn Văn Linh' }];
const bills = [{ id: 1, hopDongId: 1, thang: 10, nam: 2026, trangThai: 0, tienPhong: 3200000, soDienCu: 120, soDienMoi: 160, soDienTieuThu: 40, giaDien: 3500, tienDien: 140000, soNuocCu: 30, soNuocMoi: 33, soNuocTieuThu: 3, giaNuoc: 15000, tienNuoc: 45000, phiDichVu: 100000, chiPhiKhac: 0, tongTien: 3485000, tenPhong: title, tenChuTro: 'Nguyễn Thị Bích Tùng', tenNguoiThue: 'Nguyễn Minh Anh', hanThanhToan: '2026-10-20' }];
const blogs = [{ id: 1, title: 'Những điều cần kiểm tra khi xem phòng trọ', content: 'Kiểm tra vị trí, tiện ích và các khoản chi phí trước khi thuê phòng.', summary: 'Thông tin cần chuẩn bị trước buổi xem phòng.', thumbnailUrl: photo, imageUrl: photo, authorName: 'Timnhatro.vn', createdAt: date }];
const apiData = (url) => {
  const p = url.pathname.replace('/api', '');
  if (p.includes('danh-gia')) return [];
  if (p.includes('tong-quan')) return { totalUsers: 40, totalTenants: 28, totalLandlords: 12, totalRooms: 30, availableRooms: 12, rentedRooms: 18, totalPosts: 24, pendingPosts: 5, approvedPosts: 16, hiddenPosts: 3, totalAppointments: 8, pendingReports: 2, totalReports: 4, totalViewCount: 230 };
  if (p.includes('nguoi-dung') && p.includes('quan-tri')) return Array.from({ length: 14 }, (_, i) => ({ id: i + 1, fullName: 'Nguyễn Minh Anh', email: 'minhanh@example.com', roleName: 'Người thuê', postCount: 0, roomCount: 0, isActive: true, createdAt: date }));
  if (p.includes('ho-so') || p.includes('profile') || p.includes('thong-tin')) return { id: 21, fullName: 'Nguyễn Minh Anh', email: 'minhanh@example.com', role: 'Tenant', phone: '0901234567', createdAt: date };
  if (p.includes('danh-muc') || p.includes('tien-ich')) return categories;
  if (p.includes('bai-viet')) return /\/\d+$/.test(p) ? blogs[0] : blogs;
  if (p.includes('quan-tri/bai-dang') && url.searchParams.get('status') === '0') return posts.map(post => ({ ...post, status: 0 }));
  if (p.includes('bai-dang')) return /\/\d+$/.test(p) ? posts[0] : posts;
  if (p.includes('kiem-tra')) return false;
  if (p.includes('yeu-thich')) return posts;
  if (/\/phong(?:\/|$)/.test(p)) return /\/\d+$/.test(p) ? rooms[0] : rooms;
  if (p.includes('yeu-cau-thue')) return requests;
  if (p.includes('dat-coc')) return [{ id: 1, yeuCauThueId: 2, soTien: 3200000, trangThai: 0, ngayTao: date, hanThanhToan: '2026-10-20T18:00:00' }, { id: 2, yeuCauThueId: 3, soTien: 3200000, trangThai: 2, ngayTao: date }];
  if (p.includes('hop-dong')) return /\/\d+$/.test(p) ? contracts[0] : contracts;
  if (p.includes('hoa-don')) return bills;
  if (p.includes('su-co')) return [{ id: 1, hopDongId: 1, tieuDe: 'Máy lạnh cần bảo dưỡng', moTa: 'Máy lạnh phát ra tiếng ồn khi hoạt động.', trangThai: 0, ngayTao: date }];
  if (p.includes('danh-gia')) return [];
  if (p.includes('lich-hen')) return [{ id: 1, postId: 1, postTitle: title, roomName: title, postAddress: '123 Nguyễn Văn Linh', landlordName: 'Nguyễn Thị Bích Tùng', landlordPhone: '0901234567', tenantName: 'Nguyễn Minh Anh', scheduledAt: '2026-10-15T09:00:00', status: 0 }];
  if (p.includes('bao-cao')) return [{ id: 1, postTitle: title, reason: 'Thông tin cần xác minh.', reporterName: 'Nguyễn Minh Anh', status: 0, createdAt: date }];
  if (p.includes('chua-doc') || p.includes('unread')) return 1;
  if (p.includes('thong-bao')) return [{ id: 1, title: 'Yêu cầu thuê phòng đã được duyệt', content: 'Chủ trọ đã thiết lập khoản đặt cọc cho phòng của bạn.', isRead: false, link: '/tenant/rentals', createdAt: date }];
  return [];
};

const groups = {
  Public: ['/', '/rooms', '/rooms/1', '/blog', '/blog/1', '/payment/result', '/login', '/register', '/missing-page'],
  Tenant: ['/tenant/profile', '/tenant/favorites', '/tenant/appointments', '/tenant/rentals'],
  Landlord: ['/landlord/dashboard', '/landlord/posts', '/landlord/posts/create', '/landlord/posts/1/edit', '/landlord/appointments', '/landlord/rooms', '/landlord/contracts?tab=requests', '/landlord/contracts?tab=contracts', '/landlord/contracts?tab=bills'],
  Admin: ['/admin/dashboard', '/admin/users', '/admin/posts', '/admin/posts/approval', '/admin/rooms', '/admin/categories', '/admin/amenities', '/admin/reports'],
};
const widths = [1440, 1200, 1024, 768, 375];
const screenshotRoutes = ['/', '/rooms', '/rooms/1', '/tenant/rentals', '/admin/categories', '/admin/dashboard', '/landlord/rooms'];
const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const results = [];
const issues = [];
const interactions = [];
const ensure = (condition, message) => { if (!condition) throw new Error(message); };
async function inspectDialog(page, role, width, name) {
  const dialog = page.getByRole('dialog', { name });
  await dialog.waitFor();
  const box = await dialog.boundingBox();
  const viewport = page.viewportSize();
  ensure(box && box.x >= 0 && box.y >= 0 && box.x + box.width <= viewport.width + 1 && box.y + box.height <= viewport.height + 1, 'Dialog exceeds viewport');
  ensure(await dialog.evaluate(el => {
    const r = el.getBoundingClientRect();
    return el.contains(document.elementFromPoint(r.left + r.width / 2, r.top + 20));
  }), 'Dialog is covered by navigation');
  ensure(await page.evaluate(() => document.body.style.overflow === 'hidden'), 'Background scroll is not locked');
  ensure(await dialog.locator('input,select,textarea').evaluateAll(fields => fields.every(field => field.labels?.length || field.getAttribute('aria-label'))), 'Form fields lack accessible labels');
  for (let i = 0; i < 4; i++) {
    await page.keyboard.press('Tab');
    ensure(await dialog.evaluate(el => el.contains(document.activeElement)), 'Keyboard focus escaped dialog');
  }
  if (width === 375) await page.screenshot({ path: path.join(output, `${role}-dialog-${name.replaceAll(' ', '_')}-${width}.png`) });
}
async function checkInteractions(page, role, width) {
  await page.setViewportSize({ width, height: width === 375 ? 560 : 900 });
  const go = async route => { await page.goto(baseURL + route); await page.waitForTimeout(350); };
  const modal = async (button, title) => {
    await button.click();
    await inspectDialog(page, role, width, title);
    await page.keyboard.press('Escape');
    await page.getByRole('dialog', { name: title }).waitFor({ state: 'hidden' });
    ensure(await page.evaluate(() => document.body.style.overflow !== 'hidden'), 'Scroll lock remains after closing dialog');
  };
  if (role === 'Public') {
    await go('/');
    await page.getByRole('link', { name: 'Dưới 2 triệu' }).click();
    ensure(page.url().includes('maxPrice=2000000'), 'Budget filter did not navigate');
    await go('/login');
    await page.locator('form button[type="submit"]').click();
    ensure(await page.getByRole('alert').count() === 2, 'Login lacks inline validation');
    await go('/register');
    await page.locator('form button[type="submit"]').click();
    ensure(await page.getByRole('alert').count() === 6, 'Register lacks inline validation');
  }
  if (role === 'Tenant') {
    await go('/rooms/1');
    await page.getByRole('button', { name: 'Mở ảnh phòng', exact: true }).click();
    await inspectDialog(page, role, width, 'Ảnh phòng');
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('Escape');
    await page.getByRole('button', { name: 'Đặt lịch xem phòng', exact: true }).click();
    await inspectDialog(page, role, width, 'Đặt lịch xem phòng');
    await page.getByRole('button', { name: 'Xác nhận lịch hẹn', exact: true }).click();
    ensure(await page.getByRole('alert').count() >= 3, 'Empty booking form lacks field validation');
    await page.keyboard.press('Escape');
    await modal(page.getByRole('button', { name: 'Gửi yêu cầu thuê phòng', exact: true }), 'Gửi yêu cầu thuê phòng');
    let requestsSent = 0;
    const failRequest = async route => {
      requestsSent++;
      await new Promise(resolve => setTimeout(resolve, 600));
      await route.fulfill({ status: 400, json: { success: false, message: 'Yêu cầu này đã tồn tại.', data: null } });
    };
    await page.route('**/api/yeu-cau-thue', failRequest);
    await page.getByRole('button', { name: 'Gửi yêu cầu thuê phòng', exact: true }).click();
    await page.getByRole('button', { name: 'Gửi yêu cầu', exact: true }).click();
    ensure(await page.getByRole('button', { name: 'Đang gửi...', exact: true }).isDisabled(), 'Submit button remains enabled during request');
    await page.getByRole('dialog').locator('form').evaluate(form => form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
    await page.getByRole('alert').filter({ hasText: 'Yêu cầu này đã tồn tại.' }).waitFor();
    ensure(requestsSent === 1, 'Duplicate request submitted');
    await page.keyboard.press('Escape');
    await page.unroute('**/api/yeu-cau-thue', failRequest);
    await page.getByRole('button', { name: 'Thông báo', exact: true }).click();
    const notification = await page.locator('.notification-popover').boundingBox();
    ensure(notification && notification.x >= 0 && notification.x + notification.width <= width + 1, 'Notification popover exceeds viewport');
    await page.locator('h1').first().click();
    await page.getByRole('button', { name: 'Nhắn tin trực tiếp' }).click();
    const chat = await page.getByRole('dialog', { name: 'Tin nhắn' }).boundingBox();
    ensure(chat && chat.x >= 0 && chat.y >= 0 && chat.x + chat.width <= width + 1 && chat.y + chat.height <= page.viewportSize().height + 1, 'Chat exceeds viewport');
    await page.getByRole('button', { name: 'Đóng tin nhắn' }).click();
    await go('/tenant/rentals');
    await page.locator('.rental-tabs').getByRole('button', { name: /^Hợp đồng/ }).click();
    await modal(page.getByRole('button', { name: /Báo.*sự cố/i }).first(), 'Báo cáo sự cố phòng trọ');
    await modal(page.getByRole('button', { name: /Đánh giá phòng/i }).first(), 'Đánh giá trải nghiệm phòng trọ');
    await go('/tenant/profile');
    await page.getByRole('textbox', { name: /Họ và tên/ }).fill('');
    await page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
    await page.getByRole('alert').filter({ hasText: 'Họ và tên không được để trống.' }).waitFor();
    await page.getByRole('button', { name: 'Đổi mật khẩu', exact: true }).click();
    await page.locator('form button[type="submit"]').click();
    ensure(await page.getByRole('alert').count() === 3, 'Password form lacks inline validation');
  }
  if (role === 'Landlord') {
    await page.route(/https:\/\/[^/]*tile[^/]*\/(?:.*)\.png/, route => route.abort());
    await go('/landlord/posts/create');
    await page.getByRole('status').filter({ hasText: 'Bản đồ tạm thời không khả dụng.' }).waitFor();
    ensure(await page.locator('form').count() === 1, 'Map failure unmounted post form');
    await page.locator('form button[type="submit"]').click();
    ensure(await page.getByRole('alert').count() === 6, 'Post form lacks inline validation');
    await page.unroute(/https:\/\/[^/]*tile[^/]*\/(?:.*)\.png/);
    await go('/landlord/contracts?tab=requests');
    await modal(page.getByRole('button', { name: 'Duyệt Yêu Cầu', exact: true }).first(), 'Duyệt yêu cầu và thiết lập cọc');
    await modal(page.getByRole('button', { name: 'Tạo Hợp Đồng Thuê', exact: true }).first(), 'Tạo hợp đồng thuê phòng');
    await go('/landlord/contracts?tab=contracts');
    await modal(page.getByRole('button', { name: 'Lập Hóa Đơn', exact: true }).first(), 'Tạo hóa đơn hàng tháng');
    await go('/landlord/contracts?tab=bills');
    await modal(page.locator('button[title="Chỉnh sửa chỉ số"]').first(), 'Cập nhật hóa đơn #1 (10/2026)');
  }
  if (role === 'Admin') {
    await go('/admin/categories');
    await modal(page.getByRole('button', { name: 'Thêm mới', exact: true }), 'Thêm danh mục');
    await page.getByRole('textbox', { name: 'Tìm kiếm dữ liệu' }).fill('Không có danh mục nào tên này');
    await page.getByText('Chưa có dữ liệu', { exact: true }).waitFor();
    await go('/admin/users');
    await page.getByRole('button', { name: 'Trang sau' }).click();
    ensure(await page.locator('tbody tr').count() === 4, 'Admin pagination did not change rows');
    await go('/admin/posts/approval');
    await modal(page.getByRole('button', { name: 'Từ chối tin đăng', exact: true }).first(), 'Từ chối tin đăng');
  }
  if (width <= 1024) {
    await page.getByRole('button', { name: 'Mở menu', exact: true }).click();
    await page.keyboard.press('Escape');
    ensure(await page.getByRole('button', { name: 'Mở menu', exact: true }).getAttribute('aria-expanded') === 'false', 'Menu did not close on Escape');
  }
}
try {
  for (const [role, routes] of Object.entries(groups)) {
    const context = await browser.newContext();
    if (role !== 'Public') await context.addInitScript(role => {
      localStorage.setItem('room_rental_token', 'ui-audit-fixture');
      localStorage.setItem('room_rental_user', JSON.stringify({ id: 21, userId: 21, fullName: 'Nguyễn Minh Anh', email: 'minhanh@example.com', role }));
    }, role);
    await context.route('**/api/**', route => route.fulfill({ json: { success: true, message: 'UI audit fixture', data: apiData(new URL(route.request().url())) } }));
    // SignalR is intentionally unavailable in this isolated layout test.
    await context.route('**/hubs/**', route => route.fulfill({ status: 503, body: 'No live backend in UI audit' }));
    const page = await context.newPage();
    const errors = [];
    const consoleErrors = [];
    const consoleWarnings = [];
    const networkFailures = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
    page.on('console', message => { if (message.type() === 'warning') consoleWarnings.push(message.text()); });
    page.on('requestfailed', request => networkFailures.push({ url: request.url(), error: request.failure()?.errorText, mapTile: /https:\/\/[^/]*tile[^/]*\//.test(request.url()) }));
    const routeList = process.argv.includes('--interactions-only') ? [] : process.argv.includes('--visual-only') ? routes.filter(route => screenshotRoutes.includes(route)) : routes;
    for (const route of routeList) {
      for (const width of widths) {
        await page.setViewportSize({ width, height: 900 });
        await page.goto(baseURL + route, { waitUntil: 'domcontentloaded' });
        await page.locator('main').first().waitFor({ timeout: 10000 }).catch(() => {});
        await page.waitForTimeout(350);
        const measurement = await page.evaluate(() => {
          const width = document.documentElement.clientWidth;
          return {
            pageWidth: document.documentElement.scrollWidth,
            viewport: width,
            heading: document.querySelector('h1')?.textContent,
            overflows: [...document.querySelectorAll('main,form,input,select,textarea,table,article,[class*="grid"],[class*="heading"]')].filter(el => {
              const r = el.getBoundingClientRect();
              return r.width > 0 && (r.right > width + 1 || r.left < -1) && !el.closest('[class*="table-wrap"],.overflow-x-auto');
            }).slice(0, 10).map(el => ({ tag: el.tagName, class: el.className, text: el.textContent?.slice(0, 50) })),
            utilityPadding: [...document.querySelectorAll('.p-6')].map(el => getComputedStyle(el).padding).slice(0, 3),
            brokenImages: [...document.images].filter(i => i.complete && !i.naturalWidth).map(i => i.src),
          };
        });
        results.push({ role, route, width, ...measurement });
        if (measurement.pageWidth > width + 1 || measurement.overflows.length || measurement.heading === 'Không thể hiển thị trang này') issues.push({ role, route, width, ...measurement });
        if ((width === 1440 || width === 375) && screenshotRoutes.includes(route)) {
          await page.locator('img').evaluateAll(images => images.forEach(image => { image.loading = 'eager'; }));
          await page.waitForFunction(() => [...document.images].every(image => image.complete), null, { timeout: 15000 }).catch(() => {});
          results.push({ role, route, width, imageCheck: await page.locator('img').evaluateAll(images => images.map(image => ({ src: image.currentSrc || image.src, loaded: image.complete && image.naturalWidth > 0 }))) });
          await page.screenshot({ path: path.join(output, `${role}-${route.replaceAll('/', '_') || 'home'}-${width}.png`), fullPage: true });
        }
      }
      console.log(`${role} ${route}: checked 5 widths`);
    }
    for (const width of process.argv.includes('--visual-only') ? [] : widths) {
      try { await checkInteractions(page, role, width); interactions.push({ role, width, passed: true }); }
      catch (error) { interactions.push({ role, width, passed: false, error: error.message }); console.log(`${role} ${width}: ${error.message}`); }
    }
    const expectedConsole = consoleErrors.filter(message => /503|No live backend in UI audit/.test(message));
    const expectedOfflineTileErrors = consoleErrors.filter(message => /net::ERR_FAILED|net::ERR_NAME_NOT_RESOLVED/.test(message));
    results.push({ role, pageErrors: [...new Set(errors)], consoleErrors: [...new Set(consoleErrors.filter(message => !expectedConsole.includes(message) && !expectedOfflineTileErrors.includes(message) && !/status of 400/.test(message)))], consoleWarnings: [...new Set(consoleWarnings)], expectedSignalRErrors: expectedConsole.length, expectedOfflineTileErrors: expectedOfflineTileErrors.length, networkFailures });
    await context.close();
  }
} finally {
  await browser.close();
}
await writeFile(path.join(output, 'results.json'), JSON.stringify({ fixtureMode: true, widths, results, issues, interactions }, null, 2));
console.log(JSON.stringify({ interactions }, null, 2));
if (issues.length || interactions.some(result => !result.passed) || results.some(result => result.pageErrors?.length || result.consoleErrors?.length)) process.exitCode = 1;
console.log(JSON.stringify({ checks: results.filter(r => r.viewport).length, issues: issues.map(r => ({ role: r.role, route: r.route, width: r.width, pageWidth: r.pageWidth, overflows: r.overflows })) }, null, 2));
