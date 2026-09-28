# Test Location API

## 1. Test Backend Endpoint Trực Tiếp

### PowerShell/CMD:
```powershell
curl "http://localhost:5078/api/location/search?q=42%20Đường%20Nguyễn%20Lân,%20Phố%20Nối,%20Mỹ%20Hào,%20Hưng%20Yên"
```

### Browser:
Mở trình duyệt và truy cập:
```
http://localhost:5078/api/location/search?q=Mỹ Hào, Hưng Yên
```

### Expected Response:
```json
{
  "success": true,
  "message": "Tìm kiếm thành công",
  "data": [
    {
      "displayName": "Mỹ Hào, Hưng Yên, Vietnam",
      "latitude": 20.943199999999998,
      "longitude": 106.1032
    }
  ]
}
```

---

## 2. Test Các Địa Chỉ Khác

### Hà Nội:
```
http://localhost:5078/api/location/search?q=Hà Nội
```

### TP.HCM:
```
http://localhost:5078/api/location/search?q=Thành phố Hồ Chí Minh
```

### Địa chỉ cụ thể:
```
http://localhost:5078/api/location/search?q=144 Xuân Thủy, Cầu Giấy, Hà Nội
```

---

## 3. Test Nominatim Trực Tiếp (Từ Backend Server)

Để xác nhận backend có truy cập Internet:

```bash
curl "https://nominatim.openstreetmap.org/search?format=json&q=Hanoi&limit=1&countrycodes=vn"
```

### Expected Response:
```json
[
  {
    "lat": "21.0244",
    "lon": "105.8412",
    "display_name": "Hà Nội, Vietnam"
  }
]
```

**Nếu command này FAIL** → Backend không có Internet hoặc bị block Nominatim.

---

## 4. Test Từ Frontend (DevTools Console)

Mở Console trong browser tại trang frontend:

```javascript
fetch('http://localhost:5078/api/location/search?q=Hà Nội')
  .then(res => res.json())
  .then(data => console.log(data))
  .catch(err => console.error(err));
```

---

## 5. Kiểm Tra Logs Backend

Khi gọi API, backend sẽ log:

```
info: RoomRental.BackEnd.Controllers.LocationController[0]
      Calling Nominatim: https://nominatim.openstreetmap.org/search?format=json&q=Mỹ+Hào&limit=5&countrycodes=vn
info: RoomRental.BackEnd.Controllers.LocationController[0]
      Found 5 results
```

---

## 6. Các Lỗi Có Thể Gặp

### Lỗi 1: CORS
```
Access to fetch at 'http://localhost:5078/api/location/search' from origin 'http://localhost:5173' has been blocked by CORS policy
```

**Giải pháp:** Đã thêm CORS trong `Program.cs`, restart backend.

---

### Lỗi 2: Backend Không Chạy
```
Failed to fetch
net::ERR_CONNECTION_REFUSED
```

**Giải pháp:**
```bash
cd backend/RoomRental.BackEnd
dotnet run
```

---

### Lỗi 3: Backend Không Truy Cập Được Nominatim
```json
{
  "success": false,
  "message": "Không thể kết nối đến dịch vụ bản đồ. Vui lòng thử lại sau."
}
```

**Nguyên nhân:**
- Backend server không có Internet
- Firewall block Nominatim
- DNS không resolve được `nominatim.openstreetmap.org`

**Kiểm tra:**
```bash
# Từ máy backend server
ping nominatim.openstreetmap.org
curl https://nominatim.openstreetmap.org/search?format=json&q=test
```

---

### Lỗi 4: Không Tìm Thấy Kết Quả
```json
{
  "success": true,
  "message": "Không tìm thấy kết quả",
  "data": []
}
```

**Nguyên nhân:**
- Địa chỉ không tồn tại
- Query string sai format
- countrycodes=vn limit quá hẹp (có thể bỏ để test)

---

## 7. Test Flow Hoàn Chỉnh

1. **Backend running:**
   ```
   ✅ http://localhost:5078
   ```

2. **Frontend running:**
   ```
   ✅ http://localhost:5173
   ```

3. **Mở trang Create Post:**
   ```
   http://localhost:5173/landlord/create-post
   ```

4. **Tìm kiếm địa chỉ:**
   - Nhập: `Mỹ Hào, Hưng Yên`
   - Bấm 🔍 Tìm

5. **Kiểm tra Network tab:**
   ```
   Request: GET /api/location/search?q=Mỹ+Hào,+Hưng+Yên
   Status: 200 OK
   Response: { success: true, data: [...] }
   ```

6. **Kiểm tra Map:**
   - Map bay đến Mỹ Hào
   - Marker hiện tại vị trí
   - Latitude/Longitude hiển thị

---

## ✅ Checklist

- [ ] Backend API trả về 200 OK
- [ ] Response có `success: true`
- [ ] Response có `data` array
- [ ] Mỗi item trong data có `latitude`, `longitude`, `displayName`
- [ ] Frontend nhận được response
- [ ] Map di chuyển đến vị trí đúng
- [ ] Marker hiển thị
- [ ] Latitude/Longitude cập nhật
- [ ] Không có lỗi CORS
- [ ] Không có lỗi trong Console
