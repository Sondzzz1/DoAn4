# Phác thảo biểu đồ lớp tổng quan

## 1. Phạm vi vẽ

Biểu đồ tổng quan nên đặt 18 Model nghiệp vụ sau. Đây là các class có vai trò trực tiếp trong tìm phòng, đăng bài, thuê phòng, thanh toán và tương tác. Các thuộc tính navigation không cần ghi trong ô class vì đã được biểu diễn qua đường quan hệ bên dưới.

Các class còn lại có trong source nhưng nên đưa sang sơ đồ phụ để tổng quan không quá rối: `Amenity`, `PostAmenity`, `Incident`, `BlogPost`, `BlogComment`, `Role`.

`Role` không phải quan hệ phân quyền chính: `User.RoleId` được dùng trực tiếp với giá trị `0 = Admin`, `1 = Tenant`, `2 = Landlord`. Vì vậy không vẽ kế thừa giữa `User`, `TenantProfile` và `LandlordProfile`.

## 2. Danh sách class để đặt vào biểu đồ

### TÊN LỚP: `User`

**THUỘC TÍNH:**

```text
+ Id : int
+ UserName : string
+ PasswordHash : string
+ FullName : string
+ Phone : string?
+ Email : string?
+ AvatarUrl : string?
+ RoleId : int
+ IsActive : bool
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
+ IsBlocked : bool <<NotMapped>>
+ RoleName : string <<NotMapped>>
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/User.cs`.

### TÊN LỚP: `TenantProfile`

**THUỘC TÍNH:**

```text
+ Id : int
+ AccountId : int
+ BirthDate : DateOnly?
+ Gender : string?
+ Occupation : string?
+ CurrentAddress : string?
+ Introduction : string?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/TenantProfile.cs`.

### TÊN LỚP: `LandlordProfile`

**THUỘC TÍNH:**

```text
+ Id : int
+ AccountId : int
+ Introduction : string?
+ Address : string?
+ CitizenId : string?
+ IsVerified : bool
+ VerifiedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/LandlordProfile.cs`.

### TÊN LỚP: `RoomCategory`

**THUỘC TÍNH:**

```text
+ Id : int
+ Name : string
+ Description : string?
+ ImageUrl : string?
+ IsActive : bool
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/RoomCategory.cs`.

### TÊN LỚP: `Room`

**THUỘC TÍNH:**

```text
+ Id : int
+ LandlordId : int
+ CategoryId : int
+ RoomName : string
+ Description : string?
+ Price : decimal
+ Area : decimal
+ MaxOccupants : int
+ CurrentOccupants : int
+ Bedrooms : int?
+ Bathrooms : int?
+ Floor : int?
+ Address : string
+ Ward : string?
+ District : string?
+ Province : string?
+ Latitude : decimal?
+ Longitude : decimal?
+ ElectricityPrice : decimal?
+ WaterPrice : decimal?
+ ServiceFee : decimal?
+ Status : RoomStatus
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/Room.cs`.

### TÊN LỚP: `Post`

**THUỘC TÍNH:**

```text
+ Id : int
+ RoomId : int
+ LandlordId : int
+ Title : string
+ Content : string
+ DisplayPrice : decimal
+ Status : PostStatus
+ RejectionReason : string?
+ ViewCount : int
+ PostedAt : DateTime?
+ ExpiredAt : DateTime?
+ ApprovedAt : DateTime?
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/Post.cs`.

### TÊN LỚP: `PostImage`

**THUỘC TÍNH:**

```text
+ Id : int
+ RoomId : int
+ ImageUrl : string
+ IsThumbnail : bool
+ DisplayOrder : int
+ CreatedAt : DateTime
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/PostImage.cs`.

### TÊN LỚP: `ViewingAppointment`

**THUỘC TÍNH:**

```text
+ Id : int
+ TenantId : int
+ PostId : int
+ LandlordId : int
+ ScheduledAt : DateTime
+ Status : AppointmentStatus
+ TenantNote : string?
+ LandlordResponse : string?
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/ViewingAppointment.cs`.

### TÊN LỚP: `RentalRequest`

**THUỘC TÍNH:**

```text
+ Id : int
+ PostId : int
+ TenantAccountId : int
+ LandlordAccountId : int
+ Note : string?
+ Status : int
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/RentalFeatures.cs`.

### TÊN LỚP: `Deposit`

**THUỘC TÍNH:**

```text
+ Id : int
+ RentalRequestId : int
+ TenantAccountId : int
+ LandlordAccountId : int
+ Amount : decimal
+ Status : int
+ PaidAt : DateTime?
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/RentalFeatures.cs`.

### TÊN LỚP: `RentalContract`

**THUỘC TÍNH:**

```text
+ Id : int
+ RentalRequestId : int
+ PostId : int
+ TenantAccountId : int
+ LandlordAccountId : int
+ StartDate : DateTime
+ EndDate : DateTime
+ MonthlyRent : decimal
+ DepositAmount : decimal
+ ElectricityPrice : decimal
+ WaterPrice : decimal
+ ServiceFee : decimal
+ Terms : string?
+ Status : int
+ TenantConfirmed : bool
+ LandlordConfirmed : bool
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/RentalFeatures.cs`.

### TÊN LỚP: `MonthlyBill`

**THUỘC TÍNH:**

```text
+ Id : int
+ ContractId : int
+ Month : int
+ Year : int
+ OldElectricity : decimal
+ NewElectricity : decimal
+ ElectricityPrice : decimal
+ OldWater : decimal
+ NewWater : decimal
+ WaterPrice : decimal
+ RoomPrice : decimal
+ ServiceFee : decimal
+ OtherFees : decimal
+ OtherFeesNote : string?
+ TotalAmount : decimal
+ Status : int
+ DueDate : DateTime?
+ PaidAt : DateTime?
+ PaymentMethod : string?
+ Note : string?
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/MonthlyBill.cs`.

### TÊN LỚP: `PaymentTransaction`

**THUỘC TÍNH:**

```text
+ Id : int
+ DepositId : int?
+ MonthlyBillId : int?
+ TargetType : string
+ OrderId : string
+ TransactionCode : string?
+ Amount : decimal
+ Status : int
+ ResponseCode : string?
+ OrderInfo : string?
+ CreatedAt : DateTime
+ ProcessedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/PaymentTransaction.cs`.

### TÊN LỚP: `Favorite`

**THUỘC TÍNH:**

```text
+ Id : int
+ TenantId : int
+ PostId : int
+ CreatedAt : DateTime
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/Favorite.cs`.

### TÊN LỚP: `RoomReview`

**THUỘC TÍNH:**

```text
+ Id : int
+ ContractId : int
+ PostId : int
+ TenantAccountId : int
+ Rating : int
+ Comment : string?
+ CreatedAt : DateTime
+ UpdatedAt : DateTime?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/RentalFeatures.cs`.

### TÊN LỚP: `Report`

**THUỘC TÍNH:**

```text
+ Id : int
+ PostId : int
+ ReporterAccountId : int
+ Reason : string
+ Description : string?
+ Status : int
+ CreatedAt : DateTime
+ ResolvedAt : DateTime?
+ ResolvedByAccountId : int?
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/Report.cs`.

### TÊN LỚP: `ChatMessage`

**THUỘC TÍNH:**

```text
+ Id : int
+ SenderId : int
+ ReceiverId : int
+ PostId : int?
+ Message : string
+ IsRead : bool
+ CreatedAt : DateTime
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/ChatMessage.cs`.

### TÊN LỚP: `Notification`

**THUỘC TÍNH:**

```text
+ Id : int
+ AccountId : int
+ Title : string
+ Content : string
+ Type : int?
+ Link : string?
+ IsRead : bool
+ CreatedAt : DateTime
```

**PHƯƠNG THỨC:** Không có phương thức trong class.

**Nguồn:** `backend/RoomRental.BackEnd/Models/Notification.cs`.

## 3. Các class còn lại để vẽ ở sơ đồ phụ

| Class thực tế | Dùng trong nhóm nào | Căn cứ source |
|---|---|---|
| `Amenity` | Phòng và bài đăng | `Models/Amenity.cs` |
| `PostAmenity` | Class liên kết Room - Amenity | `Models/PostAmenity.cs`, `Configurations/PostAmenityConfiguration.cs` |
| `Incident` | Sự cố hợp đồng | `Models/RentalFeatures.cs` |
| `BlogPost`, `BlogComment` | Blog phụ trợ | `Models/BlogPost.cs`, `Models/BlogComment.cs` |
| `Role` | Helper cho mã cũ, không ghép với User | `Models/Role.cs`, `Configurations/RoleConfiguration.cs` |

## 4. Bảng quan hệ tổng hợp

| Lớp A | Lớp B | Quan hệ UML | Multiplicity đầu A | Multiplicity đầu B | Căn cứ source |
|---|---|---:|---:|---:|---|
| `User` | `TenantProfile` | Association | 1 | 0..1 | `UserConfiguration`: `HasOne(u => u.TenantProfile).WithOne(...).HasForeignKey<TenantProfile>(...)`. |
| `User` | `LandlordProfile` | Association | 1 | 0..1 | `UserConfiguration`: `HasOne(u => u.LandlordProfile).WithOne(...).HasForeignKey<LandlordProfile>(...)`. |
| `LandlordProfile` | `Room` | Association | 1 | 0..* | `RoomConfiguration`: `Room.Landlord` - `LandlordProfile.Rooms`. |
| `RoomCategory` | `Room` | Association | 1 | 0..* | `RoomConfiguration`: `Room.Category` - `RoomCategory.Rooms`. |
| `LandlordProfile` | `Post` | Association | 1 | 0..* | `PostConfiguration`: `Post.Landlord` - `LandlordProfile.Posts`. |
| `Room` | `Post` | Association | 1 | 0..* | `PostConfiguration`: `Post.Room` - `Room.Posts`. |
| `Room` | `PostImage` | Association | 1 | 0..* | `PostImageConfiguration`: `PostImage.Room`, delete behavior `Cascade`; vẫn vẽ Association để không suy ra composition chỉ từ FK. |
| `Room` | `PostAmenity` | Association | 1 | 0..* | `PostAmenityConfiguration`: `PostAmenity.Room` - `Room.RoomAmenities`. |
| `Amenity` | `PostAmenity` | Association | 1 | 0..* | `PostAmenityConfiguration`: `PostAmenity.Amenity` - `Amenity.RoomAmenities`. |
| `TenantProfile` | `Favorite` | Association | 1 | 0..* | `FavoriteConfiguration`: `Favorite.Tenant` - `TenantProfile.Favorites`. |
| `Post` | `Favorite` | Association | 1 | 0..* | `FavoriteConfiguration`: `Favorite.Post` - `Post.Favorites`. |
| `TenantProfile` | `ViewingAppointment` | Association | 1 | 0..* | `ViewingAppointmentConfiguration`: `ViewingAppointment.Tenant` - `TenantProfile.Appointments`. |
| `LandlordProfile` | `ViewingAppointment` | Association | 1 | 0..* | `ViewingAppointmentConfiguration`: `ViewingAppointment.Landlord` - `LandlordProfile.Appointments`. |
| `Post` | `ViewingAppointment` | Association | 1 | 0..* | `ViewingAppointmentConfiguration`: `ViewingAppointment.Post` - `Post.Appointments`. |
| `Post` | `RentalRequest` | Association | 1 | 0..* | `RentalRequestConfiguration`: `HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId)`. |
| `RentalRequest` | `Deposit` | Dependency (tham chiếu nghiệp vụ) | 1 | 0..1 | `Deposit.RentalRequestId`; index unique trong `DepositConfiguration`; không có navigation/FK EF trực tiếp. |
| `RentalRequest` | `RentalContract` | Dependency (tham chiếu nghiệp vụ) | 1 | 0..1 | `RentalContract.RentalRequestId`; `RentalContractBLL.TaoAsync` kiểm tra chưa có hợp đồng cho request. |
| `Post` | `RentalContract` | Association | 1 | 0..* | `RentalContract.PostId`; `Phase2_P0_WorkflowSafety.sql` tạo FK `HopDongThue.PostId -> TinDang.Id`. |
| `RentalContract` | `MonthlyBill` | Association | 1 | 0..* | `MonthlyBillConfiguration`: `MonthlyBill.Contract` - `RentalContract.MonthlyBills`. |
| `Deposit` | `PaymentTransaction` | Association | 1 | 0..* | `PaymentTransactionConfiguration`: FK `DepositId`, `DeleteBehavior.Restrict`. |
| `MonthlyBill` | `PaymentTransaction` | Association | 1 | 0..* | `PaymentTransactionConfiguration`: FK `MonthlyBillId`, `DeleteBehavior.Restrict`. |
| `RentalContract` | `RoomReview` | Dependency (tham chiếu nghiệp vụ) | 1 | 0..* | `RoomReview.ContractId`; `RoomReviewBLL.TaoAsync` kiểm tra hợp đồng. |
| `Post` | `RoomReview` | Dependency (tham chiếu nghiệp vụ) | 1 | 0..* | `RoomReview.PostId`; được gán từ `RentalContract.PostId`. |
| `Post` | `Report` | Association | 1 | 0..* | `ReportConfiguration`: `Report.Post` - `Post.Reports`. |
| `User` | `Report` | Association (hai vai trò) | 1 | 0..* | `ReportConfiguration`: FK `ReporterAccountId` và FK nullable `ResolvedByAccountId`. |
| `User` | `Notification` | Association | 1 | 0..* | `NotificationConfiguration`: `Notification.Account`; FK `AccountId`. |
| `User` | `ChatMessage` | Association (Sender) | 1 | 0..* | `ChatMessageConfiguration`: FK `SenderId`. |
| `User` | `ChatMessage` | Association (Receiver) | 1 | 0..* | `ChatMessageConfiguration`: FK `ReceiverId`. |
| `Post` | `ChatMessage` | Association | 0..1 | 0..* | `ChatMessageConfiguration`: FK nullable `PostId`, `DeleteBehavior.SetNull`. |

**Quy tắc đặc biệt của `PaymentTransaction`:** `PaymentTransactionConfiguration` và `Phase2_P0_WorkflowSafety.sql` có check constraint bắt buộc đúng một mục tiêu: `DepositId` hoặc `MonthlyBillId`, tương ứng `TargetType`. Khi vẽ, gắn ghi chú `XOR: Deposit | MonthlyBill` cạnh class `PaymentTransaction`.

## 5. Bố cục vẽ đề xuất

Đặt biểu đồ theo chiều ngang. Không cần vẽ `BLL`, `Controller` hoặc `ApplicationDbContext` trong Class Diagram này.

```text
HÀNG TRÊN:
[TenantProfile]       [User]       [LandlordProfile]
       \                 |                 /
        \                |                /

HÀNG GIỮA TRÁI:
[RoomCategory] --- [Room] --- [Post] --- [Report]
                     |         |  \
                [PostImage]    |   \--- [ChatMessage]
                                |
                           [ViewingAppointment]

HÀNG GIỮA PHẢI:
[Favorite] --- [Post] --- [RentalRequest] ..> [Deposit]
                                   |
                                   ..> [RentalContract] --- [MonthlyBill]
                                                         \
                                                          \--- [RoomReview]

HÀNG DƯỚI:
[Deposit] ---- [PaymentTransaction] ---- [MonthlyBill]
[User] ------------------------------ [Notification]
```

- Đặt `User` ở giữa hàng trên; `TenantProfile` bên trái và `LandlordProfile` bên phải. Vẽ association `1` gần `User`, `0..1` gần mỗi profile.
- Đặt `Room` ở trung tâm trái; `RoomCategory` bên trái, `Post` bên phải, `PostImage` phía dưới Room.
- Đặt `RentalRequest` bên phải của `Post`; đặt `Deposit` ngay trên/phải và `RentalContract` dưới/phải. Dùng nét đứt có mũi tên từ `RentalRequest` sang hai class này vì đó là dependency qua ID/BLL.
- Đặt `MonthlyBill` bên phải của `RentalContract`; `PaymentTransaction` dưới giữa `Deposit` và `MonthlyBill`. Ghi `0..*` ở đầu `PaymentTransaction`; thêm ghi chú XOR.
- Đặt `ViewingAppointment`, `Favorite`, `Report`, `ChatMessage`, `Notification` ở vùng dưới hoặc hai bên để giảm đường chéo. Với `ChatMessage`, vẽ hai đường từ `User`, ghi rõ `SenderId` và `ReceiverId`.
- Không dùng ký hiệu tam giác Generalization giữa role; không dùng diamond Composition trong bản tổng quan này.

## 6. Phân biệt Model và BLL

- **Model/entity** là class dữ liệu được vẽ trong Class Diagram: ví dụ `Post`, `RentalContract`, `MonthlyBill`. Chúng chứa thuộc tính và navigation property; đa số không có method nghiệp vụ.
- **BLL** là class xử lý nghiệp vụ, không đưa chung vào biểu đồ lớp tổng quan: ví dụ `PostBLL.CreatePostAsync`, `RentalRequestBLL.TaoAsync`, `PaymentBLL.ProcessPaymentReturnAsync`, `MonthlyBillBLL.TaoAsync`.
- **Controller** nhận HTTP request và gọi BLL: ví dụ `PostController.CreatePost`, `PaymentController.PaymentReturn`. Controller/BLL nên xuất hiện trong Sequence Diagram, không cần ở Class Diagram nghiệp vụ.
- **DAL** ở project này được thể hiện chủ yếu bằng `ApplicationDbContext` và các `IEntityTypeConfiguration<T>` trong `DAL/Configurations/`; chúng là căn cứ xác định relationship, không phải class nghiệp vụ để đặt vào sơ đồ tổng quan.
