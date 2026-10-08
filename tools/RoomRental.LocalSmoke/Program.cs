using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Helpers;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.Models.Enums;

if (!args.Contains("--allow-local-test-writes"))
    throw new InvalidOperationException("Requires --allow-local-test-writes; creates dedicated QA data in the configured local database.");
var root = Path.GetFullPath(args.FirstOrDefault(a => a.StartsWith("--root="))?[7..] ?? ".");
var backend = Path.Combine(root, "backend/RoomRental.BackEnd");
var config = new ConfigurationBuilder().SetBasePath(backend).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Local.json").Build();
var connection = config.GetConnectionString("DefaultConnection")!;
var sql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
if (sql.InitialCatalog != "RoomRentalDB" || sql.DataSource.Split('\\')[0].ToLowerInvariant() is not ("duysonw" or "localhost" or "." or "(local)"))
    throw new InvalidOperationException("Local QA only: unexpected SQL instance/database.");
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
var manifestPath = Path.Combine(Path.GetTempPath(), "room-rental-local-smoke-auth.json");
var resultPath = Path.Combine(root, "docs/ui-audit/local-workflow/api-results.json");
Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
if (args.Contains("--finish"))
{
    var saved = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath)).RootElement;
    var prefix = saved.GetProperty("prefix").GetString()!;
    foreach (var role in saved.GetProperty("roles").EnumerateObject())
    {
        var id = role.Value.GetProperty("userId").GetInt32();
        var user = await db.Users.FindAsync(id) ?? throw new Exception("Missing QA user");
        if (user.Email != $"{prefix}-{role.Name}@example.test") throw new Exception("Not the expected QA account");
        user.IsActive = false;
    }
    foreach (var id in saved.GetProperty("postIds").EnumerateArray().Select(x => x.GetInt32()))
    {
        var post = await db.Posts.Include(p => p.Landlord).ThenInclude(l => l.Account).SingleAsync(p => p.Id == id);
        if (post.Landlord.Account.Email != $"{prefix}-landlord@example.test") throw new Exception("Not a QA post");
        post.Status = PostStatus.Hidden;
    }
    await db.SaveChangesAsync();
    File.Delete(manifestPath);
    Console.WriteLine("QA accounts disabled, QA posts hidden, all business history preserved; private auth manifest removed.");
    return;
}
var prefixRun = "qa" + DateTime.Now.ToString("yyyyMMddHHmmss");
var password = "Qa!" + Guid.NewGuid().ToString("N");
var paymentSecret = Guid.NewGuid().ToString("N");
var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
var api = $"http://127.0.0.1:{port}";
var start = new ProcessStartInfo("dotnet", Path.Combine(backend, "bin/Debug/net8.0/RoomRental.BackEnd.dll"))
{
    WorkingDirectory = backend, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
};
start.Environment["ASPNETCORE_URLS"] = api;
start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
start.Environment["RentalLifecycle__Enabled"] = "false";
start.Environment["VnPay__TmnCode"] = "LOCAL_TEST_ONLY";
start.Environment["VnPay__HashSecret"] = paymentSecret;
start.Environment["VnPay__BaseUrl"] = "https://local-test.invalid/pay";
start.Environment["VnPay__ReturnUrl"] = api + "/api/thanh-toan/vnpay-return";
var checks = new List<object>();
var handedOff = false;
var roles = new Dictionary<string, JsonElement>();
var ids = new List<int>();
using var host = Process.Start(start) ?? throw new Exception("Cannot start QA host");
var output = host.StandardOutput.ReadToEndAsync(); var error = host.StandardError.ReadToEndAsync();
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(api), Timeout = TimeSpan.FromSeconds(30) };
try
{
    for (var i = 0; ; i++)
    {
        try { if ((await client.GetAsync("/swagger/index.html")).IsSuccessStatusCode) break; } catch (HttpRequestException) { }
        if (i >= 30 || host.HasExited) throw new Exception("QA backend startup failed");
        await Task.Delay(500);
    }
    for (var i = 0; i < 2; i++)
    {
        var role = i == 0 ? "landlord" : "tenant";
        var phone = "09" + (DateTime.Now.Ticks % 100_000_000).ToString("D8");
        roles[role] = await Call("POST", "/api/xac-thuc/dang-ky", new { fullName = $"QA {role} {prefixRun}", email = $"{prefixRun}-{role}@example.test", phone, password, confirmPassword = password, roleName = i == 0 ? "Landlord" : "Tenant" });
    }
    // Dedicated bootstrap account, never resets an existing administrator's credentials.
    var administrator = new User { Email = $"{prefixRun}-admin@example.test", UserName = $"{prefixRun}-admin@example.test", FullName = "QA Admin " + prefixRun, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), RoleId = 0, IsActive = true };
    db.Users.Add(administrator); await db.SaveChangesAsync();
    // RoleId=0 is EF's default-value sentinel; explicitly provision only this new QA admin.
    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.TaiKhoan SET VaiTro = 0 WHERE Id = {administrator.Id} AND Email = {administrator.Email}");
    roles["admin"] = await Call("POST", "/api/xac-thuc/dang-nhap", new { email = administrator.Email, password });
    var category = await db.RoomCategories.OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();
    var room = await Call("POST", "/api/phong", new { roomName = "QA phong thuc " + prefixRun, price = 3_000_000, area = 26, maxOccupants = 2, address = "42 Nguyen Lan, Thanh Xuan, Ha Noi", province = "Hà Nội", district = "Thanh Xuân", ward = "Phương Mai", latitude = 21.0042, longitude = 105.8216, categoryId = category, electricityPrice = 3500, waterPrice = 12000, serviceFee = 100000, imageUrls = new[] { "/room-placeholder.svg" } }, "landlord");
    var roomId = Id(room);
    var roomCount = await db.Rooms.CountAsync();
    var post = await Call("POST", "/api/bai-dang", new { roomId, title = "QA phòng có cửa sổ " + prefixRun, description = "Dữ liệu QA trên database local. Điện 3500đ/kWh, nước 12000đ/m³, có bếp và cửa sổ." }, "landlord");
    var postId = Id(post); ids.Add(postId);
    Check(await db.Rooms.CountAsync() == roomCount, "Create Post never creates another Room");
    await Call("PUT", $"/api/quan-tri/bai-dang/{postId}/duyet", null, "admin");
    Check(await Found(postId), "Tenant filter finds Approved + Available post");
    Check((await Call("GET", $"/api/bai-dang/{postId}")).GetProperty("roomId").GetInt32() == roomId, "Detail resolves physical room");
    var oldRequest = await Call("POST", "/api/yeu-cau-thue", new { baiDangId = postId, ghiChu = "Ghi chú gốc cần được giữ nguyên" }, "tenant");
    await Call("PUT", $"/api/phong/{roomId}", new { price = 3_250_000 }, "landlord");
    var pending = await Call("GET", $"/api/bai-dang/cua-toi/{postId}", null, "landlord");
    Check(pending.GetProperty("status").GetInt32() == 0 && !await Found(postId), "Price update: Post Pending and removed from search");
    var old = (await Call("GET", "/api/yeu-cau-thue/cua-toi", null, "tenant")).EnumerateArray().Single(x => Id(x) == Id(oldRequest));
    Check(old.GetProperty("trangThai").GetInt32() == 3 && old.GetProperty("lyDoHuy").GetString()!.Length > 0 && old.GetProperty("ghiChu").GetString() == "Ghi chú gốc cần được giữ nguyên", "Old Pending request cancelled with separate reason and original note preserved");
    await Call("PUT", $"/api/quan-tri/bai-dang/{postId}/duyet", null, "admin");
    var appointment = await Call("POST", "/api/lich-hen-xem-phong", new { postId, scheduledAt = DateTime.Now.AddHours(1), tenantNote = "QA xem phòng" }, "tenant");
    await Call("PUT", $"/api/lich-hen-xem-phong/{Id(appointment)}/xac-nhan", null, "landlord");
    Check(true, "Appointment created and confirmed via real API");
    var request = await Call("POST", "/api/yeu-cau-thue", new { baiDangId = postId, ghiChu = "Đã xem và chấp nhận giá 3.250.000đ/tháng" }, "tenant");
    var requestId = Id(request);
    await Call("PUT", $"/api/yeu-cau-thue/{requestId}/trang-thai", new { trangThai = 1, soTienDatCoc = 1_000_000, hanThanhToanCoc = DateTime.Now.AddDays(1) }, "landlord");
    var reserved = await Call("GET", $"/api/phong/{roomId}", null, "landlord");
    Check(reserved.GetProperty("status").GetInt32() == 2 && !await Found(postId), "Approval reserves room and excludes it from search");
    var deposits = await Call("GET", "/api/dat-coc/cua-toi", null, "tenant");
    var deposit = deposits.EnumerateArray().Single(x => x.GetProperty("yeuCauThueId").GetInt32() == requestId);
    Check(deposit.GetProperty("soTien").GetDecimal() == 1_000_000, "Exactly one deposit automatically created with landlord amount");
    await Call("PUT", $"/api/phong/{roomId}", new { price = 9_000_000 }, "landlord", expected: 409);
    Check(true, "Approved request prevents silent price changes before contract");
    var cancelledAppointment = await Call("GET", $"/api/lich-hen-xem-phong/{Id(appointment)}", null, "tenant");
    Check(cancelledAppointment.GetProperty("status").GetInt32() == (int)AppointmentStatus.Cancelled, "Reservation cancels future appointment");
    await Pay(new { depositId = Id(deposit) }, "deposit");
    var confirmed = await Call("PUT", $"/api/dat-coc/{Id(deposit)}/trang-thai", new { trangThai = 2 }, "landlord");
    Check(confirmed.GetProperty("trangThai").GetInt32() == 2, "Landlord confirms paid deposit");
    var contract = await Call("POST", "/api/hop-dong", new { yeuCauThueId = requestId, ngayBatDau = DateTime.Now.AddMinutes(-1), ngayKetThuc = DateTime.Now.AddMonths(2), tienThueHangThang = 3_250_000, dieuKhoan = "LOCAL TEST ONLY: callback mô phỏng có chữ ký, không có giao dịch ngân hàng." }, "landlord");
    var contractId = Id(contract);
    await Call("PUT", $"/api/hop-dong/{contractId}/xac-nhan", null, "landlord");
    var signed = await Call("PUT", $"/api/hop-dong/{contractId}/xac-nhan", null, "tenant");
    var rented = await Call("GET", $"/api/phong/{roomId}", null, "landlord");
    Check(signed.GetProperty("trangThai").GetInt32() == 1 && rented.GetProperty("status").GetInt32() == 1, "Both signatures activate contract and mark room Rented");
    var bill = await Call("POST", "/api/hoa-don", new { hopDongId = contractId, thang = DateTime.Now.Month, nam = DateTime.Now.Year, soDienCu = 100, soDienMoi = 110, soNuocCu = 20, soNuocMoi = 23, hanThanhToan = DateTime.Now.AddDays(7) }, "landlord");
    await Pay(new { monthlyBillId = Id(bill) }, "bill");
    var paidBill = await Call("GET", $"/api/hoa-don/{Id(bill)}", null, "tenant");
    Check(paidBill.GetProperty("trangThai").GetInt32() == 1, "Monthly bill paid via signed local test callback");
    await Call("PUT", $"/api/hop-dong/{contractId}/cham-dut", new { lyDo = "Kết thúc luồng QA local, không thuê thật" }, "landlord");
    var released = await Call("GET", $"/api/phong/{roomId}", null, "landlord");
    Check(released.GetProperty("status").GetInt32() == 0 && await Found(postId), "Termination releases Room Available and returns it to search");
    await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(new { prefix = prefixRun, roles, postIds = ids, roomId, postId, contractId, billId = Id(bill), requestId }, jsonOptions));
    await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(new { prefix = prefixRun, database = sql.InitialCatalog, paymentMode = "signed-local-test-callback-NOT-bank-sandbox", roomId, postId, requestId, oldRequestId = Id(oldRequest), depositId = Id(deposit), contractId, billId = Id(bill), appointmentId = Id(appointment), checks }, jsonOptions));
    handedOff = true;
    Console.WriteLine($"PASS {checks.Count} local API checks. Room={roomId}, Post={postId}, Request={requestId}, Deposit={Id(deposit)}, Contract={contractId}, Bill={Id(bill)}. Auth kept only in OS temp for browser verification; run --finish afterward.");
}
finally
{
    if (!host.HasExited) host.Kill(entireProcessTree: true);
    await host.WaitForExitAsync(); await output; await error;
    if (!handedOff)
    {
        db.ChangeTracker.Clear();
        var emails = new[] { $"{prefixRun}-landlord@example.test", $"{prefixRun}-tenant@example.test", $"{prefixRun}-admin@example.test" };
        foreach (var user in await db.Users.Where(u => emails.Contains(u.Email!)).ToListAsync()) user.IsActive = false;
        foreach (var post in await db.Posts.Where(p => p.Landlord.Account.Email == emails[0]).ToListAsync()) post.Status = PostStatus.Hidden;
        await db.SaveChangesAsync();
    }
}

int Id(JsonElement value) => value.GetProperty("id").GetInt32();
void Check(bool ok, string name)
{
    if (!ok) throw new Exception("FAILED: " + name);
    checks.Add(new { name, status = "PASS" }); Console.WriteLine("PASS " + name);
}
async Task<JsonElement> Call(string method, string path, object? body = null, string? role = null, int expected = 200)
{
    using var message = new HttpRequestMessage(new HttpMethod(method), path);
    if (body != null) message.Content = JsonContent.Create(body);
    if (role != null) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", roles[role].GetProperty("token").GetString());
    using var response = await client.SendAsync(message);
    var text = await response.Content.ReadAsStringAsync();
    if ((int)response.StatusCode != expected) throw new Exception($"{method} {path}: {(int)response.StatusCode}; {text}");
    var parsed = JsonDocument.Parse(text).RootElement;
    return expected == 200 && parsed.TryGetProperty("data", out var data) ? data.Clone() : parsed.Clone();
}
async Task<bool> Found(int postId)
{
    var data = await Call("GET", "/api/bai-dang/search?keyword=" + Uri.EscapeDataString(prefixRun) + "&province=" + Uri.EscapeDataString("Hà Nội") + "&minPrice=3000000&maxPrice=4000000&categoryId=" + (await db.RoomCategories.OrderBy(c => c.Id).Select(c => c.Id).FirstAsync()) + "&pageSize=100");
    return data.GetProperty("items").EnumerateArray().Any(p => Id(p) == postId);
}
async Task Pay(object target, string name)
{
    var attempt = await Call("POST", "/api/thanh-toan/vnpay-tao-url", target, "tenant");
    var signer = new VnPayHelper();
    signer.AddRequestData("vnp_TxnRef", attempt.GetProperty("orderId").GetString()!);
    signer.AddRequestData("vnp_Amount", ((long)(attempt.GetProperty("amount").GetDecimal() * 100)).ToString());
    signer.AddRequestData("vnp_ResponseCode", "00"); signer.AddRequestData("vnp_TransactionStatus", "00");
    signer.AddRequestData("vnp_TransactionNo", "LOCAL_TEST_" + prefixRun + "_" + name);
    var callback = signer.CreateRequestUrl(api + "/api/thanh-toan/vnpay-ipn", paymentSecret);
    var ipn = await Call("GET", callback);
    Check(ipn.GetProperty("rspCode").GetString() == "00", name + " payment signed test IPN accepted");
    Check((await Call("GET", callback)).GetProperty("rspCode").GetString() == "00", name + " duplicate IPN idempotent");
}
